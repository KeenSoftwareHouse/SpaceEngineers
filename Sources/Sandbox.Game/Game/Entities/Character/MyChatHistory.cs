using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Utils;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;

namespace Sandbox.Game.Entities.Character
{
    [PreloadRequired]
    public class MyChatHistory
    {
        static public string LongToBase64(long valueToConvert)
        {
            var bytes = BitConverter.GetBytes(valueToConvert);
            return Convert.ToBase64String(bytes);
        }

        static public long Base64ToLong(string base64)
        {
            var bytes = Convert.FromBase64String(base64);
            System.Diagnostics.Debug.Assert(bytes.Length == 8, "Invalid number of bytes for long!");
            return BitConverter.ToInt64(bytes, 0);
        }

        private long m_identityId;
        /// <summary>
        /// Identifies the owner of this chat history
        /// </summary>
        public long IdentityId
        {
            get
            {
                return m_identityId;
            }
        }

        private Dictionary<long, MyPlayerChatHistory> m_playerChatHistory;
        /// <summary>
        /// Chat history with other players. Key identifies the other member of the conversation (PlayerChatHistory.PlayerId).
        /// </summary>
        public Dictionary<long, MyPlayerChatHistory> PlayerChatHistory
        {
            get
            {
                return m_playerChatHistory;
            }
        }

        private MyGlobalChatHistory m_globalChatHistory;
        public MyGlobalChatHistory GlobalChatHistory
        {
            get
            {
                return m_globalChatHistory;
            }
        }

        public MyChatHistory(long identityId)
        {
            m_playerChatHistory = new Dictionary<long, MyPlayerChatHistory>();
            m_globalChatHistory = new MyGlobalChatHistory();

            m_identityId = identityId;
        }

        public MyChatHistory(MyObjectBuilder_ChatHistory chatBuilder)
            : this(0)
        {
            Init(chatBuilder);
        }

        public void Init(MyObjectBuilder_ChatHistory chatBuilder)
        {
            if (chatBuilder != null)
            {
                m_identityId = chatBuilder.IdentityId;
            }

            if (chatBuilder != null && chatBuilder.PlayerChatHistory != null)
            {
                foreach (var playerChat in chatBuilder.PlayerChatHistory)
                {
                    m_playerChatHistory.Add(playerChat.IdentityId, new MyPlayerChatHistory(playerChat));
                }
            }

            if (chatBuilder != null && chatBuilder.GlobalChatHistory != null)
            {
                m_globalChatHistory.Init(chatBuilder.GlobalChatHistory);
            }
        }

        public MyObjectBuilder_ChatHistory GetObjectBuilder()
        {
            var objectBuilder = new MyObjectBuilder_ChatHistory();

            objectBuilder.IdentityId = IdentityId;

            objectBuilder.PlayerChatHistory = new List<MyObjectBuilder_PlayerChatHistory>(m_playerChatHistory.Count);
            foreach (var playerChat in m_playerChatHistory.Values)
            {
                objectBuilder.PlayerChatHistory.Add(playerChat.GetObjectBuilder());
            }

            objectBuilder.GlobalChatHistory = m_globalChatHistory.GetObjectBuilder();
            return objectBuilder;
        }

        public void AddPlayerChatItem(MyPlayerChatItem chatItem, long senderId)
        {
            MyPlayerChatHistory playerChat;
            if (PlayerChatHistory.TryGetValue(senderId, out playerChat))
            {
                if (playerChat.Chat.Count == MyChatConstants.MAX_PLAYER_CHAT_HISTORY_COUNT)
                {
                    playerChat.Chat.Dequeue();
                }
                playerChat.Chat.Enqueue(chatItem);
            }
            else
            {
                var newChatHistory = new Entities.Character.MyPlayerChatHistory(senderId);
                newChatHistory.Chat.Enqueue(chatItem);
                
                PlayerChatHistory.Add(senderId, newChatHistory);
            }
        }

        public void AddGlobalChatItem(MyGlobalChatItem chatItem)
        {
            if (m_globalChatHistory.Chat.Count == MyChatConstants.MAX_GLOBAL_CHAT_HISTORY_COUNT)
            {
                m_globalChatHistory.Chat.Dequeue();
            }
            m_globalChatHistory.Chat.Enqueue(chatItem);
        }
    }

    public class MyPlayerChatHistory
    {
        protected Queue<MyPlayerChatItem> m_chat;
        public Queue<MyPlayerChatItem> Chat
        {
            get
            {
                return m_chat;
            }
        }

        private long m_identityId;
        public long IdentityId
        {
            get
            {
                return m_identityId;
            }
        }

        //This is not saved
        public int UnreadMessageCount
        {
            get;
            set;
        }

        public MyPlayerChatHistory(long identityId)
        {
            m_chat = new Queue<MyPlayerChatItem>();
            m_identityId = identityId;
        }

        public MyPlayerChatHistory(MyObjectBuilder_PlayerChatHistory chatBuilder)
            : this(chatBuilder.IdentityId)
        {
            if (chatBuilder.Chat != null)
            {
                m_chat = new Queue<MyPlayerChatItem>(chatBuilder.Chat.Count);
                foreach (var chatItem in chatBuilder.Chat)
                {
                    MyPlayerChatItem newChatItem = new MyPlayerChatItem();
                    newChatItem.Init(chatItem);
                    m_chat.Enqueue(newChatItem);
                }
            }
            else
            {
                m_chat = new Queue<MyPlayerChatItem>();
            }
        }

        public MyObjectBuilder_PlayerChatHistory GetObjectBuilder()
        {
            var objectBuilder = new MyObjectBuilder_PlayerChatHistory();

            objectBuilder.Chat = new List<MyObjectBuilder_PlayerChatItem>(m_chat.Count);
            foreach (var chatItem in m_chat)
            {
                objectBuilder.Chat.Add(chatItem.GetObjectBuilder());
            }
            objectBuilder.IdentityId = m_identityId;

            return objectBuilder;
        }
    }

    public class MyFactionChatHistory
    {
        protected Queue<MyFactionChatItem> m_chat;
        public Queue<MyFactionChatItem> Chat
        {
            get
            {
                return m_chat;
            }
        }

        private long m_factionId1;
        public long FactionId1
        {
            get
            {
                return m_factionId1;
            }
        }

        private long m_factionId2;
        public long FactionId2
        {
            get
            {
                return m_factionId2;
            }
        }

        //This is not saved
        public int UnreadMessageCount
        {
            get;
            set;
        }
        
        public MyFactionChatHistory(long factionId1, long factionId2)
        {
            m_chat = new Queue<MyFactionChatItem>();
            m_factionId1 = factionId1;
            m_factionId2 = factionId2;
        }

        public MyFactionChatHistory(MyObjectBuilder_FactionChatHistory chatBuilder)
            : this(chatBuilder.FactionId1, chatBuilder.FactionId2)
        {
            if (chatBuilder.Chat != null)
            {
                m_chat = new Queue<MyFactionChatItem>(chatBuilder.Chat.Count);
                foreach (var chatItem in chatBuilder.Chat)
                {
                    MyFactionChatItem newChatItem = new MyFactionChatItem();
                    newChatItem.Init(chatItem);
                    m_chat.Enqueue(newChatItem);
                }
            }
            else
            {
                m_chat = new Queue<MyFactionChatItem>();
            }
            m_factionId1 = chatBuilder.FactionId1;
            m_factionId2 = chatBuilder.FactionId2;
        }

        public MyObjectBuilder_FactionChatHistory GetObjectBuilder()
        {
            var objectBuilder = new MyObjectBuilder_FactionChatHistory();


            objectBuilder.Chat = new List<MyObjectBuilder_FactionChatItem>(m_chat.Count);
            foreach (var chatItem in m_chat)
            {
                if (chatItem.PlayersToSendTo != null && chatItem.PlayersToSendTo.Count > 0)
                {
                    objectBuilder.Chat.Add(chatItem.GetObjectBuilder());
                }
            }
            objectBuilder.FactionId1 = m_factionId1;
            objectBuilder.FactionId2 = m_factionId2;

            return objectBuilder;
        }
    }

    public class MyGlobalChatHistory
    {
        protected Queue<MyGlobalChatItem> m_chat;
        public Queue<MyGlobalChatItem> Chat
        {
            get
            {
                return m_chat;
            }
        }

        //This is not saved
        public int UnreadMessageCount
        {
            get;
            set;
        }

        public MyGlobalChatHistory()
        {
            m_chat = new Queue<MyGlobalChatItem>();
        }

        public void Init(MyObjectBuilder_GlobalChatHistory chatBuilder)
        {
            if (chatBuilder.Chat != null)
            {
                m_chat = new Queue<MyGlobalChatItem>(chatBuilder.Chat.Count);
                foreach (var chatItem in chatBuilder.Chat)
                {
                    MyGlobalChatItem newChatItem = new MyGlobalChatItem();
                    newChatItem.Init(chatItem);
                    m_chat.Enqueue(newChatItem);
                }
            }
            else
            {
                m_chat = new Queue<MyGlobalChatItem>();
            }
        }

        public MyObjectBuilder_GlobalChatHistory GetObjectBuilder()
        {
            var objectBuilder = new MyObjectBuilder_GlobalChatHistory();

            objectBuilder.Chat = new List<MyObjectBuilder_GlobalChatItem>(m_chat.Count);
            foreach (var chatItem in m_chat)
            {
                objectBuilder.Chat.Add(chatItem.GetObjectBuilder());
            }

            return objectBuilder;
        }
    }

    public class MyPlayerChatItem
    {
        public string Text;
        public long IdentityId;
        public TimeSpan Timestamp;
        public bool Sent;

        public MyPlayerChatItem()
        {
        }

        public MyPlayerChatItem(string text, long identityId, long timestampMs, bool sent)
            : this(text, identityId, new TimeSpan(timestampMs), sent)
        {
        }

        public MyPlayerChatItem(string text, long identityId, TimeSpan timestamp, bool sent)
        {
            Text = text;
            IdentityId = identityId;
            Timestamp = timestamp;
            Sent = sent;
        }

        public void Init(MyObjectBuilder_PlayerChatItem chatBuilder)
        {
            Text = chatBuilder.Text;
            IdentityId = MyEntityIdentifier.ConstructId(MyEntityIdentifier.ID_OBJECT_TYPE.IDENTITY, chatBuilder.IdentityIdUniqueNumber); 
            Timestamp = TimeSpan.FromMilliseconds(chatBuilder.TimestampMs);
            Sent = chatBuilder.Sent;
        }

        public MyObjectBuilder_PlayerChatItem GetObjectBuilder()
        {
            var objectBuilder = new MyObjectBuilder_PlayerChatItem();

            objectBuilder.Text = Text;
            objectBuilder.IdentityIdUniqueNumber = MyEntityIdentifier.GetIdUniqueNumber(IdentityId);
            objectBuilder.TimestampMs = (long)Timestamp.TotalMilliseconds;
            objectBuilder.Sent = Sent;

            return objectBuilder;
        }
    }

    public class MyFactionChatItem
    {
        public string Text;
        public long IdentityId;
        public TimeSpan Timestamp;
        /// <summary>
        /// Stores players that should receive this message
        /// Value means if the player already received the message
        /// Can be NULL!
        public Dictionary<long, bool> PlayersToSendTo;

        public MyFactionChatItem()
        {
        }

        public MyFactionChatItem(string text, long identityId, long timestampMs, Dictionary<long, bool> playersToSendTo)
            : this(text, identityId, TimeSpan.FromMilliseconds(timestampMs), playersToSendTo)
        {
        }

        public MyFactionChatItem(string text, long identityId, TimeSpan timestamp, Dictionary<long, bool> playersToSendTo)
        {
            Text = text;
            IdentityId = identityId;
            Timestamp = timestamp;
            PlayersToSendTo = playersToSendTo;
        }

        public void Init(MyObjectBuilder_FactionChatItem chatBuilder)
        {
            Text = chatBuilder.Text;
            IdentityId = MyEntityIdentifier.ConstructId(MyEntityIdentifier.ID_OBJECT_TYPE.IDENTITY, chatBuilder.IdentityIdUniqueNumber);
            Timestamp = TimeSpan.FromMilliseconds(chatBuilder.TimestampMs);
            PlayersToSendTo = new Dictionary<long,bool>();
            if (chatBuilder.PlayersToSendToUniqueNumber != null && chatBuilder.PlayersToSendToUniqueNumber.Count != 0)
            {
                for (int i = 0; i < chatBuilder.PlayersToSendToUniqueNumber.Count; i++)
                {
                    PlayersToSendTo.Add(MyEntityIdentifier.ConstructId(MyEntityIdentifier.ID_OBJECT_TYPE.IDENTITY, chatBuilder.PlayersToSendToUniqueNumber[i]), chatBuilder.IsAlreadySentTo[i]);
                }
            }
        }

        public MyObjectBuilder_FactionChatItem GetObjectBuilder()
        {
            var objectBuilder = new MyObjectBuilder_FactionChatItem();

            objectBuilder.Text = Text;
            objectBuilder.IdentityIdUniqueNumber = MyEntityIdentifier.GetIdUniqueNumber(IdentityId);
            objectBuilder.TimestampMs = (long)Timestamp.TotalMilliseconds;
            if (PlayersToSendTo != null)
            {
                objectBuilder.PlayersToSendToUniqueNumber = PlayersToSendTo.Keys.ToList();
                for (int i = 0; i < objectBuilder.PlayersToSendToUniqueNumber.Count; i++)
                {
                    objectBuilder.PlayersToSendToUniqueNumber[i] = MyEntityIdentifier.GetIdUniqueNumber(objectBuilder.PlayersToSendToUniqueNumber[i]);
                }
                objectBuilder.IsAlreadySentTo = PlayersToSendTo.Values.ToList();
            }
            return objectBuilder;
        }
    }

    public class MyGlobalChatItem
    {
        public string Text = "";
        public long IdentityId = 0;
        public string Author = "";
        public string AuthorFont = MyFontEnum.Blue;

        public MyGlobalChatItem()
        {
        }

        public MyGlobalChatItem(string text, long identityId)
        {
            Text = text;
            IdentityId = identityId;
        }

        public void Init(MyObjectBuilder_GlobalChatItem chatBuilder)
        {
            Text = chatBuilder.Text;
            AuthorFont = chatBuilder.Font;
            if (chatBuilder.IdentityIdUniqueNumber == 0)
            {
                IdentityId = 0;
                Author = chatBuilder.Author;
            }
            else
            {
                IdentityId = MyEntityIdentifier.ConstructId(MyEntityIdentifier.ID_OBJECT_TYPE.IDENTITY, chatBuilder.IdentityIdUniqueNumber);
                Author = "";
            }
        }

        public MyObjectBuilder_GlobalChatItem GetObjectBuilder()
        {
            var objectBuilder = new MyObjectBuilder_GlobalChatItem();

            objectBuilder.Text = Text;
            objectBuilder.Font = AuthorFont;
            if (IdentityId == 0)
            {
                objectBuilder.IdentityIdUniqueNumber = 0;
                objectBuilder.Author = Author;
            }
            else
            {
                objectBuilder.IdentityIdUniqueNumber = MyEntityIdentifier.GetIdUniqueNumber(IdentityId);
                objectBuilder.Author = "";
            }
        
            return objectBuilder;
        }
    }
}
