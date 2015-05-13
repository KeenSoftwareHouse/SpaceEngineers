using System;
using SharpDX.Direct3D9;

namespace VRageRender.Effects
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Matrix = VRageMath.Matrix;
    using VRageRender.Utils;

    class MyEffectGaussianBlur : MyEffectBase
    {
        readonly EffectHandle m_sourceTexture;
        readonly EffectHandle m_halfPixel;

        readonly EffectHandle m_weightsParameter;
        readonly EffectHandle m_offsetsParameter;
        readonly float[] m_sampleWeights;
        readonly Vector2[] m_sampleOffsets;
        readonly int m_sampleCount;

        //  Controls how much blurring is applied to the image. The typical range is from 1 up to 10 or so.
        public float BlurAmount = 1;

        public MyEffectGaussianBlur()
            : base("Effects2\\Fullscreen\\MyEffectGaussianBlur")
        {
            m_sourceTexture = m_D3DEffect.GetParameter(null, "SourceTexture");
            m_halfPixel = m_D3DEffect.GetParameter(null, "HalfPixel");

            // Look up the sample weight and offset effect parameters.
            m_weightsParameter = m_D3DEffect.GetParameter(null, "SampleWeights");
            m_offsetsParameter = m_D3DEffect.GetParameter(null, "SampleOffsets");

            // Look up how many samples our gaussian blur effect supports.
            m_sampleCount = m_D3DEffect.GetParameterDescription(m_weightsParameter).Elements;

            // Create temporary arrays for computing our filter settings.
            m_sampleWeights = new float[m_sampleCount];
            m_sampleOffsets = new Vector2[m_sampleCount];
        }

        public void SetSourceTexture(Texture texture2D)
        {
            m_D3DEffect.SetTexture(m_sourceTexture, texture2D);
        }

        public void SetHalfPixel(int screenSizeX, int screenSizeY)
        {
            m_D3DEffect.SetValue(m_halfPixel, MyUtilsRender9.GetHalfPixel(screenSizeX, screenSizeY));
        }

        public void SetWidthForHorisontalPass(int width)
        {
            // Pass 2: draw from rendertarget 1 into rendertarget 2,
            // using a shader to apply a horizontal gaussian blur filter.
            SetBlurEffectHandles(1.0f / (float)width, 0);
        }

        public void SetHeightForVerticalPass(int height)
        {
            // Pass 3: draw from rendertarget 2 back into rendertarget 1,
            // using a shader to apply a vertical gaussian blur filter.
            SetBlurEffectHandles(0, 1.0f / (float)height);
        }

        //  Computes sample weightings and texture coordinate offsets
        //  for one pass of a separable gaussian blur filter.
        void SetBlurEffectHandles(float dx, float dy)
        {
            // The first sample always has a zero offset.
            m_sampleWeights[0] = ComputeGaussian(0);
            m_sampleOffsets[0] = new Vector2(0);

            // Maintain a sum of all the weighting values.
            float totalWeights = m_sampleWeights[0];

            // Add pairs of additional sample taps, positioned
            // along a line in both directions from the center.
            for (int i = 0; i < m_sampleCount / 2; i++)
            {
                // Store weights for the positive and negative taps.
                float weight = ComputeGaussian(i + 1);

                m_sampleWeights[i * 2 + 1] = weight;
                m_sampleWeights[i * 2 + 2] = weight;

                totalWeights += weight * 2;

                // To get the maximum amount of blurring from a limited number of
                // pixel shader samples, we take advantage of the bilinear filtering
                // hardware inside the texture fetch unit. If we position our texture
                // coordinates exactly halfway between two texels, the filtering unit
                // will average them for us, giving two samples for the price of one.
                // This allows us to step in units of two texels per sample, rather
                // than just one at a time. The 1.5 offset kicks things off by
                // positioning us nicely in between two texels.
                float sampleOffset = i * 2 + 1.5f;

                Vector2 delta = new Vector2(dx, dy) * sampleOffset;

                // Store texture coordinate offsets for the positive and negative taps.
                m_sampleOffsets[i * 2 + 1] = delta;
                m_sampleOffsets[i * 2 + 2] = -delta;
            }

            // Normalize the list of sample weightings, so they will always sum to one.
            for (int i = 0; i < m_sampleWeights.Length; i++)
            {
                m_sampleWeights[i] /= totalWeights;
            }

            // Tell the effect about our new filter settings.
            m_D3DEffect.SetValue(m_weightsParameter, m_sampleWeights);
            m_D3DEffect.SetValue(m_offsetsParameter, m_sampleOffsets);
        }

        //  Evaluates a single point on the gaussian falloff curve.
        //  Used for setting up the blur filter weightings.
        float ComputeGaussian(float n)
        {
            float theta = BlurAmount;
            return (float)((1.0 / Math.Sqrt(2 * Math.PI * theta)) * Math.Exp(-(n * n) / (2 * theta * theta)));
        }
    }
}
