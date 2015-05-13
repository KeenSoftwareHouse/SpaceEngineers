using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Render11.Shaders;
using VRageMath;
using VRageRender.Resources;
using VRageRender.Vertex;
using Vector3 = VRageMath.Vector3;
using Color = VRageMath.Color;
using Matrix = VRageMath.Matrix;
using BoundingFrustum = VRageMath.BoundingFrustum;
using BoundingBox = VRageMath.BoundingBox;

namespace VRageRender
{
    class MyRendererBase
    {
        internal virtual void Init()
        {

        }

        internal virtual void Destory()
        {

        }

        internal virtual void Draw()
        {

        }
    }

    class MyShapesRenderer
    {
        internal static MyShader m_vertexShader = MyShaderCache.Create("primitive.hlsl", "vs", MyShaderProfile.VS_5_0);
        internal static MyShader m_pixelShader = MyShaderCache.Create("primitive.hlsl", "ps", MyShaderProfile.PS_5_0);

        internal static MyVertexBuffer m_vertexBuffer;
        internal static InputLayout m_inputLayout;

        internal static List<MyVertexFormatPositionColor> m_vertexList = new List<MyVertexFormatPositionColor>();
        internal static List<MyVertexFormatPositionColor> m_postSortVertexList = new List<MyVertexFormatPositionColor>();

        internal unsafe static void Init()
        {
            m_vertexBuffer = MyRender.WrapResource(new MyVertexBuffer(500 * sizeof(MyVertexFormatPositionColor), ResourceUsage.Dynamic),
                "primitives vertex buffer");
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

            m_sortedIndices.Sort((x, y) => m_triangleSortDistance[y].CompareTo(m_triangleSortDistance[x]));

            for(int i=0; i< trianglesNum; i++)
            {
                m_postSortVertexList.Add(m_vertexList[m_sortedIndices[i] * 3]);
                m_postSortVertexList.Add(m_vertexList[m_sortedIndices[i] * 3 + 1]);
                m_postSortVertexList.Add(m_vertexList[m_sortedIndices[i] * 3 + 2]);
            }
        }

        internal static void DrawTriangle(Vector3 v0, Vector3 v1, Vector3 v2, Color color)
        {
            var distance = ((v0 + v1 + v2) / 3f - MyEnvironment.CameraPosition).LengthSquared();
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

        internal static void DrawFrustum(BoundingFrustum frustum, Color color, float alpha)
        {
            var corners = frustum.GetCorners();

            Color c = color;
            c.A = (byte)(alpha * 255);
            
            DrawQuad(corners[0], corners[1], corners[2], corners[3], c);
            DrawQuad(corners[4], corners[5], corners[6], corners[7], c);

            // top left bottom right
            DrawQuad(corners[0], corners[1], corners[5], corners[4], c);
            DrawQuad(corners[0], corners[3], corners[7], corners[4], c);
            DrawQuad(corners[3], corners[2], corners[6], corners[7], c);
            DrawQuad(corners[2], corners[1], corners[5], corners[6], c);
        }

        internal static void Draw()
        {
            if (m_inputLayout == null)
            {
                var input = MyVertexInput.Empty()
                .Append(MyVertexInputComponentType.POSITION4H)
                .Append(MyVertexInputComponentType.COLOR4);

                m_inputLayout = MyVertexInput.CreateLayout(input.Hash, MyShaderCache.CompileFromFile("primitive.hlsl", "vs", MyShaderProfile.VS_5_0).Bytecode);
            }

            var context = MyRender.Context;
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(m_vertexBuffer.Buffer, MyVertexFormatPositionColor.STRIDE, 0));
            context.InputAssembler.InputLayout = m_inputLayout;

            context.Rasterizer.State = MyRender.m_nocullRasterizerState;

            context.Rasterizer.SetViewport(0, 0, MyRender.ViewportResolution.X, MyRender.ViewportResolution.Y);

            context.VertexShader.Set(m_vertexShader.VertexShader);
            context.VertexShader.SetConstantBuffer(MyCommon.ProjectionSlot, MyCommon.ProjectionConstants.Buffer);

            context.PixelShader.Set(m_pixelShader.PixelShader);

            context.OutputMerger.BlendState = MyRender.m_transparentBlendState;
            context.OutputMerger.ResetTargets();
            context.OutputMerger.SetTargets(MyRender.MainGbuffer.DepthBuffer.DepthStencil, MyRender.Backbuffer.RenderTarget);

            //SortTransparent();

            DataStream stream;
            context.MapSubresource(m_vertexBuffer.Buffer, MapMode.WriteDiscard, MapFlags.None, out stream);
            for (int i = 0; i < m_vertexList.Count; i++)
                stream.Write(m_vertexList[i]);
            context.UnmapSubresource(m_vertexBuffer.Buffer, 0);
            stream.Dispose();

            context.Draw(m_vertexList.Count, 0);

            m_vertexList.Clear();
            m_postSortVertexList.Clear();
            m_triangleSortDistance.Clear();
            m_sortedIndices.Clear();
        }
    }
}
