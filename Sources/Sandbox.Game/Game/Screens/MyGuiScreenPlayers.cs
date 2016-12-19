using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.GUI;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using SteamSDK;
using System;
using System.Diagnostics;
using System.Text;
using VRage;
using VRage.Input;
using VRage.Utils;
using VRageMath;
using System.Collections.Generic;
using Sandbox.Engine.Networking;
using VRage.Network;
using Sandbox.Graphics;
using Sandbox.Engine.Utils;
using VRage.Audio;
using VRage.Game;
using VRage.Game.ModAPI;

namespace Sandbox.Game.Gui
{
    [StaticEventOwner]
    public class MyGuiScreenPlayers : MyGuiScreenBase
    {
        protected static readonly string OWNER_MARKER = "*****";
        protected int PlayerNameColumn = 0;
        protected int PlayerFactionTagColumn = 1;
        protected int PlayerFactionNameColumn = 2;
        protected int PlayerMutedColumn = 3;
        protected int GameAdminColumn = 4;

        protected MyGuiControlTable m_playersTable;
        protected MyGuiControlButton m_inviteButton;
        protected MyGuiControlButton m_promoteButton;
        protected MyGuiControlButton m_demoteButton;
        protected MyGuiControlButton m_kickButton;
        protected MyGuiControlButton m_banButton;
        protected MyGuiControlCombobox m_lobbyTypeCombo;
        protected MyGuiControlSlider m_maxPlayersSlider;
        protected MyGuiControlTextbox m_playerBlockLimitTextbox;
        protected MyGuiControlButton m_confirmLimitChangeButton;
        protected HashSet<ulong> m_mutedPlayers;
        protected ulong m_lastSelected;

        public MyGuiScreenPlayers() :
            base(size: MyGuiConstants.TEXTURE_SCREEN_BACKGROUND.SizeGui * 1.1f + new Vector2(0.1f, 0f),
                 backgroundColor: MyGuiConstants.SCREEN_BACKGROUND_COLOR,
                 backgroundTexture: MyGuiConstants.TEXTURE_SCREEN_BACKGROUND.Texture)
        {
            EnabledBackgroundFade = true;

            MyMultiplayer.Static.ClientJoined += Multiplayer_PlayerJoined;
            MyMultiplayer.Static.ClientLeft += Multiplayer_PlayerLeft;

            MySession.Static.Factions.FactionCreated += OnFactionCreated;
            MySession.Static.Factions.FactionEdited += OnFactionEdited;
            MySession.Static.Factions.FactionStateChanged += OnFactionStateChanged;

            SteamAPI.Instance.Matchmaking.LobbyDataUpdate += Matchmaking_LobbyDataUpdate;

            if (MyPerGameSettings.EnableMutePlayer)
                // shifting of second comumn after mute checkbox
                GameAdminColumn = 4;

            m_mutedPlayers = MySandboxGame.Config.MutedPlayers;

            RecreateControls(true);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenPlayers";
        }

        protected override void OnClosed()
        {
            base.OnClosed();

            if (MyMultiplayer.Static != null)
            {
                MyMultiplayer.Static.ClientJoined -= Multiplayer_PlayerJoined;
                MyMultiplayer.Static.ClientLeft -= Multiplayer_PlayerLeft;
            }

            if (MySession.Static != null)
            {
                MySession.Static.Factions.FactionCreated -= OnFactionCreated;
                MySession.Static.Factions.FactionEdited -= OnFactionEdited;
                MySession.Static.Factions.FactionStateChanged -= OnFactionStateChanged;
            }

            SteamAPI.Instance.Matchmaking.LobbyDataUpdate -= Matchmaking_LobbyDataUpdate;
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            CloseButtonEnabled = true;

            var caption = AddCaption(MyCommonTexts.ScreenCaptionPlayers);
            var captionCenter = MyUtils.GetCoordCenterFromAligned(caption.Position, caption.Size, caption.OriginAlign);
            var captionBottomCenter = captionCenter + new Vector2(0f, 0.5f * caption.Size.Y);

            Vector2 sizeScale = Size.Value / MyGuiConstants.TEXTURE_SCREEN_BACKGROUND.SizeGui;
            Vector2 topLeft = -0.5f * Size.Value + sizeScale * MyGuiConstants.TEXTURE_SCREEN_BACKGROUND.PaddingSizeGui * 1.1f;

            float verticalSpacing = 0.0045f;

            m_lobbyTypeCombo = new MyGuiControlCombobox(
                position: new Vector2(-topLeft.X, captionBottomCenter.Y + verticalSpacing),
                openAreaItemsCount: 3);
            m_lobbyTypeCombo.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP;
            m_lobbyTypeCombo.AddItem((int)LobbyTypeEnum.Private, MyCommonTexts.ScreenPlayersLobby_Private);
            m_lobbyTypeCombo.AddItem((int)LobbyTypeEnum.FriendsOnly, MyCommonTexts.ScreenPlayersLobby_Friends);
            m_lobbyTypeCombo.AddItem((int)LobbyTypeEnum.Public, MyCommonTexts.ScreenPlayersLobby_Public);
            m_lobbyTypeCombo.SelectItemByKey((int)MyMultiplayer.Static.GetLobbyType());

            MyGuiControlBase aboveControl;

            m_inviteButton = new MyGuiControlButton(
                position: new Vector2(-m_lobbyTypeCombo.Position.X, m_lobbyTypeCombo.Position.Y + m_lobbyTypeCombo.Size.Y + verticalSpacing),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                text: MyTexts.Get(MyCommonTexts.ScreenPlayers_Invite));
            aboveControl = m_inviteButton;

            m_promoteButton = new MyGuiControlButton(
                position: aboveControl.Position + new Vector2(0f, aboveControl.Size.Y + verticalSpacing),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                text: MyTexts.Get(MyCommonTexts.ScreenPlayers_Promote));
            aboveControl = m_promoteButton;

            m_demoteButton = new MyGuiControlButton(
                position: aboveControl.Position + new Vector2(0f, aboveControl.Size.Y + verticalSpacing),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                text: MyTexts.Get(MyCommonTexts.ScreenPlayers_Demote));
            aboveControl = m_demoteButton;

            m_kickButton = new MyGuiControlButton(
                position: aboveControl.Position + new Vector2(0f, aboveControl.Size.Y + verticalSpacing),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                text: MyTexts.Get(MyCommonTexts.ScreenPlayers_Kick));
            aboveControl = m_kickButton;

            m_banButton = new MyGuiControlButton(
                position: aboveControl.Position + new Vector2(0f, aboveControl.Size.Y + verticalSpacing),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                text: MyTexts.Get(MyCommonTexts.ScreenPlayers_Ban));
            aboveControl = m_banButton;

            var maxPlayersLabel = new MyGuiControlLabel(
                position: aboveControl.Position + new Vector2(0f, aboveControl.Size.Y + verticalSpacing),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                text: MyTexts.GetString(MyCommonTexts.MaxPlayers));
            aboveControl = maxPlayersLabel;

            m_maxPlayersSlider = new MyGuiControlSlider(
                position: aboveControl.Position + new Vector2(0f, aboveControl.Size.Y),
                width: 0.15f,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                minValue: 2,
                maxValue: MyMultiplayer.Static != null ? MyMultiplayer.Static.MaxPlayers : 16,
                labelText: new StringBuilder("{0}").ToString(),
                labelDecimalPlaces: 0,
                labelSpaceWidth: 0.02f,
                defaultValue: Sync.IsServer ? MySession.Static.MaxPlayers : MyMultiplayer.Static.MemberLimit,
                intValue: true);
            m_maxPlayersSlider.ValueChanged = MaxPlayersSlider_Changed;
            aboveControl = m_maxPlayersSlider;

            var playerBlockLimitLabel = new MyGuiControlLabel(
                position: aboveControl.Position + new Vector2(0f, aboveControl.Size.Y + 0.05f),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                text: MyTexts.GetString(MyCommonTexts.PlayerMaxBlocks));
            aboveControl = playerBlockLimitLabel;

            m_playerBlockLimitTextbox = new MyGuiControlTextbox(
                position: aboveControl.Position + new Vector2(0.09f, aboveControl.Size.Y + verticalSpacing + 0.02f),
                maxLength: 6,
                type: MyGuiControlTextboxType.DigitsOnly);
            aboveControl = m_playerBlockLimitTextbox;

            m_playerBlockLimitTextbox.Size = new Vector2(0.18f, m_playerBlockLimitTextbox.Size.Y);

            m_confirmLimitChangeButton = new MyGuiControlButton(
                position: new Vector2(topLeft.X, aboveControl.Position.Y + aboveControl.Size.Y + verticalSpacing),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                text: MyTexts.Get(MyCommonTexts.ScreenMenuButtonSave));

            m_playersTable = new MyGuiControlTable()
            {
                Position = new Vector2(-m_inviteButton.Position.X, m_inviteButton.Position.Y),
                Size = new Vector2(1200f / MyGuiConstants.GUI_OPTIMAL_SIZE.X, 1f) - m_inviteButton.Size * 1.05f,
                VisibleRowsCount = 21,
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP,
                ColumnsCount = 5,
            };
            float PlayerNameWidth = 0.3f;
            float FactionTagWidth = 0.12f;
            float FactionNameWidth = MyPerGameSettings.EnableMutePlayer ? 0.3f : 0.34f;
            float MutedWidth = MyPerGameSettings.EnableMutePlayer ? 0.13f : 0;
            m_playersTable.SetCustomColumnWidths(new float[] { PlayerNameWidth, FactionTagWidth, FactionNameWidth, MutedWidth, 1 - PlayerNameWidth - FactionTagWidth - FactionNameWidth - MutedWidth });
            m_playersTable.SetColumnName(PlayerNameColumn, MyTexts.Get(MyCommonTexts.ScreenPlayers_PlayerName));
            m_playersTable.SetColumnName(PlayerFactionTagColumn, MyTexts.Get(MyCommonTexts.ScreenPlayers_FactionTag));
            m_playersTable.SetColumnName(PlayerFactionNameColumn, MyTexts.Get(MyCommonTexts.ScreenPlayers_FactionName));
            m_playersTable.SetColumnName(PlayerMutedColumn, new StringBuilder(MyTexts.GetString(MyCommonTexts.ScreenPlayers_Muted)));
            m_playersTable.SetColumnName(GameAdminColumn, MyTexts.Get(MyCommonTexts.ScreenPlayers_GameAdmin));
            m_playersTable.SetColumnComparison(0, (a, b) => (a.Text.CompareToIgnoreCase(b.Text)));
            m_playersTable.ItemSelected += playersTable_ItemSelected;

            // CH: To show the clients correctly, we would need to know, whether the game is a dedicated-server-hosted game.
            // We don't know that, so I just show all clients with players
            foreach (var player in Sync.Players.GetOnlinePlayers())
            {
                if (player.Id.SerialId != 0) continue;

                for (int i = 0; i < m_playersTable.RowsCount; ++i)
                {
                    var row = m_playersTable.GetRow(i);
                    if (row.UserData is ulong && (ulong)row.UserData == player.Id.SteamId) continue;
                }

                AddPlayer(player.Id.SteamId);
            }

            m_inviteButton.ButtonClicked += inviteButton_ButtonClicked;
            m_promoteButton.ButtonClicked += promoteButton_ButtonClicked;
            m_demoteButton.ButtonClicked += demoteButton_ButtonClicked;
            m_kickButton.ButtonClicked += kickButton_ButtonClicked;
            m_banButton.ButtonClicked += banButton_ButtonClicked;
            m_lobbyTypeCombo.ItemSelected += lobbyTypeCombo_OnSelect;
            m_confirmLimitChangeButton.ButtonClicked += confirmLimitChange_ButtonClicked;

            Controls.Add(m_inviteButton);
            Controls.Add(m_promoteButton);
            Controls.Add(m_demoteButton);
            Controls.Add(m_kickButton);
            Controls.Add(m_banButton);
            Controls.Add(m_playersTable);
            Controls.Add(m_lobbyTypeCombo);
            Controls.Add(m_maxPlayersSlider);
            Controls.Add(maxPlayersLabel);
            if (MySession.Static.MaxBlocksPerPlayer > 0)
            {
                Controls.Add(m_playerBlockLimitTextbox);
                Controls.Add(m_confirmLimitChangeButton);
                Controls.Add(playerBlockLimitLabel);
            }

            //re-select the last selected player in ci
            if (m_lastSelected != 0)
            {
                var select = m_playersTable.Find(r => (ulong)r.UserData == m_lastSelected);
                if (select != null)
                    m_playersTable.SelectedRow = select;
            }

            UpdateButtonsEnabledState();
        }

        protected void MutePlayer(ulong mutedUserId)
        {
            // adding of user into muted players
            m_mutedPlayers.Add(mutedUserId);
            MySandboxGame.Config.MutedPlayers = m_mutedPlayers;
            MySandboxGame.Config.Save();

            // sending of a message about muting
            Sandbox.Game.VoiceChat.MyVoiceChatSessionComponent.MutePlayerRequest(mutedUserId, true);
        }


        protected void UnmutePlayer(ulong mutedUserId)
        {
            // remove of user from muted players
            m_mutedPlayers.Remove(mutedUserId);
            MySandboxGame.Config.MutedPlayers = m_mutedPlayers;
            MySandboxGame.Config.Save();

            // sending of a message about unmuting
            Sandbox.Game.VoiceChat.MyVoiceChatSessionComponent.MutePlayerRequest(mutedUserId, false);
        }

        protected void IsMuteCheckedChanged(MyGuiControlCheckbox obj)
        {
            // some mute player checkbox is changed
            ulong userId = (ulong)obj.UserData;
            if (obj.IsChecked)
                MutePlayer(userId);
            else
                UnmutePlayer(userId);
        }

        public override void HandleInput(bool receivedFocusInThisUpdate)
        {
            base.HandleInput(receivedFocusInThisUpdate);
            if (MyInput.Static.IsNewKeyPressed(MyKeys.F3))
            {
                MyGuiAudio.PlaySound(MyGuiSounds.HudMouseClick);
                CloseScreen();
            }
        }

        protected void AddPlayer(ulong userId)
        {
            //string playerName = SteamAPI.Instance.Friends.GetPersonaName(userId);
            string playerName = MyMultiplayer.Static.GetMemberName(userId);

            if (String.IsNullOrEmpty(playerName))
                return;

            var row = new MyGuiControlTable.Row(userData: userId);
            row.AddCell(new MyGuiControlTable.Cell(text: new StringBuilder(playerName), userData: playerName));

            var playerId = Sync.Players.TryGetIdentityId(userId);
            var faction = MySession.Static.Factions.GetPlayerFaction(playerId);
            row.AddCell(new MyGuiControlTable.Cell(text: new StringBuilder(faction != null ? faction.Tag : "")));
            row.AddCell(new MyGuiControlTable.Cell(text: new StringBuilder(faction != null ? faction.Name : "")));

            // cell with/without mute checkbox
            MyGuiControlTable.Cell cell = new MyGuiControlTable.Cell(text: new StringBuilder(""));
            row.AddCell(cell);

            if (MyPerGameSettings.EnableMutePlayer && userId != Sync.MyId)
            {
                MyGuiControlCheckbox check = new MyGuiControlCheckbox(toolTip: "", visualStyle: MyGuiControlCheckboxStyleEnum.Muted);
                check.IsChecked = MySandboxGame.Config.MutedPlayers.Contains(userId);
                check.IsCheckedChanged += IsMuteCheckedChanged;
                check.UserData = userId;
                cell.Control = check;

                m_playersTable.Controls.Add(check);
            }

            // cell with admin marker
            StringBuilder adminString = new StringBuilder();
            MyPromoteLevel userLevel = MySession.Static.GetUserPromoteLevel(userId);

            for (int i = 0; i < (int)userLevel; i++)
                adminString.Append("*");

            row.AddCell(new MyGuiControlTable.Cell(adminString));
            m_playersTable.Add(row);
        }

        protected void RemovePlayer(ulong userId)
        {
            m_playersTable.Remove((row) => (((ulong)row.UserData) == userId));
            UpdateButtonsEnabledState();
        }

        protected void UpdateButtonsEnabledState()
        {
            if (MyMultiplayer.Static == null)
                return;

            bool hasTarget = m_playersTable.SelectedRow != null;
            ulong currentUserId = SteamAPI.Instance.GetSteamUserId();
            ulong currentOwnerId = MyMultiplayer.Static.GetOwner();
            ulong selectedUserId = hasTarget ? (ulong)m_playersTable.SelectedRow.UserData : 0;
            bool isSelectedSelf = currentUserId == selectedUserId;
            bool isAdmin = MySession.Static.IsUserAdmin(currentUserId);//MyMultiplayer.Static.IsAdmin(currentUserId);
            bool isOwner = (currentUserId == currentOwnerId);
            bool canPromote = hasTarget && MySession.Static.CanPromoteUser(selectedUserId);
            bool canDemote = hasTarget && MySession.Static.CanDemoteUser(selectedUserId);
            var lobbyType = (LobbyTypeEnum)m_lobbyTypeCombo.GetSelectedKey();
            

            if (hasTarget && !isSelectedSelf)
            {
                m_promoteButton.Enabled = canPromote;
                m_demoteButton.Enabled = canDemote;
                m_kickButton.Enabled = canPromote && isAdmin;
                m_banButton.Enabled = canPromote && isAdmin;
            }
            else
            {
                m_promoteButton.Enabled = false;
                m_demoteButton.Enabled = false;
                m_kickButton.Enabled = false;
                m_banButton.Enabled = false;
            }

            m_banButton.Visible = MyMultiplayer.Static is MyMultiplayerClient;

            if (MyMultiplayer.Static.IsServer)
            {
                m_inviteButton.Enabled = (lobbyType == LobbyTypeEnum.Public) || (lobbyType == LobbyTypeEnum.FriendsOnly);
            }
            else
            {
                m_inviteButton.Enabled = (lobbyType == LobbyTypeEnum.Public);
            }

            m_lobbyTypeCombo.Visible = isOwner;
            m_maxPlayersSlider.Visible = isOwner;
            m_lobbyTypeCombo.Enabled = isOwner;
            m_maxPlayersSlider.Enabled = isOwner;

            if (isAdmin)
            {
                var identity = MySession.Static.Players.TryGetIdentity(MySession.Static.Players.TryGetIdentityId(selectedUserId));
                if (identity != null)
                {
                    m_playerBlockLimitTextbox.Text = (MySession.Static.MaxBlocksPerPlayer + identity.BlockLimitModifier).ToString();
                }
                m_confirmLimitChangeButton.Enabled = true;
            }
            else
            {
                m_confirmLimitChangeButton.Enabled = false;
            }
        }

        #region Event handlers

        protected void Multiplayer_PlayerJoined(ulong userId)
        {
            AddPlayer(userId);
        }

        protected void Multiplayer_PlayerLeft(ulong userId, ChatMemberStateChangeEnum arg2)
        {
            RemovePlayer(userId);
        }

        protected void Matchmaking_LobbyDataUpdate(bool success, Lobby lobby, ulong memberOrLobby)
        {
            if (!success)
                return;

            VRage.Profiler.ProfilerShort.Begin("Matchmaking_LobbyDataUpdate");

            var newOwnerId = lobby.GetOwner();
            var oldOwnerRow = m_playersTable.Find((row) => (row.GetCell(GameAdminColumn).Text.Length == OWNER_MARKER.Length));
            var newOwnerRow = m_playersTable.Find((row) => ((ulong)row.UserData) == newOwnerId);
            Debug.Assert(oldOwnerRow != null);
            Debug.Assert(newOwnerRow != null);
            if (oldOwnerRow != null) oldOwnerRow.GetCell(GameAdminColumn).Text.Clear();
            if (newOwnerRow != null) newOwnerRow.GetCell(GameAdminColumn).Text.Clear().Append(OWNER_MARKER);

            var lobbyType = lobby.GetLobbyType();
            m_lobbyTypeCombo.SelectItemByKey((int)lobbyType, sendEvent: false);
            MySession.Static.Settings.OnlineMode = GetOnlineMode(lobbyType);

            UpdateButtonsEnabledState();

            if (!Sync.IsServer)
            {
                m_maxPlayersSlider.ValueChanged = null;
                MySession.Static.Settings.MaxPlayers = (short)MyMultiplayer.Static.MemberLimit;
                m_maxPlayersSlider.Value = MySession.Static.MaxPlayers;
                m_maxPlayersSlider.ValueChanged = MaxPlayersSlider_Changed;
            }
            VRage.Profiler.ProfilerShort.End();
        }

        protected MyOnlineModeEnum GetOnlineMode(LobbyTypeEnum lobbyType)
        {
            switch (lobbyType)
            {
                case LobbyTypeEnum.Private: return MyOnlineModeEnum.PRIVATE;
                case LobbyTypeEnum.FriendsOnly: return MyOnlineModeEnum.FRIENDS;
                case LobbyTypeEnum.Public: return MyOnlineModeEnum.PUBLIC;

                case LobbyTypeEnum.Invisible:
                default:
                    Debug.Fail("Invalid branch.");
                    return MyOnlineModeEnum.PUBLIC;
            }
        }

        protected void playersTable_ItemSelected(MyGuiControlTable table, MyGuiControlTable.EventArgs args)
        {
            Debug.Assert(table == m_playersTable);
            UpdateButtonsEnabledState();

            if(m_playersTable.SelectedRow != null)
                m_lastSelected = (ulong)m_playersTable.SelectedRow.UserData;
        }

        protected void inviteButton_ButtonClicked(MyGuiControlButton obj)
        {
            Lobby.OpenInviteOverlay();
        }

        protected void makeOwnerButton_ButtonClicked(MyGuiControlButton obj)
        {
            if (m_playersTable.SelectedRow != null)
            {
                ulong oldOwnerId = MyMultiplayer.Static.GetOwner();
                ulong newOwnerId = (ulong)m_playersTable.SelectedRow.UserData;
                MyMultiplayer.Static.SetOwner(newOwnerId);
            }
        }

        protected void promoteButton_ButtonClicked(MyGuiControlButton obj)
        {
            var selectedRow = m_playersTable.SelectedRow;
            if (selectedRow != null)
                MyMultiplayer.RaiseStaticEvent(x => Promote, (ulong)selectedRow.UserData, true);
        }

        protected void demoteButton_ButtonClicked(MyGuiControlButton obj)
        {
            var selectedRow = m_playersTable.SelectedRow;
            if (selectedRow != null)
                MyMultiplayer.RaiseStaticEvent(x => Promote, (ulong)selectedRow.UserData, false);
        }

        protected void confirmLimitChange_ButtonClicked(MyGuiControlButton obj)
        {
            var selectedRow = m_playersTable.SelectedRow;
            int newAmount;
            if (selectedRow != null && int.TryParse(m_playerBlockLimitTextbox.Text, out newAmount) && newAmount >= 0)
            {
                var identityID = MySession.Static.Players.TryGetIdentityId((ulong)selectedRow.UserData);
                MyMultiplayer.RaiseStaticEvent(x => ChangePlayerBlockLimit, identityID, newAmount - MySession.Static.MaxBlocksPerPlayer);
            }
        }

        [Event, Reliable, Server, Broadcast]
        protected static void Promote(ulong playerId, bool promote)
        {
            if (!MyEventContext.Current.IsLocallyInvoked && !MySession.Static.IsUserAdmin(MyEventContext.Current.Sender.Value))
            {
                MyEventContext.ValidationFailed();
                return;
            }
            
            MyPromoteLevel targetUserLevel = MySession.Static.GetUserPromoteLevel(playerId);

            if (promote)
            {
                targetUserLevel++;
                if (!MySession.Static.EnableScripterRole && targetUserLevel == MyPromoteLevel.Scripter)
                    targetUserLevel++;
                MySession.Static.PromotedUsers[playerId] = targetUserLevel;
            }
            else
            {
                targetUserLevel--;
                if (!MySession.Static.EnableScripterRole && targetUserLevel == MyPromoteLevel.Scripter)
                    targetUserLevel--;
                if (targetUserLevel > MyPromoteLevel.None)
                    MySession.Static.PromotedUsers[playerId] = targetUserLevel;
                else
                    MySession.Static.PromotedUsers.Remove(playerId);
            }

            if (Sync.IsServer)
            {
                MyMultiplayer.RaiseStaticEvent(x => ShowPromoteMessage, targetUserLevel, promote, new EndpointId(playerId));
            }
            Refresh();
        }

        [Event, Reliable, Client]
        protected static void ShowPromoteMessage(MyPromoteLevel promoteLevel, bool promote)
        {
            ClearPromoteNotificaions();
            switch (promoteLevel)
            {
                case MyPromoteLevel.None:
                    MyHud.Notifications.Add(MyNotificationSingletons.PlayerDemotedNone);
                    break;
                case MyPromoteLevel.Scripter:
                    MyHud.Notifications.Add(promote ? MyNotificationSingletons.PlayerPromotedScripter : MyNotificationSingletons.PlayerDemotedScripter);
                    break;
                case MyPromoteLevel.Moderator:
                    MyHud.Notifications.Add(promote ? MyNotificationSingletons.PlayerPromotedModerator : MyNotificationSingletons.PlayerDemotedModerator);
                    break;
                case MyPromoteLevel.SpaceMaster:
                    MyHud.Notifications.Add(promote ? MyNotificationSingletons.PlayerPromotedSpaceMaster : MyNotificationSingletons.PlayerDemotedSpaceMaster);
                    break;
                case MyPromoteLevel.Admin:
                    MyHud.Notifications.Add(MyNotificationSingletons.PlayerPromotedAdmin);
                    break;
                case MyPromoteLevel.Owner:
                    break;
                default:
                    throw new ArgumentOutOfRangeException("promoteLevel", promoteLevel, null);
            }
        }

        private static void ClearPromoteNotificaions()
        {
            MyHud.Notifications.Remove(MyNotificationSingletons.PlayerDemotedNone);
            MyHud.Notifications.Remove(MyNotificationSingletons.PlayerDemotedScripter);
            MyHud.Notifications.Remove(MyNotificationSingletons.PlayerDemotedModerator);
            MyHud.Notifications.Remove(MyNotificationSingletons.PlayerDemotedSpaceMaster);
            MyHud.Notifications.Remove(MyNotificationSingletons.PlayerPromotedScripter);
            MyHud.Notifications.Remove(MyNotificationSingletons.PlayerPromotedModerator);
            MyHud.Notifications.Remove(MyNotificationSingletons.PlayerPromotedSpaceMaster);
            MyHud.Notifications.Remove(MyNotificationSingletons.PlayerPromotedAdmin);
        }

        [Event, Reliable, Server, Broadcast]
        protected static void ChangePlayerBlockLimit(long identityID, int newAmount)
        {
            var identity = MySession.Static.Players.TryGetIdentity(identityID);
            if (identity != null)
            {
                identity.BlockLimitModifier = newAmount;
            }
        }

        protected static void Refresh()
        {
            if (!MySandboxGame.IsDedicated)
            {
                var playerScreen = MyScreenManager.GetFirstScreenOfType<MyGuiScreenPlayers>();
                if (playerScreen != null)
                    playerScreen.RecreateControls(false);
            }            
        }

        protected void kickButton_ButtonClicked(MyGuiControlButton obj)
        {
            var selectedRow = m_playersTable.SelectedRow;
            if (selectedRow != null)
                MyMultiplayer.Static.KickClient((ulong)selectedRow.UserData);
        }

        protected void banButton_ButtonClicked(MyGuiControlButton obj)
        {
            var selectedRow = m_playersTable.SelectedRow;
            if (selectedRow != null)
                MyMultiplayer.Static.BanClient((ulong)selectedRow.UserData, true);
        }

        protected void lobbyTypeCombo_OnSelect()
        {
            var selectedLobbyType = (LobbyTypeEnum)m_lobbyTypeCombo.GetSelectedKey();
            // select current type since we don't know whether change is successful yet
            m_lobbyTypeCombo.SelectItemByKey((int)MyMultiplayer.Static.GetLobbyType(), sendEvent: false);

            // change the lobby type (combobox will update when we get LobbyDataUpdate event)
            MyMultiplayer.Static.SetLobbyType(selectedLobbyType);
        }

        protected void MaxPlayersSlider_Changed(MyGuiControlSlider control)
        {
            MySession.Static.Settings.MaxPlayers = (short)m_maxPlayersSlider.Value;
            MyMultiplayer.Static.SetMemberLimit(MySession.Static.MaxPlayers);
        }

        private void OnFactionCreated(long insertedId)
        {
            Refresh();
        }

        private void OnFactionEdited(long editedId)
        {
            Refresh();
        }

        private void OnFactionStateChanged(MyFactionCollection.MyFactionStateChange action, long fromFactionId, long toFactionId, long playerId, long senderId)
        {
            Refresh();
        }


        #endregion

    }
}
