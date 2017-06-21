using SharpDX.Direct3D11;
using VRage.Render11.Resources.Internal;
using VRageRender;

namespace VRage.Render11.Resources
{
    internal interface IDepthStencilState : IMyPersistentResource<DepthStencilStateDescription>
    {
    }

    internal interface IDepthStencilStateInternal : IDepthStencilState
    {
        DepthStencilState Resource { get; }
    }

    namespace Internal
    {
        internal class MyDepthStencilState : MyPersistentResource<DepthStencilState, DepthStencilStateDescription>, IDepthStencilStateInternal
        {
            protected override DepthStencilState CreateResource(ref DepthStencilStateDescription description)
            {
                DepthStencilState ret = new DepthStencilState(MyRender11.Device, description);
                ret.DebugName = Name;
                return ret;
            }
        }
    }

    internal class MyDepthStencilStateManager : MyPersistentResourceManager<MyDepthStencilState, DepthStencilStateDescription>
    {
        internal static IDepthStencilState DepthTestWrite;
        internal static IDepthStencilState DepthTestReadOnly;
        internal static IDepthStencilState IgnoreDepthStencil;
        internal static IDepthStencilState MarkEdgeInStencil;
        internal static IDepthStencilState WriteHighlightStencil;
        internal static IDepthStencilState TestHighlightOuterStencil;
        internal static IDepthStencilState TestHighlightInnerStencil;
        internal static IDepthStencilState TestEdgeStencil;
        internal static IDepthStencilState TestDepthAndEdgeStencil;
        internal static IDepthStencilState[] MarkIfInsideCascade;
        internal static IDepthStencilState[] MarkIfInsideCascadeOld;
        internal static IDepthStencilState DefaultDepthState { get { return MyRender11.UseComplementaryDepthBuffer ? DepthTestWrite : null; } }

        internal static IDepthStencilState StereoDefaultDepthState;
        internal static IDepthStencilState StereoStencilMask;
        internal static IDepthStencilState StereoDepthTestReadOnly;
        internal static IDepthStencilState StereoDepthTestWrite;
        internal static IDepthStencilState StereoIgnoreDepthStencil;

        internal static byte GetStereoMask()
        {
            return 0x10;
        }

        protected override int GetAllocResourceCount()
        {
            return 128;
        }

        public MyDepthStencilStateManager()
        {
            // Bits in stencil buffer: 76543210
            // bits 3-0 used in CSM
            // bit 4 used for stereo rendering mask
            // bit 6 used for outlines

            {
                DepthStencilStateDescription desc = new DepthStencilStateDescription();
                desc.DepthComparison = MyRender11.UseComplementaryDepthBuffer ? Comparison.Greater : Comparison.Less;
                desc.DepthWriteMask = DepthWriteMask.All;
                desc.IsDepthEnabled = true;
                desc.IsStencilEnabled = false;
                MyDepthStencilStateManager.DepthTestWrite = CreateResource("DepthTestWrite", ref desc);
            }

            {
                DepthStencilStateDescription desc = new DepthStencilStateDescription();
                desc.DepthComparison = MyRender11.UseComplementaryDepthBuffer ? Comparison.Greater : Comparison.Less;
                desc.DepthWriteMask = DepthWriteMask.Zero;
                desc.IsDepthEnabled = true;
                desc.IsStencilEnabled = false;
                MyDepthStencilStateManager.DepthTestReadOnly = CreateResource("DepthTest", ref desc);
            }

            {
                DepthStencilStateDescription desc = new DepthStencilStateDescription();
                desc.DepthComparison = MyRender11.UseComplementaryDepthBuffer ? Comparison.Greater : Comparison.Less;
                desc.DepthWriteMask = DepthWriteMask.Zero;
                desc.IsDepthEnabled = false;
                desc.IsStencilEnabled = false;
                MyDepthStencilStateManager.IgnoreDepthStencil = CreateResource("IgnoreDepthStencil", ref desc);
            }

            {
                DepthStencilStateDescription desc = new DepthStencilStateDescription();
                desc.IsDepthEnabled = false;
                desc.IsStencilEnabled = true;
                desc.StencilReadMask = 0xFF;
                desc.StencilWriteMask = 0x80;
                desc.BackFace.Comparison = Comparison.Always;
                desc.BackFace.DepthFailOperation = StencilOperation.Replace;
                desc.BackFace.FailOperation = StencilOperation.Replace;
                desc.BackFace.PassOperation = StencilOperation.Replace;
                desc.FrontFace = desc.BackFace;
                MyDepthStencilStateManager.MarkEdgeInStencil = CreateResource("MarkEdgeInStencil", ref desc);
            }

            {
                DepthStencilStateDescription desc = new DepthStencilStateDescription();
                desc.IsDepthEnabled = false;
                desc.DepthComparison = Comparison.Always;
                desc.DepthWriteMask = DepthWriteMask.Zero;
                desc.IsStencilEnabled = true;
                desc.StencilReadMask = 0x00;
                desc.StencilWriteMask = MyHighlight.HIGHLIGHT_STENCIL_MASK;
                desc.BackFace.Comparison = Comparison.Always;
                desc.BackFace.DepthFailOperation = StencilOperation.Keep;
                desc.BackFace.FailOperation = StencilOperation.Replace;
                desc.BackFace.PassOperation = StencilOperation.Replace;
                desc.FrontFace = desc.BackFace;
                MyDepthStencilStateManager.WriteHighlightStencil = CreateResource("WriteHighlightStencil", ref desc);
            }

            {
                DepthStencilStateDescription desc = new DepthStencilStateDescription();
                desc.IsDepthEnabled = false;
                desc.IsStencilEnabled = true;
                desc.StencilReadMask = MyHighlight.HIGHLIGHT_STENCIL_MASK;
                desc.StencilWriteMask = 0x0;
                desc.BackFace.Comparison = Comparison.NotEqual;
                desc.BackFace.DepthFailOperation = StencilOperation.Keep;
                desc.BackFace.FailOperation = StencilOperation.Keep;
                desc.BackFace.PassOperation = StencilOperation.Keep;
                desc.FrontFace = desc.BackFace;
                MyDepthStencilStateManager.TestHighlightOuterStencil = CreateResource("TestHighlightOuterStencil", ref desc);
            }

            {
                DepthStencilStateDescription desc = new DepthStencilStateDescription();
                desc.IsDepthEnabled = false;
                desc.IsStencilEnabled = true;
                desc.StencilReadMask = MyHighlight.HIGHLIGHT_STENCIL_MASK;
                desc.StencilWriteMask = 0x0;
                desc.BackFace.Comparison = Comparison.Equal;
                desc.BackFace.DepthFailOperation = StencilOperation.Keep;
                desc.BackFace.FailOperation = StencilOperation.Keep;
                desc.BackFace.PassOperation = StencilOperation.Keep;
                desc.FrontFace = desc.BackFace;
                MyDepthStencilStateManager.TestHighlightInnerStencil = CreateResource("TestHighlightInnerStencil", ref desc);
            }

            {
                DepthStencilStateDescription desc = new DepthStencilStateDescription();
                desc.IsDepthEnabled = false;
                desc.IsStencilEnabled = true;
                desc.StencilReadMask = 0x80;
                desc.StencilWriteMask = 0x00;
                desc.BackFace.Comparison = Comparison.Equal;
                desc.BackFace.DepthFailOperation = StencilOperation.Keep;
                desc.BackFace.FailOperation = StencilOperation.Keep;
                desc.BackFace.PassOperation = StencilOperation.Keep;
                desc.FrontFace = desc.BackFace;
                MyDepthStencilStateManager.TestEdgeStencil = CreateResource("TestEdgeStencil", ref desc);
            }

            {
                DepthStencilStateDescription desc = new DepthStencilStateDescription();
                desc.DepthComparison = MyRender11.UseComplementaryDepthBuffer ? Comparison.Greater : Comparison.Less;
                desc.DepthWriteMask = DepthWriteMask.Zero;
                desc.IsDepthEnabled = true;
                desc.IsStencilEnabled = true;
                desc.StencilReadMask = 0x80;
                desc.StencilWriteMask = 0x00;
                desc.BackFace.Comparison = Comparison.Equal;
                desc.BackFace.DepthFailOperation = StencilOperation.Keep;
                desc.BackFace.FailOperation = StencilOperation.Keep;
                desc.BackFace.PassOperation = StencilOperation.Keep;
                desc.FrontFace = desc.BackFace;
                MyDepthStencilStateManager.TestDepthAndEdgeStencil = CreateResource("TestDepthAndEdgeStencil", ref desc);
            }

            MyDepthStencilStateManager.MarkIfInsideCascade = new MyDepthStencilState[8];

            {
                DepthStencilStateDescription desc = new DepthStencilStateDescription();
                desc.IsDepthEnabled = true;
                desc.DepthComparison = MyRender11.UseComplementaryDepthBuffer ? Comparison.Less : Comparison.Greater;
                desc.DepthWriteMask = DepthWriteMask.Zero;

                for (int cascadeIndex = 0; cascadeIndex < MyDepthStencilStateManager.MarkIfInsideCascade.Length; ++cascadeIndex)
                {
                    desc.IsStencilEnabled = true;
                    desc.StencilReadMask = 0xF;
                    desc.StencilWriteMask = (byte)(0xF - cascadeIndex);
                    desc.BackFace.Comparison = cascadeIndex == 0 ? Comparison.Always : Comparison.GreaterEqual;
                    desc.BackFace.DepthFailOperation = StencilOperation.Keep;
                    desc.BackFace.FailOperation = StencilOperation.Keep;
                    desc.BackFace.PassOperation = StencilOperation.Invert;
                    desc.FrontFace = desc.BackFace;
                    MyDepthStencilStateManager.MarkIfInsideCascade[cascadeIndex] = CreateResource("MarkIfInsideCascade_" + cascadeIndex, ref desc);
                }
            }

            MyDepthStencilStateManager.MarkIfInsideCascadeOld = new MyDepthStencilState[8];

            {
                DepthStencilStateDescription desc = new DepthStencilStateDescription();
                desc.IsDepthEnabled = true;
                desc.DepthComparison = MyRender11.UseComplementaryDepthBuffer ? Comparison.Less : Comparison.Greater;
                desc.DepthWriteMask = DepthWriteMask.Zero;

                for (int cascadeIndex = 0; cascadeIndex < MyDepthStencilStateManager.MarkIfInsideCascadeOld.Length; ++cascadeIndex)
                {
                    desc.IsStencilEnabled = true;
                    desc.StencilReadMask = 0xF;
                    desc.StencilWriteMask = (byte)(0xF - cascadeIndex);
                    desc.BackFace.Comparison = cascadeIndex == 0 ? Comparison.Always : Comparison.GreaterEqual;
                    desc.BackFace.DepthFailOperation = StencilOperation.Keep;
                    desc.BackFace.FailOperation = StencilOperation.Keep;
                    desc.BackFace.PassOperation = StencilOperation.Invert;
                    desc.FrontFace = desc.BackFace;
                    MyDepthStencilStateManager.MarkIfInsideCascadeOld[cascadeIndex] = CreateResource("MarkIfInsideCascade_" + cascadeIndex, ref desc);
                }
            }

            {
                DepthStencilStateDescription desc = new DepthStencilStateDescription();
                desc.DepthComparison = MyRender11.UseComplementaryDepthBuffer ? Comparison.Greater : Comparison.Less;
                desc.DepthWriteMask = DepthWriteMask.All;
                desc.IsDepthEnabled = true;
                desc.IsStencilEnabled = true;
                desc.StencilWriteMask = GetStereoMask();
                desc.StencilReadMask = GetStereoMask();
                desc.BackFace.Comparison = Comparison.GreaterEqual;
                desc.BackFace.DepthFailOperation = StencilOperation.Keep;
                desc.BackFace.FailOperation = StencilOperation.Keep;
                desc.BackFace.PassOperation = StencilOperation.Replace;
                desc.FrontFace = desc.BackFace;
                MyDepthStencilStateManager.StereoDefaultDepthState = CreateResource("StereoDefaultDepthState", ref desc);
            }
            {
                DepthStencilStateDescription desc = new DepthStencilStateDescription();
                desc.DepthComparison = MyRender11.UseComplementaryDepthBuffer ? Comparison.Greater : Comparison.Less;
                desc.DepthWriteMask = DepthWriteMask.Zero;
                desc.IsDepthEnabled = false;
                desc.IsStencilEnabled = true;
                desc.StencilWriteMask = GetStereoMask();
                desc.StencilReadMask = GetStereoMask();
                desc.BackFace.Comparison = Comparison.Always;
                desc.BackFace.DepthFailOperation = StencilOperation.Replace;
                desc.BackFace.FailOperation = StencilOperation.Replace;
                desc.BackFace.PassOperation = StencilOperation.Replace;
                desc.FrontFace = desc.BackFace;
                MyDepthStencilStateManager.StereoStencilMask = CreateResource("StereoStencilMask", ref desc);
            }
            {
                DepthStencilStateDescription desc = new DepthStencilStateDescription();
                desc.DepthComparison = MyRender11.UseComplementaryDepthBuffer ? Comparison.Greater : Comparison.Less;
                desc.DepthWriteMask = DepthWriteMask.Zero;
                desc.IsDepthEnabled = true;
                desc.IsStencilEnabled = true;
                desc.StencilWriteMask = GetStereoMask();
                desc.StencilReadMask = GetStereoMask();
                desc.BackFace.Comparison = Comparison.GreaterEqual;
                desc.BackFace.DepthFailOperation = StencilOperation.Keep;
                desc.BackFace.FailOperation = StencilOperation.Keep;
                desc.BackFace.PassOperation = StencilOperation.Replace;
                desc.FrontFace = desc.BackFace;
                MyDepthStencilStateManager.StereoDepthTestReadOnly = CreateResource("StereoDepthTestReadOnly", ref desc);
            }
            {
                DepthStencilStateDescription desc = new DepthStencilStateDescription();
                desc.DepthComparison = MyRender11.UseComplementaryDepthBuffer ? Comparison.Greater : Comparison.Less;
                desc.DepthWriteMask = DepthWriteMask.All;
                desc.IsDepthEnabled = true;
                desc.IsStencilEnabled = true;
                desc.StencilWriteMask = GetStereoMask();
                desc.StencilReadMask = GetStereoMask();
                desc.BackFace.Comparison = Comparison.GreaterEqual;
                desc.BackFace.DepthFailOperation = StencilOperation.Keep;
                desc.BackFace.FailOperation = StencilOperation.Keep;
                desc.BackFace.PassOperation = StencilOperation.Replace;
                desc.FrontFace = desc.BackFace;
                MyDepthStencilStateManager.StereoDepthTestWrite = CreateResource("StereoDepthTestWrite", ref desc);
            }
            {
                DepthStencilStateDescription desc = new DepthStencilStateDescription();
                desc.DepthComparison = MyRender11.UseComplementaryDepthBuffer ? Comparison.Greater : Comparison.Less;
                desc.DepthWriteMask = DepthWriteMask.Zero;
                desc.IsDepthEnabled = false;
                desc.IsStencilEnabled = true;
                desc.StencilWriteMask = GetStereoMask();
                desc.StencilReadMask = GetStereoMask();
                desc.BackFace.Comparison = Comparison.GreaterEqual;
                desc.BackFace.DepthFailOperation = StencilOperation.Keep;
                desc.BackFace.FailOperation = StencilOperation.Keep;
                desc.BackFace.PassOperation = StencilOperation.Replace;
                desc.FrontFace = desc.BackFace;
                MyDepthStencilStateManager.StereoIgnoreDepthStencil = CreateResource("StereoIgnoreDepthStencil", ref desc);
            }
        }
    }
}
