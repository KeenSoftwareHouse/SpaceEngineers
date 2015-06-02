using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Gui;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using VRage;
using VRage.Collections;
using VRage.Input;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.Gui;
using System.Diagnostics;
using SteamSDK;
using Sandbox.Game.GUI.HudViewers;
using Sandbox.Game.Localization;
using Sandbox.Game.GameSystems;

namespace Sandbox.Game.Screens
{
    public abstract class MyGuiScreenScenarioMpBase : MyGuiScreenBase
    {
        /*private static string FACTION_PLAYERS_FMT_TEXT = "Players : {0}/{1}";

        protected MyGuiControlTextbox m_points1Textbox;
        protected MyGuiControlTextbox m_points2Textbox;
        protected MyGuiControlLabel m_playersFaction1Label;
        protected MyGuiControlLabel m_playersFaction2Label;
        protected MyGuiControlTextbox m_timeLimitTextbox;
        protected MyGuiControlLabel m_hostnameLabel;
        protected MyGuiControlSlider m_maxPlayersSlider;
        protected MyGuiControlCombobox m_onlineModeComboBox;
        protected MyGuiControlCombobox m_faction1ComboBox;
        protected MyGuiControlCombobox m_faction2ComboBox;
        protected MyGuiControlButton m_joinFaction1Button;
        protected MyGuiControlButton m_joinFaction2Button;
        protected MyGuiControlButton m_leaderFaction1Button;
        protected MyGuiControlButton m_leaderFaction2Button;
        protected MyGuiControlButton m_kickFaction1Button;*/
        protected MyGuiControlButton m_kickPlayerButton;
        /*protected MyGuiControlButton m_planAttackFaction1Button;
        protected MyGuiControlButton m_planAttackFaction2Button;
        protected MyGuiControlCheckbox m_readyFaction1Checkbox;
        protected MyGuiControlCheckbox m_readyFaction2Checkbox;
        protected MyGuiControlTable m_playersFaction1List;*/
        protected MyGuiControlTable m_connectedPlayers;
        MyGuiControlLabel m_timeoutLabel;
        MyGuiControlCombobox m_timeoutCombo;
        
        protected MyHudControlChat m_chatControl;
        protected MyGuiControlTextbox m_chatTextbox;
        protected MyGuiControlButton m_sendChatButton;
        
        protected MyGuiControlButton m_startButton;

        private bool m_update;

        private StringBuilder m_editBoxStringBuilder = new StringBuilder();

        private long m_battleFaction1Id;
        private long m_battleFaction2Id;

        protected static HashSet<ulong> m_readyPlayers = new HashSet<ulong>();

        private enum MyBattleStartFailState
        {
            None,
            InvalidTimeLimit,
            Faction1_NotReady,
            Faction2_NotReady,
            Faction1_InvalidMaxBattlePoints,
            Faction2_InvalidMaxBattlePoints,
            Faction1_SelectedBluprintsExceedsMaxPoints,
            Faction2_SelectedBluprintsExceedsMaxPoints,
        }

        private MyBattleStartFailState m_startFailState = MyBattleStartFailState.None;


        public MyGuiScreenScenarioMpBase()
            : base(position: new Vector2(0.5f, 0.5f), backgroundColor: MyGuiConstants.SCREEN_BACKGROUND_COLOR, size: new Vector2(1620f, 1125f) / MyGuiConstants.GUI_OPTIMAL_SIZE)
        {
            RecreateControls(true);
            CanHideOthers = false;
            MySyncScenario.PlayerReadyToStartScenario += MySyncScenario_PlayerReady;
            MySyncScenario.TimeoutReceived += MySyncScenario_SetTimeout;
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            /*AddCaption(MyMedievalTexts.ScreenCaptionBattleLobby);

            var hostLabel = new MyGuiControlLabel(text: "Host:", originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            m_hostnameLabel = new MyGuiControlLabel(originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            var timeLimitLabel = new MyGuiControlLabel(text: "Time limit [min]:", originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            var maxPlayersLabel = new MyGuiControlLabel(text: "Max players:", originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            var onlineModeLabel = new MyGuiControlLabel(text: "Online mode:", originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            m_playersFaction1Label = new MyGuiControlLabel(originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            m_playersFaction2Label = new MyGuiControlLabel(originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            var readyFaction1Label = new MyGuiControlLabel(text: "Ready:", originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            var readyFaction2Label = new MyGuiControlLabel(text: "Ready:", originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            var pointsFaction1Label = new MyGuiControlLabel(text: "Points:", originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            var pointsFaction2Label = new MyGuiControlLabel(text: "Points:", originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);

            m_timeLimitTextbox = new MyGuiControlTextbox(type: MyGuiControlTextboxType.DigitsOnly);
            m_timeLimitTextbox.Size = new Vector2(100f, 48f) / MyGuiConstants.GUI_OPTIMAL_SIZE;
            m_timeLimitTextbox.Enabled = Sync.IsServer;
            m_timeLimitTextbox.SetText(m_editBoxStringBuilder.Clear().Append(MyBattleGameComponent.DEFAULT_BATTLE_TIME_LIMIT_MIN));
            m_timeLimitTextbox.TextChanged += TimeLimitTextbox_TextChanged;

            m_points1Textbox = new MyGuiControlTextbox(type: MyGuiControlTextboxType.DigitsOnly);
            m_points1Textbox.Size = new Vector2(100f, 48f) / MyGuiConstants.GUI_OPTIMAL_SIZE;
            m_points1Textbox.Enabled = Sync.IsServer;
            m_points1Textbox.SetText(m_editBoxStringBuilder.Clear().Append(MyBattleGameComponent.DEFAULT_BATTLE_MAX_BLUEPRINT_POINTS));
            m_points1Textbox.TextChanged += Points1Textbox_TextChanged;
            m_points1Textbox.Enabled = false;

            m_points2Textbox = new MyGuiControlTextbox(type: MyGuiControlTextboxType.DigitsOnly);
            m_points2Textbox.Size = new Vector2(100f, 48f) / MyGuiConstants.GUI_OPTIMAL_SIZE;
            m_points2Textbox.Enabled = Sync.IsServer;
            m_points2Textbox.SetText(m_editBoxStringBuilder.Clear().Append(MyBattleGameComponent.DEFAULT_BATTLE_MAX_BLUEPRINT_POINTS));
            m_points2Textbox.TextChanged += Points2Textbox_TextChanged;

            m_onlineModeComboBox = new MyGuiControlCombobox(size: new Vector2(200f, 48f) / MyGuiConstants.GUI_OPTIMAL_SIZE);
            m_onlineModeComboBox.AddItem((int)MyOnlineModeEnum.PRIVATE, MyMedievalTexts.WorldSettings_OnlineModePrivate);
            m_onlineModeComboBox.AddItem((int)MyOnlineModeEnum.FRIENDS, MyMedievalTexts.WorldSettings_OnlineModeFriends);
            m_onlineModeComboBox.AddItem((int)MyOnlineModeEnum.PUBLIC, MyMedievalTexts.WorldSettings_OnlineModePublic);
            m_onlineModeComboBox.SelectItemByKey((int)MyBattleGameComponent.GetOnlineModeFromCurrentLobbyType());
            m_onlineModeComboBox.Enabled = false;
            m_onlineModeComboBox.ItemSelected += OnlineModeComboBox_ItemSelected;

            m_maxPlayersSlider = new MyGuiControlSlider(
                position: Vector2.Zero,
                width: 200 / MyGuiConstants.GUI_OPTIMAL_SIZE.X,
                minValue: 2,
                maxValue: 16,
                labelText: new StringBuilder("{0}").ToString(),
                labelDecimalPlaces: 0,
                labelSpaceWidth: 0.05f,
                intValue: true
                );
            m_maxPlayersSlider.Enabled = false;
            m_maxPlayersSlider.ValueChanged += MaxPlayersSlider_ValueChanged;
            if (Sync.IsServer && MyMultiplayer.Static != null)
                m_maxPlayersSlider.Value = MyMultiplayer.Static.MemberLimit;

            m_faction1ComboBox = new MyGuiControlCombobox(size: new Vector2(200f, 48f) / MyGuiConstants.GUI_OPTIMAL_SIZE);
            m_faction1ComboBox.AddItem(0, "Defender");
            m_faction1ComboBox.SelectItemByIndex(0);
            m_faction1ComboBox.Enabled = Sync.IsServer;

            m_joinFaction1Button = new MyGuiControlButton(text: new StringBuilder("Join"), visualStyle: MyGuiControlButtonStyleEnum.Rectangular, highlightType: MyGuiControlHighlightType.WHEN_ACTIVE,
                size: new Vector2(190f, 48f) / MyGuiConstants.GUI_OPTIMAL_SIZE, onButtonClick: OnJoin1Clicked);

            m_playersFaction1List = new MyGuiControlTable();
            m_playersFaction1List.Size = new Vector2(490f, 150f) / MyGuiConstants.GUI_OPTIMAL_SIZE;
            m_playersFaction1List.VisibleRowsCount = 4;
            m_playersFaction1List.ColumnsCount = 2;
            m_playersFaction1List.SetCustomColumnWidths(new float[] { 0.7f, 0.3f });
            m_playersFaction1List.SetColumnName(0, MyTexts.Get(MyMedievalTexts.Name));

            m_leaderFaction1Button = new MyGuiControlButton(text: new StringBuilder("Leader"), visualStyle: MyGuiControlButtonStyleEnum.Rectangular, highlightType: MyGuiControlHighlightType.WHEN_ACTIVE,
                size: new Vector2(190f, 48f) / MyGuiConstants.GUI_OPTIMAL_SIZE, onButtonClick: OnLeader1Clicked);
            m_kickFaction1Button = new MyGuiControlButton(text: new StringBuilder("Kick"), visualStyle: MyGuiControlButtonStyleEnum.Rectangular, highlightType: MyGuiControlHighlightType.WHEN_ACTIVE,
                size: new Vector2(190f, 48f) / MyGuiConstants.GUI_OPTIMAL_SIZE, onButtonClick: OnKick1Clicked);
            m_planAttackFaction1Button = new MyGuiControlButton(text: new StringBuilder("Plan attack"), visualStyle: MyGuiControlButtonStyleEnum.Rectangular, highlightType: MyGuiControlHighlightType.WHEN_ACTIVE,
                size: new Vector2(190f, 48f) / MyGuiConstants.GUI_OPTIMAL_SIZE, onButtonClick: OnPlanAttackClicked);
            m_planAttackFaction1Button.Enabled = false;
            m_readyFaction1Checkbox = new MyGuiControlCheckbox();
            m_readyFaction1Checkbox.IsCheckedChanged += Faction1Ready_IsCheckedChanged;

            m_faction2ComboBox = new MyGuiControlCombobox(size: new Vector2(200f, 48f) / MyGuiConstants.GUI_OPTIMAL_SIZE);
            m_faction2ComboBox.AddItem(0, "Attacker");
            m_faction2ComboBox.SelectItemByIndex(0);
            m_faction2ComboBox.Enabled = Sync.IsServer;

            m_joinFaction2Button = new MyGuiControlButton(text: new StringBuilder("Join"), visualStyle: MyGuiControlButtonStyleEnum.Rectangular, highlightType: MyGuiControlHighlightType.WHEN_ACTIVE,
                size: new Vector2(190f, 48f) / MyGuiConstants.GUI_OPTIMAL_SIZE, onButtonClick: OnJoin2Clicked);
            */
            m_connectedPlayers = new MyGuiControlTable();
            m_connectedPlayers.Size = new Vector2(490f, 150f) / MyGuiConstants.GUI_OPTIMAL_SIZE;
            m_connectedPlayers.VisibleRowsCount = 8;
            m_connectedPlayers.ColumnsCount = 2;
            m_connectedPlayers.SetCustomColumnWidths(new float[] { 0.7f, 0.3f });
            m_connectedPlayers.SetColumnName(0, MyTexts.Get(MySpaceTexts.GuiScenarioPlayerName));
            m_connectedPlayers.SetColumnName(1, MyTexts.Get(MySpaceTexts.GuiScenarioPlayerStatus));
            /*
            m_leaderFaction2Button = new MyGuiControlButton(text: new StringBuilder("Leader"), visualStyle: MyGuiControlButtonStyleEnum.Rectangular, highlightType: MyGuiControlHighlightType.WHEN_ACTIVE,
                size: new Vector2(190f, 48f) / MyGuiConstants.GUI_OPTIMAL_SIZE, onButtonClick: OnLeader2Clicked);*/
            m_kickPlayerButton = new MyGuiControlButton(text: MyTexts.Get(MySpaceTexts.Kick), visualStyle: MyGuiControlButtonStyleEnum.Rectangular, highlightType: MyGuiControlHighlightType.WHEN_ACTIVE,
                size: new Vector2(190f, 48f) / MyGuiConstants.GUI_OPTIMAL_SIZE, onButtonClick: OnKick2Clicked);
            m_kickPlayerButton.Enabled = CanKick();

            m_timeoutLabel = new MyGuiControlLabel(text: MyTexts.GetString(MySpaceTexts.GuiScenarioTimeout), originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);

            m_timeoutCombo = new MyGuiControlCombobox();
            m_timeoutCombo.ItemSelected += OnTimeoutSelected;
            m_timeoutCombo.AddItem(3, MyTexts.Get(MySpaceTexts.GuiScenarioTimeout3min));
            m_timeoutCombo.AddItem(5, MyTexts.Get(MySpaceTexts.GuiScenarioTimeout5min));
            m_timeoutCombo.AddItem(10, MyTexts.Get(MySpaceTexts.GuiScenarioTimeout10min));
            m_timeoutCombo.AddItem(-1, MyTexts.Get(MySpaceTexts.GuiScenarioTimeoutUnlimited));
            m_timeoutCombo.SelectItemByIndex(0);
            m_timeoutCombo.Enabled = Sync.IsServer;

            /*m_planAttackFaction2Button = new MyGuiControlButton(text: new StringBuilder("Plan attack"), visualStyle: MyGuiControlButtonStyleEnum.Rectangular, highlightType: MyGuiControlHighlightType.WHEN_ACTIVE,
                size: new Vector2(190f, 48f) / MyGuiConstants.GUI_OPTIMAL_SIZE, onButtonClick: OnPlanAttackClicked);
            m_readyFaction2Checkbox = new MyGuiControlCheckbox();
            m_readyFaction2Checkbox.IsCheckedChanged += Faction2Ready_IsCheckedChanged;
            */
            m_startButton = new MyGuiControlButton(text: MyTexts.Get(MySpaceTexts.GuiScenarioStart), visualStyle: MyGuiControlButtonStyleEnum.Rectangular, highlightType: MyGuiControlHighlightType.WHEN_ACTIVE,
                size: new Vector2(200, 48f) / MyGuiConstants.GUI_OPTIMAL_SIZE, onButtonClick: OnStartClicked);
            m_startButton.Enabled = Sync.IsServer;

            m_chatControl = new MyHudControlChat(
                MyHud.Chat,
                size: new Vector2(1400f, 300f) / MyGuiConstants.GUI_OPTIMAL_SIZE,
                font: MyFontEnum.DarkBlue,
                textScale: 0.7f,
                textAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM,
                backgroundColor: MyGuiConstants.THEMED_GUI_BACKGROUND_COLOR,
                contents: null,
                drawScrollbar: true,
                textBoxAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM);
            m_chatControl.BorderEnabled = true;
            m_chatControl.BorderColor = Color.CornflowerBlue;

            m_chatTextbox = new MyGuiControlTextbox(maxLength: ChatMessageBuffer.MAX_MESSAGE_SIZE);
            m_chatTextbox.Size = new Vector2(1400f, 48f) / MyGuiConstants.GUI_OPTIMAL_SIZE;
            m_chatTextbox.TextScale = 0.8f;
            m_chatTextbox.VisualStyle = MyGuiControlTextboxStyleEnum.Default;
            m_chatTextbox.EnterPressed += ChatTextbox_EnterPressed;

            m_sendChatButton = new MyGuiControlButton(text: MyTexts.Get(MySpaceTexts.GuiScenarioSend), visualStyle: MyGuiControlButtonStyleEnum.Rectangular, highlightType: MyGuiControlHighlightType.WHEN_ACTIVE,
                size: new Vector2(190f, 48f) / MyGuiConstants.GUI_OPTIMAL_SIZE, onButtonClick: OnSendChatClicked);


            var layout = new MyLayoutTable(this);
            layout.SetColumnWidthsNormalized(50, 300, 300, 300, 300, 300, 50);
            layout.SetRowHeightsNormalized(50, 450, 70, 70, 70, 400, 70, 70, 50);

            /*layout.Add(hostLabel, MyAlignH.Left, MyAlignV.Center, 1, 1);
            layout.Add(m_hostnameLabel, MyAlignH.Left, MyAlignV.Center, 1, 2);
            layout.Add(timeLimitLabel, MyAlignH.Left, MyAlignV.Center, 1, 3);
            layout.Add(m_timeLimitTextbox, MyAlignH.Left, MyAlignV.Center, 1, 4);
            layout.Add(maxPlayersLabel, MyAlignH.Left, MyAlignV.Center, 1, 5);
            layout.Add(m_maxPlayersSlider, MyAlignH.Left, MyAlignV.Center, 1, 6);
            layout.Add(onlineModeLabel, MyAlignH.Left, MyAlignV.Center, 1, 8);
            layout.Add(m_onlineModeComboBox, MyAlignH.Right, MyAlignV.Center, 1, 9);

            layout.Add(m_faction1ComboBox, MyAlignH.Left, MyAlignV.Center, 3, 1, rowSpan: 1, colSpan: 2);
            layout.Add(m_playersFaction1Label, MyAlignH.Left, MyAlignV.Center, 3, 3);
            layout.Add(m_joinFaction1Button, MyAlignH.Left, MyAlignV.Center, 4, 5);
            layout.Add(m_planAttackFaction1Button, MyAlignH.Left, MyAlignV.Center, 4, 6, rowSpan: 1, colSpan: 2);
            layout.Add(m_playersFaction1List, MyAlignH.Left, MyAlignV.Top, 4, 1, rowSpan: 3, colSpan: 4);

            layout.Add(m_leaderFaction1Button, MyAlignH.Left, MyAlignV.Center, 5, 5);
            layout.Add(pointsFaction1Label, MyAlignH.Left, MyAlignV.Center, 5, 6);
            layout.Add(m_points1Textbox, MyAlignH.Left, MyAlignV.Center, 5, 7);

            layout.Add(m_kickFaction1Button, MyAlignH.Left, MyAlignV.Center, 6, 5); 
            layout.Add(readyFaction1Label, MyAlignH.Left, MyAlignV.Center, 6, 6);
            layout.Add(m_readyFaction1Checkbox, MyAlignH.Right, MyAlignV.Center, 6, 7);

            layout.Add(m_faction2ComboBox, MyAlignH.Left, MyAlignV.Center, 8, 1, rowSpan: 1, colSpan: 2);
            layout.Add(m_playersFaction2Label, MyAlignH.Left, MyAlignV.Center, 8, 3);
            layout.Add(m_joinFaction2Button, MyAlignH.Left, MyAlignV.Center, 9, 5);*/
            layout.AddWithSize(m_connectedPlayers, MyAlignH.Left, MyAlignV.Top, 1, 4, rowSpan: 2, colSpan: 2);
            /*layout.Add(m_planAttackFaction2Button, MyAlignH.Left, MyAlignV.Center, 9, 6, rowSpan: 1, colSpan: 2);

            layout.Add(m_leaderFaction2Button, MyAlignH.Left, MyAlignV.Center, 10, 5);
            layout.Add(pointsFaction2Label, MyAlignH.Left, MyAlignV.Center, 10, 6);
            layout.Add(m_points2Textbox, MyAlignH.Left, MyAlignV.Center, 10, 7);*/
            /*layout.Add(readyFaction2Label, MyAlignH.Left, MyAlignV.Center, 11, 6);
            layout.Add(m_readyFaction2Checkbox, MyAlignH.Right, MyAlignV.Center, 11, 7);*/


            layout.AddWithSize(m_kickPlayerButton, MyAlignH.Left, MyAlignV.Center, 2, 5);
            layout.AddWithSize(m_timeoutLabel, MyAlignH.Left, MyAlignV.Center, 3, 4);
            layout.AddWithSize(m_timeoutCombo, MyAlignH.Left, MyAlignV.Center, 3, 5);
            
            layout.AddWithSize(m_chatControl, MyAlignH.Left, MyAlignV.Top, 5, 1, rowSpan: 1, colSpan: 5);

            layout.AddWithSize(m_chatTextbox, MyAlignH.Left, MyAlignV.Top, 6, 1, rowSpan: 1, colSpan: 4);
            layout.AddWithSize(m_sendChatButton, MyAlignH.Right, MyAlignV.Top, 6, 5);

            layout.AddWithSize(m_startButton, MyAlignH.Left, MyAlignV.Top, 7, 2);
        }

        void ChatTextbox_EnterPressed(MyGuiControlTextbox textBox)
        {
            SendMessageFromChatTextBox();
        }

        private void SendMessageFromChatTextBox()
        {
            m_chatTextbox.GetText(m_editBoxStringBuilder.Clear());
            string message = m_editBoxStringBuilder.ToString();

            SendChatMessage(message);

            m_chatTextbox.SetText(m_editBoxStringBuilder.Clear());
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenMpScenario";
        }

        public override bool Update(bool hasFocus)
        {
            m_update = true;
            UpdateControls();
            m_update = false;

            return base.Update(hasFocus);
        }

        protected virtual void OnStartClicked(MyGuiControlButton sender)
        {

        }

                    
        protected override void OnClosed()
        {
            MySyncScenario.PlayerReadyToStartScenario -= MySyncScenario_PlayerReady;
            MySyncScenario.TimeoutReceived -= MySyncScenario_SetTimeout;

            m_readyPlayers.Clear();
            base.OnClosed();
            if (Cancelled)
                MyGuiScreenMainMenu.ReturnToMainMenu();
        }

        public void MySyncScenario_PlayerReady(ulong Id)
        {
            m_readyPlayers.Add(Id);
        }

        private void OnSendChatClicked(MyGuiControlButton sender)
        {
            SendMessageFromChatTextBox();
        }

        private void SendChatMessage(string message)
        {
            bool send = true;
            MyAPIUtilities.Static.EnterMessage(message, ref send);
            if (send)
            {
                if (MyMultiplayer.Static != null)
                    MyMultiplayer.Static.SendChatMessage(message);
            }
        }

        /*private void OnKick1Clicked(MyGuiControlButton sender)
        {
            MyPlayer selectedRowPlayer = m_playersFaction1List.SelectedRow != null ? m_playersFaction1List.SelectedRow.UserData as MyPlayer : null;
            if (selectedRowPlayer == null || selectedRowPlayer.Identity.IdentityId == MySession.LocalPlayerId)
                return;

            MyMultiplayer.Static.KickClient(selectedRowPlayer.Id.SteamId);
        }*/

        protected bool CanKick()
        {
            if (!Sync.IsServer)
                return false;
            MyPlayer selectedRowPlayer = m_connectedPlayers.SelectedRow != null ? m_connectedPlayers.SelectedRow.UserData as MyPlayer : null;
            if (selectedRowPlayer == null || selectedRowPlayer.Identity.IdentityId == MySession.LocalPlayerId)
                return false;
            return true;
        }
        private void OnKick2Clicked(MyGuiControlButton sender)
        {
            MyPlayer selectedRowPlayer = m_connectedPlayers.SelectedRow != null ? m_connectedPlayers.SelectedRow.UserData as MyPlayer : null;
            if (selectedRowPlayer == null || selectedRowPlayer.Identity.IdentityId == MySession.LocalPlayerId)
                return;

            MyMultiplayer.Static.KickClient(selectedRowPlayer.Id.SteamId);
        }


        private void OnTimeoutSelected()
        {
            MyScenarioSystem.LoadTimeout = 60*(int)m_timeoutCombo.GetSelectedKey();
            if (Sync.IsServer)
                MySyncScenario.SetTimeout((int)m_timeoutCombo.GetSelectedIndex());//for GUI display only, no logic on clients
        }
        public void MySyncScenario_SetTimeout(int index)
        {
            m_timeoutCombo.SelectItemByIndex(index);
        }


        private void UpdateControls()
        {
            if (MyMultiplayer.Static == null || MySession.Static == null)
                return;

            //IMyFaction playerFaction = MySession.Static.Factions.TryGetPlayerFaction(MySession.LocalPlayerId);
            UpdatePlayerList(m_connectedPlayers);
            //if (m_hostnameLabel.Text == null || m_hostnameLabel.Text.Length == 0)
            //    m_hostnameLabel.Text = MyMultiplayer.Static.HostName;

            /*m_kickFaction1Button.Enabled = !faction1Ready && Sync.IsServer && m_playersFaction1List.SelectedRow != null 
                && (m_playersFaction1List.SelectedRow.UserData as MyPlayer).Identity.IdentityId != MySession.LocalPlayerId;

            m_kickFaction2Button.Enabled = !faction2Ready && Sync.IsServer && m_playersFaction2List.SelectedRow != null 
                && (m_playersFaction2List.SelectedRow.UserData as MyPlayer).Identity.IdentityId != MySession.LocalPlayerId;*/

            /*if (Sync.IsServer)
            {
                UpdateStartGameFailState(timeLimit, maxPoints1, maxPoints2, points1, points2, faction1Ready, faction2Ready);
                m_startBattleButton.Enabled = m_startFailState == MyBattleStartFailState.None;
            }
            else
                m_startButton.Enabled = false;*/
        }

        private static void UpdatePlayerList(MyGuiControlTable table)
        {
            MyPlayer selectedRowPlayer = table.SelectedRow != null ? table.SelectedRow.UserData as MyPlayer : null;

            table.Clear();

            var onlinePlayers = Sync.Players.GetOnlinePlayers();
            foreach (var player in onlinePlayers)
            {
                var name = player.DisplayName;
                var row = new MyGuiControlTable.Row(player);
                row.AddCell(new MyGuiControlTable.Cell(text: name));
                if (Sync.ServerId == player.Id.SteamId)
                    row.AddCell(new MyGuiControlTable.Cell(text: "SERVER"));
                else
                {
                    if (m_readyPlayers.Contains(player.Id.SteamId))
                        row.AddCell(new MyGuiControlTable.Cell(text: "ready"));
                    else
                        row.AddCell(new MyGuiControlTable.Cell(text: ""));
                }
                table.Add(row);

                if (player == selectedRowPlayer)
                    table.SelectedRow = row;
            }
        }

    }

}
