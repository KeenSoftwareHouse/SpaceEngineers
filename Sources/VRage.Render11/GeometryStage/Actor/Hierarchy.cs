using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Generics;

using VRageMath;
using VRageRender.Resources;
using VRageRender.Vertex;
using Buffer = SharpDX.Direct3D11.Buffer;
using Matrix = VRageMath.Matrix;
using Vector3 = VRageMath.Vector3;
using BoundingBox = VRageMath.BoundingBox;
using BoundingFrustum = VRageMath.BoundingFrustum;
using VRage.Collections;
using System.Collections.Specialized;
using System.Threading;
using VRage.Utils;


namespace VRageRender
{
    class MyGroupLeafComponent : MyActorComponent
    {
        internal MyActor m_parent;
        internal MyMaterialMergeGroup m_mergeGroup;
        internal bool m_mergable;

        internal override void Construct()
        {
            base.Construct();
            Type = MyActorComponentEnum.GroupLeaf;

            m_parent = null;
            m_mergeGroup = null;
            m_mergable = false;
        }

        internal override void OnMatrixChange()
        {
            
        }

        internal override void OnRemove(MyActor owner)
        {
            if (m_mergeGroup != null)
            {
                m_mergeGroup.RemoveEntity(m_owner);
                m_mergeGroup = null;
            }

            if (m_parent != null)
            {
                var root = m_parent.GetGroupRoot();
                if (root != null)
                {
                    root.Remove(this);
                }
                m_parent = null;
            }

            base.OnRemove(owner);

            this.Deallocate();
        }
    }

    class MyBigMeshTable
    {
        internal static MyMeshTableSRV Table = new MyMeshTableSRV();
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

        static MyObjectsPool<MyCullProxy_2> Pool = new MyObjectsPool<MyCullProxy_2>(128);

        internal static MyCullProxy_2 Allocate()
        {
            var o = Pool.Allocate1();
            o.Construct();
            return o;
        }

        internal static void Free(MyCullProxy_2 o)
        {
            Pool.Deallocate(o);
        }
    }

    // 
    class MyMaterialMergeGroup
    {
        //string m_rootMaterial;
        int m_rootMaterialRK;

        internal MyMergeInstancing m_mergeGroup;
        internal int m_index;
        internal HashSet<MyActor> m_actors;

        internal MyMaterialMergeGroup(MyMeshTableSRV meshTable, MyMeshMaterialId matId, int index)
        {
            m_mergeGroup = new MyMergeInstancing(meshTable);
            m_rootMaterialRK = MyMeshMaterials1.Table[matId.Index].RepresentationKey;
            m_index = index;

            m_actors = new HashSet<MyActor>();
        }

        internal void AddEntity(MyActor actor, MeshId model)
        {
            m_actors.Add(actor);
            m_mergeGroup.AddEntity(actor.ID, model);
        }

        internal void RemoveEntity(MyActor actor)
        {
            m_actors.Remove(actor);
            m_mergeGroup.RemoveEntity(actor.ID);
        }

        internal void UpdateEntity(MyActor actor)
        {
            //var matrix = actor.WorldMatrix;
            //matrix.Translation = matrix.Translation - MyEnvironment.CameraPosition;
            m_mergeGroup.UpdateEntity(actor.ID, ref actor.WorldMatrix);
        }

        internal void UpdateAll()
        {
            foreach(var actor in m_actors)
            {
                UpdateEntity(actor);
            }
        }

        internal void BuildProxy(out MyRenderableProxy_2 proxy, out UInt64 key)
        {
            proxy = new MyRenderableProxy_2
            {
                MaterialType = MyMaterialType.OPAQUE,

                ObjectConstants = new MyConstantsPack { },

                ObjectSRVs = new MySrvTable { StartSlot = MyCommon.INSTANCE_INDIRECTION, SRVs = m_mergeGroup.m_SRVs, BindFlag = MyBindFlag.BIND_VS, Version = this.GetHashCode() },
                VertexData = new MyVertexDataProxy_2 { },

                DepthShaders = MyMaterialShaders.Get(X.TEXT("standard"), X.TEXT(MyGeometryRenderer.DEFAULT_DEPTH_PASS), MyVertexLayouts.Empty, MyShaderUnifiedFlags.USE_MERGE_INSTANCING | MyShaderUnifiedFlags.DEPTH_ONLY),
                Shaders = MyMaterialShaders.Get(X.TEXT("standard"), X.TEXT(MyGeometryRenderer.DEFAULT_OPAQUE_PASS), MyVertexLayouts.Empty, MyShaderUnifiedFlags.USE_MERGE_INSTANCING),
                ForwardShaders = MyMaterialShaders.Get(X.TEXT("standard"), X.TEXT(MyGeometryRenderer.DEFAULT_FORWARD_PASS), MyVertexLayouts.Empty, MyShaderUnifiedFlags.USE_MERGE_INSTANCING | MyShaderUnifiedFlags.USE_SHADOW_CASCADES),

                RenderFlags = MyRenderableProxyFlags.DepthSkipTextures,

                Submeshes = new MyDrawSubmesh_2[] { new MyDrawSubmesh_2 { DrawCommand = MyDrawCommandEnum.Draw, Count = m_mergeGroup.VerticesNum, MaterialId = MyMeshMaterials1.GetProxyId(MyMeshMaterials1.MaterialRkIndex.Get(m_rootMaterialRK, MyMeshMaterialId.NULL)) } },
                SubmeshesDepthOnly = new MyDrawSubmesh_2[] { new MyDrawSubmesh_2 { DrawCommand = MyDrawCommandEnum.Draw, Count = m_mergeGroup.VerticesNum, MaterialId = MyMeshMaterials1.GetProxyId(MyMeshMaterials1.MaterialRkIndex.Get(m_rootMaterialRK, MyMeshMaterialId.NULL)) } },

                InstanceCount = 0,
                StartInstance = 0,
            };

            key = 0;
        }

        internal void UpdateProxyVerticesNum(ref MyRenderableProxy_2 proxy)
        {
            proxy.Submeshes[0].Count = m_mergeGroup.VerticesNum;
            proxy.SubmeshesDepthOnly[0].Count = m_mergeGroup.VerticesNum;
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

        internal void OnDeviceReset()
        {
            foreach(var mg in m_materialGroups.Values)
            {
                mg.m_mergeGroup.OnDeviceReset();
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
                    var index = kv.Value.m_index;
                    kv.Value.BuildProxy(out m_proxy.Proxies[index], out m_proxy.SortingKeys[index]);
                }

                m_dirtyProxy = false;
            }
        }

        internal void PropagateMatrixChange(MyActor child)
        {
            var matrix = child.m_relativeTransform.HasValue
                        ? (MatrixD)child.m_relativeTransform.Value * m_owner.WorldMatrix
                        : m_owner.WorldMatrix;
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
            foreach (var child in m_children)
            {
                var r = child.GetRenderable();

                var model = r.GetModel();
                var material = MyMeshes.GetMeshPart(model, 0, 0).Info.Material;

                bool fracture = model.Info.RuntimeGenerated || model.Info.Dynamic;

                if (MyMeshMaterials1.IsMergable(material) && MyBigMeshTable.Table.IsMergable(model) && !fracture)
                {
                    if(child.GetGroupLeaf().m_mergeGroup != null)
                    {
                        var materialRk = MyMeshMaterials1.Table[material.Index].RepresentationKey;
                        var mergeGroupForMaterial = m_materialGroups.Get(materialRk);
                        if (mergeGroupForMaterial == null)
                        {
                            continue;
                        }

                        r.SetStandaloneRendering(true);
                        child.GetGroupLeaf().m_mergeGroup = null;

                        mergeGroupForMaterial.RemoveEntity(child);
                    }
                }
            }

            m_isMerged = false;
        }

        internal void Remove(MyGroupLeafComponent leaf)
        {
            m_mergablesCounter = leaf.m_mergable ? m_mergablesCounter - 1 : m_mergablesCounter;

            if (m_mergablesCounter < MERGE_THRESHOLD && m_isMerged)
            {
                TurnIntoSeparateRenderables();
            }

            m_children.Remove(leaf.m_owner);
        }

        internal void Merge(MyActor child)
        {
            var r = child.GetRenderable();

            var model = r.GetModel();
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

                r.SetStandaloneRendering(false);
                child.GetGroupLeaf().m_mergeGroup = mergeGroupForMaterial;

                mergeGroupForMaterial.AddEntity(child, model);
                mergeGroupForMaterial.UpdateEntity(child);
            }
        }

        internal void Add(MyActor child)
        {
            child.AddComponent(MyComponentFactory<MyGroupLeafComponent>.Create());

            child.GetGroupLeaf().m_parent = m_owner;

            m_children.Add(child);

            if (child.m_relativeTransform == null)
            {
                child.m_relativeTransform = (Matrix)( child.WorldMatrix * MatrixD.Invert(m_owner.WorldMatrix) );
            }

            if (!m_owner.m_localAabb.HasValue)
            {
                m_owner.m_localAabb = child.m_localAabb;
            }
            else
            {
                var localAabb = child.m_localAabb.Value;
                m_owner.m_localAabb = m_owner.m_localAabb.Value.Include(ref localAabb);
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
                child.GetGroupLeaf().m_mergable = true;

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
                var index = val.m_index;
                val.UpdateProxyVerticesNum(ref m_proxy.Proxies[index]);
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
                    if (child.m_visible && child.GetRenderable() != null && !child.GetRenderable().IsRendered)
                    {
                        bb.Include(child.Aabb);
                    }
                }

                m_owner.Aabb = bb;

                if (m_materialGroups.Count > 0)
                {
                    if (m_btreeProxy == -1)
                    {
                        m_btreeProxy = MyScene.GroupsDBVH.AddProxy(ref m_owner.Aabb, m_proxy, 0);
                    }
                    else
                    {
                        MyScene.GroupsDBVH.MoveProxy(m_btreeProxy, ref m_owner.Aabb, Vector3.Zero);
                    }
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
                    m_children[i].RemoveComponent(m_children[i].GetGroupLeaf());
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
