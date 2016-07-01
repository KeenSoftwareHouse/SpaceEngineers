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
        }

        internal static byte GetStereoMask()
        {
            return 0x10;
        }

        internal static void Init()
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
                DepthTestWrite = MyPipelineStates.CreateDepthStencil(desc);
            }

            {
                DepthStencilStateDescription desc = new DepthStencilStateDescription();
                desc.DepthComparison = MyRender11.UseComplementaryDepthBuffer ? Comparison.Greater : Comparison.Less;
                desc.DepthWriteMask = DepthWriteMask.Zero;
                desc.IsDepthEnabled = true;
                desc.IsStencilEnabled = false;
                DepthTest = MyPipelineStates.CreateDepthStencil(desc);
            }

            {
                DepthStencilStateDescription desc = new DepthStencilStateDescription();
                desc.DepthComparison = MyRender11.UseComplementaryDepthBuffer ? Comparison.Greater : Comparison.Less;
                desc.DepthWriteMask = DepthWriteMask.Zero;
                desc.IsDepthEnabled = false;
                desc.IsStencilEnabled = false;
                IgnoreDepthStencil = MyPipelineStates.CreateDepthStencil(desc);
            }

            {
                DepthStencilStateDescription desc = new DepthStencilStateDescription();
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
                MarkEdgeInStencil = MyPipelineStates.CreateDepthStencil(desc);
            }

            {
                DepthStencilStateDescription desc = new DepthStencilStateDescription();
                desc.IsDepthEnabled = true;
                desc.DepthComparison = Comparison.Equal;
                desc.DepthWriteMask = DepthWriteMask.Zero;
                desc.IsStencilEnabled = true;
                desc.StencilReadMask = 0x00;
                desc.StencilWriteMask = 0x40;
                desc.BackFace.Comparison = Comparison.Always;
                desc.BackFace.DepthFailOperation = StencilOperation.Keep;
                desc.BackFace.FailOperation = StencilOperation.Replace;
                desc.BackFace.PassOperation = StencilOperation.Replace;
                desc.FrontFace = desc.BackFace;
                OutlineMesh = MyPipelineStates.CreateDepthStencil(desc);
            }

            {
                DepthStencilStateDescription desc = new DepthStencilStateDescription();
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
            }

            {
                DepthStencilStateDescription desc = new DepthStencilStateDescription();
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
                TestEdgeStencil = MyPipelineStates.CreateDepthStencil(desc);
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
                TestDepthAndEdgeStencil = MyPipelineStates.CreateDepthStencil(desc);
            }

            MarkIfInsideCascade = new DepthStencilId[8];

            {
                DepthStencilStateDescription desc = new DepthStencilStateDescription();
                desc.IsDepthEnabled = true;
                desc.DepthComparison = MyRender11.UseComplementaryDepthBuffer ? Comparison.Less : Comparison.Greater;
                desc.DepthWriteMask = DepthWriteMask.Zero;

                for (int cascadeIndex = 0; cascadeIndex < MarkIfInsideCascade.Length; ++cascadeIndex)
                {
                    desc.IsStencilEnabled = true;
                    desc.StencilReadMask = 0xF;
                    desc.StencilWriteMask = 0xF;
                    desc.BackFace.Comparison = cascadeIndex == 0 ? Comparison.Always : Comparison.Greater;
                    desc.BackFace.DepthFailOperation = StencilOperation.Keep;
                    desc.BackFace.FailOperation = StencilOperation.Keep;
                    desc.BackFace.PassOperation = StencilOperation.Replace;
                    desc.FrontFace = desc.BackFace;
                    MarkIfInsideCascade[cascadeIndex] = MyPipelineStates.CreateDepthStencil(desc);
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
                StereoDefaultDepthState = MyPipelineStates.CreateDepthStencil(desc);
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
                StereoStereoStencilMask = MyPipelineStates.CreateDepthStencil(desc);
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
                StereoDepthTestWrite = MyPipelineStates.CreateDepthStencil(desc);
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
                StereoIgnoreDepthStencil = MyPipelineStates.CreateDepthStencil(desc);
            }
        }

        internal static DepthStencilId DepthTestWrite;
        internal static DepthStencilId DepthTest;
        internal static DepthStencilId IgnoreDepthStencil;
        internal static DepthStencilId WriteDepthAndStencil;
        internal static DepthStencilId OutlineMesh;
        internal static DepthStencilId TestOutlineMeshStencil;
        internal static DepthStencilId TestHighlightMeshStencil;
        internal static DepthStencilId MarkEdgeInStencil;
        internal static DepthStencilId TestEdgeStencil;
        internal static DepthStencilId TestDepthAndEdgeStencil;
        internal static DepthStencilId[] MarkIfInsideCascade;
        internal static DepthStencilState DefaultDepthState { get { return MyRender11.UseComplementaryDepthBuffer ? (DepthStencilState)DepthTestWrite : null; } }

        internal static DepthStencilId StereoDefaultDepthState; 
        internal static DepthStencilId StereoStereoStencilMask;
        internal static DepthStencilId StereoDepthTestWrite;
        internal static DepthStencilId StereoIgnoreDepthStencil;

    }
}
