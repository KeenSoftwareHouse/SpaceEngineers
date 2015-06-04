﻿using System;
using SharpDX;
using Vector2 = VRageMath.Vector2;
using Vector4 = VRageMath.Vector4;

namespace VRageRender
{
    class MySSAO_Params
    {
        internal float MinRadius = 0.095f;
        internal float MaxRadius = 4.16f;
        internal float RadiusGrow = 1.007f;
        internal float Falloff = 3.08f;
        internal float RadiusBias = 0.25f;
        internal float Contrast = 2.617f;
        internal float Normalization = 0.075f;
        internal float ColorScale = 0.6f;
    }
    		
    class MySSAO : MyScreenPass
    {
        internal static MySSAO_Params Params = new MySSAO_Params();

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

        static void FillRandomVectors(DataStream stream)
        {
            float maxTapMag = -1;
            for (uint i = 0; i < NUM_SAMPLES; i++)
            {
                float curr = m_filterKernel[i].Length();
                maxTapMag = Math.Max(maxTapMag, curr);
            }


            float maxTapMagInv = 1.0f / maxTapMag;
            float rsum = 0.0f;
            Vector4[] occluderPoints = new Vector4[NUM_SAMPLES];
            Vector4[] occluderPointsFlipped = new Vector4[NUM_SAMPLES];
            for (uint i = 0; i < NUM_SAMPLES; i++)
            {
                Vector2 tapOffs = new Vector2(m_filterKernel[i].X * maxTapMagInv, m_filterKernel[i].Y * maxTapMagInv);

                occluderPoints[i].X = tapOffs.X;
                occluderPoints[i].Y = tapOffs.Y;
                occluderPoints[i].Z = 0;
                occluderPoints[i].W = (float)Math.Sqrt(1 - tapOffs.X * tapOffs.X - tapOffs.Y * tapOffs.Y);

                rsum += occluderPoints[i].W;

                //
                occluderPointsFlipped[i].X = tapOffs.X;
                occluderPointsFlipped[i].Y = -tapOffs.Y;
            }

            var colorScale = 1.0f / (2 * rsum);
            colorScale *= Params.ColorScale;


            for (uint i = 0; i < NUM_SAMPLES; i++)
            {
                stream.Write(occluderPoints[i]);
            }
            for (uint i = 0; i < NUM_SAMPLES; i++)
            {
                stream.Write(occluderPointsFlipped[i]);
            }
        }

        static PixelShaderId m_ps;

        internal static void RecreateShadersForSettings()
        {
            m_ps = MyShaders.CreatePs("ssao_0.hlsl", "volumetric_ssao2");
        }

        internal static void Run(MyBindableResource dst, MyGBuffer gbuffer, MyBindableResource resolvedDepth)
        {
            RC.Context.ClearRenderTargetView((dst as IRenderTargetBindable).RTV, new Color4(1, 1, 1, 1));

            var paramsCB = MyCommon.GetObjectCB(16 * (2 + NUM_SAMPLES * 2));

            var mapping = MyMapping.MapDiscard(paramsCB);
            mapping.stream.Write(Params.MinRadius);
            mapping.stream.Write(Params.MaxRadius);
            mapping.stream.Write(Params.RadiusGrow);
            mapping.stream.Write(Params.Falloff);
            mapping.stream.Write(Params.RadiusBias);
            mapping.stream.Write(Params.Contrast);
            mapping.stream.Write(Params.Normalization);
            mapping.stream.Write(0);
            FillRandomVectors(mapping.stream);
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
            MyRender11.RegisterSettingsChangedListener(RecreateShadersForSettings);
        }
    }
}
