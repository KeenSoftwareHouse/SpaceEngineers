#region Using Statements

using System.Collections.Generic;
using VRageMath;
using System;
using ParallelTasks;
using VRage.Utils;
using VRageRender.Shadows;
using VRageRender.Textures;
using VRageRender.Utils;
using VRageRender.Lights;
using VRageRender.Graphics;
using VRageRender.Effects;

#endregion

namespace VRageRender
{
    class MyRenderLight : MyRenderTransformObject
    {
        LightTypeEnum m_lightType;

        internal Vector3D m_position;
        internal int m_parentID = -1;
        public Vector3D Position
        {
            set
            {
                m_position = value;
            }
            get
            {
                if (m_parentID != -1)
                {
                    MyRenderObject renderObject;
                    if (MyRender.m_renderObjects.TryGetValue((uint)m_parentID, out renderObject))
                    {
                        MyRenderTransformObject transformObject = renderObject as MyRenderTransformObject;
                        MyManualCullableRenderObject cullableObject = renderObject as MyManualCullableRenderObject;
                        if (transformObject != null)
                        {
                            var worldMatrix = transformObject.WorldMatrix;
                            Vector3D result;
                            Vector3D.Transform(ref m_position, ref worldMatrix, out result);
                            return result;
                        }
                        else if (cullableObject != null)
                        {
                            var worldMatrix = cullableObject.WorldMatrix;
                            Vector3D result;
                            Vector3D.Transform(ref m_position, ref worldMatrix, out result);
                            return result;
                        }
                    }
                }
                return m_position;
            }
        }
        internal Vector3D m_positionWithOffset;
        public Vector3D PositionWithOffset
        {
            set
            {
                m_positionWithOffset = value;
            }
            get
            {
                if (m_parentID != -1)
                {
                    MyRenderObject renderObject;
                    if (MyRender.m_renderObjects.TryGetValue((uint)m_parentID, out renderObject))
                    {
                        MyRenderTransformObject transformObject = renderObject as MyRenderTransformObject;
                        MyManualCullableRenderObject cullableObject = renderObject as MyManualCullableRenderObject;
                        if (transformObject != null)
                        {
                            var worldMatrix = transformObject.WorldMatrix;
                            Vector3D result;
                            Vector3D.Transform(ref m_positionWithOffset, ref worldMatrix, out result);
                            return result;
                        }
                        else if (cullableObject != null)
                        {
                            var worldMatrix = cullableObject.WorldMatrix;
                            Vector3D result;
                            Vector3D.Transform(ref m_positionWithOffset, ref worldMatrix, out result);
                            return result;
                        }
                    }
                }
                
                return m_positionWithOffset;
            }
        }

        internal Vector3D m_reflectorDirection;
        public Vector3D ReflectorDirection
        {
            set
            {
                m_reflectorDirection = value;
            }
            get
            {
                if (m_parentID != -1)
                {
                    MyRenderObject renderObject;
                    if (MyRender.m_renderObjects.TryGetValue((uint)m_parentID, out renderObject))
                    {
                        MyRenderTransformObject transformObject = renderObject as MyRenderTransformObject;
                        MyManualCullableRenderObject cullableObject = renderObject as MyManualCullableRenderObject;
                        if (transformObject != null)
                        {
                            var worldMatrix = transformObject.WorldMatrix;
                            Vector3D result;
                            Vector3D.TransformNormal(ref m_reflectorDirection, ref worldMatrix, out result);
                            return result;
                        }
                        else if (cullableObject != null)
                        {
                            var worldMatrix = cullableObject.WorldMatrix;
                            Vector3D result;
                            Vector3D.TransformNormal(ref m_reflectorDirection, ref worldMatrix, out result);
                            return result;
                        }
                    }
                }
                
                return m_reflectorDirection;
            }
        }

        internal Vector3D m_reflectorUp;
        public Vector3D ReflectorUp
        {
            set
            {
                m_reflectorUp = value;
            }
            get
            {
                if (m_parentID != -1)
                {
                    MyRenderObject renderObject;
                    if (MyRender.m_renderObjects.TryGetValue((uint)m_parentID, out renderObject))
                    {
                        MyRenderTransformObject transformObject = renderObject as MyRenderTransformObject;
                        MyManualCullableRenderObject cullableObject = renderObject as MyManualCullableRenderObject;
                        if (transformObject != null)
                        {
                            var worldMatrix = transformObject.WorldMatrix;
                            Vector3D result;
                            Vector3D.TransformNormal(ref m_reflectorUp, ref worldMatrix, out result);
                            return result;
                        }
                        else if (cullableObject != null)
                        {
                            var worldMatrix = cullableObject.WorldMatrix;
                            Vector3D result;
                            Vector3D.TransformNormal(ref m_reflectorUp, ref worldMatrix, out result);
                            return result;
                        }
                    }
                }

                return m_reflectorUp;
            }
        }

        Color m_color;
        Color m_specularColor = Color.White;
        float m_falloff;
        float m_range;
        float m_intensity;
        bool m_lightOn;        //  If true, we use the light in lighting calculation. Otherwise it's like turned off, but still in the buffer.
        public bool UseInForwardRender = false;

        // Reflector parameters are also parameters for spot light
        public float ReflectorIntensity;
        public bool ReflectorOn = false;
        public float ReflectorConeMaxAngleCos;
        public Color ReflectorColor;
        public float ReflectorRange;
        public float ReflectorFalloff;
        MyTexture2D m_reflectorTexture;
        public float PointLightOffset;
        public float ShadowDistance;


        //Calculated values
        private bool m_spotParamsDirty = true;
        private BoundingBoxD m_spotBoundingBox;
        private MatrixD m_spotWorld;
        private BoundingSphereD m_pointBoundingSphere;

        MyLightGlare m_glare;
        public bool GlareOn;

        public int ShadowMapIndex = -1; //just for debug purposes
        public MyOcclusionQuery SpotQuery;
        public QueryState SpotQueryState = QueryState.IssueOcc;
        public int QueryPixels;

        protected List<uint> m_shadowIgnoreObjects = new List<uint>();

        public MyRenderLight(
           uint id
           )
            : base(id, "RenderLight", MatrixD.Identity, 0)
        {
            LoadContent();
        }

        public void UpdateParameters(
            LightTypeEnum type,
            Vector3D position,
            int parentID,
            float offset,
            Color color,
            Color specularColor,
            float falloff,
            float range,
            float intensity,
            bool lightOn, 
            bool useInForwardRender,
            float reflectorIntensity,
            bool reflectorOn,
            Vector3 reflectorDirection,
            Vector3 reflectorUp,
            float reflectorConeMaxAngleCos,
            Color reflectorColor,
            float reflectorRange,
            float reflectorFalloff,
            string reflectorTexture,
            float shadowDistance,
            bool castShadows,
            bool glareOn,
            MyGlareTypeEnum glareType,
            float glareSize,
            float glareQuerySize,
            float glareIntensity,
            string glareMaterial,
            float glareMaxDistance
            )
        {
            m_lightType = type;
            GlareOn = glareOn;

            Position = position;
            m_parentID = parentID;

            m_color = color;
            m_specularColor = specularColor;
            m_falloff = falloff;
            m_range = range;
            m_intensity = intensity;
            m_lightOn = lightOn;
            UseInForwardRender = useInForwardRender;
            ReflectorIntensity = reflectorIntensity;
            ReflectorOn = reflectorOn;
            ReflectorDirection = reflectorDirection;
            ReflectorUp = reflectorUp;
            ReflectorConeMaxAngleCos = reflectorConeMaxAngleCos;
            ReflectorColor = reflectorColor;
            ReflectorRange = reflectorRange;
            ReflectorFalloff = reflectorFalloff;
            ShadowDistance = shadowDistance;
            CastShadows = castShadows;

            if (Glare != null)
            {
                Glare.GlareMaterial = glareMaterial;
                Glare.Type = glareType;
                Glare.Size = glareSize;
                System.Diagnostics.Debug.Assert(!GlareOn || (GlareOn && glareQuerySize > 0));
                Glare.QuerySize = glareQuerySize;
                Glare.Intensity = glareIntensity == -1 ? (float?)null : glareIntensity;
                Glare.MaxDistance = glareMaxDistance;
            }

            m_pointBoundingSphere.Radius = range;

            m_reflectorTexture = string.IsNullOrEmpty(reflectorTexture) ? null : MyTextureManager.GetTexture<MyTexture2D>(reflectorTexture);
            PointLightOffset = offset;

            UpdatePositionWithOffset();

            UpdateSpotParams();

        }

        void UpdatePositionWithOffset()
        {
			// using local rotation here
            PositionWithOffset = m_position + m_reflectorDirection * m_range * PointLightOffset;
            m_pointBoundingSphere.Center = PositionWithOffset;
        }

        private static void CalculateAABB(ref BoundingBoxD bbox, out float scaleZ, out float scaleXY, ref Vector3D position, ref Vector3D direction, ref Vector3D up, float reflectorConeMaxAngleCos, float reflectorRange)
        {
            scaleZ = 1;
            scaleXY = 1;
            float cosAngle = 1 - reflectorConeMaxAngleCos;
            scaleZ = reflectorRange;
            // Calculate cone side (hypotenuse of triangle)
            float side = reflectorRange / cosAngle;
            // Calculate cone bottom scale (Pythagoras theorem)
            scaleXY = (float)System.Math.Sqrt(side * side - reflectorRange * reflectorRange) * 2;

            if (scaleXY == 0)
                scaleXY = 1;

            up = MyUtils.Normalize(up);
            Vector3 coneSideDirection = Vector3.Cross(up, direction);
            coneSideDirection = MyUtils.Normalize(coneSideDirection);
            Vector3D coneCenter = position + direction * scaleZ;
            Vector3D pt1 = coneCenter + coneSideDirection * scaleXY / 2 + up * scaleXY / 2;
            Vector3D pt2 = coneCenter - coneSideDirection * scaleXY / 2 + up * scaleXY / 2;
            Vector3D pt3 = coneCenter + coneSideDirection * scaleXY / 2 - up * scaleXY / 2;
            Vector3D pt4 = coneCenter - coneSideDirection * scaleXY / 2 - up * scaleXY / 2;

            bbox = BoundingBoxD.CreateInvalid();
            bbox = bbox.Include(ref position);
            //bbox = bbox.Include(ref coneCenter);
            bbox = bbox.Include(ref pt1);
            bbox = bbox.Include(ref pt2);
            bbox = bbox.Include(ref pt3);
            bbox = bbox.Include(ref pt4);
        }

        private void UpdateSpotParams()
        {
            float scaleZ, scaleXY;
            var position = Position;
            var reflectorDirection = ReflectorDirection;
            var reflectorUp = ReflectorUp;
            CalculateAABB(ref m_spotBoundingBox, out scaleZ, out scaleXY, ref position, ref reflectorDirection, ref reflectorUp, ReflectorConeMaxAngleCos, ReflectorRange);
            m_spotWorld = MatrixD.CreateScale((double)scaleXY, (double)scaleXY, (double)scaleZ) * MatrixD.CreateWorld(position, reflectorDirection, reflectorUp);
            m_spotParamsDirty = false;

            UpdateWorldAABB();
        }

        

        public override void UpdateWorldAABB()
        {
            m_localAABB = new BoundingBoxD(new Vector3D(-0.5), new Vector3D(0.5));

            if (IsTypeSpot)
            {
                if (m_spotParamsDirty)
                    UpdateSpotParams();

                m_localAABB = m_spotBoundingBox;
                m_aabb = m_spotBoundingBox;
            }
            else
            if (IsTypePoint || IsTypeHemisphere)
            {
                m_localAABB = BoundingBoxD.CreateFromSphere(m_pointBoundingSphere);
                m_aabb = m_localAABB;
            }

            base.UpdateWorldAABB();
        }


        public override MatrixD WorldMatrix
        {
            set
            {
                m_worldMatrix = MatrixD.Identity;
                Position = value.Translation;
                UpdatePositionWithOffset();
                
                Flags |= MyElementFlag.EF_AABB_DIRTY;
                m_spotParamsDirty = true;
            }
        }

        public override bool Draw()
        {
            if (GlareOn)
                Glare.Draw();

            return true;
        }

        public override void IssueOcclusionQueries()
        {
            base.IssueOcclusionQueries();

           if (GlareOn)
               Glare.IssueOcclusionQueries();

            IssueSpotQuery();
        }

        private void IssueSpotQuery()
        {
            if (((LightType & LightTypeEnum.Spotlight) > 0) && SpotQuery != null && SpotQueryState == QueryState.IssueOcc)
            {
                BlendState previousBlendState = BlendState.Current; ;
                MyStateObjects.DisabledColorChannels_BlendState.Apply();
               // RasterizerState.CullNone.Apply();
               // DepthStencilState.None.Apply();

                MyEffectOcclusionQueryDraw effectOQ = MyRender.GetEffect(MyEffects.OcclusionQueryDrawMRT) as MyEffectOcclusionQueryDraw;
                effectOQ.SetTechnique(MyEffectOcclusionQueryDraw.Technique.DepthTestEnabled);

                MatrixD spotWorld = SpotWorld;
                spotWorld.Translation = SpotWorld.Translation - MyRenderCamera.Position;
                effectOQ.SetWorldMatrix((Matrix)spotWorld);
                effectOQ.SetViewMatrix(MyRenderCamera.ViewMatrixAtZero);
                effectOQ.SetProjectionMatrix(MyRenderCamera.ProjectionMatrix);

                var depthRenderTarget = MyRender.GetRenderTarget(MyRenderTargets.Depth);
                effectOQ.SetDepthRT(depthRenderTarget);
                effectOQ.SetScale(MyRender.GetScaleForViewport(depthRenderTarget));

                SpotQuery.Begin();

                effectOQ.Begin();

                MyDebugDraw.ModelCone.Render();

                effectOQ.End();

                SpotQuery.End();

                SpotQueryState = QueryState.CheckOcc;

                previousBlendState.Apply();
            }

            if (SpotQueryState == QueryState.WaitOcc)
                SpotQueryState = QueryState.IssueOcc;
        }


        #region Properties

        public bool IsTypePoint
        {
            get
            {
                return (LightType & LightTypeEnum.PointLight) != 0 && !IsTypeHemisphere;
            }
        }

        public bool IsTypeHemisphere
        {
            get
            {
                return (LightType & LightTypeEnum.Hemisphere) != 0;
            }
        }

        public bool IsTypeSpot
        {
            get
            {
                return (LightType & LightTypeEnum.Spotlight) != 0;
            }
        }

        public void SetPosition(Vector3D position)
        {
            if (Vector3D.DistanceSquared(Position, position) > 0.0001)
            {
                position.AssertIsValid();
                Position = position;
                m_spotParamsDirty = true;
                UpdatePositionWithOffset();
                m_pointBoundingSphere.Center = PositionWithOffset;
            }
        }

        public Color Color
        {
            get { return m_color; }
            set { m_color = value; }
        }

        public Vector3 SpecularColor
        {
            get { return m_specularColor; }
            set { m_specularColor = value; }
        }

        /// <summary>
        /// Exponential falloff (1 = linear, 2 = quadratic, etc)
        /// </summary>
        public float Falloff
        {
            get { return m_falloff; }
            set { m_falloff = value; }
        }

        public float Range
        {
            get
            {
                System.Diagnostics.Debug.Assert(m_range > 0, "Point light range not set");
                return m_range;
            }
            set
            {
                if (m_range != value)
                {
                    System.Diagnostics.Debug.Assert(value > 0, "Cannot set zero point light range");
                    System.Diagnostics.Debug.Assert(value <= MyLightsConstants.MAX_POINTLIGHT_RADIUS, "Cannot set point light range bigger than MyLightsConstants.MAX_POINTLIGHT_RADIUS");
                    m_range = value;
                    m_pointBoundingSphere.Radius = value;
                }
            }
        }

        public float Intensity
        {
            get { return m_intensity; }
            set { m_intensity = value; }
        }

        public bool LightOn        //  If true, we use the light in lighting calculation. Otherwise it's like turned off, but still in the buffer.
        {
            get { return m_lightOn; }
            set
            {
                if (m_lightOn != value) { m_lightOn = value; /*UpdateLight();*/ }
            }
        }

        public bool PointOn { get; set; }

        public LightTypeEnum LightType
        {
            get { return m_lightType; }
            set { m_lightType = value; }
        }
            
        public MyTexture2D ReflectorTexture
        {
            get { return m_reflectorTexture; }
            set { m_reflectorTexture = value; }
        }
            
        public MyLightGlare Glare { get { return m_glare; } }

        public MatrixD SpotWorld
        {
            get
            {
                if (m_spotParamsDirty)
                {
                    UpdateSpotParams();
                }
                return m_spotWorld;
            }
        }

        public BoundingSphereD PointBoundingSphere { get { return m_pointBoundingSphere; } }

        #endregion


        public override void LoadContent()
        {
            if (m_glare == null)
                m_glare = new MyLightGlare(this);

            m_glare.LoadContent();
            
            SpotQuery = MyOcclusionQueries.Get();
            SpotQueryState = QueryState.IssueOcc;
            
            base.LoadContent();
        }

        public override void UnloadContent()
        {
            m_glare.UnloadContent();

            if (SpotQuery != null)
            {
                MyOcclusionQueries.Return(SpotQuery);
                SpotQuery = null;
            }

            base.UnloadContent();
        }

        public List<uint> ShadowIgnoreObjects
        {
            get { return m_shadowIgnoreObjects; }
        }
        

    }
}
