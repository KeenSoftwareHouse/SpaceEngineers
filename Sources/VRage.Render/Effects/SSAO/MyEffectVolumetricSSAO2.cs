using VRage.Utils;

using SharpDX.Direct3D9;

namespace VRageRender.Effects
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Matrix = VRageMath.Matrix;
    using VRageRender.Utils;

    class MyEffectVolumetricSSAO2 : MyEffectBase
    {
        public float ColorScale = 0.6f;

        readonly EffectHandle m_depthsRT;
        readonly EffectHandle m_halfPixel;
        readonly EffectHandle m_normalsTexture;
        readonly EffectHandle m_viewMatrix;
        readonly EffectHandle m_projCoef;
        readonly EffectHandle m_occlPos;
        readonly EffectHandle m_occlPosFlipped;
        readonly EffectHandle m_frustumCorners;
        readonly EffectHandle m_projectionMatrix;

        readonly EffectHandle m_params;
        readonly EffectHandle m_params2;
        readonly EffectHandle m_contrast;

        
        float m_colorScale = 0;

        const uint	NUM_SAMPLES = 8;

        readonly Vector2[] filterKernel =
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

        public MyEffectVolumetricSSAO2()
            : base("Effects2\\SSAO\\MyEffectVolumetricSSAO2")
        {
            m_depthsRT = m_D3DEffect.GetParameter(null, "DepthsRT");
            m_normalsTexture = m_D3DEffect.GetParameter(null, "NormalsTexture");
            m_halfPixel = m_D3DEffect.GetParameter(null, "HalfPixel");
            m_viewMatrix = m_D3DEffect.GetParameter(null, "ViewMatrix");
            m_projCoef = m_D3DEffect.GetParameter(null, "ProjViewPortCoef");
            m_occlPos = m_D3DEffect.GetParameter(null, "OcclPos");
            m_occlPosFlipped = m_D3DEffect.GetParameter(null, "OcclPosFlipped");
            m_frustumCorners = m_D3DEffect.GetParameter(null, "FrustumCorners");
            m_projectionMatrix = m_D3DEffect.GetParameter(null, "ProjectionMatrix");

            m_params = m_D3DEffect.GetParameter(null, "SSAOParams");
            m_params2 = m_D3DEffect.GetParameter(null, "SSAOParams2");
            m_contrast = m_D3DEffect.GetParameter(null, "Contrast");

            fillRandomVectors();
        }

        void fillRandomVectors()
        {
            float maxTapMag = -1;		
		    for (uint i = 0; i < NUM_SAMPLES; i++)
		    {
			    float curr = filterKernel[i].Length();
			    maxTapMag = (float)System.Math.Max(maxTapMag,curr);
		    }


		    float maxTapMagInv = 1.0f / maxTapMag;
		    float rsum = 0.0f;
		    Vector4[]	occluderPoints = new Vector4[NUM_SAMPLES];
		    Vector4[]	occluderPointsFlipped = new Vector4[NUM_SAMPLES];
		    for (uint i = 0; i < NUM_SAMPLES; i++)
		    {
			    Vector2	tapOffs = new Vector2(filterKernel[i].X * maxTapMagInv,filterKernel[i].Y * maxTapMagInv);

			    occluderPoints[i].X = tapOffs.X;
			    occluderPoints[i].Y = tapOffs.Y;
			    occluderPoints[i].Z = 0;
			    occluderPoints[i].W = (float)System.Math.Sqrt(1 - tapOffs.X * tapOffs.X - tapOffs.Y * tapOffs.Y);

			    rsum += occluderPoints[i].W;

			    //
			    occluderPointsFlipped[i].X = tapOffs.X;
			    occluderPointsFlipped[i].Y = -tapOffs.Y;
		    }

            m_colorScale = 1.0f / (2 * rsum); // 1 / Samples total volume
            m_colorScale *= ColorScale;

            SetOcclPos(occluderPoints);
            SetOcclPosFlipped(occluderPointsFlipped);
        }

        public void SetDepthsRT(Texture renderTarget2D)
        {
            m_D3DEffect.SetTexture(m_depthsRT, renderTarget2D);
        }

        public void SetHalfPixel(int screenSizeX, int screenSizeY)
        {
            m_D3DEffect.SetValue(m_halfPixel, MyUtilsRender9.GetHalfPixel(screenSizeX, screenSizeY));
        }

        public void SetNormalsTexture(Texture normalsTexture)
        {
            m_D3DEffect.SetTexture(m_normalsTexture, normalsTexture);
        }

        public void SetViewMatrix(Matrix viewMatrix)
        {
            m_D3DEffect.SetValue(m_viewMatrix, viewMatrix);
        }

        public void SetOcclPos(Vector4[] occlPos)
        {
            m_D3DEffect.SetValue(m_occlPos, occlPos);           
        }

        public void SetOcclPosFlipped(Vector4[] occlPosFlipped)
        {
            m_D3DEffect.SetValue(m_occlPosFlipped, occlPosFlipped);
        }

        public void SetParams1(Vector4 params1)
        {
            m_D3DEffect.SetValue(m_params, params1);
        }

        public void SetParams2(Vector4 params2)
        {
            params2.Z *= m_colorScale;
            m_D3DEffect.SetValue(m_params2, params2);
        }

        public void SetFrustumCorners(Vector3[] frustumCornersVS)
        {
            m_D3DEffect.SetValue(m_frustumCorners, frustumCornersVS);
        }

        public void SetProjectionMatrix(Matrix projectionMatrix)
        {
            m_D3DEffect.SetValue(m_projectionMatrix, projectionMatrix);
        }

        public void SetContrast(float contrast)
        {
            m_D3DEffect.SetValue(m_contrast, contrast);
        }

  
    }
}
