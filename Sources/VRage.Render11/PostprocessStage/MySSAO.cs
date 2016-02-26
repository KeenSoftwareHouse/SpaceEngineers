using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    struct MySSAO_Params
    {
        internal float MinRadius;
        internal float MaxRadius;
        internal float RadiusGrow;
        internal float Falloff;
        internal float RadiusBias;
        internal float Contrast;
        internal float Normalization;
        internal float ColorScale;

        internal static MySSAO_Params Default = new MySSAO_Params
        {
            MinRadius = 0.095f,
            MaxRadius = 4.16f,
            RadiusGrow = 1.007f,
            Falloff = 3.08f,
            RadiusBias = 0.25f,
            Contrast = 2.617f,
            Normalization = 0.075f,
            ColorScale = 0.6f,
        };
    }
    		
    class MySSAO : MyScreenPass
    {
        internal static MySSAO_Params Params = MySSAO_Params.Default;

        internal static bool UseBlur = true;

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

        static unsafe void FillRandomVectors(MyMapping myMapping)
        {
            float maxTapMag = -1;
            for (uint i = 0; i < NUM_SAMPLES; i++)
            {
                float curr = m_filterKernel[i].Length();
                maxTapMag = (float)System.Math.Max(maxTapMag, curr);
            }


            float maxTapMagInv = 1.0f / maxTapMag;
            float rsum = 0.0f;
            Vector4* occluderPoints = stackalloc Vector4[NUM_SAMPLES];
            Vector4* occluderPointsFlipped = stackalloc Vector4[NUM_SAMPLES];
            for (uint i = 0; i < NUM_SAMPLES; i++)
            {
                Vector2 tapOffs = new Vector2(m_filterKernel[i].X * maxTapMagInv, m_filterKernel[i].Y * maxTapMagInv);

                occluderPoints[i].X = tapOffs.X;
                occluderPoints[i].Y = tapOffs.Y;
                occluderPoints[i].Z = 0;
                occluderPoints[i].W = (float)System.Math.Sqrt(1 - tapOffs.X * tapOffs.X - tapOffs.Y * tapOffs.Y);

                rsum += occluderPoints[i].W;

                //
                occluderPointsFlipped[i].X = tapOffs.X;
                occluderPointsFlipped[i].Y = -tapOffs.Y;
            }

            var colorScale = 1.0f / (2 * rsum);
            colorScale *= Params.ColorScale;

            for (int occluderIndex = 0; occluderIndex < NUM_SAMPLES; ++occluderIndex)
                myMapping.WriteAndPosition(ref occluderPoints[occluderIndex]);

            for (int occluderIndex = 0; occluderIndex < NUM_SAMPLES; ++occluderIndex)
                myMapping.WriteAndPosition(ref occluderPointsFlipped[occluderIndex]);
        }

        static PixelShaderId m_ps;

        internal static void RecreateShadersForSettings()
        {
            m_ps = MyShaders.CreatePs("ssao_0.hlsl");
        }

        internal static void Run(MyBindableResource dst, MyGBuffer gbuffer, MyBindableResource resolvedDepth)
        {
            RC.DeviceContext.ClearRenderTargetView((dst as IRenderTargetBindable).RTV, new SharpDX.Color4(1, 1, 1, 1));

            var paramsCB = MyCommon.GetObjectCB(16 * (2 + NUM_SAMPLES * 2));

            var mapping = MyMapping.MapDiscard(paramsCB);
            mapping.WriteAndPosition(ref Params);
            FillRandomVectors(mapping);
            mapping.Unmap();

            RC.SetCB(0, MyCommon.FrameConstants);
            RC.SetCB(1, paramsCB);

            RC.SetPS(m_ps);
            RC.BindDepthRT(null, DepthStencilAccess.DepthReadOnly, dst);

            RC.BindGBufferForRead(0, gbuffer);
            RC.BindSRV(5, resolvedDepth);

            DrawFullscreenQuad();
        }

        internal static void Init()
        {
            MyRender11.RegisterSettingsChangedListener(new OnSettingsChangedDelegate(RecreateShadersForSettings));
        }
    }
}
