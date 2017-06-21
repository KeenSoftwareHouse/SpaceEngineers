using ProtoBuf;
using Sandbox.Common;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Components;

namespace Sandbox.Game.GameSystems
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation, 900)]
    public class MyChatSystem : MySessionComponentBase
    {
        public static MyChatHistory GetChatHistory(long localPlayerId)
        {
            if (!MySession.Static.ChatHistory.ContainsKey(localPlayerId))
            {
                MySession.Static.ChatHistory.Add(localPlayerId, new MyChatHistory(localPlayerId));
            }
            return MySession.Static.ChatHistory[localPlayerId];
        }

        public static MyPlayerChatHistory GetPlayerChatHistory(long localPlayerId, long otherPlayerId)
        {
            MyChatHistory localChatHistory;
            if (MySession.Static.ChatHistory.TryGetValue(localPlayerId, out localChatHistory))
            {
                MyPlayerChatHistory playerChatHistory;
                if (localChatHistory.PlayerChatHistory.TryGetValue(otherPlayerId, out playerChatHistory))
                {
                    return playerChatHistory;
                }
            }

            return null;
        }

        public static MyFactionChatHistory GetFactionChatHistory(long localPlayerId, long factionId)
        {
            var localFaction = MySession.Static.Factions.TryGetPlayerFaction(MySession.Static.LocalPlayerId);
            if (localFaction == null)
            {
                return null;
            }

            return FindFactionChatHistory(localFaction.FactionId, factionId);
        }

        public static MyFactionChatHistory FindFactionChatHistory(long factionId1, long factionId2)
        {
            foreach (var factionChat in MySession.Static.FactionChatHistory)
            {
                if (factionChat.FactionId1 == factionId1 && factionChat.FactionId2 == factionId2 ||
                    factionChat.FactionId2 == factionId1 && factionChat.FactionId1 == factionId2)
                {
                    return factionChat;
                }
            }

            return null;
        }

        public static void AddPlayerChatItem(long localPlayerId, long remotePlayerId, MyPlayerChatItem chatItem)
        {
            GetChatHistory(localPlayerId).AddPlayerChatItem(chatItem, remotePlayerId);
        }

        public static void AddFactionChatItem(long localPlayerId, long factionId1, long factionId2, MyFactionChatItem chatItem)
        {
            var factionChat = FindFactionChatHistory(factionId1, factionId2);
            if (factionChat == null)
            {
                factionChat = new MyFactionChatHistory(factionId1, factionId2);
                MySession.Static.FactionChatHistory.Add(factionChat);
            }
         
            if (factionChat.Chat.Count == MyChatConstants.MAX_FACTION_CHAT_HISTORY_COUNT)
            {
                factionChat.Chat.Dequeue();
            }
            factionChat.Chat.Enqueue(chatItem);
        }

        public static void AddGlobalChatItem(long localPlayerId, MyGlobalChatItem chatItem)
        {
            GetChatHistory(localPlayerId).AddGlobalChatItem(chatItem);
        }

        public static void SetPlayerChatItemSent(long localPlayerId, long remotePlayerId, string text, TimeSpan timestamp, bool sent)
        {
            MyPlayerChatHistory playerChatHistory; 
            if (GetChatHistory(localPlayerId).PlayerChatHistory.TryGetValue(remotePlayerId, out playerChatHistory))
            {
                foreach (var chatItem in playerChatHistory.Chat)
                {
                    if (chatItem.Text == text && chatItem.Timestamp == timestamp)
                    {
                        chatItem.Sent = sent;
                        return;
                    }   
                }
            }
            else
            {
                System.Diagnostics.Debug.Fail("Could not find chat history for player " + remotePlayerId + " in player " + localPlayerId + " history!");
            }

            System.Diagnostics.Debug.Fail("Could not find chat message!");
        }

        public event Action<long> PlayerMessageReceived;
        public event Action<long> FactionMessageReceived;

        public event Action PlayerHistoryDeleted;
        public event Action FactionHistoryDeleted;

        public event Action GlobalMessageReceived;

        private MyHudNotification m_newPlayerMessageNotification;
        private MyHudNotification m_newFactionMessageNotification;
        private MyHudNotification m_newGlobalMessageNotification;

        private bool m_initFactionCallback = false;

        public MyChatSystem()
        {
            m_newPlayerMessageNotification = new MyHudNotification(MyCommonTexts.NotificationNewPlayerChatMessage, 2000, level: MyNotificationLevel.Normal);
            m_newFactionMessageNotification = new MyHudNotification(MyCommonTexts.NotificationNewFactionChatMessage, 2000, level: MyNotificationLevel.Normal);

            m_newGlobalMessageNotification = new MyHudNotification(MyCommonTexts.NotificationNewGlobalChatMessage, 2000, level: MyNotificationLevel.Normal);
        }

        void Factions_FactionStateChanged(MyFactionCollection.MyFactionStateChange change, long fromFactionId, long toFactionId, long playerId, long sender)
        {
            if (change == MyFactionCollection.MyFactionStateChange.RemoveFaction)
            {
                DeleteEmptyFactionChat(fromFactionId);
            }
            if (change == MyFactionCollection.MyFactionStateChange.FactionMemberLeave || change == MyFactionCollection.MyFactionStateChange.FactionMemberKick)
            {
                DeletePendingFactionChat(fromFactionId, playerId);
            }
        }


        public void OnNewPlayerMessage(long playerId, long senderId)
        {
            var handler = PlayerMessageReceived;
            if (handler != null)
            {
                handler(playerId);
            }
            else if (senderId != MySession.Static.LocalPlayerId)
            {
                var identity = MySession.Static.Players.TryGetIdentity(senderId);
                if (identity != null)
                {
                    MyPlayerChatHistory chatHistory = MyChatSystem.GetPlayerChatHistory(MySession.Static.LocalPlayerId, senderId);
                    if (chatHistory != null)
                    {
                        chatHistory.UnreadMessageCount++;
                    }

                    m_newPlayerMessageNotification.SetTextFormatArguments(identity.DisplayName);
                    ShowNewMessageHudNotification(m_newPlayerMessageNotification);
                }
            }
        }

        public void OnNewFactionMessage(long factionId1, long factionId2, long senderId, bool showNotification)
        {
            var localFactionId = MySession.Static.Factions.TryGetPlayerFaction(MySession.Static.LocalPlayerId);
            if (localFactionId == null)
            {
                Debug.Fail("OnNewFactionMessage should not be triggered if local player is not a member of a faction, or if this is DedicatedServer");
                return;
            }

            long otherFactionId = factionId1 == localFactionId.FactionId ? factionId2 : factionId1;

            var handler = FactionMessageReceived;
            if (handler != null)
            {
                handler(otherFactionId);
            }
            else if (senderId != MySession.Static.LocalPlayerId && showNotification)
            {
                var faction = MySession.Static.Factions.TryGetFactionById(otherFactionId);
                if (faction != null)
                {
                    var chatHistory = MyChatSystem.GetFactionChatHistory(MySession.Static.LocalPlayerId, faction.FactionId);
                    if (chatHistory != null)
                    {
                        chatHistory.UnreadMessageCount++;
                    }

                    m_newFactionMessageNotification.SetTextFormatArguments(faction.Name);
                    ShowNewMessageHudNotification(m_newFactionMessageNotification);
                }
            }
        }

        public void OnNewGlobalMessage(long senderId)
        {
            var handler = GlobalMessageReceived;
            if (handler != null)
            {
                handler();
            }
            else if (senderId != MySession.Static.LocalPlayerId)
            {
                MyChatHistory chatHistory;
                if (MySession.Static.ChatHistory.TryGetValue(MySession.Static.LocalPlayerId, out chatHistory))
                {
                    chatHistory.GlobalChatHistory.UnreadMessageCount++;
                }

                ShowNewMessageHudNotification(m_newGlobalMessageNotification);
            }
        }

        private void ShowNewMessageHudNotification(MyHudNotification notification)
        {
            MyHud.Notifications.Add(notification);
        }

        private int m_frameCount = 0;
        public override void UpdateAfterSimulation()
        {
            if (Sync.IsServer)
            {
                if (m_frameCount > 100)
                {
                    if (!m_initFactionCallback && MySession.Static.Factions != null)
                    {
                        MySession.Static.Factions.FactionStateChanged += Factions_FactionStateChanged;
                        m_initFactionCallback = true;
                    }

                    m_frameCount = 0;

                    foreach (var faction in MySession.Static.Factions)
                    {
                        if (faction.Value.Members.Count() == 0)
                        {
                            DeleteEmptyFactionChat(faction.Key);
                        }
                    }

                    RetryFactionMessages();
                }
                m_frameCount++;
            }
        }

        private void DeleteEmptyFactionChat(long factionId)
        {
            int index = 0;
            bool changed = false;
            while (index < MySession.Static.FactionChatHistory.Count)
            {
                if (MySession.Static.FactionChatHistory[index].FactionId1 == factionId || MySession.Static.FactionChatHistory[index].FactionId2 == factionId)
                {
                    MySession.Static.FactionChatHistory.RemoveAt(index);
                    changed = true;
                }
                else
                {
                    index++;
                }
            }

            if (changed)
            {
                var handler = FactionHistoryDeleted;
                if (handler != null)
                {
                    handler();
                }
            }
        }

        private void DeletePendingFactionChat(long factionId, long playerId)
        {
            foreach (var factionChatHistory in MySession.Static.FactionChatHistory)
            {
                if (factionChatHistory.FactionId1 == factionId || factionChatHistory.FactionId2 == factionId)
                {
                    foreach (var chatItem in factionChatHistory.Chat)
                    {
                        chatItem.PlayersToSendTo.Remove(playerId);
                    }
                }
            }

            var handler = MySession.Static.ChatSystem.FactionHistoryDeleted;
            if (handler != null)
            {
                handler();
            }
        }

        private static void RetryFactionMessages()
        {
            foreach (var chatFactionHistory in MySession.Static.FactionChatHistory)
            {
                foreach (var chatItem in chatFactionHistory.Chat)
                {
                    //TODO(AF) Cache this!
                    bool isPending = false;
                    foreach (var playerToSendTo in chatItem.PlayersToSendTo)
                    {
                        if (!playerToSendTo.Value)
                        {
                            isPending = true;
                        }
                    }

                    if (isPending)
                    {
                        foreach (var playerToSendTo in chatItem.PlayersToSendTo)
                        {
                            if (playerToSendTo.Value)
                            {
                                if (MyCharacter.RetryFactionMessage(chatFactionHistory.FactionId1, chatFactionHistory.FactionId2, chatItem, MySession.Static.Players.TryGetIdentity(playerToSendTo.Key)))
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        public void ClearChatHistoryForPlayer(MyIdentity identity)
        {
            MySession.Static.ChatHistory.Remove(identity.IdentityId);

            foreach (var chatHistory in MySession.Static.ChatHistory.Values)
            {
                chatHistory.PlayerChatHistory.Remove(identity.IdentityId);
            }

            foreach (var factionChatHistory in MySession.Static.FactionChatHistory)
            {
                foreach (var chatItem in factionChatHistory.Chat)
                {
                    chatItem.PlayersToSendTo.Remove(identity.IdentityId);
                }
            }

            var handler = MySession.Static.ChatSystem.PlayerHistoryDeleted;
            if (handler != null)
            {
                handler();
            }
        }
    }
}
