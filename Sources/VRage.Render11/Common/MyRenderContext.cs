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
using VRage;
using VRage.Library.Collections;
using VRage.Utils;

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

        #region Equals
        public class MyBindingComparerType : IEqualityComparer<MyBinding>
        {
            public bool Equals(MyBinding left, MyBinding right)
            {
                return left.WriteView == right.WriteView &&
                        left.UavSlot == right.UavSlot &&
                        left.DsvRead == right.DsvRead;
            }

            public int GetHashCode(MyBinding binding)
            {
                return (int)(binding.WriteView) << 27 | binding.UavSlot | (binding.DsvRead ? 1 : 0) << 31;
            }
        }
        public static MyBindingComparerType Comparer = new MyBindingComparerType();
        #endregion
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

        public int ShadowDrawIndexed;
        public int ShadowDrawIndexedInstanced;

        public int BillboardDrawIndexed;

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

            ShadowDrawIndexed = 0;
            ShadowDrawIndexedInstanced = 0;

            BillboardDrawIndexed = 0;

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

            ShadowDrawIndexed           += other.ShadowDrawIndexed;
            ShadowDrawIndexedInstanced  += other.ShadowDrawIndexedInstanced;

            BillboardDrawIndexed        += other.BillboardDrawIndexed;

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

        #region Equals
        public class MyStageBindingComparerType : IEqualityComparer<MyStageBinding>
        {
            public bool Equals(MyStageBinding left, MyStageBinding right)
            {
                return left.Slot == right.Slot && left.Stage == right.Stage;
            }

            public int GetHashCode(MyStageBinding stageBinding)
            {
                return stageBinding.Slot << 4 + (int)stageBinding.Stage;
            }
        }
        public static MyStageBindingComparerType Comparer = new MyStageBindingComparerType();
        #endregion
    }

    struct MyStageSrvBinding
    {
        internal int Slot;
        internal MyShaderStage Stage;
        //internal int Length;
        //internal int Version;

        #region Equals
        public class MyStageSrvBindingComparerType : IEqualityComparer<MyStageSrvBinding>
        {
            public bool Equals(MyStageSrvBinding left, MyStageSrvBinding right)
            {
                return left.Slot == right.Slot && left.Stage == right.Stage;
            }

            public int GetHashCode(MyStageSrvBinding stageBinding)
            {
                return stageBinding.Slot << 4 + (int)stageBinding.Stage;
            }
        }
        public static MyStageSrvBindingComparerType Comparer = new MyStageSrvBindingComparerType();
        #endregion
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
        internal Dictionary<int, MyBinding> m_bindings;
        internal MyListDictionary<int, int> m_slotToBindingKeys;
        internal MyListDictionary<int, int> m_srvBindings;

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

                m_bindings = new Dictionary<int, MyBinding>();
                m_slotToBindingKeys = new MyListDictionary<int, int>();
                m_srvBindings = new MyListDictionary<int, int>();

                m_RTVs = new RenderTargetView[8];
                m_SRVs = new ShaderResourceView[8];

                m_constantsVersion = new Dictionary<Buffer, int>();
                m_constantBindings = new Dictionary<MyStageBinding, Buffer>(MyStageBinding.Comparer);
                m_srvTableBindings = new Dictionary<MyStageSrvBinding, int>(MyStageSrvBinding.Comparer);
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
            m_slotToBindingKeys.Clear();
            m_srvBindings.Clear();

            m_constantsVersion.Clear();
            m_constantBindings.Clear();
            m_srvTableBindings.Clear();
            m_srvBindings1.SetSize(0);
        }
    }

    class MyRenderContextAnnotation
    {
        SharpDX.Direct3D11.UserDefinedAnnotation m_annotation;

        [Conditional("DEBUG")]
        public void Init(DeviceContext context)
        {
            m_annotation = context.QueryInterface<SharpDX.Direct3D11.UserDefinedAnnotation>();
        }

        [Conditional("DEBUG")]
        public void BeginEvent(string tag)
        {
            m_annotation.BeginEvent(tag);
        }

        [Conditional("DEBUG")]
        public void EndEvent()
        {
            m_annotation.EndEvent();
        }

        [Conditional("DEBUG")]
        public void SetMarker(string tag)
        {
            m_annotation.SetMarker(tag);
        }
    }

    class MyRenderContext
    {
        MyRenderContextAnnotation m_annotation = new MyRenderContextAnnotation();

        static readonly int[] ZeroOffsets = { 0, 0, 0, 0, 0, 0, 0, 0 };

        internal MyContextState State;
        internal DeviceContext  DeviceContext;
        internal MyRCStats Stats;
        internal MyFrameProfilingContext ProfilingQueries = new MyFrameProfilingContext();
        bool m_deferred;
        bool m_finished;
        bool m_joined;
        internal CommandList m_commandList;

        private readonly List<int> m_tmpBindingClearList = new List<int>();

        internal bool Joined { get { return m_joined; } }
        internal bool OkToRelease { get { return m_finished == true && m_joined == true && ProfilingQueries.m_issued.Count == 0; } }

        internal static MyRenderContext Immediate = new MyRenderContext(MyRender11.DeviceContext, false);

        public MyRenderContext()
        {
            m_deferred = true;
        }

        internal MyRenderContext(DeviceContext context, bool isDeferred)
        {
            m_deferred = isDeferred;
            DeviceContext = context;
            LazyInitialize();
        }

        internal void LazyInitialize()
        {
            if(DeviceContext == null)
            {
                DeviceContext = new DeviceContext(MyRender11.Device);
                //MyRenderContextPool.m_intialized.Enqueue(Context);
            }

            State.Clear();
            Stats.Clear();
            m_finished = false;
            m_joined = false;
            m_commandList = null;

            m_annotation.Init(DeviceContext);
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
                m_commandList = DeviceContext.FinishCommandList(false);
                MyRender11.ProcessDebugOutput();
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

        internal bool UpdateVB(int slot, Buffer vb, int stride)
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
                DeviceContext.InputAssembler.SetVertexBuffers(slot, new VertexBufferBinding(vb, stride, 0));
                Stats.SetVB++;
                MyRender11.ProcessDebugOutput();
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
                    DeviceContext.InputAssembler.SetVertexBuffers(slot, new VertexBufferBinding(State.m_VBs[slot], State.m_strides[slot], 0));
                    Stats.SetVB++;
                }
                else if (change)
                { 
                    DeviceContext.InputAssembler.SetVertexBuffers(0, State.m_VBs, State.m_strides, ZeroOffsets);
                    Stats.SetVB++;
                }
                MyRender11.ProcessDebugOutput();
            }
        }

        internal void SetIB(Buffer ib, Format format)
        {
            if (State.m_IB != ib)
            {
                State.m_IB = ib;
                DeviceContext.InputAssembler.SetIndexBuffer(ib, format, 0);
                Stats.SetIB++;
                MyRender11.ProcessDebugOutput();
            }
        }

        internal void CSSetCB(int slot, Buffer cb)
        {
            DeviceContext.ComputeShader.SetConstantBuffer(slot, cb);
            MyRender11.ProcessDebugOutput();
        }

        internal void SetCB(int slot, Buffer cb)
        {
            if (State.m_CBs[slot] != cb)
            {
                State.m_CBs[slot] = cb;
                DeviceContext.VertexShader.SetConstantBuffer(slot, cb);
                Stats.SetCB++;
                DeviceContext.GeometryShader.SetConstantBuffer(slot, cb);
                Stats.SetCB++;
                DeviceContext.PixelShader.SetConstantBuffer(slot, cb);
                Stats.SetCB++;
                MyRender11.ProcessDebugOutput();
            }
        }

        internal void SetPS(PixelShader ps)
        {
            if (State.m_ps != ps)
            {
                State.m_ps = ps;
                DeviceContext.PixelShader.Set(ps);
                Stats.SetPS++;
                MyRender11.ProcessDebugOutput();
            }
        }

        internal void SetVS(VertexShader vs)
        {
            if (State.m_vs != vs)
            {
                State.m_vs = vs;
                DeviceContext.VertexShader.Set(vs);
                Stats.SetVS++;
                MyRender11.ProcessDebugOutput();
            }
        }

        internal void SetCS(ComputeShader cs)
        {
            DeviceContext.ComputeShader.Set(cs);
            MyRender11.ProcessDebugOutput();
        }

        internal void SetGS(GeometryShader gs)
        {
            if (State.m_gs != gs)
            {
                State.m_gs = gs;
                DeviceContext.GeometryShader.Set(gs);
                Stats.SetGS++;
                MyRender11.ProcessDebugOutput();
            }
        }

        internal void SetIL(InputLayout il)
        {
            if (State.m_inputLayout != il)
            {
                State.m_inputLayout = il;
                DeviceContext.InputAssembler.InputLayout = il;
                Stats.SetIL++;
                MyRender11.ProcessDebugOutput();
            }
        }

        internal void SetRS(RasterizerState rs)
        {
            if (State.m_RS != rs)
            {
                State.m_RS = rs;
                DeviceContext.Rasterizer.State = rs;
                Stats.SetRasterizerState++;
                MyRender11.ProcessDebugOutput();
            }
        }

        internal void SetBS(BlendState bs, Color4? blendFactor = null)
        {
            if (State.m_BS != bs || blendFactor != null)
            {
                State.m_BS = bs;
                DeviceContext.OutputMerger.SetBlendState(bs, blendFactor);
                Stats.SetBlendState++;
                MyRender11.ProcessDebugOutput();
            }
        }

        internal void SetDS(DepthStencilState ds, int stencilRef = 0)
        {
            if ((State.m_DS != ds) || (State.m_stencilRef != stencilRef))
            {
                State.m_DS = ds;
                State.m_stencilRef = stencilRef;
                DeviceContext.OutputMerger.SetDepthStencilState(ds, stencilRef);
                MyRender11.ProcessDebugOutput();
            }
        }

        internal void BindRawSRV(int slot, IShaderResourceBindable srv)
        {
            ShaderResourceView sharpSRV = srv != null ? srv.SRV : null;
            DeviceContext.VertexShader.SetShaderResource(slot, sharpSRV);
            DeviceContext.PixelShader.SetShaderResource(slot, sharpSRV);

            Stats.BindShaderResources++;
            Stats.BindShaderResources++;
            MyRender11.ProcessDebugOutput();
        }

        internal void VSBindRawSRV(int slot, IShaderResourceBindable srv)
        {
            ShaderResourceView sharpSRV = srv != null ? srv.SRV : null;
            DeviceContext.VertexShader.SetShaderResource(slot, sharpSRV);

            Stats.BindShaderResources++;
            MyRender11.ProcessDebugOutput();
        }

        internal void CSBindRawSRV(int slot, IShaderResourceBindable srv)
        {
            ShaderResourceView sharpSRV = srv != null ? srv.SRV : null;
            DeviceContext.ComputeShader.SetShaderResource(slot, sharpSRV);

            Stats.BindShaderResources++;
            MyRender11.ProcessDebugOutput();
        }

        internal void PSBindRawSRV(int slot, IShaderResourceBindable srv)
        {
            ShaderResourceView sharpSRV = srv != null ? srv.SRV : null;
            DeviceContext.PixelShader.SetShaderResources(slot, sharpSRV);

            Stats.BindShaderResources++;
            MyRender11.ProcessDebugOutput();
        }

        void UnbindSRVRead(int resId)
        {
            List<int> resourceList = State.m_srvBindings.GetList(resId);
            if (resourceList == null)
                return;

            foreach (var resourceId in resourceList)
            {
                DeviceContext.VertexShader.SetShaderResource(resourceId, null);
                DeviceContext.PixelShader.SetShaderResource(resourceId, null);
                DeviceContext.ComputeShader.SetShaderResource(resourceId, null);
            }
            resourceList.Clear();
            MyRender11.ProcessDebugOutput();
        }

        class KeyListComparer : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                return y - x;
            }

            internal static KeyListComparer Comparer = new KeyListComparer();
        }

        UnorderedAccessView[] m_tmpBuffer = new UnorderedAccessView[1];
        internal void BindUAV(int slot, MyBindableResource UAV, int initialCount = -1)
        {
            if (UAV != null)
            {
                var ua = UAV as IUnorderedAccessBindable;
                Debug.Assert(ua != null);

                UnbindSRVRead(UAV.GetID());
                //UnbindDSVReadOnly(UAVs[i].ResId); necessary?

                List<int> keyList = State.m_slotToBindingKeys.GetList(slot);
                if (keyList != null)
                {
                    keyList.Sort(KeyListComparer.Comparer);
                    State.m_bindings.Remove(keyList[0]);
                    State.m_slotToBindingKeys.Remove(slot);
                }

                var binding = new MyBinding(MyWriteBindingEnum.UAV, slot);
                State.m_bindings[UAV.GetID()] = binding;
                State.m_slotToBindingKeys.Add(slot, UAV.GetID());
                ComputeShaderId.TmpUav[0] = ua.UAV;
            }

            ComputeShaderId.TmpCount[0] = initialCount;
            DeviceContext.ComputeShader.SetUnorderedAccessViews(slot, ComputeShaderId.TmpUav, ComputeShaderId.TmpCount);
            ComputeShaderId.TmpCount[0] = -1;
            MyRender11.ProcessDebugOutput();
        }

        internal void BindDepthRT(MyBindableResource depthStencil, DepthStencilAccess dsAccess, MyBindableResource RT)
        {
            m_tmpBinds1[0] = RT;

            BindDepthRTInternal(depthStencil, dsAccess, m_tmpBinds1);

            Array.Clear(m_tmpBinds1, 0, m_tmpBinds1.Length);
        }

        internal void BindDepthRT(MyBindableResource depthStencil, DepthStencilAccess dsAccess, MyBindableResource bindable0, MyBindableResource bindable1)
        {
            m_tmpBinds2[0] = bindable0;
            m_tmpBinds2[1] = bindable1;

            BindDepthRTInternal(depthStencil, dsAccess, m_tmpBinds2);

            Array.Clear(m_tmpBinds2, 0, m_tmpBinds2.Length);
        }

        internal void BindDepthRT(MyBindableResource depthStencil, DepthStencilAccess dsAccess, MyBindableResource bindable0, MyBindableResource bindable1, MyBindableResource bindable2)
        {
            m_tmpBinds3[0] = bindable0;
            m_tmpBinds3[1] = bindable1;
            m_tmpBinds3[2] = bindable2;

            BindDepthRTInternal(depthStencil, dsAccess, m_tmpBinds3);

            Array.Clear(m_tmpBinds3, 0, m_tmpBinds3.Length);
        }

        private void BindDepthRTInternal(MyBindableResource depthStencil, DepthStencilAccess dsAccess, params MyBindableResource[] RTs)
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
            DeviceContext.OutputMerger.SetTargets(
                dsv,
                State.m_RTVs);

            ClearDsvRtvWriteBindings();

            if (depthStencil != null)
            {
                var ds = depthStencil as MyDepthStencil;
                if (dsAccess == DepthStencilAccess.ReadWrite)
                {
                    var binding = new MyBinding(MyWriteBindingEnum.DSV);
                    State.m_bindings[ds.Depth.GetID()] = binding;
                    State.m_slotToBindingKeys.Add(binding.UavSlot, ds.Depth.GetID());
                    State.m_bindings[ds.Stencil.GetID()] = binding;
                    State.m_slotToBindingKeys.Add(binding.UavSlot, ds.Stencil.GetID());
                }
                else if (dsAccess == DepthStencilAccess.DepthReadOnly)
                {
                    var depthBinding = new MyBinding(true);
                    State.m_bindings[ds.Depth.GetID()] = depthBinding;
                    State.m_slotToBindingKeys.Add(depthBinding.UavSlot, ds.Depth.GetID());

                    var stencilBinding = new MyBinding(MyWriteBindingEnum.DSV);
                    State.m_bindings[ds.Stencil.GetID()] = stencilBinding;
                    State.m_slotToBindingKeys.Add(stencilBinding.UavSlot, ds.Stencil.GetID());
                }
                else if (dsAccess == DepthStencilAccess.StencilReadOnly)
                {
                    var depthBinding = new MyBinding(MyWriteBindingEnum.DSV);
                    State.m_bindings[ds.Depth.GetID()] = depthBinding;
                    State.m_slotToBindingKeys.Add(depthBinding.UavSlot, ds.Depth.GetID());

                    var stencilBinding = new MyBinding(true);
                    State.m_bindings[ds.Stencil.GetID()] = stencilBinding;
                    State.m_slotToBindingKeys.Add(stencilBinding.UavSlot, ds.Stencil.GetID());
                }
                else if (dsAccess == DepthStencilAccess.ReadOnly)
                {
                    var binding = new MyBinding(true);
                    State.m_bindings[ds.Depth.GetID()] = binding;
                    State.m_bindings[ds.Stencil.GetID()] = binding;

                    State.m_slotToBindingKeys.Add(binding.UavSlot, ds.Depth.GetID());
                    State.m_slotToBindingKeys.Add(binding.UavSlot, ds.Stencil.GetID());
                }
            }
            if (RTs != null)
            {
                for (int i = 0; i < RTs.Length; i++)
                {
                    if (RTs[i] != null)
                    {
                        var binding = new MyBinding(MyWriteBindingEnum.RTV);
                        State.m_bindings[RTs[i].GetID()] = binding;
                        State.m_slotToBindingKeys.Add(binding.UavSlot, RTs[i].GetID());
                    }
                }
            }
        }

        void ClearDsvRtvWriteBindings()
        {
            foreach(var pair in State.m_bindings)
            {
                var view = pair.Value.WriteView;
                if (view == MyWriteBindingEnum.RTV || view == MyWriteBindingEnum.DSV)
                {
                    m_tmpBindingClearList.Add(pair.Key);

                    List<int> keyList = State.m_slotToBindingKeys.GetList(pair.Value.UavSlot);
                    if (keyList != null)
                    {
                        keyList.Remove(pair.Key);
                        if (keyList.Count == 0)
                            State.m_slotToBindingKeys.Remove(pair.Value.UavSlot);
                    }
                }
            }
            foreach (int key in m_tmpBindingClearList)
            {
                State.m_bindings.Remove(key);
            }
            m_tmpBindingClearList.SetSize(0);
        }

        internal void BindSRV(int slot, MyBindableResource bindable)
        {
            Array.Clear(State.m_SRVs, 0, State.m_SRVs.Length);
            Debug.Assert(bindable as IShaderResourceBindable != null);
            MyBinding binding;
            State.m_bindings.TryGetValue(bindable.GetID(), out binding);
            if (binding.WriteView == MyWriteBindingEnum.RTV || binding.WriteView == MyWriteBindingEnum.DSV)
            {
                DeviceContext.OutputMerger.ResetTargets();
                ClearDsvRtvWriteBindings();
            }
            else if (binding.WriteView == MyWriteBindingEnum.UAV)
            {
                ComputeShaderId.TmpUav[0] = null;
                DeviceContext.ComputeShader.SetUnorderedAccessViews(binding.UavSlot, ComputeShaderId.TmpUav, ComputeShaderId.TmpCount);
                State.m_bindings.Remove(bindable.GetID());
                List<int> keyList = State.m_slotToBindingKeys.GetList(slot);
                if(keyList != null)
                {
                    keyList.Remove(bindable.GetID());
                    if (keyList.Count == 0)
                        State.m_slotToBindingKeys.Remove(slot);
                }
            }

            State.m_srvBindings.Add(bindable.GetID(), slot);
            var bindableShaderResource = bindable as IShaderResourceBindable;
            Debug.Assert(bindableShaderResource != null);
            State.m_SRVs[0] = bindableShaderResource.SRV;

            DeviceContext.VertexShader.SetShaderResource(slot, State.m_SRVs[0]);
            DeviceContext.PixelShader.SetShaderResource(slot, State.m_SRVs[0]);
            DeviceContext.ComputeShader.SetShaderResource(slot, State.m_SRVs[0]);

            MyRender11.ProcessDebugOutput();

        }

        MyBindableResource[] m_tmpBinds1 = new MyBindableResource[1];
        MyBindableResource[] m_tmpBinds2 = new MyBindableResource[2];
        MyBindableResource[] m_tmpBinds3 = new MyBindableResource[3];
        MyBindableResource[] m_tmpBinds4 = new MyBindableResource[4];
        MyBindableResource[] m_tmpBinds5 = new MyBindableResource[5];

        internal void BindSRVs(int slot, MyBindableResource bindable0, MyBindableResource bindable1)
        {
            m_tmpBinds2[0] = bindable0;
            m_tmpBinds2[1] = bindable1;
            BindSRVsInternal(slot, m_tmpBinds2);

            Array.Clear(m_tmpBinds2, 0, m_tmpBinds2.Length);
        }

        internal void BindSRVs(int slot, MyBindableResource bindable0, MyBindableResource bindable1, MyBindableResource bindable2)
        {
            m_tmpBinds3[0] = bindable0;
            m_tmpBinds3[1] = bindable1;
            m_tmpBinds3[2] = bindable2;
            BindSRVsInternal(slot, m_tmpBinds3);

            Array.Clear(m_tmpBinds3, 0, m_tmpBinds3.Length);
        }

        private void BindSRVsInternal(int slot, MyBindableResource[] bindable)
        {
            Array.Clear(State.m_SRVs, 0, State.m_SRVs.Length);
            for (int i = 0; i < bindable.Length; i++)
            {
                Debug.Assert(bindable[i] as IShaderResourceBindable != null);
                MyBinding binding;
                State.m_bindings.TryGetValue(bindable[i].GetID(), out binding);
                if (binding.WriteView == MyWriteBindingEnum.RTV || binding.WriteView == MyWriteBindingEnum.DSV)
                {
                    DeviceContext.OutputMerger.ResetTargets();
                    ClearDsvRtvWriteBindings();
                }
                else if (binding.WriteView == MyWriteBindingEnum.UAV)
                {
                    ComputeShaderId.TmpUav[0] = null;
                    DeviceContext.ComputeShader.SetUnorderedAccessViews(binding.UavSlot, ComputeShaderId.TmpUav, ComputeShaderId.TmpCount);
                    State.m_bindings.Remove(bindable[i].GetID());
                    List<int> keyList = State.m_slotToBindingKeys.GetList(slot);
                    if (keyList != null)
                    {
                        keyList.Remove(bindable[i].GetID());
                        if (keyList.Count == 0)
                            State.m_slotToBindingKeys.Remove(slot);
                    }
                }

                State.m_srvBindings.Add(bindable[i].GetID(), slot + i);
                State.m_SRVs[i] = (bindable[i] as IShaderResourceBindable).SRV;
            }

            for (int i = 0; i < bindable.Length; i++)
            {
                DeviceContext.VertexShader.SetShaderResource(slot + i, State.m_SRVs[i]);
                DeviceContext.PixelShader.SetShaderResource(slot + i, State.m_SRVs[i]);
                DeviceContext.ComputeShader.SetShaderResource(slot + i, State.m_SRVs[i]);
            }
            MyRender11.ProcessDebugOutput();
        }

        internal void BindGBufferForRead(int slot, MyGBuffer gbuffer)
        {
            m_tmpBinds5[0] = gbuffer.DepthStencil.Depth;
            m_tmpBinds5[1] = gbuffer.Get(MyGbufferSlot.GBuffer0);
            m_tmpBinds5[2] = gbuffer.Get(MyGbufferSlot.GBuffer1);
            m_tmpBinds5[3] = gbuffer.Get(MyGbufferSlot.GBuffer2);
            m_tmpBinds5[4] = gbuffer.DepthStencil.Stencil;
            BindSRVsInternal(slot, m_tmpBinds5);

            Array.Clear(m_tmpBinds5, 0, m_tmpBinds5.Length);
        }

        internal void BindGBufferForReadSkipStencil(int slot, MyGBuffer gbuffer)
        {
            m_tmpBinds4[0] = gbuffer.DepthStencil.Depth;
            m_tmpBinds4[1] = gbuffer.Get(MyGbufferSlot.GBuffer0);
            m_tmpBinds4[2] = gbuffer.Get(MyGbufferSlot.GBuffer1);
            m_tmpBinds4[3] = gbuffer.Get(MyGbufferSlot.GBuffer2);
            BindSRVsInternal(slot, m_tmpBinds4);

            Array.Clear(m_tmpBinds4, 0, m_tmpBinds4.Length);
        }

        internal void BindGBufferForWrite(MyGBuffer gbuffer, DepthStencilAccess depthStencilFlags = DepthStencilAccess.ReadWrite)
        {
            m_tmpBinds3[0] = gbuffer.Get(MyGbufferSlot.GBuffer0);
            m_tmpBinds3[1] = gbuffer.Get(MyGbufferSlot.GBuffer1);
            m_tmpBinds3[2] = gbuffer.Get(MyGbufferSlot.GBuffer2);
            BindDepthRTInternal(gbuffer.Get(MyGbufferSlot.DepthStencil), depthStencilFlags, m_tmpBinds3);

            Array.Clear(m_tmpBinds3, 0, m_tmpBinds3.Length);
        }

        /// <summary>
        /// Bind the provided slot to SV_Target0. Useful for dual-source blending
        /// </summary>
        internal void BindGBufferForWrite(MyGBuffer gbuffer, MyGbufferSlot slot, DepthStencilAccess depthStencilFlags = DepthStencilAccess.ReadWrite)
        {
            m_tmpBinds1[0] = gbuffer.Get(slot);
            BindDepthRTInternal(gbuffer.Get(MyGbufferSlot.DepthStencil), depthStencilFlags, m_tmpBinds1);

            Array.Clear(m_tmpBinds1, 0, m_tmpBinds1.Length);
        }

        internal void ClearDepthStencil(MyDepthStencil depthStencil, float depth, byte stencil)
        {
            DeviceContext.ClearDepthStencilView(depthStencil.m_DSV, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, depth, stencil);
            MyRender11.ProcessDebugOutput();
        }

        internal void SetupScreenViewport()
        {
            DeviceContext.Rasterizer.SetViewport(0, 0, MyRender11.ViewportResolution.X, MyRender11.ViewportResolution.Y);
            MyRender11.ProcessDebugOutput();
        }

        internal void Begin(MyQuery query)
        {
            DeviceContext.Begin(query.m_query);
            MyRender11.ProcessDebugOutput();
        }

        internal void End(MyQuery query)
        {
            DeviceContext.End(query.m_query);
            MyRender11.ProcessDebugOutput();
        }

        //IMPORTANT: If you change anything here, also change it in BeginProfilingBlockAlways
        [Conditional(VRage.ProfilerShort.PerformanceProfilingSymbol)]
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
            // this tag will be visible in NSight because of this call:
            m_annotation.BeginEvent(tag);
        }

        //IMPORTANT: If you change anything here, also change it in EndProfilingBlockAlways
        [Conditional(VRage.ProfilerShort.PerformanceProfilingSymbol)]
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
            // this tag will be visible in NSight because of this call:
            m_annotation.EndEvent();
        }

        /// <summary>
        /// BeginProfilingBlock that works even when PerformanceProfilingSymbol is false
        /// </summary>
        internal void BeginProfilingBlockAlways(string tag)
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
            // this tag will be visible in NSight because of this call:
            m_annotation.BeginEvent(tag);
        }

        /// <summary>
        /// EndProfilingBlock that works even when PerformanceProfilingSymbol is false
        /// </summary>
        internal void EndProfilingBlockAlways()
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
            // this tag will be visible in NSight because of this call:
            m_annotation.EndEvent();
        }

        [Conditional(VRage.ProfilerShort.PerformanceProfilingSymbol)]
        internal void SetProfilingMarker(string tag)
        {
            // this tag will be visible in NSight because of this call:
            m_annotation.SetMarker(tag);
        }

        internal static void OnDeviceReset()
        {
            Immediate.DeviceContext = MyRender11.DeviceContext;
        }

        internal void BindVertexData(ref MyVertexDataProxy_2 desc)
        {
            SetIB(desc.IB, desc.IndexFormat);
            SetVBs(desc.VB, desc.VertexStrides);
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
                for (int i = 0; i < desc.SRVs.Length; i++)
                    DeviceContext.VertexShader.SetShaderResource(desc.StartSlot + i, desc.SRVs[i].SRV);
            }
            if ((desc.BindFlag & MyBindFlag.BIND_PS) > 0 &&
                State.m_srvTableBindings.Get(new MyStageSrvBinding { Stage = MyShaderStage.PS, Slot = desc.StartSlot }, NO_VERSION) != desc.Version)
            {
                State.m_srvTableBindings[new MyStageSrvBinding { Stage = MyShaderStage.PS, Slot = desc.StartSlot }] = desc.Version;
                for (int i = 0; i < desc.SRVs.Length; i++)
                    DeviceContext.PixelShader.SetShaderResource(desc.StartSlot + i, desc.SRVs[i].SRV);
            }
            MyRender11.ProcessDebugOutput();
        }

        internal unsafe void MoveConstants(ref MyConstantsPack desc)
        {
            if (desc.CB == null)
                return;
            if (State.m_constantsVersion.Get(desc.CB) != desc.Version)
            {
                State.m_constantsVersion[desc.CB] = desc.Version;

                var mapping = MyMapping.MapDiscard(DeviceContext, desc.CB);
                mapping.WriteAndPosition(desc.Data, 0, desc.Data.Length);
                mapping.Unmap();
            }
            MyRender11.ProcessDebugOutput();
        }

        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
        [System.Security.SecurityCriticalAttribute]
        internal void SetConstants(ref MyConstantsPack desc, int slot)
        {
            MyStageBinding key;
            key.Stage = MyShaderStage.VS;
         
            try
            {
                if ((desc.BindFlag & MyBindFlag.BIND_VS) > 0 && State.m_constantBindings.Get(new MyStageBinding { Slot = slot, Stage = MyShaderStage.VS }) != desc.CB)
                {
                    key = new MyStageBinding {Slot = slot, Stage = MyShaderStage.VS};
                    State.m_constantBindings[key] = desc.CB;
                    DeviceContext.VertexShader.SetConstantBuffer(slot, desc.CB);
                }
                if ((desc.BindFlag & MyBindFlag.BIND_PS) > 0 && State.m_constantBindings.Get(new MyStageBinding { Slot = slot, Stage = MyShaderStage.PS }) != desc.CB)
                {
                    key = new MyStageBinding {Slot = slot, Stage = MyShaderStage.PS};
                    State.m_constantBindings[key] = desc.CB;
                    DeviceContext.PixelShader.SetConstantBuffer(slot, desc.CB);
                }
                MyRender11.ProcessDebugOutput();
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine(ex);
                MyLog.Default.WriteLine(string.Format("Some additional info: slot {0}, Stage {1}, desc {2}", slot, key.Stage, desc));
                MyLog.Default.WriteLine("m_constantBindings.Count: " + State.m_constantBindings.Count);
                MyLog.Default.WriteLine("m_constantBindings: " + State.m_constantBindings);
                throw;
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
