using System;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using VRage;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Gui
{

#if !XB1
    [MyDebugScreen("Render", "Shadows")]
    class MyGuiScreenDebugRenderShadows : MyGuiScreenDebugBase
    {
		public MyGuiScreenDebugRenderShadows()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;
            m_sliderDebugScale = 0.7f;

            AddCaption("Shadows", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);
            m_currentPosition.Y += 0.01f;
            AddLabel("Old shadows", Color.Yellow.ToVector4(), 1.2f);
            AddCheckBox("Enable Shadows", () => MyRenderProxy.Settings.EnableShadows, (newValue) => { MyRenderProxy.Settings.EnableShadows = newValue; });
            AddCheckBox("Enable Shadow Blur", () => MySector.ShadowSettings.Data.EnableShadowBlur, (newValue) => { MySector.ShadowSettings.Data.EnableShadowBlur = newValue; });

            m_currentPosition.Y += 0.01f;
            AddLabel("Shadow cascades", Color.Yellow.ToVector4(), 1.2f);

            AddCheckBox("Force per-frame updating", MySector.ShadowSettings.Data.UpdateCascadesEveryFrame, (x) => MySector.ShadowSettings.Data.UpdateCascadesEveryFrame = x.IsChecked);
            AddCheckBox("Show cascade splits", MyRenderProxy.Settings.DisplayShadowsWithDebug, (x) => MyRenderProxy.Settings.DisplayShadowsWithDebug = x.IsChecked);
            AddCheckBox("Show cascade textures", MyRenderProxy.Settings.DrawCascadeTextures, (x) => MyRenderProxy.Settings.DrawCascadeTextures = x.IsChecked);
            AddCheckBox("Display frozen cascades", MySector.ShadowSettings.Data.DisplayFrozenShadowCascade, (x) => MySector.ShadowSettings.Data.DisplayFrozenShadowCascade = x.IsChecked);
            for (int cascadeIndex = 0; cascadeIndex < MySector.ShadowSettings.NewData.CascadesCount; ++cascadeIndex)
            {
                int captureIndex = cascadeIndex;
                AddCheckBox("Freeze cascade " + cascadeIndex.ToString(), MySector.ShadowSettings.ShadowCascadeFrozen[captureIndex], (x) => MySector.ShadowSettings.ShadowCascadeFrozen[captureIndex] = x.IsChecked);
            }

            AddSlider("Max base shadow cascade distance", MySector.ShadowSettings.Data.ShadowCascadeMaxDistance, 1.0f, 20000.0f, (x) => MySector.ShadowSettings.Data.ShadowCascadeMaxDistance = x.Value);
            AddSlider("Back offset", MySector.ShadowSettings.Data.ShadowCascadeZOffset, 1.0f, 10000.0f, (x) => MySector.ShadowSettings.Data.ShadowCascadeZOffset = x.Value);
            AddSlider("Spread factor", MySector.ShadowSettings.Data.ShadowCascadeSpreadFactor, 0.0f, 2.0f, (x) => MySector.ShadowSettings.Data.ShadowCascadeSpreadFactor = x.Value);

            AddLabel("New shadows (disabled)", Color.Yellow.ToVector4(), 1.2f);
            AddCheckBox("Freeze sun direction", MySector.ShadowSettings.NewData.FreezeSunDirection,
                (x) => MySector.ShadowSettings.NewData.FreezeSunDirection = x.IsChecked);
            AddCheckBox("Freeze shadow volume positions", MySector.ShadowSettings.NewData.FreezeShadowVolumePositions,
                (x) => MySector.ShadowSettings.NewData.FreezeShadowVolumePositions = x.IsChecked);
            AddCheckBox("Freeze shadow maps", MySector.ShadowSettings.NewData.FreezeShadowMaps,
                (x) => MySector.ShadowSettings.NewData.FreezeShadowMaps = x.IsChecked);
            AddCheckBox("Show cascade textures", MyRenderProxy.Settings.DrawCascadeTextures, (x) => MyRenderProxy.Settings.DrawCascadeTextures = x.IsChecked);
            AddCheckBox("Draw cascade coverage", MySector.ShadowSettings.NewData.DisplayCascadeCoverage,
                (x) => MySector.ShadowSettings.NewData.DisplayCascadeCoverage = x.IsChecked);
            AddCheckBox("Display hard shadows", MySector.ShadowSettings.NewData.DisplayHardShadows,
                (x) => MySector.ShadowSettings.NewData.DisplayHardShadows = x.IsChecked);
            AddCheckBox("Display simple shadows", MySector.ShadowSettings.NewData.DisplaySimpleShadows,
                (x) => MySector.ShadowSettings.NewData.DisplaySimpleShadows = x.IsChecked);
            AddCheckBox("Draw volumes", MySector.ShadowSettings.NewData.DrawVolumes, (x) => MySector.ShadowSettings.NewData.DrawVolumes = x.IsChecked);

            m_currentPosition.Y += 0.01f;
            AddLabel("Global calibration", Color.Yellow.ToVector4(), 1.2f);
            AddCheckBox("Stabilize movement", MySector.ShadowSettings.NewData.StabilizeMovement, (x) => MySector.ShadowSettings.NewData.StabilizeMovement = x.IsChecked);
            AddCheckBox("Stabilize rotation", MySector.ShadowSettings.NewData.StabilizeRotation, (x) => MySector.ShadowSettings.NewData.StabilizeRotation = x.IsChecked);
            AddCheckBox("Enable FXAA on shadows", MySector.ShadowSettings.NewData.EnableFXAAOnShadows,
                (x) => MySector.ShadowSettings.NewData.EnableFXAAOnShadows = x.IsChecked);
            AddSlider("Sun angle threshold", MySector.ShadowSettings.NewData.SunAngleThreshold, 0, 0.5f,
                (x) => MySector.ShadowSettings.NewData.SunAngleThreshold = x.Value);
            AddSlider("Z offset toward to sun", MySector.ShadowSettings.NewData.ZOffset, 1, 100000,
                (x) => MySector.ShadowSettings.NewData.ZOffset = x.Value);
            AddSlider("Small objects threshold (broken)", 0, 0, 1000, OnChangeSmallObjectsThreshold);

            m_currentPosition.Y += 0.01f;
            AddLabel("Cascade calibration", Color.Yellow.ToVector4(), 1.2f);
            m_checkboxHigherRange = AddCheckBox("Use higher values", () => true, CheckboxHigherRangeChanged);
            AddSlider("Selected volume", 0, MySector.ShadowSettings.NewData.CascadesCount, GetSelectedVolume, SetSelectedVolume);
            m_sliderFullCoveredDepth = AddSlider("Full covered depth", MySector.ShadowSettings.Cascades[m_selectedVolume].FullCoverageDepth, 1, 200,
                (x) => MySector.ShadowSettings.Cascades[m_selectedVolume].FullCoverageDepth = x.Value);
            m_sliderExtCoveredDepth = AddSlider("Extended covered depth", MySector.ShadowSettings.Cascades[m_selectedVolume].ExtendedCoverageDepth, 0, 100,
                (x) => MySector.ShadowSettings.Cascades[m_selectedVolume].ExtendedCoverageDepth = x.Value);
            m_sliderShadowNormalOffset = AddSlider("Shadow normal offset", MySector.ShadowSettings.Cascades[m_selectedVolume].ShadowNormalOffset, 0, 1,
                (x) => MySector.ShadowSettings.Cascades[m_selectedVolume].ShadowNormalOffset = x.Value);

            CheckboxHigherRangeChanged(true); // update intervals
        }

        int m_selectedVolume = 0;
        MyGuiControlCheckbox m_checkboxHigherRange;
        MyGuiControlSlider m_sliderFullCoveredDepth;
        MyGuiControlSlider m_sliderExtCoveredDepth;
        MyGuiControlSlider m_sliderShadowNormalOffset;

        void OnChangeSmallObjectsThreshold(MyGuiControlSlider slider)
        {
            float v = slider.Value;
            for (int i = 0; i < MySector.ShadowSettings.Cascades.Length; i++)
            {
                MySector.ShadowSettings.Cascades[i].SkippingSmallObjectThreshold = v;
            }
        }

        void CheckboxHigherRangeChanged(bool isChecked)
        {
            float prevFullCoveredDepth = m_sliderFullCoveredDepth.Value;
            float prevExtCoveredDepth = m_sliderExtCoveredDepth.Value;
            float prevShadowNormalOffset = m_sliderShadowNormalOffset.Value;
            if (isChecked)
            {
                m_sliderFullCoveredDepth.MaxValue = 40000;
                m_sliderExtCoveredDepth.MaxValue = 20000;
                m_sliderShadowNormalOffset.MaxValue = 5;
            }
            else
            {
                m_sliderFullCoveredDepth.MaxValue = 200;
                m_sliderExtCoveredDepth.MaxValue = 100;
                m_sliderShadowNormalOffset.MaxValue = 0.5f;
            }
            // to force refresh values:
            m_sliderFullCoveredDepth.Value = prevFullCoveredDepth;
            m_sliderExtCoveredDepth.Value = prevExtCoveredDepth;
            m_sliderShadowNormalOffset.Value = prevShadowNormalOffset;
        }

        void SetSelectedVolume(float value)
        {
            bool isChanged = false;
            int newSelectedVolume = (int)Math.Floor(value);
            newSelectedVolume = MathHelper.Clamp(newSelectedVolume, 0, MySector.ShadowSettings.NewData.CascadesCount - 1);
            if (m_selectedVolume == newSelectedVolume)
                return;
            m_selectedVolume = newSelectedVolume;

            MyShadowsSettings.Cascade cascade = MySector.ShadowSettings.Cascades[m_selectedVolume];
            m_checkboxHigherRange.IsChecked = true;
            m_sliderFullCoveredDepth.Value = cascade.FullCoverageDepth;
            m_sliderExtCoveredDepth.Value = cascade.ExtendedCoverageDepth;
            m_sliderShadowNormalOffset.Value = cascade.ShadowNormalOffset;
        }

        float GetSelectedVolume()
        {
            return m_selectedVolume;
        }

        protected override void ValueChanged(MyGuiControlBase sender)
        {
            MyRenderProxy.SetSettingsDirty();
            MyRenderProxy.UpdateShadowsSettings(MySector.ShadowSettings);
        }

        public override string GetFriendlyName()
        {
			return "MyGuiScreenDebugRenderShadows";
        }

    }

#endif
}
