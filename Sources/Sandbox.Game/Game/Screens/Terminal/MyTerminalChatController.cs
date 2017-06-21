
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Localization;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Engine.Multiplayer;
using VRage;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Gui
{
    class MyTerminalChatController
    {
        private MyGuiControlListbox m_playerList;
        private MyGuiControlListbox m_factionList;
        private MyGuiControlListbox.Item m_broadcastItem;
        private MyGuiControlListbox.Item m_globalItem;

        private MyGuiControlMultilineText m_chatHistory;
        private MyGuiControlTextbox m_chatbox;

        private MyGuiControlButton m_sendButton;

        private readonly StringBuilder m_emptyText = new StringBuilder();
        private StringBuilder m_chatboxText = new StringBuilder();
        private StringBuilder m_tempStringBuilder = new StringBuilder();

        private bool m_closed = true;
        private bool m_pendingUpdatePlayerList = false;
        private bool m_waitedOneFrameBeforeUpdating = false;

        int m_frameCount = 0;

        public void Init(IMyGuiControlsParent controlsParent)
        {
            m_playerList = (MyGuiControlListbox)controlsParent.Controls.GetControlByName("PlayerListbox");
            m_factionList = (MyGuiControlListbox)controlsParent.Controls.GetControlByName("FactionListbox");

            m_chatHistory = (MyGuiControlMultilineText)controlsParent.Controls.GetControlByName("ChatHistory");
            m_chatbox = (MyGuiControlTextbox)controlsParent.Controls.GetControlByName("Chatbox");

            m_playerList.ItemsSelected += m_playerList_ItemsSelected;
            m_playerList.MultiSelect = false;

            m_factionList.ItemsSelected += m_factionList_ItemsSelected;
            m_factionList.MultiSelect = false;

            m_sendButton = (MyGuiControlButton)controlsParent.Controls.GetControlByName("SendButton");
            m_sendButton.ButtonClicked += m_sendButton_ButtonClicked;

            m_chatbox.TextChanged += m_chatbox_TextChanged;
            m_chatbox.EnterPressed += m_chatbox_EnterPressed;

            if (MySession.Static.LocalCharacter != null)
            {
                MySession.Static.ChatSystem.PlayerMessageReceived += MyChatSystem_PlayerMessageReceived;
                MySession.Static.ChatSystem.FactionMessageReceived += MyChatSystem_FactionMessageReceived;
                MySession.Static.ChatSystem.GlobalMessageReceived += MyChatSystem_GlobalMessageReceived;

                MySession.Static.ChatSystem.FactionHistoryDeleted += ChatSystem_FactionHistoryDeleted;
                MySession.Static.ChatSystem.PlayerHistoryDeleted += ChatSystem_PlayerHistoryDeleted;
            }

            MySession.Static.Players.PlayersChanged += Players_PlayersChanged;
            
            RefreshLists();

            m_chatbox.SetText(m_emptyText);
            m_sendButton.Enabled = false;

            if (MyMultiplayer.Static != null)
            {
                MyMultiplayer.Static.ChatMessageReceived += Multiplayer_ChatMessageReceived;
            }


            m_closed = false;
        }
        
        #region UI Events
        void m_chatbox_TextChanged(MyGuiControlTextbox obj)
        {
            m_chatboxText.Clear();
            obj.GetText(m_chatboxText);
            if (m_chatboxText.Length == 0)
            {
                m_sendButton.Enabled = false;
            }
            else
            {
                if (MySession.Static.LocalCharacter != null)
                {
                    m_sendButton.Enabled = true;
                }

                if (m_chatboxText.Length > MyChatConstants.MAX_CHAT_STRING_LENGTH)
                {
                    m_chatboxText.Length = MyChatConstants.MAX_CHAT_STRING_LENGTH;
                    m_chatbox.SetText(m_chatboxText);
                }
            }
        }

        void m_chatbox_EnterPressed(MyGuiControlTextbox obj)
        {
            if (m_chatboxText.Length > 0)
            {
                SendMessage();
            }
        }

        void m_sendButton_ButtonClicked(MyGuiControlButton obj)
        {
            SendMessage();
        }

        void m_playerList_ItemsSelected(MyGuiControlListbox obj)
        {
            if (m_playerList.SelectedItems.Count > 0)
            {
                var selectedItem = m_playerList.SelectedItems[0];

                if ( selectedItem == m_globalItem )
                {
                    RefreshGlobalChatHistory();
                }
                else if(selectedItem==m_broadcastItem)
                {
                    RefreshBroadcastChatHistory();

                    MyChatHistory chatHistory;
                    if (MySession.Static.ChatHistory.TryGetValue(MySession.Static.LocalPlayerId, out chatHistory) && chatHistory.GlobalChatHistory.UnreadMessageCount > 0)
                    {
                        chatHistory.GlobalChatHistory.UnreadMessageCount = 0;
                        UpdatePlayerList();
                    }
                }
                else
                {
                    var playerIdentity = (MyIdentity)selectedItem.UserData;
                    RefreshPlayerChatHistory(playerIdentity);

                    var playerChatHistory = MyChatSystem.GetPlayerChatHistory(MySession.Static.LocalPlayerId, playerIdentity.IdentityId);
                    if (playerChatHistory != null && playerChatHistory.UnreadMessageCount > 0)
                    {
                        playerChatHistory.UnreadMessageCount = 0;
                        UpdatePlayerList();
                    }
                }
                m_chatbox.SetText(m_emptyText);
            }
        }


        void m_factionList_ItemsSelected(MyGuiControlListbox obj)
        {
            if (m_factionList.SelectedItems.Count > 0)
            {
                var selectedItem = m_factionList.SelectedItems[0];
                var faction = (MyFaction)selectedItem.UserData;
                RefreshFactionChatHistory(faction);

                var factions = MySession.Static.Factions;
                var localFaction = factions.TryGetPlayerFaction(MySession.Static.LocalPlayerId);
                if (localFaction != null)
                {
                    MyFactionChatHistory factionChat = MyChatSystem.FindFactionChatHistory(faction.FactionId, localFaction.FactionId);
                    if (factionChat != null)
                    {
                        factionChat.UnreadMessageCount = 0;
                        UpdateFactionList(true);
                    }
                }

                m_chatbox.SetText(m_emptyText);
            }
        }
        #endregion

        #region Network Events
        public void IncrementPlayerUnreadMessageCount(long otherPlayerId, bool refresh)
        {
            MyPlayerChatHistory chatHistory = MyChatSystem.GetPlayerChatHistory(MySession.Static.LocalPlayerId, otherPlayerId);
            if (chatHistory != null)
            {
                chatHistory.UnreadMessageCount++;
                if (refresh)
                {
                    UpdatePlayerList();
                }
            }
        }

        public void IncrementFactionUnreadMessageCount(long factionId, bool refresh)
        {
            var chatHistory = MyChatSystem.GetFactionChatHistory(MySession.Static.LocalPlayerId, factionId);
            if (chatHistory != null)
            {
                chatHistory.UnreadMessageCount++;
                if (refresh)
                {
                    UpdateFactionList(true);
                }
            }
        }

        public void IncrementGlobalUnreadMessageCount(bool refresh)
        {
            MyChatHistory chatHistory;
            if (MySession.Static.ChatHistory.TryGetValue(MySession.Static.LocalPlayerId, out chatHistory))
            {
                chatHistory.GlobalChatHistory.UnreadMessageCount++;
                if (refresh)
                {
                    UpdatePlayerList();
                }
            }
        }

        void MyChatSystem_PlayerMessageReceived(long playerId)
        {
            if (m_playerList.SelectedItems.Count > 0)
            {
                var selectedItem = m_playerList.SelectedItems[0];
                var playerIdentity = (MyIdentity)selectedItem.UserData;

                if (selectedItem != m_broadcastItem)
                {
                    if (playerIdentity.IdentityId == playerId)
                    {
                        RefreshPlayerChatHistory(playerIdentity);
                    }
                    else
                    {
                        IncrementPlayerUnreadMessageCount(playerId, true);
                    }
                }
                else
                {
                    IncrementPlayerUnreadMessageCount(playerId, true);
                }
            }
            else
            {
                IncrementPlayerUnreadMessageCount(playerId, true);
            }
        }

        void MyChatSystem_FactionMessageReceived(long factionId)
        {
            if (m_factionList.SelectedItems.Count > 0)
            {
                var selectedItem = m_factionList.SelectedItems[0];
                var faction = (MyFaction)selectedItem.UserData;
                if (faction.FactionId == factionId)
                {
                    RefreshFactionChatHistory(faction);
                }
                else
                {
                    IncrementFactionUnreadMessageCount(factionId, true);
                }
            }
            else
            {
                IncrementFactionUnreadMessageCount(factionId, true);
            }
        }

        void MyChatSystem_GlobalMessageReceived()
        {
            if (m_playerList.SelectedItems.Count > 0 && m_playerList.SelectedItems[0] == m_broadcastItem)
            {
                RefreshBroadcastChatHistory();
            }
            else
            {
                IncrementGlobalUnreadMessageCount(true);
            }
        }

        void Multiplayer_ChatMessageReceived(ulong steamUserId, string messageText, SteamSDK.ChatEntryTypeEnum arg3)
        {
            if ( m_playerList.SelectedItems.Count > 0 && m_playerList.SelectedItems[0] == m_globalItem )
                RefreshGlobalChatHistory();

        }

        void ChatSystem_PlayerHistoryDeleted()
        {
            if (!m_closed)
            {
                UpdatePlayerList();
                if (m_factionList.SelectedItems.Count > 0)
                {
                    RefreshFactionChatHistory((MyFaction)m_factionList.SelectedItems[0].UserData);
                }
            }
        }

        void ChatSystem_FactionHistoryDeleted()
        {
            if (!m_closed)
            {
                UpdateFactionList(true);
                if (m_factionList.SelectedItems.Count > 0)
                {
                    RefreshFactionChatHistory((MyFaction)m_factionList.SelectedItems[0].UserData);
                }
            }
        }
        #endregion

        void Players_PlayersChanged(bool added, MyPlayer.PlayerId playerId)
        {
            if (!m_closed)
            {
                UpdatePlayerList();
            }
        }

        private void SendMessage()
        {
            //Cannot send any message if local character is missing
            if (MySession.Static.LocalCharacter == null)
            {
                return;
            }

            m_chatboxText.Clear();
            m_chatbox.GetText(m_chatboxText);

            MyDebug.AssertDebug(m_chatboxText.Length > 0, "Length of chat text should be positive");
            MyDebug.AssertDebug(m_chatboxText.Length <= MyChatConstants.MAX_CHAT_STRING_LENGTH, "Length of chat text should not exceed maximum allowed");

            var history = MyChatSystem.GetChatHistory(MySession.Static.LocalPlayerId);
            if (m_playerList.SelectedItems.Count > 0)
            {
                var selectedItem = m_playerList.SelectedItems[0];

                if(selectedItem==m_globalItem)
                {
                    //messages entered in the global chat history should be treated as normal ingame chat
                    if ( MyMultiplayer.Static != null )
                        MyMultiplayer.Static.SendChatMessage( m_chatboxText.ToString() );
                    else
                        MyHud.Chat.ShowMessage( MySession.Static.LocalHumanPlayer == null ? "Player" : MySession.Static.LocalHumanPlayer.DisplayName, m_chatboxText.ToString() );

                    //add the message to history
                    //MySession.Static.GlobalChatHistory.GlobalChatHistory.Chat.Enqueue(new MyGlobalChatItem
                    //{
                    //    IdentityId = MySession.Static.LocalPlayerId,
                    //    Text = m_chatboxText.ToString()
                    //});

                    RefreshGlobalChatHistory();
                }
                else if (selectedItem == m_broadcastItem)
                {
                    MySession.Static.LocalCharacter.SendNewGlobalMessage(MySession.Static.LocalHumanPlayer.Id, m_chatboxText.ToString());
                }
                else
                {
                    var playerIdentity = (MyIdentity)selectedItem.UserData;

                    MySession.Static.ChatHistory[MySession.Static.LocalPlayerId].AddPlayerChatItem(new MyPlayerChatItem(m_chatboxText.ToString(), MySession.Static.LocalPlayerId, MySession.Static.ElapsedGameTime, false), playerIdentity.IdentityId);
                    RefreshPlayerChatHistory(playerIdentity);
                }
            }
            else if (m_factionList.SelectedItems.Count > 0)
            {
                var toSendTo = new Dictionary<long, bool>();
                var selectedItem = m_factionList.SelectedItems[0];
                var targetFaction = (MyFaction)selectedItem.UserData;

                foreach (var member in targetFaction.Members)
                {
                    toSendTo.Add(member.Value.PlayerId, false);
                }

                if (!targetFaction.IsMember(MySession.Static.LocalPlayerId))
                {
                    var localFaction = MySession.Static.Factions.TryGetPlayerFaction(MySession.Static.LocalPlayerId);
                    if (localFaction != null)
                    {
                        foreach (var member in localFaction.Members)
                        {
                            toSendTo.Add(member.Value.PlayerId, false);
                        }
                    }
                }
                
                var factionChatItem = new MyFactionChatItem(m_chatboxText.ToString(), MySession.Static.LocalPlayerId, MySession.Static.ElapsedGameTime, toSendTo);
                
                //This has to exist!
                var currentFaction = MySession.Static.Factions.TryGetPlayerFaction(MySession.Static.LocalPlayerId);

                MySession.Static.LocalCharacter.SendNewFactionMessage(targetFaction.FactionId, currentFaction.FactionId, factionChatItem);

                RefreshFactionChatHistory(targetFaction);
            }

            m_chatbox.SetText(m_emptyText);
        }

        private void RefreshPlayerChatHistory(MyIdentity playerIdentity)
        {
            m_chatHistory.Clear();
            var history = MyChatSystem.GetChatHistory(MySession.Static.LocalPlayerId);
            
            var playerId = playerIdentity.IdentityId;
            MyPlayerChatHistory playerChat;
            if (history.PlayerChatHistory.TryGetValue(playerId, out playerChat))
            {
                var chat = playerChat.Chat;
                foreach (var text in chat)
                {
                    var identity = MySession.Static.Players.TryGetIdentity(text.IdentityId);

                    if (identity == null) continue;
                    bool isPlayer = identity.IdentityId == MySession.Static.LocalPlayerId;

                    m_chatHistory.AppendText(identity.DisplayName, isPlayer ? MyFontEnum.DarkBlue : MyFontEnum.Blue, m_chatHistory.TextScale, Vector4.One);
                    if (!text.Sent)
                    {
                        m_tempStringBuilder.Clear();
                        m_tempStringBuilder.Append(" (");
                        m_tempStringBuilder.Append(MyTexts.GetString(MySpaceTexts.TerminalTab_Chat_Pending));
                        m_tempStringBuilder.Append(")");
                        m_chatHistory.AppendText(m_tempStringBuilder, MyFontEnum.Red, m_chatHistory.TextScale, Vector4.One);
                    }
                    m_chatHistory.AppendText(": ", isPlayer ? MyFontEnum.DarkBlue : MyFontEnum.Blue, m_chatHistory.TextScale, Vector4.One);
                    m_chatHistory.AppendText(text.Text, MyFontEnum.White, m_chatHistory.TextScale, Vector4.One);
                    m_chatHistory.AppendLine();
                }
            }

            m_factionList.SelectedItems.Clear();
            m_chatHistory.ScrollbarOffset = 1.0f;
        }

        private void RefreshFactionChatHistory(MyFaction faction)
        {
            m_chatHistory.Clear();
            
            var localFaction = MySession.Static.Factions.TryGetPlayerFaction(MySession.Static.LocalPlayerId);
            if (localFaction == null)
            {
                System.Diagnostics.Debug.Fail("Chat shouldn't be refreshed if local player is not a member of a faction!");
                return;
            }
            MyFactionChatHistory factionChat = MyChatSystem.FindFactionChatHistory(faction.FactionId, localFaction.FactionId);
            if (factionChat != null)
            {
                var chat = factionChat.Chat;
                foreach (var item in chat)
                {
                    bool alreadySentToMe;
                    if (item.IdentityId == MySession.Static.LocalPlayerId || (item.PlayersToSendTo.TryGetValue(MySession.Static.LocalPlayerId, out alreadySentToMe) && alreadySentToMe))
                    {
                        int alreadySentToCount = 0;
                        foreach (var keyValue in item.PlayersToSendTo)
                        {
                            if (keyValue.Value)
                            {
                                alreadySentToCount++;
                            }
                        }
                        var identity = MySession.Static.Players.TryGetIdentity(item.IdentityId);

                        if (identity == null) continue;
                        bool isPlayer = identity.IdentityId == MySession.Static.LocalPlayerId;

                        m_chatHistory.AppendText(identity.DisplayName, isPlayer ? MyFontEnum.DarkBlue : MyFontEnum.Blue, m_chatHistory.TextScale, Vector4.One);
                        if (item.PlayersToSendTo != null && item.PlayersToSendTo.Count > 0 && alreadySentToCount < item.PlayersToSendTo.Count)
                        {
                            var pendingText = new StringBuilder();
                            pendingText.Append(" (");
                            pendingText.Append(alreadySentToCount.ToString());
                            pendingText.Append("/");
                            pendingText.Append(item.PlayersToSendTo.Count.ToString());
                            pendingText.Append(") ");
                            m_chatHistory.AppendText(pendingText, MyFontEnum.Red, m_chatHistory.TextScale, Vector4.One);
                        }
                        m_chatHistory.AppendText(": ", isPlayer ? MyFontEnum.DarkBlue : MyFontEnum.Blue, m_chatHistory.TextScale, Vector4.One);
                        m_chatHistory.AppendText(item.Text, MyFontEnum.White, m_chatHistory.TextScale, Vector4.One);
                        m_chatHistory.AppendLine();
                    }
                }
            }

            m_playerList.SelectedItems.Clear();
            m_chatHistory.ScrollbarOffset = 1.0f;
        }

        private void RefreshGlobalChatHistory()
        {
            m_chatHistory.Clear();

            var chat = MySession.Static.GlobalChatHistory.GlobalChatHistory.Chat;
            foreach (var text in chat)
            {
                if (text.IdentityId == 0)
                {
                    if (text.Author.Length > 0)
                        m_chatHistory.AppendText(text.Author + ": ", text.AuthorFont, m_chatHistory.TextScale, Vector4.One);
                }
                else
                {
                    var identity = MySession.Static.Players.TryGetIdentity(text.IdentityId);

                    if (identity == null) continue;
                    bool isPlayer = identity.IdentityId == MySession.Static.LocalPlayerId;

                    m_chatHistory.AppendText(identity.DisplayName + ": ", isPlayer ? MyFontEnum.DarkBlue : MyFontEnum.Blue, m_chatHistory.TextScale, Vector4.One);
                }

                m_chatHistory.AppendText(text.Text, MyFontEnum.White, m_chatHistory.TextScale, Vector4.One);
                m_chatHistory.AppendLine();
            }

            m_factionList.SelectedItems.Clear();
            m_chatHistory.ScrollbarOffset = 1.0f;
        }

        private void RefreshBroadcastChatHistory()
        {
            m_chatHistory.Clear();
            var history = MyChatSystem.GetChatHistory(MySession.Static.LocalPlayerId);

            var chat = history.GlobalChatHistory.Chat;
            foreach (var text in chat)
            {
                var identity = MySession.Static.Players.TryGetIdentity(text.IdentityId);

                if (identity == null) continue;
                bool isPlayer = identity.IdentityId == MySession.Static.LocalPlayerId;

                m_chatHistory.AppendText(identity.DisplayName, isPlayer ? MyFontEnum.DarkBlue : MyFontEnum.Blue, m_chatHistory.TextScale, Vector4.One);
                
                m_chatHistory.AppendText(": ", isPlayer ? MyFontEnum.DarkBlue : MyFontEnum.Blue, m_chatHistory.TextScale, Vector4.One);
                m_chatHistory.AppendText(text.Text, MyFontEnum.White, m_chatHistory.TextScale, Vector4.One);
                m_chatHistory.AppendLine();
            }

            m_factionList.SelectedItems.Clear();
            m_chatHistory.ScrollbarOffset = 1.0f;
        }

        private void ClearChat()
        {
            m_chatHistory.Clear();

            m_chatbox.SetText(m_emptyText);
        }

        private void RefreshLists()
        {
            RefreshPlayerList();
            RefreshFactionList();
        }

        List<MyIdentity> m_tempOnlinePlayers = new List<MyIdentity>();
        List<MyIdentity> m_tempOfflinePlayers = new List<MyIdentity>();
        private void RefreshPlayerList()
        {
            //Add the global chat log first
            m_globalItem = new MyGuiControlListbox.Item( MyTexts.Get( MySpaceTexts.TerminalTab_Chat_ChatHistory ) );
            m_playerList.Add( m_globalItem );

            //Comms broadcast history
            m_tempStringBuilder.Clear();
            m_tempStringBuilder.Append(MyTexts.Get(MySpaceTexts.TerminalTab_Chat_GlobalChat));
            
            MyChatHistory chatHistory;
            if (MySession.Static.ChatHistory.TryGetValue(MySession.Static.LocalPlayerId, out chatHistory) && chatHistory.GlobalChatHistory.UnreadMessageCount > 0)
            {
                m_tempStringBuilder.Append(" (");
                m_tempStringBuilder.Append(chatHistory.GlobalChatHistory.UnreadMessageCount);
                m_tempStringBuilder.Append(")");
            }
            
            m_broadcastItem = new MyGuiControlListbox.Item(m_tempStringBuilder);
            m_playerList.Add(m_broadcastItem);

            //var allPlayers = MySession.Static.Players.GetAllIdentities();
            var allPlayers = MySession.Static.Players.GetAllPlayers();

            m_tempOnlinePlayers.Clear();
            m_tempOfflinePlayers.Clear();

            foreach (var player in allPlayers)
            {
                var playerIdentity = MySession.Static.Players.TryGetIdentity(MySession.Static.Players.TryGetIdentityId(player.SteamId, player.SerialId));

                if (playerIdentity != null && playerIdentity.IdentityId != MySession.Static.LocalPlayerId && player.SerialId == 0)
                {
                    if (playerIdentity.Character == null)
                    {
                        m_tempOfflinePlayers.Add(playerIdentity);
                    }
                    else
                    {
                        m_tempOnlinePlayers.Add(playerIdentity);
                    }
                }
            }

            foreach (var onlinePlayer in m_tempOnlinePlayers)
            {
                m_tempStringBuilder.Clear();
                m_tempStringBuilder.Append(onlinePlayer.DisplayName);

                var playerChatHistory = MyChatSystem.GetPlayerChatHistory(MySession.Static.LocalPlayerId, onlinePlayer.IdentityId);
                if (playerChatHistory != null && playerChatHistory.UnreadMessageCount > 0)
                {
                    m_tempStringBuilder.Append(" (");
                    m_tempStringBuilder.Append(playerChatHistory.UnreadMessageCount);
                    m_tempStringBuilder.Append(")");
                }

                var item = new MyGuiControlListbox.Item(text: m_tempStringBuilder, userData: onlinePlayer);
                m_playerList.Add(item);
            }

            foreach (var offlinePlayer in m_tempOfflinePlayers)
            {
                m_tempStringBuilder.Clear();
                m_tempStringBuilder.Append(offlinePlayer.DisplayName);
                m_tempStringBuilder.Append(" (");
                m_tempStringBuilder.Append(MyTexts.GetString(MySpaceTexts.TerminalTab_Chat_Offline));
                m_tempStringBuilder.Append(")");

                var playerChatHistory = MyChatSystem.GetPlayerChatHistory(MySession.Static.LocalPlayerId, offlinePlayer.IdentityId);
                if (playerChatHistory != null && playerChatHistory.UnreadMessageCount > 0)
                {
                    m_tempStringBuilder.Append(" (");
                    m_tempStringBuilder.Append(playerChatHistory.UnreadMessageCount);
                    m_tempStringBuilder.Append(")");
                }

                var item = new MyGuiControlListbox.Item(text: m_tempStringBuilder, userData: offlinePlayer, fontOverride: MyFontEnum.DarkBlue);
                m_playerList.Add(item);
            }
        }

        private void RefreshFactionList()
        {
            var localFaction = MySession.Static.Factions.TryGetPlayerFaction(MySession.Static.LocalPlayerId);
            if (localFaction != null)
            {
                //Add local player faction first
                m_tempStringBuilder.Clear();
                m_tempStringBuilder.Append(localFaction.Name);

                var chatHistory = MyChatSystem.GetFactionChatHistory(MySession.Static.LocalPlayerId, localFaction.FactionId);
                if (chatHistory != null && chatHistory.UnreadMessageCount > 0)
                {
                    m_tempStringBuilder.Append(" (");
                    m_tempStringBuilder.Append(chatHistory.UnreadMessageCount);
                    m_tempStringBuilder.Append(")");
                }

                var item = new MyGuiControlListbox.Item(text: m_tempStringBuilder, userData: localFaction);
                m_factionList.Add(item);

                m_factionList.SetToolTip(string.Empty);
                foreach (var faction in MySession.Static.Factions)
                {
                    //Don't add local player faction twice
                    if (faction.Value != localFaction && faction.Value.AcceptHumans)
                    {
                        m_tempStringBuilder.Clear();
                        m_tempStringBuilder.Append(faction.Value.Name);

                        chatHistory = MyChatSystem.GetFactionChatHistory(MySession.Static.LocalPlayerId, faction.Value.FactionId);
                        if (chatHistory != null && chatHistory.UnreadMessageCount > 0)
                        {
                            m_tempStringBuilder.Append(" (");
                            m_tempStringBuilder.Append(chatHistory.UnreadMessageCount);
                            m_tempStringBuilder.Append(")");
                        }

                        item = new MyGuiControlListbox.Item(text: m_tempStringBuilder, userData: faction.Value);
                        m_factionList.Add(item);
                    }
                }
            }
            else
            {
                m_factionList.SelectedItems.Clear();
                m_factionList.Items.Clear();

                m_factionList.SetToolTip(MyTexts.GetString(MySpaceTexts.TerminalTab_Chat_NoFaction));
            }
        }

        public void Update()
        {
            if (!m_closed)
            {
                UpdateLists();
            }
        }

        private void UpdatePlayerList()
        {
            long selectedPlayerId = -1;
            bool broadcastChatSelected = false;
            bool globalChatSelected = false;
            if (m_playerList.SelectedItems.Count > 0)
            {
                if ( m_playerList.SelectedItems[0] == m_globalItem )
                {
                    globalChatSelected = true;
                }
                else if (m_playerList.SelectedItems[0] == m_broadcastItem)
                {
                    broadcastChatSelected = true;
                }
                else
                {
                    selectedPlayerId = ((MyIdentity)m_playerList.SelectedItems[0].UserData).IdentityId;
                }
            }

            int scrollIndex = m_playerList.FirstVisibleRow;

            m_playerList.SelectedItems.Clear();
            m_playerList.Items.Clear();
            RefreshPlayerList();

            if (selectedPlayerId != -1)
            {
                bool found = false;
                foreach (var item in m_playerList.Items)
                {
                    if (item.UserData == null) continue;

                    long currentItemId = ((MyIdentity)item.UserData).IdentityId;
                    if (currentItemId == selectedPlayerId)
                    {
                        m_playerList.SelectedItems.Clear();
                        m_playerList.SelectedItems.Add(item);

                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    ClearChat();
                }
            }
            else if ( globalChatSelected )
            {
                m_playerList.SelectedItems.Clear();
                m_playerList.SelectedItems.Add( m_globalItem );
            }
            else if (broadcastChatSelected)
            {
                m_playerList.SelectedItems.Clear();
                m_playerList.SelectedItems.Add(m_broadcastItem);
            }

            if (scrollIndex >= m_playerList.Items.Count)
            {
                scrollIndex = m_playerList.Items.Count - 1;
            }

            m_playerList.FirstVisibleRow = scrollIndex;
        }

        private void UpdateLists()
        {
            UpdateFactionList(false);
            if (m_frameCount > 100)
            {
                m_frameCount = 0;
                UpdatePlayerList();
            }
            m_frameCount++;
        }

        private void UpdateFactionList(bool forceRefresh)
        {
            var factions = MySession.Static.Factions;
            var localFaction = factions.TryGetPlayerFaction(MySession.Static.LocalPlayerId);
            if (localFaction == null)
            {
                if (m_factionList.Items.Count != 0)
                {
                    RefreshFactionList();
                }
                return;
            }

            if (forceRefresh || m_factionList.Items.Count != factions.Count())
            {
                long selectedFactionId = -1;
                if (m_factionList.SelectedItems.Count > 0)
                {
                    selectedFactionId = ((MyFaction)m_factionList.SelectedItems[0].UserData).FactionId;
                }

                int scrollIndex = m_factionList.FirstVisibleRow;

                m_factionList.SelectedItems.Clear();
                m_factionList.Items.Clear();
                RefreshFactionList();

                if (selectedFactionId != -1)
                {
                    bool found = false;
                    foreach (var item in m_factionList.Items)
                    {
                        long currentItemId = ((MyFaction)item.UserData).FactionId;
                        if (currentItemId == selectedFactionId)
                        {
                            m_factionList.SelectedItems.Clear();
                            m_factionList.SelectedItems.Add(item);

                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        ClearChat();
                    }
                }


                if (scrollIndex >= m_factionList.Items.Count)
                {
                    scrollIndex = m_factionList.Items.Count - 1;
                }

                m_factionList.FirstVisibleRow = scrollIndex;
            }
        }

        public void Close()
        {
            m_closed = false;

            m_playerList.ItemsSelected -= m_playerList_ItemsSelected;
            m_factionList.ItemsSelected -= m_factionList_ItemsSelected;
            
            m_sendButton.ButtonClicked -= m_sendButton_ButtonClicked;
            m_chatbox.TextChanged -= m_chatbox_TextChanged;
            m_chatbox.EnterPressed -= m_chatbox_EnterPressed;

            if (MyMultiplayer.Static != null)
                MyMultiplayer.Static.ChatMessageReceived -= Multiplayer_ChatMessageReceived;

            if (MySession.Static.LocalCharacter != null)
            {
                MySession.Static.ChatSystem.PlayerMessageReceived -= MyChatSystem_PlayerMessageReceived;
                MySession.Static.ChatSystem.FactionMessageReceived -= MyChatSystem_FactionMessageReceived;
                MySession.Static.ChatSystem.GlobalMessageReceived -= MyChatSystem_GlobalMessageReceived;

                MySession.Static.ChatSystem.FactionHistoryDeleted -= ChatSystem_FactionHistoryDeleted;
                MySession.Static.ChatSystem.PlayerHistoryDeleted -= ChatSystem_PlayerHistoryDeleted;
            }

            MySession.Static.Players.PlayersChanged -= Players_PlayersChanged;
        }
    }
}
