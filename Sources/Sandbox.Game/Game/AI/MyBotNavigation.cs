using Sandbox.Engine.Utils;
using Sandbox.Game.AI.Pathfinding;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.GameSystems;
using Sandbox.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.AI
{
    public class MyBotNavigation
    {
        private List<MySteeringBase> m_steerings;
        private MyPathSteering m_path;
        private MyBotAiming m_aiming;

        private MyEntity m_entity;

        private MyDestinationSphere m_destinationSphere;

        private Vector3 m_forwardVector;
        private Vector3 m_correction;
        private Vector3 m_upVector;
        private float m_speed;
        private float m_rotationSpeedModifier;
        private Vector3 m_gravityDirection;
        private float? m_maximumRotationAngle; // radians

        private MyStuckDetection m_stuckDetection;

        /// <summary>
        /// Current wanted forward vector
        /// </summary>
        public Vector3 ForwardVector { get { return m_forwardVector; } }

        /// <summary>
        /// Current wanted up vector
        /// </summary>
        public Vector3 UpVector { get { return m_upVector; } }

        /// <summary>
        /// Current wanted Speed
        /// </summary>
        public float Speed { get { return m_speed; } }

        /// <summary>
        /// Whether the navigation is moving the bot towards a target. Beware: the bot could still be stuck
        /// </summary>
        public bool Navigating
        {
            get
            {
                return m_path.TargetSet;
            }
        }

        public bool Stuck
        {
            get
            {
                return m_stuckDetection.IsStuck;
            }
        }

        public MyEntity BotEntity
        {
            get
            {
                return m_entity;
            }
        }

        public float? MaximumRotationAngle
        {
            set { m_maximumRotationAngle = value; }
            get { return m_maximumRotationAngle; }
        }

        public Vector3 GravityDirection { get { return m_gravityDirection; } }

        private MatrixD m_worldMatrix;
        private MatrixD m_invWorldMatrix;
        private MatrixD m_aimingPositionAndOrientation;
        private MatrixD m_invAimingPositionAndOrientation;
        /// <summary>
        /// Current position and orientation of the controlled entity
        /// </summary>
        public MatrixD PositionAndOrientation
        {
            get
            {
                if (m_entity == null)
                {
                    Debug.Assert(false, "Getting position on bot navigation, but entity is not set");
                    return MatrixD.Identity;
                }
                else
                    return m_worldMatrix;
            }
        }

        public MatrixD PositionAndOrientationInverted
        {
            get
            {
                if (m_entity == null)
                {
                    Debug.Assert(false, "Getting position on bot navigation, but entity is not set");
                    return MatrixD.Identity;
                }
                else
                    return m_invWorldMatrix;
            }
        }

        public MatrixD AimingPositionAndOrientation
        {
            get
            {
                if (m_entity == null)
                {
                    Debug.Assert(false, "Getting aiming transformation on bot navigation, but entity is not set");
                    return MatrixD.Identity;
                }
                else
                    return m_aimingPositionAndOrientation;
            }
        }

        public MatrixD AimingPositionAndOrientationInverted
        {
            get
            {
                if (m_entity == null)
                {
                    Debug.Assert(false, "Getting aiming transformation on bot navigation, but entity is not set");
                    return MatrixD.Identity;
                }
                else
                    return m_invAimingPositionAndOrientation;
            }
        }

        public bool HasRotation(float epsilon = 0.0316f)
        {
            return m_aiming.RotationHint.LengthSquared() > epsilon * epsilon;
        }

        // radians
        public bool HasXRotation(float epsilon)
        {
            return Math.Abs(m_aiming.RotationHint.Y) > epsilon;
        }

        // radians
        public bool HasYRotation(float epsilon)
        {
            return Math.Abs(m_aiming.RotationHint.X) > epsilon;
        }

        public MyBotNavigation()
        {
            m_steerings = new List<MySteeringBase>();

            m_path = new MyPathSteering(this);
            m_steerings.Add(m_path);

            //m_steerings.Add(new MyCollisionDetectionSteering(this));

            m_aiming = new MyBotAiming(this);
            m_stuckDetection = new MyStuckDetection();

            m_destinationSphere = new MyDestinationSphere(ref Vector3D.Zero, 0.0f);
        }

        public void Cleanup()
        {
            foreach (var steering in m_steerings)
            {
                steering.Cleanup();
            }
        }

        public void ChangeEntity(IMyControllableEntity newEntity)
        {
            m_entity = newEntity == null ? null : newEntity.Entity;
            if (m_entity != null)
            {
                m_forwardVector = PositionAndOrientation.Forward;
                m_upVector = PositionAndOrientation.Up;
                m_speed = 0.0f;
                m_rotationSpeedModifier = 1;
            }
        }

        public void Update()
        {
            ProfilerShort.Begin("MyBotNavigation.Update");
            AssertIsValid();

            if (m_entity == null) return;

            ProfilerShort.Begin("UpdateMatrices");
            UpdateMatrices();
            ProfilerShort.End();

            m_gravityDirection = MyGravityProviderSystem.CalculateGravityInPoint(m_entity.PositionComp.WorldMatrix.Translation);
            if (!Vector3.IsZero(m_gravityDirection, 0.01f))
                m_gravityDirection.Normalize();

            if (MyFakes.NAVMESH_PRESUMES_DOWNWARD_GRAVITY)
                m_upVector = Vector3.Up;
            else
                m_upVector = -m_gravityDirection;

            if (!m_speed.IsValid())
            {
                m_forwardVector = PositionAndOrientation.Forward;
                m_speed = 0.0f;
                m_rotationSpeedModifier = 1;
            }

            ProfilerShort.Begin("Steering update");
            foreach (var steering in m_steerings)
            {
                ProfilerShort.Begin(steering.GetName());
                steering.Update();
                ProfilerShort.End();
            }
            ProfilerShort.End();

            ProfilerShort.Begin("Aiming");
            m_aiming.Update();
            ProfilerShort.End();

            ProfilerShort.Begin("Steering accumulate correction");
            CorrectMovement(m_aiming.RotationHint);
            ProfilerShort.End();

            ProfilerShort.Begin("MoveCharacter");
            MoveCharacter();
            ProfilerShort.End();

            AssertIsValid();
            ProfilerShort.End();
        }

        private void UpdateMatrices()
        {
            if (m_entity as MyCharacter != null)
            {
                var character = m_entity as MyCharacter;
                m_worldMatrix = character.WorldMatrix;
                m_invWorldMatrix = Matrix.Invert(m_worldMatrix);
                m_aimingPositionAndOrientation = character.GetHeadMatrix(true, true, false, true);
                m_invAimingPositionAndOrientation = MatrixD.Invert(m_aimingPositionAndOrientation);
            }
            else
            {
                m_worldMatrix = m_entity.PositionComp.WorldMatrix;
                m_invWorldMatrix = m_entity.PositionComp.WorldMatrixInvScaled;
                m_aimingPositionAndOrientation = m_worldMatrix;
                m_invAimingPositionAndOrientation = m_invWorldMatrix;
            }
        }

        private void AccumulateCorrection()
        {
            m_rotationSpeedModifier = 1;

            float totalWeight = 0.0f;
            // Accumulate correction from all steerings
            for (int i = 0; i < m_steerings.Count; ++i)
            {
                ProfilerShort.Begin(m_steerings[i].GetName());
                m_steerings[i].AccumulateCorrection(ref m_correction, ref totalWeight);
                ProfilerShort.End();
            }

            if (m_maximumRotationAngle.HasValue)
            {
                var maximumCos = Math.Cos(m_maximumRotationAngle.Value);
                var normalizedCorrection = Vector3D.Normalize(m_correction);
                var normalizedCorrectedDirXZ = normalizedCorrection + m_forwardVector;
                normalizedCorrectedDirXZ = Vector3D.Normalize(Vector3D.Reject(normalizedCorrectedDirXZ, Vector3D.Up));
                var currentCos = Vector3D.Dot(normalizedCorrectedDirXZ, PositionAndOrientation.Forward);
                if (currentCos < maximumCos)
                {
                    m_rotationSpeedModifier = (float)(Math.Acos(MathHelper.Clamp((double)currentCos, -1, 1)) / m_maximumRotationAngle.Value);
                     
                    var correctionLength = m_correction.Length();
                    var det = normalizedCorrectedDirXZ.X * PositionAndOrientation.Forward.Z - normalizedCorrectedDirXZ.Z * PositionAndOrientation.Forward.X;
                    var sign = Math.Sign(det);
                    var clampedDir = Vector3.TransformNormal(PositionAndOrientation.Forward, Matrix.CreateRotationY(sign * m_maximumRotationAngle.Value));
                    m_correction = normalizedCorrection;
                    m_correction.X = clampedDir.X;
                    m_correction.Z = clampedDir.Z;
                    m_correction *= correctionLength;
                }
            }

            if (totalWeight > 1.0f)
                m_correction /= totalWeight;
        }

        private void CorrectMovement(Vector3 rotationHint)
        {
            m_correction = Vector3.Zero;

            if (!Navigating)
            {
                m_speed = 0.0f;
                return;
            }

            AccumulateCorrection();

            if (rotationHint.Length() > 1.0f)
            {
                m_correction = Vector3.Zero;
                m_speed = 0.0f;
                m_stuckDetection.SetRotating(true);
            }
            else
            {
                m_stuckDetection.SetRotating(false);
            }

            // Correct the movement vector
            Vector3 movement = m_forwardVector * m_speed;
            movement += m_correction;
            m_speed = movement.Length();
            if (m_speed <= 0.001f)
            {
                m_speed = 0.0f;
            }
            else
            {
                m_forwardVector = movement / m_speed;
                if (m_speed > 1.0f)
                    m_speed = 1.0f;
            }
        }

        private void MoveCharacter()
        {
            var character = m_entity as MyCharacter;
            if (character != null)
            {
                if (m_speed != 0.0f/* && (character.IsIdle || (m_updateCounter++ % 1 == 0))*/)
                {
                    if (character.JetpackEnabled == false && m_path.Flying)
                        character.EnableJetpack(true);
                    else if (character.JetpackEnabled == true && !m_path.Flying)
                        character.EnableJetpack(false);
                    Vector3 worldLocal = Vector3.TransformNormal(m_forwardVector, character.PositionComp.WorldMatrixNormalizedInv);
                    Vector3 rot = m_aiming.RotationHint * m_rotationSpeedModifier;
                    if (m_path.Flying)
                    {
                        if (worldLocal.Y > 0.0f)
                        {
                            character.Up();
                        }
                        else
                        {
                            character.Down();
                        }
                    }
                    character.MoveAndRotate(worldLocal * m_speed, new Vector2(rot.Y * 10.0f, rot.X * 10.0f), 0.0f);
                }
                else if (m_speed == 0.0f)
                {
                    if (HasRotation())
                    {
                        float rotationMultiplier = (character.WantsWalk || character.IsCrouching) ? 1 : 2;
                        Vector3 rot = m_aiming.RotationHint * m_rotationSpeedModifier;
                        character.MoveAndRotate(Vector3.Zero, new Vector2(rot.Y * 20.0f * rotationMultiplier, rot.X * 25.0f * rotationMultiplier), 0.0f);
                    }
                    else if (!character.IsIdle)
                    {
                        character.MoveAndRotate(Vector3.Zero, Vector2.Zero, 0.0f);
                    }
                }
            }

            // Stuck detection
            m_stuckDetection.Update(m_worldMatrix.Translation, m_aiming.RotationHint);
        }

        public void AddSteering(MySteeringBase steering)
        {
            m_steerings.Add(steering);
        }

        public void RemoveSteering(MySteeringBase steering)
        {
            m_steerings.Remove(steering);
        }

        public bool HasSteeringOfType(Type steeringType)
        {
            foreach (var s in m_steerings)
            {
                if (s.GetType() == steeringType)
                {
                    return true;
                }
            }
            return false;
        }

        public void Goto(Vector3D position, float radius = 0.0f, MyEntity relativeEntity = null)
        {
            m_destinationSphere.Init(ref position, radius);
            Goto(m_destinationSphere, relativeEntity);
        }

        /// <summary>
        /// Tells the bot to go to the given world coordinate.
        /// If the relative entity is set, the coordinate is updated automatically as the entity moves
        /// </summary>
        public void Goto(IMyDestinationShape destination, MyEntity relativeEntity = null)
        {
            var path = MyAIComponent.Static.Pathfinding.FindPathGlobal(PositionAndOrientation.Translation, destination, relativeEntity);
            if (path == null)
            {
                return;
            }

            m_path.SetPath(path);
            m_stuckDetection.Reset();
        }

        public void GotoNoPath(Vector3D worldPosition, MyEntity relativeEntity = null)
        {
            m_path.SetTarget(worldPosition, relativeEntity: relativeEntity, weight: 1.0f, fly: false);
            m_stuckDetection.Reset();
        }

        /// <summary>
        /// Tells the bot to fly to the given world coordinate.
        /// If the relative entity is set, the coordinate is updated automatically as the entity moves
        /// </summary>
        public void Flyto(Vector3D worldPosition, MyEntity relativeEntity = null)
        {
            m_path.SetTarget(worldPosition, relativeEntity: relativeEntity, weight: 1.0f, fly: true);
            m_stuckDetection.Reset();
        }

        /// <summary>
        /// Stop the bot from moving.
        /// </summary>
        public void Stop()
        {
            m_path.UnsetPath();

            m_stuckDetection.Reset();
        }

        public void StopImmediate(bool forceUpdate = false)
        {
            Stop();
            m_speed = 0;
            if (forceUpdate)
                MoveCharacter();
        }

        public void FollowPath(MySmartPath path)
        {
            m_path.SetPath(path);
            m_stuckDetection.Reset();
        }

        public void AimAt(MyEntity entity, Vector3D? worldPosition = null)
        {
            if (worldPosition.HasValue)
            {
                if (entity != null)
                {
                    MatrixD worldMatrixInv = entity.PositionComp.WorldMatrixNormalizedInv;
                    Vector3 localPosition = (Vector3)Vector3D.Transform(worldPosition.Value, worldMatrixInv);
                    m_aiming.SetTarget(entity, localPosition);
                }
                else
                {
                    m_aiming.SetAbsoluteTarget(worldPosition.Value);
                }
            }
            else
            {
                m_aiming.SetTarget(entity);
            }
        }

        public void ResetAiming(bool followMovement)
        {
            if (followMovement)
                m_aiming.FollowMovement();
            else
                m_aiming.StopAiming();
        }

        [Conditional("DEBUG")]
        private void AssertIsValid()
        {
            Debug.Assert(m_forwardVector.X.IsValid());
            Debug.Assert(m_forwardVector.Y.IsValid());
            Debug.Assert(m_forwardVector.Z.IsValid());
            Debug.Assert(m_upVector.X.IsValid());
            Debug.Assert(m_upVector.Y.IsValid());
            Debug.Assert(m_upVector.Z.IsValid());
            Debug.Assert(m_speed.IsValid());
        }

        [Conditional("DEBUG")]
        public void DebugDraw()
        {
            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW == false) return;

            foreach (var steering in m_steerings)
            {
                steering.DebugDraw();
            }

            Vector3 pos = PositionAndOrientation.Translation;// + /*PositionAndOrientation.Up * 1.5f + */ForwardVector;
            Vector3 rightVector = Vector3.Cross(m_forwardVector, UpVector);

            if (Stuck)
                MyRenderProxy.DebugDrawSphere(pos, 1.0f, Color.Red.ToVector3(), 1.0f, false);

            //MyRenderProxy.DebugDrawLine3D(pos, pos + rightVector, Color.Red, Color.Red, false);
            //MyRenderProxy.DebugDrawSphere(pos + rightVector * m_aiming.RotationHint.X, 0.05f, Color.Red.ToVector3(), 1.0f, true);

            //MyRenderProxy.DebugDrawLine3D(pos, pos + UpVector, Color.Green, Color.Green, false);
            //MyRenderProxy.DebugDrawSphere(pos + UpVector * m_aiming.RotationHint.Y, 0.05f, Color.Green.ToVector3(), 1.0f, true);

            //MyRenderProxy.DebugDrawLine3D(pos, pos + ForwardVector, Color.Blue, Color.Blue, false);


            Vector3 pos2 = PositionAndOrientation.Translation + PositionAndOrientation.Up * 1.5f;
            MyRenderProxy.DebugDrawLine3D(pos2, pos2 + m_aimingPositionAndOrientation.Right, Color.Red, Color.Red, false);
            MyRenderProxy.DebugDrawLine3D(pos2, pos2 + m_aimingPositionAndOrientation.Up, Color.Green, Color.Green, false);
            MyRenderProxy.DebugDrawLine3D(pos2, pos2 + m_aimingPositionAndOrientation.Forward, Color.Blue, Color.Blue, false);

            //var normalizedCorrection = Vector3D.Normalize(m_correction);
            //var normalizedCorrectedDirXZ = normalizedCorrection + m_forwardVector;
            //normalizedCorrectedDirXZ = Vector3D.Normalize(Vector3D.Reject(normalizedCorrectedDirXZ, Vector3D.Up));
            //MyRenderProxy.DebugDrawLine3D(pos, pos + normalizedCorrectedDirXZ * 3, Color.Lime, Color.Lime, false);
            //MyRenderProxy.DebugDrawSphere(pos + ForwardVector, 0.05f, Color.Blue.ToVector3(), 1.0f, true);
            //MyRenderProxy.DebugDrawLine3D(pos + ForwardVector, pos + ForwardVector + m_correction, Color.Yellow, Color.Yellow, false);
            //MyRenderProxy.DebugDrawSphere(pos + ForwardVector + m_correction, 0.05f, Color.Yellow.ToVector3(), 1.0f, true);
        }
    }
}
