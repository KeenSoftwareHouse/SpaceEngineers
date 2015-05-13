using SharpDX.Direct3D9;

namespace VRageRender.Effects
{
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Matrix = VRageMath.Matrix;
    using VRageRender.Utils;

    class MyEffectAmbientPrecalculation : MyEffectBase
    {
        readonly EffectHandle m_environmentMap;
        readonly EffectHandle m_halfPixel;
        readonly EffectHandle m_faceMatrix;
        readonly EffectHandle m_randomTexture;
        readonly EffectHandle m_randomTextureSize;
        readonly EffectHandle m_iterationCount;
        readonly EffectHandle m_mainVectorWeight;
        readonly EffectHandle m_backlightColorAndIntensity;

        public MyEffectAmbientPrecalculation()
            : base("Effects2\\Fullscreen\\MyEffectAmbientPrecalculation")
        {
            m_environmentMap = m_D3DEffect.GetParameter(null, "EnvironmentMap");
            m_halfPixel = m_D3DEffect.GetParameter(null, "HalfPixel");
            m_faceMatrix = m_D3DEffect.GetParameter(null, "FaceMatrix");
            m_randomTexture = m_D3DEffect.GetParameter(null, "RandomTexture");
            m_randomTextureSize = m_D3DEffect.GetParameter(null, "RandomTextureSize");
            m_iterationCount = m_D3DEffect.GetParameter(null, "IterationCount");
            m_mainVectorWeight = m_D3DEffect.GetParameter(null, "MainVectorWeight");
            m_backlightColorAndIntensity = m_D3DEffect.GetParameter(null, "BacklightColorAndIntensity");
        }

        public void SetRandomTexture(Texture randomTexture)
        {
            m_D3DEffect.SetTexture(m_randomTexture, randomTexture);
            m_D3DEffect.SetValue(m_randomTextureSize, randomTexture.GetLevelDescription(0).Width);
        }

        public void SetFaceMatrix(Matrix faceMatrix)
        {
            m_D3DEffect.SetValue(m_faceMatrix, faceMatrix);
        }

        public void SetEnvironmentMap(CubeTexture environmentMap)
        {
            m_D3DEffect.SetTexture(m_environmentMap, environmentMap);
            m_D3DEffect.SetValue(m_halfPixel, MyUtilsRender9.GetHalfPixel(environmentMap.GetLevelDescription(0).Width, environmentMap.GetLevelDescription(0).Height));
        }

        public void SetIterationCount(int iterationCount)
        {
            m_D3DEffect.SetValue(m_iterationCount, iterationCount);
        }

        public void SetMainVectorWeight(float mainVectorWeight)
        {
            m_D3DEffect.SetValue(m_mainVectorWeight, mainVectorWeight);
        }

        public void SetBacklightColorAndIntensity(Vector3 lightColor, float intensity)
        {
            m_D3DEffect.SetValue(m_backlightColorAndIntensity, new Vector4(lightColor.X, lightColor.Y, lightColor.Z, intensity));
        }

    }
}
