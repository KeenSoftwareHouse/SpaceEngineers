using System;
using VRage.ModAPI;
using VRageMath;
using VRageRender;
using VRage.Utils;
using Vector3 = VRageMath.Vector3;
using Matrix = VRageMath.Matrix;

namespace VRage.Game.Utils
{
    public class MyCamera : IMyCamera
    {
        public const float DefaultFarPlaneDistance = 20000; // default far plane distance

        //  Original was 0.5, but I changed it to 0.35 so in extreme-wide screen resolution cockpit glass isn't truncated!
        // Previous distance was 0.35, changed to 0.27 so fov 100 degrees is displayed properly (otherwise cockpit would be truncated)
        // Lowered even more to 0.13 to solve near clip problems with cockpit in triple-head
        public float NearPlaneDistance = 0.05f;
        //  Two times bigger than sector's diameter because we want to draw impostor voxel maps in surrounding sectors
        //  According to information from xna creators site, far plane distance doesn't have impact on depth buffer precission, but near plane has.
        //  Therefore far plane distance can be any large number, but near plane distance can't be too small.
        public float FarPlaneDistance = DefaultFarPlaneDistance; // farplane is now set by MyObjectBuilder_SessionSettings.ViewDistance

        // Near clip plane for "near" objects, near objects are cockpit, cockpit glass and weapons
        const float NearForNearObjects = 0.01f;

        // Far clip plane for "near" objects, near objects are cockpit, cockpit glass and weapons
        const float FarForNearObjects = 100.0f;

        public float FieldOfView = (float)(Math.PI / 2.0);
        public float FieldOfViewForNearObjects = (float)(MathHelper.ToRadians(70));

        //  This are ACTUAL public properties of a camera. If we are looking forward, it contains related values.
        public Vector3D Position;

        public Vector3 ForwardVector = Vector3.Forward;
        public Vector3 LeftVector = Vector3.Left;
        public Vector3 UpVector = Vector3.Up;
        public Vector3D PreviousPosition;
        public MyViewport Viewport;                    //  Current viewport
        public MatrixD InversePositionTranslationMatrix;  //  This is: Matrix.CreateTranslation(-MyCamera.Position);
        public MatrixD ViewMatrix;                    //  This is view matrix when camera in real position
        public MatrixD ViewMatrixAtZero;
        public MatrixD WorldMatrix;
        public MatrixD ProjectionMatrix;
        public MatrixD ProjectionMatrixFar;
        public MatrixD ProjectionMatrixForNearObjects;
        public MatrixD ViewProjectionMatrix;          //  This is view-projection matrix when camera in real position
        public MatrixD ViewProjectionMatrixFar;       
        public BoundingBoxD BoundingBox;              //    Bounding box calculated from bounding frustum, updated every draw
        public BoundingSphereD BoundingSphere;        //    Bounding sphere calculated from bounding frustum, updated every draw

        public float FieldOfViewAngle
        {
            get { return MathHelper.ToDegrees(FieldOfView); }
            set { FieldOfView = MathHelper.ToRadians(value); }
        }

        public float FieldOfViewAngleForNearObjects
        {
            get { return MathHelper.ToDegrees(FieldOfViewForNearObjects); }
            set { FieldOfViewForNearObjects = MathHelper.ToRadians(value); }
        }

        public MyCameraZoomProperties Zoom;

        //  Calculated or constants parameters of this camera
        public float ForwardAspectRatio;

        public BoundingFrustumD BoundingFrustum = new BoundingFrustumD(MatrixD.Identity);
        public BoundingFrustumD BoundingFrustumFar = new BoundingFrustumD(MatrixD.Identity);

        /// <summary>
        /// Gets current fov with considering if zoom is enabled
        /// </summary>
        public float FovWithZoom
        {
            get { return Zoom.GetFOV(); }
        }

        /// <summary>
        /// Gets current fov with considering if zoom is enabled
        /// </summary>
        public float FovWithZoomForNearObjects
        {
            get { return Zoom.GetFOVForNearObjects(); }
        }

        public MyCamera(float fieldOfView, MyViewport currentScreenViewport)
        {
            FieldOfView = fieldOfView; // MySandboxGame.Config.FieldOfView; // no dependency on sandbox!
            Zoom = new MyCameraZoomProperties(this);
            UpdateScreenSize(currentScreenViewport);
        }

        public void Update(float updateStepSize)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyCamera-Update");
            Zoom.Update(updateStepSize);
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        public void UpdateScreenSize(MyViewport currentScreenViewport)
        {
            Viewport = currentScreenViewport; // MySandboxGame.ScreenViewport; // no dependency on sandbox!

            PreviousPosition = Vector3D.Zero;
            BoundingFrustum = new BoundingFrustumD(MatrixD.Identity);

            ForwardAspectRatio = (float)Viewport.Width / (float)Viewport.Height;
        }

        public void SetViewMatrix(MatrixD value)
        {
            ViewMatrix = value;

            MatrixD.Invert(ref ViewMatrix, out WorldMatrix);
            Position = WorldMatrix.Translation;
            InversePositionTranslationMatrix = MatrixD.CreateTranslation(-Position);

            PreviousPosition = Position;

            ForwardVector = (Vector3)WorldMatrix.Forward;
            UpVector = (Vector3)WorldMatrix.Up;
            LeftVector = (Vector3)WorldMatrix.Left;

            //  Projection matrix according to zoom level
            ProjectionMatrix = MatrixD.CreatePerspectiveFieldOfView(FovWithZoom, ForwardAspectRatio,
                GetSafeNear(),
                FarPlaneDistance);

            ProjectionMatrixFar = MatrixD.CreatePerspectiveFieldOfView(FovWithZoom, ForwardAspectRatio,
                GetSafeNear(),
                1000000);

            ViewProjectionMatrix = ViewMatrix * ProjectionMatrix;
            ViewProjectionMatrixFar = ViewMatrix * ProjectionMatrixFar;

            //  Projection matrix according to zoom level
            float near = System.Math.Min(NearPlaneDistance, NearForNearObjects); //minimum cockpit distance 
            ProjectionMatrixForNearObjects = MatrixD.CreatePerspectiveFieldOfView(FovWithZoomForNearObjects, ForwardAspectRatio,
                near,
                FarForNearObjects);

            float safenear = System.Math.Min(4, NearPlaneDistance); //minimum cockpit distance

            ViewMatrixAtZero = value;
            ViewMatrixAtZero.M14 = 0;
            ViewMatrixAtZero.M24 = 0;
            ViewMatrixAtZero.M34 = 0;
            ViewMatrixAtZero.M41 = 0;
            ViewMatrixAtZero.M42 = 0;
            ViewMatrixAtZero.M43 = 0;
            ViewMatrixAtZero.M44 = 1;

            UpdateBoundingFrustum();

            VRageRender.MyRenderProxy.SetCameraViewMatrix(
                value,
                ProjectionMatrix,
                ProjectionMatrixForNearObjects,
                safenear,
                Zoom.GetFOVForNearObjects(),
                Zoom.GetFOV(),
                NearPlaneDistance,
                FarPlaneDistance,
                NearForNearObjects,
                FarForNearObjects,
                Position);
        }

        /// <summary>
        /// Changes FOV for ForwardCamera (updates projection matrix)
        /// SetViewMatrix overwrites this changes
        /// </summary>
        /// <param name="fov"></param>
        public void ChangeFov(float fov)
        {
            //  Projection matrix according to zoom level
            ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(fov, ForwardAspectRatio,
                GetSafeNear(),
                FarPlaneDistance);
        }

        float GetSafeNear()
        {
            return System.Math.Min(4, NearPlaneDistance); //minimum cockpit distance            
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

        public double GetDistanceWithFOV(Vector3D position)
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

        #region ModAPI

        Vector3D IMyCamera.WorldToScreen(ref Vector3D worldPos)
        {
            return Vector3D.Transform(worldPos, ViewProjectionMatrix);
        }

        float IMyCamera.FieldOfViewAngle
        {
            get { return FieldOfViewAngle; }
        }

        float IMyCamera.FieldOfViewAngleForNearObjects
        {
            get { return FieldOfViewAngleForNearObjects; }
        }

        float IMyCamera.FovWithZoom
        {
            get { return FovWithZoom; }
        }

        float IMyCamera.FovWithZoomForNearObjects
        {
            get { return FovWithZoomForNearObjects; }
        }

        double IMyCamera.GetDistanceWithFOV(Vector3D position)
        {
            return GetDistanceWithFOV(position);
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
            get { return ProjectionMatrixForNearObjects; }
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