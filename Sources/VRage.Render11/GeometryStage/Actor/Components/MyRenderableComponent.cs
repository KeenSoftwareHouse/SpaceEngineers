using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using VRage.Import;
using VRage.Render11.Common;
using VRage.Render11.GeometryStage;
using VRage.Render11.Resources;
using VRage.Utils;
using VRageMath;
using VRageRender.Import;
using VRageRender.Messages;
using Matrix = VRageMath.Matrix;
using Vector3 = VRageMath.Vector3;
using Vector4 = VRageMath.Vector4;


namespace VRageRender
{
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    internal struct MyObjectDataCommon
    {
        internal Vector4 m_row0;
        internal Vector4 m_row1;
        internal Vector4 m_row2;

        internal Vector3 KeyColor;
        internal float CustomAlpha;

        internal Vector3 ColorMul;
        internal float Emissive;

        internal uint LOD;
        internal uint DepthBias;
        internal MyMaterialFlags MaterialFlags;
        float __Padding;

        internal Matrix LocalMatrix
        {
            get
            {
                var row0 = m_row0;
                var row1 = m_row1;
                var row2 = m_row2;

                return new Matrix(
                    row0.X, row1.X, row2.X, 0,
                    row0.Y, row1.Y, row2.Y, 0,
                    row0.Z, row1.Z, row2.Z, 0,
                    row0.W, row1.W, row2.W, 1);

            }
            set
            {
                m_row0 = new Vector4(value.M11, value.M21, value.M31, value.M41);
                m_row1 = new Vector4(value.M12, value.M22, value.M32, value.M42);
                m_row2 = new Vector4(value.M13, value.M23, value.M33, value.M43);
            }
        }
        internal void Translate(Vector3 v)
        {
            m_row0.W += v.X;
            m_row1.W += v.Y;
            m_row2.W += v.Z;
        }
    }

    [StructLayout(LayoutKind.Explicit, Size=32)]
    internal struct MyObjectDataNonVoxel
    {
        [FieldOffset(0)]internal float Facing;
        [FieldOffset(4)]internal Vector2 WindScaleAndFreq;
        [FieldOffset(12)]internal float __padding;
        [FieldOffset(16)]internal Vector3 CenterOffset;
        [FieldOffset(28)]internal float __padding1;

        internal static MyObjectDataNonVoxel Invalid = new MyObjectDataNonVoxel { Facing = float.NaN, WindScaleAndFreq = Vector2.Zero, CenterOffset = Vector3.Zero};

        internal bool IsValid { get { return Facing.IsValid(); } }
    }

    static class My64BitValueHelper
    {
        internal static void SetBits(ref ulong key, int from, int num, ulong value)
        {
            // for clamping
            ulong MASK = ((1u << num) - 1);
            // for clearing
            ulong CMASK = MASK << from;
            key &= (~CMASK);
            key |= ((value & MASK) << from);

            //Debug.Assert((value & MASK) == value);
        }

        internal static ulong GetValue(ref ulong key, int from, int num)
        {
            ulong MASK = ((1u << num) - 1);
            return (key >> from) & MASK;
        }
    }

    struct MyEntityMaterialKey
    {
        internal int LOD;
        internal MyStringId Material;

        #region Equals

        public class MyEntityMaterialKeyComparerType : IEqualityComparer<MyEntityMaterialKey>
        {
            public bool Equals(MyEntityMaterialKey left, MyEntityMaterialKey right)
            {
                return  left.LOD == right.LOD &&
                        left.Material == right.Material;
            }

            public int GetHashCode(MyEntityMaterialKey materialKey)
            {
                return  materialKey.LOD << 28 |
                        materialKey.Material.GetHashCode();
            }
        }
        public static MyEntityMaterialKeyComparerType Comparer = new MyEntityMaterialKeyComparerType();
        #endregion
    }

    enum MyMaterialTypeEnum : uint
    {
        STANDARD = 0,
        FOLIAGE = 1
    }

    [Flags]
    enum MyMaterialFlags : uint
    {
        NONE = 0,
        NO_KEYCOLOR = 2
    }

    struct MyPerMaterialData
    {
        internal MyMaterialTypeEnum Type;
        
        internal int CalculateKey()
        {
            var hash = Type.GetHashCode();
            return hash;
        }
    }

    enum MyRenderableTrees : uint
    {
        DynamicRenderables = 0,
        StaticRenderables = 1,
    }

    static class MyScene
    {
        internal const bool SeparateGeometry = false;
        private static MyDynamicAABBTreeD[] m_renderableTrees;
        internal static MyDynamicAABBTreeD[] RenderableTrees { get { return m_renderableTrees; } }
        internal static MyDynamicAABBTreeD DynamicRenderablesDBVH { get { return m_renderableTrees[(uint)MyRenderableTrees.DynamicRenderables]; } }
        internal static MyDynamicAABBTreeD StaticRenderablesDBVH { get { return m_renderableTrees[(uint)MyRenderableTrees.StaticRenderables]; } }
        internal static readonly MyDynamicAABBTreeD GroupsDBVH = new MyDynamicAABBTreeD(MyRender11Constants.PRUNNING_EXTENSION);
        internal static readonly MyDynamicAABBTreeD FoliageDBVH = new MyDynamicAABBTreeD(MyRender11Constants.PRUNNING_EXTENSION);

        internal static readonly Dictionary<uint, HashSet<MyEntityMaterialKey>> EntityDisabledMaterials = new Dictionary<uint, HashSet<MyEntityMaterialKey>>();

        static MyScene()
        {
            int treeCount = SeparateGeometry ? Enum.GetValues(typeof(MyRenderableTrees)).Length : 1;
            m_renderableTrees = new MyDynamicAABBTreeD[treeCount];
            for (int treeIndex = 0; treeIndex < treeCount; ++treeIndex)
                m_renderableTrees[treeIndex] = new MyDynamicAABBTreeD(MyRender11Constants.PRUNNING_EXTENSION);
        }

        public static void Clear()
        {
            DynamicRenderablesDBVH.Clear();
            if (SeparateGeometry)
                StaticRenderablesDBVH.Clear();

            GroupsDBVH.Clear();
            FoliageDBVH.Clear();

            EntityDisabledMaterials.Clear();
        }
    }

    class MyModelProperties
    {
        internal static readonly float DefaultEmissivity = 0;
        internal static readonly Vector3 DefaultColorMul = Vector3.One;

        internal float Emissivity = DefaultEmissivity;
        internal Vector3 ColorMul = DefaultColorMul;
        
        public void AddTextureChange(MyTextureChange textureChange)
        {
            AddTextureChanges(new MyTextureChange[] { textureChange });
        }

        public void AddTextureChanges(IEnumerable<MyTextureChange> textureChanges)
        {
            if (TextureSwaps == null)
            {
                TextureSwaps = new List<MyTextureChange>(textureChanges);
            }
            else
            {
                foreach (var s in textureChanges)
                {
                    bool swapped = false;
                    for (int i = 0; i < TextureSwaps.Count; ++i)
                    {
                        if (TextureSwaps[i].TextureType == s.TextureType)
                        {
                            TextureSwaps[i] = s;
                            swapped = true;
                            break;
                        }
                    }

                    if (!swapped)
                    {
                        TextureSwaps.Add(s);
                    }
                }
            }
        }

        internal List<MyTextureChange> TextureSwaps = null;
    }

    class MyRenderableComponent : MyActorComponent
    {
        internal static Dictionary<uint, uint> DebrisEntityVoxelMaterial = new Dictionary<uint,uint>();

        #region Fields

        Vector3 m_keyColor;
        internal float m_objectDithering;
        
        internal MeshId Mesh;
        protected InstancingId m_instancing;
        protected MyCullProxy m_cullProxy;
        protected MyRenderLod[] m_lods;
        protected MyCullProxy[] m_renderableProxiesForLodTransition;

        protected int m_btreeProxy;

        protected int m_instanceCount;
        protected int m_startInstance;

        protected int m_lod;
        protected bool m_culled;
        protected float m_lodTransitionState; // [-1,0] or [0,1]
        protected float m_lodTransitionStartDistance;
        protected bool m_lodBorder;

        /// <summary>
        /// Is used in merge-instancing to indicate whether the owning actor has been merged.
        /// </summary>
        protected bool m_isRenderedStandalone;

        internal MyRenderableProxyFlags m_additionalFlags;
        internal MyDrawSubmesh.MySubmeshFlags m_drawFlags = MyDrawSubmesh.MySubmeshFlags.All;
        internal bool AnyDrawOutsideViewDistance;
        protected bool m_colorEmissivityDirty = false;
        internal Dictionary<MyEntityMaterialKey, MyModelProperties> ModelProperties;
        protected bool m_isGenericInstance = false;
        private bool m_perInstanceLods = true;

        internal int m_voxelLod;
        internal byte m_depthBias;

        internal MyRenderLod[] Lods { get { return m_lods; } }

        internal bool IsCulled { get { return m_culled; } }
        internal bool IsRendered { get { return m_isRenderedStandalone && Owner.IsVisible; } }
        internal bool SkipProcessing { get { return m_btreeProxy == -1; } }
        internal int CurrentLod { get { return m_lod; } }

        private bool IsLodTransitionInProgress { get { return m_lodTransitionState != 0; } }
        private int LodTransitionProxyIndex { get { return m_lodTransitionState > 0 ? m_lod : m_lod - 1; } }

        internal bool IsRenderedStandAlone { get { return m_isRenderedStandalone; } set { SetStandaloneRendering(value); } }

        float m_globalEmissivity;
        #endregion

        #region Memory

        internal override void Construct()
        {
            base.Construct();

            Type = MyActorComponentEnum.Renderable;
            //m_mesh = null;

            DeallocateLodProxies();
            if (m_cullProxy != null)
            {
                MyObjectPoolManager.Deallocate(m_cullProxy);
                m_cullProxy = null;
            }

            m_cullProxy = MyObjectPoolManager.Allocate<MyCullProxy>();
            m_btreeProxy = -1;

            Mesh = MeshId.NULL;
            m_instancing = InstancingId.NULL;
            m_isGenericInstance = false;
            
            m_instanceCount = 0;
            m_startInstance = 0;

            m_isRenderedStandalone = true;

            m_keyColor = Vector3.One;
            m_objectDithering = 0;

            if (m_renderableProxiesForLodTransition != null)
                foreach (var cullProxy in m_renderableProxiesForLodTransition)
                    MyObjectPoolManager.Deallocate(cullProxy);

            m_renderableProxiesForLodTransition = null;

            m_lodTransitionState = 0;
            m_lod = 0;

            m_voxelLod = -1;
            m_depthBias = 0;
            m_additionalFlags = 0;

            m_globalEmissivity = 0;

            ModelProperties = new Dictionary<MyEntityMaterialKey, MyModelProperties>(MyEntityMaterialKey.Comparer);

            MyRender11.PendingComponentsToUpdate.Add(this);
        }

        internal override void Destruct()
        {
            RemoveFromRenderables();

            if(m_cullProxy != null)
            {
                MyObjectPoolManager.Deallocate(m_cullProxy);
                m_cullProxy = null;
            }
            DeallocateLodProxies();
            ModelProperties.Clear();
            MyRender11.PendingComponentsToUpdate.Remove(this);

            base.Destruct();
        }

        #endregion

        internal void SetLocalAabbToModelLod(int lod)
        {
            var boundingBox = Mesh.GetBoundingBox(lod);
            if (boundingBox.HasValue)
                Owner.SetLocalAabb(boundingBox.Value);
        }

        internal void SetModel(MeshId mesh)
        {
            Mesh = mesh;

            SetLocalAabbToModelLod(0);

            Owner.MarkRenderDirty();
        }

        public void SetGlobalEmissivity(float emissivity)
        {
            m_globalEmissivity = emissivity;
            m_colorEmissivityDirty = true;
            MyRender11.PendingComponentsToUpdate.Add(this);
        }

        // for now
        internal void SetKeyColor(Vector4 keyColor)
        {
            m_keyColor.X = keyColor.X;
            m_keyColor.Y = keyColor.Y;
            m_keyColor.Z = keyColor.Z;

            UpdateProxiesObjectData();
        }

        internal void SetDithering(float val)
        {
            var oldDithering = m_objectDithering;
            m_objectDithering = val;

            if (!SkipProcessing)
            {
                if (val > 0 && oldDithering <= 0)
                {
                    SetLodShaders(m_lod, MyShaderUnifiedFlags.DITHERED);
                }
                if (val == 0 && oldDithering > 0)
                {
                    SetLodShaders(m_lod, MyShaderUnifiedFlags.NONE);
                }

                UpdateProxiesCustomAlpha();
            }
        }

        internal void UpdateProxiesObjectData()
        {
            if (m_lods == null)
                return;

            for (int it = 0; it < m_lods.Length; it++)
            {
                var lod = m_lods[it];
                if (lod.RenderableProxies == null)
                    continue;


                foreach (var renderableProxy in lod.RenderableProxies)
                {
                    renderableProxy.WorldMatrix = Owner.WorldMatrix;

                    UpdateKeyColor(ref renderableProxy.CommonObjectData, m_keyColor);
                    renderableProxy.CommonObjectData.DepthBias = m_depthBias;
                    renderableProxy.CommonObjectData.LOD = (uint)it;
                }
            }
        }

        internal void UpdateKeyColor(ref MyObjectDataCommon data, Vector3 keyColor)
        {
            data.KeyColor = keyColor;
            if (keyColor.X.IsZero() && keyColor.Y.IsEqual(-1) && keyColor.Z.IsEqual(-1))
                data.MaterialFlags |= MyMaterialFlags.NO_KEYCOLOR;
            else data.MaterialFlags &= ~MyMaterialFlags.NO_KEYCOLOR;
        }

        internal override void OnMatrixChange()
        {
            base.OnMatrixChange();

            if(!Owner.RenderDirty)
                UpdateProxiesObjectData();
        }

        internal override void OnAabbChange()
        {
            base.OnAabbChange();

            if (IsRendered)
                MoveRenderableAABB();
        }

        internal override void OnVisibilityChange()
        {
            base.OnVisibilityChange();

            if (!Owner.IsVisible)
                RemoveFromRenderables();

            if (Owner.IsVisible && IsRendered)
                Owner.MarkRenderDirty();
        }

        internal void SetInstancing(InstancingId instancing)
        {
            if(m_instancing != instancing)
            {
                m_instancing = instancing;
                Owner.MarkRenderDirty();

                m_isGenericInstance = m_instancing == InstancingId.NULL ? false : MyInstancing.Instancings.Data[m_instancing.Index].Type == MyRenderInstanceBufferType.Generic;
                m_perInstanceLods = m_instancing == InstancingId.NULL ? false : MyInstancing.Instancings.Data[m_instancing.Index].PerInstanceLods;
            }
        }

        internal void SetInstancingCounters(int instanceCount, int startInstance)
        {
            m_instanceCount = instanceCount;
            m_startInstance = startInstance;

            if (m_lods == null)
                return;

            for (int i = 0; i < m_lods.Length; i++)
            {
                for (int j = 0; j < m_lods[i].RenderableProxies.Length; j++)
                {
                    m_lods[i].RenderableProxies[j].InstanceCount = instanceCount;
                    m_lods[i].RenderableProxies[j].StartInstance = startInstance;
                }
            }
        }

        internal MeshId GetModel()
        {
            return Mesh;
        }

        private void SetLodPartShaders(int lodNum, int proxyIndex, MyShaderUnifiedFlags appendedFlags)
        {
            Debug.Assert(!MyMeshes.IsVoxelMesh(Mesh));

            var lod = m_lods[lodNum];

            var partId = MyMeshes.GetMeshPart(Mesh, lodNum, proxyIndex);
            var technique = partId.Info.Material.Info.Technique;

            MyShaderUnifiedFlags flags = appendedFlags;
            if (partId.Info.Material.Info.Facing == MyFacingEnum.Impostor || partId.Info.Material.Info.Facing == MyFacingEnum.Vertical)
                flags |= MyShaderUnifiedFlags.DITHERED_LOD;
            if (partId.Info.Material.Info.Facing == MyFacingEnum.Impostor)
                flags |= MyShaderUnifiedFlags.ALPHA_MASK_ARRAY;
            MyMeshMaterialInfo info = partId.Info.Material.Info;
            if (info.GeometryTextureRef.IsUsed && MyRender11.Settings.UseGeometryArrayTextures)
                flags |= MyShaderUnifiedFlags.USE_TEXTURE_INDICES;

            switch (partId.Info.Material.Info.Technique)
            {
                case MyMeshDrawTechnique.DECAL:
                case MyMeshDrawTechnique.DECAL_NOPREMULT:
                    flags |= MyShaderUnifiedFlags.STATIC_DECAL;
                    break;
                case MyMeshDrawTechnique.DECAL_CUTOUT:
                    flags |= MyShaderUnifiedFlags.STATIC_DECAL_CUTOUT;
                    break;
            }                

            if (DebrisEntityVoxelMaterial.ContainsKey(Owner.ID))
                technique = MyMeshDrawTechnique.VOXELS_DEBRIS;

            MyStringId shaderMaterial = MyMaterialShaders.MapTechniqueToShaderMaterial(technique);

            var renderableProxy = lod.RenderableProxies[proxyIndex];

            AssignShadersToProxy(renderableProxy, shaderMaterial, lod.VertexLayout1, lod.VertexShaderFlags | MapTechniqueToShaderMaterialFlags(technique) | GetCurrentStateMaterialFlags(lodNum) | flags);
        }

        protected void AssignShadersToProxy(MyRenderableProxy renderableProxy, MyStringId shaderMaterial, VertexLayoutId vertexLayoutId, MyShaderUnifiedFlags shaderFlags)
        {
            MyFileTextureEnum textureTypes = MyFileTextureEnum.UNSPECIFIED;
            MyMeshDrawTechnique technique = MyMeshDrawTechnique.MESH;
            if (renderableProxy.Material != MyMeshMaterialId.NULL)
            {
                textureTypes = renderableProxy.Material.Info.TextureTypes;
                technique = renderableProxy.Material.Info.Technique;
            }

            Debug.Assert(renderableProxy != null, "Renderable proxy cannot be null!");
            renderableProxy.Shaders = MyMaterialShaders.Get(
                shaderMaterial,
                MyMaterialShaders.MapTechniqueToDefaultPass(technique),
                vertexLayoutId,
                shaderFlags, textureTypes);
            renderableProxy.DepthShaders = MyMaterialShaders.Get(
                shaderMaterial,
                MyMaterialShaders.DEPTH_PASS_ID,
                vertexLayoutId,
                shaderFlags | MyShaderUnifiedFlags.DEPTH_ONLY, textureTypes);
            renderableProxy.HighlightShaders = MyMaterialShaders.Get(
                shaderMaterial,
                MyMaterialShaders.HIGHLIGHT_PASS_ID,
                vertexLayoutId,
                shaderFlags, textureTypes);
            renderableProxy.ForwardShaders = MyMaterialShaders.Get(
                shaderMaterial,
                MyMaterialShaders.FORWARD_PASS_ID,
                vertexLayoutId,
                shaderFlags, textureTypes);
        }
        
        protected void SetLodShaders(int lodNum, MyShaderUnifiedFlags appendedFlags)
        {
            if (MyMeshes.IsVoxelMesh(Mesh))
                return;

            var partCount = MyMeshes.GetLodMesh(Mesh, lodNum).Info.PartsNum;

            for (int partIndex = 0; partIndex < partCount; ++partIndex)
                SetLodPartShaders(lodNum, partIndex, appendedFlags);
        }

        private Vector4 GetUvScaleOffset(Vector2I uvTiles, Vector2I tileIndex)
        {
            Vector4 scaleOffset = new Vector4(uvTiles.X, uvTiles.Y, (1.0f / uvTiles.X) * tileIndex.X, (1.0f / uvTiles.Y) * tileIndex.Y);
            return scaleOffset;
        }

        // Returns false if proxy could/should not be built. In this case, it will not allocate a new MyRenderLod
        internal virtual unsafe bool RebuildLodProxy(int lodNum,
            bool skinningEnabled, MySkinningComponent skinning)
        {
            var lodMesh = MyMeshes.GetLodMesh(Mesh, lodNum);
            var lodMeshInfo = lodMesh.Info;

            if (lodMeshInfo.NullLodMesh)
            {
                Debug.Assert(m_lods[lodNum] == null);
                return false;
            }

            MyObjectPoolManager.Init(ref m_lods[lodNum]);

            AddToRenderables();

            var lod = m_lods[lodNum];
            lod.Distance = lodMesh.Info.LodDistance;

            Matrix[] skinningMatrices = null;
            var vsFlags = MyShaderUnifiedFlags.NONE;
            if (skinningEnabled)
            {
                skinningMatrices = skinning.SkinMatrices;
                vsFlags |= MyShaderUnifiedFlags.USE_SKINNING;
            }

            if (m_instancing != InstancingId.NULL)
            {
                lod.VertexLayout1 = MyVertexLayouts.GetLayout(lodMesh.VertexLayout, m_instancing.Info.Layout);

                if (m_instancing.Info.Type == MyRenderInstanceBufferType.Cube)
                {
                    if (lodMesh.VertexLayout.HasBonesInfo)
                    {
                        vsFlags |= MyShaderUnifiedFlags.USE_DEFORMED_CUBE_INSTANCING;
                    }
                    else
                    {
                        vsFlags |= MyShaderUnifiedFlags.USE_CUBE_INSTANCING;
                    }
                }
                else if (m_instancing.Info.Type == MyRenderInstanceBufferType.Generic)
                {
                    vsFlags |= MyShaderUnifiedFlags.USE_GENERIC_INSTANCING;
                }
            }
            else
            {
                lod.VertexLayout1 = lodMesh.VertexLayout;
            }
            lod.VertexShaderFlags = vsFlags;

            var partCount = lodMeshInfo.PartsNum;
            Debug.Assert(partCount > 0);

            lod.AllocateProxies(partCount);

            for (int partIndex = 0; partIndex < partCount; ++partIndex )
                CreateRenderableProxyForPart(lodNum, partIndex, GetConstantBufferSize(lod, skinningEnabled), skinningMatrices);

            return true;
        }

        private void CreateRenderableProxyForPart(int lodIndex, int partIndex, int objectConstantsSize, Matrix[] skinningMatrices)
        {
            MyRenderLod lod = m_lods[lodIndex];
            var lodMesh = MyMeshes.GetLodMesh(Mesh, lodIndex);
            var partId = MyMeshes.GetMeshPart(Mesh, lodIndex, partIndex);
            var technique = partId.Info.Material.Info.Technique;

            var voxelMaterialId = -1;
            if (DebrisEntityVoxelMaterial.ContainsKey(Owner.ID))
            {
                technique = MyMeshDrawTechnique.VOXEL_MAP_SINGLE;
                voxelMaterialId = (int)DebrisEntityVoxelMaterial[Owner.ID];
            }

            lod.RenderableProxies[partIndex].CommonObjectData.Emissive = MyModelProperties.DefaultEmissivity;
            lod.RenderableProxies[partIndex].CommonObjectData.ColorMul = MyModelProperties.DefaultColorMul;
            lod.RenderableProxies[partIndex].CommonObjectData.DepthBias = m_depthBias;
            lod.RenderableProxies[partIndex].CommonObjectData.LOD = (uint)lodIndex;
            lod.RenderableProxies[partIndex].NonVoxelObjectData.Facing = (byte)partId.Info.Material.Info.Facing;
            lod.RenderableProxies[partIndex].NonVoxelObjectData.WindScaleAndFreq = partId.Info.Material.Info.WindScaleAndFreq;

            lod.RenderableProxies[partIndex].VoxelCommonObjectData = MyObjectDataVoxelCommon.Invalid;

            if ((partId.Info.Material.Info.Facing == MyFacingEnum.Full) || (partId.Info.Material.Info.Facing == MyFacingEnum.Impostor))
            {
                lod.RenderableProxies[partIndex].NonVoxelObjectData.CenterOffset = partId.Info.CenterOffset;
            }
            else
            {
                lod.RenderableProxies[partIndex].NonVoxelObjectData.CenterOffset = Vector3.Zero;
            }

            MyStringId shaderMaterial = MyMaterialShaders.MapTechniqueToShaderMaterial(technique);

            var partInfo = partId.Info;
            lod.RenderableProxies[partIndex].WorldMatrix = Owner.WorldMatrix;
            lod.RenderableProxies[partIndex].Mesh = lodMesh;
            lod.RenderableProxies[partIndex].Material = partInfo.Material;
            lod.RenderableProxies[partIndex].UnusedMaterials = partInfo.UnusedMaterials ?? new HashSet<string>();

            SetLodPartShaders(lodIndex, partIndex, MyShaderUnifiedFlags.NONE);

            MyDrawSubmesh submesh = new MyDrawSubmesh
            {
                BaseVertex = partInfo.BaseVertex,
                StartIndex = partInfo.StartIndex,
                IndexCount = partInfo.IndexCount,
                BonesMapping = partInfo.BonesMapping,
                MaterialId = MyMeshMaterials1.GetProxyId(partInfo.Material),
                Flags = m_drawFlags
            };

            if (voxelMaterialId != -1)
            {
                bool isFillingMaterialCB = technique == MyMeshDrawTechnique.VOXELS_DEBRIS;
                submesh.MaterialId = MyVoxelMaterials1.GetMaterialProxyId(new MyVoxelMaterialTriple(voxelMaterialId, -1, -1, isFillingMaterialCB));
            }

            lod.RenderableProxies[partIndex].DrawSubmesh = submesh;

            var sectionSubmeshes = new MyDrawSubmesh[partId.Info.SectionSubmeshCount];
            int subsectionIndex = 0;
            foreach (var sectionName in lodMesh.Info.SectionNames)
            {
                var section = MyMeshes.GetMeshSection(Mesh, lodIndex, sectionName);
                MyMeshSectionPartInfo1[] meshes = section.Info.Meshes;
                for (int idx2 = 0; idx2 < meshes.Length; idx2++)
                {
                    if (meshes[idx2].PartIndex != partIndex)
                        continue;

                    sectionSubmeshes[subsectionIndex] = new MyDrawSubmesh
                    {
                        BaseVertex = meshes[idx2].BaseVertex,
                        StartIndex = meshes[idx2].StartIndex,
                        IndexCount = meshes[idx2].IndexCount,
                        BonesMapping = submesh.BonesMapping,
                        MaterialId = submesh.MaterialId,
                        Flags = submesh.Flags
                    };

                    subsectionIndex++;
                }
            }

            lod.RenderableProxies[partIndex].SectionSubmeshes = sectionSubmeshes;

            lod.RenderableProxies[partIndex].SkinningMatrices = skinningMatrices;

            lod.RenderableProxies[partIndex].ObjectBuffer = MyCommon.GetObjectCB(objectConstantsSize);
            lod.RenderableProxies[partIndex].InstanceCount = m_instanceCount;
            lod.RenderableProxies[partIndex].StartInstance = m_startInstance;
            lod.RenderableProxies[partIndex].Flags = MapTechniqueToRenderableFlags(technique) | m_additionalFlags;
            lod.RenderableProxies[partIndex].Type = MapTechniqueToMaterialType(technique);
            lod.RenderableProxies[partIndex].Parent = this;
            lod.RenderableProxies[partIndex].Lod = lodIndex;
            lod.RenderableProxies[partIndex].Instancing = m_instancing;

            AnyDrawOutsideViewDistance |= (lod.RenderableProxies[partIndex].Flags & MyRenderableProxyFlags.DrawOutsideViewDistance) != 0;

            MyPerMaterialData materialData;
            materialData.Type = 0;
            FillPerMaterialData(ref materialData, technique);
            lod.RenderableProxies[partIndex].PerMaterialIndex = MySceneMaterials.GetPerMaterialDataIndex(ref materialData);

            ulong sortingKey = 0;

            ulong decalBit;
            switch(technique)
            {
                case MyMeshDrawTechnique.DECAL:
                case MyMeshDrawTechnique.DECAL_NOPREMULT:
                case MyMeshDrawTechnique.DECAL_CUTOUT:
                    decalBit = 1;
                    break;
                default:
                    decalBit = 0;
                    break;
            }

            My64BitValueHelper.SetBits(ref sortingKey, 38, 1, decalBit);
            My64BitValueHelper.SetBits(ref sortingKey, 36, 2, (ulong)lod.RenderableProxies[partIndex].Type);
            My64BitValueHelper.SetBits(ref sortingKey, 32, 4, (ulong)lod.RenderableProxies[partIndex].PerMaterialIndex);
            My64BitValueHelper.SetBits(ref sortingKey, 26, 6, (ulong)MyShaderMaterial.GetID(MyMaterialShaders.MapTechniqueToShaderMaterial(technique).String));
            My64BitValueHelper.SetBits(ref sortingKey, 20, 6, (ulong)lod.VertexShaderFlags);
            My64BitValueHelper.SetBits(ref sortingKey, 14, 6, (ulong)lod.VertexLayout1.Index);
            My64BitValueHelper.SetBits(ref sortingKey, 7, 7, (ulong)lod.RenderableProxies[partIndex].Mesh.Index);
            My64BitValueHelper.SetBits(ref sortingKey, 0, 7, (ulong)lod.RenderableProxies[partIndex].Material.GetHashCode());

            lod.SortingKeys[partIndex] = sortingKey;
        }

        internal static MyMaterialType ExtractTypeFromSortingKey(ulong sk)
        {
            return (MyMaterialType)((sk >> 36) & 0x3);
        }

        internal static string ExtractMaterialNameFromSortingKey(ulong sk)
        {
            int id = (int)(sk >> 26) & 0x3F;
            return MyShaderMaterial.GetNameByID(id);
        }

        private MyShaderUnifiedFlags GetCurrentStateMaterialFlags(int lodNum) 
        { 
            return m_objectDithering > 0 ? MyShaderUnifiedFlags.DITHERED : MyShaderUnifiedFlags.NONE;
        }

        protected static unsafe int GetConstantBufferSize(MyRenderLod lod, bool skinningEnabled)
        {
            int objectConstantsSize = sizeof(MyObjectDataCommon);
            if ((lod.VertexShaderFlags & MyShaderUnifiedFlags.USE_VOXEL_DATA) != MyShaderUnifiedFlags.USE_VOXEL_DATA)
                objectConstantsSize += sizeof(MyObjectDataNonVoxel);
            else
            {
                objectConstantsSize += sizeof(MyObjectDataVoxelCommon);
            }
            if (skinningEnabled)
                objectConstantsSize += sizeof(Matrix) * MySkinningComponent.ConstantBufferMatrixNum;

            return objectConstantsSize;
        }

        internal bool RebuildRenderProxies()
        {
            bool ready = Mesh != MeshId.NULL;
            if(!ready)
                return false;

            if (!Owner.LocalAabb.HasValue)
            {
                SetLocalAabbToModelLod(0);
            }

            AnyDrawOutsideViewDistance = false;

            var skinning = Owner.GetSkinning();
            bool skinningEnabled = skinning != null && skinning.SkinMatrices != null;

            DeallocateLodProxies();
            MyArrayHelpers.InitOrReserve(ref m_lods, Mesh.Info.LodsNum);

            bool validProxy = true;
            for (int lodIndex = 0; lodIndex < m_lods.Length; ++lodIndex)
            {
                validProxy = validProxy && RebuildLodProxy(lodIndex, skinningEnabled, skinning);
                if (!validProxy)
                    break;
            }

            if(!validProxy)
            {
                Owner.MarkRenderClean();
                DeallocateLodProxies();
                RemoveFromRenderables();
                return false;
            }

            if (m_lods.Length > 0)
            {
                if (m_renderableProxiesForLodTransition != null)
                {
                    for (int cullProxyIndex = 0; cullProxyIndex < m_renderableProxiesForLodTransition.Length; ++cullProxyIndex )
                    {
                        MyObjectPoolManager.Deallocate(m_renderableProxiesForLodTransition[cullProxyIndex]);
                        m_renderableProxiesForLodTransition[cullProxyIndex] = null;
                    }
                }

                if (m_lods.Length < 2)
                    m_renderableProxiesForLodTransition = null;
                else
                    Array.Resize(ref m_renderableProxiesForLodTransition, m_lods.Length - 1);
            }

            m_lod = 0;
            for (int lodIndex = 0; lodIndex < m_lods.Length - 1; ++lodIndex)    // m_renderableProxiesForLodTransition != null only if m_lods.Length is >= 2
            {
                m_renderableProxiesForLodTransition[lodIndex] = MyObjectPoolManager.Allocate<MyCullProxy>();
                var cullProxy = m_renderableProxiesForLodTransition[lodIndex];

                int thisLodProxyCount = m_lods[lodIndex].RenderableProxies.Length;
                int nextLodProxyCount = m_lods[lodIndex + 1].RenderableProxies.Length;
                int transitionProxyCount = thisLodProxyCount + nextLodProxyCount;
                Array.Resize(ref cullProxy.RenderableProxies, transitionProxyCount);
                Array.Copy(m_lods[lodIndex].RenderableProxies, cullProxy.RenderableProxies, thisLodProxyCount);
                Array.Copy(m_lods[lodIndex + 1].RenderableProxies, 0, cullProxy.RenderableProxies, thisLodProxyCount, nextLodProxyCount);

                int thisLodKeyCount = m_lods[lodIndex].SortingKeys.Length;
                int nextLodKeyCount = m_lods[lodIndex + 1].SortingKeys.Length;
                int transitionKeyCount = thisLodKeyCount + nextLodKeyCount;
                Array.Resize(ref cullProxy.SortingKeys, transitionKeyCount);
                Array.Copy(m_lods[lodIndex].SortingKeys, cullProxy.SortingKeys, thisLodKeyCount);
                Array.Copy(m_lods[lodIndex + 1].SortingKeys, 0, cullProxy.SortingKeys, thisLodKeyCount, nextLodKeyCount);
            }

            float currentDistance = CalculateViewerDistance();
            bool isInstancingGeneric = m_instancing == InstancingId.NULL ? false : MyInstancing.Instancings.Data[m_instancing.Index].Type == MyRenderInstanceBufferType.Generic;
            if (MyRender11.Settings.PerInstanceLods && isInstancingGeneric)
            {
                m_lod = m_lods.Length - 1;
            }
            else
            {
                for (int lodIndex = 0; lodIndex < m_lods.Length; ++lodIndex)
                {
                    if (m_lods[lodIndex].Distance <= currentDistance && ((lodIndex == m_lods.Length - 1) || currentDistance < m_lods[lodIndex + 1].Distance))
                    {
                        m_lod = lodIndex;
                    }
                }
            }

            m_lodTransitionState = 0;
            if (m_lods.Length > 0)
            {
                m_cullProxy.RenderableProxies = m_lods[m_lod].RenderableProxies;
                m_cullProxy.SortingKeys = m_lods[m_lod].SortingKeys;
                m_cullProxy.Parent = this;
                UpdateProxiesCustomAlpha();
                UpdateProxiesObjectData();
            }
            else
                m_cullProxy.Clear();

            OnFrameUpdate();

            if(MyScene.EntityDisabledMaterials.ContainsKey(Owner.ID))
            {
                foreach( var row in MyScene.EntityDisabledMaterials[Owner.ID])
                {
                    var submeshes = MyMeshes.GetLodMesh(Mesh, row.LOD).Info.PartsNum;
                    for (int submeshIndex = 0; submeshIndex < submeshes; submeshIndex++)
                    {
                        var part = MyMeshes.GetMeshPart(Mesh, row.LOD, submeshIndex);
                        var proxy = m_lods[row.LOD].RenderableProxies[submeshIndex];

                        if (part.Info.Material.Info.Name == row.Material)
                        {
                            proxy.Flags |= MyRenderableProxyFlags.SkipInMainView;
                        }
                    }
                }
            }

            foreach (var property in ModelProperties)
            {
                var lodCount = Mesh.Info.LodsNum;

                for (var lod = 0; lod < lodCount; ++lod)
                {
                    var submeshes = MyMeshes.GetLodMesh(Mesh, lod).Info.PartsNum;
                    for (int i = 0; i < submeshes; i++)
                    {
                        var part = MyMeshes.GetMeshPart(Mesh, lod, i);
                        var proxy = m_lods[lod].RenderableProxies[i];

                        if (part.Info.Material.Info.Name == property.Key.Material)
                        {
                            proxy.CommonObjectData.Emissive = property.Value.Emissivity;
                            proxy.CommonObjectData.ColorMul = property.Value.ColorMul;

                            if (property.Value.TextureSwaps != null)
                            {
                                var meshMat = part.Info.Material;
                                var info = meshMat.Info;

                                if (info.GeometryTextureRef.IsUsed)
                                {
                                    string errorMessage =
                                        string.Format(
                                            "Material '{0}' needs to be added into Environment.sbc into ChangeableMaterial", part.Info.Material.Info.Name);
                                    MyRenderProxy.Fail(errorMessage);
                                }

                                foreach (var s in property.Value.TextureSwaps)
                                {
                                    switch (s.TextureType)
                                    {
                                        case MyTextureType.NormalGloss:
                                            info.NormalGloss_Texture = MyResourceUtils.GetTextureFullPath(s.TextureName);
                                            break;
                                        case MyTextureType.Extensions:
                                            info.Extensions_Texture = MyResourceUtils.GetTextureFullPath(s.TextureName);
                                            break;
                                        case MyTextureType.Alphamask:
                                            info.Alphamask_Texture = MyResourceUtils.GetTextureFullPath(s.TextureName);
                                            break;
                                        case MyTextureType.ColorMetal:
                                            info.ColorMetal_Texture = MyResourceUtils.GetTextureFullPath(s.TextureName);
                                            break;
                                        default:
                                            throw new Exception();
                                    }
                                }

                                proxy.DrawSubmesh.MaterialId = MyMeshMaterials1.GetProxyId(MyMeshMaterials1.GetMaterialId(ref info));
                            }
                        }
                    }
                }
            }


            // If it is needed, the other branch will do this
            if (m_colorEmissivityDirty && IsRendered && m_lods != null)
            {
                m_colorEmissivityDirty = false;

        		foreach(var lod in m_lods)
                        foreach (var renderableProxy in lod.RenderableProxies)
                        {
                            renderableProxy.CommonObjectData.Emissive = m_globalEmissivity;
                            renderableProxy.CommonObjectData.ColorMul = MyModelProperties.DefaultColorMul;
                        }

                foreach (var property in ModelProperties)
                {
                    foreach (var lod in m_lods)
                    {
                        foreach (var renderableProxy in lod.RenderableProxies)
                        {
                            if (renderableProxy.UnusedMaterials != null && renderableProxy.UnusedMaterials.Contains(property.Key.Material.String))
                            {
                                string errorMessage =
                                    string.Format(
                                    "Material '{0}' needs to be added to environment.sbc into changeable materials", property.Key.Material.String);
                                MyRenderProxy.Assert(false, errorMessage);
                            }
                            if (renderableProxy.Material.Info.Name == property.Key.Material)
                            {
                                renderableProxy.CommonObjectData.Emissive = property.Value.Emissivity;
                                renderableProxy.CommonObjectData.ColorMul = property.Value.ColorMul;
                            }
                        }
                    }
                }
            }

            m_colorEmissivityDirty = false;
            Owner.MarkRenderClean();
            return true;
        }

        public void UpdateColorEmissivity(int lod, string materialName, Color diffuse, float emissivity)
        {
            var key = new MyEntityMaterialKey { LOD = lod, Material = X.TEXT_(materialName) };
            MyModelProperties properties;
            if (!ModelProperties.TryGetValue(key, out properties))
            {
                properties = new MyModelProperties();
                ModelProperties[key] = properties;
            }
            properties.Emissivity = emissivity;
            properties.ColorMul = diffuse;
            m_colorEmissivityDirty = true;
            MyRender11.PendingComponentsToUpdate.Add(this);
        }

        private void DeallocateLodProxies()
        {
            if (m_lods == null)
                return;

            foreach (var lod in m_lods)
            {
                if (lod != null)
                    MyObjectPoolManager.Deallocate(lod);
            }

            m_lods = null;
        }

        protected virtual void UpdateProxiesCustomAlpha()
        {
            if (m_lods == null)
                return;

            foreach (var lod in m_lods)
            {
                if (lod.RenderableProxies == null)
                    continue;

                foreach (var renderableProxy in lod.RenderableProxies)
                {
                    float value = 0;

                    if (m_objectDithering != 0)
                    {
                        value = m_objectDithering;
                    }
                    else if (IsLodTransitionInProgress)
                    {
                        // Value over 1 is interpreted by the shader to do dithering with an inversed mask
                        // This is done so that when blending between two lod levels, one pixel will be from current lod
                        // and the other from the next lod and there are no missing pixels.
                        // Could not use negative because that currently means hologram rendering.
                        // SL: Moved inversed dithering to values of 2 to 3 instead of 1 to 2 to make sure the values don't overlap
                        value = (renderableProxy.Lod == m_lod ? Math.Abs(m_lodTransitionState) : (3.0f - Math.Abs(m_lodTransitionState)));
                    }

                    renderableProxy.CommonObjectData.CustomAlpha = value;
                }
            }
        }

        void SetProxiesForCurrentLod()
        {
            m_cullProxy.RenderableProxies = !IsLodTransitionInProgress ? m_lods[m_lod].RenderableProxies : m_renderableProxiesForLodTransition[LodTransitionProxyIndex].RenderableProxies;
            m_cullProxy.SortingKeys = !IsLodTransitionInProgress ? m_lods[m_lod].SortingKeys : m_renderableProxiesForLodTransition[LodTransitionProxyIndex].SortingKeys;
        }

        internal void UpdateInstanceLods()
        {
            if (MyRender11.Settings.PerInstanceLods && m_perInstanceLods && m_isGenericInstance)
                UpdatePerInstanceLods(CalculateViewerDistance());
        }
        internal virtual void UpdateLodState()
        {
            if (m_lods == null || !IsRendered)
                return;

            var distanceFromCamera = CalculateViewerDistance();

            m_culled = CheckDistanceCulling(distanceFromCamera);
            if (m_culled)
                return;

            if (m_lods.Length == 1 || MyRender11.Settings.SkipLodUpdates)
                return;

            if (!distanceFromCamera.IsValid())
                return;

            if (IsLodTransitionInProgress)
            {
                float delta = MyLodUtils.GetTransitionDelta(Math.Abs(distanceFromCamera - m_lodTransitionStartDistance), m_lodTransitionState, m_lod);
                m_lodTransitionState = Math.Sign(m_lodTransitionState) * (Math.Abs(m_lodTransitionState) + delta);
                
                if (Math.Abs(m_lodTransitionState) >= 1)
                {
                    m_lod = m_lodTransitionState > 0 ? m_lod + 1 : m_lod - 1;
                    m_lodTransitionState = 0;

                    SetProxiesForCurrentLod();
                    SetLodShaders(m_lod, MyShaderUnifiedFlags.NONE);
                }
                UpdateProxiesCustomAlpha();
            }
            else
            {
                if (m_lodBorder)
                {
                    if (Math.Abs(distanceFromCamera - m_lods[m_lod].Distance) > m_lods[m_lod].Distance * 0.1f)
                        m_lodBorder = false;
                }
                else
                {
                    var lod = 0;
                    for (int lodIndex = 0; lodIndex < m_lods.Length; ++lodIndex)
                    {
                        if (m_lods[lodIndex].Distance <= distanceFromCamera && ((lodIndex == m_lods.Length - 1) || distanceFromCamera < m_lods[lodIndex + 1].Distance))
                            lod = lodIndex;
                    }

                    if (lod != m_lod && (!MyRender11.Settings.PerInstanceLods || !m_isGenericInstance || lod == m_lods.Length - 1))
                    {
                        if (m_objectDithering != 0.0f) // if dithering is enabled, there is no transition
                        {
                            m_lod = lod;
                            SetProxiesForCurrentLod();
                        }
                        else
                        {
                            m_lodTransitionState = lod < m_lod ? -0.001f : 0.001f;
                            m_lodTransitionStartDistance = distanceFromCamera;
                            m_lodBorder = true;

                            SetProxiesForCurrentLod();
                            UpdateProxiesCustomAlpha();
                            SetLodShaders(m_lod, MyShaderUnifiedFlags.DITHERED_LOD);
                            SetLodShaders(lod, MyShaderUnifiedFlags.DITHERED_LOD);
                        }
                    }
                }
            }
        }

        private bool CheckDistanceCulling(float distance)
        {
            bool isCulled = false;
            if ((!AnyDrawOutsideViewDistance && distance > MyRender11.Environment.Matrices.FarClipping) || (m_lods == null || m_lods.Length < 1))
            {
                isCulled = true;
            }
            else if (distance > m_lods[m_lods.Length - 1].Distance && m_instancing == InstancingId.NULL && m_voxelLod < 0)
            {
                double minX = double.MaxValue;
                double minY = double.MaxValue;
                double maxX = double.MinValue;
                double maxY = double.MinValue;

                Vector3D aabbMin = Owner.Aabb.Min;
                Vector3D aabbMax = Owner.Aabb.Max;

                for (int cornerIndex = 0; cornerIndex < 4; ++cornerIndex)
                {
                    Vector3D corner;
                    switch (cornerIndex)
                    {
                        case 0:
                            corner = aabbMax;
                            break;
                        case 1:
                            corner.X = aabbMin.X;
                            corner.Y = aabbMax.Y;
                            corner.Z = aabbMin.Z;
                            break;
                        case 2:
                            corner.X = aabbMax.X;
                            corner.Y = aabbMin.Y;
                            corner.Z = aabbMin.Z;
                            break;
                        case 3:
                            corner.X = aabbMin.X;
                            corner.Y = aabbMax.Y;
                            corner.Z = aabbMax.Z;
                            break;
                        default:
                            corner = Vector3D.Zero;
                            break;
                    }

                    Vector3D.Transform(ref corner, ref MyRender11.Environment.Matrices.ViewProjectionD, out corner);
                    maxX = Math.Max(maxX, corner.X);
                    minX = Math.Min(minX, corner.X);
                    maxY = Math.Max(maxY, corner.Y);
                    minY = Math.Min(minY, corner.Y);
                }
                // Calculate the area of the object on the screen
                float area = (float)((maxX - minX) * (maxY - minY)) * (MyRender11.ResolutionF.X * MyRender11.ResolutionF.Y);

                // Cull the object if it is smaller than 20 pixels^2
                if (area < 20)
                    isCulled = true;
            }
            return isCulled;
        }

        private void UpdatePerInstanceLods(float distance)
        {
            if (m_lods.Length <= 1 || m_instancing.Index == -1)
                return;

            //TODO(AF) remove after lod distances are adjusted
            float distMultiplier = 5.0f;
            float distMultiplierSq = distMultiplier * distMultiplier;
            // The 2.0f is added because the last lod needs transition based on distance
            float lastLodMultiplier = 2.0f;
            float lastLodMultiplierSq = lastLodMultiplier * lastLodMultiplier;

            var instanceLodComponent = MyInstancing.GetInstanceActor(this).GetInstanceLod();
            var instanceInfo = MyInstancing.Instancings.Data[m_instancing.Index];
            if (distance < m_lods[m_lods.Length - 1].Distance * distMultiplier * lastLodMultiplier)
            {
                int capacity = instanceInfo.TotalCapacity;

                float lodDistanceSquared = m_lods[m_lods.Length - 1].Distance * m_lods[m_lods.Length - 1].Distance * distMultiplierSq;
                Vector3D translation = m_lods[0].RenderableProxies[0].WorldMatrix.Translation;

                for (int instanceIndex = 0; instanceIndex < capacity; ++instanceIndex)
                {
                    Vector3D position = (Vector3D)instanceInfo.Positions[instanceIndex] + translation;
                    double distanceToCameraSquared = (position - MyRender11.Environment.Matrices.CameraPosition).LengthSquared();

                    bool oldIsInstanceFar = instanceLodComponent.IsFar(m_instancing, instanceIndex);
                    bool isInstanceFar = distanceToCameraSquared > lodDistanceSquared * lastLodMultiplierSq;

                    if (!isInstanceFar)
                    {
                        bool lodFound = false;
                        for (int lodIndex = 0; lodIndex < m_lods.Length - 1; ++lodIndex)
                        {
                            float lodDistanceSq = m_lods[lodIndex + 1].Distance * m_lods[lodIndex + 1].Distance;
                            if (distanceToCameraSquared < lodDistanceSq * distMultiplierSq)
                            {
                                instanceLodComponent.SetLod(m_instancing, instanceIndex, lodIndex);
                                lodFound = true;
                                break;
                            }
                        }
                        if (!lodFound)
                            instanceLodComponent.SetLod(m_instancing, instanceIndex, m_lods.Length - 2);
                    }

                    if (isInstanceFar != oldIsInstanceFar)
                    {
                        if (isInstanceFar)
                        {
                            instanceLodComponent.RemoveInstanceLod(m_instancing, instanceIndex);
                        }
                        else
                        {
                            MyRenderableProxy[][] newProxies = null;
                            ulong[][] newSortingKeys = null;
                            Array.Resize(ref newProxies, m_lods.Length - 1);
                            Array.Resize(ref newSortingKeys, m_lods.Length - 1);

                            BoundingBoxD aabb = BoundingBoxD.CreateInvalid();
                            for (int lodIndex = 0; lodIndex < m_lods.Length - 1; ++lodIndex)
                            {
                                // Switch to LOD0, add proxies
                                int proxyCount = m_lods[lodIndex].RenderableProxies.Length;
                                Array.Resize(ref newProxies[lodIndex], proxyCount);
                                Array.Resize(ref newSortingKeys[lodIndex], proxyCount);
                                var lodProxies = newProxies[lodIndex];
                                var lodSortingKeys = newSortingKeys[lodIndex];

                                for (int proxyIndex = 0; proxyIndex < proxyCount; ++proxyIndex)
                                {
                                    var newProxy = MyObjectPoolManager.Allocate<MyRenderableProxy>();
                                    var originalProxy = m_lods[lodIndex].RenderableProxies[proxyIndex];
                                    MyRenderableProxy oldProxy;
                                    if (m_lods[m_lods.Length - 1].RenderableProxies.Length > 0)
                                        oldProxy = m_lods[m_lods.Length - 1].RenderableProxies[0];
                                    else
                                        oldProxy = originalProxy;

                                    lodProxies[proxyIndex] = newProxy;

                                    var partId = MyMeshes.GetMeshPart(Mesh, lodIndex, proxyIndex);
                                    var technique = partId.Info.Material.Info.Technique;

                                    newProxy.WorldMatrix = instanceInfo.InstanceData[instanceIndex].LocalMatrix;
                                    newProxy.WorldMatrix.Translation = newProxy.WorldMatrix.Translation + oldProxy.WorldMatrix.Translation;

                                    newProxy.CommonObjectData = oldProxy.CommonObjectData;
                                    newProxy.CommonObjectData.LocalMatrix = newProxy.WorldMatrix;
                                    UpdateKeyColor(ref newProxy.CommonObjectData, new Vector3(instanceInfo.InstanceData[instanceIndex].ColorMaskHSV.ToVector4()));

                                    newProxy.NonVoxelObjectData.Facing = 0;
                                    newProxy.NonVoxelObjectData.WindScaleAndFreq = partId.Info.Material.Info.WindScaleAndFreq;
                                    newProxy.NonVoxelObjectData.CenterOffset = Vector3.Zero;

                                    newProxy.VoxelCommonObjectData = MyObjectDataVoxelCommon.Invalid;

                                    newProxy.Mesh = originalProxy.Mesh;
                                    newProxy.Instancing = InstancingId.NULL;
                                    newProxy.DrawSubmesh = originalProxy.DrawSubmesh;
                                    newProxy.PerMaterialIndex = originalProxy.PerMaterialIndex;
                                    newProxy.InstanceCount = 0;
                                    newProxy.StartInstance = 0;
                                    newProxy.Type = originalProxy.Type;
                                    newProxy.Flags = originalProxy.Flags;
                                    //newProxy.Flags |= MyRenderableProxyFlags.DrawOutsideViewDistance;
                                    newProxy.Lod = 0;
                                    newProxy.ObjectBuffer = oldProxy.ObjectBuffer;
                                    newProxy.Parent = instanceLodComponent;
                                    newProxy.Material = originalProxy.Material;

                                    var textureTypes = originalProxy.Material.Info.TextureTypes;
                                    var depthFlags = m_lods[lodIndex].VertexShaderFlags | MyShaderUnifiedFlags.DEPTH_ONLY | MyShaderUnifiedFlags.DITHERED_LOD | MapTechniqueToShaderMaterialFlags(technique);
                                    var shaderFlags = m_lods[lodIndex].VertexShaderFlags | MyShaderUnifiedFlags.DITHERED_LOD | MapTechniqueToShaderMaterialFlags(technique);
                                    var forwardFlags = m_lods[lodIndex].VertexShaderFlags | MyShaderUnifiedFlags.DITHERED_LOD | MapTechniqueToShaderMaterialFlags(technique);
                                    depthFlags &= ~MyShaderUnifiedFlags.USE_GENERIC_INSTANCING;
                                    shaderFlags &= ~MyShaderUnifiedFlags.USE_GENERIC_INSTANCING;
                                    forwardFlags &= ~MyShaderUnifiedFlags.USE_GENERIC_INSTANCING;

                                    MyStringId shaderMaterial = MyMaterialShaders.MapTechniqueToShaderMaterial(technique);

                                    newProxy.Shaders = MyMaterialShaders.Get(
                                        shaderMaterial,
                                        MyMaterialShaders.MapTechniqueToDefaultPass(technique),
                                        m_lods[lodIndex].VertexLayout1,
                                        shaderFlags, textureTypes);
                                    newProxy.DepthShaders = MyMaterialShaders.Get(
                                        shaderMaterial,
                                        MyMaterialShaders.DEPTH_PASS_ID,
                                        m_lods[lodIndex].VertexLayout1,
                                        depthFlags, textureTypes);
                                    newProxy.HighlightShaders = MyMaterialShaders.Get(
                                        shaderMaterial,
                                        MyMaterialShaders.HIGHLIGHT_PASS_ID,
                                        m_lods[lodIndex].VertexLayout1,
                                        shaderFlags, textureTypes);
                                    newProxy.ForwardShaders = MyMaterialShaders.Get(
                                        shaderMaterial,
                                        MyMaterialShaders.FORWARD_PASS_ID,
                                        m_lods[lodIndex].VertexLayout1,
                                        forwardFlags, textureTypes);

                                    lodSortingKeys[proxyIndex] = m_lods[lodIndex].SortingKeys[proxyIndex];
                                    aabb.Include((BoundingBoxD)originalProxy.Mesh.Info.BoundingBox.Value);
                                }
                            }
                            aabb.Translate(position);
                            instanceLodComponent.AddInstanceLod(m_instancing, instanceIndex, newProxies, newSortingKeys, aabb, position);
                        }
                    }
                }
            }
            else if (instanceInfo.NonVisibleInstanceCount > 0)
            {
                instanceLodComponent.RemoveAllInstanceLods(m_instancing);
            }

            if (instanceInfo.VisibleCapacity == 0)
                m_culled = true;
        }

        internal void OnFrameUpdate()
        {
            UpdateLodState();
        }

        internal override void OnRemove(MyActor owner)
        {
            DebrisEntityVoxelMaterial.Remove(owner.ID);
            DeallocateLodProxies();

            base.OnRemove(owner);

            this.Deallocate();
        }

        virtual protected void AddToRenderables()
        {
            if (m_btreeProxy != MyDynamicAABBTreeD.NullNode || !m_isRenderedStandalone)
                return;

            m_btreeProxy = MyScene.DynamicRenderablesDBVH.AddProxy(ref Owner.Aabb, m_cullProxy, 0);
        }

        virtual protected bool MoveRenderableAABB()
        {
            if (m_btreeProxy == MyDynamicAABBTreeD.NullNode)
                return false;

            return MyScene.DynamicRenderablesDBVH.MoveProxy(m_btreeProxy, ref Owner.Aabb, Vector3.Zero);
        }

        virtual protected void RemoveFromRenderables()
        {
            if (m_btreeProxy == MyDynamicAABBTreeD.NullNode)
                return;

            MyScene.DynamicRenderablesDBVH.RemoveProxy(m_btreeProxy);
            m_btreeProxy = MyDynamicAABBTreeD.NullNode;
        }

        private void SetStandaloneRendering(bool val)
        {
            if (m_isRenderedStandalone != val && val == false)
                RemoveFromRenderables();

            if (m_isRenderedStandalone != val && val == true)
                Owner.MarkRenderDirty();

            m_isRenderedStandalone = val;
        }

        #region Helpers

        internal float CalculateViewerDistance()
        {
            return Owner.CalculateCameraDistance();
        }

        internal static bool IsSortingOrderBackToFront(MyMaterialType type)
        {
            switch(type)
            {
                case MyMaterialType.TRANSPARENT:
                    return true;
                default:
                    return false;
            }
        }

        internal static void FillPerMaterialData(ref MyPerMaterialData perMaterialData, MyMeshDrawTechnique technique)
        {
            perMaterialData.Type = 0;

            switch(technique)
            {
                case MyMeshDrawTechnique.FOLIAGE:
                    perMaterialData.Type = MyMaterialTypeEnum.FOLIAGE;
                    break;
            }
        }

        internal static MyShaderUnifiedFlags MapTechniqueToShaderMaterialFlags(MyMeshDrawTechnique technique)
        {
			MyShaderUnifiedFlags flags = MyShaderUnifiedFlags.USE_SHADOW_CASCADES;
            switch(technique)
            {
                case MyMeshDrawTechnique.ALPHA_MASKED:
                case MyMeshDrawTechnique.FOLIAGE:
					flags |= MyShaderUnifiedFlags.ALPHA_MASKED;
					break;
                default:
					flags |= MyShaderUnifiedFlags.NONE;
					break;
            }

			return flags;
        }

        internal static MyRenderableProxyFlags MapTechniqueToRenderableFlags(MyMeshDrawTechnique technique)
        {
            switch (technique)
            {
                case MyMeshDrawTechnique.ALPHA_MASKED:
                case MyMeshDrawTechnique.FOLIAGE:
                case MyMeshDrawTechnique.CLOUD_LAYER:
                    return MyRenderableProxyFlags.DisableFaceCulling;
                default:
                    return MyRenderableProxyFlags.DepthSkipTextures;
            }
        }

        internal static MyMaterialType MapTechniqueToMaterialType(MyMeshDrawTechnique technique)
        {
            switch (technique)
            {
                case MyMeshDrawTechnique.ALPHA_MASKED:
                case MyMeshDrawTechnique.FOLIAGE:
                    return MyMaterialType.ALPHA_MASKED;
                default:
                    return MyMaterialType.OPAQUE;
            }
        }

        #endregion

        internal static void MarkAllDirty()
        {
            var actors = MyActorFactory.GetAll();
            foreach (var actor in actors)
            {
                actor.MarkRenderDirty();
            }
        }
    }
}
