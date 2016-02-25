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
using Vector4 = VRageMath.Vector4;
using Color = VRageMath.Color;
using Matrix = VRageMath.Matrix;
using BoundingFrustum = VRageMath.BoundingFrustum;
using BoundingBox = VRageMath.BoundingBox;
using System.Diagnostics;

namespace VRageRender
{

    internal class MyDebugMesh {
        public VertexBufferId vbuffer;
        public bool edges;
        public bool depth;

        public unsafe MyDebugMesh(MyRenderMessageDebugDrawMesh message)
        {
            vbuffer = MyHwBuffers.CreateVertexBuffer(message.VertexCount, sizeof(MyVertexFormatPositionColor), BindFlags.VertexBuffer, ResourceUsage.Dynamic);

            Update(message);
        }

        internal unsafe void Update(MyRenderMessageDebugDrawMesh message)
        {
            edges = !message.Shaded;
            depth = message.DepthRead;

            if (message.VertexCount > vbuffer.Capacity)
            {
                MyHwBuffers.ResizeVertexBuffer(vbuffer, message.VertexCount);
            }

            var mapping = MyMapping.MapDiscard(MyPrimitivesRenderer.RC.DeviceContext, vbuffer.Buffer);
            for (int i = 0; i < message.VertexCount; i++)
            {
                MyVertexFormatPositionColor vert = new MyVertexFormatPositionColor(Vector3.Transform(message.Vertices[i].Position, message.WorldMatrix), message.Vertices[i].Color);
                mapping.WriteAndPosition(ref vert);
            }
            mapping.Unmap();

            message.Vertices.Clear();
        }

        internal void Close()
        {
            MyHwBuffers.Destroy(vbuffer);
        }
    }

    class MyPrimitivesRenderer : MyImmediateRC
    {
        static int m_currentBufferSize;
        static VertexBufferId m_VB;
        internal static List<MyVertexFormatPositionColor> m_vertexList = new List<MyVertexFormatPositionColor>();
        internal static List<MyVertexFormatPositionColor> m_postSortVertexList = new List<MyVertexFormatPositionColor>();

        internal static Dictionary<uint, MyDebugMesh> m_debugMeshes = new Dictionary<uint, MyDebugMesh>();

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

            m_vs = MyShaders.CreateVs("primitive.hlsl");
            m_ps = MyShaders.CreatePs("primitive.hlsl");
            m_inputLayout = MyShaders.CreateIL(m_vs.BytecodeId, MyVertexLayouts.GetLayout(MyVertexInputComponentType.POSITION3, MyVertexInputComponentType.COLOR4));
        }

        internal static void Unload()
        {
            foreach (var mesh in m_debugMeshes.Values)
            {
                mesh.Close();
            }
            m_debugMeshes.Clear();
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

        internal static void DebugMesh(MyRenderMessageDebugDrawMesh message)
        {
            if (!m_debugMeshes.ContainsKey(message.ID))
                m_debugMeshes.Add(message.ID, new MyDebugMesh(message));
            else
                m_debugMeshes[message.ID].Update(message);
        }

        internal static void RemoveDebugMesh(uint ID)
        {
            if (m_debugMeshes.ContainsKey(ID))
            {
                m_debugMeshes[ID].Close();
                m_debugMeshes.Remove(ID);
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

        internal static void DrawQuadClockWise(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Color color)
        {
            DrawTriangle(v0, v1, v2, color);
            DrawTriangle(v0, v2, v3, color);
        }

        internal static void DrawQuadRowWise(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Color color)
        {
            DrawTriangle(v0, v1, v2, color);
            DrawTriangle(v1, v3, v2, color);
        }

        // Assuming natural order (+Y) first
        internal unsafe static void Draw6FacedConvex(Vector3D[] vertices, Color color, float alpha)
        {
            Debug.Assert(vertices.Length == 8);
            fixed(Vector3D* verticesPtr = vertices)
            {
                Draw6FacedConvex(verticesPtr, color, alpha);
            }
        }

        // Assuming natural order (+Y) first
        internal unsafe static void Draw6FacedConvex(Vector3D* vertices, Color color, float alpha)
        {
            Color cc = color;
            cc.A = (byte)(alpha * 255);

            Vector3D c = MyEnvironment.CameraPosition;
            Vector3D v0 = vertices[0] - c, v1 = vertices[1] - c, v2 = vertices[2] - c, v3 = vertices[3] - c, v4 = vertices[4] - c, v5 = vertices[5] - c, v6 = vertices[6] - c, v7 = vertices[7] - c;

            DrawQuadRowWise(v0, v1, v2, v3, cc);
            DrawQuadRowWise(v4, v5, v6, v7, cc);

            DrawQuadRowWise(v0, v1, v4, v5, cc);
            DrawQuadRowWise(v0, v2, v4, v6, cc);
            DrawQuadRowWise(v2, v3, v6, v7, cc);
            DrawQuadRowWise(v3, v1, v7, v5, cc);
        }

        internal unsafe static void Draw6FacedConvexZ(Vector3[] vertices, Color color, float alpha)
        {
            Debug.Assert(vertices.Length == 8);

            fixed(Vector3* verticesPtr = vertices)
            {
                Draw6FacedConvexZ(verticesPtr, color, alpha);
            }
        }

        // Assuming engine order (+Z first)
        internal unsafe static void Draw6FacedConvexZ(Vector3* vertices, Color color, float alpha)
        {
            Color c = color;
            c.A = (byte)(alpha * 255);

            DrawQuadClockWise(vertices[0], vertices[1], vertices[2], vertices[3], c);
            DrawQuadClockWise(vertices[4], vertices[5], vertices[6], vertices[7], c);

            /* top left bottom right */
            DrawQuadClockWise(vertices[0], vertices[1], vertices[5], vertices[4], c);
            DrawQuadClockWise(vertices[0], vertices[3], vertices[7], vertices[4], c);
            DrawQuadClockWise(vertices[3], vertices[2], vertices[6], vertices[7], c);
            DrawQuadClockWise(vertices[2], vertices[1], vertices[5], vertices[6], c);
        }

        internal static void DrawFrustum(BoundingFrustum frustum, Color color, float alpha)
        {
            var corners = frustum.GetCorners();

            Draw6FacedConvexZ(corners, color, alpha);
        }

        internal static void Draw(MyBindableResource renderTarget, MyBindableResource depth)
        {
            RC.SetupScreenViewport();
            RC.DeviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            RC.SetIL(m_inputLayout);

            RC.SetRS(MyRender11.m_nocullRasterizerState);
            RC.SetDS(MyDepthStencilState.DefaultDepthState);

            RC.SetVS(m_vs);
            RC.SetPS(m_ps);

            RC.BindDepthRT(depth, DepthStencilAccess.ReadOnly, renderTarget);

            RC.SetCB(MyCommon.PROJECTION_SLOT, MyCommon.ProjectionConstants);

            RC.SetBS(MyRender11.BlendTransparent);

            SortTransparent();
            var transpose = Matrix.Transpose(MyEnvironment.ViewProjectionAt0);
            var mapping = MyMapping.MapDiscard(MyCommon.ProjectionConstants);
            mapping.WriteAndPosition(ref transpose);
            mapping.Unmap();

            CheckBufferSize(m_vertexList.Count);

            RC.SetVB(0, m_VB.Buffer, m_VB.Stride);

            if (m_vertexList.Count > 0)
            {
                mapping = MyMapping.MapDiscard(m_VB.Buffer);
                mapping.WriteAndPosition(m_vertexList.GetInternalArray(), 0, m_vertexList.Count);
                mapping.Unmap();
            }

            RC.DeviceContext.Draw(m_vertexList.Count, 0);

            if (m_debugMeshes.Count > 0)
            {
                var transposeViewProj = Matrix.Transpose(MyEnvironment.ViewProjection);
                mapping = MyMapping.MapDiscard(MyCommon.ProjectionConstants);
                mapping.WriteAndPosition(ref transposeViewProj);
                mapping.Unmap();
            }

            foreach (var mesh in m_debugMeshes.Values)
            {
                if (mesh.depth)
                    RC.BindDepthRT(depth, DepthStencilAccess.ReadWrite, MyRender11.Backbuffer);
                else
                    RC.BindDepthRT(null, DepthStencilAccess.ReadOnly, null);

                if (mesh.edges)
                {
                    RC.SetRS(MyRender11.m_nocullWireframeRasterizerState);
                }
                else
                {
                    RC.SetRS(MyRender11.m_nocullRasterizerState);
                }

                RC.SetVB(0, mesh.vbuffer.Buffer, mesh.vbuffer.Stride);
                RC.DeviceContext.Draw(mesh.vbuffer.Capacity, 0);
            }

            RC.SetBS(null);

            m_vertexList.Clear();
            m_postSortVertexList.Clear();
            m_triangleSortDistance.Clear();
            m_sortedIndices.Clear();
        }
    }
}
