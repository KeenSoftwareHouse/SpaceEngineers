using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using System.Diagnostics;
using SteamSDK;
using Sandbox.Engine.Networking;
using VRage.Game.ModAPI;

namespace Sandbox.Game.World
{
    /// <summary>
    /// This class identifies the steam client (basically a computer) on the network.
    /// </summary>
    public partial class MyNetworkClient : IMyNetworkClient
    {
        private readonly ulong m_steamUserId;
        public ulong SteamUserId { get { return m_steamUserId; } }

        public bool IsLocal { get; private set; }
        public string DisplayName { get; private set; }

        /// <summary>
        /// When player sends input, ClientTime is set on server
        /// Later when server sends position updates, it includes client time
        /// It's used for input prediction interpolation on client
        /// </summary>
        public ushort ClientFrameId;

        public MyPlayer FirstPlayer
        {
            get
            {
                return GetPlayer(0);
            }
        }

        public event Action ClientLeft;

        public MyNetworkClient(ulong steamId)
        {
            m_steamUserId = steamId;
            IsLocal = Sync.MyId == steamId;
            DisplayName = MySteam.IsActive ? MySteam.API.Friends.GetPersonaName(steamId) : "Client";
        }

        public MyPlayer GetPlayer(int serialId)
        {
            var controllerId = new MyPlayer.PlayerId() { SteamId = m_steamUserId, SerialId = serialId };
            return Sync.Players.GetPlayerById(controllerId);
        }
    }
}
