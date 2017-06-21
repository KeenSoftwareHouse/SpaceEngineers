#region Using

using Sandbox.Game.Localization;
using Sandbox.Graphics.GUI;
using SteamSDK;
using System;
using System.Text;
using VRage;

#endregion

namespace Sandbox.Game.Gui
{
    partial class MyGuiScreenJoinGame : MyGuiScreenBase
    {
        enum ContextMenuFavoriteAction
        {
            Add,
            Remove,
        }

        struct ContextMenuFavoriteActionItem
        {
            public GameServerItem Server;
            public ContextMenuFavoriteAction _Action;
        }

        #region Fields

        MyGuiControlTabPage m_favoritesPage;

        #endregion

        #region Constructor

        void InitFavoritesPage()
        {
            InitServersTable();

            m_joinButton.ButtonClicked += OnJoinServer;
            m_refreshButton.ButtonClicked += OnRefreshFavoritesServersClick;
            m_showOnlyCompatibleGames.IsCheckedChanged = OnFavoritesCheckboxCheckChanged;
            m_showOnlyWithSameMods.IsCheckedChanged = OnFavoritesCheckboxCheckChanged;
            m_allowedGroups.IsCheckedChanged = OnFavoritesCheckboxCheckChanged;

            m_searchChangedFunc += RefreshFavoritesGameList;

            m_favoritesPage = m_selectedPage;
            m_favoritesPage.SetToolTip(MyTexts.GetString(MyCommonTexts.JoinGame_TabTooltip_Favorites));


            RefreshFavoritesGameList();
        }

        void CloseFavoritesPage()
        {
            CloseFavoritesRequest();

            m_searchChangedFunc -= RefreshFavoritesGameList;
        }

        #endregion

        #region Event handling

        private void OnRefreshFavoritesServersClick(MyGuiControlButton obj)
        {
            RefreshFavoritesGameList();
        }

        private void OnFavoritesCheckboxCheckChanged(MyGuiControlCheckbox checkbox)
        {
            RefreshFavoritesGameList();
        }

        #endregion

        #region Async Loading

        private void RefreshFavoritesGameList()
        {
            CloseFavoritesRequest();

            m_gamesTable.Clear();
            AddServerHeaders();

            m_textCache.Clear();
            m_gameTypeText.Clear();
            m_gameTypeToolTip.Clear();
            m_servers.Clear();
            m_favoritesPage.Text = new StringBuilder().Append(MyTexts.Get(MyCommonTexts.JoinGame_TabTitle_Favorites));

            MySandboxGame.Log.WriteLine("Requesting dedicated servers");

            SteamAPI.Instance.OnFavoritesServerListResponded += OnFavoritesServerListResponded;
            SteamAPI.Instance.OnFavoritesServersCompleteResponse += OnFavoritesServersCompleteResponse;


            String filterOps = string.Format("gamedir:{0};secure:1", MyPerGameSettings.SteamGameServerGameDir);
            //if (m_showOnlyWithSameMods.IsChecked) filterOps = filterOps + ";gametagsand:datahash" + MyDataIntegrityChecker.GetHashBase64(); // nonsense, datahash is inconsistent

            //if (!string.IsNullOrWhiteSpace(m_blockSearch.Text)) filterOps = filterOps + ";gametagsand:" + m_blockSearch.Text.Trim().Replace(":", "a58").Replace(";", "a59"); // nonsense, servername is not in tags

            MySandboxGame.Log.WriteLine("Requesting favorite servers, filterOps: " + filterOps);

            SteamAPI.Instance.RequestFavoritesServerList(filterOps);

            m_gamesTable.SelectedRowIndex = null;
        }


        void OnFavoritesServerListResponded(int server)
        {
            VRage.Profiler.ProfilerShort.Begin("OnFavoritesServerListResponded");
            GameServerItem serverItem = SteamAPI.Instance.GetFavoritesServerDetails(server);
            AddServerItem(serverItem, 
                delegate() 
                {
                    m_favoritesPage.Text = new StringBuilder().Append(MyTexts.Get(MyCommonTexts.JoinGame_TabTitle_Favorites).ToString()).Append(" (").Append(m_gamesTable.RowsCount).Append(")");
                },
                isFiltered: false);
            VRage.Profiler.ProfilerShort.End();
        }

        void OnFavoritesServersCompleteResponse(MatchMakingServerResponseEnum response)
        {
            VRage.Profiler.ProfilerShort.Begin("OnFavoritesServersCompleteResponse");
            CloseFavoritesRequest();
            VRage.Profiler.ProfilerShort.End();
        }

        void CloseFavoritesRequest()
        {
            SteamAPI.Instance.OnFavoritesServerListResponded -= OnFavoritesServerListResponded;
            SteamAPI.Instance.OnFavoritesServersCompleteResponse -= OnFavoritesServersCompleteResponse;
            SteamAPI.Instance.CancelFavoritesServersRequest();
        }


        #endregion

    }
}
