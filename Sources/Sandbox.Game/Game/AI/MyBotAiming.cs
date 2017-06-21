using Sandbox.Game.AI.Navigation;
using Sandbox.Common;
using Sandbox.Game.AI.BehaviorTree;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Weapons;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.GameSystems;
using VRageRender;
using Sandbox.Engine.Utils;
using VRage.Game.Entity;

namespace Sandbox.Game.AI
{
    public class MyBotAiming
    {
        private enum AimingMode : byte
        {
            FIXATED,
            TARGET,
            FOLLOW_MOVEMENT,
        }

        public const float MISSING_PROBABILITY = 0.3f;        
        private MyBotNavigation m_parent;
        private AimingMode m_mode;
        private MyEntity m_aimTarget;
        private Vector3 m_rotationHint;
        private Vector3? m_relativeTarget; // serves as absolute target if m_aimTarget is null

        public Vector3 RotationHint { get { return m_rotationHint; } }

        private Vector3 m_dbgDesiredForward;

        public MyBotAiming(MyBotNavigation parent)
        {
            m_parent = parent;
            m_mode = AimingMode.FOLLOW_MOVEMENT;
            m_rotationHint = Vector3.Zero;
        }

        public void SetTarget(MyEntity entity, Vector3? relativeTarget = null)
        {
            m_mode = AimingMode.TARGET;
            m_aimTarget = entity;
            m_relativeTarget = relativeTarget;
            Update();
        }

        public void SetAbsoluteTarget(Vector3 absoluteTarget)
        {
            m_mode = AimingMode.TARGET;
            m_aimTarget = null;
            m_relativeTarget = absoluteTarget;
            Update();
        }

        public void FollowMovement()
        {
            m_aimTarget = null;
            m_mode = AimingMode.FOLLOW_MOVEMENT;
            m_relativeTarget = null;
        }

        public void StopAiming()
        {
            m_aimTarget = null;
            m_mode = AimingMode.FIXATED;
            m_relativeTarget = null;
        }

        public void Update()
        {
            if (m_mode == AimingMode.FIXATED)
            {
                m_rotationHint = Vector3.Zero;
                return;
            }

            var character = m_parent.BotEntity as MyCharacter;

            MatrixD parentMatrix = m_parent.AimingPositionAndOrientation;

            // Look forward mode
            if (m_mode == AimingMode.FOLLOW_MOVEMENT)
            {
                Matrix invertedParentMatrix = m_parent.PositionAndOrientationInverted;
                Matrix invertedAimingMatrix = m_parent.AimingPositionAndOrientationInverted;
                Vector3 forward = m_parent.ForwardVector;

                CalculateRotationHint(ref parentMatrix, ref forward);
            }
            // Relative targetting mode
            else if (m_aimTarget != null)
            {
                if (m_aimTarget.MarkedForClose)
                {
                    m_aimTarget = null;
                    m_rotationHint = Vector3.Zero;
                    return;
                }

                Vector3 transformedRelativeTarget;
                if (m_relativeTarget.HasValue)
                    transformedRelativeTarget = (Vector3)Vector3D.Transform(m_relativeTarget.Value, m_aimTarget.PositionComp.WorldMatrix);
                else
                    transformedRelativeTarget = (Vector3)m_aimTarget.PositionComp.WorldMatrix.Translation;

                PredictTargetPosition(ref transformedRelativeTarget, character);                
                
                Vector3 aimingForward = transformedRelativeTarget - m_parent.AimingPositionAndOrientation.Translation;
                aimingForward.Normalize();
                CalculateRotationHint(ref parentMatrix, ref aimingForward);
                //CalculateRotationHint(ref invertedParentMatrix, ref invertedAimingMatrix, ref forward, ref aimingForward);
                               
                if (character != null)
                {
                    character.AimedPoint = transformedRelativeTarget;
                    AddErrorToAiming(character, m_aimTarget.PositionComp != null ? m_aimTarget.PositionComp.LocalVolume.Radius * 1.5f : 1);
                }
            }
            // Absolute targetting mode
            else if (m_relativeTarget.HasValue)
            { // relative target is in world coordinates
                Vector3 aimingForward = m_relativeTarget.Value - m_parent.AimingPositionAndOrientation.Translation;
                aimingForward.Normalize();
                CalculateRotationHint(ref parentMatrix, ref aimingForward);
                //CalculateRotationHint(ref invertedParentMatrix, ref invertedAimingMatrix, ref forward, ref aimingForward);

                if (character != null)
                {
                    character.AimedPoint = m_relativeTarget.Value;
                }
            }
            // Shouldn't happen
            else
            {
                Debug.Assert(false, "Targeting is invalid in a bot");
                m_rotationHint = Vector3.Zero;
            }
        }

        private void AddErrorToAiming(MyCharacter character, float errorLenght)
        {
            if (MyUtils.GetRandomFloat() < MISSING_PROBABILITY)
            {
                character.AimedPoint += Vector3D.Normalize(MyUtils.GetRandomVector3()) * errorLenght;
            }
        }

        private void PredictTargetPosition(ref Vector3 transformedRelativeTarget, MyCharacter bot)
        {            
            if (bot != null && bot.CurrentWeapon != null)
            {
                MyGunBase gun = bot.CurrentWeapon.GunBase as MyGunBase;
                if (gun != null)
                {
                    float time;
                    MyWeaponPrediction.GetPredictedTargetPosition(gun, bot, m_aimTarget, out transformedRelativeTarget, out time, VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * MyBehaviorTreeCollection.UPDATE_COUNTER);
                }
            }
        }

        private void CalculateRotationHint(ref MatrixD parentMatrix, ref Vector3 desiredForward)
        {
            Vector3D gravityUp = m_parent.UpVector;

            if (desiredForward.LengthSquared() == 0)
            {
                m_rotationHint.X = m_rotationHint.Y = 0;
                return;
            }

            // CH: Note: The XZ and YZ in the vector names does not mean that other components are zero!
            // They would be zero in local coords, but these vectors are global!
            Vector3D desiredForwardXZ = Vector3D.Reject(desiredForward, parentMatrix.Up);
            Vector3D desiredForwardYZ = Vector3D.Reject(desiredForward, parentMatrix.Right);

            desiredForwardXZ.Normalize();
            desiredForwardYZ.Normalize();

            desiredForwardXZ.AssertIsValid();
            desiredForwardYZ.AssertIsValid();

            m_dbgDesiredForward = desiredForward;

            /*Vector3D parentForward = parentMatrix.Forward;
            parentForward.Normalize();
            parentForward.AssertIsValid();

            // Flip Z component of the parent forward, when the desiredForwardYZ Z component is oposite sign ???
            // And it's not used...
            if (desiredForwardYZ.Z * parentForward.Z < 0)
                parentForward.Z = -parentForward.Z;*/
            
            double angleY = 0;
            double angleX = 0;

            double parentHeight = Vector3D.Dot(parentMatrix.Forward, gravityUp);
            double desiredHeight = Vector3D.Dot(desiredForward, gravityUp);

            angleX = Vector3D.Dot(parentMatrix.Forward, desiredForwardYZ);
            angleX = MathHelper.Clamp(angleX, -1, 1);
            angleX = Math.Acos(angleX);
            if (desiredHeight > parentHeight) // rotate in correct direction
                angleX = -angleX;

            angleY = Vector3D.Dot(parentMatrix.Forward, desiredForwardXZ);
            angleY = MathHelper.Clamp(angleY, -1, 1);
            angleY = Math.Acos(angleY);
            double det = Vector3D.Dot(parentMatrix.Right, desiredForwardXZ);
            if (det < 0) // rotate in correct direction
                angleY = -angleY;
           
            m_rotationHint.X = MathHelper.Clamp((float)angleY, -3.0f, 3.0f);
            m_rotationHint.Y = MathHelper.Clamp((float)angleX, -3.0f, 3.0f);
        }

        public void DebugDraw(MatrixD posAndOri)
        {
            if (MyDebugDrawSettings.DEBUG_DRAW_BOT_AIMING == false) return;

            Vector3 pos2 = posAndOri.Translation;
            MyRenderProxy.DebugDrawArrow3D(pos2, pos2 + posAndOri.Right, Color.Red, Color.Red, false, text: "X");
            MyRenderProxy.DebugDrawArrow3D(pos2, pos2 + posAndOri.Up, Color.Green, Color.Green, false, text: "Y");
            MyRenderProxy.DebugDrawArrow3D(pos2, pos2 + posAndOri.Forward, Color.Blue, Color.Blue, false, text: "-Z");
            MyRenderProxy.DebugDrawArrow3D(pos2, pos2 + m_dbgDesiredForward, Color.Yellow, Color.Yellow, false, text: "Des.-Z");

            Vector3D tip = pos2 + posAndOri.Forward;
            MyRenderProxy.DebugDrawArrow3D(tip, tip + m_rotationHint.X * 10.0f * posAndOri.Right, Color.Salmon, Color.Salmon, false, text: "Rot.X");
            MyRenderProxy.DebugDrawArrow3D(tip, tip - m_rotationHint.Y * 10.0f * posAndOri.Up, Color.LimeGreen, Color.LimeGreen, false, text: "Rot.Y");

            var character = m_parent.BotEntity as MyCharacter;
            if (character != null)
                MyRenderProxy.DebugDrawSphere(character.AimedPoint, 0.2f, Color.Orange, 1, false);
        }
    }
}
