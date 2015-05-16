using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRage.Import;
using SharpDX.Direct3D9;

namespace VRageRender
{
    partial class MySortedElements
    {
        public static readonly int LodCount = MyUtils.GetMaxValueFromEnum<MyLodTypeEnum>() + 1;
        public static readonly int DrawTechniqueCount = MyUtils.GetMaxValueFromEnum<MyMeshDrawTechnique>() + 1;
        public static readonly int VoxelTechniqueCount = MyUtils.GetMaxValueFromEnum<MyRenderVoxelBatchType>() + 1;

        static VertexBufferComparer m_vertexBufferComparer = new VertexBufferComparer();
        static MeshMaterialComparer m_meshMaterialComparer = new MeshMaterialComparer();

        // For further improvement...use global material index instead of MyMeshMaterial key (dictionary -> array)
        // Are vertex buffers necessary?...for what...multiple MeshParts? (probably can be replaced by mesh/mesh part id)
        public ModelSet[] Models = new ModelSet[LodCount * DrawTechniqueCount];

        // For further improvement (removing dictionary), there can be array of voxel materials instead. It's 38^3 combinations (about 50K) which equals array size of 200 KB
        public VoxelSet[] Voxels = new VoxelSet[LodCount * VoxelTechniqueCount];

        public MySortedElements()
        {
            CreateRootDictionaries();
        }

        private void CreateRootDictionaries()
        {
            for (int i = 0; i < Models.Length; i++)
            {
                Models[i] = new ModelSet();
            }

            for (int i = 0; i < Voxels.Length; i++)
            {
                Voxels[i] = new VoxelSet();
            }
        }

        internal void Add(MyLodTypeEnum lod, List<MyRender.MyRenderElement> m_renderElements)
        {
            foreach (var el in m_renderElements)
            {
                Add(lod, el);
            }
        }

        public void Add(MyLodTypeEnum lod, MyRender.MyRenderElement renderElement)
        {
            if (renderElement.DrawTechnique == MyMeshDrawTechnique.VOXEL_MAP)
                AddVoxel(lod, renderElement);
            else
                AddModel(lod, renderElement);
        }

        public void AddModel(MyLodTypeEnum lod, MyRender.MyRenderElement renderElement)
        {
            var matDict = Models[GetModelIndex(lod, renderElement.DrawTechnique)];
            ModelMaterialSet vbDict;
            if (!matDict.Models.TryGetValue(renderElement.Material, out vbDict))
            {
                vbDict = new ModelMaterialSet();
                matDict.Models[renderElement.Material] = vbDict;
            }

            List<MyRender.MyRenderElement> elements;
            if (!vbDict.Models.TryGetValue(renderElement.VertexBuffer, out elements))
            {
                elements = new List<MyRender.MyRenderElement>(20);
                vbDict.Models[renderElement.VertexBuffer] = elements;
            }

            matDict.RenderElementCount++;
            vbDict.RenderElementCount++;

            elements.Add(renderElement);
        }

        public void AddVoxel(MyLodTypeEnum lod, MyRender.MyRenderElement renderElement)
        {
            var matDict = Voxels[GetVoxelIndex(lod, renderElement.VoxelBatch.Type)];
            List<MyRender.MyRenderElement> elements;
            if (!matDict.Voxels.TryGetValue(renderElement.VoxelBatch.MaterialId, out elements))
            {
                elements = new List<MyRender.MyRenderElement>(20);
                matDict.Voxels[renderElement.VoxelBatch.MaterialId] = elements;
            }
            matDict.RenderElementCount++;
            elements.Add(renderElement);
        }

        public void Clear()
        {
            foreach (var set in Models)
            {
                set.RenderElementCount = 0;
                foreach (var dict in set.Models)
                {
                    dict.Value.RenderElementCount = 0;
                    foreach (var list in dict.Value.Models)
                    {
                        list.Value.Clear();
                    }
                }
            }

            foreach (var set in Voxels)
            {
                set.RenderElementCount = 0;
                foreach (var list in set.Voxels)
                {
                    list.Value.Clear();
                }
            }
        }

        public void ClearAndCompact()
        {
            CreateRootDictionaries();
        }

        public int GetModelIndex(MyLodTypeEnum lod, MyMeshDrawTechnique technique)
        {
            return ((byte)lod * DrawTechniqueCount) + (int)technique;
        }

        public int GetVoxelIndex(MyLodTypeEnum lod, MyRenderVoxelBatchType technique)
        {
            return ((byte)lod * VoxelTechniqueCount) + (int)technique;
        }
    }
}
