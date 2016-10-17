#region Using

using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using SteamSDK;
using System;
using System.Collections.Generic;
using VRage.Game;


#endregion

namespace Sandbox.Game.Gui
{
    #region Chat

    public class MyHudChat
    {
        static readonly int MAX_MESSAGES_IN_CHAT = 10;
        static readonly int MAX_MESSAGE_TIME = 15000; //ms

        public Queue<Tuple<string, string, MyFontEnum>> MessagesQueue = new Queue<Tuple<string, string, MyFontEnum>>();

        private int m_lastUpdateTime = int.MaxValue;

        public int Timestamp { get; private set; }

        public MyHudChat()
        {
            Timestamp = 0;
        }

        public void RegisterChat(MyMultiplayerBase multiplayer)
        {
            multiplayer.ChatMessageReceived += Multiplayer_ChatMessageReceived;
            multiplayer.ScriptedChatMessageReceived += multiplayer_ScriptedChatMessageReceived;
        }

        public void UnregisterChat(MyMultiplayerBase multiplayer)
        {
            multiplayer.ChatMessageReceived -= Multiplayer_ChatMessageReceived;
            multiplayer.ScriptedChatMessageReceived -= multiplayer_ScriptedChatMessageReceived;
            MessagesQueue.Clear();
            
            UpdateTimestamp();
        }

        public void ShowMessage(string sender, string messageText, MyFontEnum font = MyFontEnum.Blue)
        {
            MessagesQueue.Enqueue(new Tuple<string, string, MyFontEnum>(sender, messageText, font));

            if (MessagesQueue.Count > MAX_MESSAGES_IN_CHAT)
                MessagesQueue.Dequeue();

            UpdateTimestamp();
        }

        void Multiplayer_ChatMessageReceived(ulong steamUserId, string messageText, ChatEntryTypeEnum chatEntryType)
        {
            if (MySteam.IsActive)
            {
                string userName = MyMultiplayer.Static.GetMemberName(steamUserId);
                ShowMessage(userName, messageText, steamUserId == MySteam.UserId ? MyFontEnum.DarkBlue : MyFontEnum.Blue);

                MySession.Static.GlobalChatHistory.GlobalChatHistory.Chat.Enqueue(new MyGlobalChatItem
                {
                    IdentityId = MySession.Static.Players.TryGetIdentityId(steamUserId),
                    Text = messageText
                });
            }
        }

        public void multiplayer_ScriptedChatMessageReceived(string message, string author, MyFontEnum font)
        {
            if (MySteam.IsActive)
            {
                ShowMessage(author, message, font);

                MySession.Static.GlobalChatHistory.GlobalChatHistory.Chat.Enqueue(new MyGlobalChatItem
                {
                    Author = author,
                    Text = message,
                    AuthorFont = font
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
