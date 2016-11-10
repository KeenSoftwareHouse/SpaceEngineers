using Sandbox.Graphics.GUI;
using VRage;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Gui
{
    [MyDebugScreen("Render", "Postprocess Tonemap")]
    class MyGuiScreenDebugRenderPostprocessTonemap : MyGuiScreenDebugBase
    {
        public MyGuiScreenDebugRenderPostprocessTonemap()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;
            m_sliderDebugScale = 0.7f;

            AddCaption("Postprocess Tonemap", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);
            
            AddLabel("Tonemapping", Color.Yellow.ToVector4(), 1.2f);
            AddCheckBox("Enable", () => MyPostprocessSettingsWrapper.Settings.EnableTonemapping, (bool b) => MyPostprocessSettingsWrapper.Settings.EnableTonemapping = b);
            AddSlider("Constant Luminance", 0.0001f, 2.0f, () => MyPostprocessSettingsWrapper.Settings.Data.ConstantLuminance, (float f) => MyPostprocessSettingsWrapper.Settings.Data.ConstantLuminance = f);
            AddSlider("Exposure", -5, 5, () => MyPostprocessSettingsWrapper.Settings.Data.LuminanceExposure, (float f) => MyPostprocessSettingsWrapper.Settings.Data.LuminanceExposure = f);

            AddSlider("Saturation", 0, 5, () => MyPostprocessSettingsWrapper.Settings.Data.Saturation, (float f) => MyPostprocessSettingsWrapper.Settings.Data.Saturation = f);
            AddSlider("Brightness", 0, 5, () => MyPostprocessSettingsWrapper.Settings.Data.Brightness, (float f) => MyPostprocessSettingsWrapper.Settings.Data.Brightness = f);
            AddSlider("Brightness Factor R", 0, 1, () => MyPostprocessSettingsWrapper.Settings.Data.BrightnessFactorR, (float f) => MyPostprocessSettingsWrapper.Settings.Data.BrightnessFactorR = f);
            AddSlider("Brightness Factor G", 0, 1, () => MyPostprocessSettingsWrapper.Settings.Data.BrightnessFactorG, (float f) => MyPostprocessSettingsWrapper.Settings.Data.BrightnessFactorG = f);
            AddSlider("Brightness Factor B", 0, 1, () => MyPostprocessSettingsWrapper.Settings.Data.BrightnessFactorB, (float f) => MyPostprocessSettingsWrapper.Settings.Data.BrightnessFactorB = f);
            AddSlider("Contrast", -5, 5, () => MyPostprocessSettingsWrapper.Settings.Data.Contrast, (float f) => MyPostprocessSettingsWrapper.Settings.Data.Contrast = f);
            // FIXME
            //AddSlider("Temperature", 1000, 40000, () => MyPostprocessSettingsWrapper.Settings.Temperature, (float f) => MyPostprocessSettingsWrapper.Settings.Temperature = f);
            //AddSlider("Temperature Strength", 0, 1, () => MyPostprocessSettingsWrapper.Settings.Data.TemperatureStrength, (float f) => MyPostprocessSettingsWrapper.Settings.Data.TemperatureStrength = f);
            AddSlider("Vibrance", -1, 1, () => MyPostprocessSettingsWrapper.Settings.Data.Vibrance, (float f) => MyPostprocessSettingsWrapper.Settings.Data.Vibrance = f);
            m_currentPosition.Y += 0.01f;

            AddLabel("Sepia", Color.Yellow.ToVector4(), 1.2f);
            AddColor("Light Color", MyPostprocessSettingsWrapper.Settings.Data.LightColor, (v) => MyPostprocessSettingsWrapper.Settings.Data.LightColor = v.Color);
            AddColor("Dark Color", MyPostprocessSettingsWrapper.Settings.Data.DarkColor, (v) => MyPostprocessSettingsWrapper.Settings.Data.DarkColor = v.Color);
            AddSlider("Sepia Strength", 0, 1, () => MyPostprocessSettingsWrapper.Settings.Data.SepiaStrength, (float f) => MyPostprocessSettingsWrapper.Settings.Data.SepiaStrength = f);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugRenderPostprocessTonemap";
        }

    }
}
