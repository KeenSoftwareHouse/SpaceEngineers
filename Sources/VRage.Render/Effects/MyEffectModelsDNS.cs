using SharpDX.Direct3D9;
using System.Linq;

namespace VRageRender.Effects
{
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Matrix = VRageMath.Matrix;
    using VRageRender.Utils;
    using VRageMath;
    using VRageRender.Textures;

    class MyEffectModelsDNS : MyEffectBase
    {
        readonly EffectHandle m_viewMatrix;
        readonly EffectHandle m_worldMatrix;
        readonly EffectHandle m_projectionMatrix;
        readonly EffectHandle m_textureDiffuse;
        readonly EffectHandle m_textureNormal;
        readonly EffectHandle m_emissivity;
        readonly EffectHandle m_emissivityOffset;
        readonly EffectHandle m_emissivityUVAnim;
        readonly EffectHandle m_diffuseUVAnim;
        readonly EffectHandle m_diffuseColor;
        readonly EffectHandle m_specularIntensity;
        readonly EffectHandle m_specularPower;

        readonly EffectHandle m_ditheringTexture;
        readonly EffectHandle m_ditheringTextureSize;
        readonly EffectHandle m_depthTextureNear;
        readonly EffectHandle m_depthTextureFar;
        readonly EffectHandle m_halfPixel;
        readonly EffectHandle m_scale;

        readonly EffectHandle m_bones;
        readonly EffectHandle m_dithering;
        readonly EffectHandle m_time;

        readonly EffectHandle m_colorMaskEnabled;
        readonly EffectHandle m_colorMaskHSV;

        //readonly EffectHandle m_maskTexture;
        //readonly EffectHandle[] m_channelTexture;
        //readonly EffectHandle[] m_channelIntensities;

        public static int MaxBones = 60;


        public MyEffectDynamicLightingBase DynamicLights { get; private set; }
        public MyEffectReflectorBase Reflector { get; private set; }

        bool m_diffuseTextureSet = false;
        bool m_normalTextureSet = false;

        //Techniques 
        EffectHandle m_lowTechnique;
        EffectHandle m_lowBlendedTechnique;
        EffectHandle m_lowMaskedTechnique;

        EffectHandle m_normalTechnique;
        EffectHandle m_normalBlendedTechnique;
        EffectHandle m_normalMaskedTechnique;

        EffectHandle m_highTechnique;
        EffectHandle m_highBlendedTechnique;
        EffectHandle m_highMaskedTechnique;

        EffectHandle m_extremeTechnique;
        EffectHandle m_extremeBlendedTechnique;
        EffectHandle m_extremeMaskedTechnique;

        EffectHandle m_holoTechnique;
        EffectHandle m_holoIgnoreDepthTechnique;

        EffectHandle m_stencilTechnique;
        EffectHandle m_stencilLowTechnique;

        EffectHandle m_normalInstancedTechnique;

        EffectHandle m_highSkinnedTechnique;
        EffectHandle m_highInstancedTechnique;

        EffectHandle m_extremeSkinnedTechnique;
        EffectHandle m_extremeInstancedTechnique;

        EffectHandle m_normalInstancedSkinnedTechnique;
        EffectHandle m_highInstancedSkinnedTechnique;
        EffectHandle m_extremeInstancedSkinnedTechnique;

        EffectHandle m_instancedGenericTechnique;
        EffectHandle m_instancedGenericMaskedTechnique;

        float m_emissivityLocal = -1;
        float m_emissivityOffsetLocal;
        Vector2 m_emissivityUVAnimLocal;
        Vector2 m_diffuseUVAnimLocal;
        float m_specularIntensityLocal = -1;
        float m_specularPowerLocal = -1;
        int m_screenSizeXLocal;
        int m_screenSizeYLocal;
        Vector2 m_scaleLocal;


        public MyEffectModelsDNS()
            : base("Effects2\\Models\\MyEffectModelsDNS")
        {
            m_viewMatrix = m_D3DEffect.GetParameter(null, "ViewMatrix");
            m_worldMatrix = m_D3DEffect.GetParameter(null, "WorldMatrix");
            m_projectionMatrix = m_D3DEffect.GetParameter(null, "ProjectionMatrix");

            m_textureDiffuse = m_D3DEffect.GetParameter(null, "TextureDiffuse");
            m_textureNormal = m_D3DEffect.GetParameter(null, "TextureNormal");
            m_diffuseColor = m_D3DEffect.GetParameter(null, "DiffuseColor");
            m_emissivity = m_D3DEffect.GetParameter(null, "Emissivity");
            m_emissivityOffset = m_D3DEffect.GetParameter(null, "EmissivityOffset");
            m_emissivityUVAnim = m_D3DEffect.GetParameter(null, "EmissivityUVAnim");
            m_diffuseUVAnim = m_D3DEffect.GetParameter(null, "DiffuseUVAnim");
            m_specularIntensity = m_D3DEffect.GetParameter(null, "SpecularIntensity");
            m_specularPower = m_D3DEffect.GetParameter(null, "SpecularPower");

            m_ditheringTexture = m_D3DEffect.GetParameter(null, "TextureDithering");
            m_ditheringTextureSize = m_D3DEffect.GetParameter(null, "TextureDitheringSize");
            m_depthTextureNear = m_D3DEffect.GetParameter(null, "DepthTextureNear");
            m_depthTextureFar = m_D3DEffect.GetParameter(null, "DepthTextureFar");
            m_halfPixel = m_D3DEffect.GetParameter(null, "HalfPixel");
            m_scale = m_D3DEffect.GetParameter(null, "Scale");
            m_bones = m_D3DEffect.GetParameter(null, "Bones");
            m_dithering = m_D3DEffect.GetParameter(null, "Dithering");
            m_time = m_D3DEffect.GetParameter(null, "Time");
            m_colorMaskEnabled = m_D3DEffect.GetParameter(null, "ColorMaskEnabled");
            m_colorMaskHSV = m_D3DEffect.GetParameter(null, "ColorMaskHSV");

            m_lowTechnique = m_D3DEffect.GetTechnique("Technique_RenderQualityLow");
            //m_lowInstancedTechnique = m_xnaEffect.GetTechnique("Technique_RenderQualityLowInstanced");
            m_lowBlendedTechnique = m_D3DEffect.GetTechnique("Technique_RenderQualityLowBlended");
            m_lowMaskedTechnique = m_D3DEffect.GetTechnique("Technique_RenderQualityLowMasked");

            m_normalTechnique = m_D3DEffect.GetTechnique("Technique_RenderQualityNormal");
            m_normalBlendedTechnique = m_D3DEffect.GetTechnique("Technique_RenderQualityNormalBlended");
            m_normalMaskedTechnique = m_D3DEffect.GetTechnique("Technique_RenderQualityNormalMasked");

            m_highTechnique = m_D3DEffect.GetTechnique("Technique_RenderQualityHigh");
            m_highBlendedTechnique = m_D3DEffect.GetTechnique("Technique_RenderQualityHighBlended");
            m_highMaskedTechnique = m_D3DEffect.GetTechnique("Technique_RenderQualityHighMasked");

            m_extremeTechnique = m_D3DEffect.GetTechnique("Technique_RenderQualityHigh");
            m_extremeBlendedTechnique = m_D3DEffect.GetTechnique("Technique_RenderQualityHighBlended");
            m_extremeMaskedTechnique = m_D3DEffect.GetTechnique("Technique_RenderQualityHighMasked");

            m_holoTechnique = m_D3DEffect.GetTechnique("Technique_Holo");
            m_holoIgnoreDepthTechnique = m_D3DEffect.GetTechnique("Technique_Holo_IgnoreDepth");

            m_stencilTechnique = m_D3DEffect.GetTechnique("Technique_Stencil");
            m_stencilLowTechnique = m_D3DEffect.GetTechnique("Technique_StencilLow");

            m_normalInstancedTechnique = m_D3DEffect.GetTechnique("Technique_RenderQualityNormalInstanced");

            m_highSkinnedTechnique = m_D3DEffect.GetTechnique("Technique_RenderQualityHighSkinned");
            m_highInstancedTechnique = m_D3DEffect.GetTechnique("Technique_RenderQualityHighInstanced");

            m_extremeSkinnedTechnique = m_D3DEffect.GetTechnique("Technique_RenderQualityExtremeSkinned");
            m_extremeInstancedTechnique = m_D3DEffect.GetTechnique("Technique_RenderQualityExtremeInstanced");

            m_normalInstancedSkinnedTechnique = m_D3DEffect.GetTechnique("Technique_RenderQualityNormalInstancedSkinned");
            m_highInstancedSkinnedTechnique = m_D3DEffect.GetTechnique("Technique_RenderQualityHighInstancedSkinned");
            m_extremeInstancedSkinnedTechnique = m_D3DEffect.GetTechnique("Technique_RenderQualityExtremeInstancedSkinned");

            m_instancedGenericTechnique = m_D3DEffect.GetTechnique("Technique_InstancedGeneric");
            m_instancedGenericMaskedTechnique = m_D3DEffect.GetTechnique("Technique_InstancedGenericMasked");

            DynamicLights = new MyEffectDynamicLightingBase(m_D3DEffect);
            Reflector = new MyEffectReflectorBase(m_D3DEffect);
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

        public override void SetTextureDiffuse(Texture texture2D)
        {
            m_D3DEffect.SetTexture(m_textureDiffuse, texture2D);
            m_diffuseTextureSet = texture2D != null;
        }

        public void EnableColorMaskHsv(bool enable)
        {
            m_D3DEffect.SetValue(m_colorMaskEnabled, enable);
        }

        /// <summary>
        /// Hue is absolute, Saturation and Value is offset
        /// </summary>
        public void SetColorMaskHSV(Vector3 hsv)
        {
            m_D3DEffect.SetValue(m_colorMaskHSV, hsv);
        }

        public override void SetTextureNormal(Texture texture2D)
        {
            m_D3DEffect.SetTexture(m_textureNormal, texture2D);
            m_normalTextureSet = texture2D != null;
        }

        public override bool IsTextureDiffuseSet()
        {
            return m_diffuseTextureSet;
        }

        public override bool IsTextureNormalSet()
        {
            return m_normalTextureSet;
        }

        public override void SetDiffuseColor(Vector3 diffuseColor)
        {
            m_D3DEffect.SetValue(m_diffuseColor, diffuseColor);
        }
        public override void SetEmissivity(float emissivity)
        {
            if (m_emissivityLocal != emissivity)
            {
                m_D3DEffect.SetValue(m_emissivity, emissivity);
                m_emissivityLocal = emissivity;
            }
        }
        public override void SetEmissivityOffset(float emissivityOffset)
        {
            if (m_emissivityOffsetLocal != emissivityOffset)
            {
                m_D3DEffect.SetValue(m_emissivityOffset, emissivityOffset);
                m_emissivityOffsetLocal = emissivityOffset;
            }
        }
        public override void SetEmissivityUVAnim(Vector2 uvAnim)
        {
            if (m_emissivityUVAnimLocal != uvAnim)
            {
                m_D3DEffect.SetValue(m_emissivityUVAnim, uvAnim);
                m_emissivityUVAnimLocal = uvAnim;
            }
        }

        public override void SetDiffuseUVAnim(Vector2 uvAnim)
        {
            if (m_diffuseUVAnimLocal != uvAnim)
            {
                m_D3DEffect.SetValue(m_diffuseUVAnim, uvAnim);
                m_diffuseUVAnimLocal = uvAnim;
            }
        }


        public override void SetSpecularIntensity(float specularIntensity)
        {
            if (m_specularIntensityLocal != specularIntensity)
            {
                m_D3DEffect.SetValue(m_specularIntensity, specularIntensity);
                m_specularIntensityLocal = specularIntensity;
            }
        }
        public override void SetSpecularPower(float specularPower)
        {
            if (m_specularPowerLocal != specularPower)
            {
                m_D3DEffect.SetValue(m_specularPower, specularPower);
                m_specularPowerLocal = specularPower;
            }
        }

        public void SetDitheringTexture(Texture ditheringTexture)
        {
            m_D3DEffect.SetTexture(m_ditheringTexture, ditheringTexture);
            var desc = ditheringTexture.GetLevelDescription(0);
            m_D3DEffect.SetValue(m_ditheringTextureSize, new Vector2(desc.Width, desc.Height));
        }

        public void SetDepthTextureNear(Texture depthTextureNear)
        {
            m_D3DEffect.SetTexture(m_depthTextureNear, depthTextureNear);
        }

        public void SetDepthTextureFar(Texture depthTextureFar)
        {
            m_D3DEffect.SetTexture(m_depthTextureFar, depthTextureFar);
        }

        public void SetHalfPixel(int screenSizeX, int screenSizeY)
        {
            if (m_screenSizeXLocal != screenSizeX || m_screenSizeYLocal != screenSizeY)
            {
                m_D3DEffect.SetValue(m_halfPixel, MyUtilsRender9.GetHalfPixel(screenSizeX, screenSizeY));
                m_screenSizeXLocal = screenSizeX;
                m_screenSizeYLocal = screenSizeY;
            }
        }

        public void SetScale(Vector2 scale)
        {
            if (m_scaleLocal != scale)
            {
                m_D3DEffect.SetValue(m_scale, scale);
                m_scaleLocal = scale;
            }
        }

        public float Dithering
        {
            set { m_D3DEffect.SetValue(m_dithering, value); }
            get { return m_D3DEffect.GetValue<float>(m_dithering); }
        }

        public float Time
        {
            set { m_D3DEffect.SetValue(m_time, value); }
        }

        public void SetBones(Matrix[] boneTransforms)
        {
            m_D3DEffect.SetValue(m_bones, boneTransforms);
        }

        public void SetTechnique(MyEffectModelsDNSTechniqueEnum technique)
        {
            switch (technique)
            {
                case MyEffectModelsDNSTechniqueEnum.Low:
                case MyEffectModelsDNSTechniqueEnum.LowBlended:
                    m_D3DEffect.Technique = m_lowTechnique;
                    break;
                //case MyEffectModelsDNSTechniqueEnum.LowInstanced:
                //    m_xnaEffect.Technique = m_lowInstancedTechnique;
                //    break;
                //case MyEffectModelsDNSTechniqueEnum.LowBlended:
                //m_xnaEffect.Technique = m_lowBlendedTechnique;
                // break;
                case MyEffectModelsDNSTechniqueEnum.LowMasked:
                    m_D3DEffect.Technique = m_lowMaskedTechnique;
                    break;

                case MyEffectModelsDNSTechniqueEnum.Normal:
                    m_D3DEffect.Technique = m_normalTechnique;
                    break;
                case MyEffectModelsDNSTechniqueEnum.NormalBlended:
                    m_D3DEffect.Technique = m_normalBlendedTechnique;
                    break;
                case MyEffectModelsDNSTechniqueEnum.NormalMasked:
                    m_D3DEffect.Technique = m_normalMaskedTechnique;
                    break;

                case MyEffectModelsDNSTechniqueEnum.High:
                    m_D3DEffect.Technique = m_highTechnique;
                    break;
                case MyEffectModelsDNSTechniqueEnum.HighBlended:
                    m_D3DEffect.Technique = m_highBlendedTechnique;
                    break;
                case MyEffectModelsDNSTechniqueEnum.HighMasked:
                    m_D3DEffect.Technique = m_highMaskedTechnique;
                    break;                         /*
                case MyEffectModelsDNSTechniqueEnum.HighChannels:
                    m_xnaEffect.Technique = m_highChannelsTechnique;
                    break;                           */

                case MyEffectModelsDNSTechniqueEnum.Extreme:
                    m_D3DEffect.Technique = m_extremeTechnique;
                    break;
                //case MyEffectModelsDNSTechniqueEnum.ExtremeInstanced:
                //   m_xnaEffect.Technique = m_extremeInstancedTechnique;
                // break;
                case MyEffectModelsDNSTechniqueEnum.ExtremeBlended:
                    m_D3DEffect.Technique = m_extremeBlendedTechnique;
                    break;
                case MyEffectModelsDNSTechniqueEnum.ExtremeMasked:
                    m_D3DEffect.Technique = m_extremeMaskedTechnique;
                    break;                             /*
                case MyEffectModelsDNSTechniqueEnum.ExtremeChannels:
                    m_xnaEffect.Technique = m_extremeChannelsTechnique;
                    break;                               */

                case MyEffectModelsDNSTechniqueEnum.Holo:
                    m_D3DEffect.Technique = m_holoTechnique;
                    break;
                case MyEffectModelsDNSTechniqueEnum.HoloIgnoreDepth:
                    m_D3DEffect.Technique = m_holoIgnoreDepthTechnique;
                    break;

                case MyEffectModelsDNSTechniqueEnum.Stencil:
                    m_D3DEffect.Technique = m_stencilTechnique;
                    break;
                case MyEffectModelsDNSTechniqueEnum.StencilLow:
                    m_D3DEffect.Technique = m_stencilLowTechnique;
                    break;

                case MyEffectModelsDNSTechniqueEnum.NormalInstanced:
                    m_D3DEffect.Technique = m_normalInstancedTechnique;
                    break;

                case MyEffectModelsDNSTechniqueEnum.HighSkinned:
                    m_D3DEffect.Technique = m_highSkinnedTechnique;
                    break;

                case MyEffectModelsDNSTechniqueEnum.HighInstanced:
                    m_D3DEffect.Technique = m_highInstancedTechnique;
                    break;

                case MyEffectModelsDNSTechniqueEnum.ExtremeSkinned:
                    m_D3DEffect.Technique = m_extremeSkinnedTechnique;
                    break;

                case MyEffectModelsDNSTechniqueEnum.ExtremeInstanced:
                    m_D3DEffect.Technique = m_extremeInstancedTechnique;
                    break;

                case MyEffectModelsDNSTechniqueEnum.NormalInstancedSkinned:
                    m_D3DEffect.Technique = m_normalInstancedSkinnedTechnique;
                    break;

                case MyEffectModelsDNSTechniqueEnum.HighInstancedSkinned:
                    m_D3DEffect.Technique = m_highInstancedSkinnedTechnique;
                    break;

                case MyEffectModelsDNSTechniqueEnum.ExtremeInstancedSkinned:
                    m_D3DEffect.Technique = m_extremeInstancedSkinnedTechnique;
                    break;

                case MyEffectModelsDNSTechniqueEnum.InstancedGeneric:
                    m_D3DEffect.Technique = m_instancedGenericTechnique;
                    break;

                case MyEffectModelsDNSTechniqueEnum.InstancedGenericMasked:
                    m_D3DEffect.Technique = m_instancedGenericMaskedTechnique;
                    break;
            }
        }



        public override void Begin(int pass, FX fx)
        {       /*
            if (UseChannels && m_maskTexture != null)
            {
                SetTechnique(MyRenderConstants.RenderQualityProfile.ModelsChannelsTechnique);
            }
            else  */
            //{
            //SetTechnique(MyRenderConstants.RenderQualityProfile.ModelsRenderTechnique);
            //}

            VRageRender.Graphics.SamplerState.LinearWrap.Apply();

            base.Begin(pass, fx);
        }


        public void BeginBlended()
        {
            SetTechnique(MyRenderConstants.RenderQualityProfile.ModelsBlendedRenderTechnique);
        }

        public void ApplyHolo(bool ignoreDepth)
        {
            if (!ignoreDepth)
                SetTechnique(MyRenderConstants.RenderQualityProfile.ModelsHoloRenderTechnique);
            else
                SetTechnique(MyEffectModelsDNSTechniqueEnum.HoloIgnoreDepth);
        }


        public void ApplyMasked()
        {
            SetTechnique(MyRenderConstants.RenderQualityProfile.ModelsMaskedRenderTechnique);
        }

        public void ApplyStencil()
        {
            SetTechnique(MyRenderConstants.RenderQualityProfile.ModelsStencilTechnique);
        }

        public override void Dispose()
        {
            DynamicLights.Dispose();
            Reflector.Dispose();
            base.Dispose();
        }

    }

}