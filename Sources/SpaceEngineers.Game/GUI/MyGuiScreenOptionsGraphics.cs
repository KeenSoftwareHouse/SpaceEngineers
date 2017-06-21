using Sandbox;
using Sandbox.Common;
using Sandbox.Engine.Platform.VideoMode;
using Sandbox.Engine.Utils;
using Sandbox.Game.Localization;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace SpaceEngineers.Game.GUI
{
    public class MyGuiScreenOptionsGraphics : MyGuiScreenBase
    {
        static readonly MyStringId[] m_renderers = new MyStringId[]
        {
            MySandboxGame.DirectX11RendererKey,
        };

        enum PresetEnum
        {
            Low,
            Medium,
            High,
            Custom
        }

        private static readonly MyRenderSettings1[] m_presets = new MyRenderSettings1[]
        {
            new MyRenderSettings1 // Low
            {
                AnisotropicFiltering = MyTextureAnisoFiltering.NONE,
                AntialiasingMode = MyAntialiasingMode.NONE,
                FoliageDetails = MyFoliageDetails.DISABLED,
                ShadowQuality = MyShadowsQuality.LOW,
                AmbientOcclusionEnabled = false,
                TextureQuality = MyTextureQuality.LOW,
                Dx9Quality = MyRenderQualityEnum.LOW,
                ModelQuality = MyFakes.ENABLE_MODEL_QUALITY_IN_GRAPHICS_OPTION ? MyRenderQualityEnum.LOW : MyRenderQualityEnum.HIGH,
                VoxelQuality = MyRenderQualityEnum.LOW,
                GrassDensityFactor = 0
            },
            new MyRenderSettings1 // Medium
            {
                AnisotropicFiltering = MyTextureAnisoFiltering.ANISO_4,
                AntialiasingMode = MyAntialiasingMode.FXAA,
                FoliageDetails = MyFoliageDetails.MEDIUM,
                ShadowQuality = MyShadowsQuality.MEDIUM,
                AmbientOcclusionEnabled = true,
                TextureQuality = MyTextureQuality.MEDIUM,
                Dx9Quality = MyRenderQualityEnum.NORMAL,
                ModelQuality = MyFakes.ENABLE_MODEL_QUALITY_IN_GRAPHICS_OPTION ? MyRenderQualityEnum.NORMAL : MyRenderQualityEnum.HIGH,
                VoxelQuality = MyRenderQualityEnum.NORMAL,
                GrassDensityFactor = 1

            },
            new MyRenderSettings1 // High
            {
                AnisotropicFiltering = MyTextureAnisoFiltering.ANISO_16,
                AntialiasingMode = MyAntialiasingMode.FXAA,
                FoliageDetails = MyFoliageDetails.HIGH,
                ShadowQuality = MyShadowsQuality.HIGH,
                AmbientOcclusionEnabled = true,
                TextureQuality = MyTextureQuality.HIGH,
                Dx9Quality = MyRenderQualityEnum.HIGH,
                ModelQuality = MyRenderQualityEnum.HIGH,
                VoxelQuality = MyRenderQualityEnum.HIGH,
                GrassDensityFactor = 1
            },
        };

        private bool m_writingSettings;

        private MyGuiControlCombobox m_comboRenderer;
        private MyGuiControlCombobox m_comboAntialiasing;
        private MyGuiControlCombobox m_comboShadowMapResolution;
        private MyGuiControlCheckbox m_comboAmbientOcclusionHBAO;
        private MyGuiControlCombobox m_comboTextureQuality;
        private MyGuiControlCombobox m_comboAnisotropicFiltering;
        private MyGuiControlCombobox m_comboGraphicsPresets;
        private MyGuiControlCombobox m_comboFoliageDetails;
        private MyGuiControlCombobox m_comboModelQuality;
        private MyGuiControlCombobox m_comboVoxelQuality;
        private MyGuiControlSliderBase m_vegetationViewDistance;
        private MyGuiControlSlider m_grassDensitySlider;
        private MyGuiControlSlider m_sliderFov;
        private MyGuiControlCheckbox m_checkboxHardwareCursor;
        private MyGuiControlCheckbox m_checkboxRenderInterpolation;
        //private MyGuiControlCheckbox m_checkboxMultithreadedRender;
        //private MyGuiControlCheckbox m_checkboxTonemapping;
        private MyGuiControlCheckbox m_checkboxEnableDamageEffects;

        private MyGraphicsSettings m_settingsOld;
        private MyGraphicsSettings m_settingsNew;

        public MyGuiScreenOptionsGraphics()
            : base(position: new Vector2(0.5f, 0.5f), backgroundColor: Vector4.One)
        {
            EnabledBackgroundFade = true;
            Size = new Vector2(1000f, 1075f) / MyGuiConstants.GUI_OPTIMAL_SIZE;

            if (MyFakes.ENABLE_PLANETS)
            {
                Size += new Vector2(0f, 60f) / MyGuiConstants.GUI_OPTIMAL_SIZE;
            }
            if (MyFakes.ENABLE_MODEL_QUALITY_IN_GRAPHICS_OPTION)
            {
                Size += new Vector2(0f, 60f) / MyGuiConstants.GUI_OPTIMAL_SIZE;
            }

            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            if (!constructor)
                return;

            base.RecreateControls(constructor);

            AddCaption(MyTexts.GetString(MyCommonTexts.ScreenCaptionGraphicsOptions));

            const float TEXT_SCALE = Sandbox.Graphics.GUI.MyGuiConstants.DEFAULT_TEXT_SCALE * 0.85f;

            var labelRenderer               = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MySpaceTexts.ScreenGraphicsOptions_Renderer));
            var labelHwCursor               = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MyCommonTexts.HardwareCursor));
            var labelFov                    = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MyCommonTexts.FieldOfView));
            var labelFovDefault             = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MyCommonTexts.DefaultFOV));
            var labelRenderInterpolation    = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MyCommonTexts.RenderIterpolation));
            var labelAntiAliasing           = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MyCommonTexts.ScreenGraphicsOptions_AntiAliasing));
            var labelShadowMapResolution    = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MySpaceTexts.ScreenGraphicsOptions_ShadowMapResolution));
            var labelMultithreadedRendering = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MyCommonTexts.ScreenGraphicsOptions_MultiThreadedRendering));
            //var labelTonemapping            = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MyCommonTexts.ScreenGraphicsOptions_Tonemapping));
            var labelTextureQuality         = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MyCommonTexts.ScreenGraphicsOptions_TextureQuality));
            var labelModelQuality           = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MyCommonTexts.ScreenGraphicsOptions_ModelQuality));
            var labelVoxelQuality           = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MySpaceTexts.ScreenGraphicsOptions_VoxelQuality));
            var labelAnisotropicFiltering   = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MyCommonTexts.ScreenGraphicsOptions_AnisotropicFiltering));
            var labelGraphicsPresets        = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MyCommonTexts.ScreenGraphicsOptions_QualityPreset));
            var labelAmbientOcclusion       = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MyCommonTexts.ScreenGraphicsOptions_AmbientOcclusion));
            var labelFoliageDetails         = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MyCommonTexts.ScreenGraphicsOptions_FoliageDetails));
            var labelGrassDensity           = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MyCommonTexts.WorldSettings_GrassDensity));
            var labelEnableDamageEffects    = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MySpaceTexts.EnableDamageEffects));

            var labelVegetationDistance = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MyCommonTexts.WorldSettings_VegetationDistance));

            m_comboRenderer               = new MyGuiControlCombobox(toolTip: MyTexts.GetString(MySpaceTexts.ToolTipVideoOptionsRenderer));
            m_comboGraphicsPresets        = new MyGuiControlCombobox();
            m_comboAntialiasing           = new MyGuiControlCombobox();
            m_comboShadowMapResolution    = new MyGuiControlCombobox();
            m_comboAmbientOcclusionHBAO   = new MyGuiControlCheckbox();
            m_comboTextureQuality         = new MyGuiControlCombobox();
            m_comboAnisotropicFiltering   = new MyGuiControlCombobox();
            m_checkboxHardwareCursor      = new MyGuiControlCheckbox(toolTip: MyTexts.GetString(MyCommonTexts.ToolTipVideoOptionsHardwareCursor));
            m_checkboxRenderInterpolation = new MyGuiControlCheckbox(toolTip: MyTexts.GetString(MyCommonTexts.ToolTipVideoOptionRenderIterpolation));
            //m_checkboxMultithreadedRender = new MyGuiControlCheckbox();
            //m_checkboxTonemapping         = new MyGuiControlCheckbox();
            m_checkboxEnableDamageEffects = new MyGuiControlCheckbox(toolTip: MyTexts.GetString(MySpaceTexts.ToolTipVideoOptionsEnableDamageEffects));
            m_sliderFov                   = new MyGuiControlSlider(toolTip: MyTexts.GetString(MyCommonTexts.ToolTipVideoOptionsFieldOfView),
                labelText: new StringBuilder("{0}").ToString(),
                labelSpaceWidth: 0.035f,
                labelScale: TEXT_SCALE,
                labelFont: MyFontEnum.Blue,
                defaultValue: MathHelper.ToDegrees(MySandboxGame.Config.FieldOfView));

            m_comboModelQuality = new MyGuiControlCombobox();
            m_comboVoxelQuality = new MyGuiControlCombobox();

            m_comboFoliageDetails = new MyGuiControlCombobox();
            m_grassDensitySlider = new MyGuiControlSlider(minValue: 0f, maxValue: 10f,
                labelText: new StringBuilder("{0}").ToString(),
                labelSpaceWidth: 0.035f,
                labelScale: TEXT_SCALE,
                labelFont: MyFontEnum.Blue,
                defaultValue: MySandboxGame.Config.GrassDensityFactor);

            /* Vegetation View Distance */

            m_vegetationViewDistance = new MyGuiControlSliderBase(
                props: new MyGuiSliderPropertiesExponential(100, 10 * 1000, 10, true),
                labelSpaceWidth: 0.063f,
                labelScale: TEXT_SCALE,
                labelFont: MyFontEnum.Blue);
            m_vegetationViewDistance.DefaultRatio = m_vegetationViewDistance.Propeties.ValueToRatio(MySandboxGame.Config.VegetationDrawDistance);

            var okButton = new MyGuiControlButton(text: MyTexts.Get(MyCommonTexts.Ok), onButtonClick: OnOkClick);
            var cancelButton = new MyGuiControlButton(text: MyTexts.Get(MyCommonTexts.Cancel), onButtonClick: OnCancelClick);

            m_comboGraphicsPresets.AddItem((int)PresetEnum.Low, MyTexts.GetString(MyCommonTexts.ScreenGraphicsOptions_QualityPreset_Low));
            m_comboGraphicsPresets.AddItem((int)PresetEnum.Medium, MyTexts.GetString(MyCommonTexts.ScreenGraphicsOptions_QualityPreset_Medium));
            m_comboGraphicsPresets.AddItem((int)PresetEnum.High, MyTexts.GetString(MyCommonTexts.ScreenGraphicsOptions_QualityPreset_High));
            m_comboGraphicsPresets.AddItem((int)PresetEnum.Custom, MyTexts.GetString(MyCommonTexts.ScreenGraphicsOptions_QualityPreset_Custom));

            m_comboAntialiasing.AddItem((int)MyAntialiasingMode.NONE, MyTexts.GetString(MyCommonTexts.ScreenGraphicsOptions_AntiAliasing_None));
            m_comboAntialiasing.AddItem((int)MyAntialiasingMode.FXAA, "FXAA");
            //m_comboAntialiasing.AddItem((int)MyAntialiasingMode.MSAA_2, "MSAA 2x");
            //m_comboAntialiasing.AddItem((int)MyAntialiasingMode.MSAA_4, "MSAA 4x");
            //m_comboAntialiasing.AddItem((int)MyAntialiasingMode.MSAA_8, "MSAA 8x");

            m_comboShadowMapResolution.AddItem((int)MyShadowsQuality.DISABLED, MyTexts.GetString(MyCommonTexts.ScreenGraphicsOptions_ShadowMapResolution_Disabled));
            m_comboShadowMapResolution.AddItem((int)MyShadowsQuality.LOW, MyTexts.GetString(MyCommonTexts.ScreenGraphicsOptions_ShadowMapResolution_Low));
            m_comboShadowMapResolution.AddItem((int)MyShadowsQuality.MEDIUM, MyTexts.GetString(MySpaceTexts.ScreenGraphicsOptions_ShadowMapResolution_Medium));
            m_comboShadowMapResolution.AddItem((int)MyShadowsQuality.HIGH, MyTexts.GetString(MyCommonTexts.ScreenGraphicsOptions_ShadowMapResolution_High));

            m_comboTextureQuality.AddItem((int)MyTextureQuality.LOW, MyTexts.GetString(MyCommonTexts.ScreenGraphicsOptions_TextureQuality_Low));
            m_comboTextureQuality.AddItem((int)MyTextureQuality.MEDIUM, MyTexts.GetString(MyCommonTexts.ScreenGraphicsOptions_TextureQuality_Medium));
            m_comboTextureQuality.AddItem((int)MyTextureQuality.HIGH, MyTexts.GetString(MyCommonTexts.ScreenGraphicsOptions_TextureQuality_High));

            m_comboAnisotropicFiltering.AddItem((int)MyTextureAnisoFiltering.NONE, MyTexts.GetString(MyCommonTexts.ScreenGraphicsOptions_AnisotropicFiltering_Off));
            m_comboAnisotropicFiltering.AddItem((int)MyTextureAnisoFiltering.ANISO_1, "1x");
            m_comboAnisotropicFiltering.AddItem((int)MyTextureAnisoFiltering.ANISO_4, "4x");
            m_comboAnisotropicFiltering.AddItem((int)MyTextureAnisoFiltering.ANISO_8, "8x");
            m_comboAnisotropicFiltering.AddItem((int)MyTextureAnisoFiltering.ANISO_16, "16x");

            m_comboFoliageDetails.AddItem((int)MyFoliageDetails.DISABLED, MyTexts.GetString(MyCommonTexts.ScreenGraphicsOptions_FoliageDetails_Disabled));
            m_comboFoliageDetails.AddItem((int)MyFoliageDetails.LOW, MyTexts.GetString(MyCommonTexts.ScreenGraphicsOptions_FoliageDetails_Low));
            m_comboFoliageDetails.AddItem((int)MyFoliageDetails.MEDIUM, MyTexts.GetString(MyCommonTexts.ScreenGraphicsOptions_FoliageDetails_Medium));
            m_comboFoliageDetails.AddItem((int)MyFoliageDetails.HIGH, MyTexts.GetString(MyCommonTexts.ScreenGraphicsOptions_FoliageDetails_High));

            m_comboModelQuality.AddItem((int)MyRenderQualityEnum.LOW, MyTexts.GetString(MyCommonTexts.ScreenGraphicsOptions_FoliageDetails_Low));
            m_comboModelQuality.AddItem((int)MyRenderQualityEnum.NORMAL, MyTexts.GetString(MyCommonTexts.ScreenGraphicsOptions_FoliageDetails_Medium));
            m_comboModelQuality.AddItem((int)MyRenderQualityEnum.HIGH, MyTexts.GetString(MyCommonTexts.ScreenGraphicsOptions_FoliageDetails_High)); 
            
            m_comboVoxelQuality.AddItem((int)MyRenderQualityEnum.LOW, MyTexts.GetString(MyCommonTexts.ScreenGraphicsOptions_FoliageDetails_Low));
            m_comboVoxelQuality.AddItem((int)MyRenderQualityEnum.NORMAL, MyTexts.GetString(MyCommonTexts.ScreenGraphicsOptions_FoliageDetails_Medium));
            m_comboVoxelQuality.AddItem((int)MyRenderQualityEnum.HIGH, MyTexts.GetString(MyCommonTexts.ScreenGraphicsOptions_FoliageDetails_High));

            for (int i = 0; i < m_renderers.Length; i++)
                m_comboRenderer.AddItem(i, m_renderers[i]);

            labelFovDefault.UpdateFormatParams(MathHelper.ToDegrees(MyConstants.FIELD_OF_VIEW_CONFIG_DEFAULT));


            var table = new MyLayoutTable(this);
            {
                const float h = 60f;
                table.SetColumnWidths(60f, 400f, 460f);
                if (MyFakes.ENABLE_MODEL_QUALITY_IN_GRAPHICS_OPTION)
                    table.SetRowHeights(100f, h, h, h, h, h, 40f, h, h, h, h, h, h, h, h, h, h, 120f);
                else
                    table.SetRowHeights(100f, h, h, h, h, h, 40f, h, h, h, h, h, h, h, h, h, 120f);
            }
            int row = 1;
            const int leftCol = 1;
            const int rightCol = 2;
            const MyAlignH hAlign = MyAlignH.Left;
            const MyAlignV vAlign = MyAlignV.Center;
            //table.Add(labelRenderer, hAlign, vAlign, row, leftCol);
            //table.Add(m_comboRenderer, hAlign, vAlign, row++, rightCol);
            table.Add(labelHwCursor, hAlign, vAlign, row, leftCol);
            table.Add(m_checkboxHardwareCursor, hAlign, vAlign, row++, rightCol);
            table.Add(labelRenderInterpolation, hAlign, vAlign, row, leftCol);
            table.Add(m_checkboxRenderInterpolation, hAlign, vAlign, row++, rightCol);
            table.Add(labelEnableDamageEffects, hAlign, vAlign, row, leftCol);
            table.Add(m_checkboxEnableDamageEffects, hAlign, vAlign, row++, rightCol);
            table.Add(labelFov, hAlign, vAlign, row, leftCol);
            table.Add(m_sliderFov, hAlign, vAlign, row++, rightCol);
            table.Add(labelFovDefault, hAlign, MyAlignV.Top, row++, rightCol);
            if (MyVideoSettingsManager.RunningGraphicsRenderer == MySandboxGame.DirectX11RendererKey)
            {
                table.Add(labelGraphicsPresets, hAlign, vAlign, row, leftCol);
                table.Add(m_comboGraphicsPresets, hAlign, vAlign, row++, rightCol);
                table.Add(labelAntiAliasing, hAlign, vAlign, row, leftCol);
                table.Add(m_comboAntialiasing, hAlign, vAlign, row++, rightCol);
                table.Add(labelShadowMapResolution, hAlign, vAlign, row, leftCol);
                table.Add(m_comboShadowMapResolution, hAlign, vAlign, row++, rightCol);
                table.Add(labelAmbientOcclusion, hAlign, vAlign, row, leftCol);
                table.Add(m_comboAmbientOcclusionHBAO, hAlign, vAlign, row++, rightCol);
                table.Add(labelTextureQuality, hAlign, vAlign, row, leftCol);
                table.Add(m_comboTextureQuality, hAlign, vAlign, row++, rightCol);
                if (MyFakes.ENABLE_MODEL_QUALITY_IN_GRAPHICS_OPTION)
                {
                    table.Add(labelModelQuality, hAlign, vAlign, row, leftCol);
                    table.Add(m_comboModelQuality, hAlign, vAlign, row++, rightCol);
                }
                table.Add(labelVoxelQuality, hAlign, vAlign, row, leftCol);
                table.Add(m_comboVoxelQuality, hAlign, vAlign, row++, rightCol);
                table.Add(labelAnisotropicFiltering, hAlign, vAlign, row, leftCol);
                table.Add(m_comboAnisotropicFiltering, hAlign, vAlign, row++, rightCol);
                //table.Add(labelMultithreadedRendering, hAlign, vAlign, row, leftCol);
                //table.Add(m_checkboxMultithreadedRender, hAlign, vAlign, row++, rightCol);
                //table.Add(labelTonemapping, hAlign, vAlign, row, leftCol);
                //table.Add(m_checkboxTonemapping, hAlign, vAlign, row++, rightCol);
                if (MyFakes.ENABLE_PLANETS)
                {
                    table.Add(labelFoliageDetails, hAlign, vAlign, row, leftCol);
                    table.Add(m_comboFoliageDetails, hAlign, vAlign, row++, rightCol);
                    table.Add(labelGrassDensity, hAlign, vAlign, row, leftCol);
                    table.Add(m_grassDensitySlider, hAlign, vAlign, row++, rightCol);

                    table.Add(labelVegetationDistance, hAlign, vAlign, row, leftCol);
                    table.Add(m_vegetationViewDistance, hAlign, vAlign, row++, rightCol);
                }
            }

            table.Add(okButton, MyAlignH.Left, MyAlignV.Bottom, table.LastRow, leftCol);
            table.Add(cancelButton, MyAlignH.Right, MyAlignV.Bottom, table.LastRow, rightCol);

            { // Set FoV bounds based on current display setting.
                float fovMin, fovMax;
                MyVideoSettingsManager.GetFovBounds(out fovMin, out fovMax);
                m_sliderFov.SetBounds(MathHelper.ToDegrees(fovMin), MathHelper.ToDegrees(fovMax));
                m_sliderFov.DefaultValue = MathHelper.ToDegrees(MySandboxGame.Config.FieldOfView);
            }

            {
                m_grassDensitySlider.SetBounds(0f, 10f);
            }

            //  Update controls with values from config file
            m_settingsOld = MyVideoSettingsManager.CurrentGraphicsSettings;
            m_settingsNew = m_settingsOld;
            WriteSettingsToControls(m_settingsOld);

            //  Update OLD settings
            ReadSettingsFromControls(ref m_settingsOld);
            ReadSettingsFromControls(ref m_settingsNew);

            {
                MyGuiControlCombobox.ItemSelectedDelegate onComboItemSelected = OnSettingsChanged;
                Action<MyGuiControlCheckbox> onCheckboxChanged = (checkbox) => OnSettingsChanged();

                m_comboGraphicsPresets.ItemSelected      += OnPresetSelected;
                m_comboAnisotropicFiltering.ItemSelected += onComboItemSelected;
                m_comboAntialiasing.ItemSelected         += onComboItemSelected;
                m_comboShadowMapResolution.ItemSelected  += onComboItemSelected;
                m_comboAmbientOcclusionHBAO.IsCheckedChanged += onCheckboxChanged;
                m_comboFoliageDetails.ItemSelected       += onComboItemSelected;
                m_comboVoxelQuality.ItemSelected         += onComboItemSelected;
                m_comboTextureQuality.ItemSelected       += onComboItemSelected;

                m_checkboxHardwareCursor.IsCheckedChanged = onCheckboxChanged;
                //m_checkboxMultithreadedRender.IsCheckedChanged = onCheckboxChanged;
                m_checkboxRenderInterpolation.IsCheckedChanged = onCheckboxChanged;
                //m_checkboxTonemapping.IsCheckedChanged = onCheckboxChanged;
                m_checkboxEnableDamageEffects.IsCheckedChanged = onCheckboxChanged;

                m_sliderFov.ValueChanged = (slider) => OnSettingsChanged();
                //          m_grassDensitySlider.ValueChanged = (slider) => OnSettingsChanged();
            }
            RefreshPresetCombo(m_settingsOld.Render);

            CloseButtonEnabled = true;
            CloseButtonOffset = new Vector2(-50f, 50f) / MyGuiConstants.GUI_OPTIMAL_SIZE;
        }

        private void RefreshPresetCombo(MyRenderSettings1 renderSettings)
        {
            int presetIdx;
            for (presetIdx = 0; presetIdx < m_presets.Length; ++presetIdx)
            {
                var preset = m_presets[presetIdx];
                if (preset.AnisotropicFiltering == renderSettings.AnisotropicFiltering &&
                    preset.AntialiasingMode == renderSettings.AntialiasingMode &&
                    preset.FoliageDetails == renderSettings.FoliageDetails &&
                    preset.ShadowQuality == renderSettings.ShadowQuality &&
                    preset.TextureQuality == renderSettings.TextureQuality)
                    break;
            }
            m_comboGraphicsPresets.SelectItemByKey(presetIdx, false);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenOptionsVideo";
        }

        private void OnPresetSelected()
        {
            var preset = (PresetEnum)m_comboGraphicsPresets.GetSelectedKey();
            if (preset == PresetEnum.Custom)
                return;

            var presetSettings = m_presets[(int)preset];
            // presetSettings.MultithreadingEnabled = m_checkboxMultithreadedRender.IsChecked;
            presetSettings.InterpolationEnabled = m_checkboxRenderInterpolation.IsChecked;
            //presetSettings.TonemappingEnabled = m_checkboxTonemapping.IsChecked;
            m_settingsNew.Render = presetSettings;
            WriteSettingsToControls(m_settingsNew);
        }

        private void OnSettingsChanged()
        {
            m_comboGraphicsPresets.SelectItemByKey((long)PresetEnum.Custom);
            ReadSettingsFromControls(ref m_settingsNew);
            RefreshPresetCombo(m_settingsNew.Render);
        }

        /// <returns>Bool indicating a game restart is required</returns>
        private bool ReadSettingsFromControls(ref MyGraphicsSettings graphicsSettings)
        {
            if (m_writingSettings)
                return false;

            bool restartIsNeeded;

            {
                MyGraphicsSettings read = new MyGraphicsSettings();
                read.GraphicsRenderer             = m_renderers[(int)m_comboRenderer.GetSelectedKey()];
                read.FieldOfView                  = MathHelper.ToRadians(m_sliderFov.Value);
                read.HardwareCursor               = m_checkboxHardwareCursor.IsChecked;
                read.EnableDamageEffects          = m_checkboxEnableDamageEffects.IsChecked;
                read.Render.AntialiasingMode      = (MyAntialiasingMode)m_comboAntialiasing.GetSelectedKey();
                read.Render.AmbientOcclusionEnabled = m_comboAmbientOcclusionHBAO.IsChecked;
                read.Render.ShadowQuality         = (MyShadowsQuality)m_comboShadowMapResolution.GetSelectedKey();
                read.Render.InterpolationEnabled  = m_checkboxRenderInterpolation.IsChecked;
                //read.Render.MultithreadingEnabled = m_checkboxMultithreadedRender.IsChecked;
                //read.Render.TonemappingEnabled    = m_checkboxTonemapping.IsChecked;
                read.Render.TextureQuality        = (MyTextureQuality)m_comboTextureQuality.GetSelectedKey();
                read.Render.AnisotropicFiltering  = (MyTextureAnisoFiltering)m_comboAnisotropicFiltering.GetSelectedKey();
                read.Render.Dx9Quality            = graphicsSettings.Render.Dx9Quality;
                read.Render.FoliageDetails        = (MyFoliageDetails)m_comboFoliageDetails.GetSelectedKey();
                read.Render.ModelQuality          = (MyRenderQualityEnum)m_comboModelQuality.GetSelectedKey();
                read.Render.VoxelQuality          = (MyRenderQualityEnum)m_comboVoxelQuality.GetSelectedKey();
                read.Render.GrassDensityFactor    = m_grassDensitySlider.Value;
                read.VegetationDrawDistance = m_vegetationViewDistance.Value;

                restartIsNeeded = read.GraphicsRenderer != graphicsSettings.GraphicsRenderer;
                graphicsSettings = read;
            }

            return restartIsNeeded;
        }

        private void WriteSettingsToControls(MyGraphicsSettings graphicsSettings)
        {
            m_writingSettings = true;

            int selectedRender = Math.Max(0, Array.IndexOf(m_renderers, graphicsSettings.GraphicsRenderer));
            m_comboRenderer.SelectItemByKey(selectedRender);
            m_checkboxHardwareCursor.IsChecked = graphicsSettings.HardwareCursor;

            m_sliderFov.Value = MathHelper.ToDegrees(graphicsSettings.FieldOfView);
            m_comboFoliageDetails.SelectItemByKey((long)graphicsSettings.Render.FoliageDetails, sendEvent: false);
            m_comboModelQuality.SelectItemByKey((long)graphicsSettings.Render.ModelQuality, sendEvent: false);
            m_comboVoxelQuality.SelectItemByKey((long)graphicsSettings.Render.VoxelQuality, sendEvent: false);

            m_grassDensitySlider.Value = graphicsSettings.Render.GrassDensityFactor;
            m_vegetationViewDistance.Value = graphicsSettings.VegetationDrawDistance;

            m_checkboxEnableDamageEffects.IsChecked = graphicsSettings.EnableDamageEffects;
            m_checkboxRenderInterpolation.IsChecked = graphicsSettings.Render.InterpolationEnabled;
            //            m_checkboxMultithreadedRender.IsChecked = graphicsSettings.Render.MultithreadingEnabled;
            //m_checkboxTonemapping.IsChecked = graphicsSettings.Render.TonemappingEnabled;
            m_comboAntialiasing.SelectItemByKey((long)graphicsSettings.Render.AntialiasingMode, sendEvent: false);
            m_comboAmbientOcclusionHBAO.IsChecked = graphicsSettings.Render.AmbientOcclusionEnabled;
            m_comboShadowMapResolution.SelectItemByKey((long)graphicsSettings.Render.ShadowQuality, sendEvent: false);
            m_comboTextureQuality.SelectItemByKey((long)graphicsSettings.Render.TextureQuality, sendEvent: false);
            m_comboAnisotropicFiltering.SelectItemByKey((long)graphicsSettings.Render.AnisotropicFiltering, sendEvent: false);

            m_writingSettings = false;
        }

        public void OnCancelClick(MyGuiControlButton sender)
        {
            MyVideoSettingsManager.Apply(m_settingsOld);
            MyVideoSettingsManager.SaveCurrentSettings();
            CloseScreen();
        }

        public void OnOkClick(MyGuiControlButton sender)
        {
            //  Update NEW settings
            if (ReadSettingsFromControls(ref m_settingsNew))
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                            buttonType: MyMessageBoxButtonsType.OK,
                            messageText: MyTexts.Get(MySpaceTexts.MessageBoxTextRestartNeededAfterRendererSwitch),
                            messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionWarning)));
            }
            MyVideoSettingsManager.Apply(m_settingsNew);
            MyVideoSettingsManager.SaveCurrentSettings();
            CloseScreen();
        }

    }
}
