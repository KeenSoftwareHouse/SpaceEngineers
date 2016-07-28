using System.Text;
using VRageMath;
using Sandbox.Engine.Utils;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using VRage;
using Sandbox.Graphics.Render;
using VRageRender;

namespace Sandbox.Game.Gui
{
#if !XB1

    [MyDebugScreen("Render", "Color settings")]
    class MyGuiScreenDebugColors : MyGuiScreenDebugBase
    {
        public static bool EnableRenderLights = true;

        public MyGuiScreenDebugColors()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.8f;

            AddCaption("Colors settings", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.1f);

            AddLabel("Sun", Color.Yellow.ToVector4(), 1.2f);

            var sunObj = MySector.SunProperties;
            AddSlider("Intensity", 0, 10.0f, sunObj, MemberHelper.GetMember(() => MySector.SunProperties.SunIntensity));
            AddColor(new StringBuilder("Sun color"), sunObj, MemberHelper.GetMember(() => MySector.SunProperties.SunDiffuse));

            bool rendererIsDirectX11 = MySandboxGame.Config.GraphicsRenderer.ToString().Equals("DirectX 11");
            if (!rendererIsDirectX11)
            {
                AddColor(new StringBuilder("Sun specular"), sunObj, MemberHelper.GetMember(() => MySector.SunProperties.SunSpecular));
                AddColor(new StringBuilder("Ambient color"), sunObj, MemberHelper.GetMember(() => MySector.SunProperties.AmbientColor));
                AddSlider("Ambient multiplier", 0, 5.0f, sunObj, MemberHelper.GetMember(() => MySector.SunProperties.AmbientMultiplier));
                AddSlider("Env. ambient intensity", 0, 5.0f, sunObj, MemberHelper.GetMember(() => MySector.SunProperties.EnvironmentAmbientIntensity));

                AddLabel("Post process - Contrast", Color.Yellow.ToVector4(), 1.2f);
                {
                    AddCheckBox("Enable", null, MemberHelper.GetMember(() => MyPostProcessContrast.Enabled));
                    AddSlider("Hue", -1, 5, null, MemberHelper.GetMember(() => MyPostProcessContrast.Hue));
                    AddSlider("Contrast", 0.1f, 2, null, MemberHelper.GetMember(() => MyPostProcessContrast.Contrast));
                    AddSlider("Saturation", 0, 3, null, MemberHelper.GetMember(() => MyPostProcessContrast.Saturation));
                }
            }
            else
            {
                const float minMultiplier = 0.0f;
                const float maxMultiplier = 4.0f;
                AddSlider("RGB Multiplier", minMultiplier, maxMultiplier, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.RgbMultiplier));
                AddSlider("Metalness Multiplier", minMultiplier, maxMultiplier, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.MetalnessMultiplier));
                AddSlider("Gloss Multiplier", minMultiplier, maxMultiplier, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.GlossMultiplier));
                AddSlider("AO Multiplier", minMultiplier, maxMultiplier, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.AoMultiplier));
                AddSlider("Emissive Multiplier", minMultiplier, maxMultiplier, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.EmissiveMultiplier));
                AddSlider("Color Mask Multiplier", minMultiplier, maxMultiplier, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.ColorMaskMultiplier));
            }
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugRenderColors";
        }

    }

#endif
}
