using System;
using VRage.Utils;
using System.Diagnostics;
using ParallelTasks;
using VRageMath;
using VRageRender.Graphics;

namespace VRageRender.Lights
{
    public enum QueryState
    {
        IssueOcc,
        IssueMeasure,
        WaitOcc,
        WaitOcc1,
        WaitOcc2,
        WaitOcc3,
        WaitMeasure,
        WaitMeasure1,
        WaitMeasure2,
        WaitMeasure3,
        CheckOcc,
        CheckMeasure
    }

    class MyLightGlare
    {
        public static bool EnableLightGlares = true;
        public const float MAX_GLARE_DISTANCE = 200;

        #region Enums

        public enum SizeFunctionEnum
        {
            NoChange,
            IncreasingWithDistance,
        }

    

        #endregion

        #region Private fields

        private readonly MyRenderLight m_parent;

        // An occlusion query is used to detect when the light source is hidden behind scenery.
        // !IMPORTANT! Every other frame (m_occlusionMeasurement), we render the occlusion box without depth testing in order to find the
        // pixelCount (m_occlusionMeasurementResult) for a fully unoccluded object. When doing the actual occlusion test with depth
        // testing turned on, we divide the final pixel count by m_occlusionMeasurementResult to find out the occlusion ratio.
        private MyOcclusionQuery m_occlusionQuery;
        private MyOcclusionQuery m_measurementQuery;
        private QueryState m_state = QueryState.IssueOcc;
        private BoundingBoxD m_occlusionBox;
        private float m_occlusionRatio = 0; // 0 - fully occluded, 1 - fully visible

        //todo
        //MyRayCastOcclusionJob m_rayCastOcclusionJob = new MyRayCastOcclusionJob();
        //bool m_castingRay;

        public float? Intensity;

        #endregion

        #region Properties

        public Vector3D Position
        {
            get { return m_parent.Position; }
        }

        public MyGlareTypeEnum Type { get; set; }

        public string GlareMaterial { get; set; }

        //public bool UseOcclusionQuery { get { return Type == GlareTypeEnum.Normal; } }
        public bool UseOcclusionQuery { get { return true; } }

        public float Size;

        /// <summary>
        /// Size of the object used for the occlusion query.
        /// </summary>
        float m_querySize;
        public float QuerySize
        {
            get { return m_querySize; }
            set 
            {                
                m_querySize = value; 
            }
        }

        public float MaxDistance;

        #endregion

        public MyLightGlare(MyRenderLight light)
        {
            GlareMaterial = "LightGlare";

            m_parent = light;
            m_occlusionBox = new BoundingBoxD();
        }

        #region Load content

        public void LoadContent()
        {
            System.Diagnostics.Debug.Assert(m_occlusionQuery == null);
            System.Diagnostics.Debug.Assert(m_measurementQuery == null);

            m_occlusionQuery = MyOcclusionQueries.Get();
            m_measurementQuery = MyOcclusionQueries.Get();
        }

        public void UnloadContent()
        {
            if (m_occlusionQuery != null)
            {
                //occlusion queries are managed automatically
                MyOcclusionQueries.Return(m_occlusionQuery);
                MyOcclusionQueries.Return(m_measurementQuery);

                m_occlusionQuery = null;
                m_measurementQuery = null;
            }
        }

        #endregion


        public void Draw()
        {
            if (!EnableLightGlares || !MyRender.Settings.EnableLightGlares)
            {
                return;
            }

            Vector3D position = this.Position;
            Vector3D cameraToLight = MyRenderCamera.Position - position;
            var distance = cameraToLight.Length();
            float maxDistance = MaxDistance > 0 ? (float)Math.Min(MyRenderConstants.MAX_GPU_OCCLUSION_QUERY_DISTANCE, MaxDistance) : MyRenderConstants.MAX_GPU_OCCLUSION_QUERY_DISTANCE;

            bool canBeDiscardedIfTooFar = Type != MyGlareTypeEnum.Distant;
            if (canBeDiscardedIfTooFar && distance > maxDistance)
            {
                return;
            }

            // This is absolute maximum for light glares
            if (canBeDiscardedIfTooFar && distance > MAX_GLARE_DISTANCE)
            {
                return;
            }

            switch (Type)
            {
                case MyGlareTypeEnum.Normal:
                case MyGlareTypeEnum.Directional:
                    DrawNormalGlare((float)distance, (float)maxDistance, position, cameraToLight, GlareMaterial);
                    break;
                case MyGlareTypeEnum.Distant:
                    DrawVolumetricGlare((float)distance, position);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }


            //Vector4 red = Color.Red.ToVector4();
            //Vector4 green = Color.Green.ToVector4();
            //Vector4 color = m_occlusionRatio > 0.4f ? green : red;
            //////Vector4 color = new Vector4(m_occlusionRatio, m_occlusionRatio, m_occlusionRatio, 1);
            //MyDebugDraw.DrawAABB(ref  m_occlusionBox, ref color, 1, false);
        }

        public void IssueOcclusionQueries()
        {
            if (!EnableLightGlares)
            {
                return;
            }

            System.Diagnostics.Debug.Assert(QuerySize > 0);

            Vector3D position = this.Position;
            Vector3D cameraToLight = MyRenderCamera.Position - position;
            var distance = cameraToLight.Length();
            const float maxDistance = MyRenderConstants.MAX_GPU_OCCLUSION_QUERY_DISTANCE;

            bool canBeDiscardedIfTooFar = Type != MyGlareTypeEnum.Distant;
            if (canBeDiscardedIfTooFar && distance > maxDistance)
            {
                return;
            }

            if (UseOcclusionQuery)
            {
                //float querySizeMultiplier = distance < maxDistance ? 1 : distance / maxDistance;
                float querySizeMultiplier = 0.2f;

                bool isFar = distance > maxDistance;

                // Occlusion is calculated only when closer than 200m, further visibility is handled by depth test
                if (!isFar)
                {
                    MyRender.GetRenderProfiler().StartProfilingBlock("Light glare update occlusion");
                    UpdateOcclusion(querySizeMultiplier * QuerySize);
                    MyRender.GetRenderProfiler().EndProfilingBlock();
                }
            }
        }

        private void DrawNormalGlare(float distance, float maxDistance, Vector3D position, Vector3 cameraToLight, string material)
        {
            if (m_occlusionRatio <= MyMathConstants.EPSILON)
                return;

            var intensity = GetIntensity();

            float alpha = m_occlusionRatio * intensity;

            const float minGlareRadius = 0.2f;
            const float maxGlareRadius = 10;
            float radius = MathHelper.Clamp(m_parent.Range * 20, minGlareRadius, maxGlareRadius);

            float drawingRadius = radius * Size;

            if (Type == MyGlareTypeEnum.Directional)
            {
                //cameraToLight = (1.0f / distance) * cameraToLight;
                float dot = Vector3.Dot(cameraToLight, m_parent.ReflectorDirection);
                //alpha *= MathHelper.Min(1, 6 * dot);
                alpha *= dot;
            }

            if (alpha <= MyMathConstants.EPSILON)
                return;

            if (distance > maxDistance * .5f)
            {
                // distance falloff
                float falloff = (distance - .5f * maxDistance) / (.5f * maxDistance);
                falloff = 1 - falloff;
                if (falloff < 0)
                    falloff = 0;
                drawingRadius *= falloff;
                alpha *= falloff;
            }

            if (drawingRadius <= float.Epsilon)
                return;

            //drawingRadius *= 0.1f * distance;
           // if (drawingRadius < radius)
             //   drawingRadius = radius;

            var color = m_parent.Color;
            color.A = 0;
            //alpha = 0;

            MyTransparentGeometry.AddBillboardOriented(
                material, color * alpha, position,
                MyRenderCamera.LeftVector, MyRenderCamera.UpVector, drawingRadius);
        }

        private void DrawVolumetricGlare(float distance, Vector3D position)
        {
            var intensity = GetIntensity();

            float alpha = m_occlusionRatio * intensity;

            if (alpha < MyMathConstants.EPSILON)
                return;

            const int minGlareRadius = 5;
            const int maxGlareRadius = 150;
            float radius = MathHelper.Clamp(m_parent.Range * distance / 100.0f, minGlareRadius, maxGlareRadius);

            float drawingRadius = radius;

            var startFadeout = MyRenderCamera.NEAR_PLANE_FOR_BACKGROUND;

            var endFadeout = MyRenderCamera.FAR_PLANE_FOR_BACKGROUND;

            if (distance > startFadeout)
            {
                var fade = (distance - startFadeout) / (endFadeout - startFadeout);
                alpha *= (1 - fade);
            }

            if (alpha < MyMathConstants.EPSILON)
                return;

            var color = m_parent.Color;
            color.A = 0;

            var material = (Type == MyGlareTypeEnum.Distant && distance > MyRenderConstants.MAX_GPU_OCCLUSION_QUERY_DISTANCE) ? "LightGlareDistant" : "LightGlare";
            //var material = MyTransparentMaterials.GetMaterial("LightGlare");

            MyTransparentGeometry.AddBillboardOriented(
                material, color * alpha, position,
                MyRenderCamera.LeftVector, MyRenderCamera.UpVector, drawingRadius);
        }

        private float GetIntensity()
        {
            float intensity;

            if (Intensity.HasValue)
            {
                intensity = MathHelper.Clamp(Intensity.Value, 0, 1);
            }
            else
            {
                var maxParentIntensity = 0.5f * MathHelper.Max(m_parent.Intensity, m_parent.ReflectorIntensity);
                intensity = MathHelper.Clamp(maxParentIntensity, 0, 1);
            }
            return intensity;
        }

        private void UpdateOcclusion(float querySize)
        {
            MyRender.GetRenderProfiler().StartProfilingBlock("update occ 1");

            UpdateGpuOcclusion(querySize);
            
            MyRender.GetRenderProfiler().EndProfilingBlock();
        }               

        private void UpdateGpuOcclusion(float querySize)
        {
            m_occlusionBox.Min = this.Position - new Vector3D(querySize);
            m_occlusionBox.Max = this.Position + new Vector3D(querySize);

            switch (m_state)
            {
                case QueryState.IssueOcc:
                    IssueOcclusionQuery(m_occlusionQuery, true);
                    m_state = QueryState.IssueMeasure;
                    break;

                case QueryState.IssueMeasure:
                    IssueOcclusionQuery(m_measurementQuery, false);
                    m_state = QueryState.WaitOcc;
                    break;

                case QueryState.WaitOcc:
                    m_state = QueryState.WaitMeasure;
                    break;

                case QueryState.WaitMeasure:
                    m_state = QueryState.CheckOcc;
                    break;

                case QueryState.CheckOcc:
                    if (m_occlusionQuery.IsComplete)
                    {
                        m_state = QueryState.CheckMeasure;
                    }
                    break;

                case QueryState.CheckMeasure:
                    if (m_measurementQuery.IsComplete)
                    {
                        m_state = QueryState.IssueOcc;
                        m_occlusionRatio = CalcRatio();
                    }
                    break;
            }
        }

        private void IssueOcclusionQuery(MyOcclusionQuery query, bool depthTest)
        {
            MyRender.GetRenderProfiler().StartProfilingBlock("Issue query");

            BlendState previousBlendState = BlendState.Current; ;
            MyStateObjects.DisabledColorChannels_BlendState.Apply();
            RasterizerState.CullNone.Apply();
            DepthStencilState.None.Apply();

            query.Begin();

            //generate and draw bounding box of our renderCell in occlusion query 
            MyDebugDraw.DrawOcclusionBoundingBox(m_occlusionBox, 1.0f, depthTest);

            previousBlendState.Apply();

            query.End();

            MyRender.GetRenderProfiler().EndProfilingBlock();
        }

        private float CalcRatio()
        {
            float measPixels = m_measurementQuery.PixelCount;
            if (measPixels <= 0) measPixels = 1;

            return Math.Min(m_occlusionQuery.PixelCount / measPixels, 1);
        }
    }
}
