
using SharpDX.Direct3D9;

namespace VRageRender.Effects
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Matrix = VRageMath.Matrix;
    using System;

    class MyEffectDecals : MyEffectBase
    {
        readonly EffectHandle m_voxelMapPosition;
        readonly EffectHandle m_positionLocalScale;
        readonly EffectHandle m_positionLocalOffset;
        readonly EffectHandle m_techniqueVoxelDecals;
        readonly EffectHandle m_techniqueModelDecals;

        readonly EffectHandle m_decalDiffuseTexture;
        readonly EffectHandle m_decalNormalMapTexture;
        readonly EffectHandle m_fadeoutDistance;
        readonly EffectHandle m_worldMatrix;
        readonly EffectHandle m_viewProjectionMatrix;
        readonly EffectHandle m_emissivityColor;

        public MyEffectDynamicLightingBase DynamicLights { get; private set; }
        public MyEffectReflectorBase Reflector { get; private set; }


        public enum Technique
        {
            Voxels,
            Model,
        }

        public MyEffectDecals()
            : base("Effects2\\Decals\\MyDecalEffect")
        {
            m_voxelMapPosition = m_D3DEffect.GetParameter(null, "VoxelMapPosition");
            m_positionLocalOffset = m_D3DEffect.GetParameter(null, "PositionLocalOffset");
            m_positionLocalScale = m_D3DEffect.GetParameter(null, "PositionLocalScale");
            m_techniqueVoxelDecals = m_D3DEffect.GetTechnique("TechniqueVoxelDecals");
            m_techniqueModelDecals = m_D3DEffect.GetTechnique("TechniqueModelDecals");

            m_decalDiffuseTexture = m_D3DEffect.GetParameter(null, "DecalDiffuseTexture");
            m_decalNormalMapTexture = m_D3DEffect.GetParameter(null, "DecalNormalMapTexture");
            m_fadeoutDistance = m_D3DEffect.GetParameter(null, "FadeoutDistance");
            m_worldMatrix = m_D3DEffect.GetParameter(null, "WorldMatrix");
            m_viewProjectionMatrix = m_D3DEffect.GetParameter(null, "ViewProjectionMatrix");
            m_emissivityColor = m_D3DEffect.GetParameter(null, "EmissiveColor");

            DynamicLights = new MyEffectDynamicLightingBase(m_D3DEffect);
            Reflector = new MyEffectReflectorBase(m_D3DEffect);
        }

        public void SetDecalDiffuseTexture(Texture texture)
        {
            m_D3DEffect.SetTexture(m_decalDiffuseTexture, texture);
        }

        public void SetDecalNormalMapTexture(Texture texture)
        {
            m_D3DEffect.SetTexture(m_decalNormalMapTexture, texture);
        }

        public void SetWorldMatrix(Matrix matrix)
        {
            m_D3DEffect.SetValue(m_worldMatrix, matrix);
        }

        public void SetViewProjectionMatrix(Matrix matrix)
        {
            m_D3DEffect.SetValue(m_viewProjectionMatrix, matrix);
        }

        public void SetFadeoutDistance(float distance)
        {
            m_D3DEffect.SetValue(m_fadeoutDistance, distance);
        }

        public void SetVoxelMapPosition(Vector3 pos)
        {
            m_D3DEffect.SetValue(m_voxelMapPosition, pos);
        }

        public void SetPositionLocalOffset(Vector3 off)
        {
            m_D3DEffect.SetValue(m_positionLocalOffset, off);
        }

        public void SetPositionLocalScale(Vector3 off)
        {
            m_D3DEffect.SetValue(m_positionLocalScale, off);
        }

        public void SetEmissivityColor(Vector4 color)
        {
            m_D3DEffect.SetValue(m_emissivityColor, color);
        }

        public void SetTechnique(Technique technique)
        {
            switch (technique)
            {
                case Technique.Voxels:
                     m_D3DEffect.Technique = m_techniqueVoxelDecals;
                    break;
                case Technique.Model:
                     m_D3DEffect.Technique = m_techniqueModelDecals;
                    break;

                default:
                    throw new InvalidBranchException();
            }
        }

        public override void Dispose()
        {
            DynamicLights.Dispose();
            Reflector.Dispose();
            base.Dispose();
        }

    }
}
