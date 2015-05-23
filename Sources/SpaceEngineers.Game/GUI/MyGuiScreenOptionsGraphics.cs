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
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace SpaceEngineers.Game.GUI
{
    public class MyGuiScreenOptionsGraphics : MyGuiScreenBase
    {
        static readonly MyStringId[] m_renderers = new MyStringId[]
        {
            SpaceEngineersGame.DirectX9RendererKey,
            SpaceEngineersGame.DirectX11RendererKey,
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
                TextureQuality = MyTextureQuality.LOW,
            },
            new MyRenderSettings1 // Medium
            {
                AnisotropicFiltering = MyTextureAnisoFiltering.ANISO_4,
                AntialiasingMode = MyAntialiasingMode.FXAA,
                FoliageDetails = MyFoliageDetails.MEDIUM,
                ShadowQuality = MyShadowsQuality.LOW,
                TextureQuality = MyTextureQuality.MEDIUM,
            },
            new MyRenderSettings1 // High
            {
                AnisotropicFiltering = MyTextureAnisoFiltering.ANISO_16,
                AntialiasingMode = MyAntialiasingMode.FXAA,
                FoliageDetails = MyFoliageDetails.HIGH,
                ShadowQuality = MyShadowsQuality.HIGH,
                TextureQuality = MyTextureQuality.HIGH,
            },
        };

        private MyGuiControlCombobox m_comboDx9RenderQuality;
        private MyGuiControlCombobox m_comboRenderer;
        private MyGuiControlCombobox m_comboAntialiasing;
        private MyGuiControlCombobox m_comboShadowMapResolution;
        private MyGuiControlCombobox m_comboTextureQuality;
        private MyGuiControlCombobox m_comboAnisotropicFiltering;
        private MyGuiControlCombobox m_comboFoliageDetails;
        private MyGuiControlCombobox m_comboGraphicsPresets;
        private MyGuiControlSlider m_sliderFov;
        private MyGuiControlCheckbox m_checkboxHardwareCursor;
        private MyGuiControlCheckbox m_checkboxRenderInterpolation;
        private MyGuiControlCheckbox m_checkboxMultithreadedRender;
        private MyGuiControlCheckbox m_checkboxEnableDamageEffects;

        private MyGraphicsSettings m_settingsOld;
        private MyGraphicsSettings m_settingsNew;

        public MyGuiScreenOptionsGraphics()
            : base(position: new Vector2(0.5f, 0.5f), backgroundColor: Vector4.One)
        {
            EnabledBackgroundFade = true;
            Size = new Vector2(1000f, 1050f) / MyGuiConstants.GUI_OPTIMAL_SIZE;

            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            if (!constructor)
                return;

            base.RecreateControls(constructor);

            AddCaption(MyTexts.GetString(MySpaceTexts.ScreenCaptionGraphicsOptions));

            const float TEXT_SCALE = Sandbox.Graphics.GUI.MyGuiConstants.DEFAULT_TEXT_SCALE * 0.85f;

            var labelRenderer               = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MySpaceTexts.ScreenGraphicsOptions_Renderer));
            var labelHwCursor               = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MySpaceTexts.HardwareCursor));
            var labelFov                    = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MySpaceTexts.FieldOfView));
            var labelFovDefault             = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MySpaceTexts.DefaultFOV));
            var labelRenderInterpolation    = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MySpaceTexts.RenderIterpolation));
            var labelAntiAliasing           = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MySpaceTexts.ScreenGraphicsOptions_AntiAliasing));
            var labelShadowMapResolution    = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MySpaceTexts.ScreenGraphicsOptions_ShadowMapResolution));
            var labelMultithreadedRendering = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MySpaceTexts.ScreenGraphicsOptions_MultiThreadedRendering));
            var labelTextureQuality         = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MySpaceTexts.ScreenGraphicsOptions_TextureQuality));
            var labelAnisotropicFiltering   = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MySpaceTexts.ScreenGraphicsOptions_AnisotropicFiltering));
            var labelFoliageDetails         = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MySpaceTexts.ScreenGraphicsOptions_FoliageDetails));
            var labelGraphicsPresets        = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MySpaceTexts.ScreenGraphicsOptions_QualityPreset));
            var labelEnableDamageEffects    = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MySpaceTexts.EnableDamageEffects));
            var labelDx9RenderQuality       = new MyGuiControlLabel(textScale: TEXT_SCALE, text: MyTexts.GetString(MySpaceTexts.RenderQuality));

            m_comboRenderer               = new MyGuiControlCombobox(toolTip: MyTexts.GetString(MySpaceTexts.ToolTipVideoOptionsRenderer));
            m_comboGraphicsPresets        = new MyGuiControlCombobox();
            m_comboAntialiasing           = new MyGuiControlCombobox();
            m_comboShadowMapResolution    = new MyGuiControlCombobox();
            m_comboTextureQuality         = new MyGuiControlCombobox();
            m_comboAnisotropicFiltering   = new MyGuiControlCombobox();
            m_comboFoliageDetails         = new MyGuiControlCombobox();
            m_comboDx9RenderQuality       = new MyGuiControlCombobox(toolTip: MyTexts.GetString(MySpaceTexts.ToolTipVideoOptionsRenderQuality));
            m_checkboxHardwareCursor      = new MyGuiControlCheckbox(toolTip: MyTexts.GetString(MySpaceTexts.ToolTipVideoOptionsHardwareCursor));
            m_checkboxRenderInterpolation = new MyGuiControlCheckbox(toolTip: MyTexts.GetString(MySpaceTexts.ToolTipVideoOptionRenderIterpolation));
            m_checkboxMultithreadedRender = new MyGuiControlCheckbox();
            m_checkboxEnableDamageEffects = new MyGuiControlCheckbox(toolTip: MyTexts.GetString(MySpaceTexts.ToolTipVideoOptionsEnableDamageEffects));
            m_sliderFov                   = new MyGuiControlSlider(toolTip: MyTexts.GetString(MySpaceTexts.ToolTipVideoOptionsFieldOfView),
                labelText: new StringBuilder("{0}").ToString(),
                labelSpaceWidth: 0.035f,
                labelScale: TEXT_SCALE,
                labelFont: MyFontEnum.Blue);

            var okButton = new MyGuiControlButton(text: MyTexts.Get(MySpaceTexts.Ok), onButtonClick: OnOkClick);
            var cancelButton = new MyGuiControlButton(text: MyTexts.Get(MySpaceTexts.Cancel), onButtonClick: OnCancelClick);

            m_comboDx9RenderQuality.AddItem((int)MyRenderQualityEnum.NORMAL,    MySpaceTexts.RenderQualityNormal);
            m_comboDx9RenderQuality.AddItem((int)MyRenderQualityEnum.HIGH,      MySpaceTexts.RenderQualityHigh);
            m_comboDx9RenderQuality.AddItem((int)MyRenderQualityEnum.EXTREME,   MySpaceTexts.RenderQualityExtreme);

            m_comboGraphicsPresets.AddItem((int)PresetEnum.Low,    MyTexts.GetString(MySpaceTexts.ScreenGraphicsOptions_QualityPreset_Low));
            m_comboGraphicsPresets.AddItem((int)PresetEnum.Medium, MyTexts.GetString(MySpaceTexts.ScreenGraphicsOptions_QualityPreset_Medium));
            m_comboGraphicsPresets.AddItem((int)PresetEnum.High,   MyTexts.GetString(MySpaceTexts.ScreenGraphicsOptions_QualityPreset_High));
            m_comboGraphicsPresets.AddItem((int)PresetEnum.Custom, MyTexts.GetString(MySpaceTexts.ScreenGraphicsOptions_QualityPreset_Custom));

            m_comboAntialiasing.AddItem((int)MyAntialiasingMode.NONE,   MyTexts.GetString(MySpaceTexts.ScreenGraphicsOptions_AntiAliasing_None));
            m_comboAntialiasing.AddItem((int)MyAntialiasingMode.FXAA,   "FXAA");
            m_comboAntialiasing.AddItem((int)MyAntialiasingMode.MSAA_2, "MSAA 2x");
            m_comboAntialiasing.AddItem((int)MyAntialiasingMode.MSAA_4, "MSAA 4x");
            m_comboAntialiasing.AddItem((int)MyAntialiasingMode.MSAA_8, "MSAA 8x");

            m_comboShadowMapResolution.AddItem((int)MyShadowsQuality.LOW,  MyTexts.GetString(MySpaceTexts.ScreenGraphicsOptions_ShadowMapResolution_Low));
            m_comboShadowMapResolution.AddItem((int)MyShadowsQuality.HIGH, MyTexts.GetString(MySpaceTexts.ScreenGraphicsOptions_ShadowMapResolution_High));

            m_comboTextureQuality.AddItem((int)MyTextureQuality.LOW,    MyTexts.GetString(MySpaceTexts.ScreenGraphicsOptions_TextureQuality_Low));
            m_comboTextureQuality.AddItem((int)MyTextureQuality.MEDIUM, MyTexts.GetString(MySpaceTexts.ScreenGraphicsOptions_TextureQuality_Medium));
            m_comboTextureQuality.AddItem((int)MyTextureQuality.HIGH,   MyTexts.GetString(MySpaceTexts.ScreenGraphicsOptions_TextureQuality_High));

            m_comboAnisotropicFiltering.AddItem((int)MyTextureAnisoFiltering.NONE, MyTexts.GetString(MySpaceTexts.ScreenGraphicsOptions_AnisotropicFiltering_Off));
            m_comboAnisotropicFiltering.AddItem((int)MyTextureAnisoFiltering.ANISO_1, "1x");
            m_comboAnisotropicFiltering.AddItem((int)MyTextureAnisoFiltering.ANISO_4, "4x");
            m_comboAnisotropicFiltering.AddItem((int)MyTextureAnisoFiltering.ANISO_8, "8x");
            m_comboAnisotropicFiltering.AddItem((int)MyTextureAnisoFiltering.ANISO_16, "16x");

            m_comboFoliageDetails.AddItem((int)MyFoliageDetails.DISABLED, MyTexts.GetString(MySpaceTexts.ScreenGraphicsOptions_FoliageDetails_Disabled));
            m_comboFoliageDetails.AddItem((int)MyFoliageDetails.LOW,      MyTexts.GetString(MySpaceTexts.ScreenGraphicsOptions_FoliageDetails_Low));
            m_comboFoliageDetails.AddItem((int)MyFoliageDetails.MEDIUM,   MyTexts.GetString(MySpaceTexts.ScreenGraphicsOptions_FoliageDetails_Medium));
            m_comboFoliageDetails.AddItem((int)MyFoliageDetails.HIGH,     MyTexts.GetString(MySpaceTexts.ScreenGraphicsOptions_FoliageDetails_High));

            for (int i = 0; i < m_renderers.Length; i++)
                m_comboRenderer.AddItem(i, m_renderers[i]);

            labelFovDefault.UpdateFormatParams(MathHelper.ToDegrees(MyConstants.FIELD_OF_VIEW_CONFIG_DEFAULT));


            var table = new MyLayoutTable(this);
            {
                const float h = 60f;
                table.SetColumnWidths(60f, 400f, 460f);
                table.SetRowHeights(100f, h, h, h, h, h, 40f, h, h, h, h, h, h, h, 120f);
            }
            int row = 1;
            const int leftCol = 1;
            const int rightCol = 2;
            const MyAlignH hAlign = MyAlignH.Left;
            const MyAlignV vAlign = MyAlignV.Center;
            table.Add(labelRenderer, hAlign, vAlign, row, leftCol);
            table.Add(m_comboRenderer, hAlign, vAlign, row++, rightCol);
            table.Add(labelHwCursor, hAlign, vAlign, row, leftCol);
            table.Add(m_checkboxHardwareCursor, hAlign, vAlign, row++, rightCol);
            table.Add(labelRenderInterpolation, hAlign, vAlign, row, leftCol);
            table.Add(m_checkboxRenderInterpolation, hAlign, vAlign, row++, rightCol);
            table.Add(labelEnableDamageEffects, hAlign, vAlign, row, leftCol);
            table.Add(m_checkboxEnableDamageEffects, hAlign, vAlign, row++, rightCol);
            table.Add(labelFov, hAlign, vAlign, row, leftCol);
            table.Add(m_sliderFov, hAlign, vAlign, row++, rightCol);
            table.Add(labelFovDefault, hAlign, MyAlignV.Top , row++, rightCol);
            if (MyVideoSettingsManager.RunningGraphicsRenderer == SpaceEngineersGame.DirectX11RendererKey)
            {
                table.Add(labelGraphicsPresets, hAlign, vAlign, row, leftCol);
                table.Add(m_comboGraphicsPresets, hAlign, vAlign, row++, rightCol);
                table.Add(labelAntiAliasing, hAlign, vAlign, row, leftCol);
                table.Add(m_comboAntialiasing, hAlign, vAlign, row++, rightCol);
                table.Add(labelShadowMapResolution, hAlign, vAlign, row, leftCol);
                table.Add(m_comboShadowMapResolution, hAlign, vAlign, row++, rightCol);
                table.Add(labelTextureQuality, hAlign, vAlign, row, leftCol);
                table.Add(m_comboTextureQuality, hAlign, vAlign, row++, rightCol);
                table.Add(labelAnisotropicFiltering, hAlign, vAlign, row, leftCol);
                table.Add(m_comboAnisotropicFiltering, hAlign, vAlign, row++, rightCol);
                const bool foliageDetailsEnabled = false;
                if (foliageDetailsEnabled)
                {
                    table.Add(labelFoliageDetails, hAlign, vAlign, row, leftCol);
                    table.Add(m_comboFoliageDetails, hAlign, vAlign, row++, rightCol);
                }
                table.Add(labelMultithreadedRendering, hAlign, vAlign, row, leftCol);
                table.Add(m_checkboxMultithreadedRender, hAlign, vAlign, row++, rightCol);
            }
            else // Dx9 or nothing specified
            {
                table.Add(labelDx9RenderQuality, hAlign, vAlign, row, leftCol);
                table.Add(m_comboDx9RenderQuality, hAlign, vAlign, row++, rightCol);
            }

            table.Add(okButton, MyAlignH.Left, MyAlignV.Bottom, table.LastRow, leftCol);
            table.Add(cancelButton, MyAlignH.Right, MyAlignV.Bottom, table.LastRow, rightCol);

            { // Set FoV bounds based on current display setting.
                float fovMin, fovMax;
                MyVideoSettingsManager.GetFovBounds(out fovMin, out fovMax);
                m_sliderFov.SetBounds(MathHelper.ToDegrees(fovMin), MathHelper.ToDegrees(fovMax));
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
                m_comboFoliageDetails.ItemSelected       += onComboItemSelected;
                m_comboShadowMapResolution.ItemSelected  += onComboItemSelected;
                m_comboTextureQuality.ItemSelected       += onComboItemSelected;
                m_comboDx9RenderQuality.ItemSelected     += onComboItemSelected;

                m_checkboxHardwareCursor.IsCheckedChanged = onCheckboxChanged;
                m_checkboxMultithreadedRender.IsCheckedChanged = onCheckboxChanged;
                m_checkboxRenderInterpolation.IsCheckedChanged = onCheckboxChanged;
                m_checkboxEnableDamageEffects.IsCheckedChanged = onCheckboxChanged;

                m_sliderFov.ValueChanged = (slider) => OnSettingsChanged();
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
            presetSettings.MultithreadingEnabled = m_checkboxMultithreadedRender.IsChecked;
            presetSettings.InterpolationEnabled = m_checkboxRenderInterpolation.IsChecked;
            m_settingsNew.Render = presetSettings;
            WriteSettingsToControls(m_settingsNew);
            MyVideoSettingsManager.Apply(m_settingsNew);
        }

        private void OnSettingsChanged()
        {
            m_comboGraphicsPresets.SelectItemByKey((long)PresetEnum.Custom);
            ReadSettingsFromControls(ref m_settingsNew);
            MyVideoSettingsManager.Apply(m_settingsNew);
            RefreshPresetCombo(m_settingsNew.Render);
        }

        private bool ReadSettingsFromControls(ref MyGraphicsSettings graphicsSettings)
        {
            bool changed;

            {
                MyGraphicsSettings read = new MyGraphicsSettings();
                read.GraphicsRenderer             = m_renderers[(int)m_comboRenderer.GetSelectedKey()];
                read.FieldOfView                  = MathHelper.ToRadians(m_sliderFov.Value);
                read.HardwareCursor               = m_checkboxHardwareCursor.IsChecked;
                read.EnableDamageEffects          = m_checkboxEnableDamageEffects.IsChecked;
                read.Render.AntialiasingMode      = (MyAntialiasingMode)m_comboAntialiasing.GetSelectedKey();
                read.Render.ShadowQuality         = (MyShadowsQuality)m_comboShadowMapResolution.GetSelectedKey();
                read.Render.InterpolationEnabled  = m_checkboxRenderInterpolation.IsChecked;
                read.Render.MultithreadingEnabled = m_checkboxMultithreadedRender.IsChecked;
                read.Render.TextureQuality        = (MyTextureQuality)m_comboTextureQuality.GetSelectedKey();
                read.Render.AnisotropicFiltering  = (MyTextureAnisoFiltering)m_comboAnisotropicFiltering.GetSelectedKey();
                read.Render.FoliageDetails        = (MyFoliageDetails)m_comboFoliageDetails.GetSelectedKey();
                read.Render.Dx9Quality            = (MyRenderQualityEnum)m_comboDx9RenderQuality.GetSelectedKey();

                changed = !read.Equals(ref graphicsSettings);
                graphicsSettings = read;
            }

            return changed;
        }

        private void WriteSettingsToControls(MyGraphicsSettings graphicsSettings)
        {
            m_comboRenderer.SelectItemByKey(Array.IndexOf(m_renderers, graphicsSettings.GraphicsRenderer));
            m_checkboxHardwareCursor.IsChecked = graphicsSettings.HardwareCursor;

            m_sliderFov.Value = MathHelper.ToDegrees(graphicsSettings.FieldOfView);

            m_checkboxEnableDamageEffects.IsChecked = graphicsSettings.EnableDamageEffects;
            m_checkboxRenderInterpolation.IsChecked = graphicsSettings.Render.InterpolationEnabled;
            m_checkboxMultithreadedRender.IsChecked = graphicsSettings.Render.MultithreadingEnabled;
            m_comboAntialiasing.SelectItemByKey((long)graphicsSettings.Render.AntialiasingMode, sendEvent: false);
            m_comboShadowMapResolution.SelectItemByKey((long)graphicsSettings.Render.ShadowQuality, sendEvent: false);
            m_comboTextureQuality.SelectItemByKey((long)graphicsSettings.Render.TextureQuality, sendEvent: false);
            m_comboAnisotropicFiltering.SelectItemByKey((long)graphicsSettings.Render.AnisotropicFiltering, sendEvent: false);
            m_comboFoliageDetails.SelectItemByKey((long)graphicsSettings.Render.FoliageDetails, sendEvent: false);
            m_comboDx9RenderQuality.SelectItemByKey((int)graphicsSettings.Render.Dx9Quality);
            if (m_comboDx9RenderQuality.GetSelectedKey() == -1)
                m_comboDx9RenderQuality.SelectItemByIndex(0);
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
            ReadSettingsFromControls(ref m_settingsNew);
            MyVideoSettingsManager.Apply(m_settingsNew);
            MyVideoSettingsManager.SaveCurrentSettings();
            CloseScreen();
        }

    }
}
