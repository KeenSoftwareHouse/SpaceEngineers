using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using VRage.Utils;
using VRageMath;
using VRage.Voxels;
using VRageRender.Import;

namespace VRageRender
{
    [StructLayout(LayoutKind.Explicit, Size=48)]
    internal struct MyObjectDataVoxelCommon
    {
        [FieldOffset(0)]internal float VoxelLodSize;
        [FieldOffset(4)]internal Vector3 VoxelOffset;
        [FieldOffset(16)]internal Vector4 MassiveCenterRadius;
        [FieldOffset(32)]internal Vector3 VoxelScale;
        [FieldOffset(44)]internal float _padding;

        internal static MyObjectDataVoxelCommon Invalid = new MyObjectDataVoxelCommon { VoxelLodSize = float.NaN, VoxelOffset = Vector3.Zero, MassiveCenterRadius = Vector4.Zero, VoxelScale = Vector3.One};

        internal bool IsValid { get { return VoxelLodSize.IsValid(); } }
    }

    internal sealed class MyVoxelRenderableComponent : MyRenderableComponent
    {
        internal Vector3 m_voxelScale;
        internal Vector3 m_voxelOffset;
        internal Vector3D m_massiveCenter;
        internal float m_massiveRadius;

        internal override void Construct()
        {
            base.Construct();

            m_massiveRadius = 0.0f;
            m_massiveCenter = Vector3D.Zero;
            m_voxelScale = Vector3.Zero;
            m_voxelOffset = Vector3.Zero;
        }

        internal override bool RebuildLodProxy(int lodNum, bool skinningEnabled, MySkinningComponent skinning)
        {
            Debug.Assert(Mesh.Info.LodsNum == 1);

            MyRenderLod lod = null;

            int partCount;
            LodMeshId lodMesh = new LodMeshId();
            VertexLayoutId vertexLayout;

            if (!Owner.IsVisible)
                return false;

            lodMesh = MyMeshes.GetLodMesh(Mesh, 0);
            vertexLayout = lodMesh.VertexLayout;
            partCount = lodMesh.Info.PartsNum;

            MyObjectPoolManager.Init(ref m_lods[lodNum]);
            lod = m_lods[lodNum];
            lod.VertexLayout1 = vertexLayout;

            AddToRenderables();

            Debug.Assert(partCount > 0);

            lod.VertexShaderFlags = MyShaderUnifiedFlags.USE_VOXEL_DATA | MyShaderUnifiedFlags.DITHERED ;//| MyShaderUnifiedFlags.USE_VOXEL_MORPHING;

            bool initializeProxies = true;
            bool initializeDepthProxy = true;

            int numToInitialize = (initializeProxies ? partCount : 0) + (initializeDepthProxy ? 1 : 0);
            if (numToInitialize > 0)
                lod.AllocateProxies(numToInitialize);

            AnyDrawOutsideViewDistance = false;

            int constantBufferSize = GetConstantBufferSize(lod, skinningEnabled);

            if (initializeProxies)
            {
                for (int partIndex = 0; partIndex < partCount; partIndex++)
                {
                    CreateRenderableProxyForPart(lodNum, constantBufferSize, partIndex, partIndex, false);
                }
            }
            if (initializeDepthProxy)
                CreateRenderableProxyForPart(lodNum, constantBufferSize, numToInitialize - 1, 0, true);

            return true;
        }

        private void CreateRenderableProxyForPart(int lodIndex, int objectConstantsSize, int proxyIndex, int partIndex, bool shadowsOnly)
        {
            MyRenderLod lod = m_lods[lodIndex];
            var partId = MyMeshes.GetVoxelPart(Mesh, partIndex);
            var technique = partId.Info.MaterialTriple.IsMultimaterial() && !shadowsOnly ? MyMeshDrawTechnique.VOXEL_MAP_MULTI : MyMeshDrawTechnique.VOXEL_MAP_SINGLE;

            MyRenderableProxy renderableProxy = lod.RenderableProxies[proxyIndex];

            renderableProxy.WorldMatrix = Owner.WorldMatrix;
            //lod.RenderableProxies[p].ObjectData.LocalMatrix = m_owner.WorldMatrix;

            renderableProxy.NonVoxelObjectData = MyObjectDataNonVoxel.Invalid;
            renderableProxy.VoxelCommonObjectData.VoxelOffset = m_voxelOffset;
            renderableProxy.VoxelCommonObjectData.MassiveCenterRadius = Vector4.Zero; // Set in UpdateLodState
            renderableProxy.VoxelCommonObjectData.VoxelScale = m_voxelScale;
            renderableProxy.CommonObjectData.LOD = (uint)m_voxelLod;

            MyStringId shaderMaterial = MyMaterialShaders.MapTechniqueToShaderMaterial(technique);

            Mesh.AssignLodMeshToProxy(renderableProxy);
            AssignShadersToProxy(renderableProxy, shaderMaterial, lod.VertexLayout1, lod.VertexShaderFlags | MapTechniqueToShaderMaterialFlags(technique) | MyShaderUnifiedFlags.DITHERED);

            var partInfo = partId.Info;

            MyDrawSubmesh drawSubmesh;
            if (shadowsOnly)
            {
                drawSubmesh = new MyDrawSubmesh
                {
                    BaseVertex = 0,
                    StartIndex = 0,
                    IndexCount = Mesh.GetIndexCount(),
                    BonesMapping = null,
                    MaterialId = MyVoxelMaterials1.GetMaterialProxyId(partId.Info.MaterialTriple),
                    Flags = MyDrawSubmesh.MySubmeshFlags.Depth
                };
            }
            else
            {
                drawSubmesh = new MyDrawSubmesh
                {
                    BaseVertex = partInfo.BaseVertex,
                    StartIndex = partInfo.StartIndex,
                    IndexCount = partInfo.IndexCount,
                    BonesMapping = null,
                    MaterialId = MyVoxelMaterials1.GetMaterialProxyId(partId.Info.MaterialTriple),
                    Flags = MyDrawSubmesh.MySubmeshFlags.Gbuffer | MyDrawSubmesh.MySubmeshFlags.Forward
                };
            }

            lod.RenderableProxies[proxyIndex].DrawSubmesh = drawSubmesh;
            lod.RenderableProxies[proxyIndex].SkinningMatrices = null;

            lod.RenderableProxies[proxyIndex].ObjectBuffer = MyCommon.GetObjectCB(objectConstantsSize);
            lod.RenderableProxies[proxyIndex].InstanceCount = m_instanceCount;
            lod.RenderableProxies[proxyIndex].StartInstance = m_startInstance;
            lod.RenderableProxies[proxyIndex].Flags = MapTechniqueToRenderableFlags(technique) | m_additionalFlags;
            lod.RenderableProxies[proxyIndex].Type = MapTechniqueToMaterialType(technique);
            lod.RenderableProxies[proxyIndex].Parent = this;
            lod.RenderableProxies[proxyIndex].Lod = 0;
            lod.RenderableProxies[proxyIndex].Instancing = m_instancing;

            AnyDrawOutsideViewDistance |= lod.RenderableProxies[proxyIndex].Flags.HasFlags(MyRenderableProxyFlags.DrawOutsideViewDistance);

            ulong sortingKey = 0;

            My64BitValueHelper.SetBits(ref sortingKey, 36, 2, (ulong)lod.RenderableProxies[proxyIndex].Type);
            My64BitValueHelper.SetBits(ref sortingKey, 32, 4, (ulong)drawSubmesh.MaterialId.Index);
            My64BitValueHelper.SetBits(ref sortingKey, 26, 6, (ulong)MyShaderMaterial.GetID(MyMaterialShaders.MapTechniqueToShaderMaterial(technique).String));
            My64BitValueHelper.SetBits(ref sortingKey, 22, 4, (ulong)m_voxelLod);
            My64BitValueHelper.SetBits(ref sortingKey, 16, 6, (ulong)lod.VertexShaderFlags);
            //My64BitValueHelper.SetBits(ref sortingKey, 14, 6, (ulong)lod.VertexLayout1.Index);
            //My64BitValueHelper.SetBits(ref sortingKey, 0, 14, (ulong)m_owner.ID);      

            lod.SortingKeys[proxyIndex] = sortingKey;
        }

        internal void SetVoxelLod(int lod, MyClipmapScaleEnum scaleEnum)
        {
            m_voxelLod = lod;

            UpdateProxiesCustomAlpha();
        }

        internal override void UpdateLodState()
        {
            base.UpdateLodState();

            if (m_lods == null)
                return;

            if (IsValidVoxelLod(m_voxelLod))
            {
                Vector4 massiveCenterRadius = new Vector4(
                            (float)(m_massiveCenter.X - MyRender11.Environment.Matrices.CameraPosition.X),
                            (float)(m_massiveCenter.Y - MyRender11.Environment.Matrices.CameraPosition.Y),
                            (float)(m_massiveCenter.Z - MyRender11.Environment.Matrices.CameraPosition.Z),
                            m_massiveRadius);

                foreach (MyRenderLod lod in m_lods)
                {
                    foreach (MyRenderableProxy renderableProxy in lod.RenderableProxies)
                    {
                        renderableProxy.VoxelCommonObjectData.MassiveCenterRadius = massiveCenterRadius;
                        renderableProxy.CommonObjectData.LOD =(uint)m_voxelLod;
                    }
                }
            }
        }

        protected override void UpdateProxiesCustomAlpha()
        {
            base.UpdateProxiesCustomAlpha();

            if(m_lods == null)
                return;

            foreach(var lod in m_lods)
            {
                foreach(var renderableProxy in lod.RenderableProxies)
                    renderableProxy.VoxelCommonObjectData.VoxelLodSize = m_voxelLod;
            }
        }

        protected override void AddToRenderables()
        {
            if (m_btreeProxy != MyDynamicAABBTreeD.NullNode)
                return;

            if (MyScene.SeparateGeometry)
                m_btreeProxy = MyScene.StaticRenderablesDBVH.AddProxy(ref Owner.Aabb, m_cullProxy, 0);
            else
                base.AddToRenderables();
        }

        protected override bool MoveRenderableAABB()
        {
            if (m_btreeProxy == MyDynamicAABBTreeD.NullNode)
                return false;

            bool proxyMoved = false;

            if (MyScene.SeparateGeometry)
                proxyMoved = MyScene.StaticRenderablesDBVH.MoveProxy(m_btreeProxy, ref Owner.Aabb, Vector3.Zero);
            else
                proxyMoved = base.MoveRenderableAABB();

            return proxyMoved;
        }

        protected override void RemoveFromRenderables()
        {
            if (m_btreeProxy == MyDynamicAABBTreeD.NullNode)
                return;

            if (MyScene.SeparateGeometry)
            {
                MyScene.StaticRenderablesDBVH.RemoveProxy(m_btreeProxy);
                m_btreeProxy = MyDynamicAABBTreeD.NullNode;
            }
            else
                base.RemoveFromRenderables();
        }

        private static bool IsValidVoxelLod(int voxelLod)
        {
            return voxelLod >= 0;
        }
    }
}
