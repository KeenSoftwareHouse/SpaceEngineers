using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Input;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.GUI
{
    partial class MyGuiScreenConsole : MyGuiScreenBase
    {
        private enum MyConsoleKeys
        {
            UP = 0,
            DOWN = 1,
            ENTER = 2
        }
        private class MyConsoleKeyTimerController
        {
            public MyKeys Key;

            /// <summary>
            /// This is not for converting key to string, but for controling repeated key input with delay
            /// </summary>
            public int LastKeyPressTime;

            public MyConsoleKeyTimerController(MyKeys key)
            {
                Key = key;
                LastKeyPressTime = MyGuiManager.FAREST_TIME_IN_PAST;
            }
        }

        //Singleton
        private static MyGuiScreenConsole m_instance;

        //Screen Elements
        private MyGuiControlTextbox m_commandLine;
        private MyGuiControlMultilineText m_displayScreen;
        private MyGuiControlContextMenu m_autoComplete;

        //all text
        private StringBuilder m_commandText = new StringBuilder();

        //Buffer text (to memorize what you were writing)
        private string BufferText = "";

        //screen scale to fill the whole screen regardless of screen size
        private float m_screenScale;

        private Vector2 m_margin;

        //timer to control up/down shifting
        private static MyConsoleKeyTimerController[] m_keys;

        public override string GetFriendlyName()
        {
            return "Console Screen";
        }

        public MyGuiScreenConsole()
        {
            m_backgroundTexture = MyGuiConstants.TEXTURE_MESSAGEBOX_BACKGROUND_INFO.Texture;
            m_backgroundColor = new Vector4(0, 0, 0, .75f);
            m_position = new Vector2(0.5f, 0.25f);
            m_screenScale = (MyGuiManager.GetHudSize().X / MyGuiManager.GetHudSize().Y) / MyGuiConstants.SAFE_ASPECT_RATIO;
            m_size = new Vector2(m_screenScale, 0.5f);
            m_margin = new Vector2(0.06f, 0.04f);

            m_keys = new MyConsoleKeyTimerController[3];
            m_keys[(int) MyConsoleKeys.UP] = new MyConsoleKeyTimerController(MyKeys.Up);
            m_keys[(int)MyConsoleKeys.DOWN] = new MyConsoleKeyTimerController(MyKeys.Down);
            m_keys[(int)MyConsoleKeys.ENTER] = new MyConsoleKeyTimerController(MyKeys.Enter);

                    
        }

        public override void RecreateControls(bool constructor)
        {
            //This is probably very wrong!
            m_screenScale = (MyGuiManager.GetHudSize().X / MyGuiManager.GetHudSize().Y) / MyGuiConstants.SAFE_ASPECT_RATIO;

            m_size = new Vector2(m_screenScale, 0.5f);

            base.RecreateControls(constructor);
            Vector4 consoleTextColor = new Vector4(1, 1, 0, 1);
            float consoleTextScale = 1f;
            
            m_commandLine = new MyGuiControlTextbox
            (
                position: new Vector2(0, 0.25f),
                textColor: consoleTextColor
            );

            m_commandLine.Position -= new Vector2(0, m_commandLine.Size.Y + m_margin.Y/2);
            m_commandLine.Size = new Vector2(m_screenScale, m_commandLine.Size.Y) - 2 * m_margin;
            m_commandLine.ColorMask = new Vector4(0, 0, 0, 0.5f);
            m_commandLine.VisualStyle = MyGuiControlTextboxStyleEnum.Debug;
            m_commandLine.TextChanged += commandLine_TextChanged;
            m_commandLine.Name = "CommandLine";


            m_autoComplete = new MyGuiControlContextMenu();
            m_autoComplete.ItemClicked += autoComplete_ItemClicked;
            m_autoComplete.Deactivate();
            m_autoComplete.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            m_autoComplete.ColorMask = new Vector4(0, 0, 0, .5f);
            m_autoComplete.AllowKeyboardNavigation = true;
            m_autoComplete.Name = "AutoComplete";

            m_displayScreen = new MyGuiControlMultilineText
            (
                position: new Vector2(-0.5f * m_screenScale, -0.25f) + m_margin,
                size: new Vector2(m_screenScale, 0.5f - m_commandLine.Size.Y) - 2 * m_margin,
                font: MyFontEnum.Debug,
                textAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                textBoxAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                selectable: true
            );

            m_displayScreen.TextColor = Color.Yellow;
            m_displayScreen.TextScale = consoleTextScale;
            m_displayScreen.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            m_displayScreen.Text = MyConsole.DisplayScreen;
            m_displayScreen.ColorMask = new Vector4(0, 0, 0, .5f);
            m_displayScreen.Name = "DisplayScreen";

            Controls.Add(m_displayScreen);
            Controls.Add(m_commandLine);
            Controls.Add(m_autoComplete);
        }

        public static void Show()
        {
            m_instance = new MyGuiScreenConsole();
            m_instance.RecreateControls(true);

            MyGuiSandbox.AddScreen(m_instance);
        }

        protected override void OnClosed()
        {
            base.OnClosed();
        }

        public override void HandleInput(bool receivedFocusInThisUpdate)
        {
            bool handled = false;
            if (FocusedControl == m_commandLine && MyInput.Static.IsKeyPress(MyKeys.Up) && !m_autoComplete.Visible)
            {
                if (IsEnoughDelay(MyConsoleKeys.UP, MyGuiConstants.TEXTBOX_MOVEMENT_DELAY) && !m_autoComplete.Visible)
                {
                    UpdateLastKeyPressTimes(MyConsoleKeys.UP);
                    if (MyConsole.GetLine() == "")
                        BufferText = m_commandLine.Text;
                    MyConsole.PreviousLine();
                    if (MyConsole.GetLine() == "")
                        m_commandLine.Text = BufferText;
                    else
                        m_commandLine.Text = MyConsole.GetLine();
                    m_commandLine.MoveCarriageToEnd();
                }

                //Else the GUI will change focus
                handled = true;
            }

            if (FocusedControl == m_commandLine && MyInput.Static.IsKeyPress(MyKeys.Down) && !m_autoComplete.Visible)
            {
                if (IsEnoughDelay(MyConsoleKeys.DOWN, MyGuiConstants.TEXTBOX_MOVEMENT_DELAY) && !m_autoComplete.Visible)
                {
                    UpdateLastKeyPressTimes(MyConsoleKeys.DOWN);

                    if (MyConsole.GetLine() == "")
                        BufferText = m_commandLine.Text;

                    MyConsole.NextLine();
                    if (MyConsole.GetLine() == "")
                        m_commandLine.Text = BufferText;
                    else
                        m_commandLine.Text = MyConsole.GetLine();
                    m_commandLine.MoveCarriageToEnd();
                }
                handled = true;
            }

            if (FocusedControl == m_commandLine && MyInput.Static.IsKeyPress(MyKeys.Enter) && !m_commandLine.Text.Equals("") && !m_autoComplete.Visible)
            {
                if (IsEnoughDelay(MyConsoleKeys.ENTER, MyGuiConstants.TEXTBOX_MOVEMENT_DELAY))
                {
                    UpdateLastKeyPressTimes(MyConsoleKeys.ENTER);
                    if (!m_autoComplete.Visible)
                    {
                        BufferText = "";
                        MyConsole.ParseCommand(m_commandLine.Text);
                        MyConsole.NextLine();
                        m_displayScreen.Text = MyConsole.DisplayScreen;
                        m_displayScreen.ScrollbarOffset = 1;
                        m_commandLine.Text = "";
                        handled = true;
                    }
                }
            }
            if(!handled)
                base.HandleInput(receivedFocusInThisUpdate);
        }

        public void commandLine_TextChanged(MyGuiControlTextbox sender)
        {
            var text = sender.Text;

            //do not open autocomplete if text doesn't end with dot
            if (text.Length == 0 || !sender.Text.ElementAt(sender.Text.Length - 1).Equals('.'))
            {
                if (m_autoComplete.Enabled)
                {
                    m_autoComplete.Enabled = false;
                    m_autoComplete.Deactivate();
                }
                return;
            }

            //Sees if what is before the dot is a command
            MyCommand command;
            if (MyConsole.TryGetCommand(text.Substring(0, text.Length - 1), out command))
            {
                m_autoComplete.CreateNewContextMenu();
                m_autoComplete.Position = new Vector2(((1 - m_screenScale)/2) + m_margin.X, m_size.Value.Y - 2*m_margin.Y) + MyGuiManager.MeasureString(MyFontEnum.Debug, new StringBuilder(m_commandLine.Text), m_commandLine.TextScaleWithLanguage);

                foreach (var method in command.Methods)
                    m_autoComplete.AddItem(new StringBuilder(method).Append(" ").Append(command.GetHint(method)), userData: method);
               
                m_autoComplete.Enabled = true;
                m_autoComplete.Activate(false);
            }
        }

        public void autoComplete_ItemClicked(MyGuiControlContextMenu sender, MyGuiControlContextMenu.EventArgs args)
        {
            m_commandLine.Text += (string) m_autoComplete.Items[args.ItemIndex].UserData;
            m_commandLine.MoveCarriageToEnd();
            FocusedControl = m_commandLine;
        }


        private bool IsEnoughDelay(MyConsoleKeys key, int forcedDelay)
        {
            MyConsoleKeyTimerController keyEx = m_keys[(int)key];
            if (keyEx == null) return true;

            return ((MyGuiManager.TotalTimeInMilliseconds - keyEx.LastKeyPressTime) > forcedDelay);
        }

        private void UpdateLastKeyPressTimes(MyConsoleKeys key)
        {

            MyConsoleKeyTimerController keyEx = m_keys[(int)key];
            if (keyEx != null)
            {
                keyEx.LastKeyPressTime = MyGuiManager.TotalTimeInMilliseconds;
            }
            
        }
    }
}
