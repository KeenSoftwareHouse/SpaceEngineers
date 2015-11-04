using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using VRageMath;
using VRageRender.Resources;
using VRageRender.Vertex;
using Vector3 = VRageMath.Vector3;
using Color = VRageMath.Color;
using Matrix = VRageMath.Matrix;
using BoundingFrustum = VRageMath.BoundingFrustum;
using BoundingBox = VRageMath.BoundingBox;
using System.Diagnostics;

namespace VRageRender
{

    class MyPrimitivesRenderer : MyImmediateRC
    {
        static int m_currentBufferSize;
        static VertexBufferId m_VB;
        internal static List<MyVertexFormatPositionColor> m_vertexList = new List<MyVertexFormatPositionColor>();
        internal static List<MyVertexFormatPositionColor> m_postSortVertexList = new List<MyVertexFormatPositionColor>();

        static VertexShaderId m_vs;
        static PixelShaderId m_ps;
        static InputLayoutId m_inputLayout;

        //internal static void CreateInputLayout(byte[] bytecode)
        //{
        //    m_inputLayout = MyVertexInputLayout.CreateLayout(MyVertexInputLayout.Empty().Append(MyVertexInputComponentType.POSITION3).Append(MyVertexInputComponentType.COLOR4), bytecode);
        //}

        internal unsafe static void Init()
        {
            m_currentBufferSize = 100000;

            m_VB = MyHwBuffers.CreateVertexBuffer(m_currentBufferSize, sizeof(MyVertexFormatPositionColor), BindFlags.VertexBuffer, ResourceUsage.Dynamic);

            m_vs = MyShaders.CreateVs("primitive.hlsl", "vs");
            m_ps = MyShaders.CreatePs("primitive.hlsl", "ps");
            m_inputLayout = MyShaders.CreateIL(m_vs.BytecodeId, MyVertexLayouts.GetLayout(MyVertexInputComponentType.POSITION3, MyVertexInputComponentType.COLOR4));
        }

        static unsafe void CheckBufferSize(int requiredSize)
        {
            if(m_currentBufferSize < requiredSize)
            {
                m_currentBufferSize = (int)(requiredSize * 1.33f);

                MyHwBuffers.ResizeVertexBuffer(m_VB, m_currentBufferSize);
            }
        }

        static List<float> m_triangleSortDistance = new List<float>();
        static List<int> m_sortedIndices = new List<int>();
        static void SortTransparent()
        {
            int trianglesNum = m_vertexList.Count / 3;
            for(int i=0; i< trianglesNum; i++)
            {
                m_sortedIndices.Add(i);
            }

            m_sortedIndices.Sort((x, y) => m_triangleSortDistance[x].CompareTo(m_triangleSortDistance[y]));

            for(int i=0; i< trianglesNum; i++)
            {
                m_postSortVertexList.Add(m_vertexList[m_sortedIndices[i] * 3]);
                m_postSortVertexList.Add(m_vertexList[m_sortedIndices[i] * 3 + 1]);
                m_postSortVertexList.Add(m_vertexList[m_sortedIndices[i] * 3 + 2]);
            }
        }

        internal static void DrawTriangle(Vector3 v0, Vector3 v1, Vector3 v2, Color color)
        {
            var distance = ((v0 + v1 + v2) / 3f - (Vector3)MyEnvironment.CameraPosition).LengthSquared();
            m_triangleSortDistance.Add(distance);

            m_vertexList.Add(new MyVertexFormatPositionColor(v0, color));
            m_vertexList.Add(new MyVertexFormatPositionColor(v1, color));
            m_vertexList.Add(new MyVertexFormatPositionColor(v2, color));
        }

        internal static void DrawQuad(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Color color)
        {
            DrawTriangle(v0, v1, v2, color);
            DrawTriangle(v0, v2, v3, color);
        }

        internal static void Draw6FacedConvex(Vector3[] vertices, Color color, float alpha)
        {
            Color c = color;
            c.A = (byte)(alpha * 255);

            Debug.Assert(vertices.Length == 8);

            DrawQuad(vertices[0], vertices[1], vertices[2], vertices[3], c);
            DrawQuad(vertices[4], vertices[5], vertices[6], vertices[7], c);

            // top left bottom right
            DrawQuad(vertices[0], vertices[1], vertices[5], vertices[4], c);
            DrawQuad(vertices[0], vertices[3], vertices[7], vertices[4], c);
            DrawQuad(vertices[3], vertices[2], vertices[6], vertices[7], c);
            DrawQuad(vertices[2], vertices[1], vertices[5], vertices[6], c);
        }

        internal static void DrawFrustum(BoundingFrustum frustum, Color color, float alpha)
        {
            var corners = frustum.GetCorners();

            Draw6FacedConvex(corners, color, alpha);
        }

        internal static void Draw(MyBindableResource depth)
        {
            RC.SetupScreenViewport();
            RC.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            RC.SetIL(m_inputLayout);

            RC.SetRS(MyRender11.m_nocullRasterizerState);
            RC.SetDS(MyDepthStencilState.DefaultDepthState);

            RC.SetVS(m_vs);
            RC.SetPS(m_ps);

            RC.BindDepthRT(depth, DepthStencilAccess.ReadOnly, MyRender11.Backbuffer);

            RC.SetCB(MyCommon.PROJECTION_SLOT, MyCommon.ProjectionConstants);

            RC.SetBS(MyRender11.BlendTransparent);

            SortTransparent();
            var mapping = MyMapping.MapDiscard(MyCommon.ProjectionConstants);
            mapping.stream.Write(Matrix.Transpose(MyEnvironment.ViewProjectionAt0));
            mapping.Unmap();

            CheckBufferSize(m_vertexList.Count);

            RC.SetVB(0, m_VB.Buffer, m_VB.Stride);

            DataStream stream;
            RC.Context.MapSubresource(m_VB.Buffer, MapMode.WriteDiscard, MapFlags.None, out stream);
            for (int i = 0; i < m_vertexList.Count; i++)
                stream.Write(m_vertexList[i]);
            RC.Context.UnmapSubresource(m_VB.Buffer, 0);
            stream.Dispose();

            RC.Context.Draw(m_vertexList.Count, 0);

            RC.SetBS(null);

            m_vertexList.Clear();
            m_postSortVertexList.Clear();
            m_triangleSortDistance.Clear();
            m_sortedIndices.Clear();
        }
    }
}
