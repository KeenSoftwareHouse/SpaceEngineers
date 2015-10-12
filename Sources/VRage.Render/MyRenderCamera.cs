using System;
using VRageMath;
using System.Diagnostics;

using VRageRender.Utils;
using VRageRender.Effects;

using SharpDX;

using Vector2 = VRageMath.Vector2;
using Vector3 = VRageMath.Vector3;
using Vector4 = VRageMath.Vector4;
using Rectangle = VRageMath.Rectangle;
using Matrix = VRageMath.Matrix;
using Color = VRageMath.Color;
using BoundingBox = VRageMath.BoundingBox;
using BoundingSphere = VRageMath.BoundingSphere;
using BoundingFrustum = VRageMath.BoundingFrustum;
using VRage;
using VRage.Utils;
using VRage.Stats;
using VRage.Library.Utils;
using VRage.Voxels;


namespace VRageRender
{
    class MyRenderCamera : MyRenderComponentBase
    {
        public override int GetID()
        {
            return (int)MyRenderComponentID.RenderCamera;
        }

        //  Original was 0.5, but I changed it to 0.35 so in extreme-wide screen resolution cockpit glass isn't truncated!
        // Previous distance was 0.35, changed to 0.27 so fov 100 degrees is displayed properly (otherwise cockpit would be truncated)
        // Lowered even more to 0.13 to solve near clip problems with cockpit in triple-head
        public static float NEAR_PLANE_DISTANCE = 2.0f;
        //  Two times bigger than sector's diameter because we want to draw impostor voxel maps in surrounding sectors
        //  According to information from xna creators site, far plane distance doesn't have impact on depth buffer precission, but near plane has.
        //  Therefore far plane distance can be any large number, but near plane distance can't be too small.
        public static float FAR_PLANE_DISTANCE = 7000;

        // Near clip plane for "near" objects, near objects are cockpit, cockpit glass and weapons
        public static float NEAR_PLANE_FOR_NEAR_OBJECTS = 0.08f;

        // Far clip plane for "near" objects, near objects are cockpit, cockpit glass and weapons
        public static float FAR_PLANE_FOR_NEAR_OBJECTS = 100.0f;

        public static float NEAR_PLANE_FOR_PARTICLES = NEAR_PLANE_FOR_NEAR_OBJECTS;
        public static float FAR_PLANE_FOR_PARTICLES = 50000;

        public const float FAR_OBJECTS_RATIO = 500.0f;

        public static float NEAR_PLANE_FOR_BACKGROUND 
        {
            get
            {
                return Math.Max(NEAR_PLANE_DISTANCE, FAR_PLANE_DISTANCE - 9.0f*(FAR_PLANE_DISTANCE / FAR_OBJECTS_RATIO) * MyVoxelCoordSystems.RenderCellSizeInMeters(0));
            }
        }

        public static float FAR_PLANE_FOR_BACKGROUND
        {
            get
            {
                return FAR_OBJECTS_RATIO * FAR_PLANE_DISTANCE;
            }
        }

        // When LOD transition distances are more than FAR PLANE, it must be adjusted in way where LOD near < LOD far < background start < background end
        // these distances must have different values in depth buffer, this threshold make sure they will
        public static readonly float FAR_DISTANCE_THRESHOLD = 100;

        //  This are ACTUAL public properties of a camera. If we are looking forward, it contains related values.
        public static Vector3D Position;
        public static void SetPosition(Vector3D value)
        {
            MyUtils.AssertIsValid(value);
            Position = value;
        }


        public static Vector3 ForwardVector = Vector3.Forward;
        public static Vector3 LeftVector = Vector3.Left;
        public static Vector3 UpVector = Vector3.Up;
        public static Viewport Viewport;                    //  Current viewport
        public static MatrixD InversePositionTranslationMatrix = MatrixD.Identity;  //  This is: Matrix.CreateTranslation(-MyCamera.Position);
        public static MatrixD ViewMatrix = MatrixD.Identity;                    //  This is view matrix when camera in real position
        public static Matrix ViewMatrixAtZero = Matrix.Identity;              //  This is view matrix when camera at zero position [0,0,0]
        public static Matrix ProjectionMatrix = Matrix.Identity;
        public static Matrix ProjectionMatrixForNearObjects = Matrix.Identity;
        public static Matrix ProjectionMatrixForFarObjects = Matrix.Identity;
        public static MatrixD ViewProjectionMatrix = MatrixD.Identity;          //  This is view-projection matrix when camera in real position
        public static Matrix ViewProjectionMatrixAtZero = Matrix.Identity;    //  This is view-projection matrix when camera at zero position [0,0,0]
        public static BoundingBoxD BoundingBox;              //    Bounding box calculated from bounding frustum, updated every draw
        public static float AspectRatio;
        public static Vector3D CornerFrustum;

        public static float FieldOfView = (float)(MathHelper.ToRadians(60));
        public static float FieldOfViewForNearObjects = (float)(MathHelper.ToRadians(70));

        static MatrixD m_viewMatrix = MatrixD.Identity;

        static float m_lodTransitionDistanceNear;
        static float m_lodTransitionDistanceFar;
        static float m_lodTransitionDistanceBackgroundStart;
        static float m_lodTransitionDistanceBackgroundEnd;

        static BoundingFrustumD m_boundingFrustum = new BoundingFrustumD(MatrixD.Identity);

        internal static Matrix? m_backupMatrix = null;

        /// <summary>
        /// GetBoundingFrustum
        /// </summary>
        /// <returns></returns>
        public static BoundingFrustumD GetBoundingFrustum()
        {
            return m_boundingFrustum;
        }


        public override void LoadContent()
        {
            MyRender.GetRenderProfiler().StartProfilingBlock("MyCamera::LoadContent");
            MyRender.GetRenderProfiler().EndProfilingBlock();
        }

        public static void UpdateScreenSize()
        {
            MyRender.Log.WriteLine("MyRenderCamera.UpdateScreenSize() - START");

            Viewport = MyRender.GraphicsDevice.Viewport;

            if (MyRender.GetScreenshot() != null)
            {
                Viewport = ScaleViewport(Viewport, MyRender.GetScreenshot().SizeMultiplier);
            }
            
            AspectRatio = (float)Viewport.Width / (float)Viewport.Height;

            MyRender.Log.WriteLine("MyRenderCamera.UpdateScreenSize() - END");
        }

        private static Viewport ScaleViewport(Viewport viewport, VRageMath.Vector2 scale)
        {
            return new Viewport((int)(viewport.X * scale.X), (int)(viewport.Y * scale.Y), (int)(viewport.Width * scale.X), (int)(viewport.Height * scale.Y));
        }

        static MyInterpolationQueue<MatrixD> m_interpolation = new MyInterpolationQueue<MatrixD>(5, MatrixD.Slerp);

        public static void SetViewMatrix(MatrixD value, MyTimeSpan? updateTime)
        {
            if (MyRender.Settings.EnableCameraInterpolation && updateTime.HasValue)
            {
                var world = MatrixD.Invert(value);
                m_interpolation.AddSample(ref world, updateTime.Value);
                MatrixD worldOut;
                float i = m_interpolation.Interpolate(MyRender.InterpolationTime, out worldOut);
                m_viewMatrix = MatrixD.Invert(worldOut);
                MyRenderStats.Generic.Write("Camera interpolator", i, MyStatTypeEnum.Max, 250, 2);

                SetPosition(worldOut.Translation);
            }
            else
            {
                m_viewMatrix = value;

                MatrixD invertedViewMatrix;
                MatrixD.Invert(ref m_viewMatrix, out invertedViewMatrix);
                SetPosition(invertedViewMatrix.Translation);
            }

            InversePositionTranslationMatrix = MatrixD.CreateTranslation(-Position);
        }

        public static void ChangeClipPlanes(float near, float far, bool applyNow = false)
        {
            Debug.Assert(!m_backupMatrix.HasValue, "Reset clip planes before changing clip planes again");
            m_backupMatrix = ProjectionMatrix;
            ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(FieldOfView, AspectRatio, near, far);
            if (applyNow)
            {
                UpdateCamera();
            }
        }

        public static void SetParticleClipPlanes(bool applyNow = false)
        {
            float near = System.Math.Min(MyRenderCamera.NEAR_PLANE_DISTANCE, MyRenderCamera.NEAR_PLANE_FOR_PARTICLES); //minimum cockpit distance            
            ChangeClipPlanes(near, MyRenderCamera.FAR_PLANE_FOR_PARTICLES, applyNow);
        }

        public static void SetNearObjectsClipPlanes(bool applyNow = false)
        {
            float near = System.Math.Min(MyRenderCamera.NEAR_PLANE_DISTANCE, MyRenderCamera.NEAR_PLANE_FOR_NEAR_OBJECTS); //minimum cockpit distance            
            ChangeClipPlanes(near, MyRenderCamera.FAR_PLANE_FOR_NEAR_OBJECTS, applyNow);
        }

        public static float SafeNearForForward = MyRenderCamera.NEAR_PLANE_DISTANCE;

        static float GetSafeNear()
        {
            return System.Math.Min(4, SafeNearForForward);
        }

        public static void ResetClipPlanes(bool applyNow = false)
        {
            Debug.Assert(m_backupMatrix.HasValue, "Nothing to reset, use change clip planes first");
            ProjectionMatrix = m_backupMatrix.Value;
            m_backupMatrix = null;
            if (applyNow)
            {
                UpdateCamera();
            }
            //ChangeClipPlanes(GetSafeNear(), MyCamera.FAR_PLANE_DISTANCE, applyNow);
        }

        public static void SetCustomProjection(Matrix projection)
        {
            ProjectionMatrix = projection;
        }

        //  Distances for LOD transition, near and far. Zoom is applied only of forward camera.
        static void UpdateLodTransitionDistances()
        {
            m_lodTransitionDistanceNear = MyRenderConstants.RenderQualityProfile.LodTransitionDistanceNear;
            m_lodTransitionDistanceFar = MyRenderConstants.RenderQualityProfile.LodTransitionDistanceFar;
            m_lodTransitionDistanceBackgroundStart = MyRenderConstants.RenderQualityProfile.LodTransitionDistanceBackgroundStart;
            m_lodTransitionDistanceBackgroundEnd = MyRenderConstants.RenderQualityProfile.LodTransitionDistanceBackgroundEnd;

            // Make sure all distances are smaller than FAR_PLANE_DISTANCE (otherwise it would broke LOD transition effect and background blending)
            if (m_lodTransitionDistanceBackgroundEnd > FAR_PLANE_DISTANCE)
            {
                m_lodTransitionDistanceBackgroundEnd = FAR_PLANE_DISTANCE - FAR_DISTANCE_THRESHOLD;

                if (m_lodTransitionDistanceBackgroundStart > m_lodTransitionDistanceBackgroundEnd)
                {
                    m_lodTransitionDistanceBackgroundStart = m_lodTransitionDistanceBackgroundEnd - FAR_DISTANCE_THRESHOLD;

                    if (m_lodTransitionDistanceFar > m_lodTransitionDistanceBackgroundStart)
                    {
                        m_lodTransitionDistanceFar = m_lodTransitionDistanceBackgroundStart - FAR_DISTANCE_THRESHOLD;

                        if (m_lodTransitionDistanceNear > m_lodTransitionDistanceFar)
                        {
                            m_lodTransitionDistanceNear = m_lodTransitionDistanceFar - FAR_DISTANCE_THRESHOLD;
                        }
                    }
                }
            }
        }

        public static void UpdateCamera()
        {
            ViewMatrix = m_viewMatrix;
            ViewProjectionMatrix = ViewMatrix * ProjectionMatrix;

            UpdateVectors();
            UpdateBoundingFrustum();

            ViewMatrixAtZero = Matrix.CreateLookAt(Vector3.Zero, ForwardVector, UpVector);

            ViewProjectionMatrixAtZero = ViewMatrixAtZero * ProjectionMatrix;

            UpdateLodTransitionDistances();
            CornerFrustum = CalculateCornerFrustum();

            ProjectionMatrixForFarObjects = Matrix.CreatePerspectiveFieldOfView(FieldOfView, AspectRatio, NEAR_PLANE_FOR_BACKGROUND, FAR_PLANE_FOR_BACKGROUND);
        }

        static void UpdateVectors()
        {
            MatrixD invertedViewMatrix;
            MatrixD.Invert(ref ViewMatrix, out invertedViewMatrix);
            ForwardVector = (Vector3)invertedViewMatrix.Forward;
            LeftVector = (Vector3)invertedViewMatrix.Left;
            UpVector = (Vector3)invertedViewMatrix.Up;
        }

        static void UpdateBoundingFrustum()
        {
            //  Update frustum
            m_boundingFrustum.Matrix = ViewProjectionMatrix;

            //  Update bounding box
            BoundingBox = BoundingBoxD.CreateInvalid();
            //todo
            //BoundingBox = BoundingBoxHelper.AddFrustum(ref BoundingFrustum, ref BoundingBox);

            //  Update bounding sphere
            //todo
            //BoundingSphere = MyUtils.GetBoundingSphereFromBoundingBox(ref BoundingBox);
        }


        public static bool IsInFrustum(BoundingBoxD boundingBox)
        {
            return IsInFrustum(ref boundingBox);
        }

        //  Checks if specified bounding sphere is in actual bounding frustum
        //  IMPORTANT: If you observe bad result of this test, check how you transform your bounding sphere.
        //  Don't use BoundingSphere.Transform. Instead transform sphere center manualy and then create new sphere.
        public static bool IsInFrustum(ref BoundingSphereD boundingSphere)
        {
            VRageMath.ContainmentType result;
            m_boundingFrustum.Contains(ref boundingSphere, out result);
            return result != VRageMath.ContainmentType.Disjoint;
        }

        //  Checks if specified Vector3 is in actual bounding frustum
        public static bool IsInFrustum(ref Vector3D point)
        {
            VRageMath.ContainmentType result;
            m_boundingFrustum.Contains(ref point, out result);
            return result != VRageMath.ContainmentType.Disjoint;
        }

        public static bool IsInFrustum(ref BoundingBoxD boundingBox)
        {
            VRageMath.ContainmentType result;
            m_boundingFrustum.Contains(ref boundingBox, out result);
            return result != VRageMath.ContainmentType.Disjoint;
        }


        // Should not be used elsewhere than MyRender.ApplySetups, others should use MyRender.CurrentRenderSetup...
        public static float GetLodTransitionDistanceNear()
        {
            return m_lodTransitionDistanceNear;
        }

        // Should not be used elsewhere than MyRender.ApplySetups, others should use MyRender.CurrentRenderSetup...
        public static float GetLodTransitionDistanceFar()
        {
            return m_lodTransitionDistanceFar;
        }

        // Should not be used elsewhere than MyRender.ApplySetups, others should use MyRender.CurrentRenderSetup...
        public static float GetLodTransitionDistanceBackgroundStart()
        {
            return m_lodTransitionDistanceBackgroundStart;
        }

        // Should not be used elsewhere than MyRender.ApplySetups, others should use MyRender.CurrentRenderSetup...
        public static float GetLodTransitionDistanceBackgroundEnd()
        {
            return m_lodTransitionDistanceBackgroundEnd;
        }

        static Vector3D CalculateCornerFrustum()
        {
            double farY = Math.Tan(Math.PI / 3.0 / 2.0) * MyRenderCamera.FAR_PLANE_DISTANCE;
            double farX = farY * AspectRatio;
            return new Vector3D(farX, farY, (double)MyRenderCamera.FAR_PLANE_DISTANCE);
        }

        public static void SetupBaseEffect(MyEffectBase effect, MyLodTypeEnum lodType, float fogMultiplierMult = 1.0f)
        {
            if (MyRender.Settings.EnableFog)
            {
                effect.SetFogDistanceFar(MyRender.FogProperties.FogFar);
                effect.SetFogDistanceNear(MyRender.FogProperties.FogNear);
                effect.SetFogColor(MyRender.FogProperties.FogColor);
                effect.SetFogMultiplier(MyRender.FogProperties.FogMultiplier * fogMultiplierMult);
                effect.SetFogBacklightMultiplier(MyRender.FogProperties.FogBacklightMultiplier);
            }
            else
            {
                effect.SetFogMultiplier(0);
            }
        }

        /// <summary>
        /// Changes FOV for ForwardCamera (updates projection matrix)
        /// SetViewMatrix overwrites this changes
        /// </summary>
        /// <param name="fov"></param>
        public static void ChangeFov(float fov)
        {
            //  Projection matrix according to zoom level
            ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(fov, AspectRatio,
                GetSafeNear(),
                MyRenderCamera.FAR_PLANE_DISTANCE);
        }
    }
}