#region Using

using Sandbox.Game.Localization;
using Sandbox.Graphics.GUI;
using SteamSDK;
using System.Text;
using VRage;

#endregion

namespace Sandbox.Game.Gui
{
    partial class MyGuiScreenJoinGame : MyGuiScreenBase
    {

        #region Fields

        MyGuiControlTabPage m_LANPage;
       
        #endregion

        #region Constructor

        void InitLANPage()
        {
            InitServersTable();

            m_joinButton.ButtonClicked += OnJoinServer;
            m_refreshButton.ButtonClicked += OnRefreshLANServersClick;
            m_showOnlyCompatibleGames.IsCheckedChanged = OnLANCheckboxCheckChanged;
            m_showOnlyWithSameMods.IsCheckedChanged = OnLANCheckboxCheckChanged;
            m_allowedGroups.IsCheckedChanged = OnLANCheckboxCheckChanged;

            m_searchChangedFunc += RefreshLANGameList;
            
            m_LANPage = m_selectedPage;
            m_LANPage.SetToolTip(MyTexts.GetString(MyCommonTexts.JoinGame_TabTooltip_LAN));
            

            RefreshLANGameList();
        }

        void CloseLANPage()
        {
            CloseLANRequest();

            m_searchChangedFunc -= RefreshLANGameList;
        }

        #endregion

        #region Event handling

        private void OnRefreshLANServersClick(MyGuiControlButton obj)
        {
            RefreshLANGameList();
        }

        private void OnLANCheckboxCheckChanged(MyGuiControlCheckbox checkbox)
        {
            RefreshLANGameList();
        }

        #endregion

        #region Async Loading

        private void RefreshLANGameList()
        {
            CloseLANRequest();

            m_gamesTable.Clear();
            AddServerHeaders();

            m_textCache.Clear();
            m_gameTypeText.Clear();
            m_gameTypeToolTip.Clear();
            m_servers.Clear();
            m_LANPage.Text = new StringBuilder().Append(MyTexts.Get(MyCommonTexts.JoinGame_TabTitle_LAN));

            MySandboxGame.Log.WriteLine("Requesting dedicated servers");

            SteamAPI.Instance.OnLANServerListResponded += OnLANServerListResponded;
            SteamAPI.Instance.OnLANServersCompleteResponse += OnLANServersCompleteResponse;

            SteamAPI.Instance.RequestLANServerList();

            m_gamesTable.SelectedRowIndex = null;
        }


        void OnLANServerListResponded(int server)
        {
            VRage.Profiler.ProfilerShort.Begin("OnLANServerListResponded");
            GameServerItem serverItem = SteamAPI.Instance.GetLANServerDetails(server);
            AddServerItem(serverItem, 
                delegate() 
                {
                    m_LANPage.Text = new StringBuilder().Append(MyTexts.Get(MyCommonTexts.JoinGame_TabTitle_LAN).ToString()).Append(" (").Append(m_gamesTable.RowsCount).Append(")");
                },
                isFiltered: false);
            VRage.Profiler.ProfilerShort.End();
        }

        void OnLANServersCompleteResponse(MatchMakingServerResponseEnum response)
        {
            VRage.Profiler.ProfilerShort.Begin("OnLANServersCompleteResponse");
            CloseLANRequest();
            VRage.Profiler.ProfilerShort.End();
        }

        void CloseLANRequest()
        {
            SteamAPI.Instance.OnLANServerListResponded -= OnLANServerListResponded;
            SteamAPI.Instance.OnLANServersCompleteResponse -= OnLANServersCompleteResponse;
            SteamAPI.Instance.CancelLANServersRequest();
        }


        #endregion

    }
}
