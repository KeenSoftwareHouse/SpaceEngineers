using Sandbox.Graphics.GUI;
using Sandbox.Graphics.Render;
using System.Text;
using VRage;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Gui
{
    [MyDebugScreen("Render", "Image settings", MyDirectXSupport.DX11)]
    class MyGuiScreenDebugImageProcessing : MyGuiScreenDebugBase
    {
        public MyGuiScreenDebugImageProcessing()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;

            AddCaption("Render debug 4", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);


            m_currentPosition.Y += 0.01f;
            AddLabel("Image processing", Color.Yellow.ToVector4(), 1.2f);
            AddCheckBox("Enable tonemapping", () => MyPostprocessSettingsWrapper.Settings.EnableTonemapping, (bool b) => MyPostprocessSettingsWrapper.Settings.EnableTonemapping = b);
            AddCheckBox("Enable eye adaptation", () => MyPostprocessSettingsWrapper.Settings.EnableEyeAdaptation, (bool b) => MyPostprocessSettingsWrapper.Settings.EnableEyeAdaptation = b);
            AddSlider("Constant Luminance", 0.0001f, 0.1f, () => MyPostprocessSettingsWrapper.Settings.ConstantLuminance, (float f) => MyPostprocessSettingsWrapper.Settings.ConstantLuminance = f);
            AddSlider("Eye adaptation Tau", 0.0f, 10.0f, () => MyPostprocessSettingsWrapper.Settings.EyeAdaptationTau, (float f) => MyPostprocessSettingsWrapper.Settings.EyeAdaptationTau = f);
            AddSlider("Exposure", -5.0f, 5.0f, () => MyPostprocessSettingsWrapper.Settings.LuminanceExposure, (float f) => MyPostprocessSettingsWrapper.Settings.LuminanceExposure = f);
            AddSlider("Contrast", -0.5f, 0.5f, () => MyPostprocessSettingsWrapper.Settings.Contrast, (float f) => MyPostprocessSettingsWrapper.Settings.Contrast = f);
            AddSlider("Brightness", -0.5f, 0.5f, () => MyPostprocessSettingsWrapper.Settings.Brightness, (float f) => MyPostprocessSettingsWrapper.Settings.Brightness = f);
            AddSlider("Bloom exposure", -5.0f, 5.0f, () => MyPostprocessSettingsWrapper.Settings.BloomExposure, (float f) => MyPostprocessSettingsWrapper.Settings.BloomExposure = f);
            AddSlider("Bloom magnitude", 0.0f, 2.0f, () => MyPostprocessSettingsWrapper.Settings.BloomMult, (float f) => MyPostprocessSettingsWrapper.Settings.BloomMult = f);
            AddSlider("BlueShiftRapidness", 0.0001f, 0.1f, () => MyPostprocessSettingsWrapper.Settings.BlueShiftRapidness, (float f) => MyPostprocessSettingsWrapper.Settings.BlueShiftRapidness = f);
            AddSlider("BlueShiftScale", 0.0f, 1.0f, () => MyPostprocessSettingsWrapper.Settings.BlueShiftScale, (float f) => MyPostprocessSettingsWrapper.Settings.BlueShiftScale = f);

            //m_currentPosition.Y += 0.01f;
            //AddLabel("Hacks", Color.Yellow.ToVector4(), 1.2f);
            //AddSlider("Environment multiplier", 0.0f, 10.0f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.EnvMult));
            //AddSlider("Backlight intensity", 0.0f, 10.0f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.BacklightMult));

            m_currentPosition.Y += 0.01f;
#if !XB1
            AddCheckBox("Display HDR debug", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DisplayHdrDebug));
#endif // !XB1
            AddSlider("MiddleGrey (0=automatic)", 0.0f, 1.0f, () => MyPostprocessSettingsWrapper.Settings.MiddleGrey, (float f) => MyPostprocessSettingsWrapper.Settings.MiddleGrey = f);
            AddSlider("MiddleGreyCurveSharpness", 1.0f, 10.0f, () => MyPostprocessSettingsWrapper.Settings.MiddleGreyCurveSharpness, (float f) => MyPostprocessSettingsWrapper.Settings.MiddleGreyCurveSharpness = f);
            AddSlider("MiddleGreyAt0", 0.0f, 0.5f, () => MyPostprocessSettingsWrapper.Settings.MiddleGreyAt0, (float f) => MyPostprocessSettingsWrapper.Settings.MiddleGreyAt0 = f);
            AddSlider("A", 0.0f, 1.0f, () => MyPostprocessSettingsWrapper.Settings.Tonemapping_A, (float f) => MyPostprocessSettingsWrapper.Settings.Tonemapping_A = f);
            AddSlider("B", 0.0f, 1.0f, () => MyPostprocessSettingsWrapper.Settings.Tonemapping_B, (float f) => MyPostprocessSettingsWrapper.Settings.Tonemapping_B = f);
            AddSlider("C", 0.0f, 1.0f, () => MyPostprocessSettingsWrapper.Settings.Tonemapping_C, (float f) => MyPostprocessSettingsWrapper.Settings.Tonemapping_C = f);
            AddSlider("D", 0.0f, 1.0f, () => MyPostprocessSettingsWrapper.Settings.Tonemapping_D, (float f) => MyPostprocessSettingsWrapper.Settings.Tonemapping_D = f);
            AddSlider("E", 0.0f, 1.0f, () => MyPostprocessSettingsWrapper.Settings.Tonemapping_E, (float f) => MyPostprocessSettingsWrapper.Settings.Tonemapping_E = f);
            AddSlider("F", 0.0f, 1.0f, () => MyPostprocessSettingsWrapper.Settings.Tonemapping_F, (float f) => MyPostprocessSettingsWrapper.Settings.Tonemapping_F = f);
            AddSlider("LogLum cutoff", -16.0f, 16.0f, () => MyPostprocessSettingsWrapper.Settings.LogLumThreshold, (float f) => MyPostprocessSettingsWrapper.Settings.LogLumThreshold = f);

            //AddLabel("Fog", Color.Yellow.ToVector4(), 1.2f);
            //AddSlider("Mult", 0.0f, 2.0f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.FogMult));
            //AddSlider("Density", 0.0000001f, 0.05f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.FogDensity));
            //AddSlider("Height offset", 0.0f, 1000.0f, MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.FogYOffset));
            //AddColor(new StringBuilder("Color"), MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.FogColor));
        }

        protected override void ValueChanged(MyGuiControlBase sender)
        {
            base.ValueChanged(sender);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugRender4";
        }

    }
}
