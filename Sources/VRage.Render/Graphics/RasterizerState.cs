using SharpDX.Direct3D9;
using System.Collections.Generic;
using System;

namespace VRageRender.Graphics
{
    // Summary:
    //     Contains rasterizer state, which determines how to convert vector data (shapes)
    //     into raster data (pixels).
    internal class RasterizerState : MyRenderComponentBase, IDisposable
    {
        public override int GetID()
        {
            return (int)MyRenderComponentID.RasterizerState;
        }

        public static RasterizerState Current { get; private set; }

        // Summary:
        //     A built-in state object with settings for culling primitives with clockwise
        //     winding order.
        public static readonly RasterizerState CullClockwise;
        //
        // Summary:
        //     A built-in state object with settings for culling primitives with counter-clockwise
        //     winding order.
        public static readonly RasterizerState CullCounterClockwise;
        //
        // Summary:
        //     A built-in state object with settings for not culling any primitives.
        public static readonly RasterizerState CullNone;

        static Device m_device;
        static List<RasterizerState> m_instances = new List<RasterizerState>(16);
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


        static RasterizerState()
        {
            CullClockwise = new RasterizerState()
            {
                CullMode = Cull.Clockwise
            };
            CullCounterClockwise = new RasterizerState()
            {
                CullMode = Cull.Counterclockwise
            };
            CullNone = new RasterizerState()
            {
                CullMode = Cull.None
            };
        }

        // Summary:
        //     Initializes a new instance of the rasterizer class.
        public RasterizerState()
        {
            ScissorTestEnable = false;
            CullMode = Cull.None;
            FillMode = SharpDX.Direct3D9.FillMode.Solid;
            MultiSampleAntiAlias = false;
            DepthBias = 0;
            SlopeScaleDepthBias = 0;
        }

        // Summary:
        //     Specifies the conditions for culling or removing triangles. The default value
        //     is CullMode.CounterClockwise.
        public Cull CullMode { get; set; }
        //
        // Summary:
        //     Sets or retrieves the depth bias for polygons, which is the amount of bias
        //     to apply to the depth of a primitive to alleviate depth testing problems
        //     for primitives of similar depth. The default value is 0.
        public float DepthBias { get; set; }
        //
        // Summary:
        //     The fill mode, which defines how a triangle is filled during rendering. The
        //     default is FillMode.Solid.
        public FillMode FillMode { get; set; }
        //
        // Summary:
        //     Enables or disables multisample antialiasing. The default is true.
        public bool MultiSampleAntiAlias { get; set; }
        //
        // Summary:
        //     Enables or disables scissor testing. The default is false.
        public bool ScissorTestEnable { get; set; }
        //
        // Summary:
        //     Gets or sets a bias value that takes into account the slope of a polygon.
        //     This bias value is applied to coplanar primitives to reduce aliasing and
        //     other rendering artifacts caused by z-fighting. The default is 0.
        public float SlopeScaleDepthBias { get; set; }

        public void Apply()
        {
            if (m_stateBlock == null)
            {
                m_device.BeginStateBlock();
                m_device.SetRenderState(RenderState.CullMode, CullMode);
                m_device.SetRenderState(RenderState.DepthBias, DepthBias);
                m_device.SetRenderState(RenderState.FillMode, FillMode);
                m_device.SetRenderState(RenderState.MultisampleAntialias, MultiSampleAntiAlias);
                m_device.SetRenderState(RenderState.ScissorTestEnable, ScissorTestEnable);
                m_device.SetRenderState(RenderState.SlopeScaleDepthBias, SlopeScaleDepthBias);
                m_stateBlock = m_device.EndStateBlock();
                m_instances.Add(this);
            }

            m_stateBlock.Apply();

            Current = this;
        }
    }
}
