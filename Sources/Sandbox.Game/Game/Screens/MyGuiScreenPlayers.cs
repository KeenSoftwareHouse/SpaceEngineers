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
using VRage;
using VRage.Input;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Gui
{
    class MyGuiScreenPlayers : MyGuiScreenBase
    {
        private static readonly string GAME_OWNER_MARKER = "*****";

        private MyGuiControlTable m_playersTable;
        private MyGuiControlButton m_inviteButton;
        private MyGuiControlButton m_kickButton;
        private MyGuiControlButton m_banButton;
        private MyGuiControlCombobox m_lobbyTypeCombo;
        private MyGuiControlSlider m_maxPlayersSlider;

        public MyGuiScreenPlayers() :
            base(size: MyGuiConstants.TEXTURE_SCREEN_BACKGROUND.SizeGui * 1.1f,
                 backgroundColor: MyGuiConstants.SCREEN_BACKGROUND_COLOR,
                 backgroundTexture: MyGuiConstants.TEXTURE_SCREEN_BACKGROUND.Texture)
        {
            EnabledBackgroundFade = true;

            MyMultiplayer.Static.ClientJoined             += Multiplayer_PlayerJoined;
            MyMultiplayer.Static.ClientLeft               += Multiplayer_PlayerLeft;
            SteamAPI.Instance.Matchmaking.LobbyDataUpdate += Matchmaking_LobbyDataUpdate;

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
            SteamAPI.Instance.Matchmaking.LobbyDataUpdate -= Matchmaking_LobbyDataUpdate;
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            CloseButtonEnabled = true;

            var caption = AddCaption(MySpaceTexts.ScreenCaptionPlayers);
            var captionCenter = MyUtils.GetCoordCenterFromAligned(caption.Position, caption.Size, caption.OriginAlign);
            var captionBottomCenter = captionCenter + new Vector2(0f, 0.5f * caption.Size.Y);

            Vector2 sizeScale = Size.Value / MyGuiConstants.TEXTURE_SCREEN_BACKGROUND.SizeGui;
            Vector2 topLeft = -0.5f * Size.Value + sizeScale * MyGuiConstants.TEXTURE_SCREEN_BACKGROUND.PaddingSizeGui * 1.1f;

            float verticalSpacing = 0.5f * caption.Size.Y;

            m_lobbyTypeCombo = new MyGuiControlCombobox(
                position: new Vector2(-topLeft.X, captionBottomCenter.Y + verticalSpacing),
                openAreaItemsCount: 3);
            m_lobbyTypeCombo.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP;
            m_lobbyTypeCombo.AddItem((int)LobbyTypeEnum.Private, MySpaceTexts.ScreenPlayersLobby_Private);
            m_lobbyTypeCombo.AddItem((int)LobbyTypeEnum.FriendsOnly, MySpaceTexts.ScreenPlayersLobby_Friends);
            m_lobbyTypeCombo.AddItem((int)LobbyTypeEnum.Public, MySpaceTexts.ScreenPlayersLobby_Public);
            m_lobbyTypeCombo.SelectItemByKey((int)MyMultiplayer.Static.GetLobbyType());

            MyGuiControlBase aboveControl;

            m_inviteButton = new MyGuiControlButton(
                position: new Vector2(-m_lobbyTypeCombo.Position.X, m_lobbyTypeCombo.Position.Y + m_lobbyTypeCombo.Size.Y + verticalSpacing),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                text: MyTexts.Get(MySpaceTexts.ScreenPlayers_Invite));
            aboveControl = m_inviteButton;

            m_kickButton = new MyGuiControlButton(
                position: aboveControl.Position + new Vector2(0f, aboveControl.Size.Y),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                text: MyTexts.Get(MySpaceTexts.ScreenPlayers_Kick));
            aboveControl = m_kickButton;

            m_banButton = new MyGuiControlButton(
                position: aboveControl.Position + new Vector2(0f, aboveControl.Size.Y),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                text: MyTexts.Get(MySpaceTexts.ScreenPlayers_Ban));
            aboveControl = m_banButton;

            var maxPlayersLabel = new MyGuiControlLabel(
                position: aboveControl.Position + new Vector2(0f, aboveControl.Size.Y),
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                text: MyTexts.GetString(MySpaceTexts.MaxPlayers));
            aboveControl = maxPlayersLabel;

            m_maxPlayersSlider = new MyGuiControlSlider(
                position: aboveControl.Position + new Vector2(0f, aboveControl.Size.Y),
                width: 0.15f,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                minValue: 0,
                maxValue: MyMultiplayer.Static != null ? MyMultiplayer.Static.MaxPlayers : 16,
                labelText: new StringBuilder("{0}").ToString(),
                labelDecimalPlaces: 0,
                labelSpaceWidth: 0.02f,
                defaultValue: Sync.IsServer ? MySession.Static.MaxPlayers : MyMultiplayer.Static.MemberLimit,
                intValue: true);
            m_maxPlayersSlider.ValueChanged = MaxPlayersSlider_Changed;
            aboveControl = m_maxPlayersSlider;

            m_playersTable = new MyGuiControlTable()
            {
                Position = new Vector2(-m_inviteButton.Position.X, m_inviteButton.Position.Y),
                Size = new Vector2(1075f / MyGuiConstants.GUI_OPTIMAL_SIZE.X, 1f) - m_inviteButton.Size * 1.05f,
                VisibleRowsCount = 21,
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP,
                ColumnsCount = 2,
            };
            m_playersTable.SetCustomColumnWidths(new float[] { 0.7f, 0.3f });
            m_playersTable.SetColumnName(0, MyTexts.Get(MySpaceTexts.ScreenPlayers_PlayerName));
            m_playersTable.SetColumnName(1, MyTexts.Get(MySpaceTexts.ScreenPlayers_GameAdmin));
            m_playersTable.SetColumnComparison(0, (a, b) => (a.Text.CompareToIgnoreCase(b.Text)));
            m_playersTable.ItemSelected += playersTable_ItemSelected;
            foreach (var userId in MyMultiplayer.Static.Members)
                AddPlayer(userId);

            m_inviteButton.ButtonClicked  += inviteButton_ButtonClicked;
            m_kickButton.ButtonClicked    += kickButton_ButtonClicked;
            m_banButton.ButtonClicked     += banButton_ButtonClicked;
            m_lobbyTypeCombo.ItemSelected += lobbyTypeCombo_OnSelect;

            Controls.Add(m_inviteButton);
            Controls.Add(m_kickButton);
            Controls.Add(m_banButton);
            Controls.Add(m_playersTable);
            Controls.Add(m_lobbyTypeCombo);
            Controls.Add(m_maxPlayersSlider);
            Controls.Add(maxPlayersLabel);

            UpdateButtonsEnabledState();
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

        private void AddPlayer(ulong userId)
        {
            //string playerName = SteamAPI.Instance.Friends.GetPersonaName(userId);
            string playerName = MyMultiplayer.Static.GetMemberName(userId);

            if (String.IsNullOrEmpty(playerName))
                return;

            bool isAdmin = MyMultiplayer.Static.IsAdmin(userId);

            var row = new MyGuiControlTable.Row(userData: userId);
            row.AddCell(new MyGuiControlTable.Cell(text: new StringBuilder(playerName), userData: playerName));
            row.AddCell(new MyGuiControlTable.Cell(text: new StringBuilder(isAdmin ? GAME_OWNER_MARKER : "")));
            m_playersTable.Add(row);
        }

        private void RemovePlayer(ulong userId)
        {
            m_playersTable.Remove((row) => (((ulong)row.UserData) == userId));
            UpdateButtonsEnabledState();
        }

        private void UpdateButtonsEnabledState()
        {
            if (MyMultiplayer.Static == null)
                return;

            bool hasTarget       = m_playersTable.SelectedRow != null;
            ulong currentUserId  = SteamAPI.Instance.GetSteamUserId();
            ulong currentOwnerId = MyMultiplayer.Static.GetOwner();
            bool isAdmin         = MyMultiplayer.Static.IsAdmin(currentUserId);
            bool isOwner         = (currentUserId == currentOwnerId);
            var lobbyType        = (LobbyTypeEnum)m_lobbyTypeCombo.GetSelectedKey();
            if (hasTarget && isAdmin)
            {
                var selectedUserId   = (ulong)m_playersTable.SelectedRow.UserData;
                bool selectedIsAdmin = MyMultiplayer.Static.IsAdmin(selectedUserId);

                m_kickButton.Enabled = selectedUserId != currentUserId && !selectedIsAdmin;
                m_banButton.Enabled = selectedUserId != currentUserId && !selectedIsAdmin;
            }
            else
            {
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
            m_lobbyTypeCombo.Enabled = isOwner;
            m_maxPlayersSlider.Enabled = isOwner;
        }

        #region Event handlers

        private void Multiplayer_PlayerJoined(ulong userId)
        {
            AddPlayer(userId);
        }

        private void Multiplayer_PlayerLeft(ulong userId, ChatMemberStateChangeEnum arg2)
        {
            RemovePlayer(userId);
        }

        private void Matchmaking_LobbyDataUpdate(bool success, Lobby lobby, ulong memberOrLobby)
        {
            if (!success)
                return;

            var newOwnerId = lobby.GetOwner();
            var oldOwnerRow = m_playersTable.Find((row) => (row.GetCell(1).Text.Length == GAME_OWNER_MARKER.Length));
            var newOwnerRow = m_playersTable.Find((row) => ((ulong)row.UserData) == newOwnerId);
            Debug.Assert(oldOwnerRow != null);
            Debug.Assert(newOwnerRow != null);
            if (oldOwnerRow != null) oldOwnerRow.GetCell(1).Text.Clear();
            if (newOwnerRow != null) newOwnerRow.GetCell(1).Text.Clear().Append(GAME_OWNER_MARKER);

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
        }

        private MyOnlineModeEnum GetOnlineMode(LobbyTypeEnum lobbyType)
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

        private void playersTable_ItemSelected(MyGuiControlTable table, MyGuiControlTable.EventArgs args)
        {
            Debug.Assert(table == m_playersTable);
            UpdateButtonsEnabledState();
        }

        private void inviteButton_ButtonClicked(MyGuiControlButton obj)
        {
            Lobby.OpenInviteOverlay();
        }

        private void makeOwnerButton_ButtonClicked(MyGuiControlButton obj)
        {
            if (m_playersTable.SelectedRow != null)
            {
                ulong oldOwnerId = MyMultiplayer.Static.GetOwner();
                ulong newOwnerId = (ulong)m_playersTable.SelectedRow.UserData;
                MyMultiplayer.Static.SetOwner(newOwnerId);
            }
        }

        private void kickButton_ButtonClicked(MyGuiControlButton obj)
        {
            var selectedRow = m_playersTable.SelectedRow;
            if (selectedRow != null)
                MyMultiplayer.Static.KickClient((ulong)selectedRow.UserData);
        }

        private void banButton_ButtonClicked(MyGuiControlButton obj)
        {
            var selectedRow = m_playersTable.SelectedRow;
            if (selectedRow != null)
                MyMultiplayer.Static.BanClient((ulong)selectedRow.UserData, true);
        }

        private void lobbyTypeCombo_OnSelect()
        {
            var selectedLobbyType = (LobbyTypeEnum)m_lobbyTypeCombo.GetSelectedKey();
            // select current type since we don't know whether change is successful yet
            m_lobbyTypeCombo.SelectItemByKey((int)MyMultiplayer.Static.GetLobbyType(), sendEvent: false);

            // change the lobby type (combobox will update when we get LobbyDataUpdate event)
            MyMultiplayer.Static.SetLobbyType(selectedLobbyType);
        }

        private void MaxPlayersSlider_Changed(MyGuiControlSlider control)
        {
            MySession.Static.Settings.MaxPlayers = (short)m_maxPlayersSlider.Value;
            MyMultiplayer.Static.SetMemberLimit(MySession.Static.MaxPlayers);
        }

        #endregion

    }
}
