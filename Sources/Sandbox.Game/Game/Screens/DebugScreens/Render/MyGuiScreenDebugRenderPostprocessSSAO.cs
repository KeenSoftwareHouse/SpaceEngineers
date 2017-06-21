using VRageMath;
using VRage;
using Sandbox.Game.World;
using VRageRender;

namespace Sandbox.Game.Gui
{
#if !XB1_TMP
    [MyDebugScreen("Render", "Postprocess SSAO")]
    class MyGuiScreenDebugRenderPostprocessSSAO : MyGuiScreenDebugBase
    {
        public MyGuiScreenDebugRenderPostprocessSSAO()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            AddCaption("Postprocess SSAO", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_scale = 0.7f;
            m_sliderDebugScale = 0.7f;

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

            AddCheckBox("Use SSAO", MySector.SSAOSettings.Enabled, (state) => MySector.SSAOSettings.Enabled = state.IsChecked);
            AddCheckBox("Use blur", MySector.SSAOSettings.UseBlur, (state) => MySector.SSAOSettings.UseBlur = state.IsChecked);
            AddCheckBox("Show only SSAO", MyRenderProxy.Settings.DisplayAO, (x) => VRageRender.MyRenderProxy.Settings.DisplayAO = x.IsChecked);

            m_currentPosition.Y += 0.01f;

            AddSlider("MinRadius", MySector.SSAOSettings.Data.MinRadius, 0, 10, 
                (state) => MySector.SSAOSettings.Data.MinRadius = state.Value);
            AddSlider("MaxRadius", MySector.SSAOSettings.Data.MaxRadius, 0, 1000, 
                (state) => MySector.SSAOSettings.Data.MaxRadius = state.Value);
            AddSlider("RadiusGrowZScale", MySector.SSAOSettings.Data.RadiusGrowZScale, 0, 10, 
                (state) => MySector.SSAOSettings.Data.RadiusGrowZScale = state.Value);
            AddSlider("Falloff", MySector.SSAOSettings.Data.Falloff, 0, 10, 
                (state) => MySector.SSAOSettings.Data.Falloff = state.Value);
            AddSlider("RadiusBias", MySector.SSAOSettings.Data.RadiusBias, 0, 10, 
                (state) => MySector.SSAOSettings.Data.RadiusBias = state.Value);
            AddSlider("Contrast", MySector.SSAOSettings.Data.Contrast, 0, 10, 
                (state) => MySector.SSAOSettings.Data.Contrast = state.Value);
            AddSlider("Normalization", MySector.SSAOSettings.Data.Normalization, 0, 10, 
                (state) => MySector.SSAOSettings.Data.Normalization = state.Value);
            AddSlider("ColorScale", MySector.SSAOSettings.Data.ColorScale, 0, 1, 
                (state) => MySector.SSAOSettings.Data.ColorScale = state.Value);

            m_currentPosition.Y += 0.01f;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugRenderPostprocessSSAO";
        }

        protected override void ValueChanged(Graphics.GUI.MyGuiControlBase sender)
        {
            MyRenderProxy.SetSettingsDirty();
        }
    }
#endif
}
