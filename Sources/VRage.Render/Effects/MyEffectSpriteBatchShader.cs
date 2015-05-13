using SharpDX.Direct3D9;

namespace VRageRender.Effects
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Matrix = VRageMath.Matrix;

    class MyEffectSpriteBatchShader : MyEffectBase
    {

        public enum Technique
        {
            Sprite,
            SpriteBatchCube0,
            SpriteBatchCube1,
            SpriteBatchCube2,
            SpriteBatchCube3,
            SpriteBatchCube4,
            SpriteBatchCube5,
        }


        readonly EffectHandle effectMatrixTransform;
        readonly EffectHandle effectTexture;
        readonly EffectHandle effectCubeTexture;

        readonly EffectHandle spriteTechnique;
        readonly EffectHandle spriteTechniqueCube0;
        readonly EffectHandle spriteTechniqueCube1;
        readonly EffectHandle spriteTechniqueCube2;
        readonly EffectHandle spriteTechniqueCube3;
        readonly EffectHandle spriteTechniqueCube4;
        readonly EffectHandle spriteTechniqueCube5;
        

        public MyEffectSpriteBatchShader()
            : base("Effects2\\Sprites\\SpriteEffect")
        {
            effectMatrixTransform = m_D3DEffect.GetParameter(null, "MatrixTransform");
            effectTexture = m_D3DEffect.GetParameter(null, "Texture");
            effectCubeTexture = m_D3DEffect.GetParameter(null, "SpriteTextureCube");

            spriteTechnique = m_D3DEffect.GetTechnique("SpriteBatch");
            spriteTechniqueCube0 = m_D3DEffect.GetTechnique("SpriteBatchCube0");
            spriteTechniqueCube1 = m_D3DEffect.GetTechnique("SpriteBatchCube1");
            spriteTechniqueCube2 = m_D3DEffect.GetTechnique("SpriteBatchCube2");
            spriteTechniqueCube3 = m_D3DEffect.GetTechnique("SpriteBatchCube3");
            spriteTechniqueCube4 = m_D3DEffect.GetTechnique("SpriteBatchCube4");
            spriteTechniqueCube5 = m_D3DEffect.GetTechnique("SpriteBatchCube5");

        }

        public void SetMatrixTransform(ref Matrix matrix)
        {
            m_D3DEffect.SetValue(effectMatrixTransform, matrix);
        }

        public void SetTexture(Texture textureToSet)
        {
            m_D3DEffect.SetTexture(effectTexture, textureToSet);
        }

        public void SetCubeTexture(CubeTexture textureToSet)
        {
            m_D3DEffect.SetTexture(effectCubeTexture, textureToSet);
        }

        public void SetTechnique(Technique technique)
        {
            switch (technique)
            {
                case Technique.Sprite:
                    m_D3DEffect.Technique = spriteTechnique;
                    break;

                case Technique.SpriteBatchCube0:
                    m_D3DEffect.Technique = spriteTechniqueCube0;
                    break;

                case Technique.SpriteBatchCube1:
                    m_D3DEffect.Technique = spriteTechniqueCube1;
                    break;

                case Technique.SpriteBatchCube2:
                    m_D3DEffect.Technique = spriteTechniqueCube2;
                    break;

                case Technique.SpriteBatchCube3:
                    m_D3DEffect.Technique = spriteTechniqueCube3;
                    break;

                case Technique.SpriteBatchCube4:
                    m_D3DEffect.Technique = spriteTechniqueCube4;
                    break;

                case Technique.SpriteBatchCube5:
                    m_D3DEffect.Technique = spriteTechniqueCube5;
                    break;

                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
            }
        }

    }

}
