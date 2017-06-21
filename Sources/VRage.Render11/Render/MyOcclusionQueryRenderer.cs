using System.Collections.Generic;
using SharpDX.Direct3D;
using VRage.Library.Utils;
using VRage.Render11.RenderContext;
using VRage.Render11.Resources;
using VRageMath;

namespace VRageRender
{
    class MyOcclusionQueryRender
    {
        internal MyOcclusionQuery Query;
        internal float Size;
        internal Vector3D Position;
        internal bool Running;
        internal bool Visible;
        internal float FreqMinMs;
        internal float FreqRndMs;

        internal float Result;
        internal float LastResult;
        internal float NextResult;

        internal float QueryTime;
        internal float LastQueryTime;
    }

    internal static class MyOcclusionQueryRenderer
    {
        private static readonly List<MyOcclusionQueryRender> m_queries = new List<MyOcclusionQueryRender>();
        private static VertexShaderId m_vs;
        private static PixelShaderId m_ps;
        private static IVertexBuffer m_vb;
        private static MyVbConstantElement[] m_tempBuffer;
        private static MyOcclusionQuery[] m_tempBuffer2;
        private static InputLayoutId m_inputLayout;
        private static readonly MyRandom m_random = new MyRandom();

        internal static unsafe void Init()
        {
            m_vs = MyShaders.CreateVs("Primitives/OcclusionQuery.hlsl", null);
            m_ps = MyShaders.CreatePs("Primitives/OcclusionQuery.hlsl", null);
            m_inputLayout = MyShaders.CreateIL(m_vs.BytecodeId, MyVertexLayouts.GetLayout(
                new MyVertexInputComponent(MyVertexInputComponentType.CUSTOM4_0, MyVertexInputComponentFreq.PER_INSTANCE)));
        }
        internal static MyOcclusionQueryRender Get(string debugName)
        {
            var query = new MyOcclusionQueryRender()
            {
                Query = MyQueryFactory.CreateOcclusionQuery(debugName),
                Size = 1.0f,
                QueryTime = 0
            };
            m_queries.Add(query);
            return query;
        }

        internal static void Remove(MyOcclusionQueryRender query)
        {
            m_queries.Remove(query);
            query.Query.Destroy();
        }

        struct MyVbConstantElement
        {
            public Vector3 Position;
            public float Size;
        }

        internal static unsafe void Render(MyRenderContext RC, IDepthStencil ds, IRtvBindable rtv)
        {
            bool debugDraw = MyRender11.Settings.DrawOcclusionQueriesDebug;

            if (m_vb == null || m_tempBuffer2.Length < m_queries.Count)
            {
                int allocCount = System.Math.Max(m_queries.Count * 3 / 2, 32);

                if (m_vb == null)
                {
                    m_vb = VRage.Render11.Common.MyManagers.Buffers.CreateVertexBuffer("MyOcclusionQueryRenderer.VB", allocCount,
                        sizeof(MyVbConstantElement), usage: SharpDX.Direct3D11.ResourceUsage.Dynamic);
                }
                else
                {
                    VRage.Render11.Common.MyManagers.Buffers.Resize(m_vb, allocCount);
                }
                m_tempBuffer = new MyVbConstantElement[allocCount];
                m_tempBuffer2 = new MyOcclusionQuery[allocCount];
            }

            VRage.Profiler.ProfilerShort.Begin("Gather");
            int ctr = 0;
            float currentTime = MyCommon.TimerMs;
            foreach (var item in m_queries)
            {
                Vector3 cameraPos = item.Position - MyRender11.Environment.Matrices.CameraPosition;
                if (debugDraw)
                {
                    item.Result = item.LastResult = item.NextResult = 0;
                }
                else 
                {
                    if (item.Running)
                    {
                        var result = item.Query.GetResult(false);
                        if (result != -1)
                        {
                            var dist = cameraPos.Length();
                            var viewPos = new Vector3(item.Size, item.Size, dist);
                            var projPos = Vector3.Transform(viewPos, MyRender11.Environment.Matrices.Projection);
                            var pixels = new Vector2(projPos.X, projPos.Y) * MyRender11.ResolutionF / 2;
                            var squared = System.Math.Abs(pixels.X * pixels.Y);
                            item.LastResult = item.Result;
                            item.NextResult = System.Math.Min(result / squared, 1.0f);
                            item.LastQueryTime = currentTime;
                            item.QueryTime = currentTime + item.FreqMinMs + m_random.NextFloat() * item.FreqRndMs;
                            item.Running = false;
                        }
                        else continue;
                    }
                }

                if (System.Math.Abs(item.QueryTime - item.LastQueryTime) < 0.1f)
                    item.Result = item.NextResult;
                else
                {
                    float factor = (currentTime - item.LastQueryTime) / (item.QueryTime - item.LastQueryTime);
                    item.Result = MathHelper.Lerp(item.LastResult, item.NextResult, factor);
                }
                if (!item.Visible || currentTime < item.QueryTime)
                    continue;
                item.Running = true;

                var data = new MyVbConstantElement
                {
                    Position = cameraPos,
                    Size = item.Size
                };
                m_tempBuffer2[ctr] = item.Query;
                m_tempBuffer[ctr] = data;
                ctr++;

                item.Visible = false;
            }

            if (ctr > 0)
            {
                VRage.Profiler.ProfilerShort.BeginNextBlock("Setup");
                RC.SetInputLayout(m_inputLayout);
                RC.SetPrimitiveTopology(PrimitiveTopology.TriangleStrip);

                RC.VertexShader.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);

                RC.SetRasterizerState(MyRasterizerStateManager.NocullRasterizerState);
                RC.SetDepthStencilState(MyDepthStencilStateManager.DefaultDepthState);

                RC.VertexShader.Set(m_vs);
                if (debugDraw)
                {
                    RC.PixelShader.Set(m_ps);
                    RC.SetRtv(ds, MyDepthStencilAccess.ReadOnly, rtv);
                }
                else
                {
                    RC.SetRtv(ds, MyDepthStencilAccess.ReadOnly, null);
                    RC.PixelShader.Set(null);
                }

                VRage.Profiler.ProfilerShort.BeginNextBlock("Map");
                MyMapping mappingVb = MyMapping.MapDiscard(RC, m_vb);
                mappingVb.WriteAndPosition(m_tempBuffer, ctr);
                mappingVb.Unmap();

                RC.SetVertexBuffer(0, m_vb);
                
                VRage.Profiler.ProfilerShort.BeginNextBlock("Render");
                if (debugDraw)
                {
                    for (int i = 0; i < ctr; i++)
                        RC.DrawInstanced(4, 1, 0, i);
                }
                else
                {
                    for (int i = 0; i < ctr; i++)
                    {
                        m_tempBuffer2[i].Begin();
                        RC.DrawInstanced(4, 1, 0, i);

                        m_tempBuffer2[i].End();
                    }
                }
            }
            VRage.Profiler.ProfilerShort.End();

            RC.SetPrimitiveTopology(PrimitiveTopology.TriangleList);
        }
    }
}
