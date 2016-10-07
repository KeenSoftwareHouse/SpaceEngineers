#region Using

using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
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

        public Queue<Tuple<string, string>> MessagesQueue = new Queue<Tuple<string, string>>();

        private int m_lastUpdateTime = int.MaxValue;

        public int Timestamp { get; private set; }

        public MyHudChat()
        {
            Timestamp = 0;
        }

        public void RegisterChat(MyMultiplayerBase multiplayer)
        {
            multiplayer.ChatMessageReceived += Multiplayer_ChatMessageReceived;
        }

        public void UnregisterChat(MyMultiplayerBase multiplayer)
        {
            multiplayer.ChatMessageReceived -= Multiplayer_ChatMessageReceived;
            MessagesQueue.Clear();
            
            UpdateTimestamp();
        }

        public void ShowMessage(string sender, string messageText)
        {
            MessagesQueue.Enqueue(new Tuple<string, string>(sender, messageText));

            if (MessagesQueue.Count > MAX_MESSAGES_IN_CHAT)
                MessagesQueue.Dequeue();

            UpdateTimestamp();
        }

        void Multiplayer_ChatMessageReceived(ulong steamUserId, string messageText, ChatEntryTypeEnum chatEntryType)
        {
            if (MySteam.IsActive)
            {
                string userName = MyMultiplayer.Static.GetMemberName(steamUserId);
                ShowMessage(userName, messageText);

                MySession.Static.GlobalChatHistory.GlobalChatHistory.Chat.Enqueue(new MyGlobalChatItem
                {
                    IdentityId = MySession.Static.Players.TryGetIdentityId(steamUserId),
                    Text = messageText
                });
            }
        }

        void UpdateTimestamp()
        {
            Timestamp++;
            m_lastUpdateTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
        }

        public void Update()
        {
            if (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastUpdateTime > MAX_MESSAGE_TIME)
            {
                if (MessagesQueue.Count > 0)
                {
                    MessagesQueue.Dequeue();
                    UpdateTimestamp();
                }
            }
        }
    }
    #endregion
}
