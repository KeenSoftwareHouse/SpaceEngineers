using VRageMath;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX.Direct3D11;
using VRageRender.Resources;
using SharpDX.DXGI;
using System;

using SharpDX.Direct3D;

namespace VRageRender
{                     
    struct MyViewTransformation
    {
        internal Matrix View3x3;
        internal Vector3D CameraPosition;
    }

    partial class MyRender11
    {
        internal static bool UseComplementaryDepthBuffer = true;
        internal static float DepthClearValue { get { return UseComplementaryDepthBuffer ? 0 : 1; } }

        internal static string GlobalShaderHeader = "";
    }

    struct MyProjectionInfo
    {
        internal MatrixD WorldToProjection;
        internal MatrixD LocalToProjection;
        internal Vector3D WorldCameraOffsetPosition;

        internal MatrixD CurrentLocalToProjection { get { return MatrixD.CreateTranslation(MyEnvironment.CameraPosition - WorldCameraOffsetPosition) * LocalToProjection; } }
    }

    class MyLightsCameraDistanceComparer : IComparer<LightId> {

        public int Compare(LightId x, LightId y)
        {
            return x.ViewerDistanceSquared.CompareTo(y.ViewerDistanceSquared);
        }
    }

    class MyShadows : MyImmediateRC
    {
        const int MAX_SPOTLIGHT_SHADOWCASTERS = 4;
        const bool VisualizeDebug = false;

        internal struct MyShadowmapQuery
        {
            internal DepthStencilView DepthBuffer;
            internal MyViewport Viewport;
            internal MyProjectionInfo ProjectionInfo;
            internal Vector3 ProjectionDir;
            internal float ProjectionFactor;
            internal MyFrustumEnum QueryType;
            internal int CascadeIndex;

            internal HashSet<uint> IgnoredEntities;
        }

        internal static RwTexId m_cascadeShadowmapArray = RwTexId.NULL;
        internal static RwTexId m_cascadeShadowmapBackup = RwTexId.NULL;
        internal static ConstantsBufferId m_csmConstants;

        private static int m_initializedShadowCascadesCount;
        internal static float[] ShadowCascadeSplitDepths;
        internal static Vector4[] ShadowCascadeScales;
        private static Vector3D[] m_shadowCascadeLightDirections;
        private static int[] m_shadowCascadeFramesSinceUpdate;

        internal static List<MyShadowmapQuery> m_shadowmapQueries = new List<MyShadowmapQuery>();
        internal static MyProjectionInfo [] m_cascadeInfo = new MyProjectionInfo[8];

        internal static List<MyShadowmapQuery> ShadowMapQueries { get { return m_shadowmapQueries; } }
        internal static Vector3D[] m_cornersCS;

        static InputLayoutId m_inputLayout;
        static VertexShaderId m_markVS;
        static PixelShaderId m_markPS;

        static MyLightsCameraDistanceComparer m_spotlightCastersComparer = new MyLightsCameraDistanceComparer();

        internal unsafe static void Init()
        {
            ResetCascades();

            m_csmConstants = MyHwBuffers.CreateConstantsBuffer((sizeof(Matrix) + sizeof(Vector2)) * 8 + 2 * sizeof(Vector4) );

            InitIB();

            m_cornersCS = new Vector3D[8] {
                    new Vector3D(-1, -1, 0),
                    new Vector3D(-1, 1, 0),
                    new Vector3D( 1, 1, 0),
                    new Vector3D( 1, -1, 0),

                    new Vector3D(-1, -1, 1),
                    new Vector3D(-1, 1, 1),
                    new Vector3D( 1, 1, 1),
                    new Vector3D( 1, -1, 1)
                };

            m_markVS = MyShaders.CreateVs("shape.hlsl", "vs");
            m_markPS  = MyShaders.CreatePs("shape.hlsl", "ps_dummy");
            m_inputLayout = MyShaders.CreateIL(m_markVS.BytecodeId, MyVertexLayouts.GetLayout(MyVertexInputComponentType.POSITION3));
        }

        internal unsafe static void ResetCascades()
        {
            m_initializedShadowCascadesCount = MyRenderProxy.Settings.ShadowCascadeCount;

            ShadowCascadeSplitDepths = new float[m_initializedShadowCascadesCount + 1];
            ShadowCascadeScales = new Vector4[m_initializedShadowCascadesCount];
            m_shadowCascadeLightDirections = new Vector3D[m_initializedShadowCascadesCount];
            m_shadowCascadeFramesSinceUpdate = new int[m_initializedShadowCascadesCount];
            m_cascadesBoundingsVertices = MyHwBuffers.CreateVertexBuffer(8 * m_initializedShadowCascadesCount, sizeof(Vector3), BindFlags.VertexBuffer, ResourceUsage.Dynamic);

            ResizeCascades();
        }

        internal static void ResizeCascades()
        {
            if (m_cascadeShadowmapArray != RwTexId.NULL)
            {
                MyRwTextures.Destroy(m_cascadeShadowmapArray);
                MyRwTextures.Destroy(m_cascadeShadowmapBackup);
            }

            var cascadeResolution = MyRender11.m_renderSettings.ShadowQuality.ShadowCascadeResolution();

            m_cascadeShadowmapArray = MyRwTextures.CreateShadowmapArray(cascadeResolution, cascadeResolution,
                m_initializedShadowCascadesCount, Format.R24G8_Typeless, Format.D24_UNorm_S8_UInt, Format.R24_UNorm_X8_Typeless, "Cascades shadowmaps");
            m_cascadeShadowmapBackup = MyRwTextures.CreateShadowmapArray(cascadeResolution, cascadeResolution,
                m_initializedShadowCascadesCount, Format.R24G8_Typeless, Format.D24_UNorm_S8_UInt, Format.R24_UNorm_X8_Typeless, "Cascades shadowmaps backup");
        }

        internal static void PrepareShadowmaps()
        {
            m_shadowmapQueries.Clear();

            PrepareSpotlights();
            PrepareCascades();

            for(int i=0; i < m_shadowmapQueries.Count; i++)
            {
                MyRender11.Context.ClearDepthStencilView(m_shadowmapQueries[i].DepthBuffer, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1, 0);
            }
        }

        internal static List<RwTexId> ShadowmapsPool = new List<RwTexId>();

        static void PrepareSpotlights()
        {
            MyLights.SpotlightsBvh.OverlapAllFrustum(ref MyEnvironment.ViewFrustumClippedD, MyLightRendering.VisibleSpotlights);

            MyLightRendering.VisibleSpotlights.Sort(m_spotlightCastersComparer);
            MyArrayHelpers.Reserve(ref MyLightRendering.Spotlights, MyLightRendering.VisibleSpotlights.Count);

            int index = 0;
            int casterIndex = 0;
            foreach (var id in MyLightRendering.VisibleSpotlights)
            {
                var shadowMatrix = MatrixD.CreateLookAt(id.Position, id.Position + MyLights.Spotlights[id.Index].Direction, MyLights.Spotlights[id.Index].Up) *
                    MatrixD.CreatePerspectiveFieldOfView((float)(Math.Acos(MyLights.Spotlights[id.Index].ApertureCos) * 2), 1.0f, 0.5f, id.ShadowDistance);
                MatrixD localToProjection = MatrixD.CreateTranslation(MyEnvironment.CameraPosition) * shadowMatrix;
                MyLightRendering.Spotlights[index].ShadowMatrix = Matrix.Transpose(localToProjection * MyMatrixHelpers.ClipspaceToTexture);

                if (id.CastsShadows && casterIndex < MAX_SPOTLIGHT_SHADOWCASTERS)
                {
                    var query = new MyShadowmapQuery();
                    MyLights.Lights.Data[id.Index].CastsShadowsThisFrame = true;
                    
                    if(MyLights.IgnoredEntitites.ContainsKey(id))
                    {
                        query.IgnoredEntities = MyLights.IgnoredEntitites[id];
                    }
                
                
                    if(ShadowmapsPool.Count <= casterIndex)
                    {
                        ShadowmapsPool.Add(MyRwTextures.CreateShadowmap(512, 512));
                    }

                    query.DepthBuffer = ShadowmapsPool[casterIndex].Dsv;
                    query.Viewport = new MyViewport(512, 512);
                    query.QueryType = MyFrustumEnum.ShadowProjection;
                    query.ProjectionInfo = new MyProjectionInfo
                    {
                        WorldCameraOffsetPosition = MyEnvironment.CameraPosition,
                        WorldToProjection = shadowMatrix,
                        LocalToProjection = localToProjection
                    };
                    m_shadowmapQueries.Add(query);
                    casterIndex++;
                }
                else
                {
                    MyLights.Lights.Data[id.Index].CastsShadowsThisFrame = false;
                }
                MyLights.WriteSpotlightConstants(id, ref MyLightRendering.Spotlights[index]);

                index++;
            }
        }

        static Matrix m_oldView;

        static MatrixD CreateGlobalMatrix()
        {
            var verticesWorldSpace = new Vector3D[8];
            MatrixD invView = MyEnvironment.InvViewProjection;
            Vector3D.Transform(m_cornersCS, ref invView, verticesWorldSpace);

            var centroid = verticesWorldSpace.Aggregate((x, y) => x + y) / 8f;
            var view = MatrixD.CreateLookAt(centroid, centroid - MyEnvironment.DirectionalLightDir, Vector3D.UnitY);
            var proj = MatrixD.CreateOrthographic(1, 1, 0, 1);

            return view * proj * MyMatrixHelpers.ClipspaceToTexture;
        }

        static void PrepareCascades()
        {
			MyImmediateRC.RC.BeginProfilingBlock("PrepareCascades");
            MyImmediateRC.RC.Context.CopyResource(m_cascadeShadowmapArray.Resource, m_cascadeShadowmapBackup.Resource);

            bool stabilize = true;
            const float DirectionDifferenceThreshold = 0.02f;

            for (int cascadeIndex = 0; cascadeIndex < m_initializedShadowCascadesCount; ++cascadeIndex)
            {
                ++m_shadowCascadeFramesSinceUpdate[cascadeIndex];

                if( m_shadowCascadeFramesSinceUpdate[cascadeIndex] > cascadeIndex*60 ||
                    MyEnvironment.DirectionalLightDir.Dot(m_shadowCascadeLightDirections[cascadeIndex]) < (1 - DirectionDifferenceThreshold))
                {
                    m_shadowCascadeLightDirections[cascadeIndex] = MyEnvironment.DirectionalLightDir;
                    m_shadowCascadeFramesSinceUpdate[cascadeIndex] = 0;
                }
            }

            var globalMatrix = CreateGlobalMatrix();

            MatrixD[] cascadesMatrices = new MatrixD[8];

            var cascadeFrozen = MyRender11.Settings.ShadowCascadeFrozen.Any(x => x == true);
            if (!cascadeFrozen)
                m_oldView = MyEnvironment.View;

            float cascadesNearClip = 1f;

			float cascadesFarClip = MyRender11.RenderSettings.ShadowQuality.ShadowCascadeSplit(m_initializedShadowCascadesCount);
            float backOffset = MyRender11.RenderSettings.ShadowQuality.BackOffset();
            float shadowmapSize = MyRender11.RenderSettings.ShadowQuality.ShadowCascadeResolution();

			var oldCascadeSplit = 0.0f;
			bool useFarShadows = MyRenderProxy.Settings.FarShadowDistanceOverride > MyRender11.Settings.ShadowCascadeMaxDistance;
			if (useFarShadows)
			{
				oldCascadeSplit = MyRender11.Settings.ShadowCascadeMaxDistance;
              
				MyRender11.Settings.ShadowCascadeMaxDistance = MyRenderProxy.Settings.FarShadowDistanceOverride;
			}

			for (int cascadeIndex = 0; cascadeIndex < ShadowCascadeSplitDepths.Length; ++cascadeIndex)
                ShadowCascadeSplitDepths[cascadeIndex] = MyRender11.RenderSettings.ShadowQuality.ShadowCascadeSplit(cascadeIndex);
            
			if (useFarShadows)
				MyRender11.Settings.ShadowCascadeMaxDistance = oldCascadeSplit;

            double unitWidth = 1.0 / MyEnvironment.Projection.M11;
			double unitHeight = 1.0 / MyEnvironment.Projection.M22;
            var vertices = new Vector3D[]
            {
                new Vector3D( -unitWidth, -unitHeight, -1), 
                new Vector3D( -unitWidth, unitHeight, -1), 
                new Vector3D( unitWidth, unitHeight, -1), 
                new Vector3D( unitWidth, -unitHeight, -1), 
            };
            var frustumVerticesWS = new Vector3D[8];

            for (int cascadeIndex = 0; cascadeIndex < m_initializedShadowCascadesCount; ++cascadeIndex)
            {
                for (int i = 0; i < 4; i++) {
                    frustumVerticesWS[i] = vertices[i] * ShadowCascadeSplitDepths[cascadeIndex];
                    frustumVerticesWS[i + 4] = vertices[i] * ShadowCascadeSplitDepths[cascadeIndex + 1];
                }

                if (MyRender11.Settings.ShadowCascadeFrozen[cascadeIndex])
                {
                    // draw cascade bounding primtiive
                    if (VisualizeDebug)
                    {
                        var oldInvView = MatrixD.Invert(m_oldView);
                        Vector3D.Transform(frustumVerticesWS, ref oldInvView, frustumVerticesWS);
                        
                        var verticesF = new Vector3[8];
                        for (int i = 0; i < 8; i++)
                        {
                            verticesF[i] = frustumVerticesWS[i];
                        }
                        var batch = MyLinesRenderer.CreateBatch();
                        batch.Add6FacedConvex(verticesF, Color.Blue);

                        var bs = BoundingSphere.CreateFromPoints(verticesF);
                        var bb = BoundingBox.CreateFromSphere(bs);
                        batch.AddBoundingBox(bb, Color.OrangeRed);

                        batch.Commit();
                    }

                    continue;
                }


                /*
                 * Cascades update scheme:
                 *    1 2 3 4 5 6 7
                 * 0: 1 1 1 1 1 1 1
                 * 1: 1 0 1 0 1 0 1
                 * 2: 0 1 0 0 1 0 0
                 * 3: 0 0 0 1 0 0 0
                 * 4: 0 0 0 0 0 1 0
                 * 5: 0 0 0 0 0 0 1
                 */
                
                bool skipCascade = false;  // TODO: properly
                bool skipCascade1 = cascadeIndex == 1 && (MyCommon.FrameCounter % 2) != 0;
                bool skipCascade2 = cascadeIndex == 2 && (MyCommon.FrameCounter % 4) != 1;
                bool skipCascade3 = cascadeIndex == 3 && (MyCommon.FrameCounter % 4) != 3;
                bool skipCascade4 = cascadeIndex == 4 && (MyCommon.FrameCounter % 8) != 5;
                bool skipCascade5 = cascadeIndex == 5 && (MyCommon.FrameCounter % 8) != 7;
                skipCascade = skipCascade1 || skipCascade2 || skipCascade3 || skipCascade4 || skipCascade5;
                // 
                if (skipCascade && !MyRender11.Settings.UpdateCascadesEveryFrame)
                    continue;

                MatrixD invView = MyEnvironment.InvView;
                Vector3D.Transform(frustumVerticesWS, ref invView, frustumVerticesWS);

                var bSphere = BoundingSphereD.CreateFromPoints((IEnumerable<Vector3D>)frustumVerticesWS);
                if (stabilize) 
                { 
                    bSphere.Center = bSphere.Center.Round();
                    bSphere.Radius = Math.Ceiling(bSphere.Radius);
                }

                var shadowCameraPosWS = bSphere.Center + m_shadowCascadeLightDirections[cascadeIndex] * (bSphere.Radius + cascadesNearClip);

                var lightView = VRageMath.MatrixD.CreateLookAt(shadowCameraPosWS, shadowCameraPosWS - m_shadowCascadeLightDirections[cascadeIndex], Math.Abs(Vector3.UnitY.Dot(m_shadowCascadeLightDirections[cascadeIndex])) < 0.99f ? Vector3.UnitY : Vector3.UnitX);
				var longestShadow = MyRenderProxy.Settings.LongShadowFactor;
                var offset = bSphere.Radius + cascadesNearClip + backOffset + (longestShadow < 0 ? 0.0 : longestShadow);

                Vector3D vMin = new Vector3D(-bSphere.Radius, -bSphere.Radius, cascadesNearClip);
                Vector3D vMax = new Vector3D(bSphere.Radius, bSphere.Radius, offset + bSphere.Radius);

                var cascadeProjection = MatrixD.CreateOrthographicOffCenter(vMin.X, vMax.X, vMin.Y, vMax.Y, vMax.Z, vMin.Z);
                cascadesMatrices[cascadeIndex] = lightView * cascadeProjection;
                
                var transformed = Vector3D.Transform(Vector3D.Zero, cascadesMatrices[cascadeIndex]) * shadowmapSize / 2;
                var smOffset = (transformed.Round() - transformed) * 2 / shadowmapSize;

                // stabilize 1st cascade only
                if (stabilize)
                {
                    cascadeProjection.M41 += smOffset.X;
                    cascadeProjection.M42 += smOffset.Y;
                    cascadesMatrices[cascadeIndex] = lightView * cascadeProjection;
                }

                var inverseCascadeMatrix = MatrixD.Invert(cascadesMatrices[cascadeIndex]);
                var corner0 = Vector3D.Transform(Vector3D.Transform(new Vector3D(-1, -1, 0), inverseCascadeMatrix), globalMatrix);
                var corner1 = Vector3D.Transform(Vector3D.Transform(new Vector3D(1, 1, 1), inverseCascadeMatrix), globalMatrix);

                var d = corner1 - corner0;

                var cascadeScale = 1f / (corner1 - corner0);
                ShadowCascadeScales[cascadeIndex] = new Vector4D(cascadeScale, 0);

                var query = new MyShadowmapQuery();
                query.DepthBuffer = m_cascadeShadowmapArray.SubresourceDsv(cascadeIndex);
                query.Viewport = new MyViewport(shadowmapSize, shadowmapSize);

                m_cascadeInfo[cascadeIndex].WorldCameraOffsetPosition = MyEnvironment.CameraPosition;
                m_cascadeInfo[cascadeIndex].WorldToProjection = cascadesMatrices[cascadeIndex];
                // todo: skip translation, recalculate matrix in local space, keep world space matrix only for bounding frustum
                m_cascadeInfo[cascadeIndex].LocalToProjection = Matrix.CreateTranslation(MyEnvironment.CameraPosition) * cascadesMatrices[cascadeIndex];

                query.ProjectionInfo = m_cascadeInfo[cascadeIndex];
                query.ProjectionDir = m_shadowCascadeLightDirections[cascadeIndex];
                query.ProjectionFactor = (float)(shadowmapSize * shadowmapSize / (bSphere.Radius * bSphere.Radius * 4));

                query.QueryType = MyFrustumEnum.ShadowCascade;
                query.CascadeIndex = cascadeIndex;
                
                m_shadowmapQueries.Add(query);
            }

            if (true)
            {
                var verticesWS = new Vector3D[8];

                var batch = MyLinesRenderer.CreateBatch();

                var cascadeColor = new[]
                    {
                        Color.Red,
                        Color.Green,
                        Color.Blue,
                        Color.Yellow
                    };

                for (int c = 0; c < m_initializedShadowCascadesCount; c++)
                {
                    if (MyRender11.Settings.ShadowCascadeFrozen[c])
                    {
                        if (VisualizeDebug)
                        {
                            var inverseViewProj = MatrixD.Invert(cascadesMatrices[c]);
                            Vector3D.Transform(m_cornersCS, ref inverseViewProj, verticesWS);
                        
                            for (int i = 0; i < verticesWS.Length; i++ )
                            {
                                verticesWS[i] += MyEnvironment.CameraPosition;
                            }

                            var verticesF = new Vector3[8];
                            for (int i = 0; i < 8; i++)
                            {
                                verticesF[i] = verticesWS[i];
                            }

                            MyPrimitivesRenderer.Draw6FacedConvex(verticesF, cascadeColor[c], 0.2f);
                            batch.Add6FacedConvex(verticesF, Color.Pink);
                        }
                    }
                }

                batch.Commit();
            }

            var mapping = MyMapping.MapDiscard(m_csmConstants);
            for (int c = 0; c < m_initializedShadowCascadesCount; c++) {
                mapping.stream.Write(Matrix.Transpose(m_cascadeInfo[c].CurrentLocalToProjection * MyMatrixHelpers.ClipspaceToTexture));
            }
            for (int i = m_initializedShadowCascadesCount; i < 8; i++)
                mapping.stream.Write(Matrix.Zero);

            for (int i = 0; i < ShadowCascadeSplitDepths.Length; i++)
                mapping.stream.Write(ShadowCascadeSplitDepths[i]);
            for (int i = ShadowCascadeSplitDepths.Length; i < 8; i++)
                mapping.stream.Write(0.0f);

            for (int i = 0; i < m_initializedShadowCascadesCount; i++)
                mapping.stream.Write(ShadowCascadeScales[i] / ShadowCascadeScales[0]);
            
            mapping.Unmap();

			MyImmediateRC.RC.EndProfilingBlock();
        }

        static IndexBufferId m_cubeIB = IndexBufferId.NULL;
        static VertexBufferId m_cascadesBoundingsVertices;

        //internal static void CreateInputLayout(byte[] bytecode)
        //{
        //    m_inputLayout = MyVertexInputLayout.CreateLayout(MyVertexInputLayout.Empty().Append(MyVertexInputComponentType.POSITION3), bytecode);
        //}

        static unsafe void InitIB()
        {
            ushort[] indices = new ushort[]
            {
                // 0 1 2 3
                0, 1, 2, 0, 2, 3,
                // 1 5 6 2
                1, 5, 6, 1, 6, 2,
                // 5 4 7 6
                5, 4, 7, 5, 7, 6,
                // 4 0 3 7
                4, 0, 3, 4, 3, 7,
                // 3 2 6 7
                3, 2, 6, 3, 6, 7,
                // 1 0 4 5
                1, 0, 4, 1, 4, 5
            };

            if(m_cubeIB == IndexBufferId.NULL)
            {
                fixed (ushort* I = indices)
                {
                    m_cubeIB = MyHwBuffers.CreateIndexBuffer(indices.Length, Format.R16_UInt, BindFlags.IndexBuffer, ResourceUsage.Immutable, new IntPtr(I));
                }
            }
        }

        internal static void OnDeviceReset()
        {
            if (m_cubeIB != IndexBufferId.NULL)
            {
                MyHwBuffers.Destroy(m_cubeIB);
                m_cubeIB = IndexBufferId.NULL;
            }

            InitIB();
        }

        internal static unsafe void MarkCascadesInStencil()
        {
            //RC.SetRS(MyRasterizerState.CullCW);

            RC.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            RC.SetVB(0, m_cascadesBoundingsVertices.Buffer, m_cascadesBoundingsVertices.Stride);
            RC.SetIB(m_cubeIB.Buffer, m_cubeIB.Format);
            RC.SetIL(m_inputLayout);
            RC.Context.Rasterizer.SetViewport(0, 0, MyRender11.ViewportResolution.X, MyRender11.ViewportResolution.Y);
            RC.SetCB(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            RC.BindDepthRT(MyGBuffer.Main.DepthStencil, DepthStencilAccess.DepthReadOnly);
            RC.SetVS(m_markVS);
            RC.SetPS(m_markPS);

            var verticesCS = new Vector3D[8] {
                    new Vector3D(-1, -1, 0),
                    new Vector3D(-1, 1, 0),
                    new Vector3D( 1, 1, 0),
                    new Vector3D( 1, -1, 0),

                    new Vector3D(-1, -1, 1),
                    new Vector3D(-1, 1, 1),
                    new Vector3D( 1, 1, 1),
                    new Vector3D( 1, -1, 1)
                };
            var verticesLS = new Vector3D[8];

            var mapping = MyMapping.MapDiscard(m_cascadesBoundingsVertices.Buffer);
            for (int c = 0; c < m_initializedShadowCascadesCount; c++)
            {
                var inverseViewProj = MatrixD.Invert(m_cascadeInfo[c].CurrentLocalToProjection);
                Vector3D.Transform(verticesCS, ref inverseViewProj, verticesLS);
                Vector3[] verticesF = new Vector3[8];
                for (int i = 0; i < 8; i++)
                {
                    verticesF[i] = verticesLS[i];
                }
                fixed (Vector3* V = verticesF)
                {
                    mapping.stream.Write(new IntPtr(V), 0, 8 * sizeof(Vector3));
                }
            }
            mapping.Unmap();

            for (int i = 0; i < m_initializedShadowCascadesCount; i++)
            {
                RC.SetDS(MyDepthStencilState.MarkIfInsideCascade[i], 1 << i);
                // mark ith bit on depth near
                RC.Context.DrawIndexed(36, 0, 8 * i);
            }
            RC.BindDepthRT(null, DepthStencilAccess.DepthReadOnly);

            RC.SetDS(null);
            RC.SetRS(null);
        }
    }
}
