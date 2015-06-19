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
        public static MyGuiScreenScenarioMpBase Static;

        MyGuiControlMultilineText m_descriptionBox;
        protected MyGuiControlButton m_kickPlayerButton;
        protected MyGuiControlTable m_connectedPlayers;
        MyGuiControlLabel m_timeoutLabel;
        public MyGuiControlCombobox TimeoutCombo {get;  protected set;}

        MyGuiControlLabel m_canJoinRunningLabel;
        protected MyGuiControlCheckbox m_canJoinRunning;

        protected MyHudControlChat m_chatControl;
        protected MyGuiControlTextbox m_chatTextbox;
        protected MyGuiControlButton m_sendChatButton;
        
        protected MyGuiControlButton m_startButton;

        private bool m_update;

        private StringBuilder m_editBoxStringBuilder = new StringBuilder();

        //private long m_battleFaction1Id;
        //private long m_battleFaction2Id;

        public string Briefing
        {
            set { m_descriptionBox.Text = new StringBuilder(value);
            //m_descriptionBox.RefreshText();
            }
        }

        protected static HashSet<ulong> m_readyPlayers = new HashSet<ulong>();


        public MyGuiScreenScenarioMpBase()
            : base(position: new Vector2(0.5f, 0.5f), backgroundColor: MyGuiConstants.SCREEN_BACKGROUND_COLOR, size: new Vector2(1620f, 1125f) / MyGuiConstants.GUI_OPTIMAL_SIZE)
        {
            RecreateControls(true);
            CanHideOthers = false;
            MySyncScenario.PlayerReadyToStartScenario += MySyncScenario_PlayerReady;
            MySyncScenario.TimeoutReceived += MySyncScenario_SetTimeout;
            MySyncScenario.CanJoinRunningReceived += MySyncScenario_SetCanJoinRunning;
            m_canJoinRunning.IsCheckedChanged += OnJoinRunningChecked;
            Static = this;
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);
            var layout = new MyLayoutTable(this);
            layout.SetColumnWidthsNormalized(50, 300, 300, 300, 300, 300, 50);
            layout.SetRowHeightsNormalized(50, 450, 70, 70, 70, 400, 70, 70, 50);

            //BRIEFING:
            MyGuiControlParent briefing = new MyGuiControlParent();
            var briefingScrollableArea = new MyGuiControlScrollablePanel(
                scrolledControl: briefing)
            {
                Name = "BriefingScrollableArea",
                ScrollbarVEnabled = true,
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                BackgroundTexture = MyGuiConstants.TEXTURE_SCROLLABLE_LIST,
                ScrolledAreaPadding = new MyGuiBorderThickness(0.005f),
            };
            layout.AddWithSize(briefingScrollableArea, MyAlignH.Left, MyAlignV.Top, 1, 1, rowSpan: 4, colSpan: 3);
            //inside scrollable area:
            m_descriptionBox = new MyGuiControlMultilineText(
                position: new Vector2(0.0f, 0.0f),
                size: new Vector2(1f, 1f),
                textBoxAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                selectable: false);
            briefing.Controls.Add(m_descriptionBox);

            m_connectedPlayers = new MyGuiControlTable();
            m_connectedPlayers.Size = new Vector2(490f, 150f) / MyGuiConstants.GUI_OPTIMAL_SIZE;
            m_connectedPlayers.VisibleRowsCount = 8;
            m_connectedPlayers.ColumnsCount = 2;
            m_connectedPlayers.SetCustomColumnWidths(new float[] { 0.7f, 0.3f });
            m_connectedPlayers.SetColumnName(0, MyTexts.Get(MySpaceTexts.GuiScenarioPlayerName));
            m_connectedPlayers.SetColumnName(1, MyTexts.Get(MySpaceTexts.GuiScenarioPlayerStatus));

            m_kickPlayerButton = new MyGuiControlButton(text: MyTexts.Get(MySpaceTexts.Kick), visualStyle: MyGuiControlButtonStyleEnum.Rectangular, highlightType: MyGuiControlHighlightType.WHEN_ACTIVE,
                size: new Vector2(190f, 48f) / MyGuiConstants.GUI_OPTIMAL_SIZE, onButtonClick: OnKick2Clicked);
            m_kickPlayerButton.Enabled = CanKick();

            m_timeoutLabel = new MyGuiControlLabel(text: MyTexts.GetString(MySpaceTexts.GuiScenarioTimeout), originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);

            TimeoutCombo = new MyGuiControlCombobox();
            TimeoutCombo.ItemSelected += OnTimeoutSelected;
            TimeoutCombo.AddItem(3, MyTexts.Get(MySpaceTexts.GuiScenarioTimeout3min));
            TimeoutCombo.AddItem(5, MyTexts.Get(MySpaceTexts.GuiScenarioTimeout5min));
            TimeoutCombo.AddItem(10, MyTexts.Get(MySpaceTexts.GuiScenarioTimeout10min));
            TimeoutCombo.AddItem(-1, MyTexts.Get(MySpaceTexts.GuiScenarioTimeoutUnlimited));
            TimeoutCombo.SelectItemByIndex(0);
            TimeoutCombo.Enabled = Sync.IsServer;

            m_canJoinRunningLabel = new MyGuiControlLabel(text: MyTexts.GetString(MySpaceTexts.ScenarioSettings_CanJoinRunningShort), originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            m_canJoinRunning = new MyGuiControlCheckbox();

            m_canJoinRunningLabel.Enabled = false;
            m_canJoinRunning.Enabled = false;

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


            layout.AddWithSize(m_connectedPlayers, MyAlignH.Left, MyAlignV.Top, 1, 4, rowSpan: 2, colSpan: 2);

            layout.AddWithSize(m_kickPlayerButton, MyAlignH.Left, MyAlignV.Center, 2, 5);
            layout.AddWithSize(m_timeoutLabel, MyAlignH.Left, MyAlignV.Center, 3, 4);
            layout.AddWithSize(TimeoutCombo, MyAlignH.Left, MyAlignV.Center, 3, 5);

            layout.AddWithSize(m_canJoinRunningLabel, MyAlignH.Left, MyAlignV.Center, 4, 4);
            layout.AddWithSize(m_canJoinRunning, MyAlignH.Right, MyAlignV.Center, 4, 5);
            
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
            MySyncScenario.CanJoinRunningReceived -= MySyncScenario_SetCanJoinRunning;
            m_canJoinRunning.IsCheckedChanged -= OnJoinRunningChecked;

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
            MyScenarioSystem.LoadTimeout = 60*(int)TimeoutCombo.GetSelectedKey();
            if (Sync.IsServer)
                MySyncScenario.SetTimeout((int)TimeoutCombo.GetSelectedIndex());//for GUI display only, no logic on clients
        }
        public void MySyncScenario_SetTimeout(int index)
        {
            TimeoutCombo.SelectItemByIndex(index);
        }


        private void OnJoinRunningChecked(MyGuiControlCheckbox source)
        {
            Debug.Assert(Sync.IsServer);
            MySession.Static.Settings.CanJoinRunning = source.IsChecked;
            MySyncScenario.SetJoinRunning(source.IsChecked);//for GUI display only, no logic on clients
        }
        public void MySyncScenario_SetCanJoinRunning(bool canJoin)
        {
            m_canJoinRunning.IsChecked = canJoin;
        }


        private void UpdateControls()
        {
            if (MyMultiplayer.Static == null || MySession.Static == null)
                return;

            m_kickPlayerButton.Enabled = CanKick();
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
