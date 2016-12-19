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

        MyGuiControlTabPage m_historyPage;

        #endregion

        #region Constructor

        void InitHistoryPage()
        {
            InitServersTable();

            m_joinButton.ButtonClicked += OnJoinServer;
            m_refreshButton.ButtonClicked += OnRefreshHistoryServersClick;
            m_showOnlyCompatibleGames.IsCheckedChanged = OnHistoryCheckboxCheckChanged;
            m_showOnlyWithSameMods.IsCheckedChanged = OnHistoryCheckboxCheckChanged;
            m_allowedGroups.IsCheckedChanged = OnHistoryCheckboxCheckChanged;

            m_searchChangedFunc += RefreshHistoryGameList;

            m_historyPage = m_selectedPage;
            m_historyPage.SetToolTip(MyTexts.GetString(MyCommonTexts.JoinGame_TabTooltip_History));


            RefreshHistoryGameList();
        }

        void CloseHistoryPage()
        {
            CloseHistoryRequest();

            m_searchChangedFunc -= RefreshHistoryGameList;
        }

        #endregion

        #region Event handling

        private void OnRefreshHistoryServersClick(MyGuiControlButton obj)
        {
            RefreshHistoryGameList();
        }

        private void OnHistoryCheckboxCheckChanged(MyGuiControlCheckbox checkbox)
        {
            RefreshHistoryGameList();
        }

        #endregion

        #region Async Loading

        private void RefreshHistoryGameList()
        {
            CloseHistoryRequest();

            m_gamesTable.Clear();
            AddServerHeaders();

            m_textCache.Clear();
            m_gameTypeText.Clear();
            m_gameTypeToolTip.Clear();
            m_servers.Clear();
            m_historyPage.Text = new StringBuilder().Append(MyTexts.Get(MyCommonTexts.JoinGame_TabTitle_History));

            MySandboxGame.Log.WriteLine("Requesting dedicated servers");

            SteamAPI.Instance.OnHistoryServerListResponded += OnHistoryServerListResponded;
            SteamAPI.Instance.OnHistoryServersCompleteResponse += OnHistoryServersCompleteResponse;


            String filterOps = string.Format("gamedir:{0};secure:1", MyPerGameSettings.SteamGameServerGameDir);
            //if (m_showOnlyWithSameMods.IsChecked) filterOps = filterOps + ";gametagsand:datahash" + MyDataIntegrityChecker.GetHashBase64(); // nonsense, datahash is inconsistent

            //if (!string.IsNullOrWhiteSpace(m_blockSearch.Text)) filterOps = filterOps + ";gametagsand:" + m_blockSearch.Text.Trim().Replace(":", "a58").Replace(";", "a59"); // nonsense, servername is not in tags

            MySandboxGame.Log.WriteLine("Requesting history servers, filterOps: " + filterOps);

            SteamAPI.Instance.RequestHistoryServerList(filterOps);

            m_gamesTable.SelectedRowIndex = null;
        }


        void OnHistoryServerListResponded(int server)
        {
            VRage.Profiler.ProfilerShort.Begin("OnHistoryServerListResponded");
            GameServerItem serverItem = SteamAPI.Instance.GetHistoryServerDetails(server);
            AddServerItem(serverItem, 
                delegate() 
                {
                    m_historyPage.Text = new StringBuilder().Append(MyTexts.Get(MyCommonTexts.JoinGame_TabTitle_History).ToString()).Append(" (").Append(m_gamesTable.RowsCount).Append(")");
                },
                isFiltered: false);
            VRage.Profiler.ProfilerShort.End();
        }

        void OnHistoryServersCompleteResponse(MatchMakingServerResponseEnum response)
        {
            VRage.Profiler.ProfilerShort.Begin("OnHistoryServersCompleteResponse");
            CloseHistoryRequest();
            VRage.Profiler.ProfilerShort.End();
        }

        void CloseHistoryRequest()
        {
            SteamAPI.Instance.OnHistoryServerListResponded -= OnHistoryServerListResponded;
            SteamAPI.Instance.OnHistoryServersCompleteResponse -= OnHistoryServersCompleteResponse;
            SteamAPI.Instance.CancelHistoryServersRequest();
        }


        #endregion

    }
}
