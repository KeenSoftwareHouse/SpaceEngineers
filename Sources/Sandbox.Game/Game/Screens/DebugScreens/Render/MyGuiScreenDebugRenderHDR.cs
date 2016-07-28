using System.Text;
using VRageMath;
using Sandbox.Engine.Utils;
using Sandbox.Graphics.GUI;
using VRage;
using Sandbox.Graphics.Render;
using VRageRender;

namespace Sandbox.Game.Gui
{

#if !XB1

    [MyDebugScreen("Render", "HDR", MyDirectXSupport.ALL)]
    class MyGuiScreenDebugRenderHDR : MyGuiScreenDebugBase
    {
        public MyGuiScreenDebugRenderHDR()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;

            AddCaption("Render HDR", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

            m_currentPosition.Y += 0.01f * m_scale;

            if (MySandboxGame.Config.GraphicsRenderer == MySandboxGame.DirectX9RendererKey)
            {
                AddLabel("HDR", Color.Yellow.ToVector4(), 1.2f);

                AddCheckBox("Enable HDR and bloom", null, MemberHelper.GetMember(() => MyPostProcessHDR.DebugHDRChecked));

                m_currentPosition.Y += 0.01f * m_scale;

                AddSlider("Exposure", 0, 6.0f, null, MemberHelper.GetMember(() => MyPostProcessHDR.Exposure));
                AddSlider("Bloom Threshold", 0, 4.0f, null, MemberHelper.GetMember(() => MyPostProcessHDR.Threshold));
                AddSlider("Bloom Intensity", 0, 4.0f, null, MemberHelper.GetMember(() => MyPostProcessHDR.BloomIntensity));
                AddSlider("Bloom Intensity for Background", 0, 1.5f, null, MemberHelper.GetMember(() => MyPostProcessHDR.BloomIntensityBackground));
                AddSlider("Vertical Blur Amount", 1.0f, 8.0f, null, MemberHelper.GetMember(() => MyPostProcessHDR.VerticalBlurAmount));
                AddSlider("Horizontal Blur Amount", 1.0f, 8.0f, null, MemberHelper.GetMember(() => MyPostProcessHDR.HorizontalBlurAmount));
                AddSlider("Number of blur passes (integer)", 1.0f, 8.0f, null, MemberHelper.GetMember(() => MyPostProcessHDR.NumberOfBlurPasses));
            }

            m_currentPosition.Y += 0.01f * m_scale;

            AddCheckBox("Display HDR debug", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DisplayHdrDebug));
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
            AddSlider("Night LogLum cutoff", -16.0f, 16.0f, () => MyPostprocessSettingsWrapper.Settings.NightLogLumThreshold, (float f) => MyPostprocessSettingsWrapper.Settings.NightLogLumThreshold = f);

            m_currentPosition.Y += 0.01f * m_scale;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugRenderHDR";
        }

        protected override void ValueChanged(MyGuiControlBase sender)
        {
            base.ValueChanged(sender);

            VRageRender.MyRenderProxy.UpdateHDRSettings(
                MyPostProcessHDR.DebugHDRChecked,
                MyPostProcessHDR.Exposure,
                MyPostProcessHDR.Threshold,
                MyPostProcessHDR.BloomIntensity,
                MyPostProcessHDR.BloomIntensityBackground,
                MyPostProcessHDR.VerticalBlurAmount,
                MyPostProcessHDR.HorizontalBlurAmount,
                (int)MyPostProcessHDR.NumberOfBlurPasses
                );
        }
    }

#endif
}
