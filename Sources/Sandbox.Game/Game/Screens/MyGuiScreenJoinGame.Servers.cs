#region Using

using Sandbox.Common;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game.Localization;
using Sandbox.Graphics.GUI;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using VRage;
using VRage;
using VRage.Input;
using VRage.Utils;


#endregion

namespace Sandbox.Game.Gui
{
    partial class MyGuiScreenJoinGame : MyGuiScreenBase
    {

        #region Fields

        List<GameServerItem> m_servers = new List<GameServerItem>();
        MyGuiControlTabPage m_serversPage;

        #endregion

        #region Constructor

        void InitServersPage()
        {
            InitServersTable();

            m_joinButton.ButtonClicked += OnJoinServer;
            m_refreshButton.ButtonClicked += OnRefreshServersClick;
            m_showOnlyCompatibleGames.IsCheckedChanged = OnServerCheckboxCheckChanged;
            m_showOnlyWithSameMods.IsCheckedChanged = OnServerCheckboxCheckChanged;
            m_showOnlyFriends.IsCheckedChanged = OnServerCheckboxCheckChanged;
            m_allowedGroups.IsCheckedChanged = OnServerCheckboxCheckChanged;

            m_searchChangedFunc += RefreshServerGameList;

            m_serversPage = m_selectedPage;
            m_serversPage.SetToolTip(MyTexts.GetString(MySpaceTexts.JoinGame_TabTooltip_Servers));

            RefreshServerGameList();
        }

        void CloseServersPage()
        {
            CloseRequest();

            m_searchChangedFunc -= RefreshServerGameList;
        }

        void InitServersTable()
        {
            // World name, User name, Player count
            m_gamesTable.ColumnsCount = 6;
            m_gamesTable.ItemSelected += OnTableItemSelected;
            m_gamesTable.ItemSelected += OnServerTableItemSelected;
            m_gamesTable.ItemDoubleClicked += OnServerTableItemDoubleClick;
            m_gamesTable.SetCustomColumnWidths(new float[] { 0.30f, 0.19f, 0.31f, 0.08f, 0.05f, 0.07f });
            m_gamesTable.SetColumnComparison(0, TextComparisonServers);
            m_gamesTable.SetColumnComparison(1, TextComparisonServers);
            m_gamesTable.SetColumnComparison(2, TextComparisonServers);
            m_gamesTable.SetColumnComparison(3, PlayerCountComparisonServers);
            m_gamesTable.SetColumnComparison(4, PingComparison);
            m_gamesTable.SetColumnComparison(5, ModsComparisonServers);
            //m_gamesTable.SetColumnAlign(1, MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER);
            m_gamesTable.SetColumnAlign(3, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            m_gamesTable.SetColumnAlign(4, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            m_gamesTable.SetColumnAlign(5, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            //m_gamesTable.SetHeaderColumnAlign(1, MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER);
            m_gamesTable.SetHeaderColumnAlign(3, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            m_gamesTable.SetHeaderColumnAlign(4, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            m_gamesTable.SetHeaderColumnAlign(5, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);

            m_gamesTable.SortByColumn(4);
        }

        #endregion

        #region Event handling

        private void OnJoinServer(MyGuiControlButton obj)
        {
            JoinSelectedServer();
        }

        private void OnServerTableItemSelected(MyGuiControlTable sender, MyGuiControlTable.EventArgs eventArgs)
        {
            if (sender.SelectedRow == null)
                return;

            var server = (GameServerItem)sender.SelectedRow.UserData;
            if (server == null)
                return;

            IPEndPoint endpoint = server.NetAdr;
            if (endpoint == null)
                return;

            var cell = sender.SelectedRow.GetCell(5);
            if (cell == null)
                return;

            var toolTip = cell.ToolTip;
            if (toolTip == null)
                return;

            if (eventArgs.MouseButton == MyMouseButtonsEnum.Right)
            {
                m_contextMenu.CreateNewContextMenu();
                var action = m_selectedPage == m_favoritesPage ? ContextMenuFavoriteAction.Remove : ContextMenuFavoriteAction.Add;

                var itemText = MySpaceTexts.JoinGame_Favorites_Remove;
                if (action == ContextMenuFavoriteAction.Add)
                {
                    itemText = MySpaceTexts.JoinGame_Favorites_Add;
                }

                m_contextMenu.AddItem(MyTexts.Get(itemText), userData: new ContextMenuFavoriteActionItem() { Server = server, Action = action });
                m_contextMenu.Activate();
            }
            else
            {
                m_contextMenu.Deactivate();
            }

            MySandboxGame.Services.SteamService.SteamAPI.GetServerRules(endpoint.Address.ToIPv4NetworkOrder(), (ushort)endpoint.Port, delegate(Dictionary<string, string> rules)
            {
                if (rules == null)
                    return;

                if (rules.Count == 0)
                    return;

                if (toolTip == null)
                    return;

                if (toolTip.ToolTips == null)
                    return;

                if (toolTip.ToolTips.Count == 0)
                    return;
                try
                {
                    int modCount = 0;
                    int.TryParse(rules[MyMultiplayer.ModCountTag], out modCount);

                    if (modCount > 0)
                    {
                        if (toolTip.ToolTips[0] == null)
                            return;

                        if (toolTip.ToolTips[0].Text == null)
                            return;

                        var text = toolTip.ToolTips[0].Text.Clear();

                        int displayedModsMax = 15;
                        int lastMod = Math.Min(displayedModsMax, modCount - 1);

                        for (int i = 0; i < modCount; ++i)
                        {
                            if (displayedModsMax-- <= 0)
                            {
                                text.Append("...");
                                break;
                            }

                            if (lastMod-- <= 0)
                                text.Append(rules[MyMultiplayer.ModItemTag + i.ToString()]);
                            else
                                text.AppendLine(rules[MyMultiplayer.ModItemTag + i.ToString()]);
                        }
                        toolTip.RecalculateSize();
                    }
                }
                catch (System.Collections.Generic.KeyNotFoundException)
                {
                    MySandboxGame.Log.WriteLine(string.Format("Server returned corrupted rules: Address = {0}, Name = '{1}'", server.NetAdr, server.Name));
                }

            },
            delegate()
            {
                MySandboxGame.Log.WriteLine(string.Format("Failed to get server rules: Address = {0}, Name = '{1}'", server.NetAdr, server.Name));

                if (toolTip == null)
                    return;

                if (toolTip.ToolTips == null)
                    return;

                if (toolTip.ToolTips.Count == 0)
                    return;

                if (toolTip.ToolTips[0] == null)
                    return;

                if (toolTip.ToolTips[0].Text == null)
                    return;

                var text = toolTip.ToolTips[0].Text.Clear();

                text.Append(MyTexts.Get(MySpaceTexts.JoinGame_BadModsListResponse));
                toolTip.RecalculateSize();
            });
        }

        private void OnServerTableItemDoubleClick(MyGuiControlTable sender, MyGuiControlTable.EventArgs eventArgs)
        {
            JoinSelectedServer();
        }

        private void JoinSelectedServer()
        {
            var selectedRow = m_gamesTable.SelectedRow;
            if (selectedRow == null)
                return;

            MyJoinGameHelper.JoinGame((GameServerItem)selectedRow.UserData);
        }

        private void OnRefreshServersClick(MyGuiControlButton obj)
        {
            RefreshServerGameList();
        }

        private void OnServerCheckboxCheckChanged(MyGuiControlCheckbox checkbox)
        {
            RefreshServerGameList();
        }

        #endregion

        #region Async Loading

        void OnDedicatedServerListResponded(int server)
        {
            GameServerItem serverItem = SteamAPI.Instance.GetDedicatedServerDetails(server);

            AddServerItem(serverItem, delegate()
            {
                m_serversPage.Text = new StringBuilder().Append(MyTexts.Get(MySpaceTexts.JoinGame_TabTitle_Servers).ToString()).Append(" (").Append(m_gamesTable.RowsCount).Append(")");
            });
        }

        void OnDedicatedServersCompleteResponse(MatchMakingServerResponseEnum response)
        {
            CloseRequest();
        }

        void CloseRequest()
        {            
            SteamAPI.Instance.OnDedicatedServerListResponded -= OnDedicatedServerListResponded;
            SteamAPI.Instance.OnDedicatedServersCompleteResponse -= OnDedicatedServersCompleteResponse;
            SteamAPI.Instance.CancelInternetServersRequest();
        }

        private void AddServerHeaders()
        {
            m_gamesTable.SetColumnName(0, MyTexts.Get(MySpaceTexts.JoinGame_ColumnTitle_World));
            m_gamesTable.SetColumnName(1, MyTexts.Get(MySpaceTexts.JoinGame_ColumnTitle_GameMode));
            m_gamesTable.SetColumnName(2, MyTexts.Get(MySpaceTexts.JoinGame_ColumnTitle_Server));
            m_gamesTable.SetColumnName(3, MyTexts.Get(MySpaceTexts.JoinGame_ColumnTitle_Players));
            m_gamesTable.SetColumnName(4, MyTexts.Get(MySpaceTexts.JoinGame_ColumnTitle_Ping));
            m_gamesTable.SetColumnName(5, MyTexts.Get(MySpaceTexts.JoinGame_ColumnTitle_Mods));
        }

        private void RefreshServerGameList()
        {
            CloseRequest();

            m_gamesTable.Clear();
            AddServerHeaders();

            m_textCache.Clear();
            m_gameTypeText.Clear();
            m_gameTypeToolTip.Clear();
            m_servers.Clear();
            m_serversPage.TextEnum = MySpaceTexts.JoinGame_TabTitle_Servers;

            String filterOps = string.Format("gamedir:{0};secure:1", MyPerGameSettings.SteamGameServerGameDir);
            //if (m_showOnlyWithSameMods.IsChecked) filterOps = filterOps + ";gametagsand:datahash" + MyDataIntegrityChecker.GetHashBase64(); // nonsense, datahash is inconsistent

            //if (!string.IsNullOrWhiteSpace(m_blockSearch.Text)) filterOps = filterOps + ";gametagsand:" + m_blockSearch.Text.Trim().Replace(":", "a58").Replace(";", "a59"); // nonsense, servername is not in tags

            MySandboxGame.Log.WriteLine("Requesting dedicated servers, filterOps: "+filterOps);

            SteamAPI.Instance.OnDedicatedServerListResponded += OnDedicatedServerListResponded;
            SteamAPI.Instance.OnDedicatedServersCompleteResponse += OnDedicatedServersCompleteResponse;
            SteamAPI.Instance.RequestInternetServerList(filterOps);

            m_gamesTable.SelectedRowIndex = null;
        }

        bool AddServerItem(GameServerItem server, Action onAddedServerItem, bool isFiltered = false)
        {
            if (m_allowedGroups.IsChecked && !SteamAPI.Instance.Friends.IsUserInGroup(server.GetGameTagByPrefixUlong("groupId")))
                return false;

            if (server.AppID != MySteam.AppId)
                return false;

            if (!isFiltered && !string.IsNullOrWhiteSpace(m_blockSearch.Text)) // this must be here for filtering LAN games
            {
                if (!server.Name.ToLower().Contains(m_blockSearch.Text.ToLower()))
                    return false;
            }

            string sessionName = server.Map;
            int appVersion = server.ServerVersion;
            m_gameTypeText.Clear();
            m_gameTypeToolTip.Clear();

            // Skip world without name (not fully initialized)
            if (string.IsNullOrEmpty(sessionName))
                return false;

            // Show only same app versions
            if (m_showOnlyCompatibleGames.IsChecked && appVersion != MyFinalBuildConstants.APP_VERSION)
                return false;
            
            // Show only if the game data match
            string remoteHash = server.GetGameTagByPrefix("datahash");
            if (m_showOnlyWithSameMods.IsChecked && MyFakes.ENABLE_MP_DATA_HASHES && remoteHash != "" && remoteHash != MyDataIntegrityChecker.GetHashBase64())
                return false;

            var gamemodeSB = new StringBuilder();
            var gamemodeToolTipSB = new StringBuilder();

            string gamemode = server.GetGameTagByPrefix("gamemode");
            if (gamemode == "C")
            {
                gamemodeSB.Append(MyTexts.Get(MySpaceTexts.WorldSettings_GameModeCreative));
                gamemodeToolTipSB.AppendStringBuilder(MyTexts.Get(MySpaceTexts.WorldSettings_GameModeCreative));
            }
            else if (gamemode == "B")
            {
                IPEndPoint endpoint = server.NetAdr;
                if (endpoint == null)
                    return false;

                // Started battle write key value "BattleCanBeJoinedTag" "0" to server which can be accessed asynchronously from rules.
                MySandboxGame.Services.SteamService.SteamAPI.GetServerRules(endpoint.Address.ToIPv4NetworkOrder(), (ushort)endpoint.Port, delegate(Dictionary<string, string> rules)
                {
                    if (rules == null)
                        return;

                    bool canBeJoined = true;
                    string strCanBeJoined;
                    if (rules.TryGetValue(MyMultiplayer.BattleCanBeJoinedTag, out strCanBeJoined))
                    {
                        canBeJoined = strCanBeJoined != 0.ToString();
                    }

                    if (canBeJoined)
                    {
                        gamemodeSB.Append(MyTexts.Get(MySpaceTexts.WorldSettings_Battle));
                        gamemodeToolTipSB.AppendStringBuilder(MyTexts.Get(MySpaceTexts.WorldSettings_Battle));

                        AddServerItem(server, sessionName, gamemodeSB, gamemodeToolTipSB);

                        if (onAddedServerItem != null)
                            onAddedServerItem();
                    }
                },
                delegate() { });

                return false;
            }
            else if(!string.IsNullOrWhiteSpace(gamemode))
            {
                var multipliers = gamemode.Substring(1);
                var split = multipliers.Split('-');
                //TODO: refactor
                if (split.Length == 3 && server.AppID == 244850)
                {
                    gamemodeSB.Append(MyTexts.Get(MySpaceTexts.WorldSettings_GameModeSurvival)).Append(" ").Append(multipliers);
                    gamemodeToolTipSB.AppendFormat(MyTexts.Get(MySpaceTexts.JoinGame_GameTypeToolTip_MultipliersFormat).ToString(), split[0], split[1], split[2]);
                }
                else
                {
                    gamemodeSB.Append(MyTexts.Get(MySpaceTexts.WorldSettings_GameModeSurvival));
                    gamemodeToolTipSB.AppendStringBuilder(MyTexts.Get(MySpaceTexts.WorldSettings_GameModeSurvival));
                }
            }

            AddServerItem(server, sessionName, gamemodeSB, gamemodeToolTipSB);

            if (onAddedServerItem != null)
                onAddedServerItem();

            return true;
        }

        private void AddServerItem(GameServerItem server, string sessionName, StringBuilder gamemodeSB, StringBuilder gamemodeToolTipSB)
        {
            ulong modCount = server.GetGameTagByPrefixUlong(MyMultiplayer.ModCountTag);

            string limit = server.MaxPlayers.ToString();
            StringBuilder userCount = new StringBuilder(server.Players + "/" + limit);

            var viewDistance = server.GetGameTagByPrefix(MyMultiplayer.ViewDistanceTag);
            //TODO: refactor
            if (!String.IsNullOrEmpty(viewDistance) && server.AppID == 244850)
            {
                gamemodeToolTipSB.AppendLine();
                gamemodeToolTipSB.AppendFormat(MyTexts.Get(MySpaceTexts.JoinGame_GameTypeToolTip_ViewDistance).ToString(), viewDistance);
            }

            var row = new MyGuiControlTable.Row(server);
            row.AddCell(new MyGuiControlTable.Cell(text: m_textCache.Clear().Append(sessionName), userData: server.SteamID, toolTip: m_textCache.ToString()));
            row.AddCell(new MyGuiControlTable.Cell(text: gamemodeSB, toolTip: gamemodeToolTipSB.ToString()));
            row.AddCell(new MyGuiControlTable.Cell(text: m_textCache.Clear().Append(server.Name), toolTip: m_gameTypeToolTip.Clear().AppendLine(server.Name).Append(server.NetAdr.ToString()).ToString()));
            row.AddCell(new MyGuiControlTable.Cell(text: userCount, toolTip: userCount.ToString()));
            row.AddCell(new MyGuiControlTable.Cell(text: m_textCache.Clear().Append(server.Ping), toolTip: m_textCache.ToString()));
            row.AddCell(new MyGuiControlTable.Cell(text: m_textCache.Clear().Append(modCount == 0 ? "---" : modCount.ToString()), toolTip: MyTexts.GetString(MySpaceTexts.JoinGame_SelectServerToShowModList)));
            m_gamesTable.Add(row);

            var selectedRow = m_gamesTable.SelectedRow;
            m_gamesTable.Sort(false);

            m_gamesTable.SelectedRowIndex = m_gamesTable.FindRow(selectedRow);
        }

        #endregion

    }
}
