using VRageMath;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using VRageRender;

namespace Sandbox.Game.Gui
{
#if !XB1_TMP

    [MyDebugScreen("Render", "Environment settings")]
    class MyGuiScreenDebugRenderEnvironment : MyGuiScreenDebugBase
    {
        static float timeOfDay = 0f;
        static System.TimeSpan? OriginalTime = null;

        public MyGuiScreenDebugRenderEnvironment()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;

            AddCaption("Environment settings", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.1f);

            var sunObj = MySector.SunProperties;

            AddLabel("Sun", Color.Yellow.ToVector4(), 1.2f);
            AddSlider("Time of day", 0.0f, MySession.Static.Settings.SunRotationIntervalMinutes,
                () => MyTimeOfDayHelper.TimeOfDay,
                MyTimeOfDayHelper.UpdateTimeOfDay
            );

            AddSlider("Intensity", MySector.SunProperties.SunIntensity, 0, 10.0f, (v) => MySector.SunProperties.SunIntensity = v.Value);
            AddColor("Color", MySector.SunProperties.EnvironmentLight.SunColor, (v) => MySector.SunProperties.EnvironmentLight.SunColor = v.Color);
            AddSlider("Gloss factor", MySector.SunProperties.EnvironmentLight.SunGlossFactor, 0, 5.0f, (v) => MySector.SunProperties.EnvironmentLight.SunGlossFactor = v.Value);
            AddSlider("Diffuse factor", MySector.SunProperties.EnvironmentLight.SunDiffuseFactor, 0, 10.0f, (v) => MySector.SunProperties.EnvironmentLight.SunDiffuseFactor = v.Value);
            m_currentPosition.Y += 0.01f;

            AddLabel("Back light", Color.Yellow.ToVector4(), 1.2f);
            AddSlider("Intensity 1", MySector.SunProperties.BackLightIntensity1, 0, 10.0f, (v) => MySector.SunProperties.BackLightIntensity1 = v.Value);
            AddColor("Color 1", MySector.SunProperties.EnvironmentLight.BackLightColor1, (v) => MySector.SunProperties.EnvironmentLight.BackLightColor1 = v.Color);
            AddSlider("Intensity 2", MySector.SunProperties.BackLightIntensity2, 0, 10.0f, (v) => MySector.SunProperties.BackLightIntensity2 = v.Value);
            AddColor("Color 2", MySector.SunProperties.EnvironmentLight.BackLightColor2, (v) => MySector.SunProperties.EnvironmentLight.BackLightColor2 = v.Color);
            AddSlider("Specular factor", MySector.SunProperties.EnvironmentLight.BackLightGlossFactor, 0, 5.0f, (v) => MySector.SunProperties.EnvironmentLight.BackLightGlossFactor = v.Value);
            m_currentPosition.Y += 0.01f;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugRenderEnvironment";
        }

        protected override void ValueChanged(MyGuiControlBase sender)
        {
            base.ValueChanged(sender);

            var settings = new MyRenderFogSettings()
            {
                FogMultiplier = MySector.FogProperties.FogMultiplier,
                FogColor = MySector.FogProperties.FogColor,
                FogDensity = MySector.FogProperties.FogDensity
            };
            MyRenderProxy.UpdateFogSettings(ref settings);
        }
    }

#endif
}
