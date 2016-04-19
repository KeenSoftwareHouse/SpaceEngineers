using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using VRage.Utils;
using VRageMath;
using VRageRender.Resources;
using DecalNode = System.Collections.Generic.LinkedListNode<VRageRender.MyScreenDecal>;

namespace VRageRender
{
    class MyScreenDecal
    {
        public Matrix LocalOBB;
        public uint ID; // backref
        public uint ParentID;
        public MyStringId Material;
    }

    struct MyDecalTextures
    {
        internal MyScreenDecalType DecalType;
        internal TexId NormalmapTexture;
        internal TexId ColorMetalTexture;
        internal TexId AlphamaskTexture;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MyDecalConstants
    {
        internal Matrix WorldMatrix;
        internal Matrix InvWorldMatrix;
    }
    
    static class MyScreenDecals
    {
        const int DECAL_BATCH_SIZE = 512;

        static int m_decalsQueueSize = 1024;

        static IndexBufferId m_IB = IndexBufferId.NULL;

        static VertexShaderId m_vs = VertexShaderId.NULL;
        static PixelShaderId m_psNormalMap = PixelShaderId.NULL;
        static PixelShaderId m_psColorMap = PixelShaderId.NULL;
        static PixelShaderId m_psNormalColorMap = PixelShaderId.NULL;

        static Dictionary<uint, DecalNode> m_nodeMap = new Dictionary<uint, DecalNode>();
        static Dictionary<uint, List<DecalNode>> m_entityDecals = new Dictionary<uint, List<DecalNode>>();
        static LinkedList<MyScreenDecal> m_decals = new LinkedList<MyScreenDecal>();
        static Dictionary<MyStringId, MyDecalTextures> m_materials = new Dictionary<MyStringId, MyDecalTextures>(MyStringId.Comparer);

        static List<Matrix> m_matrices = new List<Matrix>();
        static Dictionary<MyStringId, List<MyScreenDecal>> m_materialsToDraw = new Dictionary<MyStringId, List<MyScreenDecal>>(MyStringId.Comparer);

        internal static void Init()
        {
            m_vs = MyShaders.CreateVs("decal.hlsl");
            var normalMapMacro = new ShaderMacro("USE_NORMALMAP_DECAL", null);
            var colorMapMacro = new ShaderMacro("USE_COLORMAP_DECAL", null);
            m_psColorMap = MyShaders.CreatePs("decal.hlsl", new ShaderMacro[] { colorMapMacro, new ShaderMacro("USE_DUAL_SOURCE_BLENDING", null) });
            m_psNormalMap = MyShaders.CreatePs("decal.hlsl", new ShaderMacro[] { normalMapMacro });
            m_psNormalColorMap = MyShaders.CreatePs("decal.hlsl", new ShaderMacro[] { normalMapMacro, colorMapMacro });

            InitIB();
        }

        internal static void OnSessionEnd()
        {
            m_nodeMap.Clear();
            m_entityDecals.Clear();
            m_decals.Clear();
            m_materials.Clear();
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

        public static void AddDecal(uint ID, uint ParentID, Matrix localOBB, string material)
        {
            if (m_decals.Count == m_decalsQueueSize)
                RemoveDecalByNode(m_decals.First);

            MyScreenDecal decal = new MyScreenDecal();
            decal.ID = ID;
            decal.ParentID = ParentID;
            decal.LocalOBB = localOBB;
            decal.Material = X.TEXT(material);

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

            // FIX-ME Add proper support for voxel maps and re-enable some sanity checks
            //Debug.Assert(MyIDTracker<MyActor>.FindByID(ParentID) != null, "Decal added to non-existing render entity");
        }

        public static void RemoveDecal(uint ID)
        {
            DecalNode node;
            bool found = m_nodeMap.TryGetValue(ID, out node);
            if (!found)
                return;

            RemoveDecalByNode(node);
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

        private static void RemoveDecalByNode(DecalNode node)
        {
            MyScreenDecal decal = node.Value;
            m_entityDecals[decal.ParentID].Remove(node);
            m_decals.Remove(node);
            m_nodeMap.Remove(decal.ID);
        }

        internal static void RegisterMaterials(List<string> names, List<MyDecalMaterialDesc> descriptions)
        {
            Debug.Assert(names.Count == descriptions.Count);

            for (int i = 0; i < names.Count; ++i)
            {
                m_materials[X.TEXT(names[i])] = new MyDecalTextures
                {
                    DecalType = descriptions[i].DecalType,
                    NormalmapTexture = MyTextures.GetTexture(descriptions[i].NormalmapTexture, MyTextureEnum.NORMALMAP_GLOSS),
                    ColorMetalTexture = MyTextures.GetTexture(descriptions[i].ColorMetalTexture, MyTextureEnum.COLOR_METAL),
                    AlphamaskTexture = MyTextures.GetTexture(descriptions[i].AlphamaskTexture, MyTextureEnum.ALPHAMASK),
                };
            }
        }

        unsafe static void DrawBatches(MyRenderContext RC, MyStringId material)
        {
            if (m_matrices.Count == 0)
                return;

            var matDesc = m_materials[material];

            switch (matDesc.DecalType)
            {
                case MyScreenDecalType.NormalMap:
                    BindResources(RC, false);
                    RC.SetPS(m_psNormalMap);
                    RC.SetBS(MyRender11.BlendDecalNormal);
                    break;
                case MyScreenDecalType.ColorMap:
                    BindResources(RC, true);
                    RC.SetPS(m_psColorMap);
                    RC.SetBS(MyRender11.BlendDecalColor);
                    break;
                case MyScreenDecalType.NormalColorMap:
                    BindResources(RC, false);
                    RC.SetPS(m_psNormalColorMap);
                    RC.SetBS(MyRender11.BlendDecalNormalColor);
                    break;
                default:
                    throw new Exception("Unknown decal type");
            }

            // factor 1 makes overwriting of gbuffer color & subtracting from ao
            RC.DeviceContext.PixelShader.SetShaderResource(3, MyTextures.GetView(matDesc.AlphamaskTexture));
            RC.DeviceContext.PixelShader.SetShaderResource(4, MyTextures.GetView(matDesc.ColorMetalTexture));
            RC.DeviceContext.PixelShader.SetShaderResource(5, MyTextures.GetView(matDesc.NormalmapTexture));

            var decalCb = MyCommon.GetObjectCB(sizeof(MyDecalConstants) * DECAL_BATCH_SIZE);

            int batchCount = m_matrices.Count / DECAL_BATCH_SIZE + 1;
            int offset = 0;
            for (int i1 = 0; i1 < batchCount; i1++)
            {
                var mapping = MyMapping.MapDiscard(decalCb);

                int leftDecals = m_matrices.Count - offset;
                int decalCount = leftDecals > DECAL_BATCH_SIZE ? DECAL_BATCH_SIZE : leftDecals;
                for (int i2 = 0; i2 < decalCount; ++i2)
                {
                    Matrix worldMatrix = Matrix.Transpose(m_matrices[i2]);
                    Matrix transposeInverseMatrix = Matrix.Transpose(Matrix.Invert(m_matrices[i2]));
                    mapping.WriteAndPosition(ref worldMatrix);
                    mapping.WriteAndPosition(ref transposeInverseMatrix);
                }

                mapping.Unmap();

                // Draw a box without buffer: 36 vertices -> 12 triangles. 2 triangles per face -> 6 faces
                MyImmediateRC.RC.DeviceContext.DrawIndexed(36 * decalCount, 0, 0);

                offset += DECAL_BATCH_SIZE;
            }

            m_matrices.Clear();
        }

        internal static void Draw()
        {
            if (m_decals.Count == 0)
                return;

            foreach (MyScreenDecal decal in m_decals)
                AddDecalForDraw(decal);

            DrawInternal();
        }

        // ENABLE-ME: As soon as a relieble list of frustum visible ojects IDs is available
        internal static void Draw(HashSet<uint> visibleRenderIDs)
        {
            if (m_decals.Count == 0)
                return;

            bool visibleDecals;
            if (visibleRenderIDs.Count > m_decals.Count)
                visibleDecals = IterateDecals(visibleRenderIDs);
            else
                visibleDecals = IterateVisibleRenderIDs(visibleRenderIDs);

            if (!visibleDecals)
                return;

            DrawInternal();
        }

        /// <returns>True if visible decals are found</returns>
        private static bool IterateVisibleRenderIDs(HashSet<uint> visibleRenderIDs)
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
                    AddDecalNodeForDraw(node);
                    ret = true;
                }
            }

            return ret;
        }

        /// <returns>True if visible decals are found</returns>
        private static bool IterateDecals(HashSet<uint> visibleRenderIDs)
        {
            bool ret = false;
            int count = m_decals.Count;
            DecalNode current = m_decals.First;
            int it = 0;
            while (current != null && it < count)
            {
                DecalNode next = current.Next;

                if (visibleRenderIDs.Contains(current.Value.ParentID))
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

        unsafe static void DrawInternal()
        {
            // copy gbuffer with normals for read
            // bind copy and depth for read
            // bind gbuffer for write
            var RC = MyImmediateRC.RC;
            RC.DeviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            RC.SetIB(m_IB.Buffer, m_IB.Format);
            RC.SetIL(null);
            RC.DeviceContext.Rasterizer.SetViewport(0, 0, MyRender11.ViewportResolution.X, MyRender11.ViewportResolution.Y);
            RC.SetCB(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            
            RC.SetVS(m_vs);
            RC.SetDS(MyDepthStencilState.DepthTest);
            RC.DeviceContext.PixelShader.SetSamplers(0, MyRender11.StandardSamplers);

            var decalCb = MyCommon.GetObjectCB(sizeof(MyDecalConstants) * DECAL_BATCH_SIZE);
            RC.SetCB(2, decalCb);

            foreach (var pair in m_materialsToDraw)
            {
                PrepareMaterialBatches(RC, pair.Key, pair.Value);
                DrawBatches(RC, pair.Key);
            }

            RC.SetBS(null);
        }

        private static void PrepareMaterialBatches(MyRenderContext RC, MyStringId material, List<MyScreenDecal> decals)
        {
            if (decals.Count == 0)
                return;

            foreach (MyScreenDecal decal in decals)
            {
                var parent = MyIDTracker<MyActor>.FindByID(decal.ParentID);

                Matrix volumeMatrix;
                if (parent == null)
                {
                    // FIXME: This is a temporary hack to allow see decals on voxel maps. Not good!
                    // Won't work if the voxels are moving. Better to find a way to locate the actor
                    // in the map
                    volumeMatrix = decal.LocalOBB * Matrix.CreateTranslation(-MyEnvironment.CameraPosition);
                }
                else
                {
                    volumeMatrix = decal.LocalOBB * parent.WorldMatrix * Matrix.CreateTranslation(-MyEnvironment.CameraPosition);
                }

                m_matrices.Add(volumeMatrix);

                if (MyRenderProxy.Settings.DebugDrawDecals)
                {
                    Matrix worldMatrix;
                    if (parent == null)
                        worldMatrix = decal.LocalOBB;
                    else
                        worldMatrix = decal.LocalOBB * parent.WorldMatrix;

                    VRageRender.MyRenderProxy.DebugDrawSphere(worldMatrix.Translation, 0.02f, Color.Red, 0, false);
                    VRageRender.MyRenderProxy.DebugDrawAxis(worldMatrix, 0.2f, false);
                }
            }

            decals.Clear();
        }

        static void AddDecalForDraw(MyScreenDecal decal)
        {
            List<MyScreenDecal> decals;
            bool found = m_materialsToDraw.TryGetValue(decal.Material, out decals);
            if (!found)
            {
                decals = new List<MyScreenDecal>();
                m_materialsToDraw[decal.Material] = decals;
            }

            decals.Add(decal);
        }

        static void BindResources(MyRenderContext RC, bool dualSourceBlending)
        {
            if (dualSourceBlending)
                RC.BindGBuffer0ForWrite(MyGBuffer.Main, DepthStencilAccess.DepthReadOnly);
            else
                RC.BindGBufferForWrite(MyGBuffer.Main, DepthStencilAccess.DepthReadOnly);

            RC.BindSRV(0, MyGBuffer.Main.DepthStencil.Depth);
            RC.DeviceContext.PixelShader.SetShaderResource(1, MyRender11.m_gbuffer1Copy.ShaderView);
        }
    }
}
