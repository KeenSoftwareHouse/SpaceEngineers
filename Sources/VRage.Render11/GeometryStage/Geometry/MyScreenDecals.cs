using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using VRage;
using VRage.Utils;
using VRageMath;
using VRageRender.Resources;
using DecalNode = System.Collections.Generic.LinkedListNode<VRageRender.MyScreenDecal>;

namespace VRageRender
{
    class MyScreenDecal
    {
        public uint FadeTimestamp; // ms from start of the game
        public MyDecalTopoData Data;
        public Matrix OBBox;
        public uint ID;
        public uint ParentID;
        public MyDecalFlags Flags;
        public string SourceTarget;
        public string Material;
        public MyStringId MaterialId;
        public int MaterialIndex;
    }

    struct MyDecalTextures
    {
        public MyScreenDecalType DecalType;
        public TexId NormalmapTexture;
        public TexId ColorMetalTexture;
        public TexId AlphamaskTexture;
        public TexId ExtensionsTexture;
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
        const int DECAL_BATCH_SIZE = 512;
        const uint DECAL_FADE_DURATION = 6000; // ms

        static int m_decalsQueueSize = 1024;

        static IndexBufferId m_IB = IndexBufferId.NULL;

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
            m_vs = MyShaders.CreateVs("decal.hlsl");
            var normalMapMacro = new ShaderMacro("USE_NORMALMAP_DECAL", null);
            var colorMapMacro = new ShaderMacro("USE_COLORMAP_DECAL", null);
            var transparentMacro = new ShaderMacro("RENDER_TO_TRANSPARENT", null);
            var extensionsMacro = new ShaderMacro("USE_EXTENSIONS_TEXTURE", null);
            m_psColorMap = MyShaders.CreatePs("decal.hlsl", new ShaderMacro[] { colorMapMacro });
            m_psColorMapTransparent = MyShaders.CreatePs("decal.hlsl", new ShaderMacro[] { colorMapMacro, transparentMacro });
            m_psNormalMap = MyShaders.CreatePs("decal.hlsl", new ShaderMacro[] { normalMapMacro });
            m_psNormalColorMap = MyShaders.CreatePs("decal.hlsl", new ShaderMacro[] { colorMapMacro, normalMapMacro });
            m_psNormalColorExtMap = MyShaders.CreatePs("decal.hlsl", new ShaderMacro[] { colorMapMacro, normalMapMacro, extensionsMacro });

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
            if (m_IB != IndexBufferId.NULL)
            {
                MyHwBuffers.Destroy(m_IB);
                m_IB = IndexBufferId.NULL;
            }
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

            if (m_IB == IndexBufferId.NULL)
            {
                fixed (ushort* I = indicesData)
                {
                    m_IB = MyHwBuffers.CreateIndexBuffer(indicesData.Length, Format.R16_UInt, BindFlags.IndexBuffer, ResourceUsage.Immutable, new IntPtr(I), "MyScreenDecals");
                }
            }
        }

        public static void AddDecal(uint ID, uint ParentID, ref MyDecalTopoData data, MyDecalFlags flags, string sourceTarget, string material, int matIndex)
        {
            if (m_decals.Count == m_decalsQueueSize)
                MarkForRemove(m_decals.First.Value);

            MyScreenDecal decal = new MyScreenDecal();
            decal.FadeTimestamp = uint.MaxValue;
            decal.ID = ID;
            decal.ParentID = ParentID;
            decal.Data = data;
            decal.Flags = flags;
            decal.OBBox = ComputeOBB(ref data, flags);
            decal.SourceTarget = sourceTarget;
            decal.Material = material;
            decal.MaterialId = X.TEXT_(material);
            decal.MaterialIndex = matIndex;

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

        public static void UpdateDecals(List<MyDecalPositionUpdate> decals)
        {
            for (int it = 0; it < decals.Count; it++)
            {
                MyDecalPositionUpdate position = decals[it];

                DecalNode node;
                bool found = m_nodeMap.TryGetValue(position.ID, out node);
                if (!found)
                    continue;

                MyScreenDecal decal = node.Value;
                decal.Data.Position = position.Position;
                decal.Data.Normal = position.Normal;
                node.Value.OBBox = ComputeOBB(ref decal.Data, decal.Flags);
            }
        }

        private static Matrix ComputeOBB(ref MyDecalTopoData data, MyDecalFlags flags)
        {
            var perp = Vector3.CalculatePerpendicularVector(data.Normal);
            if (data.Rotation != 0)
            {
                // Rotate around normal
                Quaternion q = Quaternion.CreateFromAxisAngle(data.Normal, data.Rotation);
                perp = new Vector3((new Quaternion(perp, 0) * q).ToVector4());
            }

            Matrix pos = Matrix.CreateWorld(data.Position, data.Normal, perp);
            return Matrix.CreateScale(data.Scale) * pos;
        }

        public static void RemoveDecal(uint ID)
        {
            DecalNode node;
            bool found = m_nodeMap.TryGetValue(ID, out node);
            if (!found)
                return;

            MarkForRemove(node.Value);
        }

        private static void MarkForRemove(MyScreenDecal decal)
        {
            uint now = GetTimeStampSinceStart();
            decal.FadeTimestamp = now + DECAL_FADE_DURATION;
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
        }

        public static void RegisterMaterials(Dictionary<string, List<MyDecalMaterialDesc>> descriptions)
        {
            m_materials.Clear();
            foreach (var pair in descriptions)
            {
                List<MyDecalTextures> list = new List<MyDecalTextures>();
                foreach (var desc in pair.Value)
                {
                    list.Add(new MyDecalTextures()
                    {
                        DecalType = desc.DecalType,
                        NormalmapTexture = MyTextures.GetTexture(desc.NormalmapTexture, MyTextureEnum.NORMALMAP_GLOSS),
                        ColorMetalTexture = MyTextures.GetTexture(desc.ColorMetalTexture, MyTextureEnum.COLOR_METAL),
                        AlphamaskTexture = MyTextures.GetTexture(desc.AlphamaskTexture, MyTextureEnum.ALPHAMASK),
                        ExtensionsTexture = MyTextures.GetTexture(desc.ExtensionsTexture, MyTextureEnum.EXTENSIONS),
                    });
                }

                m_materials[X.TEXT_(pair.Key)] = list;
            }
        }

        unsafe static void DrawBatches(MyRenderContext RC, MyStringId material, int matIndex, bool transparent)
        {
            if (m_jobs.Count == 0)
                return;

            var matDesc = m_materials[material][matIndex];
            MyScreenDecalType type = matDesc.DecalType;
            if (transparent)
            {
                // Always fallback to colormap for transparent surface decals
                type = MyScreenDecalType.ColorMap;
            }

            switch (type)
            {
                case MyScreenDecalType.NormalMap:
                    BindResources(RC);
                    RC.SetPS(m_psNormalMap);
                    RC.SetBS(MyRender11.BlendDecalNormal);
                    break;
                case MyScreenDecalType.ColorMap:
                    if (transparent)
                    {
                        BindResourcesTransparentBillboards(RC);
                        RC.SetPS(m_psColorMapTransparent);
                    }
                    else
                    {
                        BindResources(RC);
                        RC.SetPS(m_psColorMap);
                        RC.SetBS(MyRender11.BlendDecalColor);
                    }
                    break;
                case MyScreenDecalType.NormalColorMap:
                    BindResources(RC);
                    RC.SetPS(m_psNormalColorMap);
                    RC.SetBS(MyRender11.BlendDecalNormalColor);
                    break;
                case MyScreenDecalType.NormalColorExtMap:
                    BindResources(RC);
                    RC.SetPS(m_psNormalColorExtMap);
                    RC.SetBS(MyRender11.BlendDecalNormalColorExt);
                    break;
                default:
                    throw new Exception("Unknown decal type");
            }

            // factor 1 makes overwriting of gbuffer color & subtracting from ao
            RC.DeviceContext.PixelShader.SetShaderResource(3, MyTextures.GetView(matDesc.AlphamaskTexture));
            RC.DeviceContext.PixelShader.SetShaderResource(4, MyTextures.GetView(matDesc.ColorMetalTexture));
            RC.DeviceContext.PixelShader.SetShaderResource(5, MyTextures.GetView(matDesc.NormalmapTexture));
            RC.DeviceContext.PixelShader.SetShaderResource(6, MyTextures.GetView(matDesc.ExtensionsTexture));

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
                    EncodeJobConstants(i2, ref constants);
                    mapping.WriteAndPosition(ref constants);
                }

                mapping.Unmap();

                // Draw a box without buffer: 36 vertices -> 12 triangles. 2 triangles per face -> 6 faces
                MyImmediateRC.RC.DeviceContext.DrawIndexed(36 * decalCount, 0, 0);

                offset += DECAL_BATCH_SIZE;
            }
        }

        internal static void Draw(bool transparent)
        {
            if (m_decals.Count == 0)
                return;

            uint sinceStart = GetTimeStampSinceStart();

            MyDecalFlags targetFlag = transparent ? MyDecalFlags.Transparent : MyDecalFlags.None;
            bool decalsToDraw = false;

            DecalNode current = m_decals.First;
            while (current != null)
            {
                DecalNode next = current.Next;
                if (current.Value.FadeTimestamp < sinceStart)
                {
                    RemoveDecalByNode(current);
                    current = next;
                    continue;
                }

                MyDecalFlags flag = current.Value.Flags & MyDecalFlags.Transparent;
                if (targetFlag == flag)
                {
                    AddDecalForDraw(current.Value);
                    decalsToDraw = true;
                }

                current = next;
            }

            if (!decalsToDraw)
                return;

            DrawInternal(transparent, sinceStart);
        }

        // ENABLE-ME: As soon as a relieble list of frustum visible ojects IDs is available
        internal static void Draw(HashSet<uint> visibleRenderIDs, bool transparent)
        {
            if (m_decals.Count == 0)
                return;

            uint sinceStartTs = GetTimeStampSinceStart();

            MyDecalFlags targetFlag = transparent ? MyDecalFlags.Transparent : MyDecalFlags.None;

            bool visibleDecals;
            if (visibleRenderIDs.Count > m_decals.Count)
                visibleDecals = IterateDecals(visibleRenderIDs, targetFlag, sinceStartTs);
            else
                visibleDecals = IterateVisibleRenderIDs(visibleRenderIDs, targetFlag, sinceStartTs);

            if (!visibleDecals)
                return;

            DrawInternal(transparent, sinceStartTs);
        }

        /// <returns>True if visible decals are found</returns>
        private static bool IterateVisibleRenderIDs(HashSet<uint> visibleRenderIDs, MyDecalFlags targetFlag, uint sinceStartTs)
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
                        RemoveDecalByNode(node);
                        continue;
                    }

                    MyDecalFlags flag = node.Value.Flags & MyDecalFlags.Transparent;
                    if (flag == targetFlag)
                    {
                        AddDecalNodeForDraw(node);
                        ret = true;
                    }
                }
            }

            return ret;
        }

        /// <returns>True if visible decals are found</returns>
        private static bool IterateDecals(HashSet<uint> visibleRenderIDs, MyDecalFlags targetFlag, uint sinceStartTs)
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

                MyDecalFlags flag = current.Value.Flags & MyDecalFlags.Transparent;
                if (flag == targetFlag && visibleRenderIDs.Contains(current.Value.ParentID))
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
            // Reinsert the node at the end of the queue
            m_decals.Remove(node);
            m_decals.AddLast(node);
            AddDecalForDraw(node.Value);
        }

        unsafe static void DrawInternal(bool transparent, uint sinceStartTs)
        {
            var RC = MyImmediateRC.RC;
            int nPasses = MyStereoRender.Enable ? 2 : 1;
            for (int i = 0; i < nPasses; i++)
            {
                if (!MyStereoRender.Enable)
                {
                    RC.SetCB(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
                    RC.DeviceContext.Rasterizer.SetViewport(0, 0, MyRender11.ViewportResolution.X, MyRender11.ViewportResolution.Y);
                }
                else
                {
                    MyStereoRender.RenderRegion = i == 0 ? MyStereoRegion.LEFT : MyStereoRegion.RIGHT;
                    MyStereoRender.BindRawCB_FrameConstants(RC);
                    MyStereoRender.SetViewport(RC);
                }

                RC.DeviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
                RC.SetIB(m_IB.Buffer, m_IB.Format);
                RC.SetIL(null);
                
                RC.SetVS(m_vs);
                RC.SetDS(MyDepthStencilState.DepthTest);
                RC.DeviceContext.PixelShader.SetSamplers(0, SamplerStates.StandardSamplers);

                var decalCb = MyCommon.GetObjectCB(sizeof(MyDecalConstants) * DECAL_BATCH_SIZE);
                RC.SetCB(2, decalCb);

                foreach (var pair in m_materialsToDraw)
                {
                    PrepareMaterialBatches(RC, pair.Value, sinceStartTs);
                    DrawBatches(RC, pair.Key.Material, pair.Key.Index, transparent);
                    m_jobs.Clear();
                }
            }

            // Clear materials to draw outside eye rendering passes
            foreach (var pair in m_materialsToDraw)
                pair.Value.Clear();

            RC.SetBS(null);
            if (MyStereoRender.Enable)
            {
                RC.SetCB(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
                RC.DeviceContext.Rasterizer.SetViewport(0, 0, MyRender11.ViewportResolution.X, MyRender11.ViewportResolution.Y);
                MyStereoRender.RenderRegion = MyStereoRegion.FULLSCREEN;
            }
        }

        private static void PrepareMaterialBatches(MyRenderContext RC, List<MyScreenDecal> decals, uint sinceStartTs)
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
                    volumeMatrix = decal.OBBox;
                    Vector3D translation = decal.Data.Position - MyRender11.Environment.CameraPosition;
                    volumeMatrix.Translation = translation;
                }
                else
                {
                    MatrixD transform = decal.OBBox * parent.WorldMatrix;
                    transform.Translation = transform.Translation - MyRender11.Environment.CameraPosition;
                    volumeMatrix = transform;
                }

                uint fadeDiff = decal.FadeTimestamp - sinceStartTs;
                float fadeAlpha = decal.FadeTimestamp - sinceStartTs >= DECAL_FADE_DURATION ? 1 : fadeDiff / (float)DECAL_FADE_DURATION;
                m_jobs.Add(new MyDecalJob() { WorldMatrix = volumeMatrix, FadeAlpha = fadeAlpha });

                if (MyRenderProxy.Settings.DebugDrawDecals)
                {
                    MatrixD worldMatrix;
                    if (parent == null)
                    {
                        worldMatrix = decal.OBBox;
                        worldMatrix.Translation = decal.Data.Position;
                    }
                    else
                    {
                        worldMatrix = decal.OBBox * parent.WorldMatrix;
                    }

                    MyRenderProxy.DebugDrawAxis(worldMatrix, 0.2f, false, true);
                    MyRenderProxy.DebugDrawOBB(worldMatrix, Color.Blue, 0.1f, false, false);

                    Vector3 position = worldMatrix.Translation;
                    MyRenderProxy.DebugDrawText3D(position, decal.SourceTarget, Color.White, 1, false);
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

        static void BindResources(MyRenderContext RC)
        {
            RC.BindGBufferForWrite(MyGBuffer.Main, DepthStencilAccess.ReadOnly);
            RC.BindSRVs(0, MyGBuffer.Main.DepthStencil.Depth, MyRender11.Gbuffer1Copy);
        }

        static void BindResourcesTransparentBillboards(MyRenderContext RC)
        {
            RC.BindRawSRV(0, MyGBuffer.Main.DepthStencil.Depth);
            RC.BindRawSRV(1, MyRender11.Gbuffer1Copy);
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
