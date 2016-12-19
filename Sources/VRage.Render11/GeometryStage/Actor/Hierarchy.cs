using System;
using System.Collections.Generic;
using VRage.Generics;
using VRage.Render11.Resources;
using VRage.Utils;
using VRageMath;
using Matrix = VRageMath.Matrix;
using Vector3 = VRageMath.Vector3;


namespace VRageRender
{
    class MyGroupLeafComponent : MyActorComponent
    {
        public MyActor Parent
        {
            get;
            internal set;
        }

        public MyGroupRootComponent RootGroup
        {
            get;
            internal set;
        }

        public MyMaterialMergeGroup MergeGroup
        {
            get;
            internal set;
        }

        public bool Mergeable
        {
            get;
            internal set;
        }

        internal override void Construct()
        {
            base.Construct();
            Type = MyActorComponentEnum.GroupLeaf;

            Parent = null;
            MergeGroup = null;
            Mergeable = false;
        }

        internal override void OnMatrixChange()
        {
            
        }

        internal override void OnRemove(MyActor owner)
        {
            if (MergeGroup != null)
            {
                MergeGroup.RemoveEntity(Owner);
                MergeGroup = null;
            }

            if (Parent != null)
            {
                var root = Parent.GetGroupRoot();
                if (root != null)
                {
                    root.Remove(this);
                }
                Parent = null;
            }

            base.OnRemove(owner);

            this.Deallocate();
        }
    }

    class MyBigMeshTable
    {
        internal static MyMeshTableSrv Table = new MyMeshTableSrv();
    }

    class MyCullProxy_2
    {
        internal UInt64[] SortingKeys;
        internal MyRenderableProxy_2[] Proxies;

        internal void Construct()
        {
            SortingKeys = MyRenderableProxy_2.EmptyKeyList;
            Proxies = MyRenderableProxy_2.EmptyList;
        }

        internal void Resize(int size)
        {
            Array.Resize(ref SortingKeys, size);
            Array.Resize(ref Proxies, size);
        }

        internal void Extend(int size)
        {
            //Debug.Assert(size > SortingKeys.Length);
            Resize(size);
        }

        internal void Clear()
        {
            SortingKeys = MyRenderableProxy_2.EmptyKeyList;
            Proxies = MyRenderableProxy_2.EmptyList;
        }

        static MyObjectsPool<MyCullProxy_2> Pool = new MyObjectsPool<MyCullProxy_2>(128);

        internal static MyCullProxy_2 Allocate()
        {
            MyCullProxy_2 cullProxy2 = null;
            Pool.AllocateOrCreate(out cullProxy2);
            cullProxy2.Construct();
            return cullProxy2;
        }

        internal static void Free(MyCullProxy_2 cullProxy2)
        {
            cullProxy2.Clear();
            Pool.Deallocate(cullProxy2);
        }
    }

    class MyMaterialMergeGroup
    {
        static readonly MyStringId STANDARD_MATERIAL = MyStringId.GetOrCompute("standard");

        //string m_rootMaterial;
        int m_rootMaterialRK;

        private MyMergeInstancing m_mergeGroup;
        private Dictionary<uint, MyActor> m_actors;
        private Dictionary<MyActor, int> m_actorIndices;

        public int Index
        {
            get;
            private set;
        }

        public MyMergeInstancing MergeGroup
        {
            get { return m_mergeGroup; }
        }

        internal MyMaterialMergeGroup(MyMeshTableSrv meshTable, MyMeshMaterialId matId, int index)
        {
            m_mergeGroup = new MyMergeInstancing(meshTable);
            m_rootMaterialRK = MyMeshMaterials1.Table[matId.Index].RepresentationKey;
            Index = index;

            m_actors = new Dictionary<uint, MyActor>();
            m_actorIndices = new Dictionary<MyActor, int>();
        }

        public bool TryGetActorIndex(MyActor actor, out int index)
        {
            return m_actorIndices.TryGetValue(actor, out index);
        }

        internal void AddEntity(MyActor actor, MeshId model)
        {
            m_actors[actor.ID] = actor;
            m_mergeGroup.AddEntity(actor, model);
        }

        internal void RemoveEntity(MyActor actor)
        {
            m_actors.Remove(actor.ID);
            m_mergeGroup.RemoveEntity(actor);
        }

        internal void UpdateEntity(MyActor actor)
        {
            //var matrix = actor.WorldMatrix;
            //matrix.Translation = matrix.Translation - MyRender11.Environment.CameraPosition;
            m_mergeGroup.UpdateEntity(actor, ref actor.WorldMatrix, actor.GetRenderable().m_depthBias);
        }

        internal void UpdateAll()
        {
            foreach(var actor in m_actors.Values)
            {
                UpdateEntity(actor);
            }
        }

        internal unsafe void BuildProxy(out MyRenderableProxy_2 proxy, out UInt64 key)
        {
            MyCommon.GetObjectCB(sizeof(MyMergeInstancingConstants));
            var material = MyMeshMaterials1.GetProxyId(MyMeshMaterials1.MaterialRkIndex.Get(m_rootMaterialRK, MyMeshMaterialId.NULL));
            proxy = new MyRenderableProxy_2
            {
                MaterialType = MyMaterialType.OPAQUE,

                ObjectConstants = new MyConstantsPack { },

                ObjectSrvs = new MySrvTable { StartSlot = MyCommon.INSTANCE_INDIRECTION, Srvs = m_mergeGroup.m_srvs, BindFlag = MyBindFlag.BIND_VS, Version = this.GetHashCode() },

                DepthShaders = GetMergeInstancing(MyMaterialShaders.DEPTH_PASS_ID, MyShaderUnifiedFlags.DEPTH_ONLY),
                HighlightShaders = GetMergeInstancing(MyMaterialShaders.HIGHLIGHT_PASS_ID),
                Shaders = GetMergeInstancing(MyMaterialShaders.GBUFFER_PASS_ID),
                ForwardShaders = GetMergeInstancing(MyMaterialShaders.FORWARD_PASS_ID, MyShaderUnifiedFlags.USE_SHADOW_CASCADES),

                RenderFlags = MyRenderableProxyFlags.DepthSkipTextures,

                Submeshes = new MyDrawSubmesh_2[] { new MyDrawSubmesh_2 { DrawCommand = MyDrawCommandEnum.Draw, Count = m_mergeGroup.VerticesNum, MaterialId = material } },
                SubmeshesDepthOnly = new MyDrawSubmesh_2[] { new MyDrawSubmesh_2 { DrawCommand = MyDrawCommandEnum.Draw, Count = m_mergeGroup.VerticesNum, MaterialId = material } },

                InstanceCount = 0,
                StartInstance = 0,
            };

            key = 0;
        }

        private static MyMergeInstancingShaderBundle GetMergeInstancing(MyStringId pass, MyShaderUnifiedFlags flags = MyShaderUnifiedFlags.NONE)
        {
            MyMergeInstancingShaderBundle ret = new MyMergeInstancingShaderBundle();

            flags |= MyShaderUnifiedFlags.USE_MERGE_INSTANCING;

            ret.MultiInstance = MyMaterialShaders.Get(STANDARD_MATERIAL, pass, MyVertexLayouts.Empty, flags, MyFileTextureEnum.UNSPECIFIED);
            ret.SingleInstance = MyMaterialShaders.Get(STANDARD_MATERIAL, pass, MyVertexLayouts.Empty, flags | MyShaderUnifiedFlags.USE_SINGLE_INSTANCE, MyFileTextureEnum.UNSPECIFIED);
            return ret;
        }

        internal void UpdateProxySubmeshes(ref MyRenderableProxy_2 proxy, bool rootGroupDirtyTree)
        {
            if (m_mergeGroup.TableDirty)
            {
                proxy.Submeshes[0].Count = m_mergeGroup.VerticesNum;
                proxy.SubmeshesDepthOnly[0].Count = m_mergeGroup.VerticesNum;

                UpdateProxySectionSubmeshes(ref proxy);
            } else if (rootGroupDirtyTree)
            {
                UpdateProxySectionSubmeshes(ref proxy);
            }
        }

        internal void UpdateProxySectionSubmeshes(ref MyRenderableProxy_2 proxy)
        {
            int filledSize;
            MyInstanceEntityInfo[] infos = m_mergeGroup.GetEntityInfos(out filledSize);

            // NB: It's important here to keep SectionSubmeshes same fill size as the
            // merge group instances, keeping also the holes. In this way, indexing
            // is kept consistent with the shader and we don't need other indirections
            proxy.SectionSubmeshes = new MyDrawSubmesh_2[filledSize][];

            m_actorIndices.Clear();

            int actorIndex = 0;
            for (int it = 0; it < filledSize; it++)
            {
                MyInstanceEntityInfo info = infos[it];
                if (info.EntityId.HasValue)
                {
                    MyActor actor = m_actors[info.EntityId.Value];
                    int indexOffset = info.PageOffset * m_mergeGroup.TablePageSize;

                    UpdateActorSubmeshes(ref proxy, actor, actorIndex, indexOffset);
                    m_actorIndices[actor] = actorIndex;
                }

                actorIndex++;
            }
        }

        /// <returns>Actor full mesh indices count</returns>
        private void UpdateActorSubmeshes(ref MyRenderableProxy_2 proxy, MyActor actor, int actorIndex, int indexOffset)
        {
            MyRenderableComponent component = actor.GetRenderable();
            MyRenderableProxy proxy1 = component.Lods[0].RenderableProxies[0];
            MyDrawSubmesh_2 sectionSubmesh = proxy.Submeshes[0];

            MyDrawSubmesh_2[] sectionSubmeshes = new MyDrawSubmesh_2[proxy1.SectionSubmeshes.Length];
            proxy.SectionSubmeshes[actorIndex] = sectionSubmeshes;
            for (int it = 0; it < proxy1.SectionSubmeshes.Length; it++)
            {
                MyDrawSubmesh sectionSubmesh1 = proxy1.SectionSubmeshes[it];
                sectionSubmesh.Count = sectionSubmesh1.IndexCount;
                sectionSubmesh.Start = indexOffset + sectionSubmesh1.StartIndex;
                sectionSubmeshes[it] = sectionSubmesh;
            }
        }

        internal void MoveToGPU()
        {
            m_mergeGroup.MoveToGPU();
        }

        internal void Release()
        {
            m_mergeGroup.OnDeviceReset();
        }
    }

    class MyGroupRootComponent : MyActorComponent
    {
        internal List<MyActor> m_children;
        internal Dictionary<int, MyMaterialMergeGroup> m_materialGroups;
        bool m_dirtyPosition;
        bool m_dirtyTree;
        internal int m_btreeProxy;
        internal MyCullProxy_2 m_proxy;
        bool m_dirtyProxy;
        internal int m_mergablesCounter;
        bool m_isMerged;

        const int MERGE_THRESHOLD = 4;

        internal override void Construct()
        {
            base.Construct();
            Type = MyActorComponentEnum.GroupRoot;

            m_children = new List<MyActor>();
            m_dirtyPosition = false;
            m_dirtyTree = false;
            m_btreeProxy = -1;
            m_dirtyProxy = false;
            m_materialGroups = new Dictionary<int, MyMaterialMergeGroup>();
            m_mergablesCounter = 0;
            m_isMerged = false;

            m_proxy = MyCullProxy_2.Allocate();
        }

        public MyMaterialMergeGroup GetMaterialGroup(MyMeshMaterialId matId)
        {
            int rootMaterialRK = MyMeshMaterials1.Table[matId.Index].RepresentationKey;
            return m_materialGroups[rootMaterialRK];
        }

        public bool TryGetMaterialGroup(MyMeshMaterialId matId, out MyMaterialMergeGroup group)
        {
            int rootMaterialRK = MyMeshMaterials1.Table[matId.Index].RepresentationKey;
            return m_materialGroups.TryGetValue(rootMaterialRK, out group);
        }

        internal void OnDeviceReset()
        {
            foreach(var mg in m_materialGroups.Values)
            {
                mg.MergeGroup.OnDeviceReset();
            }
            m_dirtyProxy = true;
        }

        internal void RebuildProxies()
        {
            if (m_dirtyProxy)
            {
                m_proxy.Extend(m_materialGroups.Count);

                foreach(var kv in m_materialGroups)
                {
                    var index = kv.Value.Index;
                    kv.Value.BuildProxy(out m_proxy.Proxies[index], out m_proxy.SortingKeys[index]);
                }

                m_dirtyProxy = false;
            }
        }

        internal void PropagateMatrixChange(MyActor child)
        {
            var matrix = child.RelativeTransform.HasValue
                        ? (MatrixD)child.RelativeTransform.Value * Owner.WorldMatrix
                        : Owner.WorldMatrix;
            child.SetMatrix(ref matrix);
        }

        internal void TurnIntoMergeInstancing()
        {
            foreach(var child in m_children)
            {
                Merge(child);
            }

            m_isMerged = true;
        }

        internal void TurnIntoSeparateRenderables()
        {
            foreach (var childActor in m_children)
            {
                var renderableComponent = childActor.GetRenderable();

                var model = renderableComponent.GetModel();
                var material = MyMeshes.GetMeshPart(model, 0, 0).Info.Material;

                bool fracture = model.Info.RuntimeGenerated || model.Info.Dynamic;

                if (MyMeshMaterials1.IsMergable(material) && MyBigMeshTable.Table.IsMergable(model) && !fracture)
                {
                    if(childActor.GetGroupLeaf().MergeGroup != null)
                    {
                        var materialRk = MyMeshMaterials1.Table[material.Index].RepresentationKey;
                        var mergeGroupForMaterial = m_materialGroups.Get(materialRk);
                        if (mergeGroupForMaterial == null)
                            continue;

                        renderableComponent.IsRenderedStandAlone = true;
                        MyGroupLeafComponent leafComponent = childActor.GetGroupLeaf();
                        leafComponent.RootGroup = null;
                        childActor.GetGroupLeaf().MergeGroup = null;

                        mergeGroupForMaterial.RemoveEntity(childActor);
                    }
                }
            }

            m_isMerged = false;
        }

        internal void Remove(MyGroupLeafComponent leaf)
        {
            m_mergablesCounter = leaf.Mergeable ? m_mergablesCounter - 1 : m_mergablesCounter;

            if (m_mergablesCounter < MERGE_THRESHOLD && m_isMerged)
            {
                TurnIntoSeparateRenderables();
            }

            m_children.Remove(leaf.Owner);
        }

        internal void Merge(MyActor child)
        {
            var renderableComponent = child.GetRenderable();

            var model = renderableComponent.GetModel();
            var material = MyMeshes.GetMeshPart(model, 0, 0).Info.Material;

            bool fracture = model.Info.RuntimeGenerated || model.Info.Dynamic;

            if (MyMeshMaterials1.IsMergable(material) && MyBigMeshTable.Table.IsMergable(model) && !fracture)
            {
                var materialRk = MyMeshMaterials1.Table[material.Index].RepresentationKey;
                var mergeGroupForMaterial = m_materialGroups.Get(materialRk);
                if (mergeGroupForMaterial == null)
                {
                    var proxyIndex = m_materialGroups.Count;
                    mergeGroupForMaterial = new MyMaterialMergeGroup(MyBigMeshTable.Table, material, proxyIndex);
                    m_materialGroups[MyMeshMaterials1.Table[material.Index].RepresentationKey] = mergeGroupForMaterial;

                    m_dirtyProxy = true;
                }

                renderableComponent.IsRenderedStandAlone = false;
                child.GetGroupLeaf().RootGroup = this;
                child.GetGroupLeaf().MergeGroup = mergeGroupForMaterial;

                mergeGroupForMaterial.AddEntity(child, model);
                mergeGroupForMaterial.UpdateEntity(child);
            }
        }

        internal void Add(MyActor child)
        {
            child.AddComponent<MyGroupLeafComponent>(MyComponentFactory<MyGroupLeafComponent>.Create());

            child.GetGroupLeaf().Parent = Owner;

            m_children.Add(child);

            if (child.RelativeTransform == null)
            {
                child.RelativeTransform = (Matrix)(child.WorldMatrix * MatrixD.Invert(Owner.WorldMatrix));
            }

            if (!Owner.LocalAabb.HasValue)
            {
                Owner.LocalAabb = child.LocalAabb;
            }
            else
            {
                var localAabb = child.LocalAabb.Value;
                Owner.LocalAabb = Owner.LocalAabb.Value.Include(ref localAabb);
            }

            PropagateMatrixChange(child);

            if(child.GetRenderable() == null)
            {
                return;
            }

            //var mesh = child.GetRenderable().GetMesh();
            var model = child.GetRenderable().GetModel();
            var material = MyMeshes.GetMeshPart(model, 0, 0).Info.Material;

            bool fracture = model.Info.RuntimeGenerated || model.Info.Dynamic;

            if (MyMeshMaterials1.IsMergable(material) && MyBigMeshTable.Table.IsMergable(model) && !fracture)
            {
                child.GetGroupLeaf().Mergeable = true;

                MyBigMeshTable.Table.AddMesh(model);
                m_mergablesCounter++;

                if(!m_isMerged && m_mergablesCounter >= MERGE_THRESHOLD)
                {
                    TurnIntoMergeInstancing();
                }
                else if(m_isMerged)
                {
                    Merge(child);
                }

                //var materialRk = MyMeshMaterials1.Table[material.Index].RepresentationKey;
                //var mergeGroupForMaterial = m_materialGroups.Get(materialRk);
                //if (mergeGroupForMaterial == null)
                //{
                //    var proxyIndex = m_materialGroups.Count;
                //    mergeGroupForMaterial = new MyMaterialMergeGroup(MyBigMeshTable.Table, material, proxyIndex);
                //    m_materialGroups[MyMeshMaterials1.Table[material.Index].RepresentationKey] = mergeGroupForMaterial;

                //    m_dirtyProxy = true;
                //}

                //child.GetRenderable().SetStandaloneRendering(false);
                //child.GetGroupLeaf().m_mergeGroup = mergeGroupForMaterial;

                
                //mergeGroupForMaterial.AddEntity(child, model);
                //mergeGroupForMaterial.UpdateEntity(child);
            }
            else
            {
                //Debug.WriteLine(String.Format("Mesh {0}, material {1} is not mergable", mesh.Name, material));
            }

            m_dirtyTree = true;
        }

        internal void UpdateBeforeDraw()
        {
            if(m_dirtyProxy)
            {
                RebuildProxies();
            }

            foreach (var val in m_materialGroups.Values)
            {
                var index = val.Index;
                val.UpdateProxySubmeshes(ref m_proxy.Proxies[index], m_dirtyTree);
            }

            if (m_dirtyPosition)
            {
                foreach (var val in m_materialGroups.Values)
                {
                    val.UpdateAll();
                }

                m_dirtyPosition = false;
            }

            if (m_dirtyTree)
            {
                var bb = BoundingBoxD.CreateInvalid();

                foreach (var child in m_children)
                {
                    if (child.IsVisible && child.GetRenderable() != null && !child.GetRenderable().IsRendered)
                    {
                        bb.Include(child.Aabb);
                    }
                }

                Owner.Aabb = bb;

                if (m_materialGroups.Count > 0)
                {
                    if (m_btreeProxy == -1)
                        m_btreeProxy = MyScene.GroupsDBVH.AddProxy(ref Owner.Aabb, m_proxy, 0);
                    else
                        MyScene.GroupsDBVH.MoveProxy(m_btreeProxy, ref Owner.Aabb, Vector3.Zero);
                }

                m_dirtyTree = false;
            }
        }

        internal override void OnMatrixChange()
        {
            if (m_children != null)
            {
                foreach (var child in m_children)
                {
                    PropagateMatrixChange(child);
                }
            }

            m_dirtyPosition = true;
            m_dirtyTree = true;

            base.OnMatrixChange();
        }

        internal override void OnRemove(MyActor owner)
        {
            if (m_children != null)
            {
                for (int i = 0; i < m_children.Count; i++)
                {
                    m_children[i].RemoveComponent<MyGroupLeafComponent>(m_children[i].GetGroupLeaf());
                }
                m_children = null;
            }

            foreach (var val in m_materialGroups.Values)
            {
                val.Release();
            }
            m_materialGroups.Clear();

            m_dirtyTree = true;

            if (m_proxy != null)
            {
                MyCullProxy_2.Free(m_proxy);
                m_proxy = null;
            }

            if(m_btreeProxy != -1)
            {
                MyScene.GroupsDBVH.RemoveProxy(m_btreeProxy);
                m_btreeProxy = -1;
            }

            base.OnRemove(owner);

            this.Deallocate();
        }
    }
}
