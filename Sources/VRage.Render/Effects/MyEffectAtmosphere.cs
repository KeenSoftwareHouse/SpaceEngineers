using SharpDX.Direct3D9;
using System.Linq;
using VRageMath;
using VRageRender.Utils;

namespace VRageRender.Effects
{
    class MyEffectAtmosphere : MyEffectAtmosphereBase
    {
        readonly EffectHandle m_depthsRT;
        readonly EffectHandle m_colorRT;
        readonly EffectHandle m_viewMatrix;
        readonly EffectHandle m_worldMatrix;
        readonly EffectHandle m_projectionMatrix;
        readonly EffectHandle m_invViewMatrix;

        readonly EffectHandle m_halfPixel;
        readonly EffectHandle m_scale;
      
        EffectHandle m_normalTechnique;
        EffectHandle m_surfaceTechnique;

        public MyEffectAtmosphere()
            : base("Effects2\\Models\\MyEffectAtmosphere")
        {
            m_viewMatrix = m_D3DEffect.GetParameter(null, "ViewMatrix");
            m_worldMatrix = m_D3DEffect.GetParameter(null, "WorldMatrix");
            m_projectionMatrix = m_D3DEffect.GetParameter(null, "ProjectionMatrix");

            m_normalTechnique = m_D3DEffect.GetTechnique("Technique_RenderQualityNormal");
            m_surfaceTechnique = m_D3DEffect.GetTechnique("Technique_Surface");

            m_depthsRT = m_D3DEffect.GetParameter(null, "DepthsRT");
            m_colorRT = m_D3DEffect.GetParameter(null, "SourceRT");
            m_halfPixel = m_D3DEffect.GetParameter(null, "HalfPixel");
            m_scale = m_D3DEffect.GetParameter(null, "Scale");
            m_invViewMatrix = m_D3DEffect.GetParameter(null, "InvViewMatrix");

        }

        public void SetDepthsRT(Texture renderTarget2D)
        {
            m_D3DEffect.SetTexture(m_depthsRT, renderTarget2D);
        }

        public void SetSourceRT(Texture renderTarget2D)
        {
            m_D3DEffect.SetTexture(m_colorRT, renderTarget2D);
        }

        public void SetHalfPixel(int screenSizeX, int screenSizeY)
        {
            m_D3DEffect.SetValue(m_halfPixel, MyUtilsRender9.GetHalfPixel(screenSizeX, screenSizeY));
        }

        public void SetScale(Vector2 scale)
        {
            m_D3DEffect.SetValue(m_scale, scale);
        }

        public void SetWorldMatrix(Matrix worldMatrix)
        {
            m_D3DEffect.SetValue(m_worldMatrix, worldMatrix);
        }

        public override void SetViewMatrix(ref Matrix viewMatrix)
        {
            m_D3DEffect.SetValue(m_viewMatrix, viewMatrix);
        }

        public override void SetProjectionMatrix(ref Matrix projectionMatrix)
        {
            m_D3DEffect.SetValue(m_projectionMatrix, projectionMatrix);
        }

        public void SetTechnique(MyEffectModelsDNSTechniqueEnum technique)
        {
            switch (technique)
            {
                case MyEffectModelsDNSTechniqueEnum.Low:
                case MyEffectModelsDNSTechniqueEnum.Normal:
                case MyEffectModelsDNSTechniqueEnum.High:
                case MyEffectModelsDNSTechniqueEnum.Extreme:
                    m_D3DEffect.Technique = m_normalTechnique;
                    break;                                                 
            }
        }

        public void SetSurfaceTechnique()
        {
            m_D3DEffect.Technique = m_surfaceTechnique;
        }
 
        public void SetInvViewMatrix(Matrix viewMatrix)
        {
            m_D3DEffect.SetValue(m_invViewMatrix, viewMatrix);
        }

        public override void Begin(int pass, FX fx)
        {  
            VRageRender.Graphics.SamplerState.LinearWrap.Apply();
            base.Begin(pass, fx);
        }
    }
}
