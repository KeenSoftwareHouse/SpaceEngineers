using Sandbox.Game.Localization;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Utils;
using VRageMath;
using VRage.Library;

namespace Sandbox.Game.Screens
{
    public class MyGuiScreenBriefing : MyGuiScreenBase
    {
        public static MyGuiScreenBriefing Static;

        MyGuiControlLabel m_mainLabel;
        MyGuiControlMultilineText m_descriptionBox;
        
        protected MyGuiControlButton m_okButton;

        public string Briefing
        {
            set { m_descriptionBox.Text = new StringBuilder(value);
            //m_descriptionBox.RefreshText();
            }
        }


        public MyGuiScreenBriefing()
            : base(position: new Vector2(0.5f, 0.5f), backgroundColor: MyGuiConstants.SCREEN_BACKGROUND_COLOR, size: new Vector2(1620f, 1125f) / MyGuiConstants.GUI_OPTIMAL_SIZE)
        {
            Static = this;
            RecreateControls(true);
            FillData();
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);
            var layout = new MyLayoutTable(this);
            layout.SetColumnWidthsNormalized(50, 250, 150, 250, 50);
            layout.SetRowHeightsNormalized(50, 450, 30, 50);

            m_mainLabel = new MyGuiControlLabel(text: MyTexts.GetString(MySpaceTexts.GuiScenarioDescription), originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            layout.AddWithSize(m_mainLabel, MyAlignH.Left, MyAlignV.Center, 0, 1, colSpan: 3);
            //BRIEFING:
            m_descriptionBox = new MyGuiControlMultilineText(
                position: new Vector2(0.0f, 0.0f),
                size: new Vector2(0.2f, 0.2f),
                textBoxAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                selectable: false);
            layout.AddWithSize(m_descriptionBox, MyAlignH.Left, MyAlignV.Top, 1, 1, rowSpan: 1, colSpan: 3);

            m_okButton = new MyGuiControlButton(text: MyTexts.Get(MyCommonTexts.Ok), visualStyle: MyGuiControlButtonStyleEnum.Rectangular, highlightType: MyGuiControlHighlightType.WHEN_ACTIVE,
                size: new Vector2(200, 48f) / MyGuiConstants.GUI_OPTIMAL_SIZE, onButtonClick: OnOkClicked);
            layout.AddWithSize(m_okButton, MyAlignH.Left, MyAlignV.Top, 2, 2);
        }
        private void FillData()
        {
            m_descriptionBox.Text.Clear().Append(MySession.Static.GetWorld().Checkpoint.Briefing).Append(MyEnvironment.NewLine).Append(MyEnvironment.NewLine);
            m_descriptionBox.Text.Append(MyEnvironment.NewLine).Append(MySessionComponentMissionTriggers.GetProgress(MySession.Static.LocalHumanPlayer));
            m_descriptionBox.RefreshText(false);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenBriefing";
        }

        public override bool Update(bool hasFocus)
        {
            return base.Update(hasFocus);
        }

        protected virtual void OnOkClicked(MyGuiControlButton sender)
        {
            CloseScreen();
        }

                    
        protected override void OnClosed()
        {
            base.OnClosed();
        }

    }

}
