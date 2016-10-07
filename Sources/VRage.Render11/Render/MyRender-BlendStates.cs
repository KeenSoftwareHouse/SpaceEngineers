using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX.Direct3D11;

namespace VRageRender
{
    partial class MyRender11
    {
        internal static BlendId BlendGui;
        internal static BlendId BlendAdditive;
        internal static BlendId BlendAtmosphere;
        internal static BlendId BlendAlphaPremult;
        internal static BlendId BlendTransparent;
        internal static BlendId BlendOutscatter;

        internal static BlendId BlendDecalNormal;
        internal static BlendId BlendDecalColor;
        internal static BlendId BlendDecalNormalColor;
        internal static BlendId BlendDecalNormalColorExt;

        internal static BlendId BlendWeightedTransparency;
        internal static BlendId BlendWeightedTransparencyResolve;

        static void InitializeBlendStates()
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
                BlendGui = MyPipelineStates.CreateBlendState(desc);
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
                BlendAdditive = MyPipelineStates.CreateBlendState(desc);
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
                BlendAtmosphere = MyPipelineStates.CreateBlendState(desc);
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
                BlendTransparent = MyPipelineStates.CreateBlendState(desc);
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
                BlendAlphaPremult = MyPipelineStates.CreateBlendState(desc);
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
                BlendOutscatter = MyPipelineStates.CreateBlendState(desc);
            }

            {
                BlendStateDescription desc = new BlendStateDescription();
                desc.IndependentBlendEnable = true;

                // color
                desc.RenderTarget[0].IsBlendEnabled = true;
                desc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.Red | ColorWriteMaskFlags.Green | ColorWriteMaskFlags.Blue;
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

                BlendDecalColor = MyPipelineStates.CreateBlendState(desc);
            }

            {
                BlendStateDescription desc = new BlendStateDescription();
                desc.IndependentBlendEnable = true;

                // normal
                desc.RenderTarget[1].IsBlendEnabled = true;
                desc.RenderTarget[1].RenderTargetWriteMask = ColorWriteMaskFlags.Red | ColorWriteMaskFlags.Green | ColorWriteMaskFlags.Blue;
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

                BlendDecalNormal = MyPipelineStates.CreateBlendState(desc);
            }

            {
                BlendStateDescription desc = new BlendStateDescription();
                desc.IndependentBlendEnable = true;

                // color
                desc.RenderTarget[0].IsBlendEnabled = true;
                desc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.Red | ColorWriteMaskFlags.Green | ColorWriteMaskFlags.Blue;
                desc.RenderTarget[0].BlendOperation = BlendOperation.Add;
                desc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
                desc.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
                desc.RenderTarget[0].DestinationAlphaBlend = BlendOption.One;
                desc.RenderTarget[0].SourceBlend = BlendOption.One;
                desc.RenderTarget[0].SourceAlphaBlend = BlendOption.Zero;
                // normal
                desc.RenderTarget[1].IsBlendEnabled = true;
                desc.RenderTarget[1].RenderTargetWriteMask = ColorWriteMaskFlags.Red | ColorWriteMaskFlags.Green | ColorWriteMaskFlags.Blue;
                desc.RenderTarget[1].BlendOperation = BlendOperation.Add;
                desc.RenderTarget[1].AlphaBlendOperation = BlendOperation.Add;
                desc.RenderTarget[1].DestinationBlend = BlendOption.InverseSourceAlpha;
                desc.RenderTarget[1].DestinationAlphaBlend = BlendOption.One;
                desc.RenderTarget[1].SourceBlend = BlendOption.SourceAlpha;
                desc.RenderTarget[1].SourceAlphaBlend = BlendOption.Zero;
                // metal/gloss
                desc.RenderTarget[2].IsBlendEnabled = true;
                desc.RenderTarget[2].RenderTargetWriteMask = ColorWriteMaskFlags.Red | ColorWriteMaskFlags.Green;
                desc.RenderTarget[2].BlendOperation = BlendOperation.Add;
                desc.RenderTarget[2].AlphaBlendOperation = BlendOperation.Add;
                desc.RenderTarget[2].DestinationBlend = BlendOption.InverseSourceAlpha;
                desc.RenderTarget[2].DestinationAlphaBlend = BlendOption.One;
                desc.RenderTarget[2].SourceBlend = BlendOption.One;
                desc.RenderTarget[2].SourceAlphaBlend = BlendOption.Zero;

                BlendDecalNormalColor = MyPipelineStates.CreateBlendState(desc);

                desc.RenderTarget[2].RenderTargetWriteMask = ColorWriteMaskFlags.Red | ColorWriteMaskFlags.Green | ColorWriteMaskFlags.Blue;
                BlendDecalNormalColorExt = MyPipelineStates.CreateBlendState(desc);
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
                BlendWeightedTransparencyResolve = MyPipelineStates.CreateBlendState(desc);
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
                BlendWeightedTransparency = MyPipelineStates.CreateBlendState(desc);
            }
        }
    }
}