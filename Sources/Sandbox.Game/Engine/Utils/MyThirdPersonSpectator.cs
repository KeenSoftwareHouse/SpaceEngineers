using Havok;
using Sandbox.Common;
using Sandbox.Engine.Physics;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Input;
using VRageMath;

namespace Sandbox.Engine.Utils
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class MyThirdPersonSpectator : MySessionComponentBase
    {
        // Minimum distance camera-ship (also used for quick zoom)
        public const float MAX_DISTANCE = 200.0f;
        private const int SHAPE_CAST_MAX_STEP_COUNT = 10;
        private const float SHAPE_CAST_STEP = MAX_DISTANCE / SHAPE_CAST_MAX_STEP_COUNT;

        public static readonly float HEADSHAKE_POWER = 0.5f;

        public static readonly float CAMERA_RADIUS = 0.2f;

        public static readonly float BACKWARD_CUTOFF = 3.0f;

        public static readonly Vector3D LOOK_AT_DIRECTION = Vector3D.Normalize(new Vector3D(0, 5, 12));
        public static readonly double LOOK_AT_DEFAULT_LENGTH = 30;
        public static MyThirdPersonSpectator Static;

        #region Definitions
        // Helper class for spring physics parameters
        // Critical damping = 2*sqrt(Stiffness * Mass)
        public class SpringInfo
        {
            // Spring physics properties
            public float Stiffness;
            public float Damping;
            public float Mass;

            public SpringInfo(float stiffness, float damping, float mass)
            {
                Stiffness = stiffness;
                Damping = damping;
                Mass = mass;
            }

            public SpringInfo(SpringInfo spring)
            {
                Setup(spring);
            }

            public void Setup(SpringInfo spring)
            {
                Stiffness = spring.Stiffness;
                Damping = spring.Damping;
                Mass = spring.Mass;
            }

            public void Setup(SpringInfo a, SpringInfo b, float springChangeTime)
            {
                var amount = MathHelper.Saturate(springChangeTime);
                Stiffness = MathHelper.SmoothStep(a.Stiffness, b.Stiffness, amount);
                Damping = MathHelper.SmoothStep(a.Damping, b.Damping, amount);
                Mass = MathHelper.SmoothStep(a.Mass, b.Mass, amount);
            }
        }
        #endregion

        // Vector defining position between Target and Spectator
        Vector3D m_lookAt;
        Vector3D m_transformedLookAt;

        Vector3D m_target;
        Matrix m_targetOrientation = Matrix.Identity;

        // Current spectator position interpolated to desired position
        Vector3D m_position;
        // Position we are heading to
        Vector3D m_desiredPosition;
        // Current spectator position, interpolated and collision free
        Vector3D m_positionSafe;

        // Spring physics properties
        public SpringInfo NormalSpring;
        public SpringInfo StrafingSpring;
        public SpringInfo AngleSpring;

        // Desired spring parameters
        SpringInfo m_targetSpring;

        float m_springChangeTime;
        // Current spring parameters, we interpolate values in case StrafingSpring->NormalSpring
        SpringInfo m_currentSpring;

        Vector3 m_velocity;
        float m_angleVelocity;
        Quaternion m_orientation;
        Matrix m_orientationMatrix;

        List<MyPhysics.HitInfo> m_raycastList = new List<MyPhysics.HitInfo>();
        HashSet<Sandbox.ModAPI.IMyEntity> m_raycastHashSet = new HashSet<Sandbox.ModAPI.IMyEntity>();
        List<HkRigidBody> m_rigidList = new List<HkRigidBody>();

        bool m_saveSettings;

        /// <summary>
        /// Optimization.
        /// Used to step long shape casts so their AABBs are not huge,
        /// causing asteroids to generate too many physics shapes.
        /// </summary>
        uint m_updateCount;
        float[] m_lastShapeCastDistance = new float[SHAPE_CAST_MAX_STEP_COUNT];

        public MyThirdPersonSpectator()
        {
            Static = this;
            NormalSpring = new SpringInfo(20000, 1114, 50);
            StrafingSpring = new SpringInfo(36000, 2683, 50);
            AngleSpring = new SpringInfo(30, 14.5f, 2);

            m_targetSpring = NormalSpring;
            m_currentSpring = new SpringInfo(NormalSpring);

            m_lookAt = LOOK_AT_DIRECTION * LOOK_AT_DEFAULT_LENGTH;

            m_saveSettings = false;

            ResetDistance();

            for (int i = 0; i < m_lastShapeCastDistance.Length; ++i)
            {
                m_lastShapeCastDistance[i] = float.PositiveInfinity;
            }
        }

        // Handles interpolation of spring parameters
        private void UpdateCurrentSpring()
        {
            if (m_targetSpring == StrafingSpring)
            {
                m_currentSpring.Setup(m_targetSpring);
            }
            else if (m_targetSpring == NormalSpring)
            {
                m_currentSpring.Setup(StrafingSpring, NormalSpring, m_springChangeTime);
            }
            m_springChangeTime += MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
        }

        // Updates spectator position (spring connected to desired position)
        public override void UpdateAfterSimulation()
        {
            Sandbox.Game.Entities.IMyControllableEntity controlledEntity = MySession.ControlledEntity as Sandbox.Game.Entities.IMyControllableEntity;
            if (controlledEntity == null)
                return;

            var headMatrix = controlledEntity.GetHeadMatrix(true);

            if (controlledEntity is MyCharacter)
            {
                var character = controlledEntity as MyCharacter;
                headMatrix = character.Get3rdBoneMatrix(true, true);
            }

            m_targetOrientation = (Matrix)headMatrix.GetOrientation();
            m_target = headMatrix.Translation;


            //VRageRender.MyRenderProxy.DebugDrawAxis(headMatrix, 1, false);

            UpdateCurrentSpring();

            m_transformedLookAt = Vector3D.Transform(m_lookAt, m_targetOrientation);
            m_desiredPosition = m_target + m_transformedLookAt;

            //m_position = m_desiredPosition;
            //m_velocity = Vector3.Zero;
            // Calculate spring force
            Vector3D stretch = m_position - m_desiredPosition;

            Vector3D force = -m_currentSpring.Stiffness * stretch - m_currentSpring.Damping * m_velocity;
            force.AssertIsValid();

            // Apply acceleration
            Vector3 acceleration = (Vector3)force / m_currentSpring.Mass;
            m_velocity += acceleration * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            m_velocity.AssertIsValid();

            // Apply velocity
            if (!Sandbox.Game.Multiplayer.Sync.IsServer)
            {  //We are not able to interpolate camera correctly if position is updated through server
                m_position = m_desiredPosition;
            }
            else
            {
                m_position += m_velocity * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            }
            m_position.AssertIsValid();

            // Limit backward distance from target
            double backward = Vector3D.Dot((Vector3D)m_targetOrientation.Backward, (m_target - m_position));
            if (backward > -BACKWARD_CUTOFF)
            {
                m_position += (Vector3D)m_targetOrientation.Backward * (backward + BACKWARD_CUTOFF);
            }

            // Roll spring
            Quaternion targetOrientation = Quaternion.CreateFromRotationMatrix(m_targetOrientation);

            // Computes angle difference between current and target orientation
            var angleDifference = (float)Math.Acos(MathHelper.Clamp(Quaternion.Dot(m_orientation, targetOrientation), -1, 1));
            // Normalize angle
            angleDifference = angleDifference > MathHelper.PiOver2 ? MathHelper.Pi - angleDifference : angleDifference;

            // Compute spring physics
            float angleForce = -AngleSpring.Stiffness * angleDifference - AngleSpring.Damping * m_angleVelocity;
            m_angleVelocity += angleForce / AngleSpring.Mass * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            if (angleDifference > 0)
            {
                float factor = Math.Abs(m_angleVelocity * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS / angleDifference);
                if (angleDifference > MathHelper.PiOver4)
                {
                    factor = Math.Max(factor, 1.0f - MathHelper.PiOver4 / angleDifference);
                }
                factor = MathHelper.Clamp(factor, 0, 1);
                m_orientation = Quaternion.Slerp(m_orientation, targetOrientation, factor);
                m_orientationMatrix = Matrix.CreateFromQuaternion(m_orientation);
            }

            if (m_saveSettings)
            {
                MySession.SaveControlledEntityCameraSettings(false);
                m_saveSettings = false;
            }

            ++m_updateCount;
        }


        /// <summary>
        /// Handles camera collisions with environment
        /// </summary>
        /// <param name="controlledEntity"></param>
        /// <param name="shakeActive"></param>
        /// <param name="headPosition"></param>
        /// <param name="headDirection"></param>
        /// <returns>False if no correct position was found</returns>
        private bool HandleIntersection(MyEntity controlledEntity, MyOrientedBoundingBoxD safeOBB, bool requireRaycast, bool shakeActive, Vector3D headPosition, Vector3 headDirection)
        {
            var line = new LineD(m_target, m_position);

            var safeOBBLine = new LineD(line.From, line.From + line.Direction * 2 * safeOBB.HalfExtent.Length());
            Vector3D castStartSafe;
            {
                MyOrientedBoundingBoxD safeObbWithCollisionExtents = new MyOrientedBoundingBoxD(safeOBB.Center, safeOBB.HalfExtent + 2 * CAMERA_RADIUS, safeOBB.Orientation);
                double? safeIntersection = safeObbWithCollisionExtents.Intersects(ref safeOBBLine);
                if (!safeIntersection.HasValue)
                    safeIntersection = safeOBB.HalfExtent.Length();
                double safeDistance = safeIntersection.Value;
                castStartSafe = line.From + line.Direction * safeDistance;
            }

            {
                double? unsafeIntersection = safeOBB.Intersects(ref safeOBBLine);
                if (!requireRaycast && unsafeIntersection.HasValue)
                {
                    var castStartUnsafe = line.From + line.Direction * unsafeIntersection.Value;
                    var castEndUnsafe = castStartSafe + line.Direction;
                    // short raycast, not causing problems with asteroids generating geometry
                    Physics.MyPhysics.CastRay(castStartUnsafe, castEndUnsafe, m_raycastList, MyPhysics.DefaultCollisionLayer);
                    if (!IsRaycastOK(m_raycastList))
                    {
                        return false;
                    }
                }
            }

            if (requireRaycast)
            {
                // short raycast, not causing problems with asteroids generating geometry
                Physics.MyPhysics.CastRay(line.From, castStartSafe + line.Direction, m_raycastList, MyPhysics.DefaultCollisionLayer);
                if (!IsRaycastOK(m_raycastList))
                {
                    return false;
                }
            }

            HkShape shape = new HkSphereShape(CAMERA_RADIUS);
            try
            {
                // small shape, not causing problems with asteroids generating geometry
                Physics.MyPhysics.GetPenetrationsShape(shape, ref castStartSafe, ref Quaternion.Identity, m_rigidList, 15);
                if (m_rigidList.Count > 0)
                {
                    bool sameGrid = false;
                    if (MySession.ControlledEntity != null && m_rigidList[0] != null)
                    {
                        sameGrid = m_rigidList[0].UserObject == ((MyEntity)MySession.ControlledEntity).Physics;
                    }

                    if (sameGrid)
                        castStartSafe += line.Direction;
                }

                var shapeCastLine = new LineD(castStartSafe, m_position);
                uint steps = 1;
                uint stepIdx = 0;
                if (shapeCastLine.Length > SHAPE_CAST_STEP)
                {
                    steps = (uint)Math.Ceiling(shapeCastLine.Length / SHAPE_CAST_STEP);
                    if (steps >= SHAPE_CAST_MAX_STEP_COUNT)
                        steps = SHAPE_CAST_MAX_STEP_COUNT - 1;
                    stepIdx = m_updateCount % steps;
                    m_lastShapeCastDistance[stepIdx] = float.PositiveInfinity;

                    Vector3D step = shapeCastLine.Direction * (shapeCastLine.Length / steps);
                    shapeCastLine = new LineD(castStartSafe + stepIdx * step, castStartSafe + (stepIdx + 1) * step);
                }

                if (false)
                {
                    BoundingBoxD bbox = BoundingBoxD.CreateInvalid();
                    bbox.Include(new BoundingSphereD(shapeCastLine.From, CAMERA_RADIUS));
                    bbox.Include(new BoundingSphereD(shapeCastLine.To, CAMERA_RADIUS));
                    VRageRender.MyRenderProxy.DebugDrawAABB(bbox, Color.Crimson, 1f, 1f, true);
                }

                var matrix = MatrixD.CreateTranslation(shapeCastLine.From);
                    HkContactPointData? cpd;
                if (controlledEntity.Physics.CharacterProxy != null)
                    cpd = MyPhysics.CastShapeReturnContactData(shapeCastLine.To, shape, ref matrix, controlledEntity.Physics.CharacterCollisionFilter, 0.0f); 
                else
                    cpd = MyPhysics.CastShapeReturnContactData(shapeCastLine.To, shape, ref matrix, HkGroupFilter.CalcFilterInfo(MyPhysics.DefaultCollisionLayer,0), 0.0f);
                if (cpd.HasValue)
                {
                    var point = shapeCastLine.From + shapeCastLine.Direction * shapeCastLine.Length * cpd.Value.DistanceFraction;
                    m_lastShapeCastDistance[stepIdx] = (float)(castStartSafe - point).Length();
                }
                else
                {
                    m_lastShapeCastDistance[stepIdx] = float.PositiveInfinity;
                }


                float? dist = null;
                for (int i = 0; i < steps; ++i)
                {
                    if (m_lastShapeCastDistance[i] != float.PositiveInfinity)
                        dist = Math.Min(m_lastShapeCastDistance[i], dist ?? float.PositiveInfinity);
                }

                if (dist.HasValue)
                {
                    if (dist == 0.0f)
                    {
                        return false;
                    }
                    else
                    {
                        m_positionSafe = castStartSafe + shapeCastLine.Direction * dist.Value;
                    }
                }
                else
                {
                    m_positionSafe = m_position;
                }
                return true;
            }
            finally
            {
                shape.RemoveReference();
            }
        }

        private bool IsRaycastOK(List<MyPhysics.HitInfo> m_raycastList)
        {
            m_raycastHashSet.Clear();
            foreach (var rb in m_raycastList)
            {
                if (rb.HkHitInfo.Body == null
                    || rb.HkHitInfo.Body.UserObject == null
                    || !(rb.HkHitInfo.Body.UserObject is MyPhysicsBody))
                    continue;
                if (rb.HkHitInfo.Body.GetEntity() is IMyHandheldGunObject<Sandbox.Game.Weapons.MyDeviceBase>) // ignore player weapons
                    continue;

                m_raycastHashSet.Add(((MyPhysicsBody)rb.HkHitInfo.Body.UserObject).Entity);
            }

            if (m_raycastHashSet.Count > 1)
            {
                return false;
            }

            if (m_raycastHashSet.Count == 1 && MySession.ControlledEntity == null)
            {
                return false;
            }

            if (m_raycastHashSet.Count == 1 && m_raycastHashSet.FirstElement() != ((MyEntity)MySession.ControlledEntity).GetTopMostParent())
            {
                return false;
            }

            m_raycastHashSet.Clear();
            return true;
        }

        public MatrixD GetViewMatrix(float fov, float zoomLevel, bool shakeActive, Vector3D headPosition, Vector3 headDirection)
        {
            System.Diagnostics.Debug.Assert(m_lookAt.LengthSquared() > 0);

            Vector3D position = m_positionSafe;
            Matrix orientation = m_orientationMatrix;

            var distance = (m_target - position).Length();

            position.AssertIsValid();
            distance.AssertIsValid();

            // Push ship down (crosshair approx in middle of screen)
            var shipVerticalShift = Math.Tan(fov / 2) * 0.1 * distance;
            Vector3D lookVector = m_target + (Vector3D)orientation.Up * shipVerticalShift - position;

            float zoomPhase = MathHelper.Clamp((1.0f - zoomLevel) * 4, 0, 1);

            if (zoomLevel != 1)
            {
                // Normalize directions for more linear interpolation
                Vector3 lookDirection = (Vector3)Vector3D.Normalize(lookVector);
                Vector3 crosshairDirection = (Vector3)Vector3D.Normalize(GetCrosshair() - position);
                lookVector = (Vector3D)Vector3.Lerp(lookDirection, crosshairDirection, zoomPhase);
            }

            // Apply headshake
            if (shakeActive)
            {
                position += (Vector3D)Vector3D.Transform(headPosition, orientation) * distance * HEADSHAKE_POWER;
                Matrix matrixRotation = Matrix.CreateFromAxisAngle(Vector3.Forward, headDirection.Z) * Matrix.CreateFromAxisAngle(Vector3.Right, headDirection.X);
                lookVector = Vector3D.Transform(lookVector, matrixRotation);
            }

            return MatrixD.CreateLookAt(position, position + lookVector, m_targetOrientation.Up);
        }

        public MatrixD GetViewMatrix()
        {
            IMyCameraController cameraController = MySession.Static.CameraController;
            if (cameraController == null)
                return MatrixD.Identity;

            return GetViewMatrix(
                MySector.MainCamera.FieldOfView,
                1,
                false,
                m_target,
                m_targetOrientation.Forward);
        }

        public bool IsCameraPositionOk()
        {
            IMyCameraController cameraController = MySession.Static.CameraController;
            if (cameraController == null)
                return true;

            MyEntity topControlledEntity = ((MyEntity)cameraController).GetTopMostParent();
            if (topControlledEntity.Closed) return false;

            var localAABBHr = topControlledEntity.PositionComp.LocalAABBHr;
            Vector3D center = Vector3D.Transform((Vector3D)localAABBHr.Center, topControlledEntity.WorldMatrix);

            var safeOBB = new MyOrientedBoundingBoxD(center, localAABBHr.HalfExtents, Quaternion.CreateFromRotationMatrix(topControlledEntity.WorldMatrix.GetOrientation()));
            //VRageRender.MyRenderProxy.DebugDrawOBB(safeOBB, Vector3.One, 1, false, false);
            //VRageRender.MyRenderProxy.DebugDrawAxis(topControlledEntity.WorldMatrix, 2, false);

            bool camPosIsOk = HandleIntersection(topControlledEntity, safeOBB, topControlledEntity is Sandbox.Game.Entities.Character.MyCharacter, true, m_target, m_targetOrientation.Forward);

            return camPosIsOk;
        }

        public bool IsCameraPositionOk(Matrix worldMatrix)
        {
            IMyCameraController cameraController = MySession.Static.CameraController;
            if (cameraController == null)
                return true;

            MyEntity topControlledEntity = ((MyEntity)cameraController).GetTopMostParent();
            if (topControlledEntity.Closed) return false;

            var localAABBHr = topControlledEntity.PositionComp.LocalAABBHr;
            Vector3D center = Vector3D.Transform((Vector3D)localAABBHr.Center, worldMatrix);

            var safeOBB = new MyOrientedBoundingBoxD(center, localAABBHr.HalfExtents, Quaternion.CreateFromRotationMatrix(worldMatrix.GetOrientation()));
            //VRageRender.MyRenderProxy.DebugDrawOBB(safeOBB, Vector3.One, 1, false, false);
            //VRageRender.MyRenderProxy.DebugDrawAxis(topControlledEntity.WorldMatrix, 2, false);

            bool camPosIsOk = HandleIntersection(topControlledEntity, safeOBB, topControlledEntity is Sandbox.Game.Entities.Character.MyCharacter, true, m_target, m_targetOrientation.Forward);

            return camPosIsOk;
        }

        public void RecalibrateCameraPosition(bool isCharacter = false)
        {
           // if (m_lookAt != Vector3D.Zero)
            //    return;

            IMyCameraController cameraController = MySession.Static.CameraController;
            if (cameraController == null || !(cameraController is MyEntity))
                return;

            Sandbox.Game.Entities.IMyControllableEntity controlledEntity = MySession.ControlledEntity as Sandbox.Game.Entities.IMyControllableEntity;
            if (controlledEntity == null)
                return;

            // get latest head matrix
            if (!isCharacter)
            {
                var headMatrix = controlledEntity.GetHeadMatrix(true);
                m_targetOrientation = (Matrix)headMatrix.GetOrientation();
                m_target = headMatrix.Translation;
            }

            // parent of hierarchy
            MyEntity topControlledEntity = ((MyEntity)cameraController).GetTopMostParent();
            if (topControlledEntity.Closed) 
                return;

            // calculate controlled object coordinates in parent space
            var worldToLocal = topControlledEntity.PositionComp.WorldMatrixNormalizedInv;
            Vector3D targetInLocal = Vector3D.Transform(m_target, worldToLocal);
            MatrixD orientationInLocal = m_targetOrientation * worldToLocal;
            var localAABBHr = topControlledEntity.PositionComp.LocalAABBHr;
            Vector3D centerToTarget = targetInLocal - localAABBHr.Center;
            Vector3D backVec = Vector3D.Normalize(orientationInLocal.Backward);

            // calculate offset for the 
            double projectedCenterToTarget = Vector3D.Dot(centerToTarget, backVec);
            double projectedHalfExtent = Math.Abs(Vector3D.Dot(localAABBHr.HalfExtents, backVec));
            double finalLength = projectedHalfExtent - projectedCenterToTarget;

            Vector3D targetWithOffset = centerToTarget + (finalLength * backVec);

            double width = LOOK_AT_DEFAULT_LENGTH; // some default value
            if (Math.Abs(backVec.Z) > 0.0001)
                width = localAABBHr.HalfExtents.X * 1.5f;
            else if (Math.Abs(backVec.X) > 0.0001)
                width = localAABBHr.HalfExtents.Z * 1.5f;

            // calculate complete offset for controlled object
            double halfFovTan = Math.Tan(MySector.MainCamera.FieldOfView * 0.5);
            double offset = width / (2 * halfFovTan);
            offset += finalLength;

            double clampDist = MathHelper.Clamp(offset, GetMinDistance(), MAX_DISTANCE);

            Vector3D lookAt = LOOK_AT_DIRECTION * clampDist;

            SetPositionAndLookAt(lookAt);
        }

        private void SetPositionAndLookAt(Vector3D lookAt)
        {
            m_lookAt = lookAt;

            m_transformedLookAt = Vector3D.Transform(lookAt, m_targetOrientation);
            m_positionSafe = m_target + m_transformedLookAt;
            m_desiredPosition = m_positionSafe;
            m_position = m_positionSafe;
            m_velocity = Vector3.Zero;

            m_positionSafe.AssertIsValid();
        }

        public void Rotate(Vector2 rotationIndicator, float rollIndicator)
        {
            MoveAndRotate(Vector3.Zero, rotationIndicator, rollIndicator);
        }

        public void MoveAndRotate(Vector3 moveIndicator, Vector2 rotationIndicator, float rollIndicator)
        {
            UpdateZoom();

            SetState(moveIndicator, rotationIndicator, rollIndicator);
            //   MyThirdPersonSpectator.Static.QuickZoom = input.IsAnyAltKeyPressed();
        }

        public void UpdateZoom()
        {
            bool canZoom = !MyPerGameSettings.ZoomRequiresLookAroundPressed || MyInput.Static.IsGameControlPressed(Sandbox.Game.MyControlsSpace.LOOKAROUND);

            if (canZoom && !MyInput.Static.IsAnyCtrlKeyPressed() && !MyInput.Static.IsAnyShiftKeyPressed())
            {
                double newDistance = 0;

                var velocity = Vector3.Zero;
                if (MySession.ControlledEntity != null && MySession.ControlledEntity.Entity.Physics != null)
                    velocity = MySession.ControlledEntity.Entity.Physics.LinearVelocity;

                var positionSafe = m_positionSafe + velocity * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

                if (MyInput.Static.PreviousMouseScrollWheelValue() < MyInput.Static.MouseScrollWheelValue())
                {
                    var currentDistance = (m_target - positionSafe).Length();
                    newDistance = currentDistance / 1.2f;
                }
                else if (MyInput.Static.PreviousMouseScrollWheelValue() > MyInput.Static.MouseScrollWheelValue())
                {
                    var currentDistance = (m_target - positionSafe).Length();
                    newDistance = currentDistance * 1.2f;
                }

                if (newDistance > 0)
                {
                    var distance = m_lookAt.Length();

                    // Limit distance 
                    newDistance = MathHelper.Clamp(newDistance, MyThirdPersonSpectator.Static.GetMinDistance(), MyThirdPersonSpectator.MAX_DISTANCE);
                    m_lookAt *= newDistance / distance;
                    SaveSettings();
                    //m_desiredPosition = positionSafe;
                    //m_position = positionSafe;
                    //m_velocity = Vector3.Zero;
                }
            }
        }

        // Sets inner state (like target spring properties) which depends on ship movement type i.e. strafe
        private void SetState(Vector3 moveIndicator, Vector2 rotationIndicator, float rollIndicator)
        {
            if (rollIndicator < float.Epsilon &&
                (Math.Abs(moveIndicator.X) > float.Epsilon || Math.Abs(moveIndicator.Y) > float.Epsilon) &&
                Math.Abs(moveIndicator.Z) < float.Epsilon)
            {
                if (m_targetSpring != StrafingSpring)
                {
                    m_springChangeTime = 0;
                    m_targetSpring = StrafingSpring;
                }
            }
            else if (m_targetSpring != NormalSpring)
            {
                m_springChangeTime = 0;
                m_targetSpring = NormalSpring;
            }
        }

        // Returns 3D crosshair position
        public Vector3D GetCrosshair()
        {
            return m_target + m_targetOrientation.Forward * 25000;
        }

        public float GetMinDistance()
        {
            // return MySession.PlayerShip != null ? MySession.PlayerShip.WorldVolume.Radius * 1.3f : MyThirdPersonSpectator.DEFAULT_MIN_DISTANCE;
            return 1;
        }

        public void ResetDistance(double? newDistance = null)
        {
            if (newDistance.HasValue)
            {
                newDistance = MathHelper.Clamp(newDistance.Value, MyThirdPersonSpectator.Static.GetMinDistance(), MyThirdPersonSpectator.MAX_DISTANCE);

                Vector3D lookAt = LOOK_AT_DIRECTION * newDistance.Value;

                SetPositionAndLookAt(lookAt);
            }
        }

        public void ResetPosition(double distance, Vector2? headAngle)
        {
            if (headAngle.HasValue)
            {
                Sandbox.Game.Entities.IMyControllableEntity controlledEntity = MySession.ControlledEntity as Sandbox.Game.Entities.IMyControllableEntity;
                if (controlledEntity == null)
                    return;

                controlledEntity.HeadLocalXAngle = headAngle.Value.X;
                controlledEntity.HeadLocalYAngle = headAngle.Value.Y;

                ResetDistance(distance);

                m_positionSafe.AssertIsValid();
            }
        }

        public double GetDistance()
        {
            return m_lookAt.Length();
        }

        public void SaveSettings()
        {
            m_saveSettings = true;
        }
    }
}
