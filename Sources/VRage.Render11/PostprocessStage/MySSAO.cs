using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Render11.Resources;
using VRageMath;

namespace VRageRender
{
    class MySSAO : MyScreenPass
    {
        internal static MySSAOSettings Params = MySSAOSettings.Default;

        readonly static Vector2[] m_filterKernel =
		{
			new Vector2( 1.0f,		 1.0f),
			new Vector2( 1.0f / 4,	-1.0f / 4),
			new Vector2(-1.0f,		-1.0f),
			new Vector2(-1.0f / 4,	 1.0f / 4),
			new Vector2( 1.0f / 2,	 1.0f / 2),
			new Vector2( 3.0f / 4,	-3.0f / 4),
			new Vector2(-1.0f / 2,	-1.0f / 2),
			new Vector2(-3.0f / 4,	 3.0f / 4)
		};

        const int NUM_SAMPLES = 8;
        static Vector4[] m_tmpOccluderPoints = new Vector4[NUM_SAMPLES];
        static Vector4[] m_tmpOccluderPointsFlipped = new Vector4[NUM_SAMPLES];
        static void FillRandomVectors(MyMapping myMapping)
        {
            float maxTapMag = -1;
            for (uint i = 0; i < NUM_SAMPLES; i++)
            {
                float curr = m_filterKernel[i].Length();
                maxTapMag = (float)System.Math.Max(maxTapMag, curr);
            }


            float maxTapMagInv = 1.0f / maxTapMag;
            float rsum = 0.0f;
            for (uint i = 0; i < NUM_SAMPLES; i++)
            {
                Vector2 tapOffs = new Vector2(m_filterKernel[i].X * maxTapMagInv, m_filterKernel[i].Y * maxTapMagInv);

                m_tmpOccluderPoints[i].X = tapOffs.X;
                m_tmpOccluderPoints[i].Y = tapOffs.Y;
                m_tmpOccluderPoints[i].Z = 0;
                m_tmpOccluderPoints[i].W = (float)System.Math.Sqrt(1 - tapOffs.X * tapOffs.X - tapOffs.Y * tapOffs.Y);

                rsum += m_tmpOccluderPointsFlipped[i].W;

                m_tmpOccluderPointsFlipped[i].X = tapOffs.X;
                m_tmpOccluderPointsFlipped[i].Y = -tapOffs.Y;
            }

            var colorScale = 1.0f / (2 * rsum);
            colorScale *= Params.Data.ColorScale;

            for (int occluderIndex = 0; occluderIndex < NUM_SAMPLES; ++occluderIndex)
                myMapping.WriteAndPosition(ref m_tmpOccluderPoints[occluderIndex]);

            for (int occluderIndex = 0; occluderIndex < NUM_SAMPLES; ++occluderIndex)
                myMapping.WriteAndPosition(ref m_tmpOccluderPointsFlipped[occluderIndex]);
        }

        static PixelShaderId m_ps;

        internal static void RecreateShadersForSettings()
        {
            m_ps = MyShaders.CreatePs("Postprocess/SSAO/Ssao.hlsl");
        }

        internal static void Run(IRtvBindable dst, MyGBuffer gbuffer)
        {
            RC.ClearRtv(dst, new SharpDX.Color4(1, 1, 1, 1));

            var paramsCB = MyCommon.GetObjectCB(16 * (2 + NUM_SAMPLES * 2));

            var mapping = MyMapping.MapDiscard(paramsCB);
            mapping.WriteAndPosition(ref Params.Data);
            FillRandomVectors(mapping);
            mapping.Unmap();

            if (!MyStereoRender.Enable)
                RC.AllShaderStages.SetConstantBuffer(MyCommon.FRAME_SLOT, MyCommon.FrameConstants);
            else
                MyStereoRender.BindRawCB_FrameConstants(RC);
            RC.AllShaderStages.SetConstantBuffer(1, paramsCB);

            RC.PixelShader.Set(m_ps);
            RC.SetRtv(dst);

            RC.PixelShader.SetSrvs(0, gbuffer);
            RC.PixelShader.SetSamplers(0, MySamplerStateManager.StandardSamplers);
            RC.PixelShader.SetSrv(5, gbuffer.ResolvedDepthStencil.SrvDepth);
            RC.SetDepthStencilState(MyDepthStencilStateManager.IgnoreDepthStencil);

            DrawFullscreenQuad();
            RC.ResetTargets();
        }

        internal static void Init()
        {
            MyRender11.RegisterSettingsChangedListener(new OnSettingsChangedDelegate(RecreateShadersForSettings));
        }
    }
}
