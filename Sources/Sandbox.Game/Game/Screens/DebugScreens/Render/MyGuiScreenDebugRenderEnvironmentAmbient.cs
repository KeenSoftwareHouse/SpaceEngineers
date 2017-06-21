using VRageMath;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using VRage;
using VRageRender;
using VRageRender.Messages;

namespace Sandbox.Game.Gui
{
#if !XB1_TMP

    [MyDebugScreen("Render", "Environment Ambient")]
    class MyGuiScreenDebugRenderEnvironmentAmbient : MyGuiScreenDebugBase
    {
        static float timeOfDay = 0f;
        static System.TimeSpan? OriginalTime = null;

        public MyGuiScreenDebugRenderEnvironmentAmbient()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;
            m_sliderDebugScale = 0.7f;

            AddCaption("Environment Ambient", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.1f);

            AddLabel("Ambient", Color.Yellow.ToVector4(), 1.2f);
            AddSlider("Diffuse factor", MySector.SunProperties.EnvironmentLight.AmbientDiffuseFactor, 0, 5.0f, (v) => MySector.SunProperties.EnvironmentLight.AmbientDiffuseFactor = v.Value);
            AddSlider("Specular factor", MySector.SunProperties.EnvironmentLight.AmbientSpecularFactor, 0, 5.0f, (v) => MySector.SunProperties.EnvironmentLight.AmbientSpecularFactor = v.Value);
            AddSlider("Forward pass", MySector.SunProperties.EnvironmentLight.AmbientForwardPass, 0, 1.0f, (v) => MySector.SunProperties.EnvironmentLight.AmbientForwardPass = v.Value);
            AddSlider("Global minimum", MySector.SunProperties.EnvironmentLight.AmbientGlobalMinimum, 0, 0.1f, (v) => MySector.SunProperties.EnvironmentLight.AmbientGlobalMinimum = v.Value);
            AddSlider("Global density", MySector.SunProperties.EnvironmentLight.AmbientGlobalDensity, 0, 1.0f, (v) => MySector.SunProperties.EnvironmentLight.AmbientGlobalDensity = v.Value);
            AddSlider("Ambient global multiplier", MySector.SunProperties.EnvironmentLight.AmbientGlobalMultiplier, 0, 1.0f, (v) => MySector.SunProperties.EnvironmentLight.AmbientGlobalMultiplier = v.Value);
            m_currentPosition.Y += 0.01f;

            AddLabel("Skybox", Color.Yellow.ToVector4(), 1.2f);
            AddSlider("Intensity", MySector.SunProperties.EnvironmentLight.SkyboxBrightness, 0, 5.0f, (v) => MySector.SunProperties.EnvironmentLight.SkyboxBrightness = v.Value);
            AddSlider("Env Intensity", MySector.SunProperties.EnvironmentLight.EnvSkyboxBrightness, 0, 50.0f, (v) => MySector.SunProperties.EnvironmentLight.EnvSkyboxBrightness = v.Value);
            AddSlider("Env Atmosphere Intensity", MySector.SunProperties.EnvironmentLight.EnvAtmosphereBrightness, 0, 5.0f, (v) => MySector.SunProperties.EnvironmentLight.EnvAtmosphereBrightness = v.Value);

            AddLabel("Fog", Color.Yellow.ToVector4(), 1.2f);
            AddSlider("Fog multiplier", MySector.FogProperties.FogMultiplier, 0.0f, 0.5f, (x) => MySector.FogProperties.FogMultiplier = x.Value);
            AddSlider("Fog density", MySector.FogProperties.FogDensity, 0.0f, 0.2f, (x) => MySector.FogProperties.FogDensity = x.Value);
            AddColor("Fog color", MySector.FogProperties.FogColor, (x) => MySector.FogProperties.FogColor = x.Color);

            AddLabel("Display", Color.Yellow.ToVector4(), 1.2f);
            AddCheckBox("Environment map", MyRenderProxy.Settings.DisplayEnvProbe, (x) => MyRenderProxy.Settings.DisplayEnvProbe = x.IsChecked);
            AddCheckBox("Diffuse  (no postprocess)", MyRenderProxy.Settings.DisplayAmbientDiffuse, (x) => MyRenderProxy.Settings.DisplayAmbientDiffuse = x.IsChecked);
            AddCheckBox("Specular  (no postprocess)", MyRenderProxy.Settings.DisplayAmbientSpecular, (x) => MyRenderProxy.Settings.DisplayAmbientSpecular = x.IsChecked);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugRenderEnvironmentAmbient";
        }

        protected override void ValueChanged(MyGuiControlBase sender)
        {
            var settings = new MyRenderFogSettings()
            {
                FogMultiplier = MySector.FogProperties.FogMultiplier,
                FogColor = MySector.FogProperties.FogColor,
                FogDensity = MySector.FogProperties.FogDensity
            };
            MyRenderProxy.UpdateFogSettings(ref settings);
            MyRenderProxy.SetSettingsDirty();
        }
    }

#endif
}
