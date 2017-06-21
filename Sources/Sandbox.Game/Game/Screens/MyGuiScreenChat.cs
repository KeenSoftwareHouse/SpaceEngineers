using Sandbox.Common;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using SteamSDK;
using System.Text;
using VRage;
using VRage.Input;
using VRage.Utils;
using VRage.Compiler;
using VRage.Game;
using VRageMath;

namespace Sandbox.Game.Gui
{
    class MyGuiScreenChat : MyGuiScreenBase
    {
        readonly MyGuiControlTextbox m_chatTextbox;

        public MyGuiControlTextbox ChatTextbox
        {
            get { return m_chatTextbox; }
        }

        public static MyGuiScreenChat Static = null;

        private const int MESSAGE_HISTORY_SIZE = 20;

        private static StringBuilder[] m_messageHistory = new StringBuilder[MESSAGE_HISTORY_SIZE];
        private static int m_messageHistoryPushTo = 0;
        private static int m_messageHistoryShown = 0;

        static MyGuiScreenChat()
        {
            for (int i = 0; i < MESSAGE_HISTORY_SIZE; ++i)
            {
                m_messageHistory[i] = new StringBuilder();
            }
        }

        public MyGuiScreenChat(Vector2 position)
            : base(position, MyGuiConstants.SCREEN_BACKGROUND_COLOR, null)
        {
            MySandboxGame.Log.WriteLine("MyGuiScreenChat.ctor START");

            EnabledBackgroundFade = false;
            m_isTopMostScreen = true;
            CanHideOthers = false;
            DrawMouseCursor = false;
            m_closeOnEsc = true;
            
            m_chatTextbox = new MyGuiControlTextbox(
                Vector2.Zero,
                null,
                ChatMessageBuffer.MAX_MESSAGE_SIZE);
            m_chatTextbox.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            m_chatTextbox.Size = new Vector2(0.4f, 0.05f);
            m_chatTextbox.TextScale = 0.8f;
            m_chatTextbox.VisualStyle = MyGuiControlTextboxStyleEnum.Default;

            Controls.Add(m_chatTextbox);

            MySandboxGame.Log.WriteLine("MyGuiScreenChat.ctor END");
        }

        public override void HandleInput(bool receivedFocusInThisUpdate)
        {
            if (
                MyInput.Static.IsNewKeyPressed(MyKeys.Left) ||
                MyInput.Static.IsNewKeyPressed(MyKeys.Right) ||
                MyInput.Static.IsNewKeyPressed(MyKeys.Tab) 
                )
            {
                return;
            }

            if (MyInput.Static.IsNewKeyPressed(MyKeys.Down))
            {
                HistoryUp();
                return;
            }

            if (MyInput.Static.IsNewKeyPressed(MyKeys.Up))
            {
                HistoryDown();
                return;
            }

            base.HandleInput(receivedFocusInThisUpdate);

            if (MyInput.Static.IsNewKeyPressed(MyKeys.Enter))
            {
                string message = m_chatTextbox.Text;

                PushHistory(message);

                if (!MyFinalBuildConstants.IS_OFFICIAL && message.StartsWith("\\"))
                {
                    Process(message);
                }
                else if (!string.IsNullOrWhiteSpace(message))
                {
                    bool send = true;
                    Sandbox.ModAPI.MyAPIUtilities.Static.EnterMessage(message, ref send);
                    if (send)
                    {
                        if (MyMultiplayer.Static != null)
                            MyMultiplayer.Static.SendChatMessage(message);
                        else
                            MyHud.Chat.ShowMessage(MySession.Static.LocalHumanPlayer == null ? "Player" : MySession.Static.LocalHumanPlayer.DisplayName, message);
                    }
                }
                CloseScreen();
            }
        }

        public override bool Update(bool hasFocus)
        {
            if (!base.Update(hasFocus)) return false;

            var normPos = m_position;
            normPos = MyGuiScreenHudBase.ConvertHudToNormalizedGuiPosition(ref normPos);
            m_chatTextbox.Position = new Vector2(0.15f, 0);
            return true;
        }

        public override bool Draw()
        {
            return base.Draw();
        }

        private static void PushHistory(string message)
        {
            m_messageHistory[m_messageHistoryPushTo].Clear().Append(message);
            m_messageHistoryPushTo = HistoryIndexUp(m_messageHistoryPushTo);
            m_messageHistoryShown = m_messageHistoryPushTo;
            m_messageHistory[m_messageHistoryPushTo].Clear();
        }

        private void HistoryDown()
        {
            int previousIndex = HistoryIndexDown(m_messageHistoryShown);
            if (previousIndex == m_messageHistoryPushTo) return;

            m_messageHistoryShown = previousIndex;
            m_chatTextbox.Text = m_messageHistory[m_messageHistoryShown].ToString() ?? "";
        }

        private void HistoryUp()
        {
            if (m_messageHistoryShown == m_messageHistoryPushTo) return;

            m_messageHistoryShown = HistoryIndexUp(m_messageHistoryShown);
            m_chatTextbox.Text = m_messageHistory[m_messageHistoryShown].ToString() ?? "";
        }

        private static int HistoryIndexUp(int index)
        {
            index = index + 1;
            if (index >= MESSAGE_HISTORY_SIZE) return 0;
            return index;
        }

        private static int HistoryIndexDown(int index)
        {
            index = index - 1;
            if (index < 0) return MESSAGE_HISTORY_SIZE - 1;
            return index;
        }

        private void Process(string message)
        {
            var line = message.Substring(1);
            IlCompiler.Buffer.Append(line);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenChat";
        }

        public override void LoadContent()
        {
            base.LoadContent();
            Static = this;
        }

        public override void UnloadContent()
        {
            Static = null;
            base.UnloadContent();            
        }

        public override bool HideScreen()
        {
            UnloadContent();
            return base.HideScreen();
        }
       
    }
}