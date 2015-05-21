#region Using

using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Graphics.GUI;
using SteamSDK;
using System;
using System.Collections.Generic;


#endregion

namespace Sandbox.Game.Gui
{
    #region Chat

    public class MyHudChat
    {
        static readonly int MAX_MESSAGES_IN_CHAT = 10;
        static readonly int MAX_MESSAGE_TIME = 15000; //ms

        public bool Visible { get; set; }

        public Queue<Tuple<string, string>> MessagesQueue = new Queue<Tuple<string, string>>();
        public bool Dirty = true;

        private int m_lastChatUpdate = int.MaxValue;

        public MyHudChat()
        {
            Visible = true;
        }

        public void RegisterChat(MyMultiplayerBase multiplayer)
        {
            multiplayer.ChatMessageReceived += Multiplayer_ChatMessageReceived;
        }

        public void UnregisterChat(MyMultiplayerBase multiplayer)
        {
            multiplayer.ChatMessageReceived -= Multiplayer_ChatMessageReceived;
            MessagesQueue.Clear();
            
            SetDirty();
        }

        public void ShowMessage(string sender, string messageText)
        {
            MessagesQueue.Enqueue(new Tuple<string, string>(sender, messageText));

            if (MessagesQueue.Count > MAX_MESSAGES_IN_CHAT)
                MessagesQueue.Dequeue();

            SetDirty();
        }

        void Multiplayer_ChatMessageReceived(ulong steamUserId, string messageText, ChatEntryTypeEnum chatEntryType)
        {
            if (MySteam.IsActive)
            {
                string userName = MyMultiplayer.Static.GetMemberName(steamUserId);
                ShowMessage(userName, messageText);
            }
        }

        void SetDirty()
        {
            Dirty = true;
            m_lastChatUpdate = MySandboxGame.TotalGamePlayTimeInMilliseconds;
        }

        public void Update()
        {
            if (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastChatUpdate > MAX_MESSAGE_TIME)
            {
                if (MessagesQueue.Count > 0)
                {
                    MessagesQueue.Dequeue();
                    SetDirty();
                }
            }
        }
    }
    #endregion
}
