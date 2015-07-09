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
            MarkAAEdge = MyPipelineStates.CreateDepthStencil(desc);

            desc.IsDepthEnabled = false;
            desc.IsStencilEnabled = true;
            desc.StencilReadMask = 0x80;
            desc.StencilWriteMask = 0x00;
            desc.BackFace.Comparison = Comparison.Equal;
            desc.BackFace.DepthFailOperation = StencilOperation.Keep;
            desc.BackFace.FailOperation = StencilOperation.Keep;
            desc.BackFace.PassOperation = StencilOperation.Keep;
            desc.FrontFace = desc.BackFace;
            TestAAEdge = MyPipelineStates.CreateDepthStencil(desc);


            desc.DepthComparison = MyRender11.UseComplementaryDepthBuffer ? Comparison.Greater : Comparison.Less;
            desc.DepthWriteMask = DepthWriteMask.Zero;
            desc.IsStencilEnabled = true;
            desc.StencilReadMask = 0x80;
            desc.StencilWriteMask = 0x00;
            desc.BackFace.Comparison = Comparison.Equal;
            desc.BackFace.DepthFailOperation = StencilOperation.Keep;
            desc.BackFace.FailOperation = StencilOperation.Keep;
            desc.BackFace.PassOperation = StencilOperation.Keep;
            desc.FrontFace = desc.BackFace;
            TestDepthAndAAEdge = MyPipelineStates.CreateDepthStencil(desc);

            MarkIfInside = new DepthStencilId[4];

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
            MarkIfInside[0] = MyPipelineStates.CreateDepthStencil(desc);

            desc.StencilWriteMask = 0x02;
            MarkIfInside[1] = MyPipelineStates.CreateDepthStencil(desc);
            desc.StencilWriteMask = 0x04;
            MarkIfInside[2] = MyPipelineStates.CreateDepthStencil(desc);
            desc.StencilWriteMask = 0x08;
            MarkIfInside[3] = MyPipelineStates.CreateDepthStencil(desc);
        }



        internal static DepthStencilId DepthTestWrite;
        internal static DepthStencilId DepthTest;
        internal static DepthStencilId IgnoreDepthStencil;
        internal static DepthStencilId MarkAAEdge;
        internal static DepthStencilId TestAAEdge;
        internal static DepthStencilId TestDepthAndAAEdge;
        internal static DepthStencilId [] MarkIfInside;

        internal static DepthStencilState DefaultDepthState { get { return MyRender11.UseComplementaryDepthBuffer ? (DepthStencilState)DepthTestWrite : null; } }
    }
}
