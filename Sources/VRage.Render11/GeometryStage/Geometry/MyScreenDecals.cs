using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using VRage.Render11.Common;
using VRage.Render11.RenderContext;
using VRage.Render11.Resources;
using VRage.Utils;
using VRageMath;
using VRageRender.Messages;
using DecalNode = System.Collections.Generic.LinkedListNode<VRageRender.MyScreenDecal>;

namespace VRageRender
{
    class MyScreenDecal
    {
        internal uint FadeTimestamp; // ms from start of the game
        internal MyDecalTopoData TopoData;
        internal uint ID;
        internal uint ParentID;
        internal MyDecalFlags Flags;
        internal string SourceTarget;
        internal string Material;
        internal MyStringId MaterialId;
        internal int MaterialIndex;
    }

    struct MyDecalTextures
    {
        public MyFileTextureEnum DecalType;
        public ITexture ColorMetalTexture;
        public ITexture NormalmapTexture;
        public ITexture ExtensionsTexture;
        public ITexture AlphamaskTexture;
    }

    [StructLayout(LayoutKind.Explicit)]
    struct MyDecalConstants
    {
        [FieldOffset(0)]
        public Matrix WorldMatrix;

        [FieldOffset(48)]
        public float FadeAlpha;

        [FieldOffset(52)]
        public Vector3 __padding;

        [FieldOffset(64)]
        public Matrix InvWorldMatrix;
    }

    struct MyDecalJob
    {
        public Matrix WorldMatrix;
        public float FadeAlpha;
    }

    static class MyScreenDecals
    {
        public const float VISIBLE_DECALS_SQ_TH = 10000;       // 100m
        const int DECAL_BATCH_SIZE = 512;
        const uint DECAL_FADE_DURATION = 6000; // ms

        static int m_decalsQueueSize = 1024;

        static IIndexBuffer m_IB;

        static VertexShaderId m_vs = VertexShaderId.NULL;
        static PixelShaderId m_psNormalMap = PixelShaderId.NULL;
        static PixelShaderId m_psColorMap = PixelShaderId.NULL;
        static PixelShaderId m_psColorMapTransparent = PixelShaderId.NULL;
        static PixelShaderId m_psNormalColorMap = PixelShaderId.NULL;
        static PixelShaderId m_psNormalColorExtMap = PixelShaderId.NULL;

        static Dictionary<uint, DecalNode> m_nodeMap = new Dictionary<uint, DecalNode>();
        static Dictionary<uint, List<DecalNode>> m_entityDecals = new Dictionary<uint, List<DecalNode>>();
        static LinkedList<MyScreenDecal> m_decals = new LinkedList<MyScreenDecal>();
        static Dictionary<MyStringId, List<MyDecalTextures>> m_materials = new Dictionary<MyStringId, List<MyDecalTextures>>(MyStringId.Comparer);

        static List<MyDecalJob> m_jobs = new List<MyDecalJob>();
        static Dictionary<MyMaterialIdentity, List<MyScreenDecal>> m_materialsToDraw = new Dictionary<MyMaterialIdentity, List<MyScreenDecal>>();

        static DateTime m_startTime = DateTime.Now;


        internal static void Init()
        {
            m_vs = MyShaders.CreateVs("Decals/Decals.hlsl");
            var transparentMacro = new ShaderMacro("RENDER_TO_TRANSPARENT", null);
            m_psColorMapTransparent = MyShaders.CreatePs("Decals/Decals.hlsl", new ShaderMacro[] { transparentMacro });
            m_psColorMap = MyShaders.CreatePs("Decals/Decals.hlsl", MyMeshMaterials1.GetMaterialTextureMacros(MyFileTextureEnum.COLOR_METAL));
            m_psNormalMap = MyShaders.CreatePs("Decals/Decals.hlsl", MyMeshMaterials1.GetMaterialTextureMacros(MyFileTextureEnum.NORMALMAP_GLOSS));
            m_psNormalColorMap = MyShaders.CreatePs("Decals/Decals.hlsl",
                MyMeshMaterials1.GetMaterialTextureMacros(MyFileTextureEnum.COLOR_METAL | MyFileTextureEnum.NORMALMAP_GLOSS));
            m_psNormalColorExtMap = MyShaders.CreatePs("Decals/Decals.hlsl",
                MyMeshMaterials1.GetMaterialTextureMacros(
                MyFileTextureEnum.COLOR_METAL | MyFileTextureEnum.NORMALMAP_GLOSS | MyFileTextureEnum.EXTENSIONS));

            InitIB();
        }

        internal static void OnSessionEnd()
        {
            ClearDecals();
            m_materials.Clear();
            m_materialsToDraw.Clear();
            m_jobs.Clear();
        }

        internal static void OnDeviceReset()
        {
            OnDeviceEnd();
            InitIB();
        }

        internal static void OnDeviceEnd()
        {
            if (m_IB != null)
                MyManagers.Buffers.Dispose(m_IB); m_IB = null;
        }

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

            ushort[] indicesData = new ushort[DECAL_BATCH_SIZE * indices.Length];
            var instanceLen = indices.Length;
            for (int i = 0; i < DECAL_BATCH_SIZE; i++)
            {
                for (int j = 0; j < instanceLen; j++)
                {
                    indicesData[i * instanceLen + j] = (ushort)(indices[j] + 8 * i);
                }
            }

            if (m_IB == null)
            {
                fixed (ushort* I = indicesData)
                {
                    m_IB = MyManagers.Buffers.CreateIndexBuffer(
                        "MyScreenDecals", indicesData.Length, new IntPtr(I),
                        MyIndexBufferFormat.UShort, ResourceUsage.Immutable);
                }
            }
        }

        internal static void AddDecal(uint ID, uint ParentID, ref MyDecalTopoData topoData, MyDecalFlags flags, string sourceTarget, string material, int matIndex)
        {
            if (m_decals.Count >= m_decalsQueueSize && m_decals.Count != 0)
                MarkForRemove(m_decals.First);

            MyScreenDecal decal = new MyScreenDecal();
            decal.FadeTimestamp = uint.MaxValue;
            decal.ID = ID;
            decal.ParentID = ParentID;
            decal.TopoData = topoData;
            decal.Flags = flags;
            decal.SourceTarget = sourceTarget;
            decal.Material = material;
            decal.MaterialId = X.TEXT_(material);
            decal.MaterialIndex = matIndex;

            if (!flags.HasFlag(MyDecalFlags.World))
            {
                var parent = MyIDTracker<MyActor>.FindByID(ParentID);
                if (parent == null)
                    return;

                decal.TopoData.WorldPosition = Vector3D.Transform(topoData.MatrixCurrent.Translation, ref parent.WorldMatrix);
            }

            DecalNode node = m_decals.AddLast(decal);

            m_nodeMap[ID] = node;

            List<DecalNode> handles;
            bool found = m_entityDecals.TryGetValue(ParentID, out handles);
            if (!found)
            {
                handles = new List<DecalNode>();
                m_entityDecals.Add(ParentID, handles);
            }

            handles.Add(node);
        }

        public static void UpdateDecals(IReadOnlyList<MyDecalPositionUpdate> decals)
        {
            uint currentObjectId = MyRenderProxy.RENDER_ID_UNASSIGNED;
            MatrixD currentWorldMatrix = new MatrixD();
            for (int it = 0; it < decals.Count; it++)
            {
                MyDecalPositionUpdate position = decals[it];

                DecalNode node;
                bool found = m_nodeMap.TryGetValue(position.ID, out node);
                if (!found)
                    continue;
                MyScreenDecal decal = node.Value;
                if (decal.Flags.HasFlag(MyDecalFlags.World))
                {
                    decal.TopoData.WorldPosition = position.Position;
                }
                else
                {
                    if (currentObjectId != decal.ParentID)
                    {
                        var parent = MyIDTracker<MyActor>.FindByID(decal.ParentID);
                        currentWorldMatrix = parent.WorldMatrix;
                        currentObjectId = decal.ParentID;
                    }

                    decal.TopoData.WorldPosition = Vector3D.Transform(position.Transform.Translation, ref currentWorldMatrix);
                }

                node.Value.TopoData.MatrixCurrent = position.Transform;
            }
        }

        public static void RemoveDecal(uint ID)
        {
            DecalNode node;
            bool found = m_nodeMap.TryGetValue(ID, out node);
            if (!found)
                return;

            MarkForRemove(node);
        }

        private static void MarkForRemove(DecalNode decal)
        {
            uint now = GetTimeStampSinceStart();
            decal.Value.FadeTimestamp = now + DECAL_FADE_DURATION;

            // Reinsert the node at the end of the queue to allow
            // following decals to be candidate for removal
            m_decals.Remove(decal);
            m_decals.AddLast(decal);
        }

        public static void RemoveEntityDecals(uint id)
        {
            List<DecalNode> decals;
            bool found = m_entityDecals.TryGetValue(id, out decals);
            if (!found)
                return;

            foreach (var node in decals)
            {
                m_decals.Remove(node);
                m_nodeMap.Remove(node.Value.ID);
                //MyRenderProxy.RemoveMessageId(node.Value.ID, MyRenderProxy.ObjectType.ScreenDecal);
            }

            m_entityDecals.Remove(id);
        }

        public static void SetDecalGlobals(MyDecalGlobals globals)
        {
            m_decalsQueueSize = globals.DecalQueueSize;
        }

        public static void ClearDecals()
        {
            m_nodeMap.Clear();
            m_entityDecals.Clear();
            m_decals.Clear();
        }

        public static bool HasEntityDecals(uint ID)
        {
            return m_entityDecals.ContainsKey(ID);
        }

        private static void RemoveDecalByNode(DecalNode node)
        {
            MyScreenDecal decal = node.Value;
            List<DecalNode> decals = m_entityDecals[decal.ParentID];
            decals.Remove(node);
            if (decals.Count == 0)
                m_entityDecals.Remove(decal.ParentID);

            m_decals.Remove(node);
            m_nodeMap.Remove(decal.ID);
            MyRenderProxy.RemoveMessageId(decal.ID, MyRenderProxy.ObjectType.ScreenDecal);
        }

        public static void RegisterMaterials(Dictionary<string, List<MyDecalMaterialDesc>> descriptions)
        {
            MyFileTextureManager texManager = MyManagers.FileTextures;
            m_materials.Clear();
            foreach (var pair in descriptions)
            {
                List<MyDecalTextures> list = new List<MyDecalTextures>();
                foreach (var desc in pair.Value)
                {
                    list.Add(new MyDecalTextures()
                    {
                        DecalType = MyMeshMaterials1.GetMaterialTextureTypes(desc.ColorMetalTexture, desc.NormalmapTexture, desc.ExtensionsTexture, null),
                        ColorMetalTexture = texManager.GetTexture(desc.ColorMetalTexture, MyFileTextureEnum.COLOR_METAL),
                        NormalmapTexture = texManager.GetTexture(desc.NormalmapTexture, MyFileTextureEnum.NORMALMAP_GLOSS),
                        ExtensionsTexture = texManager.GetTexture(desc.ExtensionsTexture, MyFileTextureEnum.EXTENSIONS),
                        AlphamaskTexture = texManager.GetTexture(desc.AlphamaskTexture, MyFileTextureEnum.ALPHAMASK),
                    });
                }

                m_materials[X.TEXT_(pair.Key)] = list;
            }
        }

        public static bool GetDecalTopoData(uint decalId, out MyDecalTopoData data)
        {
            DecalNode node;
            bool found = m_nodeMap.TryGetValue(decalId, out node);
            if (!found)
            {
                data = new MyDecalTopoData();
                return false;
            }

            data = node.Value.TopoData;
            return true;
        }

        static unsafe void DrawBatches(MyRenderContext rc, IRtvTexture gbuffer1Copy, MyStringId material, int matIndex, bool transparent)
        {
            if (m_jobs.Count == 0)
                return;

            var matDesc = m_materials[material][matIndex];

            rc.PixelShader.SetSrv(0, MyGBuffer.Main.DepthStencil.SrvDepth);
            rc.PixelShader.SetSrv(1, gbuffer1Copy);
            if (transparent)
            {
                rc.PixelShader.Set(m_psColorMapTransparent);
            }
            else
            {
                rc.SetRtvs(MyGBuffer.Main, MyDepthStencilAccess.ReadOnly);
                MyFileTextureEnum type = matDesc.DecalType;
                switch (type)
                {
                    case MyFileTextureEnum.NORMALMAP_GLOSS:
                        rc.PixelShader.Set(m_psNormalMap);
                        break;
                    case MyFileTextureEnum.COLOR_METAL:
                        rc.PixelShader.Set(m_psColorMap);
                        break;
                    case MyFileTextureEnum.COLOR_METAL | MyFileTextureEnum.NORMALMAP_GLOSS:
                        rc.PixelShader.Set(m_psNormalColorMap);
                        break;
                    case MyFileTextureEnum.COLOR_METAL | MyFileTextureEnum.NORMALMAP_GLOSS | MyFileTextureEnum.EXTENSIONS:
                        rc.PixelShader.Set(m_psNormalColorExtMap);
                        break;
                    default:
                        throw new Exception("Unknown decal type");
                }
                MyMeshMaterials1.BindMaterialTextureBlendStates(rc, type, true);
            }

            // factor 1 makes overwriting of gbuffer color & subtracting from ao
            rc.PixelShader.SetSrv(3, matDesc.AlphamaskTexture);
            rc.PixelShader.SetSrv(4, matDesc.ColorMetalTexture);
            rc.PixelShader.SetSrv(5, matDesc.NormalmapTexture);
            rc.PixelShader.SetSrv(6, matDesc.ExtensionsTexture);

            var decalCb = MyCommon.GetObjectCB(sizeof(MyDecalConstants) * DECAL_BATCH_SIZE);

            int batchCount = m_jobs.Count / DECAL_BATCH_SIZE + 1;
            int offset = 0;
            for (int i1 = 0; i1 < batchCount; i1++)
            {
                var mapping = MyMapping.MapDiscard(decalCb);

                int leftDecals = m_jobs.Count - offset;
                int decalCount = leftDecals > DECAL_BATCH_SIZE ? DECAL_BATCH_SIZE : leftDecals;
                for (int i2 = 0; i2 < decalCount; ++i2)
                {
                    MyDecalConstants constants = new MyDecalConstants();
                    EncodeJobConstants(i2 + offset, ref constants);
                    mapping.WriteAndPosition(ref constants);
                }

                mapping.Unmap();

                // Draw a box without buffer: 36 vertices -> 12 triangles. 2 triangles per face -> 6 faces
                MyImmediateRC.RC.DrawIndexed(36 * decalCount, 0, 0);

                offset += DECAL_BATCH_SIZE;
            }
        }

        /// <param name="visibleRenderIDs">Optional list of visible render object IDs</param>
        internal static void Draw(IRtvTexture gbuffer1Copy, bool transparent, HashSet<uint> visibleRenderIDs = null, float squaredDistanceMax = VISIBLE_DECALS_SQ_TH)
        {
            if (m_decals.Count == 0)
                return;

            uint sinceStartTs = GetTimeStampSinceStart();
            MyDecalFlags targetFlag = transparent ? MyDecalFlags.Transparent : MyDecalFlags.None;

            bool visibleDecals;
            if (visibleRenderIDs == null || visibleRenderIDs.Count > m_decals.Count)
                visibleDecals = IterateDecals(visibleRenderIDs, targetFlag, squaredDistanceMax, sinceStartTs);
            else
                visibleDecals = IterateVisibleRenderIDs(visibleRenderIDs, targetFlag, squaredDistanceMax, sinceStartTs);

            if (!visibleDecals)
                return;

            DrawInternal(gbuffer1Copy, transparent, sinceStartTs);
        }


        static List<DecalNode> m_nodesToAdd = new List<DecalNode>();
        static List<DecalNode> m_nodesToRemove = new List<DecalNode>();

        /// <returns>True if visible decals are found</returns>
        private static bool IterateVisibleRenderIDs(HashSet<uint> visibleRenderIDs, MyDecalFlags targetFlag, float squaredDistanceMax, uint sinceStartTs)
        {
            bool ret = false;
            foreach (uint renderID in visibleRenderIDs)
            {
                List<DecalNode> decals;
                bool found = m_entityDecals.TryGetValue(renderID, out decals);
                if (!found)
                    continue;

                foreach (DecalNode node in decals)
                {
                    if (node.Value.FadeTimestamp < sinceStartTs)
                    {
                        m_nodesToRemove.Add(node);
                        continue;
                    }

                    MyScreenDecal decal = node.Value;
                    MyDecalFlags flag = decal.Flags & MyDecalFlags.Transparent;
                    if (flag == targetFlag && IsDecalWithinRadius(decal, squaredDistanceMax))
                    {
                        m_nodesToAdd.Add(node);
                        ret = true;
                    }
                }
            }

            foreach (var node in m_nodesToRemove)
            {
                RemoveDecalByNode(node);
            }
            m_nodesToRemove.Clear();


            foreach (var node in m_nodesToAdd)
            {
                AddDecalNodeForDraw(node);

            }
            m_nodesToAdd.Clear();

            return ret;
        }

        /// <returns>True if visible decals are found</returns>
        private static bool IterateDecals(HashSet<uint> visibleRenderIDs, MyDecalFlags targetFlag, float squaredDistanceMax, uint sinceStartTs)
        {
            bool ret = false;

            int count = m_decals.Count;
            DecalNode current = m_decals.First;
            int it = 0;
            while (current != null && it < count)
            {
                DecalNode next = current.Next;
                if (current.Value.FadeTimestamp < sinceStartTs)
                {
                    RemoveDecalByNode(current);
                    current = next;
                    continue;
                }

                MyScreenDecal decal = current.Value;
                MyDecalFlags flag = decal.Flags & MyDecalFlags.Transparent;
                if (flag == targetFlag && (visibleRenderIDs == null || visibleRenderIDs.Contains(decal.ParentID))
                    && IsDecalWithinRadius(decal, squaredDistanceMax))
                {
                    AddDecalNodeForDraw(current);
                    ret = true;
                }

                current = next;
                it++;
            }

            return ret;
        }

        private static void AddDecalNodeForDraw(DecalNode node)
        {
            // Reinsert the node at the end of the queue to
            // make it less likely to be candidate for removal
            m_decals.Remove(node);
            m_decals.AddLast(node);
            AddDecalForDraw(node.Value);
        }

        unsafe static void DrawInternal(IRtvTexture gbuffer1Copy, bool transparent, uint sinceStartTs)
        {
            var RC = MyImmediateRC.RC;
            int nPasses = MyStereoRender.Enable ? 2 : 1;
            for (int i = 0; i < nPasses; i++)
            {
                if (!MyStereoRender.Enable)
                {
                    RC.AllShaderStages.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
                    RC.SetViewport(0, 0, MyRender11.ViewportResolution.X, MyRender11.ViewportResolution.Y);
                }
                else
                {
                    MyStereoRender.RenderRegion = i == 0 ? MyStereoRegion.LEFT : MyStereoRegion.RIGHT;
                    MyStereoRender.BindRawCB_FrameConstants(RC);
                    MyStereoRender.SetViewport(RC);
                }

                RC.SetPrimitiveTopology(PrimitiveTopology.TriangleList);
                RC.SetIndexBuffer(m_IB);
                RC.SetInputLayout(null);

                RC.VertexShader.Set(m_vs);
                RC.SetDepthStencilState(MyDepthStencilStateManager.DepthTestReadOnly);
                RC.PixelShader.SetSamplers(0, MySamplerStateManager.StandardSamplers);

                var decalCb = MyCommon.GetObjectCB(sizeof(MyDecalConstants) * DECAL_BATCH_SIZE);
                RC.AllShaderStages.SetConstantBuffer(2, decalCb);

                foreach (var pair in m_materialsToDraw)
                {
                    PrepareMaterialBatches(RC, pair.Value, sinceStartTs);
                    DrawBatches(RC, gbuffer1Copy, pair.Key.Material, pair.Key.Index, transparent);
                    m_jobs.Clear();
                }
            }

            // Clear materials to draw outside eye rendering passes
            foreach (var pair in m_materialsToDraw)
                pair.Value.Clear();

            RC.SetBlendState(null);
            RC.PixelShader.SetSrv(0, null);

            if (MyStereoRender.Enable)
            {
                RC.AllShaderStages.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
                RC.SetViewport(0, 0, MyRender11.ViewportResolution.X, MyRender11.ViewportResolution.Y);
                MyStereoRender.RenderRegion = MyStereoRegion.FULLSCREEN;
            }
        }

        private static void PrepareMaterialBatches(MyRenderContext rc, List<MyScreenDecal> decals, uint sinceStartTs)
        {
            if (decals.Count == 0)
                return;

            List<uint> decalsToRemove = new List<uint>();
            foreach (MyScreenDecal decal in decals)
            {
                var parent = MyIDTracker<MyActor>.FindByID(decal.ParentID);
                bool world = decal.Flags.HasFlag(MyDecalFlags.World);
                if (parent == null && !world)
                {
                    decalsToRemove.Add(decal.ID);
                    continue;
                }

                Matrix volumeMatrix;
                if (world)
                {
                    volumeMatrix = decal.TopoData.MatrixCurrent;
                    volumeMatrix.Translation = (Vector3)(decal.TopoData.WorldPosition - MyRender11.Environment.Matrices.CameraPosition);
                }
                else
                {
                    MatrixD volumeMatrixD = ((MatrixD)decal.TopoData.MatrixCurrent) * parent.WorldMatrix;
                    volumeMatrix = volumeMatrixD;
                    volumeMatrix.Translation = (Vector3)(volumeMatrixD.Translation - MyRender11.Environment.Matrices.CameraPosition);
                }

                uint fadeDiff = decal.FadeTimestamp - sinceStartTs;
                float fadeAlpha = decal.FadeTimestamp - sinceStartTs >= DECAL_FADE_DURATION ? 1 : fadeDiff / (float)DECAL_FADE_DURATION;
                m_jobs.Add(new MyDecalJob() { WorldMatrix = volumeMatrix, FadeAlpha = fadeAlpha });

                if (MyRender11.Settings.DebugDrawDecals)
                {
                    MatrixD worldMatrix;
                    if (parent == null)
                    {
                        worldMatrix = decal.TopoData.MatrixCurrent;
                        worldMatrix.Translation = decal.TopoData.WorldPosition; //{X:5.27669191360474 Y:12.7891067266464 Z:-54.623966217041}
                    }
                    else
                    {
                        worldMatrix = ((MatrixD)decal.TopoData.MatrixCurrent) * parent.WorldMatrix;
                    }

                    MyRenderProxy.DebugDrawAxis(worldMatrix, 0.2f, false, true);
                    MyRenderProxy.DebugDrawOBB(worldMatrix, Color.Blue, 0.1f, false, false);

                    Vector3D position = worldMatrix.Translation;
                    MyRenderProxy.DebugDrawText3D(position, decal.SourceTarget, Color.White, 0.5f, false);
                }
            }

            foreach (uint id in decalsToRemove)
            {
                DecalNode node = m_nodeMap[id];
                RemoveDecalByNode(node);
            }
        }

        static void AddDecalForDraw(MyScreenDecal decal)
        {
            List<MyScreenDecal> decals;
            MyMaterialIdentity identity = new MyMaterialIdentity() { Material = decal.MaterialId, Index = decal.MaterialIndex };
            bool found = m_materialsToDraw.TryGetValue(identity, out decals);
            if (!found)
            {
                decals = new List<MyScreenDecal>();
                m_materialsToDraw[identity] = decals;
            }

            decals.Add(decal);
        }

        private static void EncodeJobConstants(int index, ref MyDecalConstants constants)
        {
            Matrix worldMatrix = Matrix.Transpose(m_jobs[index].WorldMatrix);
            Matrix inverseMatrix = Matrix.Transpose(Matrix.Invert(m_jobs[index].WorldMatrix));
            constants.WorldMatrix = worldMatrix;
            constants.FadeAlpha = m_jobs[index].FadeAlpha;
            constants.__padding = new Vector3(0, 0, 1);
            constants.InvWorldMatrix = inverseMatrix;
        }

        static bool IsDecalWithinRadius(MyScreenDecal decal, float squaredDistanceMax)
        {
            var parent = MyIDTracker<MyActor>.FindByID(decal.ParentID);
            bool world = decal.Flags.HasFlag(MyDecalFlags.World);

            Vector3 distance;
            if (world)
            {
                distance = (decal.TopoData.WorldPosition - MyRender11.Environment.Matrices.CameraPosition);
            }
            else
            {
                MatrixD volumeMatrixD = ((MatrixD)decal.TopoData.MatrixCurrent) * parent.WorldMatrix;
                distance = (volumeMatrixD.Translation - MyRender11.Environment.Matrices.CameraPosition);
            }

            float squaredDistance = (float)distance.LengthSquared();
            return squaredDistance <= squaredDistanceMax;
        }

        static uint GetTimeStampSinceStart()
        {
            TimeSpan sinceStart = DateTime.Now - m_startTime;
            return (uint)sinceStart.TotalMilliseconds;
        }

        struct MyMaterialIdentity : IEquatable<MyMaterialIdentity>
        {
            public MyStringId Material;
            public int Index;

            public bool Equals(MyMaterialIdentity other)
            {
                return Material == other.Material && Index == other.Index;
            }

            public override int GetHashCode()
            {
                return Material.Id ^ Index;
            }
        }
    }
}
