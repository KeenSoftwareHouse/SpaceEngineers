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
        static readonly int MAX_MESSAGES_IN_CHAT_DEFAULT = 10;
        public static readonly int MAX_MESSAGE_TIME_DEFAULT = 15000; //ms

        public static int MaxMessageTime = MAX_MESSAGE_TIME_DEFAULT;
        public static int MaxMessageCount = MAX_MESSAGES_IN_CHAT_DEFAULT;

        public Queue<Tuple<string, string, string>> MessagesQueue = new Queue<Tuple<string, string, string>>();

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

        public void ShowMessage(string sender, string messageText, string font = MyFontEnum.Blue)
        {
            MessagesQueue.Enqueue(new Tuple<string, string, string>(sender, messageText, font));

            if (MessagesQueue.Count > MaxMessageCount)
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

        public void multiplayer_ScriptedChatMessageReceived(string message, string author, string font)
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
            if (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastUpdateTime > MaxMessageTime)
            {
                if (MessagesQueue.Count > 0)
                {
                    MessagesQueue.Dequeue();
                    UpdateTimestamp();
                }
            }
        }

        public static void ResetChatSettings()
        {
            MaxMessageTime = MAX_MESSAGE_TIME_DEFAULT;
            MaxMessageCount = MAX_MESSAGES_IN_CHAT_DEFAULT;
        }

    }
    #endregion
}
