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
        internal static BlendId BlendAlphaPremult;
        internal static BlendId BlendTransparent;

        internal static BlendId BlendDecal;

        static void InitializeBlendStates()
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

            desc.RenderTarget[0].IsBlendEnabled = true;
            desc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
            desc.RenderTarget[0].BlendOperation = BlendOperation.Add;
            desc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
            desc.RenderTarget[0].DestinationBlend = BlendOption.One;
            desc.RenderTarget[0].DestinationAlphaBlend = BlendOption.One;
            desc.RenderTarget[0].SourceBlend = BlendOption.One;
            desc.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
            BlendAdditive = MyPipelineStates.CreateBlendState(desc);

            desc.RenderTarget[0].IsBlendEnabled = true;
            desc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
            desc.RenderTarget[0].BlendOperation = BlendOperation.Add;
            desc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
            desc.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
            desc.RenderTarget[0].DestinationAlphaBlend = BlendOption.InverseSourceAlpha;
            desc.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
            desc.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
            BlendTransparent = MyPipelineStates.CreateBlendState(desc);

            desc.RenderTarget[0].IsBlendEnabled = true;
            desc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
            desc.RenderTarget[0].BlendOperation = BlendOperation.Add;
            desc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
            desc.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
            desc.RenderTarget[0].DestinationAlphaBlend = BlendOption.InverseSourceAlpha;
            desc.RenderTarget[0].SourceBlend = BlendOption.One;
            desc.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
            BlendAlphaPremult = MyPipelineStates.CreateBlendState(desc);


            desc.IndependentBlendEnable = true;
            // color metal
            desc.RenderTarget[0].IsBlendEnabled = true;
            desc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.Red | ColorWriteMaskFlags.Green | ColorWriteMaskFlags.Blue;
            desc.RenderTarget[0].BlendOperation = BlendOperation.Add;
            desc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
            desc.RenderTarget[0].DestinationBlend = BlendOption.BlendFactor;
            desc.RenderTarget[0].DestinationAlphaBlend = BlendOption.BlendFactor;
            desc.RenderTarget[0].SourceBlend = BlendOption.InverseBlendFactor;
            desc.RenderTarget[0].SourceAlphaBlend = BlendOption.InverseBlendFactor;
            // normal gloss
            desc.RenderTarget[1].IsBlendEnabled = true;
            desc.RenderTarget[1].RenderTargetWriteMask = ColorWriteMaskFlags.All;
            desc.RenderTarget[1].BlendOperation = BlendOperation.Add;
            desc.RenderTarget[1].AlphaBlendOperation = BlendOperation.Add;
            desc.RenderTarget[1].DestinationBlend = BlendOption.Zero;
            desc.RenderTarget[1].DestinationAlphaBlend = BlendOption.Zero;
            desc.RenderTarget[1].SourceBlend = BlendOption.One;
            desc.RenderTarget[1].SourceAlphaBlend = BlendOption.One;

            BlendDecal = MyPipelineStates.CreateBlendState(desc);
        }
    }
}