using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRageRender.Textures;
using System.Diagnostics;
using SharpDX.Direct3D9;
using VRageMath.PackedVector;
using VRage.Import;

namespace VRageRender
{
    class MyRenderBatch : MyRenderTransformObject
    {
        struct MyRenderMeshInfo
        {
            public MyRenderModel Model;
            public MyRenderMesh Mesh;
            public int VertexOffset;
        }

        struct MyRenderGroup
        {
            public MyRenderMeshMaterial Material;
            public int IndexStart;
            public int TriangleCount;
        }

        List<MyRenderBatchPart> m_batchParts = new List<MyRenderBatchPart>();
        List<MyRenderGroup> m_meshes = new List<MyRenderGroup>();
        Dictionary<MyRenderMeshMaterial, List<MyRenderMeshInfo>> m_materialGroups = new Dictionary<MyRenderMeshMaterial, List<MyRenderMeshInfo>>();
        VertexDeclaration m_vertexDeclaration;
        VertexBuffer m_vertexBuffer;
        IndexBuffer m_indexBuffer;
        int m_vertexCount;
        int m_vertexStride;

        public MyRenderBatch(uint id, string debugName, MatrixD worldMatrix, RenderFlags renderFlags, List<MyRenderBatchPart> batchParts)
            : base(id, debugName, worldMatrix, renderFlags)
        {
            m_localAABB = BoundingBoxD.CreateInvalid();
            m_batchParts.Clear();
            m_batchParts.AddList(batchParts);
        }

        public override void LoadContent()
        {
            UpdateBatch(m_batchParts);
        }

        public override void UnloadContent()
        {
            if (m_indexBuffer != null)
            {
                m_indexBuffer.Dispose();
                m_indexBuffer = null;
            }
            if (m_vertexBuffer != null)
            {
                m_vertexBuffer.Dispose();
                m_vertexBuffer = null;
            }
        }

        void UpdateBatch(List<MyRenderBatchPart> batchParts)
        {
            // Clear all lists
            foreach (var pair in m_materialGroups)
            {
                pair.Value.Clear();
            }

            int vbSize = 0;
            int triangleCount = 0;
            m_vertexCount = 0;
            m_vertexDeclaration = null;

            if (batchParts.Count == 0)
                return;

            // Add meshes to lists
            for (int i = 0; i < batchParts.Count; i++)
            {
                var part = batchParts[i];
                var model = MyRenderModels.GetModel(part.Model);
                model.LoadData();

                if (model.LoadState == LoadState.Unloaded)
                {
                    model.LoadInDraw(LoadingMode.Immediate);
                }
                if (model.LoadState == LoadState.Loading)
                    Debug.Fail("Batch model is loading, this should not happen");

                if (m_vertexDeclaration == null)
                {
                    m_vertexStride = model.GetVertexStride();
                    m_vertexDeclaration = model.GetVertexDeclaration();
                }
                else if (m_vertexDeclaration != model.GetVertexDeclaration())
                {
                    Debug.Fail("Models in batch must have same declaration");
                    continue;
                }

                foreach (var mesh in model.GetMeshList())
                {
                    List<MyRenderMeshInfo> meshList = new List<MyRenderMeshInfo>();
                    if (!m_materialGroups.TryGetValue(mesh.Material, out meshList))
                    {
                        meshList = new List<MyRenderMeshInfo>();
                        m_materialGroups.Add(mesh.Material, meshList);
                    }
                    meshList.Add(new MyRenderMeshInfo() { Model = model, Mesh = mesh, VertexOffset = m_vertexCount });
                }

                vbSize += model.GetVBSize;
                m_vertexCount += model.GetVerticesCount();
                triangleCount += model.GetTrianglesCount();
            }

            CreateVertexBuffer(batchParts, vbSize);
            CreateIndexBuffer(triangleCount);
        }

        private void CreateIndexBuffer(int triangleCount)
        {
            int indexSize = sizeof(int);
            if (m_indexBuffer == null || m_indexBuffer.Description.Size < triangleCount * 3 * indexSize)
            {
                if (m_indexBuffer != null)
                    m_indexBuffer.Dispose();

                m_indexBuffer = new IndexBuffer(MyRender.GraphicsDevice, triangleCount * 3 * indexSize, Usage.WriteOnly, Pool.Default, false);
            }

            int indexOffset = 0;
            m_meshes.Clear();
            foreach (var pair in m_materialGroups)
            {
                var mesh = new MyRenderGroup() { IndexStart = indexOffset, Material = pair.Key };

                foreach (var meshInfo in pair.Value)
                {
                    // Copy indices
                    var compoundIb = m_indexBuffer.Lock(indexOffset * indexSize, meshInfo.Mesh.TriCount * 3 * indexSize, LockFlags.None);

                    var format = meshInfo.Model.IndexBuffer.Description.Format;
                    Debug.Assert(format == Format.Index16 || format == Format.Index32);
                    int modelIndexSize = format == Format.Index16 ? sizeof(short) : sizeof(int);
                    var modelIb = meshInfo.Model.IndexBuffer.Lock(meshInfo.Mesh.IndexStart * modelIndexSize, meshInfo.Mesh.TriCount * 3 * modelIndexSize, LockFlags.ReadOnly);

                    while (modelIb.RemainingLength > 0)
                    {
                        if (format == Format.Index16)
                        {
                            int index = modelIb.Read<short>() + meshInfo.VertexOffset;
                            compoundIb.Write(index);
                        }
                        else
                        {
                            int index = modelIb.Read<int>() + meshInfo.VertexOffset;
                            compoundIb.Write(index);
                        }
                    }
                    m_indexBuffer.Unlock();
                    meshInfo.Model.IndexBuffer.Unlock();

                    mesh.TriangleCount += meshInfo.Mesh.TriCount;
                    indexOffset += meshInfo.Mesh.TriCount * 3;
                }

                m_meshes.Add(mesh);
            }
        }

        private void CreateVertexBuffer(List<MyRenderBatchPart> batchParts, int vbSize)
        {
            if (m_vertexBuffer == null || m_vertexBuffer.Description.SizeInBytes < vbSize)
            {
                if (m_vertexBuffer != null)
                    m_vertexBuffer.Dispose();

                m_vertexBuffer = new VertexBuffer(MyRender.GraphicsDevice, vbSize, Usage.WriteOnly, VertexFormat.None, Pool.Default);
            }

            // Transform and copy vertices
            int vbOffset = 0;
            for (int i = 0; i < batchParts.Count; i++)
            {
                var part = batchParts[i];
                var model = MyRenderModels.GetModel(part.Model);
                var matrix = part.ModelMatrix;

                var modelVb = model.VertexBuffer.Lock(0, model.GetVBSize, LockFlags.ReadOnly);

                var compoundVb = m_vertexBuffer.Lock(vbOffset, model.GetVBSize, LockFlags.None);

                for (int v = 0; v < model.GetVerticesCount(); v++)
                {
                    var vertexPos = v * model.GetVertexStride();
                    modelVb.Seek(vertexPos, System.IO.SeekOrigin.Begin);

                    var position = modelVb.Read<HalfVector4>();
                    Vector3D pos = (Vector3D)VF_Packer.UnpackPosition(ref position);
                    var transformedPos = Vector3D.Transform(pos, matrix);
                    m_localAABB = m_localAABB.Include(ref transformedPos);
                    var transformedPos2 = (Vector3)transformedPos;
                    compoundVb.Write<HalfVector4>(VF_Packer.PackPosition(ref transformedPos2)); // Transform and copy position
                    compoundVb.WriteRange(modelVb.PositionPointer, model.GetVertexStride() - (modelVb.Position - vertexPos)); // Copy rest
                }

                m_vertexBuffer.Unlock();
                model.VertexBuffer.Unlock();
                vbOffset += model.GetVBSize;
            }

            BoundingSphereD.CreateFromBoundingBox(ref m_localAABB, out m_localVolume);
            m_localVolumeOffset = m_localVolume.Center;
            SetDirty();
        }

        private void CreateCopyMeshes(MyRenderModel m)
        {
            m_meshes = m.GetMeshList().Select(s => new MyRenderGroup() { IndexStart = s.IndexStart, TriangleCount = s.TriCount, Material = s.Material }).ToList();
        }

        public override void GetRenderElements(MyLodTypeEnum lodTypeEnum, List<MyRender.MyRenderElement> elements, List<MyRender.MyRenderElement> transparentElements)
        {
            foreach (var mesh in m_meshes)
            {
                mesh.Material.PreloadTexture(LoadingMode.Background);

                MyRender.MyRenderElement renderElement;
                MyRender.AllocateRenderElement(out renderElement);

                if (!MyRender.IsRenderOverloaded)
                {
                    //renderElement.DebugName = entity.Name;
                    renderElement.RenderObject = this;

                    renderElement.VertexBuffer = m_vertexBuffer;
                    renderElement.IndexBuffer = m_indexBuffer;
                    renderElement.VertexCount = m_vertexCount;
                    renderElement.VertexDeclaration = m_vertexDeclaration;
                    renderElement.VertexStride = m_vertexStride;
                    renderElement.InstanceBuffer = null;

                    renderElement.IndexStart = mesh.IndexStart;
                    renderElement.TriCount = mesh.TriangleCount;

                    renderElement.WorldMatrixForDraw = GetWorldMatrixForDraw();
                    renderElement.WorldMatrix = WorldMatrix;

                    renderElement.Material = mesh.Material;
                    renderElement.DrawTechnique = mesh.Material.DrawTechnique;
                    renderElement.Color = new Vector3(1, 1, 1);
                    renderElement.Dithering = 0;

                    Debug.Assert(renderElement.VertexBuffer != null, "Vertex buffer cannot be null!");
                    Debug.Assert(renderElement.IndexBuffer != null, "Index buffer cannot be null!");

                    elements.Add(renderElement);
                }
            }
        }

        public override void DebugDraw()
        {
            base.DebugDraw();
        }
    }
}
