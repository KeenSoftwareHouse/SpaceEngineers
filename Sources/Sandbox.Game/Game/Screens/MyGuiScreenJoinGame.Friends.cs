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

        #region Fields

        MyGuiControlTabPage m_friendsPage;

        #endregion

        #region Constructor

        void InitFriendsPage()
        {
            InitServersTable();

            m_joinButton.ButtonClicked += OnJoinServer;
            m_refreshButton.ButtonClicked += OnRefreshFriendsServersClick;
            m_showOnlyCompatibleGames.IsCheckedChanged = OnFriendsCheckboxCheckChanged;
            m_showOnlyWithSameMods.IsCheckedChanged = OnFriendsCheckboxCheckChanged;
            m_allowedGroups.IsCheckedChanged = OnFriendsCheckboxCheckChanged;

            m_searchChangedFunc += RefreshFriendsGameList;

            m_friendsPage = m_selectedPage;
            m_friendsPage.SetToolTip(MyTexts.GetString(MyCommonTexts.JoinGame_TabTooltip_Friends));


            RefreshFriendsGameList();
        }

        void CloseFriendsPage()
        {
            CloseFriendsRequest();

            m_searchChangedFunc -= RefreshFriendsGameList;
        }

        #endregion

        #region Event handling

        private void OnRefreshFriendsServersClick(MyGuiControlButton obj)
        {
            RefreshFriendsGameList();
        }

        private void OnFriendsCheckboxCheckChanged(MyGuiControlCheckbox checkbox)
        {
            RefreshFriendsGameList();
        }

        #endregion

        #region Async Loading

        private void RefreshFriendsGameList()
        {
            CloseFriendsRequest();

            m_gamesTable.Clear();
            AddServerHeaders();

            m_textCache.Clear();
            m_gameTypeText.Clear();
            m_gameTypeToolTip.Clear();
            m_servers.Clear();
            m_friendsPage.Text = new StringBuilder().Append(MyTexts.Get(MyCommonTexts.JoinGame_TabTitle_Friends));

            MySandboxGame.Log.WriteLine("Requesting dedicated servers");

            SteamAPI.Instance.OnFriendsServerListResponded += OnFriendsServerListResponded;
            SteamAPI.Instance.OnFriendsServersCompleteResponse += OnFriendsServersCompleteResponse;


            String filterOps = string.Format("gamedir:{0};secure:1", MyPerGameSettings.SteamGameServerGameDir);
            //if (m_showOnlyWithSameMods.IsChecked) filterOps = filterOps + ";gametagsand:datahash" + MyDataIntegrityChecker.GetHashBase64(); // nonsense, datahash is inconsistent

            //if (!string.IsNullOrWhiteSpace(m_blockSearch.Text)) filterOps = filterOps + ";gametagsand:" + m_blockSearch.Text.Trim().Replace(":", "a58").Replace(";", "a59"); // nonsense, servername is not in tags

            MySandboxGame.Log.WriteLine("Requesting friends servers, filterOps: " + filterOps);

            SteamAPI.Instance.RequestFriendsServerList(filterOps);

            m_gamesTable.SelectedRowIndex = null;
        }


        void OnFriendsServerListResponded(int server)
        {
            VRage.Profiler.ProfilerShort.Begin("OnFriendsServerListResponded");
            GameServerItem serverItem = SteamAPI.Instance.GetFriendsServerDetails(server);
            AddServerItem(serverItem, 
                delegate() 
                {
                    m_friendsPage.Text = new StringBuilder().Append(MyTexts.Get(MyCommonTexts.JoinGame_TabTitle_Friends).ToString()).Append(" (").Append(m_gamesTable.RowsCount).Append(")");
                },
                isFiltered: false);
            VRage.Profiler.ProfilerShort.End();
        }

        void OnFriendsServersCompleteResponse(MatchMakingServerResponseEnum response)
        {
            VRage.Profiler.ProfilerShort.Begin("OnFriendsServersCompleteResponse");
            CloseFriendsRequest();
            VRage.Profiler.ProfilerShort.End();
        }

        void CloseFriendsRequest()
        {
            SteamAPI.Instance.OnFriendsServerListResponded -= OnFriendsServerListResponded;
            SteamAPI.Instance.OnFriendsServersCompleteResponse -= OnFriendsServersCompleteResponse;
            SteamAPI.Instance.CancelFriendsServersRequest();
        }


        #endregion

    }
}
