using SharpDX.Direct3D11;
using VRage.Render11.Resources.Internal;
using VRageRender;

namespace VRage.Render11.Resources
{
    interface IBlendState : IMyPersistentResource<BlendStateDescription>
    {
    }

    interface IBlendStateInternal : IBlendState
    {
        BlendState Resource { get; }
    }

    namespace Internal
    {
        internal class MyBlendState : MyPersistentResource<BlendState, BlendStateDescription>, IBlendStateInternal
        {
            protected override BlendStateDescription CloneDescription(ref BlendStateDescription desc)
            {
                return desc.Clone();
            }

            protected override BlendState CreateResource(ref BlendStateDescription description)
            {
                BlendState ret = new BlendState(MyRender11.Device, description);
                ret.DebugName = Name;
                return ret;
            }
        }
    }

    internal class MyBlendStateManager : MyPersistentResourceManager<MyBlendState, BlendStateDescription>
    {
        internal static IBlendState BlendGui;
        internal static IBlendState BlendAdditive;
        internal static IBlendState BlendAtmosphere;
        internal static IBlendState BlendTransparent;
        internal static IBlendState BlendAlphaPremult;
        internal static IBlendState BlendAlphaPremultNoAlphaChannel;
        internal static IBlendState BlendReplace;
        internal static IBlendState BlendReplaceNoAlphaChannel;
        internal static IBlendState BlendOutscatter;

        internal static IBlendState BlendDecalColor;
        internal static IBlendState BlendDecalNormal;
        internal static IBlendState BlendDecalNormalColor;
        internal static IBlendState BlendDecalNormalColorExt;

        internal static IBlendState BlendDecalColorNoPremult;
        internal static IBlendState BlendDecalNormalNoPremult;
        internal static IBlendState BlendDecalNormalColorNoPremult;
        internal static IBlendState BlendDecalNormalColorExtNoPremult;


        internal static IBlendState BlendWeightedTransparencyResolve;
        internal static IBlendState BlendWeightedTransparency;

        protected override int GetAllocResourceCount()
        {
            return 16;
        }

        public MyBlendStateManager()
        {
            {
                BlendStateDescription desc = new BlendStateDescription();
                desc.RenderTarget[0].IsBlendEnabled = true;
                desc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
                desc.RenderTarget[0].BlendOperation = BlendOperation.Add;
                desc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
                desc.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
                desc.RenderTarget[0].DestinationAlphaBlend = BlendOption.InverseSourceAlpha;
                desc.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
                desc.RenderTarget[0].SourceAlphaBlend = BlendOption.SourceAlpha;
                BlendGui = CreateResource("BlendGui", ref desc);
            }

            {
                BlendStateDescription desc = new BlendStateDescription();
                desc.RenderTarget[0].IsBlendEnabled = true;
                desc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
                desc.RenderTarget[0].BlendOperation = BlendOperation.Add;
                desc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
                desc.RenderTarget[0].DestinationBlend = BlendOption.One;
                desc.RenderTarget[0].DestinationAlphaBlend = BlendOption.One;
                desc.RenderTarget[0].SourceBlend = BlendOption.One;
                desc.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
                BlendAdditive = CreateResource("BlendAdditive", ref desc);
            }

            {
                BlendStateDescription desc = new BlendStateDescription();
                desc.RenderTarget[0].IsBlendEnabled = true;
                desc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
                desc.RenderTarget[0].BlendOperation = BlendOperation.Add;
                desc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
                desc.RenderTarget[0].DestinationBlend = BlendOption.SourceAlpha;
                desc.RenderTarget[0].DestinationAlphaBlend = BlendOption.One;
                desc.RenderTarget[0].SourceBlend = BlendOption.One;
                desc.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
                BlendAtmosphere = CreateResource("BlendAtmosphere", ref desc);
            }

            {
                BlendStateDescription desc = new BlendStateDescription();
                desc.RenderTarget[0].IsBlendEnabled = true;
                desc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
                desc.RenderTarget[0].BlendOperation = BlendOperation.Add;
                desc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
                desc.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
                desc.RenderTarget[0].DestinationAlphaBlend = BlendOption.InverseSourceAlpha;
                desc.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
                desc.RenderTarget[0].SourceAlphaBlend = BlendOption.SourceAlpha;
                BlendTransparent = CreateResource("BlendTransparent", ref desc);
            }

            {
                BlendStateDescription desc = new BlendStateDescription();
                desc.RenderTarget[0].IsBlendEnabled = true;
                desc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
                desc.RenderTarget[0].BlendOperation = BlendOperation.Add;
                desc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
                desc.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
                desc.RenderTarget[0].DestinationAlphaBlend = BlendOption.InverseSourceAlpha;
                desc.RenderTarget[0].SourceBlend = BlendOption.One;
                desc.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
                BlendAlphaPremult = CreateResource("BlendAlphaPremult", ref desc);
            }

            {
                BlendStateDescription desc = new BlendStateDescription();
                desc.RenderTarget[0].IsBlendEnabled = true;
                desc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.Red | ColorWriteMaskFlags.Green | ColorWriteMaskFlags.Blue;
                desc.RenderTarget[0].BlendOperation = BlendOperation.Add;
                desc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
                desc.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
                desc.RenderTarget[0].DestinationAlphaBlend = BlendOption.One;
                desc.RenderTarget[0].SourceBlend = BlendOption.One;
                desc.RenderTarget[0].SourceAlphaBlend = BlendOption.Zero;
                BlendAlphaPremultNoAlphaChannel = CreateResource("BlendAlphaPremultNoAlphaChannel", ref desc);
            }

            {
                BlendStateDescription desc = new BlendStateDescription();
                desc.RenderTarget[0].IsBlendEnabled = true;
                desc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
                desc.RenderTarget[0].BlendOperation = BlendOperation.Add;
                desc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
                desc.RenderTarget[0].DestinationBlend = BlendOption.Zero;
                desc.RenderTarget[0].DestinationAlphaBlend = BlendOption.Zero;
                desc.RenderTarget[0].SourceBlend = BlendOption.One;
                desc.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
                BlendReplace = CreateResource("BlendReplace", ref desc);
            }

            {
                BlendStateDescription desc = new BlendStateDescription();
                desc.RenderTarget[0].IsBlendEnabled = true;
                desc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.Red | ColorWriteMaskFlags.Green | ColorWriteMaskFlags.Blue;
                desc.RenderTarget[0].BlendOperation = BlendOperation.Add;
                desc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
                desc.RenderTarget[0].DestinationBlend = BlendOption.Zero;
                desc.RenderTarget[0].DestinationAlphaBlend = BlendOption.Zero;
                desc.RenderTarget[0].SourceBlend = BlendOption.One;
                desc.RenderTarget[0].SourceAlphaBlend = BlendOption.Zero;
                BlendReplaceNoAlphaChannel = CreateResource("BlendReplaceNoAlphaChannel", ref desc);
            }

            {
                BlendStateDescription desc = new BlendStateDescription();
                desc.RenderTarget[0].IsBlendEnabled = true;
                desc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
                desc.RenderTarget[0].BlendOperation = BlendOperation.Add;
                desc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
                desc.RenderTarget[0].DestinationBlend = BlendOption.SourceColor;
                desc.RenderTarget[0].DestinationAlphaBlend = BlendOption.InverseSourceAlpha;
                desc.RenderTarget[0].SourceBlend = BlendOption.Zero;
                desc.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
                BlendOutscatter = CreateResource("BlendOutscatter", ref desc);
            }

            {
                BlendStateDescription desc = new BlendStateDescription();
                desc.IndependentBlendEnable = true;

                // color
                desc.RenderTarget[0].IsBlendEnabled = true;
                desc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.Red | ColorWriteMaskFlags.Green |
                                                             ColorWriteMaskFlags.Blue;
                desc.RenderTarget[0].BlendOperation = BlendOperation.Add;
                desc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
                desc.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
                desc.RenderTarget[0].DestinationAlphaBlend = BlendOption.One;
                desc.RenderTarget[0].SourceBlend = BlendOption.One;
                desc.RenderTarget[0].SourceAlphaBlend = BlendOption.Zero;
                // metal
                desc.RenderTarget[2].IsBlendEnabled = true;
                desc.RenderTarget[2].RenderTargetWriteMask = ColorWriteMaskFlags.Red;
                desc.RenderTarget[2].BlendOperation = BlendOperation.Add;
                desc.RenderTarget[2].AlphaBlendOperation = BlendOperation.Add;
                desc.RenderTarget[2].DestinationBlend = BlendOption.InverseSourceAlpha;
                desc.RenderTarget[2].DestinationAlphaBlend = BlendOption.One;
                desc.RenderTarget[2].SourceBlend = BlendOption.One;
                desc.RenderTarget[2].SourceAlphaBlend = BlendOption.Zero;

                BlendDecalColor = CreateResource("BlendDecalColor", ref desc);

                desc.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
                desc.RenderTarget[2].SourceBlend = BlendOption.SourceAlpha;
                BlendDecalColorNoPremult = CreateResource("BlendDecalColorNoPremult", ref desc);
            }

            {
                BlendStateDescription desc = new BlendStateDescription();
                desc.IndependentBlendEnable = true;

                // normal
                desc.RenderTarget[1].IsBlendEnabled = true;
                desc.RenderTarget[1].RenderTargetWriteMask = ColorWriteMaskFlags.Red | ColorWriteMaskFlags.Green |
                                                             ColorWriteMaskFlags.Blue;
                desc.RenderTarget[1].BlendOperation = BlendOperation.Add;
                desc.RenderTarget[1].AlphaBlendOperation = BlendOperation.Add;
                desc.RenderTarget[1].DestinationBlend = BlendOption.InverseSourceAlpha;
                desc.RenderTarget[1].DestinationAlphaBlend = BlendOption.One;
                desc.RenderTarget[1].SourceBlend = BlendOption.SourceAlpha;
                desc.RenderTarget[1].SourceAlphaBlend = BlendOption.Zero;
                // gloss
                desc.RenderTarget[2].IsBlendEnabled = true;
                desc.RenderTarget[2].RenderTargetWriteMask = ColorWriteMaskFlags.Green;
                desc.RenderTarget[2].BlendOperation = BlendOperation.Add;
                desc.RenderTarget[2].AlphaBlendOperation = BlendOperation.Add;
                desc.RenderTarget[2].DestinationBlend = BlendOption.InverseSourceAlpha;
                desc.RenderTarget[2].DestinationAlphaBlend = BlendOption.One;
                desc.RenderTarget[2].SourceBlend = BlendOption.One;
                desc.RenderTarget[2].SourceAlphaBlend = BlendOption.Zero;

                BlendDecalNormal = CreateResource("BlendDecalNormal", ref desc);

                desc.RenderTarget[2].SourceBlend = BlendOption.SourceAlpha;
                BlendDecalNormalNoPremult = CreateResource("BlendDecalNormalNoPremult", ref desc);
            }

            {
                BlendStateDescription desc = new BlendStateDescription();
                desc.IndependentBlendEnable = true;

                // color
                desc.RenderTarget[0].IsBlendEnabled = true;
                desc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.Red | ColorWriteMaskFlags.Green |
                                                             ColorWriteMaskFlags.Blue;
                desc.RenderTarget[0].BlendOperation = BlendOperation.Add;
                desc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
                desc.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
                desc.RenderTarget[0].DestinationAlphaBlend = BlendOption.One;
                desc.RenderTarget[0].SourceBlend = BlendOption.One;
                desc.RenderTarget[0].SourceAlphaBlend = BlendOption.Zero;
                // normal
                desc.RenderTarget[1].IsBlendEnabled = true;
                desc.RenderTarget[1].RenderTargetWriteMask = ColorWriteMaskFlags.Red | ColorWriteMaskFlags.Green |
                                                             ColorWriteMaskFlags.Blue;
                desc.RenderTarget[1].BlendOperation = BlendOperation.Add;
                desc.RenderTarget[1].AlphaBlendOperation = BlendOperation.Add;
                desc.RenderTarget[1].DestinationBlend = BlendOption.InverseSourceAlpha;
                desc.RenderTarget[1].DestinationAlphaBlend = BlendOption.One;
                desc.RenderTarget[1].SourceBlend = BlendOption.SourceAlpha;
                desc.RenderTarget[1].SourceAlphaBlend = BlendOption.Zero;
                // metal/gloss
                desc.RenderTarget[2].IsBlendEnabled = true;
                desc.RenderTarget[2].BlendOperation = BlendOperation.Add;
                desc.RenderTarget[2].AlphaBlendOperation = BlendOperation.Add;
                desc.RenderTarget[2].DestinationBlend = BlendOption.InverseSourceAlpha;
                desc.RenderTarget[2].DestinationAlphaBlend = BlendOption.One;
                desc.RenderTarget[2].SourceBlend = BlendOption.One;
                desc.RenderTarget[2].SourceAlphaBlend = BlendOption.Zero;

                // Premultiplied alpha
                desc.RenderTarget[2].RenderTargetWriteMask = ColorWriteMaskFlags.Red | ColorWriteMaskFlags.Green;
                BlendDecalNormalColor = CreateResource("BlendDecalNormalColor", ref desc);

                desc.RenderTarget[2].RenderTargetWriteMask = ColorWriteMaskFlags.Red | ColorWriteMaskFlags.Green |
                                                             ColorWriteMaskFlags.Blue;
                BlendDecalNormalColorExt = CreateResource("BlendDecalNormalColorExt", ref desc);

                // Non premultiplied alpha
                desc.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
                desc.RenderTarget[2].SourceBlend = BlendOption.SourceAlpha;

                desc.RenderTarget[2].RenderTargetWriteMask = ColorWriteMaskFlags.Red | ColorWriteMaskFlags.Green;
                BlendDecalNormalColorNoPremult = CreateResource("BlendDecalNormalColorNoPremult", ref desc);

                desc.RenderTarget[2].RenderTargetWriteMask = ColorWriteMaskFlags.Red | ColorWriteMaskFlags.Green |
                                                             ColorWriteMaskFlags.Blue;
                BlendDecalNormalColorExtNoPremult = CreateResource("BlendDecalNormalColorExtNoPremult", ref desc);
            }

            {
                BlendStateDescription desc = new BlendStateDescription();
                desc.RenderTarget[0].IsBlendEnabled = true;
                desc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
                desc.RenderTarget[0].BlendOperation = BlendOperation.Add;
                desc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
                desc.RenderTarget[0].DestinationBlend = BlendOption.SourceAlpha;
                desc.RenderTarget[0].DestinationAlphaBlend = BlendOption.SourceAlpha;
                desc.RenderTarget[0].SourceBlend = BlendOption.One;
                desc.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
                BlendWeightedTransparencyResolve = CreateResource("BlendWeightedTransparencyResolve", ref desc);
            }

            {
                BlendStateDescription desc = new BlendStateDescription();
                desc.IndependentBlendEnable = true;

                // accumulation target
                desc.RenderTarget[0].IsBlendEnabled = true;
                desc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
                desc.RenderTarget[0].BlendOperation = BlendOperation.Add;
                desc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
                desc.RenderTarget[0].DestinationBlend = BlendOption.One;
                desc.RenderTarget[0].DestinationAlphaBlend = BlendOption.One;
                desc.RenderTarget[0].SourceBlend = BlendOption.One;
                desc.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
                // coverage target
                desc.RenderTarget[1].IsBlendEnabled = true;
                desc.RenderTarget[1].RenderTargetWriteMask = ColorWriteMaskFlags.All;
                desc.RenderTarget[1].BlendOperation = BlendOperation.Add;
                desc.RenderTarget[1].AlphaBlendOperation = BlendOperation.Add;
                desc.RenderTarget[1].DestinationBlend = BlendOption.InverseSourceColor;
                desc.RenderTarget[1].DestinationAlphaBlend = BlendOption.InverseSourceAlpha;
                desc.RenderTarget[1].SourceBlend = BlendOption.Zero;
                desc.RenderTarget[1].SourceAlphaBlend = BlendOption.Zero;
                BlendWeightedTransparency = CreateResource("BlendWeightedTransparency", ref desc);
            }
        }
    }
}