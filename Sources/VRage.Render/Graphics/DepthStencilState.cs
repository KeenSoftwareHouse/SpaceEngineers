using SharpDX.Direct3D9;
using System.Collections.Generic;
using System;

namespace VRageRender.Graphics
{
    // Summary:
    //     Contains depth-stencil state for the device.
    internal class DepthStencilState : MyRenderComponentBase, IDisposable
    {
        public static DepthStencilState Current { get; private set; }

        static DepthStencilState defaultState;

        public override int GetID()
        {
            return (int)MyRenderComponentID.DepthStencilState;
        }

        public static DepthStencilState Default
        {
            get
            {
                if (defaultState == null)
                {
                    defaultState = new DepthStencilState()
                    {
                        DepthBufferEnable = true,
                        DepthBufferWriteEnable = true,
                        DepthBufferFunction = Compare.LessEqual,
                        StencilEnable = false,
                        ReferenceStencil = 0,
                        CounterClockwiseStencilFunction = Compare.Always,
                        CounterClockwiseStencilDepthBufferFail = StencilOperation.Keep,
                        CounterClockwiseStencilFail = StencilOperation.Keep,
                        CounterClockwiseStencilPass = StencilOperation.Keep,
                        StencilFunction = Compare.Always,
                        StencilDepthBufferFail = StencilOperation.Keep,
                        StencilFail = StencilOperation.Keep,
                        StencilPass = StencilOperation.Keep,
                        TwoSidedStencilMode = false,
                        StencilMask = int.MaxValue,
                        StencilWriteMask = int.MaxValue
                    };
                }

                return defaultState;
            }
        }

        static DepthStencilState depthReadState;

        public static DepthStencilState DepthRead
        {
            get
            {
                if (depthReadState == null)
                {
                    depthReadState = new DepthStencilState()
                    {
                        DepthBufferEnable = true,
                        DepthBufferWriteEnable = false,
                        DepthBufferFunction = Compare.LessEqual,
                        StencilEnable = false,
                        ReferenceStencil = 0,
                        CounterClockwiseStencilFunction = Compare.Always,
                        CounterClockwiseStencilDepthBufferFail = StencilOperation.Keep,
                        CounterClockwiseStencilFail = StencilOperation.Keep,
                        CounterClockwiseStencilPass = StencilOperation.Keep,
                        StencilFunction = Compare.Always,
                        StencilDepthBufferFail = StencilOperation.Keep,
                        StencilFail = StencilOperation.Keep,
                        StencilPass = StencilOperation.Keep,
                        TwoSidedStencilMode = false,
                        StencilMask = int.MaxValue,
                        StencilWriteMask = int.MaxValue
                    };
                }

                return depthReadState;
            }
        }

        static DepthStencilState noneState;
        public static DepthStencilState None
        {
            get
            {
                if (noneState == null)
                {
                    noneState = new DepthStencilState()
                    {
                        DepthBufferEnable = false,
                        DepthBufferWriteEnable = false,
                        DepthBufferFunction = Compare.LessEqual,
                        StencilEnable = false,
                        ReferenceStencil = 0,
                        CounterClockwiseStencilFunction = Compare.Always,
                        CounterClockwiseStencilDepthBufferFail = StencilOperation.Keep,
                        CounterClockwiseStencilFail = StencilOperation.Keep,
                        CounterClockwiseStencilPass = StencilOperation.Keep,
                        StencilFunction = Compare.Always,
                        StencilDepthBufferFail = StencilOperation.Keep,
                        StencilFail = StencilOperation.Keep,
                        StencilPass = StencilOperation.Keep,
                        TwoSidedStencilMode = false,
                        StencilMask = int.MaxValue,
                        StencilWriteMask = int.MaxValue
                    };
                }

                return noneState;
            }
        }

        static DepthStencilState backGroundObjectsState;

        public static DepthStencilState BackgroundObjects
        {
            get
            {
                if (backGroundObjectsState == null)
                {
                    backGroundObjectsState = new DepthStencilState()
                    {
                        DepthBufferEnable = true,
                        DepthBufferWriteEnable = true,
                        DepthBufferFunction = Compare.LessEqual,
                        StencilEnable = true,
                        ReferenceStencil = 0,
                        CounterClockwiseStencilFunction = Compare.Equal,
                        CounterClockwiseStencilDepthBufferFail = StencilOperation.Keep,
                        CounterClockwiseStencilFail = StencilOperation.Keep,
                        CounterClockwiseStencilPass = StencilOperation.Keep,
                        StencilFunction = Compare.Equal,
                        StencilDepthBufferFail = StencilOperation.Keep,
                        StencilFail = StencilOperation.Keep,
                        StencilPass = StencilOperation.Keep,
                        TwoSidedStencilMode = false,
                        StencilMask = 3,
                        StencilWriteMask = int.MaxValue
                    };
                }

                return backGroundObjectsState;
            }
        }

        static DepthStencilState backgroundAtmosphereState;

        static Device m_device;
        static List<DepthStencilState> m_instances = new List<DepthStencilState>(16);
        StateBlock m_stateBlock;

        public override void LoadContent(Device device)
        {
            System.Diagnostics.Debug.Assert(m_device == null);
            m_device = device;
        }

        public override void UnloadContent()
        {
            m_device = null;

            foreach (var instance in m_instances)
            {
                instance.Dispose();
            }

            m_instances.Clear();
        }

        public void Dispose()
        {
            m_stateBlock.Dispose();
            m_stateBlock = null;
        }




        // Summary:
        //     Creates an instance of DepthStencilState with default values.
        public DepthStencilState()
        {
            DepthBufferEnable = true;
            DepthBufferWriteEnable = true;
            DepthBufferFunction = Compare.LessEqual;
            StencilEnable = false;
            ReferenceStencil = 0;
            CounterClockwiseStencilFunction = Compare.Always;
            CounterClockwiseStencilDepthBufferFail = StencilOperation.Keep;
            CounterClockwiseStencilFail = StencilOperation.Keep;
            CounterClockwiseStencilPass = StencilOperation.Keep;
            StencilFunction = Compare.Always;
            StencilDepthBufferFail = StencilOperation.Keep;
            StencilFail = StencilOperation.Keep;
            StencilPass = StencilOperation.Keep;
            TwoSidedStencilMode = false;
            StencilMask = int.MaxValue;
            StencilWriteMask = int.MaxValue;
        }

        // Summary:
        //     Gets or sets the stencil operation to perform if the stencil test passes
        //     and the depth-buffer test fails for a counterclockwise triangle. The default
        //     is StencilOperation.Keep.
        public StencilOperation CounterClockwiseStencilDepthBufferFail { get; set; }
        //
        // Summary:
        //     Gets or sets the stencil operation to perform if the stencil test fails for
        //     a counterclockwise triangle. The default is StencilOperation.Keep.
        public StencilOperation CounterClockwiseStencilFail { get; set; }
        //
        // Summary:
        //     Gets or sets the comparison function to use for counterclockwise stencil
        //     tests. The default is CompareFunction.Always.
        public Compare CounterClockwiseStencilFunction { get; set; }
        //
        // Summary:
        //     Gets or sets the stencil operation to perform if the stencil and depth-tests
        //     pass for a counterclockwise triangle. The default is StencilOperation.Keep.
        public StencilOperation CounterClockwiseStencilPass { get; set; }
        //
        // Summary:
        //     Enables or disables depth buffering. The default is true.
        public bool DepthBufferEnable { get; set; }
        //
        // Summary:
        //     Gets or sets the comparison function for the depth-buffer test. The default
        //     is CompareFunction.LessEqual
        public Compare DepthBufferFunction { get; set; }
        //
        // Summary:
        //     Enables or disables writing to the depth buffer. The default is true.
        public bool DepthBufferWriteEnable { get; set; }
        //
        // Summary:
        //     Specifies a reference value to use for the stencil test. The default is 0.
        public int ReferenceStencil { get; set; }
        //
        // Summary:
        //     Gets or sets the stencil operation to perform if the stencil test passes
        //     and the depth-test fails. The default is StencilOperation.Keep.
        public StencilOperation StencilDepthBufferFail { get; set; }
        //
        // Summary:
        //     Gets or sets stencil enabling. The default is false.
        public bool StencilEnable { get; set; }
        //
        // Summary:
        //     Gets or sets the stencil operation to perform if the stencil test fails.
        //     The default is StencilOperation.Keep.
        public StencilOperation StencilFail { get; set; }
        //
        // Summary:
        //     Gets or sets the comparison function for the stencil test. The default is
        //     CompareFunction.Always.
        public Compare StencilFunction { get; set; }
        //
        // Summary:
        //     Gets or sets the mask applied to the reference value and each stencil buffer
        //     entry to determine the significant bits for the stencil test. The default
        //     mask is Int32.MaxValue.
        public int StencilMask { get; set; }
        //
        // Summary:
        //     Gets or sets the stencil operation to perform if the stencil test passes.
        //     The default is StencilOperation.Keep.
        public StencilOperation StencilPass { get; set; }
        //
        // Summary:
        //     Gets or sets the write mask applied to values written into the stencil buffer.
        //     The default mask is Int32.MaxValue.
        public int StencilWriteMask { get; set; }
        //
        // Summary:
        //     Enables or disables two-sided stenciling. The default is false.
        public bool TwoSidedStencilMode { get; set; }


        public void Apply(bool cache = true)
        {
            if (m_stateBlock == null)
            {
                if (cache)
                {
                    m_device.BeginStateBlock();
                }
                m_device.SetRenderState(RenderState.CcwStencilZFail, CounterClockwiseStencilDepthBufferFail);
                m_device.SetRenderState(RenderState.CcwStencilFail, CounterClockwiseStencilFail);
                m_device.SetRenderState(RenderState.CcwStencilFunc, CounterClockwiseStencilFunction);
                m_device.SetRenderState(RenderState.CcwStencilPass, CounterClockwiseStencilPass);
                m_device.SetRenderState(RenderState.ZEnable, DepthBufferEnable);
                m_device.SetRenderState(RenderState.ZFunc, DepthBufferFunction);
                m_device.SetRenderState(RenderState.ZWriteEnable, DepthBufferWriteEnable);
                m_device.SetRenderState(RenderState.StencilRef, ReferenceStencil);
                m_device.SetRenderState(RenderState.StencilZFail, StencilDepthBufferFail);
                m_device.SetRenderState(RenderState.StencilEnable, StencilEnable);
                m_device.SetRenderState(RenderState.StencilFail, StencilFail);
                m_device.SetRenderState(RenderState.StencilFunc, StencilFunction);
                m_device.SetRenderState(RenderState.StencilMask, StencilMask);
                m_device.SetRenderState(RenderState.StencilPass, StencilPass);
                m_device.SetRenderState(RenderState.StencilWriteMask, StencilWriteMask);
                m_device.SetRenderState(RenderState.TwoSidedStencilMode, TwoSidedStencilMode);
                if (cache)
                {
                    m_stateBlock = m_device.EndStateBlock();
                    m_instances.Add(this);
                }
            }

            if (cache)
            {
                m_stateBlock.Apply();
            }

            Current = this;
        }
    }
}
