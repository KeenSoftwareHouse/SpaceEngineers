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
using VRage.Game.Entity;
using VRage.Profiler;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.AI.Navigation
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
        private bool m_wasStopped;
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

        public Vector3D TargetPoint
        {
            get
            {
                return m_destinationSphere.GetDestination();
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
            m_stuckDetection = new MyStuckDetection(0.05f, MathHelper.ToRadians(2f));

            m_destinationSphere = new MyDestinationSphere(ref Vector3D.Zero, 0.0f);

            m_wasStopped = false;
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

        public void Update(int behaviorTicks)
        {
            m_stuckDetection.SetCurrentTicks(behaviorTicks);

            ProfilerShort.Begin("MyBotNavigation.Update");
            AssertIsValid();

            if (m_entity == null) return;

            ProfilerShort.Begin("UpdateMatrices");
            UpdateMatrices();
            ProfilerShort.End();

            m_gravityDirection = MyGravityProviderSystem.CalculateTotalGravityInPoint(m_entity.PositionComp.WorldMatrix.Translation);
            if (!Vector3.IsZero(m_gravityDirection, 0.01f))
                m_gravityDirection = Vector3D.Normalize(m_gravityDirection);

            if (MyPerGameSettings.NavmeshPresumesDownwardGravity)
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

            if (m_speed < 0.1f)// hotfix for flickering of animation from running left to running right
                m_speed = 0;

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
                var correctedForward = m_forwardVector - m_correction;
                var normalizedCorrectedForward = Vector3D.Normalize(correctedForward);
                var currentCos = Vector3D.Dot(normalizedCorrectedForward, m_forwardVector);
                if (currentCos < maximumCos)
                {
                    float currentAngle = (float)(Math.Acos(MathHelper.Clamp((double)currentCos, -1, 1)));
                    m_rotationSpeedModifier = currentAngle / m_maximumRotationAngle.Value;
                    
                    // CH: This is only an approximation, but it's quick
                    m_correction /= m_rotationSpeedModifier;
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

            if (HasRotation(10.0f))
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
	                var jetpack = character.JetpackComp;
                    if ((jetpack != null && !jetpack.TurnedOn) && m_path.Flying)
                        jetpack.TurnOnJetpack(true);
                    else if ((jetpack != null && jetpack.TurnedOn) && !m_path.Flying)
                        jetpack.TurnOnJetpack(false);
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
                    character.MoveAndRotate(worldLocal * m_speed, new Vector2(rot.Y * 30.0f, rot.X * 30.0f), 0.0f);
                }
                else if (m_speed == 0.0f)
                {
                    if (HasRotation())
                    {
                        float rotationMultiplier = (character.WantsWalk || character.IsCrouching) ? 1 : 2;
                        Vector3 rot = m_aiming.RotationHint * m_rotationSpeedModifier;
                        character.MoveAndRotate(Vector3.Zero, new Vector2(rot.Y * 20.0f * rotationMultiplier, rot.X * 25.0f * rotationMultiplier), 0.0f);
                        m_wasStopped = false;
                    }
                    else if (m_wasStopped) // Make sure we call MoveAndRotate(0) only once
                    {
                        character.MoveAndRotate(Vector3.Zero, Vector2.Zero, 0.0f);
                        m_wasStopped = true;
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
            //m_path.SetTarget(position, 0.5f, relativeEntity, 1);
        }

        /// <summary>
        /// Tells the bot to go to the given world coordinate.
        /// If the relative entity is set, the coordinate is updated automatically as the entity moves
        /// </summary>
        public void Goto(IMyDestinationShape destination, MyEntity relativeEntity = null)
        {
            if (MyAIComponent.Static.Pathfinding == null) return;

            var path = MyAIComponent.Static.Pathfinding.FindPathGlobal(PositionAndOrientation.Translation, destination, relativeEntity);
            if (path == null)
            {
                return;
            }

            m_path.SetPath(path);
            m_stuckDetection.Reset();
        }

        public void GotoNoPath(Vector3D worldPosition, float radius = 0.0f, MyEntity relativeEntity = null, bool resetStuckDetection = true)
        {
            m_path.SetTarget(worldPosition, radius: radius, relativeEntity: relativeEntity, weight: 1.0f, fly: false);
            if (resetStuckDetection)
                m_stuckDetection.Reset();
        }

        public bool CheckReachability(Vector3D worldPosition, float threshold, MyEntity relativeEntity = null)
        {
            if (MyAIComponent.Static.Pathfinding == null) return false;

            m_destinationSphere.Init(ref worldPosition, 0.0f);
            return MyAIComponent.Static.Pathfinding.ReachableUnderThreshold(PositionAndOrientation.Translation, m_destinationSphere, threshold);
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
            m_stuckDetection.Stop();
        }

        public void StopImmediate(bool forceUpdate = false)
        {
            Stop();
            m_speed = 0;
            if (forceUpdate)
                MoveCharacter();
        }

        public void FollowPath(IMyPath path)
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

        public void AimWithMovement()
        {
            m_aiming.FollowMovement();
        }

        public void StopAiming()
        {
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

            m_aiming.DebugDraw(m_aimingPositionAndOrientation);

            if (MyDebugDrawSettings.DEBUG_DRAW_BOT_STEERING)
            {
                foreach (var steering in m_steerings)
                {
                    steering.DebugDraw();
                }
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_BOT_NAVIGATION)
            {
                Vector3 pos = PositionAndOrientation.Translation;// + /*PositionAndOrientation.Up * 1.5f + */ForwardVector;
                Vector3 rightVector = Vector3.Cross(m_forwardVector, UpVector);

                if (Stuck)
                    MyRenderProxy.DebugDrawSphere(pos, 1.0f, Color.Red.ToVector3(), 1.0f, false);

                //MyRenderProxy.DebugDrawLine3D(pos, pos + ForwardVector, Color.Blue, Color.Blue, false);

                //var normalizedCorrection = Vector3D.Normalize(m_correction);
                //var normalizedCorrectedDirXZ = normalizedCorrection + m_forwardVector;
                //normalizedCorrectedDirXZ = Vector3D.Normalize(Vector3D.Reject(normalizedCorrectedDirXZ, Vector3D.Up));
                //MyRenderProxy.DebugDrawLine3D(pos, pos + normalizedCorrectedDirXZ * 3, Color.Lime, Color.Lime, false);
                MyRenderProxy.DebugDrawArrow3D(pos, pos + ForwardVector, Color.Blue, Color.Blue, false, text: "Nav. FW");
                MyRenderProxy.DebugDrawArrow3D(pos + ForwardVector, pos + ForwardVector + m_correction, Color.LightBlue, Color.LightBlue, false, text: "Correction");
                //MyRenderProxy.DebugDrawLine3D(pos + ForwardVector, pos + ForwardVector + m_correction, Color.Yellow, Color.Yellow, false);
                //MyRenderProxy.DebugDrawSphere(pos + ForwardVector + m_correction, 0.05f, Color.Yellow.ToVector3(), 1.0f, true);

                if ( m_destinationSphere != null )
                {
                    m_destinationSphere.DebugDraw();
                }

                var character = this.BotEntity as MyCharacter;
                if (character != null)
                {
                    var viewMatrix = character.GetViewMatrix();
                    var worldMatrix = MatrixD.Invert(viewMatrix);
                    var headMatrix = character.GetHeadMatrix(true, true);

                    MyRenderProxy.DebugDrawLine3D(worldMatrix.Translation, Vector3D.Transform(Vector3D.Forward * 50, worldMatrix), Color.Yellow, Color.White, false);
                    MyRenderProxy.DebugDrawLine3D(headMatrix.Translation, Vector3D.Transform(Vector3D.Forward * 50, headMatrix), Color.Red, Color.Red, false);

                    if (character.CurrentWeapon != null)
                    {
                        var direction = character.CurrentWeapon.DirectionToTarget(character.AimedPoint);
                        var spos = (character.CurrentWeapon as MyEntity).WorldMatrix.Translation;

                        MyRenderProxy.DebugDrawSphere(character.AimedPoint, 1.0f, Color.Yellow, 1.0f, false);
                        MyRenderProxy.DebugDrawLine3D(spos, spos + direction * 20, Color.Purple, Color.Purple, false);
                    }
                }
            }
        }
    }
}
