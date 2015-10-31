using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VRage.Import;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;
using VRageRender.Resources;
using BoundingBox = VRageMath.BoundingBox;
using Matrix = VRageMath.Matrix;
using Vector3 = VRageMath.Vector3;
using Vector4 = VRageMath.Vector4;


namespace VRageRender
{
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
        RGB_COLORING = 1,
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

    static class MySceneMaterials
    {
        internal static StructuredBufferId m_buffer = StructuredBufferId.NULL;

        static List<MyPerMaterialData> Data = new List<MyPerMaterialData>();
        static Dictionary<int, int> HashIndex = new Dictionary<int, int>();

        // data refreshed every frame
        static MyPerMaterialData[] TransferData = new MyPerMaterialData[4096];
        static Dictionary<int, uint> TransferHashIndex = new Dictionary<int, uint>();

        internal unsafe static void Init()
        {
            m_buffer = MyHwBuffers.CreateStructuredBuffer(4096, sizeof(MyPerMaterialData), true);
        }

        internal static void PreFrame()
        {
            TransferHashIndex.Clear();

            // bump default material as 0
            MyPerMaterialData defaultMat = new MyPerMaterialData();
            GetDrawMaterialIndex(GetPerMaterialDataIndex(ref defaultMat));

            // bump foliage material as 1 (important)
            MyPerMaterialData foliageMat = new MyPerMaterialData();
            foliageMat.Type = MyMaterialTypeEnum.FOLIAGE;
            GetDrawMaterialIndex(GetPerMaterialDataIndex(ref foliageMat));
        }

        internal static int GetPerMaterialDataIndex(ref MyPerMaterialData data)
        {
            var key = data.CalculateKey();

            if (!HashIndex.ContainsKey(key))
            {
                HashIndex[key] = HashIndex.Count;
                Data.Add(data);
            }

            return HashIndex[key];
        }

        internal static uint GetDrawMaterialIndex(int index)
        {
            if (!TransferHashIndex.ContainsKey(index))
            {
                TransferHashIndex[index] = (uint)TransferHashIndex.Count;
                TransferData[TransferHashIndex.Count - 1] = Data[index];
            }

            return TransferHashIndex[index];
        }

        internal static void OnDeviceReset()
        {
            if(m_buffer != StructuredBufferId.NULL)
            {
                MyHwBuffers.Destroy(m_buffer);
                m_buffer = StructuredBufferId.NULL;
            }
            Init();
        }

        internal unsafe static void MoveToGPU()
        {
            var context = MyImmediateRC.RC.Context;

            fixed (void* ptr = TransferData)
            {
                var intPtr = new IntPtr(ptr);

                var mapping = MyMapping.MapDiscard(m_buffer.Buffer);
                mapping.stream.Write(intPtr, 0, sizeof(MyPerMaterialData) * TransferHashIndex.Count);
                mapping.Unmap();
            }
        }
    }

    static class MyScene
    {
        internal static MyDynamicAABBTreeD RenderablesDBVH = new MyDynamicAABBTreeD(MyRender11Constants.PRUNNING_EXTENSION);
        internal static MyDynamicAABBTreeD GroupsDBVH = new MyDynamicAABBTreeD(MyRender11Constants.PRUNNING_EXTENSION);

        internal static Dictionary<uint, HashSet<MyEntityMaterialKey>> EntityDisabledMaterials = new Dictionary<uint, HashSet<MyEntityMaterialKey>>();

    }

    class MyRenderLod
    {
        internal MyRenderableProxy[] RenderableProxies;
        internal UInt64[] SortingKeys;
        internal MyMaterialShadersBundleId[] HighlightShaders;

        //internal MyVertexDataProxy VertexDataProxy;
        internal VertexLayoutId VertexLayout1;
        internal MyShaderUnifiedFlags VertexShaderFlags;

        internal float Distance;

        internal void DeallocateProxies()
        {
            if (RenderableProxies != null)
            {
                for (int i = 0; i < RenderableProxies.Length; i++)
                {
                    MyProxiesFactory.Remove(RenderableProxies[i]);
                }
                RenderableProxies = null;
            }
        }
    }

    struct MyRenderableInfo
    {
        internal Vector3 KeyColor;
        internal float ObjectDithering;

        internal MeshId Mesh;
    }

    struct MyEntityInfo
    {
        internal MatrixD WorldMatrixD;
        internal BoundingBoxD AabbD;
        internal BoundingBox Aabb;
        internal bool Visible;
    }

    struct EntityId
    {
        internal int Index;

        public static bool operator ==(EntityId x, EntityId y)
        {
            return x.Index == y.Index;
        }

        public static bool operator !=(EntityId x, EntityId y)
        {
            return x.Index != y.Index;
        }

        internal static readonly EntityId NULL = new EntityId { Index = -1 };

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    struct RenderableId
    {
        internal int Index;

        public static bool operator ==(RenderableId x, RenderableId y)
        {
            return x.Index == y.Index;
        }

        public static bool operator !=(RenderableId x, RenderableId y)
        {
            return x.Index != y.Index;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        internal static readonly RenderableId NULL = new RenderableId { Index = -1 };
    }

    struct MySkinning
    {
        internal Matrix[] SkinTransforms;
        internal Matrix[] AbsoluteTransforms;
        internal MySkeletonBoneDescription[] SkeletonHierarchy;
        internal int[] SkeletonIndices;
    }

    static class MyComponents
    {
        static Dictionary<uint, EntityId> EntityIndex = new Dictionary<uint,EntityId>();
        static MyFreelist<MyEntityInfo> Entities = new MyFreelist<MyEntityInfo>(512);
        static int[] EntityDrawDescriptors = new int[512];
        static MyFreelist<MyRenderableInfo> Renderables = new MyFreelist<MyRenderableInfo>(512);

        static Dictionary<EntityId, RenderableId> RenderableIndex = new Dictionary<EntityId,RenderableId>();
        static Dictionary<EntityId, MySkinning> Skinnings = new Dictionary<EntityId,MySkinning>();

        static HashSet<EntityId> EntityDirty = new HashSet<EntityId>();

        internal static void Init()
        {
            
        }

        
        internal static EntityId GetEntity(uint GID)
        {
            return EntityIndex.Get(GID, EntityId.NULL);
        }

        internal static EntityId CreateEntity(uint GID)
        {
            var id = new EntityId { Index = Entities.Allocate() };
            EntityIndex[GID] = id;

            Entities.Data[id.Index] = new MyEntityInfo { Visible = true };

            return id;
        }

        internal static void SetMatrix(EntityId entity, ref MatrixD matrix)
        {
            Entities.Data[entity.Index].WorldMatrixD = matrix; 
        }

        internal static void SetAabb(EntityId entity, ref BoundingBoxD aabb)
        {
            Entities.Data[entity.Index].AabbD = aabb;
            Entities.Data[entity.Index].Aabb = (BoundingBox)aabb;
        }

        internal static RenderableId CreateRenderable(EntityId entity, MeshId model, Vector3 keyColor)
        {
            var id = new RenderableId { Index = Renderables.Allocate() };

            Renderables.Data[id.Index] = new MyRenderableInfo
            {
                KeyColor = keyColor,
                ObjectDithering = 0,
                Mesh = model
            };

            EntityDirty.Add(entity);

            RenderableIndex[entity] = id;

            return id;
        }

        internal static void SetSkeleton(EntityId entity, MySkeletonBoneDescription[] hierarchy, int[] skeletonIndices)
        {
            Skinnings[entity] = new MySkinning
            {
                SkeletonHierarchy = hierarchy,
                SkeletonIndices = skeletonIndices,
                SkinTransforms = new Matrix[hierarchy.Length],
                AbsoluteTransforms = new Matrix[hierarchy.Length]
            };

            EntityDirty.Add(entity);
        }

        internal static void SetAnimation(EntityId entity, Matrix[] simulatedBones)
        {
            Debug.Assert(Skinnings.ContainsKey(entity));

            var skinning = Skinnings[entity];
            var skeletonCount = skinning.SkeletonHierarchy.Length;

            for (int i = 0; i < skeletonCount; i++)
            {
                skinning.AbsoluteTransforms[i] = simulatedBones[i];
            }

            for (int i = 0; i < skeletonCount; i++)
            {
                if (skinning.SkeletonHierarchy[i].Parent != -1)
                {
                    skinning.AbsoluteTransforms[i] = skinning.AbsoluteTransforms[i] * skinning.AbsoluteTransforms[skinning.SkeletonHierarchy[i].Parent];
                }
            }

            int bonesCount = skinning.SkeletonIndices.Length;

            for (int i = 0; i < bonesCount; i++)
            {
                skinning.SkinTransforms[i] = skinning.SkeletonHierarchy[skinning.SkeletonIndices[i]].SkinTransform * skinning.AbsoluteTransforms[skinning.SkeletonIndices[i]];
            }
        }
       
        internal unsafe static void ProcessEntities()
        {
            foreach (var entity in EntityDirty)
            {
                RenderableId renderable = RenderableIndex[entity];

                MeshId model = Renderables.Data[renderable.Index].Mesh;

                MyShaderUnifiedFlags vsFlags = MyShaderUnifiedFlags.NONE;
                //var instancing = m_owner.GetComponent(MyActorComponentEnum.Instancing) as MyInstancingComponent;

                bool skinningEnabled = Skinnings.ContainsKey(entity);
                var objectConstantsSize = sizeof(MyObjectData);

                if (skinningEnabled)
                {
                    vsFlags |= MyShaderUnifiedFlags.USE_SKINNING;
                    objectConstantsSize += sizeof(Matrix) * 60;
                }

                int lodsNum = model.Info.LodsNum;
                for (int l = 0; l < lodsNum; l++)
                {
                    var lod = MyMeshes.GetLodMesh(model, l);

                    var layout = lod.VertexLayout;

                    MyCommon.GetObjectCB(objectConstantsSize);

                    for (int p = 0; p < lod.Info.PartsNum; p++)
                    {
                        var part = MyMeshes.GetMeshPart(model, l, p);
                        var technique = part.Info.Material.Info.Technique;

                        MyMaterialShaders.Get(
                            X.TEXT(MyRenderableComponent.MapTechniqueToShaderMaterial(technique)),
                            X.TEXT(MyGeometryRenderer.DEFAULT_DEPTH_PASS),
                            layout,
                            vsFlags | MyRenderableComponent.MapTechniqueToShaderMaterialFlags(technique));

                        var type = MyRenderableComponent.MapTechniqueToMaterialType(technique);
                    }

                }
            }

            EntityDirty = new HashSet<EntityId>();
        }

        internal static void SendVisible()
        {
            foreach (var ID in EntityIndex.Keys)
            {
                MyRenderProxy.VisibleObjectsWrite.Add(ID);
            }
        }   
    }

    struct MyMaterialTextureSwap
    {
        internal string MaterialSlot;
        internal MyStringId TextureName;
    }

    class MyModelProperties
    {
        internal static readonly float DefaultEmissivity = 0;
        internal static readonly Vector3 DefaultColorMul = Vector3.One;
        internal static readonly int MaxCustomTextures = 32;
        internal static int CustomTextures = 0;

        internal float Emissivity = DefaultEmissivity;
        internal Vector3 ColorMul = DefaultColorMul;

        //internal MyMaterialTextureSwap? TextureSwap = null;
        internal List<MyMaterialTextureSwap> TextureSwaps = null;

        internal MyMaterialProxyId CustomMaterialProxy = MyMaterialProxyId.NULL;
        internal RwTexId CustomRenderedTexture = RwTexId.NULL;
    }

    class MyRenderableComponent : MyActorComponent
    {
        #region Global params

        internal static Dictionary<uint, uint> DebrisEntityVoxelMaterial = new Dictionary<uint,uint>();

        #endregion


        #region Fields

        Vector3 m_keyColor;
        float m_objectDithering;
        
        internal int m_voxelLod;
        internal Vector3 m_voxelScale;
        internal Vector3 m_voxelOffset;

        internal MeshId Mesh;
        InstancingId Instancing;
        internal MyRenderLod[] m_lods;
        internal MyCullProxy[] m_renderableProxiesForLodTransition;
        
        internal MyCullProxy m_cullProxy;
        int m_btreeProxy;

        //internal MyShaderUnifiedFlags m_vsFlags;

        int m_instanceCount;
        int m_startInstance;

        internal int m_lod;
        float m_lodTransitionState; // [-1,0] or [0,1]
        float m_lodTransitionVector; // distance at which transition must end
        float m_lodTransitionStartDistance;
        bool m_lodBorder;

        bool m_isRenderedStandalone;
        internal MyRenderableProxyFlags m_additionalFlags;
        bool m_colorEmissivityDirty = false;
        internal Dictionary<MyEntityMaterialKey, MyModelProperties> ModelProperties;

        internal void SetStandaloneRendering(bool val)
        {
            if (m_isRenderedStandalone != val && val == false && m_btreeProxy != -1)
            {
                MyScene.RenderablesDBVH.RemoveProxy(m_btreeProxy);
                m_btreeProxy = -1;
            }

            if(m_isRenderedStandalone != val && val == true)
            {
                m_owner.MarkRenderDirty();
            }

            m_isRenderedStandalone = val;
        }

        internal bool IsRendered { get { return m_isRenderedStandalone && m_owner.m_visible; } }
        internal bool SkipProcessing { get { return m_btreeProxy == -1; } }

        #endregion

        internal int CurrentLodNum { get { return m_lod; } }

        #region Memory

        internal override void Construct()
        {
            base.Construct();

            Type = MyActorComponentEnum.Renderable;
            //m_mesh = null;
            m_lods = null;
            m_cullProxy = MyProxiesFactory.CreateCullProxy();
            m_btreeProxy = -1;

            Mesh = MeshId.NULL;
            Instancing = InstancingId.NULL;
            
            m_instanceCount = 0;
            m_startInstance = 0;

            m_isRenderedStandalone = true;

            m_keyColor = Vector3.One;
            m_objectDithering = 0;

            m_renderableProxiesForLodTransition = null;

            m_lodTransitionState = 0;
            m_lod = 0;

            m_voxelLod = -1;
            m_additionalFlags = 0;

            ModelProperties = new Dictionary<MyEntityMaterialKey, MyModelProperties>();
            m_voxelScale = Vector3.Zero;
            m_voxelOffset = Vector3.Zero;

        }

        internal override void Destruct()
        {
            if(m_btreeProxy != -1)
            {
                MyScene.RenderablesDBVH.RemoveProxy(m_btreeProxy);
                m_btreeProxy = -1;
            }
            if(m_cullProxy != null)
            {
                MyProxiesFactory.Remove(m_cullProxy);
                m_cullProxy = null;
            }
            DeallocateLodProxies();
            ModelProperties.Clear();

            base.Destruct();
        }

        #endregion

        internal void SetLocalAabbToModelLod(int lod)
        {
            var bb = MyMeshes.GetLodMesh(Mesh, lod).Info.BoundingBox;
            if(bb.HasValue)
            {
                m_owner.SetLocalAabb(bb.Value);
            }
        }

        internal void SetModel(MeshId mesh)
        {
            Mesh = mesh;

            SetLocalAabbToModelLod(0);

            m_owner.MarkRenderDirty();
        }

        //internal void SetModel(MyMesh mesh)
        //{
        //    m_mesh = mesh;
        //    //m_currentLod = 0;
        //    //m_lodState = 0;
        //    //m_currentLod = 0;
        //    // todo: consider if ok when changing during lod transition

        //    // copy material data
        //    //m_lodMaterials = new MyRenderLodMaterialInfo[mesh.LODs.Length];
        //    //for(int i=0; i<mesh.LODs.Length; i++)
        //    //{
        //    //    m_lodMaterials[i] = new MyRenderLodMaterialInfo(mesh.LODs[i].m_meshInfo);
        //    //}

        //    //SetLocalAabbToModelLod(0);

        //    m_owner.MarkRenderDirty();
        //}

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

        internal void UpdateMaterialProxies()
        {
            //for (int i = 0; i < m_lods.Length; i++)
            //{
            //    var lod = m_lods[i];

            //    int c = 0;
            //    foreach (var kv in m_mesh.LODs[i].m_meshInfo.Parts)
            //    {
            //        var technique = kv.Key;

            //        var submeshes = (MyDrawSubmesh[])lod.RenderableProxies[c].submeshes.Clone();
            //        lod.RenderableProxies[c].submeshes = submeshes;

            //        c++;
            //    }
            //}
        }

        internal void UpdateProxiesObjectData()
        {
            if (m_lods != null)
            {
                for (int i = 0; i < m_lods.Length; i++)
                {
                    for (int j = 0; j < m_lods[i].RenderableProxies.Length; j++)
                    {
                        m_lods[i].RenderableProxies[j].WorldMatrix = m_owner.WorldMatrix;
                        //m_lods[i].RenderableProxies[j].ObjectData.LocalMatrix = m_owner.WorldMatrix;
                        m_lods[i].RenderableProxies[j].ObjectData.KeyColor = m_keyColor;
                    }
                }
            }
        }

        internal override void OnMatrixChange()
        {
            base.OnMatrixChange();

            if(!m_owner.RenderDirty)
            {
                UpdateProxiesObjectData();
            }
        }

        internal override void OnAabbChange()
        {
            base.OnAabbChange();

            if (IsRendered && m_btreeProxy != -1)
            {
                MyScene.RenderablesDBVH.MoveProxy(m_btreeProxy, ref m_owner.Aabb, Vector3.Zero);
            }
        }

        internal override void OnVisibilityChange()
        {
            base.OnVisibilityChange();

            if (!m_owner.m_visible && m_btreeProxy != -1)
            {
                MyScene.RenderablesDBVH.RemoveProxy(m_btreeProxy);
                m_btreeProxy = -1;
            }

            if (m_owner.m_visible && IsRendered)
            {
                m_owner.MarkRenderDirty();
            }
        }

        internal void SetInstancing(InstancingId instancing)
        {
            if(Instancing != instancing)
            {
                Instancing = instancing;
                m_owner.MarkRenderDirty();
            }
        }

        internal void SetInstancingCounters(int instanceCount, int startInstance)
        {
            m_instanceCount = instanceCount;
            m_startInstance = startInstance;

            if (m_lods != null)
            {
                for (int i = 0; i < m_lods.Length; i++)
                {
                    for (int j = 0; j < m_lods[i].RenderableProxies.Length; j++)
                    {
                        m_lods[i].RenderableProxies[j].InstanceCount = instanceCount;
                        m_lods[i].RenderableProxies[j].StartInstance = startInstance;
                    }
                }
            }
        }

        internal void SetVoxelLod(int lod, MyClipmapScaleEnum scaleEnum)
        {
            //m_voxelLod = lod + ((scaleEnum == MyClipmapScaleEnum.Massive) ? 8 : 0);
            m_voxelLod = lod;

            //Debug.Assert(m_cullProxy.Proxies != null);

            UpdateProxiesCustomAlpha();
        }

        //internal MyMesh GetMesh()
        //{
        //    return m_mesh;
        //}

        internal MeshId GetModel()
        {
            return Mesh;
        }

        // must have same number of subparts
        //internal void RefreshVoxelMeshData()
        //{
        //    if (!m_owner.RenderDirty)
        //    {
        //        var meshInfo = m_mesh.LODs[0].m_meshInfo;


        //        m_lods[0].RenderableProxies[0].geometry.IB = meshInfo.IB.Buffer;
        //        m_lods[0].RenderableProxies[0].geometry.IndexFormat = meshInfo.IB.Format;

        //        m_lods[0].RenderableProxies[0].geometry.VB = meshInfo.VB.Select(x => x.Buffer).ToArray();
        //        m_lods[0].RenderableProxies[0].geometry.VertexStrides = meshInfo.VB.Select(x => x.Stride).ToArray();

        //        int ctr = 0;
        //        foreach (var kv in m_mesh.LODs[0].m_meshInfo.Parts)
        //        {
        //            m_lods[0].RenderableProxies[ctr].depthOnlySubmeshes = kv.Value;
        //            m_lods[0].RenderableProxies[ctr].depthOnlySubmeshes = kv.Value;
        //            ctr++;
        //        }
        //    }
        //}

        internal void SetLodShaders(int lodNum, MyShaderUnifiedFlags appendedFlags)
        {
            var lod = m_lods[lodNum];

            var num = MyMeshes.GetLodMesh(Mesh, lodNum).Info.PartsNum;


            for (int p = 0; p < num; p++)
            {
                var partId = MyMeshes.GetMeshPart(Mesh, lodNum, p);
                var technique = partId.Info.Material.Info.Technique;

                MyShaderUnifiedFlags flags = appendedFlags;
                if (partId.Info.Material.Info.Facing == MyFacingEnum.Impostor || partId.Info.Material.Info.Facing == MyFacingEnum.Vertical)
                    flags |= MyShaderUnifiedFlags.DITHERED;
                if (partId.Info.Material.Info.Facing == MyFacingEnum.Impostor)
                    flags |= MyShaderUnifiedFlags.ALPHAMASK_ARRAY;

                //if (AreTechniqueDrawcallsDepthBatchable(technique) && skinning == null && shadowmapId == -1)
                //{
                //    shadowmapId = c;
                //}

                if (DebrisEntityVoxelMaterial.ContainsKey(m_owner.ID))
                {
                    technique = MyVoxelMesh.SINGLE_MATERIAL_TAG;
                }

                lod.RenderableProxies[p].DepthShaders = MyMaterialShaders.Get(
                    X.TEXT(MapTechniqueToShaderMaterial(technique)),
                    X.TEXT(MyGeometryRenderer.DEFAULT_DEPTH_PASS),
                    lod.VertexLayout1,
                    lod.VertexShaderFlags | MyShaderUnifiedFlags.DEPTH_ONLY | MapTechniqueToShaderMaterialFlags(technique) | GetCurrentStateMaterialFlags(lodNum) | flags);
                lod.RenderableProxies[p].Shaders = MyMaterialShaders.Get(
                    X.TEXT(MapTechniqueToShaderMaterial(technique)),
                    X.TEXT(MyGeometryRenderer.DEFAULT_OPAQUE_PASS),
                    lod.VertexLayout1,
                    lod.VertexShaderFlags | MapTechniqueToShaderMaterialFlags(technique) | GetCurrentStateMaterialFlags(lodNum) | flags);
                lod.RenderableProxies[p].ForwardShaders = MyMaterialShaders.Get(
                    X.TEXT(MapTechniqueToShaderMaterial(technique)),
                    X.TEXT(MyGeometryRenderer.DEFAULT_FORWARD_PASS),
                    lod.VertexLayout1,
                    lod.VertexShaderFlags | MapTechniqueToShaderMaterialFlags(technique) | GetCurrentStateMaterialFlags(lodNum) | flags);
            }
        }

        private Vector4 GetUvScaleOffset(Vector2I uvTiles, Vector2I tileIndex)
        {
            Vector4 scaleOffset = new Vector4(uvTiles.X, uvTiles.Y, (1.0f / uvTiles.X) * tileIndex.X, (1.0f / uvTiles.Y) * tileIndex.Y);
            return scaleOffset;
        }

        internal unsafe void RebuildLodProxy(int lodNum,
            bool skinningEnabled, MySkinningComponent skinning, int objectConstantsSize)
        {
            var lod = m_lods[lodNum];
            //var meshInfo = m_mesh.LODs[lodNum].m_meshInfo;
            var lodMesh = MyMeshes.GetLodMesh(Mesh, lodNum);

            //lod.VertexLayout = MyVertexInputLayout.Empty();
            //lod.VertexLayout = lod.VertexLayout.Append(meshInfo.VertexLayout);

            lod.Distance = lodMesh.Info.LodDistance;

            var vsFlags = MyShaderUnifiedFlags.NONE;
            if(skinningEnabled)
            {
                vsFlags |= MyShaderUnifiedFlags.USE_SKINNING;
            }
            if (Instancing != InstancingId.NULL)
            {
                lod.VertexLayout1 = MyVertexLayouts.GetLayout(lodMesh.VertexLayout, Instancing.Info.Layout);

                if (Instancing.Info.Type == MyRenderInstanceBufferType.Cube)
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
                else if (Instancing.Info.Type == MyRenderInstanceBufferType.Generic)
                {
                    vsFlags |= MyShaderUnifiedFlags.USE_GENERIC_INSTANCING;
                }
            }
            else
            {
                lod.VertexLayout1 = lodMesh.VertexLayout;
            }
            lod.VertexShaderFlags = vsFlags;

            var lodMeshInfo = lodMesh.Info;
            var Num = lodMeshInfo.PartsNum;
            Debug.Assert(Num > 0);

            if(lod.RenderableProxies == null)
            {
                lod.RenderableProxies = new MyRenderableProxy[Num];
                lod.SortingKeys = new UInt64[Num];
                lod.HighlightShaders = new MyMaterialShadersBundleId[Num];

                for (int i = 0; i < Num; i++)
                {
                    lod.RenderableProxies[i] = MyProxiesFactory.CreateRenderableProxy();
                }
            }

            for (int p = 0; p < Num; p++ )
            {
                var partId = MyMeshes.GetMeshPart(Mesh, lodNum, p);
                var technique = partId.Info.Material.Info.Technique;

                var voxelMaterialId = -1;
                if(DebrisEntityVoxelMaterial.ContainsKey(m_owner.ID))
                {
                    technique = MyVoxelMesh.SINGLE_MATERIAL_TAG;
                    voxelMaterialId = (int)DebrisEntityVoxelMaterial[m_owner.ID];
                }

                //if (AreTechniqueDrawcallsDepthBatchable(technique) && skinning == null && shadowmapId == -1)
                //{
                //    shadowmapId = c;
                //}

                lod.RenderableProxies[p].WorldMatrix = m_owner.WorldMatrix;

                //lod.RenderableProxies[p].ObjectData.LocalMatrix = m_owner.WorldMatrix;
                //lod.RenderableProxies[p].ObjectData.LocalMatrix = m_owner.WorldMatrix;
                lod.RenderableProxies[p].ObjectData.Emissive = MyModelProperties.DefaultEmissivity;
                lod.RenderableProxies[p].ObjectData.ColorMul = MyModelProperties.DefaultColorMul;
                lod.RenderableProxies[p].ObjectData.VoxelScale = Vector3.One;
                lod.RenderableProxies[p].ObjectData.VoxelOffset = Vector3.Zero;
                lod.RenderableProxies[p].ObjectData.MassiveCenterRadius = Vector4.Zero;
                lod.RenderableProxies[p].ObjectData.Facing = (byte)partId.Info.Material.Info.Facing;
                lod.RenderableProxies[p].ObjectData.WindScaleAndFreq = partId.Info.Material.Info.WindScaleAndFreq;

                if ((partId.Info.Material.Info.Facing == MyFacingEnum.Full) || (partId.Info.Material.Info.Facing == MyFacingEnum.Impostor))
                {
                    lod.RenderableProxies[p].ObjectData.VoxelOffset = partId.Info.CenterOffset;
                }
                                
                lod.RenderableProxies[p].Mesh = lodMesh;
                lod.RenderableProxies[p].DepthShaders = MyMaterialShaders.Get(
                    X.TEXT(MapTechniqueToShaderMaterial(technique)), 
                    X.TEXT(MyGeometryRenderer.DEFAULT_DEPTH_PASS), 
                    lod.VertexLayout1,
                    lod.VertexShaderFlags | MyShaderUnifiedFlags.DEPTH_ONLY | MapTechniqueToShaderMaterialFlags(technique) | GetCurrentStateMaterialFlags(lodNum));
                lod.RenderableProxies[p].Shaders = MyMaterialShaders.Get(
                    X.TEXT(MapTechniqueToShaderMaterial(technique)), 
                    X.TEXT(MyGeometryRenderer.DEFAULT_OPAQUE_PASS), 
                    lod.VertexLayout1,
                    lod.VertexShaderFlags | MapTechniqueToShaderMaterialFlags(technique) | GetCurrentStateMaterialFlags(lodNum));
                lod.RenderableProxies[p].ForwardShaders = MyMaterialShaders.Get(
                    X.TEXT(MapTechniqueToShaderMaterial(technique)),
                    X.TEXT(MyGeometryRenderer.DEFAULT_FORWARD_PASS),
                    lod.VertexLayout1,
                    lod.VertexShaderFlags | MapTechniqueToShaderMaterialFlags(technique) | GetCurrentStateMaterialFlags(lodNum));

                lod.RenderableProxies[p].Material = MyMeshes.GetMeshPart(Mesh, lodNum, p).Info.Material.Info.Name;

                lod.HighlightShaders[p] = MyMaterialShaders.Get(X.TEXT(MapTechniqueToShaderMaterial(technique)), X.TEXT("highlight"), lod.VertexLayout1, lod.VertexShaderFlags | MapTechniqueToShaderMaterialFlags(technique));
                var partInfo = partId.Info;

                MyDrawSubmesh draw = new MyDrawSubmesh
                {
                    BaseVertex = partInfo.BaseVertex,
                    StartIndex = partInfo.StartIndex,
                    IndexCount = partInfo.IndexCount,
                    BonesMapping = partInfo.BonesMapping,
                    MaterialId = MyMeshMaterials1.GetProxyId(partInfo.Material)
                };

                if (voxelMaterialId != -1)
                {
                    draw.MaterialId = MyVoxelMaterials1.GetMaterialProxyId(new MyVoxelMaterialTriple(voxelMaterialId, -1, -1));
                }

                lod.RenderableProxies[p].DrawSubmesh = draw;

                if (technique == "GLASS")
                {
                    lod.RenderableProxies[p].DrawSubmesh.IndexCount = 0;
                }

                lod.RenderableProxies[p].SkinningMatrices = skinningEnabled ? skinning.SkinMatrices : null;

                lod.RenderableProxies[p].ObjectBuffer = MyCommon.GetObjectCB(objectConstantsSize);
                lod.RenderableProxies[p].InstanceCount = m_instanceCount;
                lod.RenderableProxies[p].StartInstance = m_startInstance;
                lod.RenderableProxies[p].Flags = MapTechniqueToRenderableFlags(technique) | m_additionalFlags;
                lod.RenderableProxies[p].Type = MapTechniqueToMaterialType(technique);
                lod.RenderableProxies[p].Parent = this;
                lod.RenderableProxies[p].Lod = lodNum;
                lod.RenderableProxies[p].Instancing = Instancing;

                MyPerMaterialData materialData;
                materialData.Type = 0;
                FillPerMaterialData(ref materialData, technique);
                lod.RenderableProxies[p].PerMaterialIndex = MySceneMaterials.GetPerMaterialDataIndex(ref materialData);
                lod.RenderableProxies[p].ObjectData.MaterialFlags = MapTechniqueToMaterialFlags(technique);

                ulong sortingKey = 0;

                My64BitValueHelper.SetBits(ref sortingKey, 36, 2, (ulong)lod.RenderableProxies[p].Type);
                My64BitValueHelper.SetBits(ref sortingKey, 32, 4, (ulong)lod.RenderableProxies[p].PerMaterialIndex);
                My64BitValueHelper.SetBits(ref sortingKey, 26, 6, (ulong)MyShaderMaterial.GetID(MapTechniqueToShaderMaterial(technique)));
                My64BitValueHelper.SetBits(ref sortingKey, 20, 6, (ulong)lod.VertexShaderFlags);
                My64BitValueHelper.SetBits(ref sortingKey, 14, 6, (ulong)lod.VertexLayout1.Index);
                My64BitValueHelper.SetBits(ref sortingKey, 7, 7, (ulong)lod.RenderableProxies[p].Mesh.Index);
                My64BitValueHelper.SetBits(ref sortingKey, 0, 7, (ulong)lod.RenderableProxies[p].Material.GetHashCode());

                lod.SortingKeys[p] = sortingKey;
            }

            SetLodShaders(lodNum, MyShaderUnifiedFlags.NONE);
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

        internal MyShaderUnifiedFlags GetCurrentStateMaterialFlags(int lodNum) 
        { 
            return m_objectDithering > 0 ? MyShaderUnifiedFlags.DITHERED : MyShaderUnifiedFlags.NONE;
        }

        internal unsafe void RebuildVoxelRenderProxies()
        {
            var objectConstantsSize = sizeof(MyObjectData);

            DeallocateLodProxies();

            Debug.Assert(Mesh.Info.LodsNum == 1);
            m_lods = new MyRenderLod[1];
            m_lods[0] = new MyRenderLod();

            var lodMesh = MyMeshes.GetLodMesh(Mesh, 0);
            var lod = m_lods[0];
            lod.VertexLayout1 = lodMesh.VertexLayout;
            m_lods[0].VertexShaderFlags = MyShaderUnifiedFlags.USE_VOXEL_MORPHING;

            var lodMeshInfo = lodMesh.Info;
            var Num = lodMeshInfo.PartsNum;
            Debug.Assert(Num > 0);

            if(lod.RenderableProxies == null)
            {
                lod.RenderableProxies = new MyRenderableProxy[Num];
                lod.SortingKeys = new UInt64[Num];

                for (int i = 0; i < Num; i++)
                {
                    lod.RenderableProxies[i] = MyProxiesFactory.CreateRenderableProxy();
                }
            }

            for (int p = 0; p < Num; p++ )
            {
                var partId = MyMeshes.GetVoxelPart(Mesh, p);
                var technique = partId.Info.MaterialTriple.IsMultimaterial() ? MyVoxelMesh.MULTI_MATERIAL_TAG : MyVoxelMesh.SINGLE_MATERIAL_TAG;

                lod.RenderableProxies[p].WorldMatrix = m_owner.WorldMatrix;
                //lod.RenderableProxies[p].ObjectData.LocalMatrix = m_owner.WorldMatrix;
                lod.RenderableProxies[p].ObjectData.VoxelOffset = m_voxelOffset;
                lod.RenderableProxies[p].ObjectData.VoxelScale = m_voxelScale;
                lod.RenderableProxies[p].ObjectData.MassiveCenterRadius = Vector4.Zero;
                lod.RenderableProxies[p].ObjectData.Facing = 0;
                lod.RenderableProxies[p].ObjectData.WindScaleAndFreq = Vector2.Zero;

                lod.RenderableProxies[p].Mesh = lodMesh;
                lod.RenderableProxies[p].DepthShaders = MyMaterialShaders.Get(
                    X.TEXT(MapTechniqueToShaderMaterial(technique)), 
                    X.TEXT(MyGeometryRenderer.DEFAULT_DEPTH_PASS), 
                    lod.VertexLayout1, 
                    lod.VertexShaderFlags | MyShaderUnifiedFlags.DEPTH_ONLY | MapTechniqueToShaderMaterialFlags(technique));
                lod.RenderableProxies[p].Shaders = MyMaterialShaders.Get(
                    X.TEXT(MapTechniqueToShaderMaterial(technique)), 
                    X.TEXT(MyGeometryRenderer.DEFAULT_OPAQUE_PASS), 
                    lod.VertexLayout1,
                    lod.VertexShaderFlags | MapTechniqueToShaderMaterialFlags(technique));
                lod.RenderableProxies[p].ForwardShaders = MyMaterialShaders.Get(
                    X.TEXT(MapTechniqueToShaderMaterial(technique)),
                    X.TEXT(MyGeometryRenderer.DEFAULT_FORWARD_PASS),
                    lod.VertexLayout1,
                    lod.VertexShaderFlags | MapTechniqueToShaderMaterialFlags(technique));

                var partInfo = partId.Info;

                MyDrawSubmesh drawSubmesh = new MyDrawSubmesh
                {
                    BaseVertex = partInfo.BaseVertex,
                    StartIndex = partInfo.StartIndex,
                    IndexCount = partInfo.IndexCount,
                    BonesMapping = null,
                    MaterialId = MyVoxelMaterials1.GetMaterialProxyId(partId.Info.MaterialTriple)
                };

                lod.RenderableProxies[p].DrawSubmesh = drawSubmesh;

                lod.RenderableProxies[p].SkinningMatrices = null;

                lod.RenderableProxies[p].ObjectBuffer = MyCommon.GetObjectCB(objectConstantsSize);
                lod.RenderableProxies[p].InstanceCount = m_instanceCount;
                lod.RenderableProxies[p].StartInstance = m_startInstance;
                lod.RenderableProxies[p].Flags = MapTechniqueToRenderableFlags(technique) | m_additionalFlags;
                lod.RenderableProxies[p].Type = MapTechniqueToMaterialType(technique);
                lod.RenderableProxies[p].Parent = this;
                lod.RenderableProxies[p].Lod = 0;
                lod.RenderableProxies[p].Instancing = Instancing;

                ulong sortingKey = 0;

                My64BitValueHelper.SetBits(ref sortingKey, 36, 2, (ulong)lod.RenderableProxies[p].Type);
                My64BitValueHelper.SetBits(ref sortingKey, 32, 4, (ulong)drawSubmesh.MaterialId.Index);
                My64BitValueHelper.SetBits(ref sortingKey, 26, 6, (ulong)MyShaderMaterial.GetID(MapTechniqueToShaderMaterial(technique)));
                My64BitValueHelper.SetBits(ref sortingKey, 20, 6, (ulong)lod.VertexShaderFlags);
                //My64BitValueHelper.SetBits(ref sortingKey, 14, 6, (ulong)lod.VertexLayout1.Index);
                //My64BitValueHelper.SetBits(ref sortingKey, 0, 14, (ulong)m_owner.ID);      


                lod.SortingKeys[p] = sortingKey;
            }
        }

        // return true if can be removed from queue
        internal unsafe bool RebuildRenderProxies()
        {
            bool notReady = Mesh == MeshId.NULL;
            bool notNeeded = !IsRendered || !m_owner.RenderDirty;
            if(notReady)
            {
                return false;
            }
            if(notNeeded)
            {
                // If it is needed, the other branch will do this
                if (m_colorEmissivityDirty && IsRendered)
                {
                    m_colorEmissivityDirty = false;
                    foreach (var property in ModelProperties)
                    {
                        var L = Mesh.Info.LodsNum;
                    
                        for (var l = 0; l < L; ++l)
                        {
                            var submeshes = m_lods[l].RenderableProxies.Length;
                            for (int i = 0; i < submeshes; ++i)
                            {
                                var proxy = m_lods[l].RenderableProxies[i];
                                if (proxy.Material == property.Key.Material)
                                {
                                    proxy.ObjectData.Emissive = property.Value.Emissivity;
                                    proxy.ObjectData.ColorMul = property.Value.ColorMul;
                                }
                            }
                        }
                    }
                }
                return true;
            }

            if(!m_owner.m_localAabb.HasValue)
            {
                SetLocalAabbToModelLod(0);
            }
            

            if (m_btreeProxy == -1 && m_isRenderedStandalone)
            {
                m_btreeProxy = MyScene.RenderablesDBVH.AddProxy(ref m_owner.Aabb, m_cullProxy, 0);
            }

            if (!MyMeshes.IsVoxelMesh(Mesh))
            {
                var skinning = m_owner.GetComponent(MyActorComponentEnum.Skinning) as MySkinningComponent;
                bool skinningEnabled = skinning != null && skinning.SkinMatrices != null;
                var objectConstantsSize = sizeof(MyObjectData);

                if (skinningEnabled)
                {
                    objectConstantsSize += sizeof(Matrix) * 60;
                }

                DeallocateLodProxies();

                m_lods = new MyRenderLod[Mesh.Info.LodsNum];
                for (int i = 0; i < m_lods.Length; i++)
                {
                    m_lods[i] = new MyRenderLod();
                    RebuildLodProxy(i, skinningEnabled, skinning, objectConstantsSize);
                }
            }
            else
            {
                RebuildVoxelRenderProxies();
            }

                //// create proxies for transitions
                //for (int i = 0; i < m_lods.Length - 1; i++)
                //{
                //    m_lods[i].RenderableProxiesForLodTransition = m_lods[i].RenderableProxies.Concat(m_lods[i + 1].RenderableProxies).ToArray();
                //    m_lods[i].TransitionUpSortingKeys = m_lods[i].SortingKeys.Concat(m_lods[i + 1].SortingKeys).ToArray();
                //}

                //m_currentLod = 0;
                //m_prevLod = 0;
                //m_lodState = 0;
                //m_cullProxy.Proxies = m_lods[0].RenderableProxies;
                //m_cullProxy.SortingKeys = m_lods[0].SortingKeys;
            
            //

            m_renderableProxiesForLodTransition = new MyCullProxy[m_lods.Length - 1];
            float currentDistance = CalculateViewerDistance();
            m_lod = 0;
            for (int i = 0; i < m_lods.Length - 1; i++)
            {
                m_renderableProxiesForLodTransition[i] = new MyCullProxy();
                m_renderableProxiesForLodTransition[i].Proxies = m_lods[i].RenderableProxies.Concat(m_lods[i + 1].RenderableProxies).ToArray();
                m_renderableProxiesForLodTransition[i].SortingKeys = m_lods[i].SortingKeys.Concat(m_lods[i + 1].SortingKeys).ToArray();
            }

            for (int i = 0; i < m_lods.Length; i++)
            {
                if ( m_lods[i].Distance <= currentDistance && ((i == m_lods.Length - 1) || currentDistance < m_lods[i+1].Distance))
                { 
                    m_lod = i;
                }
            }

            m_lodTransitionState = 0;
            m_cullProxy.Proxies = m_lods[m_lod].RenderableProxies;
            m_cullProxy.SortingKeys = m_lods[m_lod].SortingKeys;

            //UpdateMaterialProxies();
            UpdateProxiesCustomAlpha();
            UpdateProxiesObjectData();

            //Debug.Assert(m_voxelLod == -1 || m_lods[0].RenderableProxies[0].ObjectData.CustomAlpha == m_voxelLod);

            OnFrameUpdate();

            if(MyScene.EntityDisabledMaterials.ContainsKey(m_owner.ID))
            {
                foreach( var row in MyScene.EntityDisabledMaterials[m_owner.ID])
                {
                    var submeshes = MyMeshes.GetLodMesh(Mesh, row.LOD).Info.PartsNum;
                    for(int i=0; i< submeshes; i++)
                    {
                        var part = MyMeshes.GetMeshPart(Mesh, row.LOD, i);
                        var proxy = m_lods[row.LOD].RenderableProxies[i];

                        if(part.Info.Material.Info.Name == row.Material)
                        {
                            proxy.Flags |= MyRenderableProxyFlags.SkipInMainView;
                        }
                    }
                }
            }

            foreach (var property in ModelProperties)
            {
                var L = Mesh.Info.LodsNum;

                for (var l = 0; l < L; ++l)
                {
                    var submeshes = MyMeshes.GetLodMesh(Mesh, l).Info.PartsNum;
                    for (int i = 0; i < submeshes; i++)
                    {
                        var part = MyMeshes.GetMeshPart(Mesh, l, i);
                        var proxy = m_lods[l].RenderableProxies[i];

                        if (part.Info.Material.Info.Name == property.Key.Material)
                        {
                            proxy.ObjectData.Emissive = property.Value.Emissivity;
                            proxy.ObjectData.ColorMul = property.Value.ColorMul;

                            //

                            if (property.Value.TextureSwaps != null)
                            {
                                var meshMat = part.Info.Material;
                                var info = meshMat.Info;

                                foreach (var s in property.Value.TextureSwaps)
                                {
                                    switch (s.MaterialSlot)
                                    {
                                        case "NormalGlossTexture":
                                            info.NormalGloss_Texture = s.TextureName;
                                            break;
                                        case "AddMapsTexture":
                                            info.Extensions_Texture = s.TextureName;
                                            break;
                                        case "AlphamaskTexture":
                                            info.Alphamask_Texture = s.TextureName;
                                            break;
                                        default:
                                            info.ColorMetal_Texture = s.TextureName;
                                            break;
                                    }
                                }

                                proxy.DrawSubmesh.MaterialId = MyMeshMaterials1.GetProxyId(MyMeshMaterials1.GetMaterialId(ref info));
                            }

                            else if (property.Value.CustomRenderedTexture != RwTexId.NULL)
                            {
                                MyMaterialProxyId matProxy = property.Value.CustomMaterialProxy;
                                if (matProxy == MyMaterialProxyId.NULL)
                                {
                                    matProxy = MyMaterials1.AllocateProxy();
                                    property.Value.CustomMaterialProxy = matProxy;

                                    MyMaterials1.ProxyPool.Data[matProxy.Index] = MyMaterials1.ProxyPool.Data[proxy.DrawSubmesh.MaterialId.Index];
                                    MyMaterials1.ProxyPool.Data[matProxy.Index].MaterialSRVs.SRVs = (ShaderResourceView[])MyMaterials1.ProxyPool.Data[matProxy.Index].MaterialSRVs.SRVs.Clone();
                                    MyMaterials1.ProxyPool.Data[matProxy.Index].MaterialSRVs.SRVs[0] = property.Value.CustomRenderedTexture.ShaderView;
                                    MyMaterials1.ProxyPool.Data[matProxy.Index].MaterialSRVs.Version = (int)m_owner.ID;
                                }

                                proxy.DrawSubmesh.MaterialId = matProxy;
                            }
                        }
                    }
                }
            }
            m_colorEmissivityDirty = false;
            m_owner.MarkRenderClean();
            return true;
        }

        public void UpdateColorEmissivity(int lod, string materialName, Color diffuse, float emissivity)
        {
            var key = new MyEntityMaterialKey { LOD = lod, Material = X.TEXT(materialName) };
            MyModelProperties properties;
            if (!ModelProperties.TryGetValue(key, out properties))
            {
                properties = new MyModelProperties();
                ModelProperties[key] = properties;
            }
            properties.Emissivity = emissivity;
            properties.ColorMul = diffuse;
            m_colorEmissivityDirty = true;
        }

        private void DeallocateLodProxies()
        {
            if (m_lods != null)
            {
                for (int i = 0; i < m_lods.Length; i++)
                    m_lods[i].DeallocateProxies();
                m_lods = null;
            }
        }

        internal void FreeCustomRenderTextures(MyEntityMaterialKey key)
        {
            var prop = ModelProperties[key];
            if (prop.CustomMaterialProxy != MyMaterialProxyId.NULL)
            {
                MyMaterials1.FreeProxy(prop.CustomMaterialProxy);
                prop.CustomMaterialProxy = MyMaterialProxyId.NULL;
            }
            if (prop.CustomRenderedTexture != RwTexId.NULL)
            {
                MyRwTextures.Destroy(prop.CustomRenderedTexture);
                prop.CustomRenderedTexture = RwTexId.NULL;
                --MyModelProperties.CustomTextures;
            }

        }

        void FreeCustomRenderTextures()
        {
            foreach (var prop in ModelProperties)
            {
                FreeCustomRenderTextures(prop.Key);
            }
        }


        const float LodDistanceTransitionThreshold = 4f;
        const float LodTransitionTime = 1.0f;

        float GetLodTransitionBorder(int lodNum)
        {
            return LodDistanceTransitionThreshold * (float)Math.Pow(2, lodNum);
        }

        void UpdateProxiesCustomAlpha()
        {
            if (m_lods != null)
            {
                for (int i = 0; i < m_lods.Length; i++)
                {
                    for (int j = 0; j < m_lods[i].RenderableProxies.Length; j++)
                    {
                        var proxy = m_lods[i].RenderableProxies[j];
                        float value = 0;

                        if (m_objectDithering != 0 || (proxy.Instancing != InstancingId.NULL && proxy.Instancing.Info.Type == MyRenderInstanceBufferType.Cube))
                        {
                            value = m_objectDithering;
                        }
                        else if (IsLodTransitionInProgress)
                        {
                            // Value over 1 is interpreted by the shader to do dithering with an inversed mask
                            // This is done so that when blending between two lod levels, one pixel will be from current lod
                            // and the other from the next lod and there are no missing pixels.
                            // Could not use negative because that currently means hologram rendering.
                            value = (proxy.Lod == m_lod ? Math.Abs(m_lodTransitionState) : (2.0f - Math.Abs(m_lodTransitionState)));
                        }

                        proxy.ObjectData.CustomAlpha = value;
                        proxy.ObjectData.VoxelLodSize = m_voxelLod;
                    }
                }
            }
        }

        void SetProxiesForCurrentLod()
        {
            m_cullProxy.Proxies = !IsLodTransitionInProgress ? m_lods[m_lod].RenderableProxies : m_renderableProxiesForLodTransition[LodTransitionProxyIndex].Proxies;
            m_cullProxy.SortingKeys = !IsLodTransitionInProgress ? m_lods[m_lod].SortingKeys : m_renderableProxiesForLodTransition[LodTransitionProxyIndex].SortingKeys;
        }

        bool IsLodTransitionInProgress { get { return m_lodTransitionState != 0; } }
        int LodTransitionProxyIndex { get { return m_lodTransitionState > 0 ? m_lod  : m_lod - 1; } }

        internal void UpdateLodState()
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("UpdateLodState");
            if (m_lods != null && MyMeshes.IsVoxelMesh(Mesh))
            {
                for (int i = 0; i < m_lods.Length; i++)
                {
                    for (int j = 0; j < m_lods[i].RenderableProxies.Length; j++)
                    {
                        m_lods[i].RenderableProxies[j].ObjectData.MassiveCenterRadius = new Vector4(0);
                    }
                }
            }

            if (m_lods == null || !IsRendered)
            {
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
                return;
            }

            var distance = CalculateViewerDistance();
            bool isInstancingGeneric = Instancing == InstancingId.NULL ? false : MyInstancing.Instancings.Data[Instancing.Index].Type == MyRenderInstanceBufferType.Generic;
            if (MyRenderSettings.PerInstanceLods && isInstancingGeneric)
            {
                UpdatePerInstanceLods(distance);
            }

            if (m_lods.Length == 1)
            {
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
                return;
            }

            if(distance.IsValid())
            {
                if (m_lodTransitionState != 0)
                {
                    //For now independent from movement speed
                    float state = 0; // Math.Abs(distance - m_lodTransitionStartDistance) / Math.Max(m_lodTransitionVector, 4.0f) * 2.0f;
                    state = Math.Max(Math.Abs(m_lodTransitionState) + (float)MyRender11.TimeDelta.Seconds / LodTransitionTime, MathHelper.Clamp(state, 0.0f, 1.0f));

                    // Transition should be at least 4 frames
                    float delta = Math.Abs(Math.Abs(m_lodTransitionState) - state);
                    delta = Math.Min(delta, 0.25f);

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
                        if (Math.Abs(distance - m_lods[m_lod].Distance) > GetLodTransitionBorder(m_lod))
                        {
                            m_lodBorder = false;
                        }
                    }
                    else
                    {
                        var lod = 0;
                        for (int i = 0; i < m_lods.Length; i++)
                        {
                            if (m_lods[i].Distance <= distance && ((i == m_lods.Length - 1) || distance < m_lods[i + 1].Distance))
                            {
                                lod = i;
                            }
                        }

                        if (lod != m_lod && (!MyRenderSettings.PerInstanceLods || !isInstancingGeneric || lod == m_lods.Length - 1))
                        {
                            m_lodTransitionState = lod < m_lod ? -0.001f : 0.001f;
                            m_lodTransitionStartDistance = distance;
                            m_lodTransitionVector = GetLodTransitionBorder(m_lod) * 2;
                            m_lodBorder = true;

                            SetProxiesForCurrentLod();
                            UpdateProxiesCustomAlpha();
                            SetLodShaders(m_lod, MyShaderUnifiedFlags.DITHERED);
                            SetLodShaders(lod, MyShaderUnifiedFlags.DITHERED);
                        }
                    }
                }
            }
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        private void UpdatePerInstanceLods(float distance)
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Instancing");
            if (m_lods.Length > 1)
            {
                var instanceInfo = MyInstancing.Instancings.Data[Instancing.Index];
                var instanceLodComponent = MyInstancing.GetInstanceActor(this).GetInstanceLod();

                //TODO(AF) remove after lod distances are adjusted
                float debugDistanceMultiplier = 5;

                if (distance < m_lods[m_lods.Length - 1].Distance * debugDistanceMultiplier)
                {
                    int capacity = instanceInfo.TotalCapacity;

                    float lodDistanceSquared = m_lods[m_lods.Length - 1].Distance * m_lods[m_lods.Length - 1].Distance * debugDistanceMultiplier * debugDistanceMultiplier;
                    Vector3D translation = this.m_lods[m_lods.Length - 1].RenderableProxies[0].WorldMatrix.Translation;

                    for (int i = 0; i < capacity; i++)
                    {
                        Vector3D position = (Vector3D)instanceInfo.Positions[i] + translation;
                        double distanceToCameraSquared = (position - MyEnvironment.CameraPosition).LengthSquared();

                        bool oldIsInstanceFar = instanceLodComponent.IsFar(Instancing, i);
                        bool isInstanceFar = distanceToCameraSquared > lodDistanceSquared;

                        if (!isInstanceFar)
                        {
                            bool lodFound = false;
                            for (int l = 0; l < m_lods.Length - 1; l++)
                            {
                                if (distanceToCameraSquared < m_lods[l + 1].Distance * m_lods[l + 1].Distance * debugDistanceMultiplier * debugDistanceMultiplier)
                                {
                                    instanceLodComponent.SetLod(Instancing, i, l);
                                    lodFound = true;
                                    break;
                                }
                            }
                            if (!lodFound)
                            {
                                instanceLodComponent.SetLod(Instancing, i, m_lods.Length - 2);
                            }
                        }

                        if (isInstanceFar != oldIsInstanceFar)
                        {
                            if (isInstanceFar)
                            {
                                instanceLodComponent.RemoveInstanceLod(Instancing, i);
                            }
                            else
                            {
                                MyRenderableProxy[][] newProxies = new MyRenderableProxy[m_lods.Length - 1][];
                                ulong[][] newSortingKeys = new ulong[m_lods.Length - 1][];

                                BoundingBoxD aabb = BoundingBoxD.CreateInvalid();
                                for (int l = 0; l < m_lods.Length - 1; l++)
                                {
                                    // Switch to LOD0, add proxies
                                    int proxyCount = m_lods[l].RenderableProxies.Length;
                                    var lodProxies = new MyRenderableProxy[proxyCount];
                                    var lodSortingKeys = new ulong[proxyCount];
                                    for (int p = 0; p < proxyCount; p++)
                                    {
                                        var newProxy = MyProxiesFactory.CreateRenderableProxy();
                                        var oldProxy = m_lods[m_lods.Length - 1].RenderableProxies[0];
                                        var originalProxy = m_lods[l].RenderableProxies[p];
                                        lodProxies[p] = newProxy;

                                        newProxy.WorldMatrix = instanceInfo.InstanceData[i].LocalMatrix;
                                        newProxy.WorldMatrix.Translation = newProxy.WorldMatrix.Translation + oldProxy.WorldMatrix.Translation;

                                        newProxy.ObjectData = oldProxy.ObjectData;
                                        newProxy.ObjectData.LocalMatrix = newProxy.WorldMatrix;
                                        newProxy.ObjectData.Facing = 0;
                                        

                                        newProxy.Mesh = originalProxy.Mesh;
                                        newProxy.Instancing = InstancingId.NULL;
                                        newProxy.DrawSubmesh = originalProxy.DrawSubmesh;
                                        newProxy.PerMaterialIndex = originalProxy.PerMaterialIndex;
                                        newProxy.InstanceCount = 0;
                                        newProxy.StartInstance = 0;
                                        newProxy.Type = originalProxy.Type;
                                        newProxy.Flags = originalProxy.Flags;
                                        newProxy.Flags |= MyRenderableProxyFlags.DrawOutsideViewDistance;
                                        newProxy.Lod = 0;
                                        newProxy.ObjectBuffer = oldProxy.ObjectBuffer;
                                        newProxy.Parent = instanceLodComponent;
                                        newProxy.Material = originalProxy.Material;

                                        var partId = MyMeshes.GetMeshPart(Mesh, l, p);
                                        var technique = partId.Info.Material.Info.Technique;

                                        newProxy.ObjectData.WindScaleAndFreq = partId.Info.Material.Info.WindScaleAndFreq;

                                        var depthFlags = m_lods[l].VertexShaderFlags | MyShaderUnifiedFlags.DEPTH_ONLY | MyShaderUnifiedFlags.DITHERED | MapTechniqueToShaderMaterialFlags(technique);
                                        depthFlags &= ~MyShaderUnifiedFlags.USE_GENERIC_INSTANCING;
                                        newProxy.DepthShaders = MyMaterialShaders.Get(
                                            X.TEXT(MapTechniqueToShaderMaterial(technique)),
                                            X.TEXT(MyGeometryRenderer.DEFAULT_DEPTH_PASS),
                                            m_lods[l].VertexLayout1,
                                            depthFlags);
                                        var shaderFlags = m_lods[l].VertexShaderFlags | MyShaderUnifiedFlags.DITHERED | MapTechniqueToShaderMaterialFlags(technique);
                                        shaderFlags &= ~MyShaderUnifiedFlags.USE_GENERIC_INSTANCING;
                                        newProxy.Shaders = MyMaterialShaders.Get(
                                            X.TEXT(MapTechniqueToShaderMaterial(technique)),
                                            X.TEXT(MyGeometryRenderer.DEFAULT_OPAQUE_PASS),
                                            m_lods[l].VertexLayout1,
                                            shaderFlags);
                                        var forwardFlags = m_lods[l].VertexShaderFlags | MyShaderUnifiedFlags.DITHERED | MapTechniqueToShaderMaterialFlags(technique);
                                        forwardFlags &= ~MyShaderUnifiedFlags.USE_GENERIC_INSTANCING;
                                        newProxy.ForwardShaders = MyMaterialShaders.Get(
                                            X.TEXT(MapTechniqueToShaderMaterial(technique)),
                                            X.TEXT(MyGeometryRenderer.DEFAULT_FORWARD_PASS),
                                            m_lods[l].VertexLayout1,
                                            forwardFlags);

                                        lodSortingKeys[p] = m_lods[l].SortingKeys[p];
                                        aabb.Include((BoundingBoxD)originalProxy.Mesh.Info.BoundingBox.Value);
                                    }

                                    newProxies[l] = lodProxies;
                                    newSortingKeys[l] = lodSortingKeys;
                                }
                                aabb.Translate(position);
                                instanceLodComponent.AddInstanceLod(Instancing, i, newProxies, newSortingKeys, aabb);
                            }
                        }
                    }
                }
                else if (instanceInfo.NonVisibleInstanceCount > 0)
                {
                    // Reset all
                    for (int i = 0; i < instanceInfo.TotalCapacity; i++)
                    {
                        if (!instanceInfo.VisibilityMask[i])
                        {
                            instanceLodComponent.RemoveInstanceLod(Instancing, i);
                        }
                    }
                    instanceInfo.ResetVisibility();
                }
            }
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        internal void OnFrameUpdate()
        {
            UpdateLodState();
        }

        internal override void OnRemove(MyActor owner)
        {
            FreeCustomRenderTextures();
            DebrisEntityVoxelMaterial.Remove(owner.ID);

            base.OnRemove(owner);

            this.Deallocate();
        }

        #region Helpers

        internal float CalculateViewerDistance()
        {
            return (float)m_owner.Aabb.Distance(MyEnvironment.CameraPosition);
        }

        internal static readonly string DEFAULT_MATERIAL_TAG = "standard";
        internal static readonly string ALPHA_MASKED_MATERIAL_TAG = "alpha_masked";

        internal static string MapTechniqueToShaderMaterial(string technique)
        {
            switch (technique)
            {
                case MyVoxelMesh.SINGLE_MATERIAL_TAG:
                case MyVoxelMesh.MULTI_MATERIAL_TAG:
                    return technique;
                case "ALPHA_MASKED":
                case "FOLIAGE":
                    return ALPHA_MASKED_MATERIAL_TAG;
                default:
                    return DEFAULT_MATERIAL_TAG;
            }
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

        internal static void FillPerMaterialData(ref MyPerMaterialData perMaterialData, string technique)
        {
            perMaterialData.Type = 0;

            switch(technique)
            {
                case "FOLIAGE":
                    perMaterialData.Type = MyMaterialTypeEnum.FOLIAGE;
                    break;
            }
        }

        internal static bool AreTechniqueDrawcallsDepthBatchable(string technique)
        {
            switch (technique)
            {
                case "ALPHA_MASKED":
                case "FOLIAGE":
                    return false;
                default:
                    return true;
            }
        }

        internal static MyShaderUnifiedFlags MapTechniqueToShaderMaterialFlags(string technique)
        {
			MyShaderUnifiedFlags flags = MyShaderUnifiedFlags.USE_SHADOW_CASCADES;
            switch(technique)
            {
                case "ALPHA_MASKED":
					flags |= MyShaderUnifiedFlags.ALPHAMASK;
					break;
                case "FOLIAGE":
					flags |= MyShaderUnifiedFlags.ALPHAMASK;// | MyShaderUnifiedFlags.FOLIAGE;
					break;
                default:
					flags |= MyShaderUnifiedFlags.NONE;
					break;
            }

			return flags;
        }

        internal static MyMaterialFlags MapTechniqueToMaterialFlags(string technique)
        {
            switch(technique)
            {
                case "CLOTH":
                    return MyMaterialFlags.RGB_COLORING;
            }

            return MyMaterialFlags.NONE;
        }

        internal static MyRenderableProxyFlags MapTechniqueToRenderableFlags(string technique)
        {
            switch (technique)
            {
                case "ALPHA_MASKED":
                case "FOLIAGE":
				case "CLOUD_LAYER":
                    return MyRenderableProxyFlags.DisableFaceCulling;
                default:
                    return MyRenderableProxyFlags.DepthSkipTextures;
            }
        }

        internal static MyMaterialType MapTechniqueToMaterialType(string technique)
        {
            switch (technique)
            {
                case "ALPHA_MASKED":
                case "FOLIAGE":
                    return MyMaterialType.ALPHA_MASKED;
                default:
                    return MyMaterialType.OPAQUE;
            }
        }

        #endregion

        internal static void MarkAllDirty()
        {
            foreach (var r in MyComponentFactory<MyRenderableComponent>.GetAll())
            {
                r.m_owner.MarkRenderDirty();
            }
        }
    }
}
