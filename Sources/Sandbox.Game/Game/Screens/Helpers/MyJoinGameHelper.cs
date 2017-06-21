using SteamSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

namespace Sandbox.Game.Screens.Helpers
{
    public class MyLobbyHelper
    {
        Lobby m_lobby;
        LobbyDataUpdate m_dataUpdateHandler;

        public event Action<Lobby> OnSuccess;

        public MyLobbyHelper(Lobby lobby)
        {
            m_lobby = lobby;
            m_dataUpdateHandler = new LobbyDataUpdate(JoinGame_LobbyUpdate);
        }

        void t_Elapsed(object sender, ElapsedEventArgs e)
        {
            SteamAPI.Instance.Matchmaking.LobbyDataUpdate -= m_dataUpdateHandler;
        }

        public bool RequestData()
        {
            SteamAPI.Instance.Matchmaking.LobbyDataUpdate += m_dataUpdateHandler;
            if (!m_lobby.RequestLobbyData())
            {
                SteamAPI.Instance.Matchmaking.LobbyDataUpdate -= m_dataUpdateHandler;
                return false;
            }
            return true;
        }

        public void Cancel()
        {
            SteamAPI.Instance.Matchmaking.LobbyDataUpdate -= m_dataUpdateHandler;
        }

        void JoinGame_LobbyUpdate(bool success, Lobby lobby, ulong memberOrLobby)
        {
            VRage.Profiler.ProfilerShort.Begin("JoinGame_LobbyUpdate");
            if (lobby.LobbyId == m_lobby.LobbyId)
            {
                SteamAPI.Instance.Matchmaking.LobbyDataUpdate -= m_dataUpdateHandler;
                var handler = OnSuccess;
                if (handler != null) handler(lobby);
            }
            VRage.Profiler.ProfilerShort.End();
        }
    }
}
