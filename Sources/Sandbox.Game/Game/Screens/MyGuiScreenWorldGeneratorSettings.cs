using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Utils;
using Sandbox.Game.Localization;
using Sandbox.Game.World.Generator;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Gui
{
    public class MyGuiScreenWorldGeneratorSettings : MyGuiScreenBase
    {
        public enum AsteroidAmountEnum
        {
            None = 0,
            Normal = 4,
            More = 7,
            Many = 16,
            ProceduralLow = -1,
            ProceduralNormal = -2,
            ProceduralHigh = -3,
        }

        MyGuiScreenWorldSettings m_parent;

        public enum MyFloraDensityEnum
        {
            NONE = 0,
            LOW = 10,
            MEDIUM = 20,
            HIGH = 30,
            EXTREME = 40,
        }

        private int? m_asteroidAmount;
        public int AsteroidAmount
        {
            get
            {
                return m_asteroidAmount.HasValue ? m_asteroidAmount.Value : (int)AsteroidAmountEnum.ProceduralLow;
            }
            set
            {
                m_asteroidAmount = value;
                switch (value)
                {
                    case (int)AsteroidAmountEnum.None:
                        m_asteroidAmountCombo.SelectItemByKey((int)AsteroidAmountEnum.None);
                        return;
                    case (int)AsteroidAmountEnum.Normal:
                        m_asteroidAmountCombo.SelectItemByKey((int)AsteroidAmountEnum.Normal);
                        return;
                    case (int)AsteroidAmountEnum.More:
                        m_asteroidAmountCombo.SelectItemByKey((int)AsteroidAmountEnum.More);
                        return;
                    case (int)AsteroidAmountEnum.Many:
                        m_asteroidAmountCombo.SelectItemByKey((int)AsteroidAmountEnum.Many);
                        return;
                    case (int)AsteroidAmountEnum.ProceduralLow:
                        m_asteroidAmountCombo.SelectItemByKey((int)AsteroidAmountEnum.ProceduralLow);
                        return;
                    case (int)AsteroidAmountEnum.ProceduralNormal:
                        m_asteroidAmountCombo.SelectItemByKey((int)AsteroidAmountEnum.ProceduralNormal);
                        return;
                    case (int)AsteroidAmountEnum.ProceduralHigh:
                        m_asteroidAmountCombo.SelectItemByKey((int)AsteroidAmountEnum.ProceduralHigh);
                        return;
                    default:
                        Debug.Assert(false, "Unhandled value in AsteroidAmountEnum");
                        return;
                }
            }
        }

        MyGuiControlButton m_okButton, m_cancelButton;
        MyGuiControlCombobox m_asteroidAmountCombo, m_floraDensityCombo;
        MyGuiControlSlider m_moonMinSizeSlider, m_moonMaxSizeSlider, m_planetMinSizeSlider, m_planetMaxSizeSlider;
        MyGuiControlLabel m_asteroidAmountLabel, m_floraDensityLabel, m_moonSizeMinLabel, m_moonSizeMaxLabel, m_planetSizeMinLabel, m_planetSizeMaxLabel;
        MyGuiControlCheckbox m_enablePlanets;

        public override string GetFriendlyName()
        {
            return "MyGuiScreenWorldGeneratorSettings";
        }

        public static Vector2 CalcSize()
        {
            float width = 0.65f;
            float height = 1.00f;
            height -= 0.27f;
            return new Vector2(width, height);
        }

        public MyGuiScreenWorldGeneratorSettings(MyGuiScreenWorldSettings parent)
            : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, CalcSize())
        {
            m_parent = parent;
            RecreateControls(true);
            SetSettingsToControls();
        }

        public event System.Action OnOkButtonClicked;

        public void GetSettings(MyObjectBuilder_SessionSettings output)
        {
            output.EnablePlanets = m_enablePlanets.IsChecked;
            output.FloraDensity = GetFloraDensity();
            output.EnableFlora = output.FloraDensity > 0;
            output.MoonMaxSize = m_moonMaxSizeSlider.Value;
            output.MoonMinSize = m_moonMinSizeSlider.Value;
            output.PlanetMaxSize = m_planetMaxSizeSlider.Value;
            output.PlanetMinSize = m_planetMinSizeSlider.Value;
        }

        protected virtual void SetSettingsToControls()
        {
            m_enablePlanets.IsChecked = m_parent.Settings.EnablePlanets;
            m_floraDensityCombo.SelectItemByKey((int)FloraDensityEnumKey(m_parent.Settings.FloraDensity));
            m_planetMaxSizeSlider.Value = m_parent.Settings.PlanetMaxSize;
            m_planetMinSizeSlider.Value = m_parent.Settings.PlanetMinSize;
            m_moonMinSizeSlider.Value = m_parent.Settings.MoonMinSize;
            m_moonMaxSizeSlider.Value = m_parent.Settings.MoonMaxSize;
            AsteroidAmount = m_parent.AsteroidAmount;
        }

        public override void RecreateControls(bool constructor)
        {
            float width = 0.284375f + 0.025f;
            base.RecreateControls(constructor);

            AddCaption(MySpaceTexts.ScreenCaptionWorldGeneratorSettings);

            m_moonMinSizeSlider = new MyGuiControlSlider(
                position: Vector2.Zero,
                width: width,
                minValue: MyProceduralPlanetCellGenerator.MOON_SIZE_MIN_LIMIT,
                maxValue: MyProceduralPlanetCellGenerator.MOON_SIZE_MAX_LIMIT,
                labelText: new StringBuilder("{0} m").ToString(),
                labelDecimalPlaces: 0,
                labelSpaceWidth: 0.09f,
                intValue: true
            );

            m_moonMinSizeSlider.ValueChanged += (x) =>
            {
                if (x.Value > m_moonMaxSizeSlider.Value)
                {
                    m_moonMaxSizeSlider.Value = x.Value;
                }
            };

            m_moonMaxSizeSlider = new MyGuiControlSlider(
                position: Vector2.Zero,
                width: width,
                minValue: MyProceduralPlanetCellGenerator.MOON_SIZE_MIN_LIMIT,
                maxValue: MyProceduralPlanetCellGenerator.MOON_SIZE_MAX_LIMIT,
                labelText: new StringBuilder("{0} m").ToString(),
                labelDecimalPlaces: 0,
                labelSpaceWidth: 0.09f,
                intValue: true
            );

            m_moonMaxSizeSlider.ValueChanged += (x) =>
            {
                if (x.Value < m_moonMinSizeSlider.Value)
                {
                    m_moonMinSizeSlider.Value = x.Value;
                }
            };

            m_planetMinSizeSlider = new MyGuiControlSlider(
                position: Vector2.Zero,
                width: width,
                minValue: MyProceduralPlanetCellGenerator.PLANET_SIZE_MIN_LIMIT,
                maxValue: MyProceduralPlanetCellGenerator.PLANET_SIZE_MAX_LIMIT,
                labelText: new StringBuilder("{0} m").ToString(),
                labelDecimalPlaces: 0,
                labelSpaceWidth: 0.09f,
                intValue: true
            );

            m_planetMinSizeSlider.ValueChanged += (x) =>
            {
                if (x.Value > m_planetMaxSizeSlider.Value)
                {
                    m_planetMaxSizeSlider.Value = x.Value;
                }
            };

            m_planetMaxSizeSlider = new MyGuiControlSlider(
                position: Vector2.Zero,
                width: width,
                minValue: MyProceduralPlanetCellGenerator.PLANET_SIZE_MIN_LIMIT,
                maxValue: MyProceduralPlanetCellGenerator.PLANET_SIZE_MAX_LIMIT,
                labelText: new StringBuilder("{0} m").ToString(),
                labelDecimalPlaces: 0,
                labelSpaceWidth: 0.09f,
                intValue: true
            );

            m_planetMaxSizeSlider.ValueChanged += (x) =>
            {
                if (x.Value < m_planetMinSizeSlider.Value)
                {
                    m_planetMinSizeSlider.Value = x.Value;
                }
            };

            m_asteroidAmountLabel = MakeLabel(MySpaceTexts.Asteroid_Amount);
            m_asteroidAmountCombo = new MyGuiControlCombobox(size: new Vector2(width, 0.04f));

            m_asteroidAmountCombo.ItemSelected += m_asteroidAmountCombo_ItemSelected;

            m_asteroidAmountCombo.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettingsAsteroidAmount));

            m_asteroidAmountCombo.AddItem((int)AsteroidAmountEnum.Normal, MySpaceTexts.WorldSettings_AsteroidAmountNormal);
            m_asteroidAmountCombo.AddItem((int)AsteroidAmountEnum.More, MySpaceTexts.WorldSettings_AsteroidAmountLarge);
            if (Environment.Is64BitProcess)
                m_asteroidAmountCombo.AddItem((int)AsteroidAmountEnum.Many, MySpaceTexts.WorldSettings_AsteroidAmountExtreme);

            if (MyFakes.ENABLE_ASTEROID_FIELDS)
            {
                m_asteroidAmountCombo.AddItem((int)AsteroidAmountEnum.ProceduralLow, MySpaceTexts.WorldSettings_AsteroidAmountProceduralLow);
                m_asteroidAmountCombo.AddItem((int)AsteroidAmountEnum.ProceduralNormal, MySpaceTexts.WorldSettings_AsteroidAmountProceduralNormal);
                if (Environment.Is64BitProcess)
                    m_asteroidAmountCombo.AddItem((int)AsteroidAmountEnum.ProceduralHigh, MySpaceTexts.WorldSettings_AsteroidAmountProceduralHigh);
            }


            Controls.Add(m_asteroidAmountLabel);
            Controls.Add(m_asteroidAmountCombo);


            m_floraDensityLabel = MakeLabel(MySpaceTexts.WorldSettings_FloraDensity);
            m_floraDensityCombo = new MyGuiControlCombobox(size: new Vector2(width, 0.04f));

            m_floraDensityCombo.AddItem((int)MyFloraDensityEnum.NONE, MySpaceTexts.WorldSettings_FloraDensity_None);
            m_floraDensityCombo.AddItem((int)MyFloraDensityEnum.LOW, MySpaceTexts.WorldSettings_FloraDensity_Low);
            m_floraDensityCombo.AddItem((int)MyFloraDensityEnum.MEDIUM, MySpaceTexts.WorldSettings_FloraDensity_Medium);
            m_floraDensityCombo.AddItem((int)MyFloraDensityEnum.HIGH, MySpaceTexts.WorldSettings_FloraDensity_High);
            m_floraDensityCombo.AddItem((int)MyFloraDensityEnum.EXTREME, MySpaceTexts.WorldSettings_FloraDensity_Extreme);

            m_floraDensityCombo.SetToolTip(MyTexts.GetString(MySpaceTexts.ToolTipWorldSettings_FloraDensity));
            Controls.Add(m_floraDensityLabel);
            Controls.Add(m_floraDensityCombo);

            m_moonSizeMinLabel = MakeLabel(MySpaceTexts.WorldSettings_MoonSizeMin);
            Controls.Add(m_moonSizeMinLabel);
            Controls.Add(m_moonMinSizeSlider);

            m_moonSizeMaxLabel = MakeLabel(MySpaceTexts.WorldSettings_MoonSizeMax);
            Controls.Add(m_moonSizeMaxLabel);
            Controls.Add(m_moonMaxSizeSlider);

            m_planetSizeMinLabel = MakeLabel(MySpaceTexts.WorldSettings_PlanetSizeMin);
            Controls.Add(m_planetSizeMinLabel);
            Controls.Add(m_planetMinSizeSlider);

            m_planetSizeMaxLabel = MakeLabel(MySpaceTexts.WorldSettings_PlanetSizeMax);
            Controls.Add(m_planetSizeMaxLabel);
            Controls.Add(m_planetMaxSizeSlider);


            var enablePlanetsLabel = MakeLabel(MySpaceTexts.WorldSettings_EnablePlanets);
            m_enablePlanets = new MyGuiControlCheckbox();
            m_enablePlanets.SetToolTip(MySpaceTexts.ToolTipWorldSettings_EnablePlanets);
            m_enablePlanets.IsCheckedChanged += enablePlanets_IsCheckedChanged;
            Controls.Add(enablePlanetsLabel);
            Controls.Add(m_enablePlanets);

            int numControls = 0;
            float MARGIN_TOP = 0.22f;
            float MARGIN_LEFT = 0.055f;
            float labelSize = 0.25f;
            Vector2 originL, originC;
            Vector2 controlsDelta = new Vector2(0f, 0.052f);
            float rightColumnOffset;
            originL = -m_size.Value / 2 + new Vector2(MARGIN_LEFT, MARGIN_TOP);
            originC = originL + new Vector2(labelSize, 0f);
            rightColumnOffset = originC.X + width - labelSize - 0.017f;

            foreach (var control in Controls)
            {
                control.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER;
                if (control is MyGuiControlLabel)
                    control.Position = originL + controlsDelta * numControls;
                else
                    control.Position = originC + controlsDelta * numControls++;
            }


            Vector2 buttonsOrigin = m_size.Value / 2 - new Vector2(0.23f, 0.03f);
            m_okButton = new MyGuiControlButton(position: buttonsOrigin - new Vector2(0.01f, 0f), size: MyGuiConstants.BACK_BUTTON_SIZE, text: MyTexts.Get(MySpaceTexts.Ok), onButtonClick: OkButtonClicked, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
            m_cancelButton = new MyGuiControlButton(position: buttonsOrigin + new Vector2(0.01f, 0f), size: MyGuiConstants.BACK_BUTTON_SIZE, text: MyTexts.Get(MySpaceTexts.Cancel), onButtonClick: CancelButtonClicked, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM);

            Controls.Add(m_okButton);
            Controls.Add(m_cancelButton);
        }

        void m_asteroidAmountCombo_ItemSelected()
        {
            m_asteroidAmount = (int)m_asteroidAmountCombo.GetSelectedKey();
        }

        private MyGuiControlLabel MakeLabel(MyStringId textEnum)
        {
            return new MyGuiControlLabel(text: MyTexts.GetString(textEnum), originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
        }

        public int GetFloraDensity()
        {
            return (int)m_floraDensityCombo.GetSelectedKey();
        }

        private MyFloraDensityEnum FloraDensityEnumKey(int floraDensity)
        {
            var value = (MyFloraDensityEnum)floraDensity;
            if (Enum.IsDefined(typeof(MyFloraDensityEnum), value))
            {
                return (MyFloraDensityEnum)floraDensity;
            }
            return MyFloraDensityEnum.LOW;
        }

        private void CancelButtonClicked(object sender)
        {
            this.CloseScreen();
        }

        private void OkButtonClicked(object sender)
        {
            if (OnOkButtonClicked != null)
            {
                OnOkButtonClicked();
            }

            this.CloseScreen();
        }

        protected MyGuiControlLabel AddCaption(MyStringId textEnum, Vector4? captionTextColor = null, Vector2? captionOffset = null, float captionScale = MyGuiConstants.DEFAULT_TEXT_SCALE)
        {
            return AddCaption(MyTexts.GetString(textEnum), captionTextColor: captionTextColor, captionOffset: captionOffset, captionScale: captionScale);
        }

        private void enablePlanets_IsCheckedChanged(MyGuiControlCheckbox checkBox)
        {
            m_floraDensityCombo.Enabled = checkBox.IsChecked;
            m_floraDensityLabel.Enabled = checkBox.IsChecked;

            m_moonMaxSizeSlider.Enabled = checkBox.IsChecked;
            m_moonMinSizeSlider.Enabled = checkBox.IsChecked;
            m_moonSizeMaxLabel.Enabled = checkBox.IsChecked;
            m_moonSizeMinLabel.Enabled = checkBox.IsChecked;

            m_planetMaxSizeSlider.Enabled = checkBox.IsChecked;
            m_planetMinSizeSlider.Enabled = checkBox.IsChecked;
            m_planetSizeMaxLabel.Enabled = checkBox.IsChecked;
            m_planetSizeMinLabel.Enabled = checkBox.IsChecked;

            if (checkBox.IsChecked)
            {
                m_floraDensityCombo.SelectItemByKey((int)FloraDensityEnumKey(m_parent.Settings.FloraDensity));
            }
            else
            {
                m_floraDensityCombo.SelectItemByKey((int)FloraDensityEnumKey(0));
            }
        }
    }
}
