using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Import;
using VRageMath;
using VRageRender.Graphics;

namespace VRageRender.RenderObjects
{
    class MyRenderLineBasedObject : MyRenderObject
    {
        private static MyRenderMeshMaterial m_ropeMaterial = new MyRenderMeshMaterial(
            "Rope", "",
            @"Textures\Miscellaneous\rope_de.dds",
            @"Textures\Miscellaneous\rope_ns.dds", 0.0f, 0.0f, true,
            Vector3.One, Vector3.Zero);

        private Vector3 m_pointA;
        private Vector3 m_pointB;
        private Vector3D m_worldPosition;

        private readonly MyVertexFormatPositionNormalTextureTangent[] m_vertices = new MyVertexFormatPositionNormalTextureTangent[8];
        private readonly ushort[] m_indices = new ushort[36];

        private VertexBuffer m_vertexBuffer;
        private IndexBuffer m_indexBuffer;


        public MyRenderLineBasedObject(uint id, string debugName):
            base(id, debugName)
        {
            int indexCounter = 0;
            int vertexCounter = 0;
            AddTriangle(ref indexCounter, vertexCounter + 0, vertexCounter + 1, vertexCounter + 2);
            AddTriangle(ref indexCounter, vertexCounter + 0, vertexCounter + 2, vertexCounter + 3);

            for (; vertexCounter < (m_vertices.Length - 4); vertexCounter += 4)
            {
                AddTriangle(ref indexCounter, vertexCounter + 0, vertexCounter + 4, vertexCounter + 5);
                AddTriangle(ref indexCounter, vertexCounter + 0, vertexCounter + 5, vertexCounter + 1);
                AddTriangle(ref indexCounter, vertexCounter + 1, vertexCounter + 5, vertexCounter + 6);
                AddTriangle(ref indexCounter, vertexCounter + 1, vertexCounter + 6, vertexCounter + 2);
                AddTriangle(ref indexCounter, vertexCounter + 2, vertexCounter + 6, vertexCounter + 7);
                AddTriangle(ref indexCounter, vertexCounter + 2, vertexCounter + 7, vertexCounter + 3);
                AddTriangle(ref indexCounter, vertexCounter + 3, vertexCounter + 7, vertexCounter + 4);
                AddTriangle(ref indexCounter, vertexCounter + 3, vertexCounter + 4, vertexCounter + 0);
            }

            AddTriangle(ref indexCounter, vertexCounter + 2, vertexCounter + 1, vertexCounter + 0);
            AddTriangle(ref indexCounter, vertexCounter + 3, vertexCounter + 2, vertexCounter + 0);
        }

        internal void SetWorldPoints(ref Vector3D worldPointA, ref Vector3D worldPointB)
        {
            m_worldPosition = (worldPointA + worldPointB) * 0.5f;
            m_pointA = (Vector3)(worldPointA - m_worldPosition);
            m_pointB = (Vector3)(worldPointB - m_worldPosition);
            SetDirty();

            var length = (m_pointA - m_pointB).Length() * 10.0f;

            Vector3 tangent, normal, binormal;
            tangent = m_pointB - m_pointA;
            Vector3.Normalize(ref tangent, out tangent);
            tangent.CalculatePerpendicularVector(out normal);
            Vector3.Cross(ref tangent, ref normal, out binormal);

            normal *= 0.025f;
            binormal *= 0.025f;

            unsafe
            {
                Vector3* points = stackalloc Vector3[2];
                points[0] = m_pointA;
                points[1] = m_pointB;
                int vertexCounter = 0;
                for (int i = 0; i < 2; ++i)
                {
                    int baseVertex = i * 4;
                    float texCoordX = (i - 0.5f) * length;
                    AddVertex(ref vertexCounter, new MyVertexFormatPositionNormalTextureTangent()
                    {
                        Position = points[i] + normal,
                        Normal = normal,
                        Tangent = tangent,
                        TexCoord = new Vector2(texCoordX, 0.0f),
                    });
                    AddVertex(ref vertexCounter, new MyVertexFormatPositionNormalTextureTangent()
                    {
                        Position = points[i] + binormal,
                        Normal = binormal,
                        Tangent = tangent,
                        TexCoord = new Vector2(texCoordX, 0.33333f),
                    });
                    AddVertex(ref vertexCounter, new MyVertexFormatPositionNormalTextureTangent()
                    {
                        Position = points[i] - normal,
                        Normal = -normal,
                        Tangent = tangent,
                        TexCoord = new Vector2(texCoordX, 0.66667f),
                    });
                    AddVertex(ref vertexCounter, new MyVertexFormatPositionNormalTextureTangent()
                    {
                        Position = points[i] - binormal,
                        Normal = -binormal,
                        Tangent = tangent,
                        TexCoord = new Vector2(texCoordX, 1.0f),
                    });
                }
            }

            if (m_vertexBuffer != null)
                m_vertexBuffer.SetData(m_vertices, LockFlags.Discard);
        }

        private void AddVertex(ref int vertexCounter, MyVertexFormatPositionNormalTextureTangent vertexData)
        {
            m_vertices[vertexCounter++] = vertexData;
        }

        private void AddTriangle(ref int indexCounter, int p1, int p2, int p3)
        {
            m_indices[indexCounter++] = (ushort)p1;
            m_indices[indexCounter++] = (ushort)p2;
            m_indices[indexCounter++] = (ushort)p3;
        }

        public override void LoadContent()
        {
            int vbSize;
            unsafe { vbSize = sizeof(MyVertexFormatPositionNormalTextureTangent) * m_vertices.Length; }
            int ibSize = sizeof(ushort) * m_indices.Length;

            m_vertexBuffer = new VertexBuffer(MyRender.Device, vbSize, Usage.Dynamic | Usage.WriteOnly, VertexFormat.None, Pool.Default);
            m_vertexBuffer.Tag = this;
            m_vertexBuffer.SetData(m_vertices, LockFlags.Discard);

            m_indexBuffer = new IndexBuffer(MyRender.Device, ibSize, Usage.Dynamic | Usage.WriteOnly, Pool.Default, true);
            m_indexBuffer.Tag = this;
            m_indexBuffer.SetData(m_indices, LockFlags.Discard);

            m_ropeMaterial.PreloadTexture();

            base.LoadContent();
        }

        public override void UnloadContent()
        {
            m_vertexBuffer.Dispose();
            m_indexBuffer.Dispose();

            m_vertexBuffer = null;
            m_indexBuffer = null;

            base.UnloadContent();
        }

        public override void UpdateWorldAABB()
        {
            m_aabb = BoundingBoxD.CreateInvalid();
            var pointA = (Vector3D)m_pointA;
            var pointB = (Vector3D)m_pointB;
            m_aabb.Include(ref pointA);
            m_aabb.Include(ref pointB);
            m_aabb.Inflate(0.25);
            m_aabb.Translate(m_worldPosition);
            base.UpdateWorldAABB();
        }

        public override void DebugDraw()
        {
            var fromColor = Color.White;
            var toColor = Color.Tomato;
            var worldPointA = m_worldPosition + m_pointA;
            var worldPointB = m_worldPosition + m_pointB;
            MyDebugDraw.DrawLine3D(ref worldPointA, ref worldPointB, ref fromColor, ref toColor, true);
            base.DebugDraw();
        }

        public override void GetRenderElements(MyLodTypeEnum lodTypeEnum, List<MyRender.MyRenderElement> elements, List<MyRender.MyRenderElement> transparentElements)
        {
            SetupRenderElement(lodTypeEnum, elements);
            base.GetRenderElements(lodTypeEnum, elements, transparentElements);
        }

        public override void GetRenderElementsForShadowmap(MyLodTypeEnum lodTypeEnum, List<MyRender.MyRenderElement> renderElements, List<MyRender.MyRenderElement> transparentRenderElements)
        {
            SetupRenderElement(lodTypeEnum, renderElements);
            base.GetRenderElementsForShadowmap(lodTypeEnum, renderElements, transparentRenderElements);
        }

        private void SetupRenderElement(MyLodTypeEnum lodTypeEnum, List<MyRender.MyRenderElement> elements)
        {
            if (lodTypeEnum != MyLodTypeEnum.LOD0)
                return;

            MyRender.MyRenderElement renderElement;
            MyRender.AllocateRenderElement(out renderElement);
            if (MyRender.IsRenderOverloaded)
                return;

            renderElement.VertexBuffer = m_vertexBuffer;
            renderElement.VertexCount = m_vertices.Length;
            renderElement.VertexStride = MyVertexFormatPositionNormalTextureTangent.Stride;
            renderElement.VertexDeclaration = MyVertexFormatPositionNormalTextureTangent.VertexDeclaration;

            renderElement.IndexBuffer = m_indexBuffer;
            renderElement.IndexStart = 0;
            renderElement.TriCount = m_indices.Length / 3;

            renderElement.InstanceBuffer = null;
            MatrixD.CreateTranslation(ref m_worldPosition, out renderElement.WorldMatrix);
            MatrixD.Multiply(ref renderElement.WorldMatrix, ref MyRenderCamera.InversePositionTranslationMatrix, out renderElement.WorldMatrixForDraw);
            renderElement.DrawTechnique = MyMeshDrawTechnique.MESH;
            renderElement.Material = m_ropeMaterial;
            renderElement.RenderObject = this;
            renderElement.Color = new Vector3(1f, 0.93f, 0.42f);

            elements.Add(renderElement);
        }

    }
}
