using System;
using VRage.Game.Components;
using VRage.ModAPI;
using VRageRender;
using VRage.Utils;
using VRageMath;
using Vector3 = VRageMath.Vector3;
using Vector3D = VRageMath.Vector3D;
using MatrixD = VRageMath.MatrixD;
using BoundingBoxD = VRageMath.BoundingBoxD;
using BoundingSphereD = VRageMath.BoundingSphereD;
using BoundingFrustumD = VRageMath.BoundingFrustumD;

namespace VRage.Game.Utils
{
    public class MyCamera: IMyCamera
    {
        // Default far plane distance
        public const float DefaultFarPlaneDistance = 20000;
        // Near clip plane for "near" objects, near objects are cockpit, cockpit glass and weapons
        private const float NearForNearObjects = 0.01f;
        // Far clip plane for "near" objects, near objects are cockpit, cockpit glass and weapons
        private const float FarForNearObjects = 100.0f;

        //  Original was 0.5, but I changed it to 0.35 so in extreme-wide screen resolution cockpit glass isn't truncated!
        // Previous distance was 0.35, changed to 0.27 so fov 100 degrees is displayed properly (otherwise cockpit would be truncated)
        // Lowered even more to 0.13 to solve near clip problems with cockpit in triple-head
        public float NearPlaneDistance = 0.05f;
        //  Two times bigger than sector's diameter because we want to draw impostor voxel maps in surrounding sectors
        //  According to information from xna creators site, far plane distance doesn't have impact on depth buffer precission, but near plane has.
        //  Therefore far plane distance can be any large number, but near plane distance can't be too small.
        public float FarPlaneDistance = DefaultFarPlaneDistance; // farplane is now set by MyObjectBuilder_SessionSettings.ViewDistance

        // Current field of view. Set in constructor, can be changed later.
        public float FieldOfView;
        
        //  This are ACTUAL public properties of a camera. If we are looking forward, it contains related values.
        public Vector3D PreviousPosition;

        public MyViewport Viewport;                   // Current viewport
        public MatrixD WorldMatrix = MatrixD.Identity;             // World matrix is cached inversion of view matrix
        public MatrixD ViewMatrix = MatrixD.Identity;              // This is view matrix when camera in real position
        public MatrixD ProjectionMatrix = MatrixD.Identity;        // Projection matrix of this camera
        public MatrixD ProjectionMatrixFar = MatrixD.Identity;     // Projection matrix for far objects
        public MatrixD ViewProjectionMatrix = MatrixD.Identity;    // This is view-projection matrix when camera in real position
        public MatrixD ViewProjectionMatrixFar = MatrixD.Identity; // This is view-projection matrix for far objects when camera in real position
        public BoundingBoxD BoundingBox;              // Bounding box calculated from bounding frustum, updated every draw
        public BoundingSphereD BoundingSphere;        // Bounding sphere calculated from bounding frustum, updated every draw

        public MyCameraZoomProperties Zoom;

        public BoundingFrustumD BoundingFrustum = new BoundingFrustumD(MatrixD.Identity);
        public BoundingFrustumD BoundingFrustumFar = new BoundingFrustumD(MatrixD.Identity);

        public float AspectRatio { get; private set; }

        /// <summary>
        /// Member that shakes with the camera.
        /// Note: If we start to have more cameras in the scene, this should be changed to component, because not every camera needs it.
        ///       But currently - we use just one camera, so it is a member.
        /// </summary>
        public readonly MyCameraShake CameraShake = new MyCameraShake();
        /// <summary>
        /// Member that implements camera spring.
        /// Note: If we start to have more cameras in the scene, this should be changed to component, because not every camera needs it.
        ///       But currently - we use just one camera, so it is a member.
        /// </summary>
        public readonly MyCameraSpring CameraSpring = new MyCameraSpring();

        /// <summary>
        /// Current view matrix without translation part.
        /// </summary>
        public MatrixD ViewMatrixAtZero
        {
            get
            {
                MatrixD rtnMatrix = ViewProjectionMatrix;
                rtnMatrix.M14 = 0;
                rtnMatrix.M24 = 0;
                rtnMatrix.M34 = 0;
                rtnMatrix.M41 = 0;
                rtnMatrix.M42 = 0;
                rtnMatrix.M43 = 0;
                rtnMatrix.M44 = 1;
                return rtnMatrix;
            }
        }

        /// <summary>
        /// Forward vector of camera world matrix ("ahead from camera")
        /// </summary>
        public Vector3 ForwardVector
        {
            get { return WorldMatrix.Forward; }
        }
        /// <summary>
        /// Left vector of camera world matrix ("to the left from camera")
        /// </summary>
        public Vector3 LeftVector
        {
            get { return WorldMatrix.Left; }
        }
        /// <summary>
        /// Up vector of camera world matrix ("up from camera")
        /// </summary>
        public Vector3 UpVector
        {
            get { return WorldMatrix.Up; }
        }

        /// <summary>
        /// Field of view in degrees.
        /// </summary>
        public float FieldOfViewDegrees
        {
            get { return VRageMath.MathHelper.ToDegrees(FieldOfView); }
            set { FieldOfView = VRageMath.MathHelper.ToRadians(value); }
        }

        /// <summary>
        /// Gets current fov with considering if zoom is enabled
        /// </summary>
        public float FovWithZoom
        {
            get { return Zoom.GetFOV(); }
        }

        /// <summary>
        /// Get position of the camera.
        /// </summary>
        public Vector3D Position
        {
            get { return WorldMatrix.Translation; }
        }

        // -------------------------------------------------------------------------------------

        public MyCamera(float fieldOfView, MyViewport currentScreenViewport)
        {
            FieldOfView = fieldOfView; 
            Zoom = new MyCameraZoomProperties(this);
            UpdateScreenSize(currentScreenViewport);
        }

        public void Update(float updateStepTime)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyCamera-Update");
            Zoom.Update(updateStepTime);
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            Vector3 newCameraPosOffset = Vector3.Zero;
            // spring
            if (CameraSpring.Enabled)
            {
                CameraSpring.Update(updateStepTime, out newCameraPosOffset);
            }
            // shake
            if (CameraShake.ShakeEnabled)
            {
                Vector3 shakePos, shakeDir;
                CameraShake.UpdateShake(updateStepTime, out shakePos, out shakeDir);
                newCameraPosOffset += shakePos;
            }
            // apply
            if (newCameraPosOffset != Vector3.Zero)
            {
                Vector3D newCameraPosOffsetD = newCameraPosOffset;
                Vector3D newCameraPosOffsetRotatedD;
                Vector3D.Rotate(ref newCameraPosOffsetD, ref ViewMatrix, out newCameraPosOffsetRotatedD);
                ViewMatrix.Translation += newCameraPosOffsetRotatedD;
            }

            UpdatePropertiesInternal(ViewMatrix);
        }

        public void UpdateScreenSize(MyViewport currentScreenViewport)
        {
            Viewport = currentScreenViewport; 

            PreviousPosition = Vector3D.Zero;
            BoundingFrustum = new BoundingFrustumD(MatrixD.Identity);

            AspectRatio = Viewport.Width / Viewport.Height;
        }

        public void SetViewMatrix(MatrixD newViewMatrix)
        {
            PreviousPosition = Position;
            UpdatePropertiesInternal(newViewMatrix);
        }

        public void UploadViewMatrixToRender()
        {
            MyRenderProxy.SetCameraViewMatrix(
                ViewMatrix,
                ProjectionMatrix,
                GetSafeNear(),
                Zoom.GetFOV(),
                NearPlaneDistance,
                FarPlaneDistance,
                NearForNearObjects,
                FarForNearObjects,
                Position);
        }

        private void UpdatePropertiesInternal(MatrixD newViewMatrix)
        {
            ViewMatrix = newViewMatrix;
            MatrixD.Invert(ref ViewMatrix, out WorldMatrix);

            //  Projection matrix according to zoom level
            ProjectionMatrix = MatrixD.CreatePerspectiveFieldOfView(FovWithZoom, AspectRatio,
                GetSafeNear(),
                FarPlaneDistance);

            ProjectionMatrixFar = MatrixD.CreatePerspectiveFieldOfView(FovWithZoom, AspectRatio,
                GetSafeNear(),
                1000000);

            ViewProjectionMatrix = ViewMatrix * ProjectionMatrix;
            ViewProjectionMatrixFar = ViewMatrix * ProjectionMatrixFar;

            //  Projection matrix according to zoom level
            // float near = System.Math.Min(NearPlaneDistance, NearForNearObjects); //minimum cockpit distance 
            // ProjectionMatrixForNearObjects = MatrixD.CreatePerspectiveFieldOfView(FovWithZoomForNearObjects, ForwardAspectRatio,
            //    near,
            //    FarForNearObjects);

            UpdateBoundingFrustum();
        }

        float GetSafeNear()
        {
            return Math.Min(4, NearPlaneDistance); //minimum cockpit distance            
        }

        void UpdateBoundingFrustum()
        {
            //  Update frustum
            BoundingFrustum.Matrix = ViewProjectionMatrix;
            BoundingFrustumFar.Matrix = ViewProjectionMatrixFar;

            //  Update bounding box
            BoundingBox = BoundingBoxD.CreateInvalid();
            BoundingBox.Include(ref BoundingFrustum);

            //  Update bounding sphere
            BoundingSphere = MyUtils.GetBoundingSphereFromBoundingBox(ref BoundingBox);
        }

        //  Checks if specified bounding box is in actual bounding frustum
        //  IMPORTANT: If you observe bad result of this test, check how you transform your bounding box.
        //  Don't use BoundingBox.Transform. Instead transform box manualy and then create new box.
        public bool IsInFrustum(ref BoundingBoxD boundingBox)
        {
            VRageMath.ContainmentType result;
            BoundingFrustum.Contains(ref boundingBox, out result);
            return result != VRageMath.ContainmentType.Disjoint;
        }

        public bool IsInFrustum(BoundingBoxD boundingBox)
        {
            return IsInFrustum(ref boundingBox);
        }

        //  Checks if specified bounding sphere is in actual bounding frustum
        //  IMPORTANT: If you observe bad result of this test, check how you transform your bounding sphere.
        //  Don't use BoundingSphere.Transform. Instead transform sphere center manualy and then create new sphere.
        public bool IsInFrustum(ref BoundingSphereD boundingSphere)
        {
            VRageMath.ContainmentType result;
            BoundingFrustum.Contains(ref boundingSphere, out result);
            return result != VRageMath.ContainmentType.Disjoint;
        }

        public double GetDistanceFromPoint(Vector3D position)
        {
            return Vector3D.Distance(this.Position, position);
        }

        /// <summary>
        /// Gets screen coordinates of 3d world pos in 0 - 1 distance where 1.0 is screen width(for X) or height(for Y).
        /// WARNING: Y is from bottom to top.
        /// </summary>
        /// <param name="worldPos">World position.</param>
        /// <returns>Screen coordinate in 0-1 distance.</returns>
        public Vector3D WorldToScreen(ref Vector3D worldPos)
        {
            return Vector3D.Transform(worldPos, ViewProjectionMatrix);
        }

        /// <summary>
        /// Gets normalized world space line from screen space coordinates.
        /// </summary>
        /// <param name="screenCoords"></param>
        /// <returns></returns>
        public LineD WorldLineFromScreen(Vector2 screenCoords)
        {
            var matViewProjInv = MatrixD.Invert(ViewProjectionMatrix);

            // normalized screen space vector
            var raySource = new Vector4D(
                    (2.0f * screenCoords.X) / Viewport.Width - 1.0f,
                    1.0f - (2.0f * screenCoords.Y) / Viewport.Height,
                    0.0f,
                    1.0f
                );
            var rayTarget = new Vector4D(
                    (2.0f * screenCoords.X) / Viewport.Width - 1.0f,
                    1.0f - (2.0f * screenCoords.Y) / Viewport.Height,
                    1.0f,
                    1.0f
                );

            var raySourceWorld = Vector4D.Transform(raySource, matViewProjInv);
            var rayTargetWorld = Vector4D.Transform(rayTarget, matViewProjInv);

            raySourceWorld /= raySourceWorld.W;
            rayTargetWorld /= rayTargetWorld.W;

            return new LineD(new Vector3D(raySourceWorld), new Vector3D(rayTargetWorld));
        }

        #region ModAPI

        Vector3D IMyCamera.WorldToScreen(ref Vector3D worldPos)
        {
            return Vector3D.Transform(worldPos, ViewProjectionMatrix);
        }

        float IMyCamera.FieldOfViewAngle
        {
            get { return FieldOfViewDegrees; }
        }

        float IMyCamera.FieldOfViewAngleForNearObjects
        {
            get { return FieldOfViewDegrees; }
        }

        float IMyCamera.FovWithZoom
        {
            get { return FovWithZoom; }
        }

        float IMyCamera.FovWithZoomForNearObjects
        {
            get { return FovWithZoom; }
        }

        double IMyCamera.GetDistanceWithFOV(Vector3D position)
        {
            return GetDistanceFromPoint(position);
        }

        bool IMyCamera.IsInFrustum(ref BoundingBoxD boundingBox)
        {
            return IsInFrustum(ref boundingBox);
        }

        bool IMyCamera.IsInFrustum(ref BoundingSphereD boundingSphere)
        {
            return IsInFrustum(ref boundingSphere);
        }

        bool IMyCamera.IsInFrustum(BoundingBoxD boundingBox)
        {
            return IsInFrustum(boundingBox);
        }

        Vector3D IMyCamera.Position
        {
            get { return Position; }
        }

        Vector3D IMyCamera.PreviousPosition
        {
            get { return PreviousPosition; }
        }

        VRageMath.Vector2 IMyCamera.ViewportOffset
        {
            get { return new VRageMath.Vector2(Viewport.OffsetX, Viewport.OffsetY); }
        }

        VRageMath.Vector2 IMyCamera.ViewportSize
        {
            get { return new VRageMath.Vector2(Viewport.Width, Viewport.Height); }
        }

        MatrixD IMyCamera.ViewMatrix
        {
            get { return ViewMatrix; }
        }

        MatrixD IMyCamera.WorldMatrix
        {
            get { return WorldMatrix; }
        }

        MatrixD IMyCamera.ProjectionMatrix
        {
            get { return ProjectionMatrix; }
        }

        MatrixD IMyCamera.ProjectionMatrixForNearObjects
        {
            get { return ProjectionMatrix; }
        }

        float IMyCamera.NearPlaneDistance
        {
            get { return NearPlaneDistance; }
        }

        float IMyCamera.FarPlaneDistance
        {
            get { return FarPlaneDistance; }
        }

        float IMyCamera.NearForNearObjects
        {
            get { return NearForNearObjects; }
        }

        float IMyCamera.FarForNearObjects
        {
            get { return FarForNearObjects; }
        }
        #endregion
    }
}