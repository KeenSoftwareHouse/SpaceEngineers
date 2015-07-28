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
        internal Matrix LocalToProjection;
        internal Vector3D WorldCameraOffsetPosition;

        internal Matrix CurrentLocalToProjection { get { return MatrixD.CreateTranslation(MyEnvironment.CameraPosition - WorldCameraOffsetPosition) * (MatrixD)LocalToProjection; } }
    }

    class MyLightsCameraDistanceComparer : IComparer<LightId> {

        public int Compare(LightId x, LightId y)
        {
            return x.ViewerDistanceSquared.CompareTo(y.ViewerDistanceSquared);
        }
    }

    class MyShadows : MyImmediateRC
    {
        const int MAX_SPOTLIGHT_SHADOWCASTERS = 20;
        const bool VisualizeDebug = false;

        internal struct MyShadowmapQuery
        {
            internal DepthStencilView DepthBuffer;
            internal MyViewport Viewport;
            internal MyProjectionInfo ProjectionInfo;
            internal Vector3 ProjectionDir;
            internal float ProjectionFactor;
            internal MyFrustumEnum QueryType;

            internal HashSet<uint> IgnoredEntities;
        }

        internal static RwTexId m_cascadeShadowmapArray = RwTexId.NULL;
        internal static RwTexId m_cascadeShadowmapBackup = RwTexId.NULL;
        internal static ConstantsBufferId m_csmConstants;
        static int m_cascadeResolution;
        static int m_cascadesNum;
        
        internal static float[] m_splitDepth;
        internal static Vector4[] m_cascadeScale = new Vector4[4];
        internal static List<MyShadowmapQuery> m_shadowmapQueries = new List<MyShadowmapQuery>();
        internal static MyProjectionInfo [] m_cascadeInfo = new MyProjectionInfo[8];

        internal static List<MyShadowmapQuery> ShadowmapList { get { return m_shadowmapQueries; } }
        internal static Vector3[] m_cornersCS;

        static InputLayoutId m_inputLayout;
        static VertexShaderId m_markVS;
        static PixelShaderId m_markPS;

        static MyLightsCameraDistanceComparer m_spotlightCastersComparer = new MyLightsCameraDistanceComparer();

        internal static void ResizeCascades()
        {
            if (m_cascadeShadowmapArray != RwTexId.NULL)
            {
                MyRwTextures.Destroy(m_cascadeShadowmapArray);
                MyRwTextures.Destroy(m_cascadeShadowmapBackup);
            }

            m_cascadeResolution = MyRender11.m_renderSettings.ShadowQuality.Resolution();

            m_cascadeShadowmapArray = MyRwTextures.CreateShadowmapArray(m_cascadeResolution, m_cascadeResolution,
                m_cascadesNum, Format.R24G8_Typeless, Format.D24_UNorm_S8_UInt, Format.R24_UNorm_X8_Typeless, "cascades shadowmaps");
            m_cascadeShadowmapBackup = MyRwTextures.CreateShadowmapArray(m_cascadeResolution, m_cascadeResolution,
                m_cascadesNum, Format.R24G8_Typeless, Format.D24_UNorm_S8_UInt, Format.R24_UNorm_X8_Typeless, "cascades shadowmaps backup");
        }

        internal unsafe static void Init()
        {
            //m_spotlightShadowmapPool = new MyShadowmapArray(256, 256, 4, Format.R16_Typeless, Format.D16_UNorm, Format.R16_Float);
            //m_spotlightShadowmapPool.SetDebugName("spotlight shadowmaps pool");

            m_cascadesNum = 4;
            m_splitDepth = new float[m_cascadesNum + 1];

            m_cascadeResolution = 1024;
            ResizeCascades();

            m_csmConstants = MyHwBuffers.CreateConstantsBuffer((sizeof(Matrix) + sizeof(Vector2)) * 8 + 2 * sizeof(Vector4) );

            m_cascadesBoundingsVertices = MyHwBuffers.CreateVertexBuffer(8 * 4, sizeof(Vector3), BindFlags.VertexBuffer, ResourceUsage.Dynamic);
            InitIB();

            m_cornersCS = new Vector3[8] {
                    new Vector3(-1, -1, 0),
                    new Vector3(-1, 1, 0),
                    new Vector3( 1, 1, 0),
                    new Vector3( 1, -1, 0),

                    new Vector3(-1, -1, 1),
                    new Vector3(-1, 1, 1),
                    new Vector3( 1, 1, 1),
                    new Vector3( 1, -1, 1)
                };

            m_markVS = MyShaders.CreateVs("shape.hlsl", "vs");
            m_markPS  = MyShaders.CreatePs("shape.hlsl", "ps_dummy");
            m_inputLayout = MyShaders.CreateIL(m_markVS.BytecodeId, MyVertexLayouts.GetLayout(MyVertexInputComponentType.POSITION3));
        }

        internal static void ResetShadowmaps()
        {
            m_shadowmapQueries.Clear();

            PrepareSpotlights();
            PrepareCascades();

            for(int i=0; i<m_shadowmapQueries.Count; i++)
            {
                MyRender11.Context.ClearDepthStencilView(m_shadowmapQueries[i].DepthBuffer, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1, 0);
            }
        }

        internal static List<RwTexId> ShadowmapsPool = new List<RwTexId>();

        static void PrepareSpotlights()
        {
            MyLights.SpotlightsBvh.OverlapAllFrustum(ref MyEnvironment.ViewFrustumClippedD, MyLightRendering.VisibleSpotlights);

            MyLightRendering.VisibleSpotlights.Sort(m_spotlightCastersComparer);
            while (MyLightRendering.VisibleSpotlights.Count > MAX_SPOTLIGHT_SHADOWCASTERS)
            {
                MyLightRendering.VisibleSpotlights.RemoveAtFast(MyLightRendering.VisibleSpotlights.Count - 1);
            }

            MyArrayHelpers.Reserve(ref MyLightRendering.Spotlights, MyLightRendering.VisibleSpotlights.Count);

            int index = 0;
            int casterIndex = 0;
            foreach (var id in MyLightRendering.VisibleSpotlights)
            {
                MyLights.WriteSpotlightConstants(id, ref MyLightRendering.Spotlights[index]);

                if (id.CastsShadows)
                {
                    var query = new MyShadowmapQuery();

                    if(MyLights.IgnoredEntitites.ContainsKey(id))
                    {
                        query.IgnoredEntities = MyLights.IgnoredEntitites[id];
                    }

                    var shadowMatrix = MatrixD.CreateLookAt(id.Position, id.Position + MyLights.Spotlights[id.Index].Direction, MyLights.Spotlights[id.Index].Up) *
                        MatrixD.CreatePerspectiveFieldOfView((float)(Math.Acos(MyLights.Spotlights[id.Index].ApertureCos) * 2), 1.0f, 0.5f, id.ShadowDistance);

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
                        LocalToProjection = MatrixD.CreateTranslation(MyEnvironment.CameraPosition) * shadowMatrix
                    };

                    MyLightRendering.Spotlights[index].ShadowMatrix = Matrix.Transpose(query.ProjectionInfo.CurrentLocalToProjection * MyMatrixHelpers.ClipspaceToTexture);

                    m_shadowmapQueries.Add(query);
                    casterIndex++;
                }
                index++;
            }
        }

        static Matrix m_oldView;

        static Matrix CreateGlobalMatrix()
        {
            var verticesWS = new Vector3[8];
            Vector3.Transform(m_cornersCS, ref MyEnvironment.InvViewProjection, verticesWS);

            var centroid = verticesWS.Aggregate((x, y) => x + y) / 8f;
            var view = Matrix.CreateLookAt(centroid, centroid - MyEnvironment.DirectionalLightDir, Vector3.UnitY);
            var proj = Matrix.CreateOrthographic(1, 1, 0, 1);

            return view * proj * MyMatrixHelpers.ClipspaceToTexture;
        }

        static Vector3[] CascadeLightDirection = new Vector3[4];
        static int[] FramesSinceUpdate = new int[4];

        static void PrepareCascades()
        {
            MyImmediateRC.RC.Context.CopyResource(m_cascadeShadowmapArray.Resource, m_cascadeShadowmapBackup.Resource);

            bool stabilize = true;

            for (int i = 0; i < 4; i++)
            {
                ++FramesSinceUpdate[i];
            }

            CascadeLightDirection[0] = MyEnvironment.DirectionalLightDir;
            CascadeLightDirection[1] = MyEnvironment.DirectionalLightDir;

            const float DirectionDifferenceThreshold = 0.02f;


            if (FramesSinceUpdate[2] > 180 || MyEnvironment.DirectionalLightDir.Dot(CascadeLightDirection[2]) < (1 - DirectionDifferenceThreshold))
            {
                FramesSinceUpdate[2] = 0;
                CascadeLightDirection[2] = MyEnvironment.DirectionalLightDir;    
            }
            if (FramesSinceUpdate[3] > 180 || MyEnvironment.DirectionalLightDir.Dot(CascadeLightDirection[3]) < (1 - DirectionDifferenceThreshold))
            {
                FramesSinceUpdate[3] = 0;
                CascadeLightDirection[3] = MyEnvironment.DirectionalLightDir;
            }

            var globalMatrix = CreateGlobalMatrix();

            Matrix[] cascadesMatrices = new Matrix[8];

            var cascadeFrozen = MyRender11.Settings.FreezeCascade.Any(x => x == true);
            if (!cascadeFrozen)
            {
                m_oldView = MyEnvironment.View;
            }

            float cascadesNearClip = 1f;
            float cascadesFarClip = 1000f;
            float backOffset = 100f; // more and fit projection to objects inside
            float shadowmapSize = m_cascadeResolution;

            m_splitDepth[0] = cascadesNearClip;
            m_splitDepth[1] = MyRender11.Settings.CascadesSplit0;
            m_splitDepth[2] = MyRender11.Settings.CascadesSplit1;
            m_splitDepth[3] = MyRender11.Settings.CascadesSplit2;
            m_splitDepth[4] = MyRender11.Settings.CascadesSplit3;

            float unitWidth = 1 / MyEnvironment.Projection.M11;
            float unitHeight = 1 / MyEnvironment.Projection.M22;
            var vertices = new Vector3[]
            {
                new Vector3( -unitWidth, -unitHeight, -1), 
                new Vector3( -unitWidth, unitHeight, -1), 
                new Vector3( unitWidth, unitHeight, -1), 
                new Vector3( unitWidth, -unitHeight, -1), 
            };
            var frustumVerticesWS = new Vector3[8];

            for (int c = 0; c < m_cascadesNum; c++)
            {
                for (int i = 0; i < 4; i++) {
                    frustumVerticesWS[i] = vertices[i] * m_splitDepth[c];
                    frustumVerticesWS[i + 4] = vertices[i] * m_splitDepth[c + 1];
                }

                if (MyRender11.Settings.FreezeCascade[c])
                {
                    // draw cascade bounding primtiive
                    if (VisualizeDebug)
                    { 
                        var invView = Matrix.Invert(m_oldView);
                        Vector3.Transform(frustumVerticesWS, ref invView, frustumVerticesWS);
                        var batch = MyLinesRenderer.CreateBatch();
                        batch.Add6FacedConvex(frustumVerticesWS, Color.Blue);

                        var bs = BoundingSphere.CreateFromPoints(frustumVerticesWS);
                        var bb = BoundingBox.CreateFromSphere(bs);
                        batch.AddBoundingBox(bb, Color.OrangeRed);

                        batch.Commit();
                    }

                    continue;
                }


                /*
                 * Cascades update scheme:
                 * 0: 1 1 1 1
                 * 1: 1 0 1 0
                 * 2: 0 1 0 0
                 * 3: 0 0 0 1
                 */

                bool skipCascade1 = c == 1 && (MyCommon.FrameCounter % 2) != 0;
                bool skipCascade2 = c == 2 && (MyCommon.FrameCounter % 4) != 1;
                bool skipCascade3 = c == 3 && (MyCommon.FrameCounter % 4) != 3;
                // 
                if (skipCascade1 || skipCascade2 || skipCascade3)
                {
                    if (!MyRender11.Settings.UpdateCascadesEveryFrame)
                    { 
                        continue;
                    }
                }
                
                Vector3.Transform(frustumVerticesWS, ref MyEnvironment.InvView, frustumVerticesWS);

                var bSphere = BoundingSphere.CreateFromPoints(frustumVerticesWS);
                if (stabilize) 
                { 
                    bSphere.Center = bSphere.Center.Round();
                    bSphere.Radius = (float)Math.Ceiling(bSphere.Radius);
                }

                var offset = bSphere.Radius + cascadesNearClip + backOffset;
                var shadowCameraPosWS = bSphere.Center + CascadeLightDirection[c] * (bSphere.Radius + cascadesNearClip);

                var lightView = VRageMath.Matrix.CreateLookAt(shadowCameraPosWS, shadowCameraPosWS - CascadeLightDirection[c], Math.Abs(Vector3.UnitY.Dot(CascadeLightDirection[c])) < 0.99f ? Vector3.UnitY : Vector3.UnitX);

                Vector3 vMin = new Vector3(-bSphere.Radius, -bSphere.Radius, cascadesNearClip);
                Vector3 vMax = new Vector3(bSphere.Radius, bSphere.Radius, offset + bSphere.Radius);

                var cascadeProjection = Matrix.CreateOrthographicOffCenter(vMin.X, vMax.X, vMin.Y, vMax.Y, vMax.Z, vMin.Z);
                cascadesMatrices[c] = lightView * cascadeProjection;
                
                var transformed = Vector3.Transform(Vector3.Zero, ref cascadesMatrices[c]) * shadowmapSize / 2;
                var smOffset = (transformed.Round() - transformed) * 2 / shadowmapSize;

                // stabilize 1st cascade only
                if (stabilize)
                {
                    cascadeProjection.M41 += smOffset.X;
                    cascadeProjection.M42 += smOffset.Y;
                    cascadesMatrices[c] = lightView * cascadeProjection;
                }

                var inverseCascadeMatrix = Matrix.Invert(cascadesMatrices[c]);
                var corner0 = Vector3.Transform(Vector3.Transform(new Vector3(-1, -1, 0), inverseCascadeMatrix), globalMatrix);
                var corner1 = Vector3.Transform(Vector3.Transform(new Vector3(1, 1, 1), inverseCascadeMatrix), globalMatrix);

                var d = corner1 - corner0;

                var cascadeScale = 1f / (corner1 - corner0);
                m_cascadeScale[c] = new Vector4(cascadeScale, 0);

                var query = new MyShadowmapQuery();
                query.DepthBuffer = m_cascadeShadowmapArray.SubresourceDsv(c);
                query.Viewport = new MyViewport(shadowmapSize, shadowmapSize);

                m_cascadeInfo[c].WorldCameraOffsetPosition = MyEnvironment.CameraPosition;
                m_cascadeInfo[c].WorldToProjection = cascadesMatrices[c];
                // todo: skip translation, recalculate matrix in local space, keep world space matrix only for bounding frustum
                m_cascadeInfo[c].LocalToProjection = Matrix.CreateTranslation(MyEnvironment.CameraPosition) * cascadesMatrices[c];

                query.ProjectionInfo = m_cascadeInfo[c];
                query.ProjectionDir = CascadeLightDirection[c];
                query.ProjectionFactor = shadowmapSize * shadowmapSize / (bSphere.Radius * bSphere.Radius * 4);

                if (c == 0)
                    query.QueryType = MyFrustumEnum.Cascade0;
                if (c == 1)
                    query.QueryType = MyFrustumEnum.Cascade1;
                if (c == 2)
                    query.QueryType = MyFrustumEnum.Cascade2;
                if (c == 3)
                    query.QueryType = MyFrustumEnum.Cascade3;
                
                m_shadowmapQueries.Add(query);
            }

            if (true)
            {
                var verticesWS = new Vector3[8];

                var batch = MyLinesRenderer.CreateBatch();

                var cascadeColor = new[]
                    {
                        Color.Red,
                        Color.Green,
                        Color.Blue,
                        Color.Yellow
                    };

                for (int c = 0; c < m_cascadesNum; c++)
                {
                    if (MyRender11.Settings.FreezeCascade[c])
                    {
                        if (VisualizeDebug)
                        {
                            var inverseViewProj = Matrix.Invert(cascadesMatrices[c]);
                            Vector3.Transform(m_cornersCS, ref inverseViewProj, verticesWS);

                            for (int i = 0; i < verticesWS.Length; i++ )
                            {
                                verticesWS[i] += MyEnvironment.CameraPosition;
                            }

                            MyPrimitivesRenderer.Draw6FacedConvex(verticesWS, cascadeColor[c], 0.2f);

                            batch.Add6FacedConvex(verticesWS, Color.Pink);
                        }
                    }
                }

                batch.Commit();
            }

            var mapping = MyMapping.MapDiscard(m_csmConstants);
            for (int c = 0; c < m_cascadesNum; c++) {
                mapping.stream.Write(Matrix.Transpose(m_cascadeInfo[c].CurrentLocalToProjection * MyMatrixHelpers.ClipspaceToTexture));
            }
            for (int i = m_cascadesNum; i < 8; i++)
                mapping.stream.Write(Matrix.Zero);

            for (int i = 0; i < m_splitDepth.Length; i++)
                mapping.stream.Write(m_splitDepth[i]);
            for (int i = m_splitDepth.Length; i < 8; i++)
                mapping.stream.Write(0.0f);

            for (int i = 0; i < 4; i++)
                mapping.stream.Write(m_cascadeScale[i] / m_cascadeScale[0]);
            
            mapping.Unmap();
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

            var verticesCS = new Vector3[8] {
                    new Vector3(-1, -1, 0),
                    new Vector3(-1, 1, 0),
                    new Vector3( 1, 1, 0),
                    new Vector3( 1, -1, 0),

                    new Vector3(-1, -1, 1),
                    new Vector3(-1, 1, 1),
                    new Vector3( 1, 1, 1),
                    new Vector3( 1, -1, 1)
                };
            var verticesLS = new Vector3[8];

            var mapping = MyMapping.MapDiscard(m_cascadesBoundingsVertices.Buffer);
            for (int c = 0; c < m_cascadesNum; c++)
            {
                var inverseViewProj = Matrix.Invert(m_cascadeInfo[c].CurrentLocalToProjection);
                Vector3.Transform(verticesCS, ref inverseViewProj, verticesLS);

                fixed (Vector3* V = verticesLS)
                {
                    mapping.stream.Write(new IntPtr(V), 0, 8 * sizeof(Vector3));
                }
            }
            mapping.Unmap();

            for(int i=0; i<m_cascadesNum; i++)
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
