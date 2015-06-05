using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.AI
{
    public class MyBotAiming
    {
        private MyBotNavigation m_parent;
        private bool m_followMovement;
        private MyEntity m_aimTarget;
        private Vector3 m_rotationHint;
        private Vector3? m_relativeTarget; // serves as absolute target if m_aimTarget is null

        public Vector3 RotationHint { get { return m_rotationHint; } }

        public MyBotAiming(MyBotNavigation parent)
        {
            m_parent = parent;
            m_followMovement = true;
            m_rotationHint = Vector3.Zero;
        }

        public void SetTarget(MyEntity entity, Vector3? relativeTarget = null)
        {
            m_followMovement = false;
            m_aimTarget = entity;
            m_relativeTarget = relativeTarget;
            Update();
        }

        public void SetAbsoluteTarget(Vector3 absoluteTarget)
        {
            m_followMovement = false;
            m_aimTarget = null;
            m_relativeTarget = absoluteTarget;
            Update();
        }

        public void FollowMovement()
        {
            m_aimTarget = null;
            m_followMovement = true;
            m_relativeTarget = null;
        }

        public void StopAiming()
        {
            m_aimTarget = null;
            m_followMovement = false;
            m_relativeTarget = null;
        }

        public void Update()
        {
            if (m_followMovement)
            {
                MatrixD parentMatrix = m_parent.AimingPositionAndOrientation;
                Matrix invertedParentMatrix = m_parent.PositionAndOrientationInverted;
                Matrix invertedAimingMatrix = m_parent.AimingPositionAndOrientationInverted;
                Vector3 forward = m_parent.ForwardVector;

                CalculateRotationHint(ref parentMatrix, ref forward);
            }
            else if (m_aimTarget != null)
            {
                if (m_aimTarget.MarkedForClose)
                {
                    m_aimTarget = null;
                    m_rotationHint = Vector3.Zero;
                    return;
                }

                MatrixD parentMatrix = m_parent.AimingPositionAndOrientation;
                Vector3 transformedRelativeTarget = (Vector3)Vector3D.Transform(m_relativeTarget.Value, m_aimTarget.PositionComp.WorldMatrix);
                Vector3 aimingForward = transformedRelativeTarget - m_parent.AimingPositionAndOrientation.Translation;
                aimingForward.Normalize();
                CalculateRotationHint(ref parentMatrix, ref aimingForward);
                //CalculateRotationHint(ref invertedParentMatrix, ref invertedAimingMatrix, ref forward, ref aimingForward);
            }
            else if (m_relativeTarget.HasValue)
            { // relative target is in world coordinates
                MatrixD parentMatrix = m_parent.AimingPositionAndOrientation;
                Vector3 aimingForward = m_relativeTarget.Value - m_parent.AimingPositionAndOrientation.Translation;
                aimingForward.Normalize();
                CalculateRotationHint(ref parentMatrix, ref aimingForward);
                //CalculateRotationHint(ref invertedParentMatrix, ref invertedAimingMatrix, ref forward, ref aimingForward);
            }
            else
            {
                m_rotationHint = Vector3.Zero;
            }
        }

        private void CalculateRotationHint(ref MatrixD parentMatrix, ref Vector3 desiredForward)
        {
            if (desiredForward.LengthSquared() == 0)
            {
                m_rotationHint.X = m_rotationHint.Y = 0;
                return;
            }

            Vector3D desiredForwardXZ = Vector3D.Reject(desiredForward, parentMatrix.Up);
            desiredForwardXZ.Normalize();
            desiredForwardXZ.AssertIsValid();

            Vector3D desiredForwardYZ = Vector3D.Reject(desiredForward, parentMatrix.Right);
            desiredForwardYZ.Normalize();
            desiredForwardYZ.AssertIsValid();

            /*Vector3D parentForward = parentMatrix.Forward;
            parentForward.Normalize();
            parentForward.AssertIsValid();

            // Flip Z component of the parent forward, when the desiredForwardYZ Z component is oposite sign ???
            // And it's not used...
            if (desiredForwardYZ.Z * parentForward.Z < 0)
                parentForward.Z = -parentForward.Z;*/
            
            double angleY = 0;
            double angleX = 0;

            angleX = Vector3D.Dot(parentMatrix.Forward, desiredForwardYZ);
            angleX = MathHelper.Clamp(angleX, -1, 1);
            angleX = Math.Acos(angleX);
            if (desiredForwardYZ.Y > parentMatrix.Forward.Y) // rotate in correct direction
                angleX = -angleX;

            angleY = Vector3D.Dot(parentMatrix.Forward, desiredForwardXZ);
            angleY = MathHelper.Clamp(angleY, -1, 1);
            angleY = Math.Acos(angleY);
            var det = desiredForwardXZ.X * parentMatrix.Forward.Z - desiredForwardXZ.Z * parentMatrix.Forward.X;
            if (det > 0) // rotate in correct direction
                angleY = -angleY;
           
            m_rotationHint.X = MathHelper.Clamp((float)angleY, -3.0f, 3.0f);
            m_rotationHint.Y = MathHelper.Clamp((float)angleX, -3.0f, 3.0f);
            Vector3D localDesiredForward = VRageMath.Vector3D.TransformNormal(desiredForward, VRageMath.MatrixD.Invert(parentMatrix));
        }
    }
}
