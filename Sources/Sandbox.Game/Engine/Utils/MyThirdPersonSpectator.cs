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
using System.Diagnostics;
using Sandbox.Game.Multiplayer;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Interfaces;
using VRage.Input;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Engine.Utils
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class MyThirdPersonSpectator : MySessionComponentBase
    {
        // Global instance of third person spectator.
        public static MyThirdPersonSpectator Static;

        // Minimum distance camera-ship (also used for quick zoom)
        public const float MIN_VIEWER_DISTANCE = 1.0f;
        // Maximum distance camera-ship (also used for quick zoom)
        public const float MAX_VIEWER_DISTANCE = 200.0f;
        // "Size" of the camera.
        public const float CAMERA_RADIUS = 0.2f;

        // Direction in which we are looking. View space.
        public readonly Vector3D LOOK_AT_DIRECTION = Vector3D.Normalize(new Vector3D(0, 5, 12));
        // Vertical camera offset (both target and viewer).
        public const float LOOK_AT_OFFSET_Y = 0.0f;
        // Default camera distance.
        public const double LOOK_AT_DEFAULT_LENGTH = 50;

        private const int SHAPE_CAST_MAX_STEP_COUNT = 10;
        private const float SHAPE_CAST_STEP = MAX_VIEWER_DISTANCE / SHAPE_CAST_MAX_STEP_COUNT;
        public const float BACKWARD_CUTOFF = 3.0f;

        // Intensity of head shaking.
        public const float HEADSHAKE_POWER = 0.5f;

        enum MyCameraRaycastResult
        {
            Ok,  // no occluder
            FoundOccluder,  // found occluder, there is space for camera in front of it
            FoundOccluderNoSpace // found occluder, there is no space for camera in front of it, switch to first person
        }
        
        #region Definitions
        // Helper class for spring physics parameters
        // Critical damping = 2*sqrt(Stiffness * Mass)
        public class SpringInfo
        {
            // Spring physics properties
            public float Stiffness;
            public float Dampening;
            public float Mass;

            public SpringInfo(float stiffness, float dampening, float mass)
            {
                Stiffness = stiffness;
                Dampening = dampening;
                Mass = mass;
            }

            public SpringInfo(SpringInfo spring)
            {
                Setup(spring);
            }

            public void Setup(SpringInfo spring)
            {
                Stiffness = spring.Stiffness;
                Dampening = spring.Dampening;
                Mass = spring.Mass;
            }

            public void Setup(SpringInfo a, SpringInfo b, float springChangeTime)
            {
                var amount = MathHelper.Saturate(springChangeTime);
                Stiffness = MathHelper.SmoothStep(a.Stiffness, b.Stiffness, amount);
                Dampening = MathHelper.SmoothStep(a.Dampening, b.Dampening, amount);
                Mass = MathHelper.SmoothStep(a.Mass, b.Mass, amount);
            }
        }
        #endregion

        // Vector defining position between Target and Spectator
        Vector3D m_lookAt;
        Vector3D m_clampedlookAt;
        Vector3D m_transformedLookAt;

        Vector3D m_target;
        Matrix m_targetOrientation = Matrix.Identity;
        Vector3D m_targetUpVec;

        // Current spectator position interpolated to desired position
        Vector3D m_position;
        // Desired position (before applying spring).
        Vector3D m_desiredPosition;
        // Current spectator position, interpolated and collision free
        Vector3D m_positionSafe;
        // Zooming out when there is no obstacle => speed
        private float m_positionSafeZoomingOutSpeed = 0;
        // Zooming out when there is no obstacle => last distance between m_position and m_safeposition
        private float m_lastRaycastDist = float.PositiveInfinity;
        // Is m_position safe? collision free?
        bool m_positionCurrentIsSafe;

        // Spring physics properties
        public SpringInfo NormalSpring;
        //public SpringInfo StrafingSpring;
        //public SpringInfo AngleSpring;

        // Desired spring parameters
        SpringInfo m_targetSpring;

        float m_springChangeTime;
        // Current spring parameters, we interpolate values in case StrafingSpring->NormalSpring
        SpringInfo m_currentSpring;

        Vector3 m_velocity;
        float m_angleVelocity;
        Quaternion m_orientation;
        Matrix m_orientationMatrix;

        readonly List<MyPhysics.HitInfo> m_raycastList = new List<MyPhysics.HitInfo>(64);

        bool m_saveSettings;
        bool m_debugDraw = false;

        /// <summary>
        /// Optimization.
        /// Used to step long shape casts so their AABBs are not huge,
        /// causing asteroids to generate too many physics shapes.
        /// </summary>
        uint m_updateCount;

        readonly float[] m_lastShapeCastDistance = new float[SHAPE_CAST_MAX_STEP_COUNT];

        private bool? m_localCharacterWasInThirdPerson;
        private double m_safeMinimumDistance = MIN_VIEWER_DISTANCE;

        public bool? LocalCharacterWasInThirdPerson
        {
            get { return m_localCharacterWasInThirdPerson; }
            set { m_localCharacterWasInThirdPerson = value; }
        }

        public MyThirdPersonSpectator()
        {
            Static = this;
            NormalSpring = new SpringInfo(20000, 1114, 50);
            //StrafingSpring = new SpringInfo(36000, 2683, 50);
            //AngleSpring = new SpringInfo(30, 14.5f, 2);

            m_targetSpring = NormalSpring;
            m_currentSpring = new SpringInfo(NormalSpring);

            m_lookAt = LOOK_AT_DIRECTION * LOOK_AT_DEFAULT_LENGTH;
            m_clampedlookAt = m_lookAt;

            m_saveSettings = false;

            ResetViewerDistance();

            for (int i = 0; i < m_lastShapeCastDistance.Length; ++i)
            {
                m_lastShapeCastDistance[i] = float.PositiveInfinity;
            }
        }

        // Updates spectator position (spring connected to desired position)
        public override void UpdateAfterSimulation()
        {
            Sandbox.Game.Entities.IMyControllableEntity genericControlledEntity = MySession.Static.ControlledEntity as Sandbox.Game.Entities.IMyControllableEntity;
            if (genericControlledEntity == null)
                return;
            var remotelyControlledEntity = genericControlledEntity as MyRemoteControl;
            var controlledEntity = remotelyControlledEntity != null ? remotelyControlledEntity.Pilot : genericControlledEntity.Entity;
            while (controlledEntity.Parent is MyCockpit)
                controlledEntity = controlledEntity.Parent;
            if (controlledEntity != null && controlledEntity.PositionComp != null)
            {
                var positionComp = controlledEntity.PositionComp;
                float localY = positionComp.LocalAABB.Max.Y - positionComp.LocalAABB.Min.Y;
                Vector3D lastTarget = m_target;
                var headMatrix = remotelyControlledEntity == null ? genericControlledEntity.GetHeadMatrix(true) : remotelyControlledEntity.Pilot.GetHeadMatrix(true);
                m_target = controlledEntity is MyCharacter ? ((positionComp.GetPosition() + (localY + LOOK_AT_OFFSET_Y) * positionComp.WorldMatrix.Up)) : headMatrix.Translation;
                m_targetOrientation = headMatrix.GetOrientation();
                m_targetUpVec = m_positionCurrentIsSafe ? (Vector3D)m_targetOrientation.Up : positionComp.WorldMatrix.Up;
                m_transformedLookAt = Vector3D.Transform(m_clampedlookAt, m_targetOrientation);
                m_desiredPosition = m_target + m_transformedLookAt;

                m_position += m_target - lastTarget; // compensate character movement

                //m_position = m_desiredPosition;
            }
            else
            {
                var headMatrix = genericControlledEntity.GetHeadMatrix(true);
                m_target = headMatrix.Translation;
                m_targetOrientation = headMatrix.GetOrientation();
                m_targetUpVec = m_targetOrientation.Up;

                m_transformedLookAt = Vector3D.Transform(m_clampedlookAt, m_targetOrientation);
                m_desiredPosition = m_target + m_transformedLookAt;
                m_position = m_desiredPosition;
            }

            Vector3D stretch = m_position - m_desiredPosition;

            Vector3D force = -m_currentSpring.Stiffness * stretch - m_currentSpring.Dampening * m_velocity;
            force.AssertIsValid();

            // Apply acceleration
            Vector3 acceleration = (Vector3)force / m_currentSpring.Mass;
            m_velocity += acceleration * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            m_velocity.AssertIsValid();

            // Apply velocity
            m_position += m_velocity * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            m_position.AssertIsValid();

            // Limit backward distance from target
            double backward = Vector3D.Dot((Vector3D)m_targetOrientation.Backward, (m_target - m_position));
            if (backward > -BACKWARD_CUTOFF)
            {
                m_position += (Vector3D)m_targetOrientation.Backward * (backward + BACKWARD_CUTOFF);
            }

            // -------- raycast, prevent camera being inside things ------
            if (controlledEntity != null)
            {
                if (!controlledEntity.Closed)
                {
                    HandleIntersection(controlledEntity);
                }
                else
                {
                    m_positionCurrentIsSafe = false;
                }
            }

            // ------- save current settings -------
            if (m_saveSettings)
            {
                MySession.Static.SaveControlledEntityCameraSettings(false);
                m_saveSettings = false;
            }
            ++m_updateCount;
        }

        // ------------------------- basic methods of camera --------------------------------------

        public void Rotate(Vector2 rotationIndicator, float rollIndicator)
        {
            MoveAndRotate(Vector3.Zero, rotationIndicator, rollIndicator);
        }

        public void MoveAndRotate(Vector3 moveIndicator, Vector2 rotationIndicator, float rollIndicator)
        {
            UpdateZoom();
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

        public MatrixD GetViewMatrix()
        {
            IMyCameraController cameraController = MySession.Static.CameraController;
            if (cameraController == null)
                return MatrixD.Identity;

            //return GetViewMatrix(
            //    MySector.MainCamera.FieldOfView,
            //    1,
            //    false,
            //    m_target,
            //    m_targetOrientation.Forward);

            //VRageRender.MyRenderProxy.DebugDrawArrow3D(m_target, m_target + m_targetOrientation.Up, Color.Aqua, Color.Red, false);
            //VRageRender.MyRenderProxy.DebugDrawArrow3D(m_positionSafe, m_target + m_targetOrientation.Up, Color.Green, Color.Red, false);
            //VRageRender.MyRenderProxy.DebugDrawArrow3D(m_positionSafe, m_target, Color.Yellow, Color.Red, false);

            Vector3D shipVerticalOffset = Vector3D.Zero;
            var cameraControllerEntity = cameraController as MyEntity;
            if (cameraControllerEntity != null)
            {
                MyEntity topControlledEntity = cameraControllerEntity.GetTopMostParent();
                if (topControlledEntity != null && !topControlledEntity.Closed && cameraControllerEntity is MyShipController)
                {
                    float fov = MySector.MainCamera.FieldOfView;
                    double shipVerticalShift = (float) Math.Tan(fov / 2) * 0.1f * m_lookAt.Length();
                    shipVerticalOffset = m_targetUpVec * shipVerticalShift;
                }
            }

            //VRageRender.MyRenderProxy.DebugDrawArrow3D(m_positionSafe - shipVerticalOffset, m_target - shipVerticalOffset, Color.Green, Color.Green, false);
            return MatrixD.CreateLookAt(m_positionSafe, m_target, m_targetUpVec);
        }

        // ------------------ raycasting, checking, recalibration ------------------------------------

        // Is current camera ok or are we inside any obstacle?
        // there was same method IsCameraPositionOk(Matrix worldMatrix) with custom world matrix - removed, it was not really needed. 
        // see bottom, maybe it is still there, commented out.
        public bool IsCameraPositionOk()
        {
            return m_positionCurrentIsSafe;
        }

        /// <summary>
        /// Processes previously acquired raycast results.
        /// Returns safe camera eye position (As far from target as possible, but not colliding, best case = desired eye pos).
        /// </summary>
        private MyCameraRaycastResult RaycastOccludingObjects(MyEntity controlledEntity, ref Vector3D raycastOrigin, ref Vector3D raycastEnd, ref Vector3D raycastSafeCameraStart, out Vector3D outSafePosition)
        {
            Vector3D rayDirection = raycastEnd - raycastOrigin;
            rayDirection.Normalize();

            outSafePosition = m_position;
            double closestDistanceSquared = double.PositiveInfinity;
            bool positionChanged = false;
            
            // ray cast - very close objects
            Physics.MyPhysics.CastRay(raycastOrigin, raycastOrigin + CAMERA_RADIUS * rayDirection, m_raycastList);
            foreach (MyPhysics.HitInfo rb in m_raycastList)
            {
                if (rb.HkHitInfo.Body == null
                    || rb.HkHitInfo.Body.UserObject == null
                    || !(rb.HkHitInfo.Body.UserObject is MyPhysicsBody)
                    || rb.HkHitInfo.GetHitEntity() == controlledEntity)
                    continue;
                if (rb.HkHitInfo.GetHitEntity() is IMyHandheldGunObject<Sandbox.Game.Weapons.MyDeviceBase>) // ignore player weapons
                    continue;

                double distSq = Vector3D.DistanceSquared(rb.Position, raycastOrigin);
                if (distSq < closestDistanceSquared)
                {
                    closestDistanceSquared = distSq;
                    float dist = (float)Math.Sqrt(distSq) - CAMERA_RADIUS;
                    outSafePosition = raycastOrigin + rayDirection * dist;
                    positionChanged = true;
                }
            }

            if (m_debugDraw)
                foreach (var raycastResult in m_raycastList)
                {
                    VRageRender.MyRenderProxy.DebugDrawPoint(raycastResult.Position, Color.Red, false);
                }

            // shape cast - further objects
            if ((raycastEnd - raycastOrigin).LengthSquared() > CAMERA_RADIUS * CAMERA_RADIUS)
            {
                HkShape shapeSphere = new HkSphereShape(CAMERA_RADIUS);
                MatrixD raycastOriginTransform = MatrixD.Identity;
                raycastOriginTransform.Translation = raycastOrigin + CAMERA_RADIUS * rayDirection;
                Physics.MyPhysics.CastShapeReturnContactBodyDatas(raycastEnd, shapeSphere, ref raycastOriginTransform, 0,
                    0, m_raycastList);
                foreach (MyPhysics.HitInfo rb in m_raycastList)
                {
                    IMyEntity hitEntity = rb.HkHitInfo.GetHitEntity();
                    if (rb.HkHitInfo.Body == null
                        || rb.HkHitInfo.Body.UserObject == null
                        || hitEntity == controlledEntity
                        || !(rb.HkHitInfo.Body.UserObject is MyPhysicsBody))
                        continue;
                    if (hitEntity is IMyHandheldGunObject<Sandbox.Game.Weapons.MyDeviceBase>)
                        // ignore player weapons
                        continue;

                    double distSq = Vector3D.DistanceSquared(rb.Position, raycastOrigin);
                    if (distSq < closestDistanceSquared)
                    {
                        closestDistanceSquared = distSq;
                        float dist = (float) Math.Sqrt(distSq);
                        outSafePosition = raycastOrigin + rayDirection * dist;
                        positionChanged = true;
                    }
                }
                shapeSphere.RemoveReference();
            }

            if (m_debugDraw)
                foreach (var raycastResult in m_raycastList)
                {
                    VRageRender.MyRenderProxy.DebugDrawSphere(raycastResult.Position, CAMERA_RADIUS, Color.Red, 1, false);
                }

            if (closestDistanceSquared < (raycastSafeCameraStart - raycastOrigin).LengthSquared() + CAMERA_RADIUS)
            {
                return MyCameraRaycastResult.FoundOccluderNoSpace; // obstacle too close, switch to first person
            }

            return positionChanged ? MyCameraRaycastResult.FoundOccluder : MyCameraRaycastResult.Ok;
        }

        /// <summary>
        /// Handles camera collisions with environment
        /// </summary>
        /// <returns>False if no correct position was found</returns>
        private bool HandleIntersection(MyEntity controlledEntity)
        {
            Debug.Assert(controlledEntity != null);
            var parentEntity = controlledEntity.GetTopMostParent() ?? controlledEntity;

            // line from target to eye
            var line = new LineD(m_target, m_position);
            // oriented bb of the entity
            var safeObb = GetEntitySafeOBB(parentEntity);
            // oriented bb of the entity + camera radius
            MyOrientedBoundingBoxD safeObbWithCollisionExtents;
            if (controlledEntity.Parent == null)
                safeObbWithCollisionExtents = new MyOrientedBoundingBoxD(safeObb.Center, safeObb.HalfExtent + 0.5 * CAMERA_RADIUS, safeObb.Orientation);
            else
                safeObbWithCollisionExtents = new MyOrientedBoundingBoxD(safeObb.Center, safeObb.HalfExtent + 2.0 * CAMERA_RADIUS, safeObb.Orientation);

            // start = target, end = eye
            // find safe start...
            LineD safeOBBLine = new LineD(line.From + line.Direction * 2 * safeObb.HalfExtent.Length(), line.From);
            double? safeIntersection = safeObbWithCollisionExtents.Intersects(ref safeOBBLine);
            Vector3D castStartSafe = safeIntersection != null ? (safeOBBLine.From + safeOBBLine.Direction * safeIntersection.Value) : m_target;

            if (controlledEntity.Parent != null && safeIntersection != null)
            {
                var hitInfo = Physics.MyPhysics.CastRay(castStartSafe, m_target);
                if (hitInfo.HasValue)
                {
                    castStartSafe = hitInfo.Value.Position + line.Direction;
                }
                else
                {
                    safeObb = GetEntitySafeOBB(controlledEntity);
                    safeObbWithCollisionExtents = new MyOrientedBoundingBoxD(safeObb.Center, safeObb.HalfExtent + 0.5f * CAMERA_RADIUS, safeObb.Orientation);
                    safeIntersection = safeObbWithCollisionExtents.Intersects(ref safeOBBLine);
                    castStartSafe = safeIntersection != null ? (safeOBBLine.From + safeOBBLine.Direction * safeIntersection.Value) : m_target;
                }
            }

            // visual debugging :)
            if (m_debugDraw)
            {
                VRageRender.MyRenderProxy.DebugDrawOBB(safeObb, Color.Red, 0.1f, false, true);
                VRageRender.MyRenderProxy.DebugDrawOBB(safeObbWithCollisionExtents, Color.Yellow, 0.0f, false, true);
                VRageRender.MyRenderProxy.DebugDrawArrow3D(safeOBBLine.From, safeOBBLine.To, Color.White, Color.Purple,
                    false);
                VRageRender.MyRenderProxy.DebugDrawArrow3D(safeOBBLine.From, castStartSafe, Color.White, Color.Red,
                    false);
                VRageRender.MyRenderProxy.DebugDrawArrow3D(castStartSafe, m_position, Color.White, Color.Orange, false);
            }

            // raycast against occluders
            Vector3D safePositionCandidate;
            m_safeMinimumDistance = controlledEntity is MyCharacter ? 0 : (castStartSafe - m_target).Length(); // store current safe minimum dist
            m_safeMinimumDistance = Math.Max(m_safeMinimumDistance, MIN_VIEWER_DISTANCE);
            Vector3D raycastOrigin = (controlledEntity is MyCharacter) ? m_target : castStartSafe;
            MyCameraRaycastResult raycastResult = RaycastOccludingObjects(controlledEntity, ref raycastOrigin, ref m_position,
                ref castStartSafe, out safePositionCandidate);

            switch (raycastResult)
            {
                case MyCameraRaycastResult.Ok:
                case MyCameraRaycastResult.FoundOccluder:
                    m_positionCurrentIsSafe = true;
                    {
                        double newDist = (safePositionCandidate - m_position).Length();
                        if (newDist < m_lastRaycastDist - CAMERA_RADIUS || newDist < m_safeMinimumDistance)
                        {
                            // new safe position is further from target => change over time
                            float distDiffZoomSpeed = 1 - MathHelper.Clamp((float)(m_lastRaycastDist - newDist), 0.0f, 1.0f - MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS);
                            m_positionSafeZoomingOutSpeed += distDiffZoomSpeed;
                            m_positionSafeZoomingOutSpeed = MathHelper.Clamp(m_positionSafeZoomingOutSpeed, 0.0f, 1.0f);

                            m_positionSafe = Vector3D.Lerp(m_positionSafe, safePositionCandidate, m_positionSafeZoomingOutSpeed);
                        }
                        else
                        {
                            // new safe position is closer or closer than safe distance => instant change
                            m_positionSafeZoomingOutSpeed = 0.0f;    // set zooming out speed to zero for next time
                            m_positionSafe = safePositionCandidate;
                        }
                    }
                    break;
                //case MyCameraRaycastResult.FoundOccluderNoSpace:
                default:
                    m_positionSafeZoomingOutSpeed = 1.0f; // we're in first person, change instantly to third if possible
                    m_positionCurrentIsSafe = false;
                    break;    
            }

            m_lastRaycastDist = (float)(m_positionSafe - m_position).Length();

            if (m_debugDraw)
            {
                VRageRender.MyRenderProxy.DebugDrawSphere(m_positionSafe, 0.2f, Color.Purple, 1, false);
                VRageRender.MyRenderProxy.DebugDrawSphere(safePositionCandidate, 0.2f, Color.Azure, 1, false);
            }
            return m_positionCurrentIsSafe;
        }

        private static MyOrientedBoundingBoxD GetEntitySafeOBB(MyEntity controlledEntity)
        {
            var localAABB = controlledEntity.PositionComp.LocalAABB;
            Vector3D center = Vector3D.Transform((Vector3D) localAABB.Center, controlledEntity.WorldMatrix);
            var safeOBB = new MyOrientedBoundingBoxD(center, localAABB.HalfExtents, Quaternion.CreateFromRotationMatrix(controlledEntity.WorldMatrix.GetOrientation()));
            return safeOBB;
        }

        public void RecalibrateCameraPosition(bool isCharacter = false)
        {
            // if (m_lookAt != Vector3D.Zero)
            //    return;

            IMyCameraController cameraController = MySession.Static.CameraController;
            if (cameraController == null || !(cameraController is MyEntity))
                return;

            Sandbox.Game.Entities.IMyControllableEntity controlledEntity = MySession.Static.ControlledEntity as Sandbox.Game.Entities.IMyControllableEntity;
            if (controlledEntity == null)
                return;

            // get latest head matrix
            if (!isCharacter)
            {
                var headMatrix = controlledEntity.GetHeadMatrix(true);
                m_targetOrientation = (Matrix) headMatrix.GetOrientation();
                m_target = headMatrix.Translation;
            }

            // parent of hierarchy
            MyEntity topControlledEntity = ((MyEntity) cameraController).GetTopMostParent();
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

            double clampDist = MathHelper.Clamp(offset, MIN_VIEWER_DISTANCE, MAX_VIEWER_DISTANCE);

            Vector3D lookAt = LOOK_AT_DIRECTION * clampDist;

            SetPositionAndLookAt(lookAt);
        }

        // ---------- zoom --------------------------

        public void UpdateZoom()
        {
            bool canZoom = (!MyPerGameSettings.ZoomRequiresLookAroundPressed || MyInput.Static.IsGameControlPressed(Sandbox.Game.MyControlsSpace.LOOKAROUND)) && !MySession.Static.Battle;

            if (canZoom && !MyInput.Static.IsAnyCtrlKeyPressed() && !MyInput.Static.IsAnyShiftKeyPressed())
            {
                double newDistance = 0;

                var velocity = Vector3.Zero;
                if (MySession.Static.ControlledEntity != null && MySession.Static.ControlledEntity.Entity.Physics != null)
                    velocity = MySession.Static.ControlledEntity.Entity.Physics.LinearVelocity;

                Vector3D positionSafe = m_positionSafe + (Vector3D) velocity * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

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
                    newDistance = MathHelper.Clamp(newDistance, MIN_VIEWER_DISTANCE, MAX_VIEWER_DISTANCE);
                    m_lookAt *= newDistance / distance;
                    SaveSettings();
                    //m_desiredPosition = positionSafe;
                    //m_position = positionSafe;
                    //m_velocity = Vector3.Zero;
                }
                else
                {
                    var distance = m_lookAt.Length();
                    // Limit distance 
                    double clampedDistance = MathHelper.Clamp(distance, MIN_VIEWER_DISTANCE, MAX_VIEWER_DISTANCE);
                    m_lookAt *= clampedDistance / distance;
                    SaveSettings();
                }

                m_clampedlookAt = m_lookAt;
                double oldLenToClamp = m_clampedlookAt.Length();
                m_clampedlookAt = m_clampedlookAt * MathHelper.Clamp(oldLenToClamp, m_safeMinimumDistance, MAX_VIEWER_DISTANCE) / oldLenToClamp;
            }
        }

        // ------------- viewer -------------------------------

        /// <summary>
        /// Reset the third person camera distance.
        /// </summary>
        /// <param name="newDistance">New camera distance. If null, it is not changed.</param>
        public bool ResetViewerDistance(double? newDistance = null)
        {
            if (!newDistance.HasValue)
                return false;
            newDistance = MathHelper.Clamp(newDistance.Value, MIN_VIEWER_DISTANCE, MAX_VIEWER_DISTANCE);
            Vector3D lookAt = LOOK_AT_DIRECTION * newDistance.Value;
            SetPositionAndLookAt(lookAt);
            return true;
        }

        /// <summary>
        /// Reset the third person camera "head" angles.
        /// </summary>
        /// <param name="headAngle">new head angle</param>
        public bool ResetViewerAngle(Vector2? headAngle)
        {
            if (!headAngle.HasValue)
                return false;

            Sandbox.Game.Entities.IMyControllableEntity controlledEntity = MySession.Static.ControlledEntity as Sandbox.Game.Entities.IMyControllableEntity;
            if (controlledEntity == null)
                return false;

            controlledEntity.HeadLocalXAngle = headAngle.Value.X;
            controlledEntity.HeadLocalYAngle = headAngle.Value.Y;
            return true;
        }

        /// <summary>
        /// Get the distance from viewer to the target.
        /// </summary>
        /// <returns></returns>
        public double GetViewerDistance()
        {
            return m_clampedlookAt.Length();
        }

        // --------- utility ----------------------

        /// <summary>
        /// Flag this spectator to save its settings next Update call.
        /// </summary>
        public void SaveSettings()
        {
            m_saveSettings = true;
        }

        // Returns 3D crosshair position
        public Vector3D GetCrosshair()
        {
            return m_target + m_targetOrientation.Forward * 25000;
        }

        // Handles interpolation of spring parameters
        //private void UpdateCurrentSpring()
        //{
        //    if (m_targetSpring == StrafingSpring)
        //    {
        //        m_currentSpring.Setup(m_targetSpring);
        //    }
        //    else if (m_targetSpring == NormalSpring)
        //    {
        //        m_currentSpring.Setup(StrafingSpring, NormalSpring, m_springChangeTime);
        //    }
        //    m_springChangeTime += VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
        //}

        // Sets inner state (like target spring properties) which depends on ship movement type i.e. strafe
        //private void SetState(Vector3 moveIndicator, Vector2 rotationIndicator, float rollIndicator)
        //{
        //    if (rollIndicator < float.Epsilon &&
        //        (Math.Abs(moveIndicator.X) > float.Epsilon || Math.Abs(moveIndicator.Y) > float.Epsilon) &&
        //        Math.Abs(moveIndicator.Z) < float.Epsilon)
        //    {
        //        //if (m_targetSpring != StrafingSpring)
        //        //{
        //        //    m_springChangeTime = 0;
        //        //    m_targetSpring = StrafingSpring;
        //        //}
        //    }
        //    else if (m_targetSpring != NormalSpring)
        //    {
        //        m_springChangeTime = 0;
        //        m_targetSpring = NormalSpring;
        //    }
        //}

        //public bool IsCameraPositionOk(Matrix worldMatrix)
        //{
        //    IMyCameraController cameraController = MySession.Static.CameraController;
        //    if (!(cameraController is MyEntity))
        //        return true;

        //    MyEntity topControlledEntity = ((MyEntity)cameraController).GetTopMostParent();
        //    if (topControlledEntity.Closed) return false;

        //    var localAABBHr = topControlledEntity.PositionComp.LocalAABBHr;
        //    Vector3D center = Vector3D.Transform((Vector3D)localAABBHr.Center, worldMatrix);

        //    var safeOBB = new MyOrientedBoundingBoxD(center, localAABBHr.HalfExtents, Quaternion.CreateFromRotationMatrix(worldMatrix.GetOrientation()));
        //    //VRageRender.MyRenderProxy.DebugDrawOBB(safeOBB, Vector3.One, 1, false, false);
        //    //VRageRender.MyRenderProxy.DebugDrawAxis(topControlledEntity.WorldMatrix, 2, false);

        //    bool camPosIsOk = HandleIntersection(topControlledEntity, safeOBB, topControlledEntity is MyCharacter, true, m_target, m_targetOrientation.Forward);
        //    return camPosIsOk;
        //}
    }
}
