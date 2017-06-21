using VRageMath;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using VRageRender;

namespace Sandbox.Game.Gui
{
#if !XB1_TMP

    [MyDebugScreen("Render", "Environment Shadows")]
    class MyGuiScreenDebugRenderEnvironmentShadows : MyGuiScreenDebugBase
    {
        static float timeOfDay = 0f;
        static System.TimeSpan? OriginalTime = null;

        public MyGuiScreenDebugRenderEnvironmentShadows()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;
            m_sliderDebugScale = 0.7f;

            AddCaption("Environment Shadows", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.1f);

            var sunObj = MySector.SunProperties;

            AddLabel("Sun", Color.Yellow.ToVector4(), 1.2f);
            AddSlider("Time of day", 0.0f, MySession.Static.Settings.SunRotationIntervalMinutes,
                () => MyTimeOfDayHelper.TimeOfDay,
                MyTimeOfDayHelper.UpdateTimeOfDay
            );
            m_currentPosition.Y += 0.01f;

            AddSlider("Shadow fadeout", MySector.SunProperties.EnvironmentLight.ShadowFadeoutMultiplier, 0f, 1f, (x) => MySector.SunProperties.EnvironmentLight.ShadowFadeoutMultiplier = x.Value);
            AddSlider("Env Shadow fadeout", MySector.SunProperties.EnvironmentLight.EnvShadowFadeoutMultiplier, 0f, 1f, (x) => MySector.SunProperties.EnvironmentLight.EnvShadowFadeoutMultiplier = x.Value);
            m_currentPosition.Y += 0.01f;

            AddLabel("Ambient Occlusion", Color.Yellow.ToVector4(), 1.2f);
            AddSlider("IndirectLight", MySector.SunProperties.EnvironmentLight.AOIndirectLight, 0, 2.0f, (v) => MySector.SunProperties.EnvironmentLight.AOIndirectLight = v.Value);
            AddSlider("DirLight", MySector.SunProperties.EnvironmentLight.AODirLight, 0, 2.0f, (v) => MySector.SunProperties.EnvironmentLight.AODirLight = v.Value);
            AddSlider("AOPointLight", MySector.SunProperties.EnvironmentLight.AOPointLight, 0, 2.0f, (v) => MySector.SunProperties.EnvironmentLight.AOPointLight = v.Value);
            AddSlider("AOSpotLight", MySector.SunProperties.EnvironmentLight.AOSpotLight, 0, 2.0f, (v) => MySector.SunProperties.EnvironmentLight.AOSpotLight = v.Value);
            m_currentPosition.Y += 0.01f;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugRenderEnvironmentShadows";
        }
    }

#endif
}
