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
using BoundingSphere = VRageMath.BoundingSphere;
using BoundingFrustum = VRageMath.BoundingFrustum;
using Color = VRageMath.Color;
using System.Collections.Generic;
using VRageMath.PackedVector;
using VRage.Generics;


namespace VRageRender
{
    class MyLinesBatch
    {
        internal Matrix? CustomViewProjection;
        internal int VertexCount;
        internal int StartVertex;
        internal List<MyVertexFormatPositionColor> List;
        internal bool IgnoreDepth;

        internal void Construct()
        {
            CustomViewProjection = null;
            IgnoreDepth = false;
            if(List == null)
            {
                List = new List<MyVertexFormatPositionColor>();
            }
            else
            {
                List.Clear();
            }
            VertexCount = 0;
            StartVertex = 0;
        }

        internal void Add(MyVertexFormatPositionColor v)
        {
            List.Add(v);
        }

        internal void Add(MyVertexFormatPositionColor from, MyVertexFormatPositionColor to)
        {
            List.Add(from);
            List.Add(to);
        }

        internal void Add(Vector3 from, Vector3 to, Color colorFrom, Color? colorTo = null)
        {
            List.Add(new MyVertexFormatPositionColor(from, new Byte4(colorFrom.PackedValue)));
            List.Add(new MyVertexFormatPositionColor(to, colorTo.HasValue ? new Byte4(colorTo.Value.PackedValue) : new Byte4(colorFrom.PackedValue)));
        }

        internal void AddQuad(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Color color)
        {
            var bcolor = new Byte4(color.R, color.G, color.B, color.A);

            Add(new MyVertexFormatPositionColor(v0, bcolor));
            Add(new MyVertexFormatPositionColor(v1, bcolor));

            Add(new MyVertexFormatPositionColor(v1, bcolor));
            Add(new MyVertexFormatPositionColor(v2, bcolor));

            Add(new MyVertexFormatPositionColor(v2, bcolor));
            Add(new MyVertexFormatPositionColor(v3, bcolor));

            Add(new MyVertexFormatPositionColor(v3, bcolor));
            Add(new MyVertexFormatPositionColor(v0, bcolor));
        }

        internal void AddCone(Vector3 translation, Vector3 directionVec, Vector3 baseVec, int tessalation, Color color)
        {
            var axis = directionVec;
            axis.Normalize();

            var apex = translation + directionVec;

            var steps = tessalation;
            var stepsRcp = (float)(Math.PI * 2 / steps);
            for (int i = 0; i < 32; i++)
            {
                float a0 = i * stepsRcp;
                float a1 = (i + 1) * stepsRcp;

                var A = translation + Vector3.Transform(baseVec, Matrix.CreateFromAxisAngle(axis, a0));
                var B = translation + Vector3.Transform(baseVec, Matrix.CreateFromAxisAngle(axis, a1));

                Add(A, B, color);
                Add(A, apex, color);
            }
        }

        internal void AddFrustum(BoundingFrustum bf, Color color)
        {
            Add6FacedConvex(bf.GetCorners(), color);
        }

        internal void Add6FacedConvex(Vector3 [] vertices, Color color)
        {
            AddQuad(vertices[0], vertices[1], vertices[2], vertices[3], color);
            AddQuad(vertices[4], vertices[5], vertices[6], vertices[7], color);
            AddQuad(vertices[0], vertices[1], vertices[5], vertices[4], color);
            AddQuad(vertices[0], vertices[3], vertices[7], vertices[4], color);
            AddQuad(vertices[3], vertices[2], vertices[6], vertices[7], color);
            AddQuad(vertices[2], vertices[1], vertices[5], vertices[6], color);
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

        internal void AddSphereRing(BoundingSphere sphere, Color color, Matrix onb)
        {
            float increment = 1.0f / 32;
            for (float i=0; i < 1; i += increment)
            {
                float a0 = 2* (float)Math.PI * i;
                float a1 = 2* (float)Math.PI * (i + increment);

                Add(
                    Vector3.Transform(new Vector3(Math.Cos(a0), 0, Math.Sin(a0)) * sphere.Radius, onb) + sphere.Center,
                    Vector3.Transform(new Vector3(Math.Cos(a1), 0, Math.Sin(a1)) * sphere.Radius, onb) + sphere.Center, 
                    color);
            }
        }

        internal void Commit()
        {
            MyLinesRenderer.Commit(this);
        }
    };

    class MyLinesRenderer : MyImmediateRC
    {
        static int m_currentBufferSize;
        static VertexBufferId m_VB;
        internal static List<MyVertexFormatPositionColor> m_vertices = new List<MyVertexFormatPositionColor>();
        static List<MyLinesBatch> m_batches = new List<MyLinesBatch>();

        static VertexShaderId m_vs;
        static PixelShaderId m_ps;
        static InputLayoutId m_inputLayout;

        static MyObjectsPool<MyLinesBatch> m_batchesPool = new MyObjectsPool<MyLinesBatch>(4);

        internal unsafe static void Init()
        {
            m_vs = MyShaders.CreateVs("line.hlsl", "vs");
            m_ps = MyShaders.CreatePs("line.hlsl", "ps");
            m_inputLayout = MyShaders.CreateIL(m_vs.BytecodeId, MyVertexLayouts.GetLayout(MyVertexInputComponentType.POSITION3, MyVertexInputComponentType.COLOR4));

            m_currentBufferSize = 100000;
            m_VB = MyHwBuffers.CreateVertexBuffer(m_currentBufferSize, sizeof(MyVertexFormatPositionColor), BindFlags.VertexBuffer, ResourceUsage.Dynamic);
        }

        static unsafe void CheckBufferSize(int requiredSize)
        {
            if (m_currentBufferSize < requiredSize)
            {
                m_currentBufferSize = (int)(requiredSize * 1.33f);
                MyHwBuffers.ResizeVertexBuffer(m_VB, m_currentBufferSize);
            }
        }

        internal static MyLinesBatch CreateBatch()
        {
            var batch = m_batchesPool.Allocate1();
            batch.Construct();
            return batch;
        }

        internal static void Commit(MyLinesBatch batch)
        {
            batch.VertexCount = batch.List.Count;
            batch.StartVertex = m_vertices.Count;

            if(batch.VertexCount > 0)
            {
                m_batches.Add(batch);
                m_vertices.AddList(batch.List);
                batch.List.Clear();
            }
            else
            {
                m_batchesPool.Deallocate(batch);
            }
        }

        internal static unsafe void Draw(MyBindableResource depth)
        {
            RC.SetupScreenViewport();
            RC.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;
            RC.SetIL(m_inputLayout);

            RC.SetRS(MyRender11.m_linesRasterizerState);

            RC.SetVS(m_vs);
            RC.SetPS(m_ps);

            RC.SetDS(MyDepthStencilState.DefaultDepthState);

            CheckBufferSize(m_vertices.Count);
            RC.SetVB(0, m_VB.Buffer, m_VB.Stride);

            RC.BindDepthRT(depth, DepthStencilAccess.ReadOnly, MyRender11.Backbuffer);
            
            RC.SetCB(MyCommon.PROJECTION_SLOT, MyCommon.ProjectionConstants);

            if(m_batches.Count > 0)
            {
                var mapping = MyMapping.MapDiscard(m_VB.Buffer);
                for (int i = 0; i < m_vertices.Count; i++)
                    mapping.stream.Write(m_vertices[i]);
                mapping.Unmap();

                Matrix prevMatrix = Matrix.Zero;
                foreach (var batch in m_batches)
                {
                    Matrix matrix;
                    if (batch.CustomViewProjection.HasValue)
                    {
                        matrix = batch.CustomViewProjection.Value;
                    }
                    else
                    {
                        matrix = MyEnvironment.ViewProjectionAt0;
                    }

                    if (prevMatrix != matrix)
                    {
                        prevMatrix = matrix;

                        mapping = MyMapping.MapDiscard(MyCommon.ProjectionConstants);
                        mapping.stream.Write(Matrix.Transpose(matrix));
                        mapping.Unmap();
                    }

                    if(batch.IgnoreDepth)
                    {
                        RC.SetDS(MyDepthStencilState.IgnoreDepthStencil);   
                    }
                    else
                    {
                        RC.SetDS(MyDepthStencilState.DefaultDepthState);
                    }

                    RC.Context.Draw(batch.VertexCount, batch.StartVertex);
                }
            }

            RC.SetDS(null);
            RC.SetRS(null);

            m_vertices.Clear();

            foreach(var batch in m_batches)
            {
                m_batchesPool.Deallocate(batch);
            }
            m_batches.Clear();
        }

        internal static void Clear()
        {
            m_vertices.Clear();
            foreach (var batch in m_batches)
            {
                m_batchesPool.Deallocate(batch);
            }
            m_batches.Clear();
        }
    }
}
