using System.Text;
using VRageMath;
using Sandbox.Engine.Utils;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using VRage;
using Sandbox.Graphics.Render;

namespace Sandbox.Game.Gui
{
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

            //m_currentPosition.Y += 0.01f;
            AddLabel("Sun", Color.Yellow.ToVector4(), 1.2f);

            var sunObj = MySector.SunProperties;
            //m_currentPosition.Y += 0.01f;
            AddSlider("Intensity", 0, 10.0f, sunObj, MemberHelper.GetMember(() => MySector.SunProperties.SunIntensity));

            //m_currentPosition.Y += 0.01f;
            AddColor(new StringBuilder("Sun color"), sunObj, MemberHelper.GetMember(() => MySector.SunProperties.SunDiffuse));

            //m_currentPosition.Y += 0.02f;
            AddColor(new StringBuilder("Sun specular"), sunObj, MemberHelper.GetMember(() => MySector.SunProperties.SunSpecular));

            //m_currentPosition.Y += 0.02f;
            //AddSlider("Back sun intensity", 0, 5.0f, sunObj, MemberHelper.GetMember(() => MySector.SunProperties.AdditionalSunIntensity));

            //m_currentPosition.Y += 0.01f;
            //AddColor(new StringBuilder("Back sun color"), sunObj, MemberHelper.GetMember(() => MySector.SunProperties.AdditionalSunDiffuse));

            //m_currentPosition.Y += 0.02f;
            AddColor(new StringBuilder("Ambient color"), sunObj, MemberHelper.GetMember(() => MySector.SunProperties.AmbientColor));

            //m_currentPosition.Y += 0.02f;
            AddSlider("Ambient multiplier", 0, 5.0f, sunObj, MemberHelper.GetMember(() => MySector.SunProperties.AmbientMultiplier));

            AddSlider("Env. ambient intensity", 0, 5.0f, sunObj, MemberHelper.GetMember(() => MySector.SunProperties.EnvironmentAmbientIntensity));

            /*
            //m_currentPosition.Y += 0.01f;
            AddLabel(new StringBuilder("Player ship"), Color.Yellow.ToVector4(), 1.2f);

            //m_currentPosition.Y += 0.01f;
            AddSlider(new StringBuilder("Light range multiplier"), 0, 10, null, MemberHelper.GetMember(() => MySmallShip.LightRangeMultiplier));
            AddSlider(new StringBuilder("Light intensity multiplier"), 0, 10, null, MemberHelper.GetMember(() => MySmallShip.LightIntensityMultiplier));
            AddSlider(new StringBuilder("Reflector intensity multiplier"), 0, 10, null, MemberHelper.GetMember(() => MySmallShip.ReflectorIntensityMultiplier));
            */
            //m_currentPosition.Y += 0.01f;
            AddLabel("Post process - Contrast", Color.Yellow.ToVector4(), 1.2f);
            //m_currentPosition.Y += 0.01f;
            {
                AddCheckBox("Enable", null, MemberHelper.GetMember(() => MyPostProcessContrast.Enabled));
                AddSlider("Hue", -1, 5, null, MemberHelper.GetMember(() => MyPostProcessContrast.Hue));
                AddSlider("Contrast", 0.1f, 2, null, MemberHelper.GetMember(() => MyPostProcessContrast.Contrast));
                AddSlider("Saturation", 0, 3, null, MemberHelper.GetMember(() => MyPostProcessContrast.Saturation));
            }

            //m_currentPosition.Y += 0.01f;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugRenderColors";
        }

    }
}
