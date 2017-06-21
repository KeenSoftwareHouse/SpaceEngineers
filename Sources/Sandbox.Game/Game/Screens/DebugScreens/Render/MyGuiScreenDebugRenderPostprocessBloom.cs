using Sandbox.Game.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Gui
{
    [MyDebugScreen("Render", "Postprocess Bloom")]
    class MyGuiScreenDebugRenderPostprocessBloom : MyGuiScreenDebugBase
    {
        public MyGuiScreenDebugRenderPostprocessBloom()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;
            m_sliderDebugScale = 0.7f;

            AddCaption("Postprocess Bloom", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

            AddLabel("Bloom", Color.Yellow.ToVector4(), 1.2f);
            AddCheckBox("Display filter", MyRenderProxy.Settings.DisplayBloomFilter, (x) => MyRenderProxy.Settings.DisplayBloomFilter = x.IsChecked);
            AddCheckBox("Display min", MyRenderProxy.Settings.DisplayBloomMin, (x) => MyRenderProxy.Settings.DisplayBloomMin = x.IsChecked);
            AddSlider("Exposure", 0.0f, 10.0f, () => MyPostprocessSettingsWrapper.Settings.Data.BloomExposure, (float f) => MyPostprocessSettingsWrapper.Settings.Data.BloomExposure = f);
            AddSlider("Luma threshold", 0.0f, 5.0f, () => MyPostprocessSettingsWrapper.Settings.Data.BloomLumaThreshold, (float f) => MyPostprocessSettingsWrapper.Settings.Data.BloomLumaThreshold = f);
            AddSlider("Emissiveness", 0.0f, 5.0f, () => MyPostprocessSettingsWrapper.Settings.Data.BloomEmissiveness, (float f) => MyPostprocessSettingsWrapper.Settings.Data.BloomEmissiveness = f);
            AddSlider("Size", 0, 10, () => MyPostprocessSettingsWrapper.Settings.BloomSize, (float f) => MyPostprocessSettingsWrapper.Settings.BloomSize = (int)f);
            AddSlider("Depth slope", 0.0f, 5.0f, () => MyPostprocessSettingsWrapper.Settings.Data.BloomDepthSlope, (float f) => MyPostprocessSettingsWrapper.Settings.Data.BloomDepthSlope = f);
            AddSlider("Depth strength", 0.0f, 4.0f, () => MyPostprocessSettingsWrapper.Settings.Data.BloomDepthStrength, (float f) => MyPostprocessSettingsWrapper.Settings.Data.BloomDepthStrength = f);
            AddSlider("Magnitude", 0.0f, 2.0f, () => MyPostprocessSettingsWrapper.Settings.Data.BloomMult, (float f) => MyPostprocessSettingsWrapper.Settings.Data.BloomMult = f);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugRenderPostprocessBloom";
        }

        protected override void ValueChanged(Graphics.GUI.MyGuiControlBase sender)
        {
            MyRenderProxy.SetSettingsDirty();
        }
    }
}
