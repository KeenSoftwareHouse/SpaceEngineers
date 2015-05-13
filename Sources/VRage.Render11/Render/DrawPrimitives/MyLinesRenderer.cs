using System;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using VRageRender.Resources;
using VRageRender.Vertex;
using Buffer = SharpDX.Direct3D11.Buffer;
using Matrix = VRageMath.Matrix;
using Rectangle = VRageMath.Rectangle;
using RectangleF = VRageMath.RectangleF;
using Vector2 = VRageMath.Vector2;
using Vector3 = VRageMath.Vector3;
using BoundingBox = VRageMath.BoundingBox;
using Color = VRageMath.Color;
using System.Collections.Generic;
using VRageMath.PackedVector;
using VRage.Render11.Shaders;


namespace VRageRender
{
    struct MyLinesBatch
    {
        internal Matrix? m_customViewProjection;
        internal int m_vertexCount;
        internal int m_startVertex;

        internal void SetCustomViewProjection(ref Matrix m)
        {
            m_customViewProjection = m;
        }

        internal void Add(MyVertexFormatPositionColor v)
        {
            MyLinesRenderer.m_lineVertexList.Add(v);
        }

        internal void Add(MyVertexFormatPositionColor from, MyVertexFormatPositionColor to)
        {
            MyLinesRenderer.m_lineVertexList.Add(from);
            MyLinesRenderer.m_lineVertexList.Add(to);
        }

        internal void AddBoundingBox(BoundingBox bb, Color color)
        {
            var v0 = bb.Center - bb.HalfExtents;
            var v1 = v0 + new Vector3(bb.HalfExtents.X * 2, 0, 0);
            var v2 = v0 + new Vector3(bb.HalfExtents.X * 2, bb.HalfExtents.Y * 2, 0);
            var v3 = v0 + new Vector3(0, bb.HalfExtents.Y * 2, 0);

            var v4 = v0 + new Vector3(0, 0, bb.HalfExtents.Z * 2);
            var v5 = v4 + new Vector3(bb.HalfExtents.X * 2, 0, 0);
            var v6 = v4 + new Vector3(bb.HalfExtents.X * 2, bb.HalfExtents.Y * 2, 0);
            var v7 = v4 + new Vector3(0, bb.HalfExtents.Y * 2, 0);

            var bcolor = new Byte4(color.R, color.G, color.B, color.A);

            Add(new MyVertexFormatPositionColor(v0, bcolor));
            Add(new MyVertexFormatPositionColor(v1, bcolor));
            Add(new MyVertexFormatPositionColor(v1, bcolor));
            Add(new MyVertexFormatPositionColor(v2, bcolor));
            Add(new MyVertexFormatPositionColor(v2, bcolor));
            Add(new MyVertexFormatPositionColor(v3, bcolor));
            Add(new MyVertexFormatPositionColor(v0, bcolor));
            Add(new MyVertexFormatPositionColor(v3, bcolor));

            Add(new MyVertexFormatPositionColor(v4, bcolor));
            Add(new MyVertexFormatPositionColor(v5, bcolor));
            Add(new MyVertexFormatPositionColor(v5, bcolor));
            Add(new MyVertexFormatPositionColor(v6, bcolor));
            Add(new MyVertexFormatPositionColor(v6, bcolor));
            Add(new MyVertexFormatPositionColor(v7, bcolor));
            Add(new MyVertexFormatPositionColor(v4, bcolor));
            Add(new MyVertexFormatPositionColor(v7, bcolor));

            Add(new MyVertexFormatPositionColor(v0, bcolor));
            Add(new MyVertexFormatPositionColor(v4, bcolor));
            Add(new MyVertexFormatPositionColor(v1, bcolor));
            Add(new MyVertexFormatPositionColor(v5, bcolor));
            Add(new MyVertexFormatPositionColor(v2, bcolor));
            Add(new MyVertexFormatPositionColor(v6, bcolor));
            Add(new MyVertexFormatPositionColor(v3, bcolor));
            Add(new MyVertexFormatPositionColor(v7, bcolor));
        }

        internal void Commit()
        {
            m_vertexCount = MyLinesRenderer.m_lineVertexList.Count - m_startVertex;
            MyLinesRenderer.m_lineBatches.Add(this);
        }
    };

    static class MyLinesRenderer
    {
        internal static MyShader m_vertexShader = MyShaderCache.Create("line.hlsl", "vs", MyShaderProfile.VS_5_0);
        internal static MyShader m_pixelShader = MyShaderCache.Create("line.hlsl", "ps", MyShaderProfile.PS_5_0);
        internal static InputLayout m_inputLayout;

        internal static MyVertexBuffer m_linesVertexBuffer;

        internal unsafe static void Init()
        {
            m_linesVertexBuffer = MyRender.WrapResource("lines vertex buffer", new MyVertexBuffer(MyRenderConstants.MAX_LINES * 2 * sizeof(MyVertexFormatPositionColor), ResourceUsage.Dynamic));
        }

        internal static List<MyVertexFormatPositionColor> m_lineVertexList = new List<MyVertexFormatPositionColor>();
        internal static List<MyLinesBatch> m_lineBatches = new List<MyLinesBatch>();

        internal static MyLinesBatch CreateLinesBatch()
        {
            var batch = new MyLinesBatch();
            batch.m_startVertex = m_lineVertexList.Count;
            return batch;
        }

        internal static unsafe void Draw()
        {
            if(m_inputLayout == null)
            {
                var linesInput = MyVertexInput.Empty()
                .Append(MyVertexInputComponentType.POSITION_PACKED)
                .Append(MyVertexInputComponentType.COLOR4);

                m_inputLayout = MyVertexInput.CreateLayout(linesInput.Hash, MyShaderCache.CompileFromFile("line.hlsl", "vs", MyShaderProfile.VS_5_0).Bytecode);
            }

            var context = MyRender.Context;
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(m_linesVertexBuffer.Buffer, MyVertexFormatPositionColor.STRIDE, 0));
            context.InputAssembler.InputLayout = m_inputLayout;

            context.Rasterizer.SetViewport(0, 0, MyRender.ViewportResolution.X, MyRender.ViewportResolution.Y);
            context.Rasterizer.State = MyRender.m_linesRasterizerState;

            context.VertexShader.Set(m_vertexShader.VertexShader);
            context.VertexShader.SetConstantBuffer(0, MyResources.ProjectionConstants.Buffer);

            context.PixelShader.Set(m_pixelShader.PixelShader);

            context.OutputMerger.ResetTargets();
            context.OutputMerger.SetTargets(MyRender.Backbuffer.RenderTarget);

            DataStream stream;
            context.MapSubresource(m_linesVertexBuffer.Buffer, MapMode.WriteNoOverwrite, MapFlags.None, out stream);
            for (int i = 0; i < m_lineVertexList.Count; i++)
                stream.Write(m_lineVertexList[i]);
            context.UnmapSubresource(m_linesVertexBuffer.Buffer, 0);
            stream.Dispose();

            Matrix viewProjection;
            for (int b = 0; b < m_lineBatches.Count; b++ )
            {
                if(m_lineBatches[b].m_customViewProjection.HasValue)
                {
                    viewProjection = m_lineBatches[b].m_customViewProjection.Value;
                }
                else
                {
                    viewProjection = MyEnvironment.CameraView * MyEnvironment.Projection;
                }


                stream = MyMapping.MapDiscard(MyResources.ProjectionConstants.Buffer);
                stream.Write(Matrix.Transpose(viewProjection));
                MyMapping.Unmap(MyResources.ProjectionConstants.Buffer, stream);


                context.Draw(m_lineBatches[b].m_vertexCount, m_lineBatches[b].m_startVertex);
            }

            // cleanup
            m_lineVertexList.Clear();
            m_lineBatches.Clear();
        }
    }
}
