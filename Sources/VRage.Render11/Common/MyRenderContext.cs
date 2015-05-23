using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Generics;

using VRageMath;
using VRageRender.Resources;
using VRageRender.Vertex;
using Buffer = SharpDX.Direct3D11.Buffer;
using Matrix = VRageMath.Matrix;
using Vector3 = VRageMath.Vector3;
using BoundingBox = VRageMath.BoundingBox;
using BoundingFrustum = VRageMath.BoundingFrustum;
using VRage.Collections;
using System.Collections.Specialized;
using System.Threading;

namespace VRageRender
{
    enum MyWriteBindingEnum
    {
        None,
        DSV,
        RTV,
        UAV
    }

    struct MyBinding
    {
        internal readonly MyWriteBindingEnum WriteView;
        internal readonly int UavSlot;
        internal readonly bool DsvRead;

        internal MyBinding(MyWriteBindingEnum view, int uavSlot = -1)
        {
            WriteView = view;
            UavSlot = uavSlot;
            DsvRead = false;
        }

        internal MyBinding(bool dsvRead)
        {
            WriteView = MyWriteBindingEnum.None;
            UavSlot = -1;
            DsvRead = dsvRead;
        }
    }

    enum DepthStencilAccess
    {
        ReadWrite,
        DepthReadOnly,
        StencilReadOnly,
        ReadOnly
    }

    public struct MyRCStats
    {
        public int Draw;
        public int DrawInstanced;
        public int DrawIndexed;
        public int DrawIndexedInstanced;
        public int DrawAuto;

        public int SetVB;
        public int SetIB;
        public int SetIL;
        public int SetVS;
        public int SetPS;
        public int SetGS;
        public int SetCB;

        public int SetRasterizerState;
        public int SetBlendState;

        public int BindShaderResources;
        
        internal void Clear()
        {
            Draw = 0;
            DrawInstanced = 0;
            DrawIndexed = 0;
            DrawIndexedInstanced = 0;
            DrawAuto = 0;

            SetVB = 0;
            SetIB = 0;
            SetIL = 0;
            SetVS = 0;
            SetPS = 0;
            SetGS = 0;
            SetCB = 0;

            SetRasterizerState = 0;
            SetBlendState = 0;

            BindShaderResources = 0;
        }

        internal void Gather(MyRCStats other)
        {
            Draw                 += other.Draw;
            DrawInstanced        += other.DrawInstanced;
            DrawIndexed          += other.DrawIndexed;
            DrawIndexedInstanced += other.DrawIndexedInstanced;
            DrawAuto             += other.DrawAuto;

            SetVB               += other.SetVB;
            SetIB               += other.SetIB;
            SetIL               += other.SetIL;
            SetVS               += other.SetVS;
            SetPS               += other.SetPS;
            SetGS               += other.SetGS;
            SetCB               += other.SetCB;
                                 
            SetRasterizerState  += other.SetRasterizerState;
            SetBlendState       += other.SetBlendState;
            
            BindShaderResources += other.BindShaderResources;
        }
    }

    static class MyRenderContextPool
    {
        const int PoolSize = 8;
        static MyConcurrentPool<MyRenderContext> m_pool = new MyConcurrentPool<MyRenderContext>(PoolSize, true);
        //internal static MyConcurrentQueue<DeviceContext> m_intialized = new MyConcurrentQueue<DeviceContext>(PoolSize);
        internal static MyRenderContext Immediate;

        static MyRenderContextPool()
        {
            Immediate = new MyRenderContext(MyRender11.ImmediateContext, false);
        }

        static internal MyRenderContext AcquireRC()
        {
            var result = m_pool.Get();
            result.LazyInitialize();
            return result;
        }

        static internal void FreeRC(MyRenderContext rc)
        {
            Debug.Assert(rc.OkToRelease);
            m_pool.Return(rc);
        }

        internal static void OnDeviceReset()
        {
            Immediate.Context = MyRender11.ImmediateContext;
            m_pool = new MyConcurrentPool<MyRenderContext>(PoolSize, true);
        }
    }

    enum MyShaderStage
    {
        VS,
        PS,
        CS
    }

    struct MyStageBinding
    {
        internal int Slot;
        internal MyShaderStage Stage;
    }

    struct MyStageSrvBinding
    {
        internal MyShaderStage Stage;
        internal int Slot;
        //internal int Length;
        //internal int Version;
    }

    struct MyContextState
    {
        internal Buffer[] m_VBs;
        internal int[] m_strides;
        internal Buffer[] m_CBs;
        internal Buffer m_IB;

        internal InputLayout m_inputLayout;
        internal PixelShader m_ps;
        internal VertexShader m_vs;
        internal GeometryShader m_gs;

        internal RenderTargetView[] m_RTVs;
        internal ShaderResourceView[] m_SRVs;

        internal RasterizerState m_RS;
        internal BlendState m_BS;
        internal DepthStencilState m_DS;
        internal int m_stencilRef;

        // for read-write resources
        internal SortedDictionary<int, MyBinding> m_bindings;
        internal SortedSet<Tuple<int, int>> m_srvBindings;

        // for read-only resources
        internal Dictionary<Buffer, int> m_constantsVersion;
        internal Dictionary<MyStageBinding, Buffer> m_constantBindings;
        internal Dictionary<MyStageSrvBinding, int> m_srvTableBindings;
        internal List<MyStageSrvBinding> m_srvBindings1;

        internal void Clear()
        {
            if (m_VBs == null)
            {
                m_VBs = new Buffer[8];
                m_strides = new int[8];
                m_CBs = new Buffer[8];

                m_bindings = new SortedDictionary<int, MyBinding>();
                m_srvBindings = new SortedSet<Tuple<int, int>>();

                m_RTVs = new RenderTargetView[8];
                m_SRVs = new ShaderResourceView[8];

                m_constantsVersion = new Dictionary<Buffer, int>();
                m_constantBindings = new Dictionary<MyStageBinding, Buffer>();
                m_srvTableBindings = new Dictionary<MyStageSrvBinding, int>();
                m_srvBindings1 = new List<MyStageSrvBinding>();
            }

            m_IB = null;

            m_inputLayout = null;
            m_ps = null;
            m_vs = null;
            m_gs = null;

            m_RS = null;
            m_BS = null;
            m_DS = null;
            m_stencilRef = 0;

            Array.Clear(m_VBs, 0, m_VBs.Length);
            Array.Clear(m_CBs, 0, m_CBs.Length);

            m_bindings.Clear();
            m_srvBindings.Clear();

            m_constantsVersion.Clear();
            m_constantBindings.Clear();
            m_srvTableBindings.Clear();
            m_srvBindings1.Clear();
        }
    }

    class MyRenderContext
    {
        static readonly int[] ZeroOffsets = { 0, 0, 0, 0, 0, 0, 0, 0 };

        internal MyContextState State;
        internal DeviceContext  Context;
        internal MyRCStats Stats;
        internal MyFrameProfilingContext ProfilingQueries = new MyFrameProfilingContext();
        bool m_deferred;
        bool m_finished;
        bool m_joined;
        internal CommandList m_commandList;

        internal bool Joined { get { return m_joined; } }
        internal bool OkToRelease { get { return m_finished == true && m_joined == true && ProfilingQueries.m_issued.Count == 0; } }

        public MyRenderContext()
        {
            m_deferred = true;
        }

        internal MyRenderContext(DeviceContext context, bool isDeferred)
        {
            m_deferred = isDeferred;
            Context = context;
            LazyInitialize();
        }

        internal void LazyInitialize()
        {
            if(Context == null)
            {
                Context = new DeviceContext(MyRender11.Device);
                //MyRenderContextPool.m_intialized.Enqueue(Context);
            }

            State.Clear();
            Stats.Clear();
            m_finished = false;
            m_joined = false;
            m_commandList = null;
        }

        internal void Join()
        {
            Debug.Assert(!m_joined && m_deferred);
            m_joined = true;

            Finish();

            MyRender11.GatherStats(Stats);

            MyGpuProfiler.Join(ProfilingQueries);
        }

        internal void Finish()
        {
            if(!m_finished && m_deferred)
            {
                m_finished = true;
                m_commandList = Context.FinishCommandList(false);
            }
        }

        internal CommandList GrabCommandList()
        {
            Debug.Assert(!(m_finished && m_commandList == null), "you grabbed that list already");

            Finish();
            var result = m_commandList;
            m_commandList = null;
            return result;
        }

        internal void Clear()
        {
            State.Clear();
        }

        bool UpdateVB(int slot, Buffer vb, int stride)
        {
            if (State.m_VBs[slot] != vb || State.m_strides[slot] != stride)
            {
                State.m_VBs[slot] = vb;
                State.m_strides[slot] = stride;

                return true;
            }
            return false;
        }

        internal void SetVB(int slot, Buffer vb, int stride)
        {
            if (UpdateVB(slot, vb, stride))
            {
                Context.InputAssembler.SetVertexBuffers(slot, new VertexBufferBinding(vb, stride, 0));
                Stats.SetVB++;
            }
        }

        internal void SetVBs(Buffer[] vbs, int[] strides)
        {
            if(vbs != null && strides != null)
            { 

                bool change = false;
                bool lastSlot = false;
                for (int i = 0; i < vbs.Length; i++)
                {
                    if (vbs[i] != null) {
                        lastSlot = change == false && i == (vbs.Length - 1);
                        change = UpdateVB(i, vbs[i], strides[i]) || change;
                    }
                }
                if(lastSlot)
                {
                    int slot = vbs.Length - 1;
                    Context.InputAssembler.SetVertexBuffers(slot, new VertexBufferBinding(State.m_VBs[slot], State.m_strides[slot], 0));
                    Stats.SetVB++;
                }
                else if (change)
                { 
                    Context.InputAssembler.SetVertexBuffers(0, State.m_VBs, State.m_strides, ZeroOffsets);
                    Stats.SetVB++;
                }

            }
        }

        internal void SetIB(Buffer ib, Format format)
        {
            if (State.m_IB != ib)
            {
                State.m_IB = ib;
                Context.InputAssembler.SetIndexBuffer(ib, format, 0);
                Stats.SetIB++;
            }
        }

        internal void CSSetCB(int slot, Buffer cb)
        {
            Context.ComputeShader.SetConstantBuffer(slot, cb);
        }

        internal void SetCB(int slot, Buffer cb)
        {
            if (State.m_CBs[slot] != cb)
            {
                State.m_CBs[slot] = cb;
                Context.VertexShader.SetConstantBuffer(slot, cb);
                Stats.SetCB++;
                Context.GeometryShader.SetConstantBuffer(slot, cb);
                Stats.SetCB++;
                Context.PixelShader.SetConstantBuffer(slot, cb);
                Stats.SetCB++;
            }
        }

        internal void SetPS(PixelShader ps)
        {
            if (State.m_ps != ps)
            {
                State.m_ps = ps;
                Context.PixelShader.Set(ps);
                Stats.SetPS++;
            }
        }

        internal void SetVS(VertexShader vs)
        {
            if (State.m_vs != vs)
            {
                State.m_vs = vs;
                Context.VertexShader.Set(vs);
                Stats.SetVS++;
            }
        }

        internal void SetCS(ComputeShader cs)
        {
            Context.ComputeShader.Set(cs);
        }

        internal void SetGS(GeometryShader gs)
        {
            if (State.m_gs != gs)
            {
                State.m_gs = gs;
                Context.GeometryShader.Set(gs);
                Stats.SetGS++;
            }
        }

        internal void SetIL(InputLayout il)
        {
            if (State.m_inputLayout != il)
            {
                State.m_inputLayout = il;
                Context.InputAssembler.InputLayout = il;
                Stats.SetIL++;
            }
        }

        internal void SetRS(RasterizerState rs)
        {
            if (State.m_RS != rs)
            {
                State.m_RS = rs;
                Context.Rasterizer.State = rs;
                Stats.SetRasterizerState++;
            }
        }

        internal void SetBS(BlendState bs, Color4? blendFactor = null)
        {
            if (State.m_BS != bs || blendFactor != null)
            {
                State.m_BS = bs;
                Context.OutputMerger.SetBlendState(bs, blendFactor);
                Stats.SetBlendState++;
            }
        }

        internal void SetDS(DepthStencilState ds, int stencilRef = 0)
        {
            if ((State.m_DS != ds) || (State.m_stencilRef != stencilRef))
            {
                State.m_DS = ds;
                State.m_stencilRef = stencilRef;
                Context.OutputMerger.SetDepthStencilState(ds, stencilRef);
            }
        }

        internal void BindRawSRV(int slot, params ShaderResourceView[] srvs)
        {
            Context.VertexShader.SetShaderResources(slot, srvs);
            Context.PixelShader.SetShaderResources(slot, srvs);

            Stats.BindShaderResources++;
            Stats.BindShaderResources++;
        }

        internal void VSBindSRV(int slot, params ShaderResourceView[] srvs)
        {
            Context.VertexShader.SetShaderResources(slot, srvs);

            Stats.BindShaderResources++;
            Stats.BindShaderResources++;
        }

        internal void CSBindRawSRV(int slot, params ShaderResourceView[] srvs)
        {
            Context.ComputeShader.SetShaderResources(slot, srvs);

            Stats.BindShaderResources++;
        }

        internal void BindRawSRV(ShaderResourceView[] srvs)
        {
            Context.PixelShader.SetShaderResources(0, srvs);

            Stats.BindShaderResources++;
        }

        void UnbindSRVRead(int resId)
        {
            foreach (var b in State.m_srvBindings.Where(x => x.Item1 == resId))
            {
                Context.VertexShader.SetShaderResource(b.Item2, null);
                Context.PixelShader.SetShaderResource(b.Item2, null);
                Context.ComputeShader.SetShaderResource(b.Item2, null);
            }
            State.m_srvBindings.RemoveWhere(x => x.Item1 == resId);
        }

        internal void BindUAV(int slot, params MyBindableResource[] UAVs)
        {
            var buffer = new UnorderedAccessView[UAVs.Length];
            for (int i = 0; i < UAVs.Length; i++)
            {
                if (UAVs[i] != null)
                {
                    var ua = UAVs[i] as IUnorderedAccessBindable;
                    Debug.Assert(ua != null);

                    UnbindSRVRead(UAVs[i].GetID());
                    //UnbindDSVReadOnly(UAVs[i].ResId); necessary?

                    int? currentlyBound = null;
                    foreach (var kv in State.m_bindings)
                    {
                        if (kv.Value.UavSlot == slot + i)
                        {
                            currentlyBound = kv.Key;
                            break;
                        }
                    }
                    if (currentlyBound.HasValue)
                    {
                        State.m_bindings.Remove(currentlyBound.Value);
                    }

                    State.m_bindings[UAVs[i].GetID()] = new MyBinding(MyWriteBindingEnum.UAV, slot + i);
                    buffer[i] = ua.UAV;
                }
            }

            Context.ComputeShader.SetUnorderedAccessViews(slot, buffer);
        }

        internal void BindDepthRT(MyBindableResource depthStencil, DepthStencilAccess dsAccess, params MyBindableResource[] RTs)
        {
            DepthStencilView dsv = null;
            if (depthStencil != null)
            {
                var ds = depthStencil as MyDepthStencil;
                Debug.Assert(ds != null);

                // check conflicts
                if (dsAccess == DepthStencilAccess.ReadWrite)
                {
                    // for both
                    // check reads
                    UnbindSRVRead(ds.Depth.GetID());
                    UnbindSRVRead(ds.Stencil.GetID());
                    dsv = ds.m_DSV;
                }
                else if (dsAccess == DepthStencilAccess.DepthReadOnly)
                {
                    // check reads
                    UnbindSRVRead(ds.Depth.GetID());
                    dsv = (ds.Depth as IDepthStencilBindable).DSV;
                }
                else if (dsAccess == DepthStencilAccess.StencilReadOnly)
                {
                    // check reads
                    UnbindSRVRead(ds.Stencil.GetID());
                    dsv = (ds.Stencil as IDepthStencilBindable).DSV;
                }
                else if (dsAccess == DepthStencilAccess.ReadOnly)
                {
                    dsv = ds.m_DSV_ro;
                }
            }
            Array.Clear(State.m_RTVs, 0, State.m_RTVs.Length);
            if (RTs != null)
            {
                for (int i = 0; i < RTs.Length; i++)
                {
                    if (RTs[i] != null)
                    {
                        Debug.Assert(RTs[i] as IRenderTargetBindable != null);
                        UnbindSRVRead(RTs[i].GetID());
                        State.m_RTVs[i] = (RTs[i] as IRenderTargetBindable).RTV;
                    }
                    else
                    {
                        State.m_RTVs[i] = null;
                    }
                }
            }
            Context.OutputMerger.SetTargets(
                dsv,
                State.m_RTVs);

            ClearDsvRtvWriteBindings();

            if (depthStencil != null)
            {
                var ds = depthStencil as MyDepthStencil;
                if (dsAccess == DepthStencilAccess.ReadWrite)
                {
                    State.m_bindings[ds.Depth.GetID()] = new MyBinding(MyWriteBindingEnum.DSV);
                    State.m_bindings[ds.Stencil.GetID()] = new MyBinding(MyWriteBindingEnum.DSV);
                }
                else if (dsAccess == DepthStencilAccess.DepthReadOnly)
                {
                    State.m_bindings[ds.Depth.GetID()] = new MyBinding(true);
                    State.m_bindings[ds.Stencil.GetID()] = new MyBinding(MyWriteBindingEnum.DSV);
                }
                else if (dsAccess == DepthStencilAccess.StencilReadOnly)
                {
                    State.m_bindings[ds.Depth.GetID()] = new MyBinding(MyWriteBindingEnum.DSV);
                    State.m_bindings[ds.Stencil.GetID()] = new MyBinding(true);
                }
                else if (dsAccess == DepthStencilAccess.ReadOnly)
                {
                    State.m_bindings[ds.Depth.GetID()] = new MyBinding(true);
                    State.m_bindings[ds.Stencil.GetID()] = new MyBinding(true);
                }
            }
            if (RTs != null)
            {
                for (int i = 0; i < RTs.Length; i++)
                {
                    if (RTs[i] != null)
                    {
                        State.m_bindings[RTs[i].GetID()] = new MyBinding(MyWriteBindingEnum.RTV);
                    }
                }
            }
        }

        void ClearDsvRtvWriteBindings()
        {
            for (int i = 0; i < State.m_bindings.Count; )
            {
                var view = State.m_bindings.ElementAt(i).Value.WriteView;
                if (view == MyWriteBindingEnum.RTV || view == MyWriteBindingEnum.DSV)
                {
                    State.m_bindings.Remove(State.m_bindings.ElementAt(i).Key);
                }
                else
                    i++;
            }
        }

        internal void BindSRV(int slot, params MyBindableResource[] bindable)
        {
            Array.Clear(State.m_SRVs, 0, State.m_SRVs.Length);
            for (int i = 0; i < bindable.Length; i++)
            {
                Debug.Assert(bindable[i] as IShaderResourceBindable != null);
                var binding = State.m_bindings.Get(bindable[i].GetID());
                if (binding.WriteView == MyWriteBindingEnum.RTV || binding.WriteView == MyWriteBindingEnum.DSV)
                {
                    Context.OutputMerger.ResetTargets();
                    ClearDsvRtvWriteBindings();
                }
                else if (binding.WriteView == MyWriteBindingEnum.UAV)
                {
                    Context.ComputeShader.SetUnorderedAccessView(binding.UavSlot, null);
                    State.m_bindings.Remove(bindable[i].GetID());
                }

                State.m_srvBindings.Add(Tuple.Create(bindable[i].GetID(), slot + i));
                State.m_SRVs[i] = (bindable[i] as IShaderResourceBindable).SRV;
            }

            for (int i = 0; i < bindable.Length; i++)
            {
                Context.VertexShader.SetShaderResource(slot + i, State.m_SRVs[i]);
                Context.PixelShader.SetShaderResource(slot + i, State.m_SRVs[i]);
                Context.ComputeShader.SetShaderResource(slot + i, State.m_SRVs[i]);
            }
            
        }

        internal void BindGBufferForRead(int slot, MyGBuffer gbuffer)
        {
            BindSRV(slot,
                gbuffer.DepthStencil.Depth,
                gbuffer.Get(MyGbufferSlot.GBuffer0),
                gbuffer.Get(MyGbufferSlot.GBuffer1),
                gbuffer.Get(MyGbufferSlot.GBuffer2),
                gbuffer.DepthStencil.Stencil
                );
        }

        internal void BindGBufferForReadSkipStencil(int slot, MyGBuffer gbuffer)
        {
            BindSRV(slot,
                gbuffer.DepthStencil.Depth,
                gbuffer.Get(MyGbufferSlot.GBuffer0),
                gbuffer.Get(MyGbufferSlot.GBuffer1),
                gbuffer.Get(MyGbufferSlot.GBuffer2));
        }

        internal void BindGBufferForWrite(MyGBuffer gbuffer)
        {
            BindDepthRT(
                gbuffer.Get(MyGbufferSlot.DepthStencil), DepthStencilAccess.ReadWrite,
                gbuffer.Get(MyGbufferSlot.GBuffer0),
                gbuffer.Get(MyGbufferSlot.GBuffer1),
                gbuffer.Get(MyGbufferSlot.GBuffer2));
        }

        internal void ClearDepthStencil(MyDepthStencil depthStencil, float depth, byte stencil)
        {
            Context.ClearDepthStencilView(depthStencil.m_DSV, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, depth, stencil);
        }

        internal void SetupScreenViewport()
        {
            Context.Rasterizer.SetViewport(0, 0, MyRender11.ViewportResolution.X, MyRender11.ViewportResolution.Y);
        }

        internal void Begin(MyQuery query)
        {
            Context.Begin(query.m_query);
        }

        internal void End(MyQuery query)
        {
            Context.End(query.m_query);
        }

        [Conditional(VRage.ProfilerShort.Symbol)]
        internal void BeginProfilingBlock(string tag)
        {
            var q = MyQueryFactory.CreateTimestampQuery();
            End(q);
            var info = new MyIssuedQuery(q, tag, MyIssuedQueryEnum.BlockStart);

            if (m_deferred)
            {
                ProfilingQueries.m_issued.Enqueue(info);
            }
            else
            {
                MyGpuProfiler.IC_Enqueue(info);
            }
        }

        [Conditional(VRage.ProfilerShort.Symbol)]
        internal void EndProfilingBlock()
        {
            var q = MyQueryFactory.CreateTimestampQuery();
            End(q);
            var info = new MyIssuedQuery(q, "", MyIssuedQueryEnum.BlockEnd);

            if (m_deferred)
            {
                ProfilingQueries.m_issued.Enqueue(info);
            }
            else
            {
                MyGpuProfiler.IC_Enqueue(info);
            }
        }

        internal void BindVertexData(ref MyVertexDataProxy_2 desc)
        {
            SetIB(desc.IB, desc.IndexFormat);
            SetVBs(desc.VB, desc.VertexStrides);
        }

        internal void BindShaders(ref MyShaderBundle desc)
        {
            SetIL(desc.InputLayout);
            SetVS(desc.VS);
            SetPS(desc.PS);
        }

        internal void BindShaders(MyMaterialShadersBundleId id)
        {
            SetIL(id.IL);
            SetVS(id.VS);
            SetPS(id.PS);
        }

        const int NO_VERSION = -1;

        internal void SetSRVs(ref MySrvTable desc)
        {
            //if (desc.BindFlag.HasFlag(MyBindFlag.BIND_VS))
            //{
            //    var val = new MyStageSrvBinding { Stage = MyShaderStage.VS, Slot = desc.StartSlot, Length = desc.SRVs.Length, Version = desc.Version };

            //    int f = State.m_srvBindings1.BinarySearch(val);
            //    var index = ~f;
            //    bool match = f >= 0; // if yes no need to bind
            //    bool collision = (index < State.m_srvBindings1.Count) && 
            //        State.m_srvBindings1[index].Stage == val.Stage &&
            //        (val.Slot + val.Length) >= State.m_srvBindings1[index].Slot; // if yes replace, if no add before, bind anyway

            //    if(!match && collision)
            //    {
            //        State.m_srvBindings1[index] = val;
            //        Context.VertexShader.SetShaderResources(desc.StartSlot, desc.SRVs);
            //    }
            //    else if(!match && !collision)
            //    {
            //        State.m_srvBindings1.Insert(index, val);
            //        Context.VertexShader.SetShaderResources(desc.StartSlot, desc.SRVs);
            //    }
            //}


            if ((desc.BindFlag & MyBindFlag.BIND_VS) > 0 &&
                State.m_srvTableBindings.Get(new MyStageSrvBinding { Stage = MyShaderStage.VS, Slot = desc.StartSlot }, NO_VERSION) != desc.Version)
            {
                State.m_srvTableBindings[new MyStageSrvBinding { Stage = MyShaderStage.VS, Slot = desc.StartSlot }] = desc.Version;
                Context.VertexShader.SetShaderResources(desc.StartSlot, desc.SRVs);
            }
            if ((desc.BindFlag & MyBindFlag.BIND_PS) > 0 &&
                State.m_srvTableBindings.Get(new MyStageSrvBinding { Stage = MyShaderStage.PS, Slot = desc.StartSlot }, NO_VERSION) != desc.Version)
            {
                State.m_srvTableBindings[new MyStageSrvBinding { Stage = MyShaderStage.PS, Slot = desc.StartSlot }] = desc.Version;
                Context.PixelShader.SetShaderResources(desc.StartSlot, desc.SRVs);
            }
        }

        internal unsafe void MoveConstants(ref MyConstantsPack desc)
        {
            if (desc.CB == null)
                return;
            if (State.m_constantsVersion.Get(desc.CB) != desc.Version)
            {
                State.m_constantsVersion[desc.CB] = desc.Version;
                
                var box = Context.MapSubresource((SharpDX.Direct3D11.Resource)desc.CB, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None);
                // TODO: try with aligned memory
                fixed(byte* ptr = desc.Data)
                {
                    MyMemory.CopyMemory(box.DataPointer, new IntPtr(ptr), (uint)desc.Data.Length);
                }
                Context.UnmapSubresource(desc.CB, 0);
            }
        }

        internal void SetConstants(ref MyConstantsPack desc, int slot)
        {
            if ((desc.BindFlag & MyBindFlag.BIND_VS) > 0 && State.m_constantBindings.Get(new MyStageBinding { Slot = slot, Stage = MyShaderStage.VS }) != desc.CB)
            {
                State.m_constantBindings[new MyStageBinding { Slot = slot, Stage = MyShaderStage.VS }] = desc.CB;
                Context.VertexShader.SetConstantBuffer(slot, desc.CB);
            }
            if ((desc.BindFlag & MyBindFlag.BIND_PS) > 0 && State.m_constantBindings.Get(new MyStageBinding { Slot = slot, Stage = MyShaderStage.PS }) != desc.CB)
            {
                State.m_constantBindings[new MyStageBinding { Slot = slot, Stage = MyShaderStage.PS }] = desc.CB;
                Context.PixelShader.SetConstantBuffer(slot, desc.CB);
            }
        }
    }

    enum MyDSBindEnum
    {
        None,
        ReadOnlyDepth,
        ReadOnlyStencil
    }

    interface IDepthStencilBindable
    {
        DepthStencilView DSV { get; }
    }

    interface IRenderTargetBindable
    {
        RenderTargetView RTV { get; }
    }

    interface IUnorderedAccessBindable
    {
        UnorderedAccessView UAV { get; }
    }

    interface IShaderResourceBindable
    {
        ShaderResourceView SRV { get; }
    }
}
