using System.Text;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using VRage;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Screens.Helpers
{
    public class MyGuiControlScreenSwitchPanel : MyGuiControlParent
    {
        public MyGuiControlScreenSwitchPanel(MyGuiScreenBase owner, StringBuilder ownerDescription)
        {
            var currentPosition = Vector2.Zero;

            var descriptingText = new MyGuiControlMultilineText
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
                Position = new Vector2(owner.Size.Value.X / 2 - 0.015f, 0.113f),
                Size = new Vector2(owner.Size.Value.X - 0.1f, 0.03f),
                TextAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                TextBoxAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                Text = ownerDescription,
                Font = "DarkBlue"
            };

            var campaignButton = new MyGuiControlButton(
                position: currentPosition,
                textScale: MyGuiConstants.MAIN_MENU_BUTTON_TEXT_SCALE,
                text: MyTexts.Get(MyCommonTexts.ScreenCaptionNewGame),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                onButtonClick: OnCampaignButtonClick
                );

            currentPosition.X += campaignButton.Size.X + MyGuiConstants.GENERIC_BUTTON_SPACING.X;
            var customWorldButton = new MyGuiControlButton(
                position: currentPosition,
                textScale: MyGuiConstants.MAIN_MENU_BUTTON_TEXT_SCALE,
                text: MyTexts.Get(MyCommonTexts.ScreenCaptionCustomWorld),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                onButtonClick: OnCustomWorldButtonClick
                );

            currentPosition.X += customWorldButton.Size.X + MyGuiConstants.GENERIC_BUTTON_SPACING.X;
            var worshopWorldsButton = new MyGuiControlButton(
                position: currentPosition,
                textScale: MyGuiConstants.MAIN_MENU_BUTTON_TEXT_SCALE,
                text: MyTexts.Get(MyCommonTexts.ScreenCaptionWorkshop),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                onButtonClick: OnWorshopButtonClick
                );

            var separator = new MyGuiControlSeparatorList();
            separator.AddHorizontal(new Vector2(0.02f, 0.083f), owner.Size.Value.X - 0.07f);
            separator.AddHorizontal(new Vector2(0.02f, 0.14f), owner.Size.Value.X - 0.07f);
            
            if(owner is MyGuiScreenNewGame)
            {
                owner.FocusedControl = campaignButton;   
            } 
            else if(owner is MyGuiScreenWorldSettings)
            {
                owner.FocusedControl = customWorldButton;
            }
            else if(owner is MyGuiScreenLoadSubscribedWorld)
            {
                owner.FocusedControl = worshopWorldsButton;
            }

            Controls.Add(descriptingText);
            Controls.Add(separator);
            Controls.Add(campaignButton);
            Controls.Add(customWorldButton);
            Controls.Add(worshopWorldsButton);

            var offset = new Vector2(0.013f, 0.02f);
            Position = - owner.Size.Value / 2 + offset;

            owner.Controls.Add(this);
        }

        private void OnCampaignButtonClick(MyGuiControlButton myGuiControlButton)
        {
            var focused = MyScreenManager.GetScreenWithFocus();
            if (focused is MyGuiScreenNewGame) return;
            SeamlesslyChangeScreen(focused, new MyGuiScreenNewGame());
        }

        private void OnCustomWorldButtonClick(MyGuiControlButton myGuiControlButton)
        {
            var focused = MyScreenManager.GetScreenWithFocus();
            if (focused is MyGuiScreenWorldSettings) return;
            SeamlesslyChangeScreen(focused, new MyGuiScreenWorldSettings());
        }

        private void OnWorshopButtonClick(MyGuiControlButton myGuiControlButton)
        {
            var focused = MyScreenManager.GetScreenWithFocus();
            if (focused is MyGuiScreenLoadSubscribedWorld) return;
            SeamlesslyChangeScreen(focused, new MyGuiScreenLoadSubscribedWorld());
        }

        private static void SeamlesslyChangeScreen(MyGuiScreenBase focusedScreen, MyGuiScreenBase exchangedFor)
        {
            focusedScreen.SkipTransition = true;
            focusedScreen.CloseScreen();
            exchangedFor.SkipTransition = true;
            MyScreenManager.AddScreenNow(exchangedFor);
        }
    }
}
