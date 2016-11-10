using VRageMath;
using VRage;
using Sandbox.Game.World;

namespace Sandbox.Game.Gui
{
#if !XB1_TMP
    [MyDebugScreen("Render", "Postprocess HBAO")]
    class MyGuiScreenDebugRenderPostprocessHBAO : MyGuiScreenDebugBase
    {
        public MyGuiScreenDebugRenderPostprocessHBAO()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;
            m_sliderDebugScale = 0.7f;

            AddCaption("Postprocess HBAO", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

            AddCheckBox("Use HBAO", MySector.HBAOSettings.Enabled, (state) => MySector.HBAOSettings.Enabled = state.IsChecked);
            AddCheckBox("Show only HBAO", VRageRender.MyRenderProxy.Settings.DisplayAO, (x) => VRageRender.MyRenderProxy.Settings.DisplayAO = x.IsChecked);

            m_currentPosition.Y += 0.01f;
            AddSlider("Radius", MySector.HBAOSettings.Radius, 1, 8,
                (state) => MySector.HBAOSettings.Radius = state.Value);
            AddSlider("Bias", MySector.HBAOSettings.Bias, 0, 0.5f,
                (state) => MySector.HBAOSettings.Bias = state.Value);
            AddSlider("SmallScaleAO", MySector.HBAOSettings.SmallScaleAO, 0, 4,
                (state) => MySector.HBAOSettings.SmallScaleAO = state.Value);
            AddSlider("Falloff", MySector.HBAOSettings.LargeScaleAO, 0, 4,
                (state) => MySector.HBAOSettings.LargeScaleAO = state.Value);
            AddSlider("PowerExponent", MySector.HBAOSettings.PowerExponent, 1, 8,
                (state) => MySector.HBAOSettings.PowerExponent = state.Value);

            m_currentPosition.Y += 0.01f;
            AddCheckBox("ForegroundAOEnable", MySector.HBAOSettings.ForegroundAOEnable,
                (state) => MySector.HBAOSettings.ForegroundAOEnable = state.IsChecked);
            AddSlider("ForegroundViewDepth", MySector.HBAOSettings.ForegroundViewDepth, 0, 1000,
                (state) => MySector.HBAOSettings.ForegroundViewDepth = state.Value);

            m_currentPosition.Y += 0.01f;
            AddCheckBox("BackgroundAOEnable", MySector.HBAOSettings.BackgroundAOEnable,
                (state) => MySector.HBAOSettings.BackgroundAOEnable = state.IsChecked);
            AddCheckBox("AdaptToFOV", MySector.HBAOSettings.AdaptToFOV,
                (state) => MySector.HBAOSettings.AdaptToFOV = state.IsChecked);
            AddSlider("BackgroundViewDepth", MySector.HBAOSettings.BackgroundViewDepth, 0, 1000,
                (state) => MySector.HBAOSettings.BackgroundViewDepth = state.Value);

            m_currentPosition.Y += 0.01f;
            AddCheckBox("DepthClampToEdge", MySector.HBAOSettings.DepthClampToEdge, 
                (state) => MySector.HBAOSettings.DepthClampToEdge = state.IsChecked);

            m_currentPosition.Y += 0.01f;
            AddCheckBox("DepthThresholdEnable", MySector.HBAOSettings.DepthThresholdEnable,
                (state) => MySector.HBAOSettings.DepthThresholdEnable = state.IsChecked);
            AddSlider("DepthThreshold", MySector.HBAOSettings.DepthThreshold, 0, 1000,
                (state) => MySector.HBAOSettings.DepthThreshold = state.Value);
            AddSlider("DepthThresholdSharpness", MySector.HBAOSettings.DepthThresholdSharpness, 0, 500,
                (state) => MySector.HBAOSettings.DepthThresholdSharpness = state.Value);

            m_currentPosition.Y += 0.01f;
            AddCheckBox("Use blur", MySector.HBAOSettings.BlurEnable, (state) => MySector.HBAOSettings.BlurEnable = state.IsChecked);
            AddCheckBox("Radius 4", MySector.HBAOSettings.BlurRadius4, (state) => MySector.HBAOSettings.BlurRadius4 = state.IsChecked);
            AddSlider("Sharpness", MySector.HBAOSettings.BlurSharpness, 0, 100,
                (state) => MySector.HBAOSettings.BlurSharpness = state.Value);

            m_currentPosition.Y += 0.01f;
            AddCheckBox("Blur Sharpness Function", MySector.HBAOSettings.BlurSharpnessFunctionEnable, 
                (state) => MySector.HBAOSettings.BlurSharpnessFunctionEnable = state.IsChecked);
            AddSlider("ForegroundScale", MySector.HBAOSettings.BlurSharpnessFunctionForegroundScale, 0, 100,
                (state) => MySector.HBAOSettings.BlurSharpnessFunctionForegroundScale = state.Value);
            AddSlider("ForegroundViewDepth", MySector.HBAOSettings.BlurSharpnessFunctionForegroundViewDepth, 0, 1,
                (state) => MySector.HBAOSettings.BlurSharpnessFunctionForegroundViewDepth = state.Value);
            AddSlider("BackgroundViewDepth", MySector.HBAOSettings.BlurSharpnessFunctionBackgroundViewDepth, 0, 1,
                (state) => MySector.HBAOSettings.BlurSharpnessFunctionBackgroundViewDepth = state.Value);

            m_currentPosition.Y += 0.01f;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugRenderPostprocessHBAO";
        }

        protected override void ValueChanged(Graphics.GUI.MyGuiControlBase sender)
        {
            VRageRender.MyRenderProxy.SetSettingsDirty();
        }
    }
#endif
}
