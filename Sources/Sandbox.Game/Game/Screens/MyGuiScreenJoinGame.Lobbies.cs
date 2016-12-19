#region Using

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ParallelTasks;
using Sandbox.Common;
using Sandbox.Graphics.GUI;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using SteamSDK;

using VRageMath;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.World;
using Sandbox.Engine.Multiplayer;
using VRage;
using VRage.Utils;
using System.Diagnostics;
using Sandbox.Game.Screens.Helpers;
using VRage.Utils;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Localization;
using VRage;
using VRage.Game;
using VRage.Library.Utils;

#endregion

namespace Sandbox.Game.Gui
{
    partial class MyGuiScreenJoinGame : MyGuiScreenBase
    {

        #region Fields

        List<Lobby> m_lobbies = new List<Lobby>();
        MyGuiControlTabPage m_lobbyPage;

        #endregion

        #region Constructor

        void InitLobbyPage()
        {
            InitLobbyTable();

            m_joinButton.ButtonClicked += OnJoinLobby;
            m_refreshButton.ButtonClicked += OnRefreshLobbiesClick;
            m_showOnlyCompatibleGames.IsCheckedChanged = OnShowCompatibleCheckChanged;
            m_showOnlyWithSameMods.IsCheckedChanged = OnShowCompatibleCheckChanged;
            m_showOnlyFriends.IsCheckedChanged += OnShowOnlyFriendsCheckChanged;

            m_searchChangedFunc += LoadPublicLobbies;

            m_lobbyPage = m_selectedPage;
            m_lobbyPage.SetToolTip(MyTexts.GetString(MyCommonTexts.JoinGame_TabTooltip_Lobbies));

            LoadPublicLobbies();
        }

        void CloseLobbyPage()
        {
            m_searchChangedFunc -= LoadPublicLobbies;
        }

        void InitLobbyTable()
        {
            // World name, User name, Player count
            m_gamesTable.ColumnsCount = MyFakes.ENABLE_JOIN_SCREEN_REMAINING_TIME ? 6 : 5;
            m_gamesTable.ItemSelected += OnTableItemSelected;
            m_gamesTable.ItemDoubleClicked += OnTableItemDoubleClick;
            if (MyFakes.ENABLE_JOIN_SCREEN_REMAINING_TIME)
                m_gamesTable.SetCustomColumnWidths(new float[] { 0.30f, 0.18f, 0.20f, 0.16f, 0.08f, 0.07f });
            else
                m_gamesTable.SetCustomColumnWidths(new float[] { 0.40f, 0.19f, 0.22f, 0.08f, 0.07f });

            int colCounter = 0;
            m_gamesTable.SetColumnComparison(colCounter++, TextComparisonLobbies);
            m_gamesTable.SetColumnComparison(colCounter++, TextComparisonLobbies);
            m_gamesTable.SetColumnComparison(colCounter++, TextComparisonLobbies);

            if (MyFakes.ENABLE_JOIN_SCREEN_REMAINING_TIME)
            {
                m_gamesTable.SetColumnComparison(colCounter, TextComparisonLobbies);
                m_gamesTable.SetColumnAlign(colCounter, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
                m_gamesTable.SetHeaderColumnAlign(colCounter, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
                ++colCounter;
            }

            m_gamesTable.SetColumnComparison(colCounter, PlayerCountComparisonLobbies);
            m_gamesTable.SetColumnAlign(colCounter, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            m_gamesTable.SetHeaderColumnAlign(colCounter, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            ++colCounter;

            m_gamesTable.SetColumnComparison(colCounter, ModsComparisonLobbies);
            m_gamesTable.SetColumnAlign(colCounter, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            m_gamesTable.SetHeaderColumnAlign(colCounter, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
            ++colCounter;
        }

        #endregion

        #region Event handling

        private void OnJoinLobby(MyGuiControlButton obj)
        {
            JoinSelectedLobby();
        }

        private void OnTableItemSelected(MyGuiControlTable sender, MyGuiControlTable.EventArgs eventArgs)
        {
            if (m_gamesTable.SelectedRow != null)
            {
                m_joinButton.Enabled = true;
            }
            else
            {
                m_joinButton.Enabled = false;
            }
        }

        private void OnTableItemDoubleClick(MyGuiControlTable sender, MyGuiControlTable.EventArgs eventArgs)
        {
            JoinSelectedLobby();
        }

        private void JoinSelectedLobby()
        {
            var selectedRow = m_gamesTable.SelectedRow;
            if (selectedRow == null)
                return;

            Lobby selectedLobby = (Lobby)selectedRow.UserData;

            MyJoinGameHelper.JoinGame(selectedLobby);
        }

        private void OnRefreshLobbiesClick(MyGuiControlButton obj)
        {
            LoadPublicLobbies();
        }

        private void OnShowCompatibleCheckChanged(MyGuiControlCheckbox checkbox)
        {
            LoadPublicLobbies();
        }

        private void OnShowOnlyFriendsCheckChanged(MyGuiControlCheckbox checkbox)
        {
            LoadPublicLobbies();
        }

        #endregion

        #region Async Loading


        private IMyAsyncResult beginAction()
        {
            return new LoadLobbyListResult(m_showOnlyCompatibleGames.IsChecked);
        }


        private void endPublicLobbiesAction(IMyAsyncResult result, MyGuiScreenProgressAsync screen)
        {
            var loadResult = (LoadLobbyListResult)result;
            m_lobbies.Clear();

            if (m_showOnlyFriends.IsChecked)
            {
                LobbySearch.AddFriendLobbies(m_lobbies);
            }
            else
            {
                LobbySearch.AddFriendLobbies(m_lobbies);
                LobbySearch.AddPublicLobbies(m_lobbies);
            }

            RefreshGameList();
            screen.CloseScreen();
        }

        void LoadPublicLobbies()
        {
            MyGuiSandbox.AddScreen(new MyGuiScreenProgressAsync(MyCommonTexts.LoadingPleaseWait, null, beginAction, endPublicLobbiesAction));
        }

        class LoadLobbyListResult : IMyAsyncResult
        {
            public bool IsCompleted { get; private set; }
            public Task Task
            {
                get;
                private set;
            }

            public LoadLobbyListResult(bool showOnlyCompatible)
            {
                MySandboxGame.Log.WriteLine("Requesting dedicated servers");

                if (showOnlyCompatible)
                    LobbySearch.AddRequestLobbyListNumericalFilter(MyMultiplayer.AppVersionTag, MyFinalBuildConstants.APP_VERSION, LobbyComparison.LobbyComparisonEqual);

                //var searchName = m_blockSearch.Text.Trim();
                //if (!string.IsNullOrWhiteSpace(searchName))
                //    LobbySearch.AddRequestLobbyListStringFilter(MyMultiplayer.WorldNameTag, searchName, LobbyComparison.LobbyComparisonEqual);

                MySandboxGame.Log.WriteLine("Requesting worlds, only compatible: " + showOnlyCompatible);
                LobbySearch.RequestLobbyList(LobbiesCompleted);
            }

            void LobbiesCompleted(Result result)
            {
                MySandboxGame.Log.WriteLine("Enumerate worlds, result: " + result.ToString());

                if (result != Result.OK)
                {
                    MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                            messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                            messageText: new StringBuilder("Cannot enumerate worlds, error code: " + result.ToString())));
                }

                IsCompleted = true;
            }
        }


        private void AddHeaders()
        {
            int colCounter = 0;
            m_gamesTable.SetColumnName(colCounter++, MyTexts.Get(MyCommonTexts.JoinGame_ColumnTitle_World));
            m_gamesTable.SetColumnName(colCounter++, MyTexts.Get(MyCommonTexts.JoinGame_ColumnTitle_GameMode));
            m_gamesTable.SetColumnName(colCounter++, MyTexts.Get(MyCommonTexts.JoinGame_ColumnTitle_Username));
            if (MyFakes.ENABLE_JOIN_SCREEN_REMAINING_TIME)
                m_gamesTable.SetColumnName(colCounter++, MyTexts.Get(MyCommonTexts.JoinGame_ColumnTitle_RemainingTime));
            m_gamesTable.SetColumnName(colCounter++, MyTexts.Get(MyCommonTexts.JoinGame_ColumnTitle_Players));
            m_gamesTable.SetColumnName(colCounter++, MyTexts.Get(MyCommonTexts.JoinGame_ColumnTitle_Mods));
        }

        private void RefreshGameList()
        {
            m_gamesTable.Clear();
            AddHeaders();

            m_textCache.Clear();
            m_gameTypeText.Clear();
            m_gameTypeToolTip.Clear();

            m_lobbyPage.Text = MyTexts.Get(MyCommonTexts.JoinGame_TabTitle_Lobbies);

            if (m_lobbies != null)
            {
                int shownGames = 0;
                for (int i = 0; i < m_lobbies.Count; ++i)
                {
                    var lobby = m_lobbies[i];
                    var row = new MyGuiControlTable.Row(lobby);

                    string sessionName = MyMultiplayerLobby.GetLobbyWorldName(lobby);
                    ulong sessionSize = MyMultiplayerLobby.GetLobbyWorldSize(lobby);
                    int appVersion = MyMultiplayerLobby.GetLobbyAppVersion(lobby);
                    int modCount = MyMultiplayerLobby.GetLobbyModCount(lobby);
                    string remainingTimeText = null;
                    float? remainingTimeSeconds = null;

                    var searchName = m_blockSearch.Text.Trim();
                    if (!string.IsNullOrWhiteSpace(searchName) && !sessionName.ToLower().Contains(searchName.ToLower()))
                        continue;

                    m_gameTypeText.Clear();
                    m_gameTypeToolTip.Clear();
                    //TODO: refactor - split to ME a SE versions
                    if (appVersion > 01022000 && MySteam.AppId == 244850)
                    {
                        var inventory = MyMultiplayerLobby.GetLobbyFloat(MyMultiplayer.InventoryMultiplierTag, lobby, 1);
                        var refinery = MyMultiplayerLobby.GetLobbyFloat(MyMultiplayer.RefineryMultiplierTag, lobby, 1);
                        var assembler = MyMultiplayerLobby.GetLobbyFloat(MyMultiplayer.AssemblerMultiplierTag, lobby, 1);

                        MyGameModeEnum gameMode = MyMultiplayerLobby.GetLobbyGameMode(lobby);
                        if (MyMultiplayerLobby.GetLobbyScenario(lobby))
                        {
                            m_gameTypeText.AppendStringBuilder(MyTexts.Get(MySpaceTexts.WorldSettings_GameScenario));
                            DateTime started = MyMultiplayerLobby.GetLobbyDateTime(MyMultiplayer.ScenarioStartTimeTag, lobby, DateTime.MinValue);
                            if (started > DateTime.MinValue)
                            {
                                TimeSpan timeRunning = DateTime.UtcNow - started;
                                var hrs = Math.Truncate(timeRunning.TotalHours);
                                int mins = (int)((timeRunning.TotalHours - hrs) * 60);
                                m_gameTypeText.Append(" ").Append(hrs).Append(":").Append(mins.ToString("D2"));
                            }
                            else
                                m_gameTypeText.Append(" Lobby");
                        }
                        else
                            switch (gameMode)
                            {
                                case MyGameModeEnum.Creative:
                                    m_gameTypeText.AppendStringBuilder(MyTexts.Get(MyCommonTexts.WorldSettings_GameModeCreative));
                                    break;
                                case MyGameModeEnum.Survival:
                                    m_gameTypeText.AppendStringBuilder(MyTexts.Get(MyCommonTexts.WorldSettings_GameModeSurvival));
                                    m_gameTypeText.Append(String.Format(" {0}-{1}-{2}", inventory, assembler, refinery));
                                    break;
                                default:
                                    Debug.Fail("Unknown game type");
                                    break;
                            }

                        m_gameTypeToolTip.AppendFormat(MyTexts.Get(MyCommonTexts.JoinGame_GameTypeToolTip_MultipliersFormat).ToString(), inventory, assembler, refinery);

                        var viewDistance = MyMultiplayerLobby.GetLobbyViewDistance(lobby);
                        m_gameTypeToolTip.AppendLine();
                        m_gameTypeToolTip.AppendFormat(MyTexts.Get(MyCommonTexts.JoinGame_GameTypeToolTip_ViewDistance).ToString(), viewDistance);
                    }
                    else
                    {
                        MyGameModeEnum gameMode = MyMultiplayerLobby.GetLobbyGameMode(lobby);

                        switch (gameMode)
                        {
                            case MyGameModeEnum.Creative:
                                m_gameTypeText.AppendStringBuilder(MyTexts.Get(MyCommonTexts.WorldSettings_GameModeCreative));
                                m_gameTypeToolTip.AppendStringBuilder(MyTexts.Get(MyCommonTexts.WorldSettings_GameModeCreative));
                                break;
                            case MyGameModeEnum.Survival:
                                m_gameTypeText.AppendStringBuilder(MyTexts.Get(MyCommonTexts.WorldSettings_GameModeSurvival));
                                m_gameTypeToolTip.AppendStringBuilder(MyTexts.Get(MyCommonTexts.WorldSettings_GameModeSurvival));                            
                                break;

                            default:
                                Debug.Fail("Unknown game type");
                                break;
                        }
                    }

                    // Skip world without name (not fully initialized)
                    if (string.IsNullOrEmpty(sessionName))
                        continue;

                    // Show only same app versions
                    if (m_showOnlyCompatibleGames.IsChecked && appVersion != MyFinalBuildConstants.APP_VERSION)
                        continue;

                    // Show only if the game data match
                    if (m_showOnlyWithSameMods.IsChecked && MyFakes.ENABLE_MP_DATA_HASHES && !MyMultiplayerLobby.HasSameData(lobby))
                        continue;

                    string owner = MyMultiplayerLobby.GetLobbyHostName(lobby);
                    string limit = lobby.MemberLimit.ToString();
                    string userCount = lobby.MemberCount + "/" + limit;

                    var modListToolTip = new StringBuilder();

                    int displayedModsMax = 15;
                    int lastMod = Math.Min(displayedModsMax, modCount - 1);
                    foreach (var mod in MyMultiplayerLobby.GetLobbyMods(lobby))
                    {
                        if (displayedModsMax-- <= 0)
                        {
                            modListToolTip.Append("...");
                            break;
                        }

                        if (lastMod-- <= 0)
                            modListToolTip.Append(mod.FriendlyName);
                        else
                            modListToolTip.AppendLine(mod.FriendlyName);
                    }

                    //row.AddCell(new MyGuiControlTable.Cell(text: m_textCache.Clear().Append(MyMultiplayerLobby.HasSameData(lobby) ? "" : "*")));
                    row.AddCell(new MyGuiControlTable.Cell(text: m_textCache.Clear().Append(sessionName), userData: lobby.LobbyId, toolTip: m_textCache.ToString()));
                    row.AddCell(new MyGuiControlTable.Cell(text: m_gameTypeText, toolTip: (m_gameTypeToolTip.Length > 0) ? m_gameTypeToolTip.ToString() : null));
                    row.AddCell(new MyGuiControlTable.Cell(text: m_textCache.Clear().Append(owner), toolTip: m_textCache.ToString()));
                    if (MyFakes.ENABLE_JOIN_SCREEN_REMAINING_TIME)
                    {
                        if (remainingTimeText != null)
                            row.AddCell(new MyGuiControlTable.Cell(text: m_textCache.Clear().Append(remainingTimeText)));
                        else if (remainingTimeSeconds != null)
                            row.AddCell(new CellRemainingTime(remainingTimeSeconds.Value));
                        else
                            row.AddCell(new MyGuiControlTable.Cell(text: m_textCache.Clear()));
                    }
                    row.AddCell(new MyGuiControlTable.Cell(text: new StringBuilder(userCount)));
                    row.AddCell(new MyGuiControlTable.Cell(text: m_textCache.Clear().Append(modCount == 0 ? "---" : modCount.ToString()), toolTip: modListToolTip.ToString()));
                    m_gamesTable.Add(row);
                    shownGames++;
                }

                m_lobbyPage.Text = new StringBuilder().Append(MyTexts.Get(MyCommonTexts.JoinGame_TabTitle_Lobbies).ToString()).Append(" (").Append(shownGames).Append(")");
            }

            //m_gameDataLabel.Visible = m_incompatibleGameData;

            m_gamesTable.SelectedRowIndex = null;
        }

        #endregion

    }
}
