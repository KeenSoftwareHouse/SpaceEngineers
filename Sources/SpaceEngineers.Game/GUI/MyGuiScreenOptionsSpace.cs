using Sandbox;
using Sandbox.Engine.Utils;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Graphics.GUI;
using SpaceEngineers.Game.Gui;
using System.Text;
using VRage;
using VRageMath;

namespace SpaceEngineers.Game.GUI
{
    class MyGuiScreenOptionsSpace : MyGuiScreenBase
    {
        public MyGuiScreenOptionsSpace()
            : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(574f / 1600f, 570 / 1200f), false, null)
        {
            EnabledBackgroundFade = true;

            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            AddCaption(MySpaceTexts.ScreenCaptionOptions);

            Vector2 menuPositionOrigin = new Vector2(0.0f, -m_size.Value.Y / 2.0f + 0.146f);

            int index = 0;

            Controls.Add(new MyGuiControlButton(
                position: menuPositionOrigin + index++ * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA,
                text: MyTexts.Get(MySpaceTexts.ScreenOptionsButtonGame),
                onButtonClick: OnGameClick));

            if (MyFakes.ENABLE_DX11_RENDERER)
            {
                Controls.Add(new MyGuiControlButton(
                    position: menuPositionOrigin + index++ * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA,
                    text: new StringBuilder("Display"),
                    onButtonClick: (sender) =>
                    {
                        MyGuiSandbox.AddScreen(new MyGuiScreenOptionsDisplay());
                    }));
                Controls.Add(new MyGuiControlButton(
                    position: menuPositionOrigin + index++ * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA,
                    text: new StringBuilder("Graphics"),
                    onButtonClick: (sender) =>
                    {
                        MyGuiSandbox.AddScreen(new MyGuiScreenOptionsGraphics());
                    }));
            }
            else
            {
                Controls.Add(new MyGuiControlButton(
                    position: menuPositionOrigin + index++ * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA,
                    text: MyTexts.Get(MySpaceTexts.ScreenOptionsButtonVideo),
                    onButtonClick: OnDx9VideoClick));
            }

            Controls.Add(new MyGuiControlButton(
                position: menuPositionOrigin + index++ * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA,
                text: MyTexts.Get(MySpaceTexts.ScreenOptionsButtonAudio),
                onButtonClick: OnAudioClick));

            Controls.Add(new MyGuiControlButton(
                position: menuPositionOrigin + index++ * MyGuiConstants.MENU_BUTTONS_POSITION_DELTA,
                text: MyTexts.Get(MySpaceTexts.ScreenOptionsButtonControls),
                onButtonClick: OnControlsClick));

            CloseButtonEnabled = true;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenOptions";
        }

        public void OnGameClick(MyGuiControlButton sender)
        {
            MyGuiSandbox.AddScreen(new MyGuiScreenOptionsGame());
        }

        public void OnDx9VideoClick(MyGuiControlButton sender)
        {
            MySandboxGame.Log.WriteLine("MyGuiScreenOptions.OnVideoClick START");

            MyGuiSandbox.AddScreen(new MyGuiScreenOptionsVideoSpace());

            MySandboxGame.Log.WriteLine("MyGuiScreenOptions.OnVideoClick END");
        }

        public void OnAudioClick(MyGuiControlButton sender)
        {
            MyGuiSandbox.AddScreen(new MyGuiScreenOptionsAudio());
        }

        public void OnControlsClick(MyGuiControlButton sender)
        {
            MyGuiSandbox.AddScreen(new MyGuiScreenOptionsControls());
        }

        public void OnBackClick(MyGuiControlButton sender)
        {
            //  Just close the screen
            CloseScreen();
        }
    }
}
