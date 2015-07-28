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

            MarkIfInsideCascade = new DepthStencilId[4];

            desc.IsDepthEnabled = true;
            desc.DepthComparison = MyRender11.UseComplementaryDepthBuffer ? Comparison.Less : Comparison.Greater;
            desc.DepthWriteMask = DepthWriteMask.Zero;
            desc.IsStencilEnabled = true;
            desc.StencilReadMask = 0x00;
            desc.StencilWriteMask = 0x01;
            desc.BackFace.Comparison = Comparison.Always;
            desc.BackFace.DepthFailOperation = StencilOperation.Keep;
            desc.BackFace.FailOperation = StencilOperation.Keep;
            desc.BackFace.PassOperation = StencilOperation.Replace;
            desc.FrontFace = desc.BackFace;
            MarkIfInsideCascade[0] = MyPipelineStates.CreateDepthStencil(desc);

            desc.StencilWriteMask = 0x02;
            MarkIfInsideCascade[1] = MyPipelineStates.CreateDepthStencil(desc);
            desc.StencilWriteMask = 0x04;
            MarkIfInsideCascade[2] = MyPipelineStates.CreateDepthStencil(desc);
            desc.StencilWriteMask = 0x08;
            MarkIfInsideCascade[3] = MyPipelineStates.CreateDepthStencil(desc);
        }



        internal static DepthStencilId DepthTestWrite;
        internal static DepthStencilId DepthTest;
        internal static DepthStencilId IgnoreDepthStencil;
        internal static DepthStencilId OutlineMesh;
        internal static DepthStencilId TestOutlineMeshStencil;
        internal static DepthStencilId MarkEdgeInStencil;
        internal static DepthStencilId TestEdgeStencil;
        internal static DepthStencilId TestDepthAndEdgeStencil;
        internal static DepthStencilId [] MarkIfInsideCascade;

        internal static DepthStencilState DefaultDepthState { get { return MyRender11.UseComplementaryDepthBuffer ? (DepthStencilState)DepthTestWrite : null; } }
    }
}
