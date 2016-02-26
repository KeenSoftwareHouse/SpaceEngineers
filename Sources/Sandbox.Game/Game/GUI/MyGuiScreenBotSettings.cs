using Sandbox.Engine.Utils;
using Sandbox.Game.AI;
using Sandbox.Game.Localization;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRageMath;

namespace Sandbox.Game.Gui
{
    public class MyGuiScreenBotSettings : MyGuiScreenBase
    {
        public override string GetFriendlyName()
        {
            return "MyGuiScreenBotSettings";
        }

        public MyGuiScreenBotSettings()
        {
            Size = new Vector2(650f, 350f) / MyGuiConstants.GUI_OPTIMAL_SIZE;
            BackgroundColor = MyGuiConstants.SCREEN_BACKGROUND_COLOR;
            RecreateControls(true);
            CanHideOthers = false;
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            base.m_position = new Vector2(MyGuiManager.GetMaxMouseCoord().X - 0.25f, 0.5f);

            var layout = new MyLayoutVertical(this, 35f);

            layout.Advance(20);

            {
                layout.Add(new MyGuiControlLabel(text: MyTexts.GetString(MyCommonTexts.BotSettingsScreen_Title)), MyAlignH.Center);
            }

            layout.Advance(30);

            {
                var enableDebuggingCheckBox = new MyGuiControlCheckbox(isChecked: MyDebugDrawSettings.DEBUG_DRAW_BOTS);
                enableDebuggingCheckBox.IsCheckedChanged += enableDebuggingCheckBox_IsCheckedChanged;
                layout.Add(new MyGuiControlLabel(text: MyTexts.GetString(MyCommonTexts.BotSettingsScreen_EnableBotsDebugging)), MyAlignH.Left, advance: false);
                layout.Add(enableDebuggingCheckBox, MyAlignH.Right);
            }

            layout.Advance(15);

            {
                MyGuiControlButton nextButton = new MyGuiControlButton(
                    text: MyTexts.Get(MyCommonTexts.BotSettingsScreen_NextBot),
                    onButtonClick: nextButton_OnButtonClick);
                MyGuiControlButton previousButton = new MyGuiControlButton(
                    text: MyTexts.Get(MyCommonTexts.BotSettingsScreen_PreviousBot),
                    onButtonClick: previousButton_OnButtonClick);
                layout.Add(nextButton, previousButton);
            }

            layout.Advance(30);

            {
                layout.Add(new MyGuiControlButton(text: MyTexts.Get(MyCommonTexts.Close), onButtonClick: OnCloseClicked), MyAlignH.Center);
            }
        }

        public override void HandleInput(bool receivedFocusInThisUpdate)
        {
            base.HandleInput(receivedFocusInThisUpdate);
        }

        private void enableDebuggingCheckBox_IsCheckedChanged(MyGuiControlCheckbox checkBox)
        {
            MyDebugDrawSettings.DEBUG_DRAW_BOTS = checkBox.IsChecked;
        }

        private void nextButton_OnButtonClick(MyGuiControlButton button)
        {
            MyAIComponent.Static.DebugSelectNextBot();
        }

        private void previousButton_OnButtonClick(MyGuiControlButton button)
        {
            MyAIComponent.Static.DebugSelectPreviousBot();
        }

        private void OnCloseClicked(MyGuiControlButton button)
        {
            CloseScreen();
        }
    }
}
