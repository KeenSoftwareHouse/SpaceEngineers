using Havok;
using Sandbox.Engine.Physics;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Interfaces;
using VRage.Input;
using VRage.ModAPI;
using VRageMath;
using IMyControllableEntity = Sandbox.Game.Entities.IMyControllableEntity;

namespace Sandbox.Engine.Utils
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class MyThirdPersonSpectator : MySessionComponentBase
    {
        // Global instance of third person spectator.
        public static MyThirdPersonSpectator Static;

        // Minimum distance camera-ship (also used for quick zoom)
        public const float MIN_VIEWER_DISTANCE = 1.45f;
        // Maximum distance camera-ship (also used for quick zoom)
        public const float MAX_VIEWER_DISTANCE = 200.0f;
        // "Size" of the camera.
        public const float CAMERA_RADIUS = 0.3f;

        // Direction in which we are looking. View space.
        private readonly Vector3D m_lookAtDirection = Vector3D.Normalize(new Vector3D(0, 5, 12));
        private readonly Vector3D m_lookAtDirectionCharacter = Vector3D.Normalize(new Vector3D(0, 0, 12));
        // Vertical camera offset (both target and viewer).
        private const float m_lookAtOffsetY = 0.0f;
        // Default camera distance.
        private const double m_lookAtDefaultLength = 2.6f;
        // Default timeout in ms before zooming out.
        private int m_positionSafeZoomingOutDefaultTimeoutMs = 500;

        public bool m_disableSpringThisFrame = false;

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
        // Zooming out when there is no obstacle => timeout remaining
        private int m_positionSafeZoomingOutTimeout = 0;
        // Zooming out when there is no obstacle => last distance between m_position and m_safeposition
        private float m_lastRaycastDist = float.PositiveInfinity;
        // Is m_position safe? collision free?
        bool m_positionCurrentIsSafe;

        // Spring physics properties
        public SpringInfo NormalSpring;
        //public SpringInfo StrafingSpring;
        //public SpringInfo AngleSpring;

        // Desired spring parameters
        //SpringInfo m_targetSpring;

        float m_springChangeTime;
        // Current spring parameters, we interpolate values in case StrafingSpring->NormalSpring
        readonly SpringInfo m_currentSpring;

        Vector3 m_velocity;
        float m_angleVelocity;
        Quaternion m_orientation;
        Matrix m_orientationMatrix;

        private readonly List<MyPhysics.HitInfo> m_raycastList = new List<MyPhysics.HitInfo>(64);

        private bool m_saveSettings;
        private bool m_debugDraw = false;

        private double m_safeMinimumDistance = MIN_VIEWER_DISTANCE;
        private IMyControllableEntity m_lastControllerEntity;
        //private readonly List<HkBodyCollision> m_hkBodyCollisions = new List<HkBodyCollision>(32);

        private List<Vector3D> m_debugLastSpectatorPositions = null;
        private List<Vector3D> m_debugLastSpectatorDesiredPositions = null;

        public bool EnableDebugDraw
        {
            get { return m_debugDraw; }
            set { m_debugDraw = value; }
        }

        public MyThirdPersonSpectator()
        {
            Static = this;
            NormalSpring = new SpringInfo(20000, 1114, 50);
            //StrafingSpring = new SpringInfo(36000, 2683, 50);
            //AngleSpring = new SpringInfo(30, 14.5f, 2);

            //m_targetSpring = NormalSpring;
            m_currentSpring = new SpringInfo(NormalSpring);

            m_lookAt = m_lookAtDirectionCharacter * m_lookAtDefaultLength;
            m_clampedlookAt = m_lookAt;

            m_saveSettings = false;

            ResetViewerDistance();
        }

        // Updates spectator position (spring connected to desired position)
        public void Update()
        {
            IMyControllableEntity genericControlledEntity = MySession.Static.ControlledEntity;
            if (genericControlledEntity == null)
                return;

            if (genericControlledEntity != m_lastControllerEntity)
            {
                m_disableSpringThisFrame = true;
                m_lastControllerEntity = genericControlledEntity;
            }

            // ----------- gather viewer position and target ------------------
            var remotelyControlledEntity = genericControlledEntity as MyRemoteControl;
            var controlledEntity = remotelyControlledEntity != null ? remotelyControlledEntity.Pilot : genericControlledEntity.Entity;
            while (controlledEntity.Parent is MyCockpit)
            {
                controlledEntity = controlledEntity.Parent;
            }
            if (controlledEntity != null && controlledEntity.PositionComp != null)
            {
                var positionComp = controlledEntity.PositionComp;
                float localY = positionComp.LocalAABB.Max.Y - positionComp.LocalAABB.Min.Y;
                Vector3D lastTarget = m_target;
                var headMatrix = remotelyControlledEntity == null ? genericControlledEntity.GetHeadMatrix(true) : remotelyControlledEntity.Pilot.GetHeadMatrix(true);
                m_target = controlledEntity is MyCharacter ? ((positionComp.GetPosition() + (localY + m_lookAtOffsetY) * 1.2f * positionComp.WorldMatrix.Up)) : headMatrix.Translation;
                m_targetOrientation = headMatrix.GetOrientation();
                m_targetUpVec = m_positionCurrentIsSafe ? (Vector3D)m_targetOrientation.Up : positionComp.WorldMatrix.Up; // this is right and prevents z-rotation if in invalid position (during timeout before switching to 1st person)
                m_transformedLookAt = Vector3D.Transform(m_clampedlookAt, m_targetOrientation);
                m_desiredPosition = m_target + m_transformedLookAt;

                m_position += m_target - lastTarget; // compensate character movement
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

            // --------- camera spring ------------------------
            Vector3D backup_mPosition = m_position;
            if (m_disableSpringThisFrame)
            {
                m_position = m_desiredPosition;
                m_velocity = Vector3.Zero;
            }
            else
            {
                ProcessSpringCalculation();
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

            // ------- in case spring is disabled - quickly lerp in new desired position ----------------
            if (m_disableSpringThisFrame)
            {
                double smoothCoeff = 0.8;
                m_position = Vector3D.Lerp(backup_mPosition, m_desiredPosition, smoothCoeff);
                m_velocity = Vector3.Zero;
                m_disableSpringThisFrame = Vector3D.DistanceSquared(m_position, m_desiredPosition) >
                                               CAMERA_RADIUS * CAMERA_RADIUS;
            }

            // ------- draw trail if debug draw is enabled --------------------------------------
            DebugDrawTrail();
        }

        private void ProcessSpringCalculation()
        {
            Vector3D stretch = m_position - m_desiredPosition;
            Vector3D force = -m_currentSpring.Stiffness * stretch - m_currentSpring.Dampening * m_velocity;
            force.AssertIsValid();
            // Apply acceleration
            Vector3 acceleration = (Vector3) force / m_currentSpring.Mass;
            m_velocity += acceleration * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            m_velocity.AssertIsValid();
            // Apply velocity
            m_position += m_velocity * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            m_position.AssertIsValid();
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
            double dist = lookAt.Length();
            m_lookAt = (MySession.Static == null || !(MySession.Static.CameraController is MyCharacter) ? m_lookAtDirection : m_lookAtDirectionCharacter) * dist;

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
            MyPhysics.CastRay(raycastOrigin, raycastOrigin + CAMERA_RADIUS * rayDirection, m_raycastList);
            foreach (MyPhysics.HitInfo rb in m_raycastList)
            {
                if (rb.HkHitInfo.Body == null
                    || rb.HkHitInfo.Body.UserObject == null
                    || !(rb.HkHitInfo.Body.UserObject is MyPhysicsBody)
                    || rb.HkHitInfo.GetHitEntity() == controlledEntity)
                    continue;
                if (rb.HkHitInfo.GetHitEntity() is IMyHandheldGunObject<Game.Weapons.MyDeviceBase>) // ignore player weapons
                    continue;

                double distSq = Vector3D.DistanceSquared(rb.Position, raycastOrigin);
                if (distSq < closestDistanceSquared)
                {
                    closestDistanceSquared = distSq;
                    double dist = Math.Sqrt(distSq) - CAMERA_RADIUS;
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
                MyPhysics.CastShapeReturnContactBodyDatas(raycastEnd, shapeSphere, ref raycastOriginTransform, 0,
                    0, m_raycastList);
                //MyPhysics.CastRay(raycastOrigin + CAMERA_RADIUS * rayDirection, raycastEnd, m_raycastList, 0);
                float closestFraction = 1;
                foreach (MyPhysics.HitInfo rb in m_raycastList)
                {
                    IMyEntity hitEntity = rb.HkHitInfo.GetHitEntity();
                    if (rb.HkHitInfo.Body == null
                        || rb.HkHitInfo.Body.UserObject == null
                        || hitEntity == controlledEntity
                        || !(rb.HkHitInfo.Body.UserObject is MyPhysicsBody))
                        continue;
                    if (hitEntity is IMyHandheldGunObject<Game.Weapons.MyDeviceBase>)
                        // ignore player weapons
                        continue;

                    Vector3D safePos = Vector3D.Lerp(raycastOrigin, raycastEnd - CAMERA_RADIUS * rayDirection,
                        Math.Max(rb.HkHitInfo.HitFraction, 0.0001));
                    double distSq = Vector3D.DistanceSquared(raycastOrigin, outSafePosition);
                    if (rb.HkHitInfo.HitFraction < closestFraction && distSq < closestDistanceSquared)
                    {
                        outSafePosition = safePos;
                        closestDistanceSquared = distSq;
                        positionChanged = true;
                        closestFraction = rb.HkHitInfo.HitFraction;
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

            // last check - isn't there voxel between safe pos and entity pos
            if (!positionChanged)
            {
                //Vector3D entityPos = controlledEntity.PositionComp.GetPosition();
                MyPhysics.CastRay(m_target, raycastSafeCameraStart, m_raycastList);

                if (m_debugDraw)
                {
                    BoundingBoxD bb = new BoundingBoxD(m_target, raycastSafeCameraStart);
                    VRageRender.MyRenderProxy.DebugDrawAABB(bb, Color.Magenta);
                }

                foreach (var collision in m_raycastList)
                {
                    if (collision.HkHitInfo.GetHitEntity() is MyVoxelBase)
                    {
                        return MyCameraRaycastResult.FoundOccluderNoSpace;
                    }
                }
            }

            return positionChanged ? MyCameraRaycastResult.FoundOccluder : MyCameraRaycastResult.Ok;
        }

        /// <summary>
        /// Handles camera collisions with environment
        /// </summary>
        /// <returns>False if no correct position was found</returns>
        private void HandleIntersection(MyEntity controlledEntity)
        {
            Debug.Assert(controlledEntity != null);
            MyEntity parentEntity = controlledEntity.GetTopMostParent() ?? controlledEntity;
            var parentEntityAsCubeGrid = parentEntity as MyCubeGrid;
            if (parentEntityAsCubeGrid != null && parentEntityAsCubeGrid.IsStatic)
                parentEntity = controlledEntity;  // cancel previous assignment, topmost parent is a station, we need smaller bounding box

            // line from target to eye
            LineD line = new LineD(m_target, m_position);
            // oriented bb of the entity
            MyOrientedBoundingBoxD safeObb = GetEntitySafeOBB(parentEntity);
            // oriented bb of the entity + camera radius
            MyOrientedBoundingBoxD safeObbWithCollisionExtents = 
                new MyOrientedBoundingBoxD(safeObb.Center, safeObb.HalfExtent + (controlledEntity.Parent == null ? 0.5 : 2.0) * CAMERA_RADIUS, safeObb.Orientation);

            // start = target, end = eye
            // find safe start...
            LineD safeOBBLine = new LineD(line.From + line.Direction * 2 * safeObb.HalfExtent.Length(), line.From);
            double? safeIntersection = safeObbWithCollisionExtents.Intersects(ref safeOBBLine);
            Vector3D castStartSafe = safeIntersection != null ? (safeOBBLine.From + safeOBBLine.Direction * safeIntersection.Value) : m_target;

            if (controlledEntity.Parent != null && safeIntersection != null)
            {
                MatrixD shapeCastStart = MatrixD.CreateTranslation(castStartSafe);
                HkShape hkSphere = new HkSphereShape(CAMERA_RADIUS * 2);
                var hitInfo = MyPhysics.CastShapeReturnContactBodyData(m_target, hkSphere, ref shapeCastStart, 0, 0);

                //VRageRender.MyRenderProxy.DebugDrawCapsule(castStartSafe, m_target, CAMERA_RADIUS * 2, Color.Orange, false);
                
                MyEntity hitEntity = hitInfo.HasValue ? hitInfo.Value.HkHitInfo.GetHitEntity() as MyEntity : null;
                MyEntity entity = controlledEntity;

                var hitEntityWeldingGroup = hitEntity != null ? MyWeldingGroups.Static.GetGroup(hitEntity) : null;
                bool weldingGroupEquals = false;

                while (entity != null && !weldingGroupEquals)
                {
                    if (hitEntityWeldingGroup == MyWeldingGroups.Static.GetGroup(entity))
                        weldingGroupEquals = true;

                    entity = entity.Parent;
                }

                if (hitInfo.HasValue && hitEntityWeldingGroup != null && weldingGroupEquals)
                {
                    castStartSafe = castStartSafe + hitInfo.Value.HkHitInfo.HitFraction * (m_target - castStartSafe);
                }
                else
                {
                    safeObb = GetEntitySafeOBB(controlledEntity);
                    safeObbWithCollisionExtents = new MyOrientedBoundingBoxD(safeObb.Center, safeObb.HalfExtent + 0.5f * CAMERA_RADIUS, safeObb.Orientation);
                    safeIntersection = safeObbWithCollisionExtents.Intersects(ref safeOBBLine);
                    castStartSafe = safeIntersection != null ? (safeOBBLine.From + safeOBBLine.Direction * safeIntersection.Value) : m_target;
                }
                hkSphere.RemoveReference();
            }

            // raycast against occluders
            Vector3D safePositionCandidate;
            //double lastSafeMinimumDistance = m_safeMinimumDistance;
            m_safeMinimumDistance = controlledEntity is MyCharacter ? 0 : (castStartSafe - m_target).Length(); // store current safe minimum dist
            m_safeMinimumDistance = Math.Max(m_safeMinimumDistance, MIN_VIEWER_DISTANCE);
            //if (lastSafeMinimumDistance + 30.0f < m_safeMinimumDistance)
            //{
            //    castStartSafe = m_target + (castStartSafe - m_target) / m_safeMinimumDistance * lastSafeMinimumDistance;
            //    m_safeMinimumDistance = lastSafeMinimumDistance;
            //}
            Vector3D raycastOrigin = (controlledEntity is MyCharacter) ? m_target : castStartSafe;
            MyCameraRaycastResult raycastResult = RaycastOccludingObjects(controlledEntity, ref raycastOrigin, ref m_position,
                ref castStartSafe, out safePositionCandidate);

            if (controlledEntity is MyCharacter && safeObb.Contains(ref safePositionCandidate))
                raycastResult = MyCameraRaycastResult.FoundOccluderNoSpace;

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

                VRageRender.MyRenderProxy.DebugDrawSphere(castStartSafe, 0.2f, Color.Green, 1.0f, false, true);
                VRageRender.MyRenderProxy.DebugDrawSphere(safePositionCandidate, 1.0f, Color.LightPink, 1, false);
            }

            switch (raycastResult)
            {
                case MyCameraRaycastResult.Ok:
                case MyCameraRaycastResult.FoundOccluder:
                    m_positionCurrentIsSafe = true;
                    {
                        double distFromCandidateToTarget = (safePositionCandidate - m_target).Length();
                        if (m_disableSpringThisFrame)
                        {
                            m_lastRaycastDist = (float) distFromCandidateToTarget;
                        }

                        if (!m_disableSpringThisFrame && 
                            ((distFromCandidateToTarget > m_lastRaycastDist + CAMERA_RADIUS && distFromCandidateToTarget > m_safeMinimumDistance)
                            || raycastResult == MyCameraRaycastResult.Ok))
                        {
                            // now we need it from the other side
                            double newDist = (safePositionCandidate - m_position).Length();
                            // new safe position is further from target => change over time (zoom out)
                            if (m_positionSafeZoomingOutTimeout <= 0)
                            {
                                float distDiffZoomSpeed = 1 -
                                                          MathHelper.Clamp((float) Math.Abs(m_lastRaycastDist - newDist), 0.0f,
                                                              1.0f - MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS);
                                m_positionSafeZoomingOutSpeed += distDiffZoomSpeed;
                                m_positionSafeZoomingOutSpeed = MathHelper.Clamp(m_positionSafeZoomingOutSpeed, 0.0f,
                                    1.0f);

                                Vector3D targetToPosSafe = m_positionSafe - m_target;
                                double lenTargetToPosSafe = targetToPosSafe.Length();
                                Vector3D rotatedPositionSafe = m_target +
                                                               Vector3D.Normalize(safePositionCandidate - m_target) *
                                                               lenTargetToPosSafe;
                                m_positionSafe = Vector3D.Lerp(rotatedPositionSafe, safePositionCandidate,
                                    m_positionSafeZoomingOutSpeed);
                            }
                            else
                            {
                                m_positionSafeZoomingOutTimeout -= MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS;

                                Vector3D targetToPosSafe = m_positionSafe - m_target;
                                double lenTargetToPosSafe = targetToPosSafe.Length();
                                m_positionSafe = m_target + Vector3D.Normalize(safePositionCandidate - m_target) * lenTargetToPosSafe;
                            }
                        }
                        else
                        {
                            // new safe position is closer or closer than safe distance => instant change
                            m_positionSafeZoomingOutSpeed = 0.0f;    // set zooming out speed to zero for next time
                            m_positionSafeZoomingOutTimeout = 0;// controlledEntity.Parent != null ? m_positionSafeZoomingOutDefaultTimeoutMs : 0;
                            m_positionSafe = safePositionCandidate;
                            m_disableSpringThisFrame = true;
                            m_positionCurrentIsSafe = distFromCandidateToTarget >= m_safeMinimumDistance;
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
                VRageRender.MyRenderProxy.DebugDrawSphere(m_positionSafe, 0.225f, Color.Purple, 1, false);
                VRageRender.MyRenderProxy.DebugDrawSphere(safePositionCandidate, 0.2f, Color.Azure, 1, false);
            }
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
            if (!(cameraController is MyEntity))  // also checks for not null
                return;

            var controlledEntity = MySession.Static.ControlledEntity;
            if (controlledEntity == null)
                return;

            // get latest head matrix
            if (!isCharacter)
            {
                var headMatrix = controlledEntity.GetHeadMatrix(true);
                m_targetOrientation = headMatrix.GetOrientation();
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
            var localAABBHr = topControlledEntity.PositionComp.LocalAABB;
            Vector3D centerToTarget = targetInLocal - localAABBHr.Center;
            Vector3D backVec = Vector3D.Normalize(orientationInLocal.Backward);

            // calculate offset for the 
            double projectedCenterToTarget = Vector3D.Dot(centerToTarget, backVec);
            double projectedHalfExtent = Math.Abs(Vector3D.Dot(localAABBHr.HalfExtents, backVec));
            double finalLength = Math.Max(projectedHalfExtent - projectedCenterToTarget, m_lookAtDefaultLength);

            //Vector3D targetWithOffset = centerToTarget + (finalLength * backVec);

            double width = m_lookAtDefaultLength; // some default value
            if (Math.Abs(backVec.Z) > 0.0001)
                width = localAABBHr.HalfExtents.X * 1.5f;
            else if (Math.Abs(backVec.X) > 0.0001)
                width = localAABBHr.HalfExtents.Z * 1.5f;

            // calculate complete offset for controlled object
            double halfFovTan = Math.Tan(MySector.MainCamera.FieldOfView * 0.5);
            double offset = width / (2 * halfFovTan);
            offset += finalLength;

            double clampDist = MathHelper.Clamp(offset, MIN_VIEWER_DISTANCE, MAX_VIEWER_DISTANCE);

            Vector3D lookAt = m_lookAtDirectionCharacter * clampDist;

            SetPositionAndLookAt(lookAt);
        }

        // ---------- zoom --------------------------

        public void UpdateZoom()
        {
            bool canZoom = (!MyPerGameSettings.ZoomRequiresLookAroundPressed || MyInput.Static.IsGameControlPressed(MyControlsSpace.LOOKAROUND));
            double newDistance = 0;
            var velocity = Vector3.Zero;
            if (MySession.Static.ControlledEntity != null && MySession.Static.ControlledEntity.Entity.Physics != null)
                velocity = MySession.Static.ControlledEntity.Entity.Physics.LinearVelocity;

            Vector3D positionSafe = m_positionSafe + (Vector3D) velocity * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

            //if (canZoom && !MyInput.Static.IsAnyCtrlKeyPressed() && !MyInput.Static.IsAnyShiftKeyPressed())
            if (canZoom)
            {
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
            Vector3D lookAt = ((MySession.Static != null && MySession.Static.ControlledEntity is MyCharacter) ? m_lookAtDirectionCharacter : m_lookAtDirection) * newDistance.Value;
            SetPositionAndLookAt(lookAt);
            m_disableSpringThisFrame = true;
            m_lastRaycastDist = (float)newDistance.Value;
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

            IMyControllableEntity controlledEntity = MySession.Static.ControlledEntity;
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

        private void DebugDrawTrail()
        {
            if (m_debugDraw)
            {
                if (m_debugLastSpectatorPositions == null)
                {
                    m_debugLastSpectatorPositions = new List<Vector3D>(1024);
                    m_debugLastSpectatorDesiredPositions = new List<Vector3D>(1024);
                }

                m_debugLastSpectatorPositions.Add(m_position);
                m_debugLastSpectatorDesiredPositions.Add(m_desiredPosition);

                if (m_debugLastSpectatorDesiredPositions.Count > 60)
                {
                    m_debugLastSpectatorPositions.RemoveRange(0, 1);
                    m_debugLastSpectatorDesiredPositions.RemoveRange(0, 1);
                }

                for (int i = 1; i < m_debugLastSpectatorPositions.Count; i++)
                {
                    float frac = (float)i / m_debugLastSpectatorPositions.Count;
                    Color color = new Color(frac * frac, 0, 0);
                    VRageRender.MyRenderProxy.DebugDrawLine3D(m_debugLastSpectatorPositions[i - 1],
                        m_debugLastSpectatorPositions[i], color, color, true);

                    color = new Color(frac * frac, frac * frac, frac * frac);
                    VRageRender.MyRenderProxy.DebugDrawLine3D(m_debugLastSpectatorDesiredPositions[i - 1],
                        m_debugLastSpectatorDesiredPositions[i], color, color, true);
                }
            }
            else
            {
                m_debugLastSpectatorPositions = null;
                m_debugLastSpectatorDesiredPositions = null;
            }
        }
    }
}
