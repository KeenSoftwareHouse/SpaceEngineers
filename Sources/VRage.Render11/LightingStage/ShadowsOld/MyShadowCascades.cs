using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Render11.Common;
using VRage.Render11.LightingStage.Shadows;
using VRage.Render11.Profiler;
using VRage.Render11.Resources;
using VRageMath;


namespace VRageRender
{
    internal class MyShadowCascades
    {
        internal static MyShadowsSettings Settings = new MyShadowsSettings();
        const bool VISUALIZE_DEBUG = false;

        internal static readonly MyTuple<int, int>[] DynamicShadowCascadeUpdateIntervals = new MyTuple<int, int>[8] {
            MyTuple.Create(1, 0),
            MyTuple.Create(1, 0),
            MyTuple.Create(1, 0),
            MyTuple.Create(1, 0),
            MyTuple.Create(4, 3),
            MyTuple.Create(8, 5),
            MyTuple.Create(8, 6),
            MyTuple.Create(8, 7),};

        internal static readonly MyTuple<int, int>[] VoxelShadowCascadeUpdateIntervals = new MyTuple<int, int>[8] {
            MyTuple.Create(4, 0),
            MyTuple.Create(4, 1),
            MyTuple.Create(8, 0),
            MyTuple.Create(8, 2),
            MyTuple.Create(16, 0),
            MyTuple.Create(16, 2),
            MyTuple.Create(16, 4),
            MyTuple.Create(16, 7),};

        #region Fields
        internal const int MaxShadowCascades = 8;
        private int m_initializedShadowCascadesCount = 0;
        internal float[] ShadowCascadeSplitDepths;
        internal Vector4[] ShadowCascadeScales;

        private Vector3D[] m_shadowCascadeUpdatePositions;
        private const double m_shadowCascadeForceUpdateDistance = 150;

        private Vector3D[] m_shadowCascadeLightDirections;
        private int[] m_shadowCascadeFramesSinceLightUpdate;
        private MyTuple<int, int>[] m_shadowCascadeUpdateIntervals;

        private readonly MyProjectionInfo[] m_cascadeInfo = new MyProjectionInfo[8];

        private MyShadowCascadesPostProcess m_cascadePostProcessor;
        
        private IConstantBuffer m_csmConstants;
        private IDepthArrayTexture m_cascadeShadowmapArray;
        private IDepthArrayTexture m_cascadeShadowmapBackup;

        private static int m_cascadesReferenceCount = 0;
        private static IDepthArrayTexture m_combinedShadowmapArray;

        private Vector3D[] m_cornersCS;
        private readonly Vector3D[] m_frustumVerticesWS = new Vector3D[8];

        private Vector3D[] m_blockOS = new Vector3D[8]
        {
                new Vector3D(-1, -1, 0),
                new Vector3D(-1, 1, 0),
                new Vector3D( 1, 1, 0),
                new Vector3D( 1, -1, 0),

                new Vector3D(-1, -1, 1),
                new Vector3D(-1, 1, 1),
                new Vector3D( 1, 1, 1),
                new Vector3D( 1, -1, 1)
        };
        private Vector3D[] m_tmpDebugFrozen8D = new Vector3D[8];
        private Vector3[] m_tmpDebugFrozen8 = new Vector3[8]; 
        
        private Matrix[] m_debugFrozenMatrixView = new Matrix[8];
        private Color[] m_debugCascadeColor = new[]
        {
                Color.Red,
                Color.Green,
                Color.Blue,
                Color.Yellow,
                Color.Gray,
                Color.Orange,
                Color.Red,
                Color.Green
        };
        #endregion

        #region Properties
        internal IDepthArrayTexture CascadeShadowmapArray { get { return m_cascadeShadowmapArray; } }
        internal IDepthArrayTexture CascadeShadowmapBackup { get { return m_cascadeShadowmapBackup; } }
        internal IConstantBuffer CascadeConstantBuffer { get { return m_csmConstants; } }
        internal MyProjectionInfo[] CascadeInfo { get { return m_cascadeInfo; } }

        internal static IDepthArrayTexture CombineShadowmapArray { get { return m_combinedShadowmapArray; } }

        internal MyShadowCascadesPostProcess PostProcessor { get { return m_cascadePostProcessor; } }
        #endregion

        internal MyShadowCascades(int numberOfCascades, int cascadeResolution)
        {
            Init(numberOfCascades, cascadeResolution);
        }

        private void Init(int numberOfCascades, int cascadeResolution)
        {
            SetNumberOfCascades(numberOfCascades);
            m_initializedShadowCascadesCount = numberOfCascades;

            InitResources(cascadeResolution);

            m_cornersCS = new Vector3D[8]
            {
                    new Vector3D(-1, -1, 0),
                    new Vector3D(-1, 1, 0),
                    new Vector3D( 1, 1, 0),
                    new Vector3D( 1, -1, 0),

                    new Vector3D(-1, -1, 1),
                    new Vector3D(-1, 1, 1),
                    new Vector3D( 1, 1, 1),
                    new Vector3D( 1, -1, 1)
            };

            if(m_cascadePostProcessor == null)
                m_cascadePostProcessor = new MyShadowCascadesPostProcess(numberOfCascades);
            else
                m_cascadePostProcessor.Reset(numberOfCascades);
        }

        internal void Reset(int numberOfCascades, int cascadeResolution)
        {
            UnloadResources();

            Init(numberOfCascades, cascadeResolution);
        }

        private void InitResources(int cascadeResolution)
        {
            InitConstantBuffer();
            InitCascadeTextures(cascadeResolution);
        }

        internal void UnloadResources()
        {
            m_cascadePostProcessor.UnloadResources();
            m_cornersCS = null;
            DestroyConstantBuffer();
            DestroyCascadeTextures();
        }

        private unsafe void InitConstantBuffer()
        {
            DestroyConstantBuffer();
            m_csmConstants = MyManagers.Buffers.CreateConstantBuffer("MyShadowCascades", (sizeof(Matrix) + 2 * sizeof(Vector4) + sizeof(Vector4)) * 8 + sizeof(Vector4), usage: ResourceUsage.Dynamic);
        }

        private void DestroyConstantBuffer()
        {
            MyManagers.Buffers.Dispose(m_csmConstants);
            m_csmConstants = null;
        }

        private void InitCascadeTextures(int cascadeResolution)
        {
            DestroyCascadeTextures();

            MyArrayTextureManager arrayManager = MyManagers.ArrayTextures;
            MyRender11.Log.WriteLine("InitCascadeTextures: " + m_cascadesReferenceCount + " / " + MyScene.SeparateGeometry);
            m_cascadeShadowmapArray = arrayManager.CreateDepthArray("MyShadowCascades.CascadeShadowmapArray",
                cascadeResolution, cascadeResolution, m_initializedShadowCascadesCount, Format.R32_Typeless, Format.R32_Float, Format.D32_Float);
            m_cascadeShadowmapBackup = arrayManager.CreateDepthArray("MyShadowCascades.CascadeShadowmapBackup",
                cascadeResolution, cascadeResolution, m_initializedShadowCascadesCount, Format.R32_Typeless, Format.R32_Float, Format.D32_Float);

            if (m_cascadesReferenceCount == 0 && MyScene.SeparateGeometry)
            {
                MyRender11.Log.WriteLine("InitCascadeTextures m_combinedShadowmapArray");
                m_combinedShadowmapArray = arrayManager.CreateDepthArray("MyShadowCascades.CombinedShadowmapArray",
                    cascadeResolution, cascadeResolution, m_initializedShadowCascadesCount, Format.R32_Typeless, Format.R32_Float, Format.D32_Float);
            }

            ++m_cascadesReferenceCount;
        }

        private void DestroyCascadeTextures()
        {
            MyRender11.Log.WriteLine("DestroyCascadeTextures");
            MyRender11.Log.IncreaseIndent();

            MyArrayTextureManager arrayManager = MyManagers.ArrayTextures;
            arrayManager.DisposeTex(ref m_cascadeShadowmapArray);
            arrayManager.DisposeTex(ref m_cascadeShadowmapBackup);

            m_cascadesReferenceCount = Math.Max(m_cascadesReferenceCount - 1, 0);
            arrayManager.DisposeTex(ref m_combinedShadowmapArray);

            MyRender11.Log.DecreaseIndent();
        }

        private void SetNumberOfCascades(int newCount)
        {
            Array.Resize(ref ShadowCascadeSplitDepths, newCount + 1);
            Array.Resize(ref ShadowCascadeScales, newCount);
            Array.Resize(ref m_shadowCascadeUpdatePositions, newCount);
            Array.Resize(ref m_shadowCascadeLightDirections, newCount);
            Array.Resize(ref m_shadowCascadeFramesSinceLightUpdate, newCount);
            Array.Resize(ref m_shadowCascadeUpdateIntervals, newCount);

            for (int cascadeIndex = m_initializedShadowCascadesCount; cascadeIndex < newCount; ++cascadeIndex)
                SetCascadeUpdateInterval(cascadeIndex, 1, 0);
        }

        internal void SetCascadeUpdateInterval(int cascadeNumber, int updateInterval, int updateIntervalFrame)
        {
            Debug.Assert(updateInterval > 0, "Update interval must be strictly positive!");
            Debug.Assert(updateInterval > updateIntervalFrame);
            m_shadowCascadeUpdateIntervals[cascadeNumber] = MyTuple.Create(updateInterval, updateIntervalFrame);
        }

        private unsafe MatrixD CreateGlobalMatrix()
        {
            MatrixD invView = MyRender11.Environment.Matrices.InvViewProjection;
            Vector3D* cornersWS = stackalloc Vector3D[8];
            Vector3D.Transform(m_cornersCS, ref invView, cornersWS);

            Vector3D centroid = Vector3D.Zero;
            for (int cornerIndex = 0; cornerIndex < 8; ++cornerIndex )
                centroid += cornersWS[cornerIndex];
            centroid /= 8f;
            MatrixD view = MatrixD.CreateLookAt(centroid, centroid - MyRender11.Environment.Data.EnvironmentLight.SunLightDirection, Vector3D.UnitY);
            MatrixD proj = MatrixD.CreateOrthographic(1, 1, 0, 1);

            return view * proj * MyMatrixHelpers.ClipspaceToTexture;
        }

        /// <summary>
        /// Creates shadowmap queries and appends them to the provided list
        /// </summary>
        internal unsafe void PrepareQueries(List<MyShadowmapQuery> appendShadowmapQueries)
        {
            Debug.Assert(appendShadowmapQueries != null, "Shadowmap query list cannot be null!");
            if (!MyRender11.Settings.EnableShadows || !MyRender11.DebugOverrides.Shadows || MyRender11.Settings.User.ShadowQuality.GetShadowsQuality() == MyShadowsQuality.DISABLED)
                return;

            MyGpuProfiler.IC_BeginBlock("PrepareCascades");
            MyImmediateRC.RC.CopyResource(m_cascadeShadowmapArray, m_cascadeShadowmapBackup);

            bool stabilize = true;
            const float DirectionDifferenceThreshold = 0.0175f;

            float shadowChangeDelayMultiplier = 180;

            for (int cascadeIndex = 0; cascadeIndex < m_initializedShadowCascadesCount; ++cascadeIndex)
            {
                ++m_shadowCascadeFramesSinceLightUpdate[cascadeIndex];

                if (m_shadowCascadeFramesSinceLightUpdate[cascadeIndex] > cascadeIndex * shadowChangeDelayMultiplier ||
                    MyRender11.Environment.Data.EnvironmentLight.SunLightDirection.Dot(m_shadowCascadeLightDirections[cascadeIndex]) < (1 - DirectionDifferenceThreshold))
                {
                    m_shadowCascadeLightDirections[cascadeIndex] = MyRender11.Environment.Data.EnvironmentLight.SunLightDirection;
                    m_shadowCascadeFramesSinceLightUpdate[cascadeIndex] = 0;
                }
            }

            var globalMatrix = CreateGlobalMatrix();

            float cascadesNearClip = 1f;

            float backOffset = MyRender11.Settings.User.ShadowQuality.BackOffset();
            float shadowmapSize = MyRender11.Settings.User.ShadowQuality.ShadowCascadeResolution();

            for (int cascadeIndex = 0; cascadeIndex < ShadowCascadeSplitDepths.Length; ++cascadeIndex)
                ShadowCascadeSplitDepths[cascadeIndex] = MyRender11.Settings.User.ShadowQuality.ShadowCascadeSplit(cascadeIndex);

            double unitWidth = 1.0 / MyRender11.Environment.Matrices.Projection.M11;
            double unitHeight = 1.0 / MyRender11.Environment.Matrices.Projection.M22;

            Vector3D* untransformedVertices = stackalloc Vector3D[4];
            untransformedVertices[0] = new Vector3D(-unitWidth, -unitHeight, -1);
            untransformedVertices[1] = new Vector3D(-unitWidth, unitHeight, -1);
            untransformedVertices[2] = new Vector3D(unitWidth, unitHeight, -1);
            untransformedVertices[3] = new Vector3D(unitWidth, -unitHeight, -1);

            MatrixD* cascadesMatrices = stackalloc MatrixD[MaxShadowCascades];

            for (int cascadeIndex = 0; cascadeIndex < m_initializedShadowCascadesCount; ++cascadeIndex)
            {
                for (int vertexIndex = 0; vertexIndex < 4; ++vertexIndex)
                {
                    m_frustumVerticesWS[vertexIndex] = untransformedVertices[vertexIndex] * ShadowCascadeSplitDepths[cascadeIndex];
                    m_frustumVerticesWS[vertexIndex + 4] = untransformedVertices[vertexIndex] * ShadowCascadeSplitDepths[cascadeIndex + 1];
                }

                bool skipCascade = MyCommon.FrameCounter % (ulong)m_shadowCascadeUpdateIntervals[cascadeIndex].Item1 != (ulong)m_shadowCascadeUpdateIntervals[cascadeIndex].Item2;
                bool forceUpdate = ShadowCascadeSplitDepths[cascadeIndex] > 1000f && Vector3D.DistanceSquared(m_shadowCascadeUpdatePositions[cascadeIndex], MyRender11.Environment.Matrices.CameraPosition) > Math.Pow(1000, 2);
                // 
                if (!forceUpdate && skipCascade && !Settings.Data.UpdateCascadesEveryFrame)
                    continue;
                if (Settings.ShadowCascadeFrozen[cascadeIndex])
                    continue;

                m_shadowCascadeUpdatePositions[cascadeIndex] = MyRender11.Environment.Matrices.CameraPosition;

                MatrixD invView = MyRender11.Environment.Matrices.InvView;
                Vector3D.Transform(m_frustumVerticesWS, ref invView, m_frustumVerticesWS);

                var bSphere = BoundingSphereD.CreateFromPoints(m_frustumVerticesWS);
                if (stabilize)
                {
                    bSphere.Center = bSphere.Center.Round();
                    bSphere.Radius = Math.Ceiling(bSphere.Radius);
                }

                var shadowCameraPosWS = bSphere.Center + m_shadowCascadeLightDirections[cascadeIndex] * (bSphere.Radius + cascadesNearClip);

                var lightView = VRageMath.MatrixD.CreateLookAt(shadowCameraPosWS, shadowCameraPosWS - m_shadowCascadeLightDirections[cascadeIndex], Math.Abs(Vector3.UnitY.Dot(m_shadowCascadeLightDirections[cascadeIndex])) < 0.99f ? Vector3.UnitY : Vector3.UnitX);
                var offset = bSphere.Radius + cascadesNearClip + backOffset;

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

                var diameter = corner1 - corner0;

                var cascadeScale = 1f / diameter;
                ShadowCascadeScales[cascadeIndex] = new Vector4D(cascadeScale, 0);

                var query = new MyShadowmapQuery();
                query.DepthBuffer = m_cascadeShadowmapArray.SubresourceDsv(cascadeIndex);
                query.Viewport = new MyViewport(shadowmapSize, shadowmapSize);

                m_cascadeInfo[cascadeIndex].WorldCameraOffsetPosition = MyRender11.Environment.Matrices.CameraPosition;
                m_cascadeInfo[cascadeIndex].WorldToProjection = cascadesMatrices[cascadeIndex];
                // todo: skip translation, recalculate matrix in local space, keep world space matrix only for bounding frustum
                m_cascadeInfo[cascadeIndex].LocalToProjection = Matrix.CreateTranslation(MyRender11.Environment.Matrices.CameraPosition) * cascadesMatrices[cascadeIndex];

                query.ProjectionInfo = m_cascadeInfo[cascadeIndex];
                query.ProjectionDir = m_shadowCascadeLightDirections[cascadeIndex];
                query.ProjectionFactor = (float)(shadowmapSize * shadowmapSize / (bSphere.Radius * bSphere.Radius * 4));

                query.QueryType = MyFrustumEnum.ShadowCascade;
                query.Index = cascadeIndex;

                appendShadowmapQueries.Add(query);
            }

            DebugProcessFrustrums();

            FillConstantBuffer(m_csmConstants);

            MyGpuProfiler.IC_EndBlock();
        }

        internal void FillConstantBuffer(IConstantBuffer constantBuffer)
        {
            var mapping = MyMapping.MapDiscard(constantBuffer);
            for (int cascadeIndex = 0; cascadeIndex < m_initializedShadowCascadesCount; ++cascadeIndex)
            {
                var matrix = Matrix.Transpose(m_cascadeInfo[cascadeIndex].CurrentLocalToProjection * MyMatrixHelpers.ClipspaceToTexture);
                mapping.WriteAndPosition(ref matrix);
            }
            for (int cascadeIndex = m_initializedShadowCascadesCount; cascadeIndex < MaxShadowCascades; ++cascadeIndex)
                mapping.WriteAndPosition(ref Matrix.Zero);

            mapping.WriteAndPosition(ShadowCascadeSplitDepths, ShadowCascadeSplitDepths.Length);

            float zero = 0;
            for (int splitIndex = ShadowCascadeSplitDepths.Length; splitIndex < MaxShadowCascades; ++splitIndex)
                mapping.WriteAndPosition(ref zero);

            for (int scaleIndex = 0; scaleIndex < ShadowCascadeScales.Length; ++scaleIndex)
            {
                Vector4 cascadeScale = ShadowCascadeScales[scaleIndex] / ShadowCascadeScales[0];
                mapping.WriteAndPosition(ref cascadeScale);
            }

            for (int scaleIndex = ShadowCascadeScales.Length; scaleIndex < MaxShadowCascades; ++scaleIndex)
                mapping.WriteAndPosition(ref Vector4.Zero);

            float resolution = MyRender11.Settings.User.ShadowQuality.ShadowCascadeResolution();
            mapping.WriteAndPosition(ref resolution);

            for (int paddingIndex = 1; paddingIndex < 4; ++paddingIndex)
                mapping.WriteAndPosition(ref zero);

            mapping.Unmap();
        }

        internal IBorrowedUavTexture PostProcess(IDepthArrayTexture cascadeArray)
        {
            IBorrowedUavTexture postProcessed = MyManagers.RwTexturesPool.BorrowUav("MyShadowCascades.PostProcess", Format.R8_UNorm);
            m_cascadePostProcessor.GatherArray(postProcessed, cascadeArray, m_cascadeInfo, m_csmConstants);
            return postProcessed;
        }

        private void DebugProcessFrustrums()
        {
            for (int i = 0; i < m_initializedShadowCascadesCount; i++)
            {
                //if (Settings.ShadowCascadeFrozen[i])
                //{
                //    if (Settings.Data.DisplayFrozenShadowCascade)
                //    {
                //        DebugDrawFrozenViewFrustrum(i);
                //        DebugDrawFrozenCascadeFrustrum(i);
                //    }
                //}
                //else
                    m_debugFrozenMatrixView[i] = MyRender11.Environment.Matrices.ViewProjectionD;

            }
        }

        private void DebugDrawFrozenViewFrustrum(int nCascade)
        {
            var oldInvView = MatrixD.Invert(Matrix.CreateTranslation(MyRender11.Environment.Matrices.CameraPosition) * m_debugFrozenMatrixView[nCascade]);
            Vector3D.Transform(m_blockOS, ref oldInvView, m_tmpDebugFrozen8D);

            for (int i = 0; i < 8; i++)
                m_tmpDebugFrozen8[i] = m_tmpDebugFrozen8D[i];

            var batch = MyLinesRenderer.CreateBatch();
            batch.Add6FacedConvex(m_tmpDebugFrozen8, Color.Blue);

            batch.Commit();
        }

        private void DebugDrawFrozenCascadeFrustrum(int nCascade)
        {
            var lineBatch = MyLinesRenderer.CreateBatch();

            MatrixD inverseViewProj = MatrixD.Invert(Matrix.CreateTranslation(MyRender11.Environment.Matrices.CameraPosition) * m_cascadeInfo[nCascade].WorldToProjection);
            Vector3D.Transform(m_blockOS, ref inverseViewProj, m_tmpDebugFrozen8D);

            for (int vertexIndex = 0; vertexIndex < 8; ++vertexIndex)
                m_tmpDebugFrozen8[vertexIndex] = m_tmpDebugFrozen8D[vertexIndex];

            MyPrimitivesRenderer.Draw6FacedConvexZ(m_tmpDebugFrozen8, m_debugCascadeColor[nCascade], 0.2f);
            lineBatch.Add6FacedConvex(m_tmpDebugFrozen8, Color.Pink);

            lineBatch.Commit();
        }
    }
}
