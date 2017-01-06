using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using VRage.Collections;
using VRage.Render11.Resources;
using VRage.Utils;
using VRageMath;
using Vector3 = VRageMath.Vector3;


namespace VRageRender
{
    internal static class MyFoliageComponents
    {
        static HashSet<MyFoliageComponent> m_pendingComponentsToInit;
        static HashSet<MyFoliageComponent> m_activeComponents;
        static List<MyFoliageComponent> m_componentsInRadius  = new List<MyFoliageComponent>();

        internal static HashSetReader<MyFoliageComponent> ActiveComponents
        {
            get
            {
                return m_activeComponents;
            }
        }

        internal static void Init()
        {
            m_pendingComponentsToInit = new HashSet<MyFoliageComponent>();
            m_activeComponents = new HashSet<MyFoliageComponent>();
        }

        internal static void Register(MyFoliageComponent component)
        {
            component.m_btreeProxy = -1;
            m_pendingComponentsToInit.Add(component);
        }

        internal static void Deregister(MyFoliageComponent component)
        {   
            m_pendingComponentsToInit.Remove(component);
            if (component.m_btreeProxy != -1)
            {
                MyScene.FoliageDBVH.RemoveProxy(component.m_btreeProxy);
            }
        }

        internal static void Update()
        {
            foreach (var component in m_pendingComponentsToInit)
            {
                if (component.ShouldHaveFoliage())
                {
                    var aabb = component.Owner.Aabb;
                    component.m_btreeProxy = MyScene.FoliageDBVH.AddProxy(ref aabb, component, 0);
                }
            }
            m_pendingComponentsToInit.Clear();

            BoundingSphereD sphere = new BoundingSphereD(MyRender11.Environment.Matrices.CameraPosition, MyRender11.Settings.User.FoliageDetails.GrassDrawDistance());
            MyScene.FoliageDBVH.OverlapAllBoundingSphere(ref sphere, m_componentsInRadius);

            foreach (var foliageComponent in m_activeComponents)
            {
                foliageComponent.Updated = false;
            }

            foreach (var foliageComponent in m_componentsInRadius)
            {
                var renderable = foliageComponent.Owner.GetRenderable();
                if (renderable.Lods[0].RenderableProxies.Length == 0)
                    continue;

                foliageComponent.FillStreams();
                
                if (!m_activeComponents.Contains(foliageComponent))
                {
                    m_activeComponents.Add(foliageComponent);
                }
                foliageComponent.Updated = true;
            }

            foreach (var component in m_activeComponents)
            {
                if (!component.Updated)
                {
                    component.InvalidateStreams();
                }
            }
            m_activeComponents.RemoveWhere(c => c.Updated == false);
        }

        internal static void OnSessionEnd()
        {
            m_pendingComponentsToInit.Clear();
            m_activeComponents.Clear();
        }
    }

    class MyFoliageComponent : MyActorComponent
    {
        const float AllocationFactor = 3f;

        /// <summary>
        /// The farthest lod that can have foliage
        /// </summary>
        internal const int LodLimit = 4;

        Dictionary<int, MyFoliageStream> m_streams;
        internal int m_btreeProxy;
        internal bool Updated;

        private bool m_pendingRefresh;

        internal override void Construct()
        {
            base.Construct();
            Type = MyActorComponentEnum.Foliage;
          	m_streams = new Dictionary<int,MyFoliageStream>();
            MyFoliageComponents.Register(this);
        }

        internal void Dispose()
        {
            if (m_streams != null)
            {
                foreach (var stream in m_streams.Values)
                {
                    stream.Dispose();
                }
                m_streams = null;
            }
        }

        internal override void OnVisibilityChange()
        {
            base.OnVisibilityChange();

            if (!Owner.IsVisible)
                Dispose();
        }

        internal override void Destruct()
        {
            Dispose();

            MyFoliageComponents.Deregister(this);
            base.Destruct();
        }

        internal override void OnRemove(MyActor owner)
        {
            base.OnRemove(owner);

            this.Deallocate();
        }

        void PrepareStream(int materialId, int triangles, int voxelLod)
        {
            float densityFactor = MyRender11.Settings.User.GrassDensityFactor * 
                (float)MathHelper.Lerp(2 * AllocationFactor, 1.0, MyRender11.Settings.User.GrassDensityFactor / 10.0);
            if (densityFactor < 0.1f)
            {
                densityFactor = 0.1f;
            }

            int predictedAllocation = (int)(triangles * MyVoxelMaterials1.Table[materialId].FoliageDensity * densityFactor);

            var firstOrDefault = m_streams.GetValueOrDefault(materialId);
            if (firstOrDefault == null)
                m_streams.SetDefault(materialId, new MyFoliageStream()).Reserve(predictedAllocation);
            else
                firstOrDefault.Reserve(predictedAllocation);
        }

        public void RefreshStreams()
        {
            if (m_streams == null)
                return;

            m_pendingRefresh = true;
            foreach (var stream in m_streams.Values)
            {
                stream.Reset();
            }
        }

        internal void InvalidateStreams()
        {
            Dispose();
        }

        internal bool ShouldHaveFoliage()
        {
            var mesh = Owner.GetRenderable().GetModel();
            int voxelLod = MyMeshes.GetVoxelInfo(mesh).Lod;
            bool voxelMeshNotReady = voxelLod > LodLimit;
            if (voxelMeshNotReady)
                return false;

            int partsNum = MyMeshes.GetLodMesh(mesh, 0).Info.PartsNum;

            // only stream stones for lod0
            if (voxelLod > 0)
            {
                bool allStone = true;
                for (int i = 0; i < partsNum; i++)
                {
                    var triple = MyMeshes.GetVoxelPart(mesh, i).Info.MaterialTriple;
                    if (triple.I0 != -1 && MyVoxelMaterials1.Table[triple.I0].HasFoliage && MyVoxelMaterials1.Table[triple.I0].FoliageType == 0)
                    {
                        allStone = false;
                        break;
                    }
                    if (triple.I1 != -1 && MyVoxelMaterials1.Table[triple.I1].HasFoliage && MyVoxelMaterials1.Table[triple.I1].FoliageType == 0)
                    {
                        allStone = false;
                        break;
                    }
                    if (triple.I2 != -1 && MyVoxelMaterials1.Table[triple.I2].HasFoliage && MyVoxelMaterials1.Table[triple.I2].FoliageType == 0)
                    {
                        allStone = false;
                        break;
                    }
                }
                if (allStone)
                {
                    return false;
                }
            }

            return true;
        }

        internal unsafe void FillStreams()
        {
            bool alreadyFilled = m_streams != null && m_streams.Count > 0 && !m_pendingRefresh;
            if (alreadyFilled)
                return;

            m_pendingRefresh = false;

            var mesh = Owner.GetRenderable().GetModel();

            int voxelLod = MyMeshes.GetVoxelInfo(mesh).Lod;

            if (!Owner.IsVisible)
                return;

            int partsNum = MyMeshes.GetLodMesh(mesh, 0).Info.PartsNum;

            if(m_streams == null)
                m_streams = new Dictionary<int, MyFoliageStream>();

            // analyze 
            for (int partIndex = 0; partIndex < partsNum; ++partIndex )
            {
                var partInfo = MyMeshes.GetVoxelPart(mesh, partIndex).Info;
                var triple = partInfo.MaterialTriple;

                if (triple.I0 != -1 && MyVoxelMaterials1.Table[triple.I0].HasFoliage)
                {
                    PrepareStream(triple.I0, partInfo.IndexCount / 3, voxelLod);
                }
                if (triple.I1 != -1 && MyVoxelMaterials1.Table[triple.I1].HasFoliage)
                {
                    PrepareStream(triple.I1, partInfo.IndexCount / 3, voxelLod);
                }
                if (triple.I2 != -1 && MyVoxelMaterials1.Table[triple.I2].HasFoliage)
                {
                    PrepareStream(triple.I2, partInfo.IndexCount / 3, voxelLod);
                }
            }

            // prepare
            foreach (var stream in m_streams.Values)
            {
                stream.AllocateStreamOutBuffer(sizeof(Vector3) + sizeof(uint));
            }

            // analyze 
            for (int partIndex = 0; partIndex < partsNum; partIndex++)
            {
                var partInfo = MyMeshes.GetVoxelPart(mesh, partIndex).Info;
                var triple = partInfo.MaterialTriple;

                if (triple.I0 != -1 && MyVoxelMaterials1.Table[triple.I0].HasFoliage)
                {
                    FillStreamWithTerrainBatch(triple.I0, 0,
                        partInfo.IndexCount, partInfo.StartIndex, 0);
                }
                if (triple.I1 != -1 && MyVoxelMaterials1.Table[triple.I1].HasFoliage)
                {
                    FillStreamWithTerrainBatch(triple.I1, 1,
                        partInfo.IndexCount, partInfo.StartIndex, 0);
                }
                if (triple.I2 != -1 && MyVoxelMaterials1.Table[triple.I2].HasFoliage)
                {
                    FillStreamWithTerrainBatch(triple.I2, 2,
                        partInfo.IndexCount, partInfo.StartIndex, 0);
                }
            }
        }

        void FillStreamWithTerrainBatch(int materialId,
            int vertexMaterialIndex, int indexCount, int startIndex, int baseVertex)
        {
            // all necessary data should be same - geometry and input layout
            var renderable = Owner.GetRenderable();
            var proxy = renderable.Lods[0].RenderableProxies[0];
            
            // get shader for streaming
            MyFileTextureEnum textureTypes = proxy.Material == MyMeshMaterialId.NULL ? MyFileTextureEnum.UNSPECIFIED : proxy.Material.Info.TextureTypes;
            var bundle = MyMaterialShaders.Get(MyMaterialShaders.TRIPLANAR_MULTI_MATERIAL_TAG,
                MyMaterialShaders.FOLIAGE_STREAMING_PASS_ID, MyMeshes.VoxelLayout,
                renderable.Lods[0].VertexShaderFlags &~ MyShaderUnifiedFlags.USE_VOXEL_MORPHING, textureTypes);

            MyRender11.FoliageGenerator.RecordCommands(proxy, m_streams[materialId], materialId,
                bundle.VS, bundle.IL,
                vertexMaterialIndex, indexCount, startIndex, baseVertex);
        }

        internal void Render(MyFoliageRenderingPass foliageRenderer)
        {
            if (m_streams == null || m_streams.Count == 0) 
                return;

            var renderableComponent = Owner.GetRenderable() as MyVoxelRenderableComponent;
            var proxy = renderableComponent.Lods[0].RenderableProxies[0];

            var invScaleMat = MatrixD.CreateScale(1.0f / renderableComponent.m_voxelScale);

            var worldMat = proxy.WorldMatrix;
            worldMat.Translation -= MyRender11.Environment.Matrices.CameraPosition;
            proxy.CommonObjectData.LocalMatrix = invScaleMat * worldMat;

            foreach(var materialStreamPair in m_streams)
            {
                foliageRenderer.RecordCommands(proxy, materialStreamPair.Value.m_stream, materialStreamPair.Key);
            }
        }
    }
}
