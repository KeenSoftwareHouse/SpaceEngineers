﻿using Sandbox.Common.ObjectBuilders;
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
using VRageRender;

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
        MyGuiControlLabel m_asteroidAmountLabel, m_floraDensityLabel;

        public override string GetFriendlyName()
        {
            return "MyGuiScreenWorldGeneratorSettings";
        }

        public static Vector2 CalcSize()
        {
            float width = 0.65f;
            float height = 0.3f;
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
        }

        protected virtual void SetSettingsToControls()
        {
            AsteroidAmount = m_parent.AsteroidAmount;
        }

        public override void RecreateControls(bool constructor)
        {
            float width = 0.284375f + 0.025f;
            base.RecreateControls(constructor);

            AddCaption(MySpaceTexts.ScreenCaptionWorldGeneratorSettings);
          
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

            m_asteroidAmountLabel = MakeLabel(MySpaceTexts.Asteroid_Amount);
            Controls.Add(m_asteroidAmountLabel);
            Controls.Add(m_asteroidAmountCombo);

            int numControls = 0;
            float MARGIN_TOP = 0.12f;
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

        void grassDensitySlider_ValueChanged(MyGuiControlSlider slider)
        {
            MyRenderProxy.Settings.GrassDensityFactor = slider.Value;
        }

        void m_asteroidAmountCombo_ItemSelected()
        {
            m_asteroidAmount = (int)m_asteroidAmountCombo.GetSelectedKey();
        }

        private MyGuiControlLabel MakeLabel(MyStringId textEnum)
        {
            return new MyGuiControlLabel(text: MyTexts.GetString(textEnum), originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
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

        protected new MyGuiControlLabel AddCaption(MyStringId textEnum, Vector4? captionTextColor = null, Vector2? captionOffset = null, float captionScale = MyGuiConstants.DEFAULT_TEXT_SCALE)
        {
            return AddCaption(MyTexts.GetString(textEnum), captionTextColor: captionTextColor, captionOffset: captionOffset, captionScale: captionScale);
        }    
    }
}
