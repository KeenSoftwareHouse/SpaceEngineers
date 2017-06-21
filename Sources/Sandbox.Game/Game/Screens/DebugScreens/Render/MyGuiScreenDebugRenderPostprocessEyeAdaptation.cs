using Sandbox.Graphics.GUI;
using VRage;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Gui
{
    [MyDebugScreen("Render", "Postprocess Eye Adaptation")]
    class MyGuiScreenDebugRenderPostprocessEyeAdaptation : MyGuiScreenDebugBase
    {
        public MyGuiScreenDebugRenderPostprocessEyeAdaptation()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;
            m_sliderDebugScale = 0.7f;

            AddCaption("Postprocess Eye Adaptation", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);
            
            AddLabel("Eye adaptation", Color.Yellow.ToVector4(), 1.2f);
            AddCheckBox("Enable", () => MyPostprocessSettingsWrapper.Settings.EnableEyeAdaptation, (bool b) => MyPostprocessSettingsWrapper.Settings.EnableEyeAdaptation = b);
            AddSlider("Tau", 0.0f, 10.0f, () => MyPostprocessSettingsWrapper.Settings.Data.EyeAdaptationTau, (float f) => MyPostprocessSettingsWrapper.Settings.Data.EyeAdaptationTau = f);
            AddSlider("LogLum cutoff", MyPostprocessSettingsWrapper.Settings.Data.LogLumThreshold, -16.0f, 16.0f, (x) => MyPostprocessSettingsWrapper.Settings.Data.LogLumThreshold = x.Value);
            AddCheckBox("Display Histogram", MyRenderProxy.Settings.DisplayHistogram, (x) => MyRenderProxy.Settings.DisplayHistogram = x.IsChecked);
            AddCheckBox("Display HDR intensity", MyRenderProxy.Settings.DisplayHdrIntensity, (x) => MyRenderProxy.Settings.DisplayHdrIntensity = x.IsChecked);
            m_currentPosition.Y += 0.01f;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugRenderPostprocessEyeAdaptation";
        }

        protected override void ValueChanged(MyGuiControlBase sender)
        {
            MyRenderProxy.SetSettingsDirty();
        }
    }
}
