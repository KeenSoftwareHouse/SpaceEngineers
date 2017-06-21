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
using System.IO;
using Sandbox.Game.Localization;
using VRage;
using VRage.Library.Utils;
using VRage.FileSystem;
using VRage.Game;
using VRage.ObjectBuilders;

#endregion

namespace Sandbox.Game.Gui
{
    public partial class MyGuiScreenJoinGame : MyGuiScreenBase
    {

        #region Fields

        MyGuiControlTabControl m_joinGameTabs;

        MyGuiControlContextMenu m_contextMenu;

        StringBuilder m_textCache = new StringBuilder();
        StringBuilder m_gameTypeText = new StringBuilder();
        StringBuilder m_gameTypeToolTip = new StringBuilder();

        MyGuiControlTable m_gamesTable;
        MyGuiControlButton m_joinButton;
        MyGuiControlButton m_refreshButton;
        MyGuiControlCheckbox m_showOnlyCompatibleGames;
        MyGuiControlButton m_showOnlyCompatibleText;
        MyGuiControlCheckbox m_showOnlyWithSameMods;
        MyGuiControlButton m_showOnlyWithSameText;
        MyGuiControlCheckbox m_showOnlyFriends;
        MyGuiControlButton m_showOnlyFriendsText;
        MyGuiControlCheckbox m_allowedGroups;
        MyGuiControlButton m_allowedGroupsText;

        MyGuiControlTextbox m_blockSearch;
        MyGuiControlButton m_blockSearchClear;

        bool m_searchChanged = false;
        DateTime m_searchLastChanged = DateTime.Now;
        Action m_searchChangedFunc;

        MyGuiControlTabPage m_selectedPage;

        int m_remainingTimeUpdateFrame;

        private class CellRemainingTime : MyGuiControlTable.Cell
        {
            private DateTime m_timeEstimatedEnd;

            public CellRemainingTime(float remainingTime)
                : base("")
            {
                m_timeEstimatedEnd = DateTime.UtcNow + TimeSpan.FromSeconds(remainingTime);
                FillText();
            }

            public override void Update()
            {
                base.Update();
                FillText();
            }

            private void FillText()
            {
                TimeSpan remainingTimeSpan = m_timeEstimatedEnd - DateTime.UtcNow;
                if (remainingTimeSpan < TimeSpan.Zero)
                    remainingTimeSpan = TimeSpan.Zero;

                Text.Clear().Append(remainingTimeSpan.ToString(@"mm\:ss"));
            }
        }

        #endregion

        #region Constructor

        public MyGuiScreenJoinGame()
            : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(0.95f, 0.8f))
        {
            EnabledBackgroundFade = true;

            RecreateControls(true);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenJoinGame";
        }

        protected override void OnClosed()
        {
            base.OnClosed();
        }

        private int PlayerCountComparisonServers(MyGuiControlTable.Cell a, MyGuiControlTable.Cell b)
        {
            var playersA = int.Parse(a.Text.Split('/')[0].ToString());
            var playersB = int.Parse(b.Text.Split('/')[0].ToString());
            var maxPlayersA = int.Parse(a.Text.Split('/')[1].ToString());
            var maxPlayersB = int.Parse(b.Text.Split('/')[1].ToString());

            if (playersA == playersB)
            {
                if (maxPlayersA == maxPlayersB)
                {
                    var serverA = (GameServerItem)a.Row.UserData;
                    var serverB = (GameServerItem)b.Row.UserData;
                    return serverA.SteamID.CompareTo(serverB.SteamID);
                }
                else
                    return maxPlayersA.CompareTo(maxPlayersB);
            }
            else
                return playersA.CompareTo(playersB);
        }

        private int PlayerCountComparisonLobbies(MyGuiControlTable.Cell a, MyGuiControlTable.Cell b)
        {
            var playersA = int.Parse(a.Text.Split('/')[0].ToString());
            var playersB = int.Parse(b.Text.Split('/')[0].ToString());
            var maxPlayersA = int.Parse(a.Text.Split('/')[1].ToString());
            var maxPlayersB = int.Parse(b.Text.Split('/')[1].ToString());

            if (playersA == playersB)
            {
                if (maxPlayersA == maxPlayersB)
                {
                    var serverA = (Lobby)a.Row.UserData;
                    var serverB = (Lobby)b.Row.UserData;
                    return serverA.LobbyId.CompareTo(serverB.LobbyId);
                }
                else
                    return maxPlayersA.CompareTo(maxPlayersB);
            }
            else
                return playersA.CompareTo(playersB);
        }

        private int TextComparisonServers(MyGuiControlTable.Cell a, MyGuiControlTable.Cell b)
        {
            var comp = a.Text.CompareToIgnoreCase(b.Text);

            if (comp == 0)
            {
                var serverA = (GameServerItem)a.Row.UserData;
                var serverB = (GameServerItem)b.Row.UserData;
                return serverA.SteamID.CompareTo(serverB.SteamID);
            }
            else
                return comp;
        }

        private int TextComparisonLobbies(MyGuiControlTable.Cell a, MyGuiControlTable.Cell b)
        {
            var comp = a.Text.CompareToIgnoreCase(b.Text);

            if (comp == 0)
            {
                var serverA = (Lobby)a.Row.UserData;
                var serverB = (Lobby)b.Row.UserData;
                return serverA.LobbyId.CompareTo(serverB.LobbyId);
            }
            else
            {
                return comp;
            }
        }

        private int PingComparison(MyGuiControlTable.Cell a, MyGuiControlTable.Cell b)
        {
            var pingA = int.Parse(a.Text.ToString());
            var pingB = int.Parse(b.Text.ToString());

            if (pingA == pingB)
            {
                var serverA = (GameServerItem)a.Row.UserData;
                var serverB = (GameServerItem)b.Row.UserData;
                return serverA.SteamID.CompareTo(serverB.SteamID);
            }
            else
                return pingA.CompareTo(pingB);
        }

        private int ModsComparisonLobbies(MyGuiControlTable.Cell a, MyGuiControlTable.Cell b)
        {
            int modCountA = 0;
            int.TryParse(a.Text.ToString(), out modCountA);
            int modCountB = 0;
            int.TryParse(b.Text.ToString(), out modCountB);

            if (modCountA == modCountB)
            {
                var serverA = (Lobby)a.Row.UserData;
                var serverB = (Lobby)b.Row.UserData;
                return serverA.LobbyId.CompareTo(serverB.LobbyId);
            }
            else
                return modCountA.CompareTo(modCountB);
        }

        private int ModsComparisonServers(MyGuiControlTable.Cell a, MyGuiControlTable.Cell b)
        {
            int modCountA = 0;
            int.TryParse(a.Text.ToString(), out modCountA);
            int modCountB = 0;
            int.TryParse(b.Text.ToString(), out modCountB);

            if (modCountA == modCountB)
            {
                var serverA = (GameServerItem)a.Row.UserData;
                var serverB = (GameServerItem)b.Row.UserData;
                return serverA.SteamID.CompareTo(serverB.SteamID);
            }
            else
                return modCountA.CompareTo(modCountB);
        }

        #endregion

        #region Recreate

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            string filepath = MakeScreenFilepath("JoinScreen");
            MyObjectBuilder_GuiScreen objectBuilder;

            var fsPath = Path.Combine(MyFileSystem.ContentPath, filepath);
            MyObjectBuilderSerializer.DeserializeXML<MyObjectBuilder_GuiScreen>(fsPath, out objectBuilder);
            Init(objectBuilder);

            m_joinGameTabs = Controls.GetControlByName("JoinGameTabs") as MyGuiControlTabControl;
            MyDebug.AssertDebug(m_joinGameTabs != null);

            m_joinGameTabs.TabButtonScale = 0.86f;

            m_joinGameTabs.OnPageChanged += joinGameTabs_OnPageChanged;

            joinGameTabs_OnPageChanged();
        }

        void joinGameTabs_OnPageChanged()
        {
            var serversPage = (MyGuiControlTabPage)m_joinGameTabs.Controls.GetControlByName("PageServersPanel");
            var lobbiesPage = (MyGuiControlTabPage)m_joinGameTabs.Controls.GetControlByName("PageLobbiesPanel");
            var favoritesPage = (MyGuiControlTabPage)m_joinGameTabs.Controls.GetControlByName("PageFavoritesPanel");
            var historyPage = (MyGuiControlTabPage)m_joinGameTabs.Controls.GetControlByName("PageHistoryPanel");
            var LANPage = (MyGuiControlTabPage)m_joinGameTabs.Controls.GetControlByName("PageLANPanel");
            var friendsPage = (MyGuiControlTabPage)m_joinGameTabs.Controls.GetControlByName("PageFriendsPanel");

            if (m_selectedPage == serversPage)
            {
                CloseServersPage();
            }
            else if (m_selectedPage == lobbiesPage)
            {
                CloseLobbyPage();
            }
            else if (m_selectedPage == favoritesPage)
            {
                CloseFavoritesPage();
            }
            else if (m_selectedPage == LANPage)
            {
                CloseLANPage();
            }
            else if (m_selectedPage == historyPage)
            {
                CloseHistoryPage();
            }
            else if (m_selectedPage == friendsPage)
            {
                CloseFriendsPage();
            }

            m_selectedPage = m_joinGameTabs.GetTabSubControl(m_joinGameTabs.SelectedPage);

            InitPageControls(m_selectedPage);
            if (m_selectedPage == serversPage)
            {
                InitServersPage();
                m_showOnlyFriends.Enabled = false;
                m_showOnlyFriendsText.Enabled = false;
            }
            else if (m_selectedPage == lobbiesPage)
            {
                InitLobbyPage();
                m_showOnlyFriends.Enabled = true;
                m_showOnlyFriendsText.Enabled = true;
            }
            else if (m_selectedPage == favoritesPage)
            {
                InitFavoritesPage();
                m_showOnlyFriends.Enabled = false;
                m_showOnlyFriendsText.Enabled = false;
            }
            else if (m_selectedPage == historyPage)
            {
                InitHistoryPage();
                m_showOnlyFriends.Enabled = false;
                m_showOnlyFriendsText.Enabled = false;
            }
            else if (m_selectedPage == LANPage)
            {
                InitLANPage();
                m_showOnlyFriends.Enabled = false;
                m_showOnlyFriendsText.Enabled = false;
            }
            else if (m_selectedPage == friendsPage)
            {
                InitFriendsPage();
                m_showOnlyFriends.Enabled = false;
                m_showOnlyFriendsText.Enabled = false;
            }

            if(m_contextMenu != null)
            {
                m_contextMenu.Deactivate();
                m_contextMenu = null;
            }

            m_contextMenu = new MyGuiControlContextMenu();
            m_contextMenu.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM;
            m_contextMenu.Deactivate();
            m_contextMenu.ItemClicked += OnContextMenu_ItemClicked;
            Controls.Add(m_contextMenu);
        }

        void OnContextMenu_ItemClicked(MyGuiControlContextMenu sender, MyGuiControlContextMenu.EventArgs eventArgs)
        {
            var actionItem = (ContextMenuFavoriteActionItem)eventArgs.UserData;
            var server = actionItem.Server;
            if (server != null)
            {
                switch (actionItem._Action)
                {
                    case ContextMenuFavoriteAction.Add:
                        UInt32 unixTimestamp = (UInt32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                        SteamAPI.Instance.AddFavoriteGame(server.AppID, System.Net.IPAddressExtensions.ToIPv4NetworkOrder(server.NetAdr.Address), (UInt16)server.NetAdr.Port, (UInt16)server.NetAdr.Port, FavoriteEnum.Favorite, unixTimestamp);
                        break;
                    case ContextMenuFavoriteAction.Remove:
                        SteamAPI.Instance.RemoveFavoriteGame(server.AppID, System.Net.IPAddressExtensions.ToIPv4NetworkOrder(server.NetAdr.Address), (UInt16)server.NetAdr.Port, (UInt16)server.NetAdr.Port, FavoriteEnum.Favorite);

                        m_gamesTable.RemoveSelectedRow();
                        m_favoritesPage.Text = new StringBuilder().Append(MyTexts.Get(MyCommonTexts.JoinGame_TabTitle_Favorites).ToString()).Append(" (").Append(m_gamesTable.RowsCount).Append(")");
                        break;
                    default:
                        throw new InvalidBranchException();
                        break;
                }
            }
        }

        void InitPageControls(MyGuiControlTabPage page)
        {
            page.Controls.Clear();

            var origin = new Vector2(-0.64f, -0.35f);
            Vector2 buttonSize = MyGuiControlButton.GetVisualStyle(MyGuiControlButtonStyleEnum.Default).NormalTexture.MinSizeGui;

            m_gamesTable = new MyGuiControlTable();
            m_gamesTable.Position = origin + new Vector2(buttonSize.X, 0f);
            m_gamesTable.Size = new Vector2(1465f / MyGuiConstants.GUI_OPTIMAL_SIZE.X, 1f);
            m_gamesTable.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            m_gamesTable.VisibleRowsCount = 16;
            page.Controls.Add(m_gamesTable);


            Vector2 buttonOrigin = origin + new Vector2(0.28f, 0.66f);
            Vector2 buttonDelta = new Vector2(0.2f, 0.0f);

            int numControls = 0;

            page.Controls.Add(m_joinButton = MakeButton(buttonOrigin + buttonDelta * numControls++, MyCommonTexts.ScreenMenuButtonJoinWorld, MyCommonTexts.ScreenMenuButtonJoinWorld, null));
            page.Controls.Add(m_refreshButton = MakeButton(buttonOrigin + buttonDelta * numControls++, MyCommonTexts.ScreenLoadSubscribedWorldRefresh, MyCommonTexts.ScreenLoadSubscribedWorldRefresh, null));
            m_joinButton.Enabled = false;

            var checkboxPos = buttonOrigin + new Vector2(-0.09f, -0.02f) + numControls * buttonDelta;
            var checkBoxDelta = new Vector2(0.0f, 0.04f);

            var blockSearchLabel = new MyGuiControlLabel()
            {
                Position = checkboxPos + new Vector2(0f, -0.04f),
                Size = new Vector2(0.05f, 0.02f),
                TextEnum = MyCommonTexts.JoinGame_SearchLabel
            };
            page.Controls.Add(blockSearchLabel);

            m_blockSearch = new MyGuiControlTextbox()
            {
                Position = blockSearchLabel.Position + new Vector2(0.255f, 0f),
                Size = new Vector2(0.27f, 0.02f)
            };
            m_blockSearch.SetToolTip(MyCommonTexts.JoinGame_SearchTooltip);
            m_blockSearch.TextChanged += OnBlockSearchTextChanged;
            page.Controls.Add(m_blockSearch);

            m_blockSearchClear = new MyGuiControlButton()
            {
                Position = m_blockSearch.Position + new Vector2(0.13f, 0f),
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER,
				VisualStyle = MyGuiControlButtonStyleEnum.Close,
				ActivateOnMouseRelease = true
            };
            m_blockSearchClear.ButtonClicked += BlockSearchClear_ButtonClicked;
            page.Controls.Add(m_blockSearchClear);

            numControls = 0;

            m_showOnlyCompatibleText = new MyGuiControlButton(
                            position: checkboxPos + checkBoxDelta * numControls + new Vector2(buttonSize.Y * 0.5f, 0),
                            text: MyTexts.Get(MyCommonTexts.MultiplayerCompatibleVersions),
                            toolTip: MyTexts.GetString(MyCommonTexts.MultiplayerCompatibleVersions),
                            onButtonClick: OnShowOnlyCompatibleTextClick,
                            visualStyle: MyGuiControlButtonStyleEnum.ClickableText,
                            originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            page.Controls.Add(m_showOnlyCompatibleText);
            bool compatibleChecked = true;
            if (m_showOnlyCompatibleGames != null)
                compatibleChecked = m_showOnlyCompatibleGames.IsChecked;
            m_showOnlyCompatibleGames = new MyGuiControlCheckbox(checkboxPos + checkBoxDelta * numControls++, null, null, MySandboxGame.Config.MultiplayerShowCompatible, MyGuiControlCheckboxStyleEnum.Debug, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            m_showOnlyCompatibleGames.IsChecked = compatibleChecked;
            page.Controls.Add(m_showOnlyCompatibleGames);

            bool showSameMods = true;
            if (m_showOnlyWithSameMods != null)
                showSameMods = m_showOnlyWithSameMods.IsChecked;
            m_showOnlyWithSameMods = new MyGuiControlCheckbox(
                position: checkboxPos + checkBoxDelta * numControls,
                visualStyle: MyGuiControlCheckboxStyleEnum.Debug,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            m_showOnlyWithSameMods.IsChecked = showSameMods;
            page.Controls.Add(m_showOnlyWithSameMods);

            m_showOnlyWithSameText = new MyGuiControlButton(
                 position: checkboxPos + checkBoxDelta * numControls + new Vector2(buttonSize.Y * 0.5f, 0),
                 text: MyTexts.Get(MyCommonTexts.MultiplayerJoinSameGameData),
                 toolTip: MyTexts.GetString(MyCommonTexts.MultiplayerJoinSameGameData),
                 onButtonClick: OnShowOnlySameModsClick,
                 visualStyle: MyGuiControlButtonStyleEnum.ClickableText,
                 originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            page.Controls.Add(m_showOnlyWithSameText);

            checkboxPos = buttonOrigin + new Vector2(-0.09f, -0.02f) + 3 * buttonDelta;
            numControls = 0;

            bool showOnlyFriends = false;
            if (m_showOnlyFriends != null)
                showOnlyFriends = m_showOnlyFriends.IsChecked;
            m_showOnlyFriends = new MyGuiControlCheckbox(
                position: checkboxPos + checkBoxDelta * numControls + new Vector2(buttonSize.Y * 0.5f, 0),
                visualStyle: MyGuiControlCheckboxStyleEnum.Debug,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            m_showOnlyFriends.IsChecked = showOnlyFriends;
            page.Controls.Add(m_showOnlyFriends);

            m_showOnlyFriendsText = new MyGuiControlButton(
                 position: checkboxPos + checkBoxDelta * numControls++ + new Vector2(buttonSize.Y, 0),
                 text: MyTexts.Get(MyCommonTexts.MultiplayerJoinFriendsGames),
                 toolTip: MyTexts.GetString(MyCommonTexts.MultiplayerJoinFriendsGames),
                 onButtonClick: OnFriendsOnlyTextClick,
                 visualStyle: MyGuiControlButtonStyleEnum.ClickableText,
                 originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            page.Controls.Add(m_showOnlyFriendsText);

            bool allowedGroups = true;
            if (m_allowedGroups != null)
                allowedGroups = m_allowedGroups.IsChecked;
            m_allowedGroups = new MyGuiControlCheckbox(
                position: checkboxPos + checkBoxDelta * numControls + new Vector2(buttonSize.Y * 0.5f, 0),
                visualStyle: MyGuiControlCheckboxStyleEnum.Debug,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            m_allowedGroups.IsChecked = allowedGroups;
            page.Controls.Add(m_allowedGroups);

            m_allowedGroupsText = new MyGuiControlButton(
                 position: checkboxPos + checkBoxDelta * numControls++ + new Vector2(buttonSize.Y, 0),
                 text: MyTexts.Get(MyCommonTexts.MultiplayerJoinAllowedGroups),
                 toolTip: MyTexts.GetString(MyCommonTexts.MultiplayerJoinAllowedGroups),
                 onButtonClick: OnAllowedGroupsTextClick,
                 visualStyle: MyGuiControlButtonStyleEnum.ClickableText,
                 originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            page.Controls.Add(m_allowedGroupsText);

        }

        private MyGuiControlButton MakeButton(Vector2 position, MyStringId text, MyStringId toolTip, Action<MyGuiControlButton> onClick)
        {
            return new MyGuiControlButton(
                            position: position,
                            text: MyTexts.Get(text),
                            toolTip: MyTexts.GetString(toolTip),
                            onButtonClick: onClick);
        }

        #endregion

        public override bool Update(bool hasFocus)
        {
            if (m_searchChanged && DateTime.Now.Subtract(m_searchLastChanged).Milliseconds > 500)
            {
                m_searchChanged = false;
                m_searchChangedFunc();
            }

            // Update table cells with remaining time
            if (MyFakes.ENABLE_JOIN_SCREEN_REMAINING_TIME)
            {
                ++m_remainingTimeUpdateFrame;

                if (m_remainingTimeUpdateFrame % 50 == 0)
                {
                    for (int i=0; i<m_gamesTable.RowsCount; ++i) 
                    {
                        var row = m_gamesTable.GetRow(i);
                        row.Update();
                    }

                    m_remainingTimeUpdateFrame = 0;
                }
            }

            return base.Update(hasFocus);
        }

        #region Event handling

        private void OnShowOnlyCompatibleTextClick(MyGuiControlButton button)
        {
            m_showOnlyCompatibleGames.IsChecked = !m_showOnlyCompatibleGames.IsChecked;
        }

        private void OnShowOnlySameModsClick(MyGuiControlButton button)
        {
            m_showOnlyWithSameMods.IsChecked = !m_showOnlyWithSameMods.IsChecked;
        }

        private void OnFriendsOnlyTextClick(MyGuiControlButton button)
        {
            m_showOnlyFriends.IsChecked = !m_showOnlyFriends.IsChecked;
        }

        private void OnAllowedGroupsTextClick(MyGuiControlButton button)
        {
            m_allowedGroups.IsChecked = !m_allowedGroups.IsChecked;
        }

        private void OnBlockSearchTextChanged(MyGuiControlTextbox textbox)
        {
            m_searchChanged = true;
            m_searchLastChanged = DateTime.Now;
        }

        private void BlockSearchClear_ButtonClicked(MyGuiControlButton obj)
        {
            m_blockSearch.Text = "";
        }

        #endregion

    }
}
