using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    class MyDepthStencilState
    {
        static MyDepthStencilState()
        {
            DepthStencilStateDescription desc = new DepthStencilStateDescription();

            desc.DepthComparison = MyRender11.UseComplementaryDepthBuffer ? Comparison.Greater : Comparison.Less;
            desc.DepthWriteMask = DepthWriteMask.All;
            desc.IsDepthEnabled = true;
            desc.IsStencilEnabled = false;
            DepthTestWrite = MyPipelineStates.CreateDepthStencil(desc);

            desc.DepthComparison = MyRender11.UseComplementaryDepthBuffer ? Comparison.Greater : Comparison.Less;
            desc.DepthWriteMask = DepthWriteMask.Zero;
            desc.IsDepthEnabled = true;
            desc.IsStencilEnabled = false;
            DepthTest = MyPipelineStates.CreateDepthStencil(desc);

            desc.IsDepthEnabled = false;
            desc.IsStencilEnabled = false;
            IgnoreDepthStencil = MyPipelineStates.CreateDepthStencil(desc);

            desc.DepthComparison = MyRender11.UseComplementaryDepthBuffer ? Comparison.Greater : Comparison.Less;
            desc.DepthWriteMask = DepthWriteMask.All;
            desc.IsDepthEnabled = true;
            desc.IsStencilEnabled = true;
            desc.StencilReadMask = 0xFF;
            desc.StencilWriteMask = 0xFF;
            desc.BackFace.Comparison = Comparison.Always;
            desc.BackFace.DepthFailOperation = StencilOperation.Keep;
            desc.BackFace.FailOperation = StencilOperation.Replace;
            desc.BackFace.PassOperation = StencilOperation.Replace;
            desc.FrontFace = desc.BackFace;
            WriteDepthAndStencil = MyPipelineStates.CreateDepthStencil(desc);

            desc.IsDepthEnabled = false;
            desc.IsStencilEnabled = true;
            desc.StencilReadMask = 0xFF;
            desc.StencilWriteMask = 0x80;
            desc.BackFace.Comparison = Comparison.Always;
            desc.BackFace.DepthFailOperation = StencilOperation.Replace;
            desc.BackFace.FailOperation = StencilOperation.Replace;
            desc.BackFace.PassOperation = StencilOperation.Replace;
            desc.FrontFace = desc.BackFace;
            MarkEdgeInStencil = MyPipelineStates.CreateDepthStencil(desc);

            desc.IsDepthEnabled = true;
            desc.DepthComparison = Comparison.Equal;
            desc.IsStencilEnabled = true;
            desc.StencilReadMask = 0x00;
            desc.StencilWriteMask = 0x40;
            desc.BackFace.Comparison = Comparison.Always;
            desc.BackFace.DepthFailOperation = StencilOperation.Keep;
            desc.BackFace.FailOperation = StencilOperation.Replace;
            desc.BackFace.PassOperation = StencilOperation.Replace;
            desc.FrontFace = desc.BackFace;
            OutlineMesh = MyPipelineStates.CreateDepthStencil(desc);

            desc.IsDepthEnabled = false;
            desc.IsStencilEnabled = true;
            desc.StencilReadMask = 0x40;
            desc.StencilWriteMask = 0x0;
            desc.BackFace.Comparison = Comparison.NotEqual;
            desc.BackFace.DepthFailOperation = StencilOperation.Keep;
            desc.BackFace.FailOperation = StencilOperation.Keep;
            desc.BackFace.PassOperation = StencilOperation.Keep;
            desc.FrontFace = desc.BackFace;
            TestOutlineMeshStencil = MyPipelineStates.CreateDepthStencil(desc);

            desc.IsDepthEnabled = false;
            desc.IsStencilEnabled = true;
            desc.StencilReadMask = 0x40;
            desc.StencilWriteMask = 0x0;
            desc.BackFace.Comparison = Comparison.Equal;
            desc.BackFace.DepthFailOperation = StencilOperation.Keep;
            desc.BackFace.FailOperation = StencilOperation.Keep;
            desc.BackFace.PassOperation = StencilOperation.Keep;
            desc.FrontFace = desc.BackFace;
            TestHighlightMeshStencil = MyPipelineStates.CreateDepthStencil(desc);

            desc.IsDepthEnabled = false;
            desc.DepthWriteMask = 0x0;
            desc.DepthComparison = MyRender11.UseComplementaryDepthBuffer ? Comparison.Less : Comparison.Greater;
            desc.IsStencilEnabled = true;
            desc.StencilReadMask = 0xFF;
            desc.StencilWriteMask = 0x0;
            desc.BackFace.Comparison = Comparison.NotEqual;
            desc.BackFace.DepthFailOperation = StencilOperation.Keep;
            desc.BackFace.FailOperation = StencilOperation.Keep;
            desc.BackFace.PassOperation = StencilOperation.Keep;
            desc.FrontFace = desc.BackFace;
            DiscardTestStencil = MyPipelineStates.CreateDepthStencil(desc);

            desc.IsDepthEnabled = false;
            desc.IsStencilEnabled = true;
            desc.StencilReadMask = 0x80;
            desc.StencilWriteMask = 0x00;
            desc.BackFace.Comparison = Comparison.Equal;
            desc.BackFace.DepthFailOperation = StencilOperation.Keep;
            desc.BackFace.FailOperation = StencilOperation.Keep;
            desc.BackFace.PassOperation = StencilOperation.Keep;
            desc.FrontFace = desc.BackFace;
            TestEdgeStencil = MyPipelineStates.CreateDepthStencil(desc);


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
            TestDepthAndEdgeStencil = MyPipelineStates.CreateDepthStencil(desc);

            MarkIfInsideCascade = new DepthStencilId[MyRender11.Settings.ShadowCascadeCount];

            desc.IsDepthEnabled = true;
            desc.DepthComparison = MyRender11.UseComplementaryDepthBuffer ? Comparison.Less : Comparison.Greater;
            desc.DepthWriteMask = DepthWriteMask.Zero;
            desc.IsStencilEnabled = true;
            desc.StencilReadMask = 0x00;
            desc.BackFace.Comparison = Comparison.Always;
            desc.BackFace.DepthFailOperation = StencilOperation.Keep;
            desc.BackFace.FailOperation = StencilOperation.Keep;
            desc.BackFace.PassOperation = StencilOperation.Replace;
            desc.FrontFace = desc.BackFace;

            for (int cascadeIndex = 0; cascadeIndex < MarkIfInsideCascade.Length; ++cascadeIndex)
            {
                desc.StencilWriteMask = (byte) (0x01 << cascadeIndex);
                MarkIfInsideCascade[cascadeIndex] = MyPipelineStates.CreateDepthStencil(desc);
            }
        }

        public static void ResizeMarkIfInsideCascade()
        {
            if (MarkIfInsideCascade == null || MarkIfInsideCascade.Length == 0)
                return;

            var desc = MyPipelineStates.GetDepthStencil(MarkIfInsideCascade[0]).Description;
            if(MarkIfInsideCascade.Length < MyRenderProxy.Settings.ShadowCascadeCount)
                MarkIfInsideCascade = new DepthStencilId[MyRenderProxy.Settings.ShadowCascadeCount];

            for (int cascadeIndex = 0; cascadeIndex < MarkIfInsideCascade.Length; ++cascadeIndex)
            {
                desc.StencilWriteMask = (byte)(0x01 << cascadeIndex);
                MarkIfInsideCascade[cascadeIndex] = MyPipelineStates.CreateDepthStencil(desc);
            }
        }



        internal static DepthStencilId DepthTestWrite;
        internal static DepthStencilId DepthTest;
        internal static DepthStencilId IgnoreDepthStencil;
        internal static DepthStencilId WriteDepthAndStencil;
        internal static DepthStencilId OutlineMesh;
        internal static DepthStencilId TestOutlineMeshStencil;
        internal static DepthStencilId TestHighlightMeshStencil;
        internal static DepthStencilId DiscardTestStencil;
        internal static DepthStencilId MarkEdgeInStencil;
        internal static DepthStencilId TestEdgeStencil;
        internal static DepthStencilId TestDepthAndEdgeStencil;
        internal static DepthStencilId [] MarkIfInsideCascade;

        internal static DepthStencilState DefaultDepthState { get { return MyRender11.UseComplementaryDepthBuffer ? (DepthStencilState)DepthTestWrite : null; } }
    }
}
