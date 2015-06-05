using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.World;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.Multiplayer
{
    /// <summary>
    /// Shortcut class for various multiplayer things
    /// </summary>
    public static class Sync
    {
        public static bool MultiplayerActive { get { return MyMultiplayer.Static != null; } }
        //public static Lobby Lobby { get { return Multiplayer.Lobby; } }
        public static bool IsServer { get { return MultiplayerActive ? MyMultiplayer.Static.IsServer : true; } }
        public static ulong ServerId { get { return MultiplayerActive ? MyMultiplayer.Static.ServerId : MyId; } }
        public static MySyncLayer Layer { get { return MySession.Static != null ? MySession.Static.SyncLayer : null; } }
        //public static MyTransportLayer Transport { get { return Layer.TransportLayer; } }

        public static ulong MyId { get { return MySteam.UserId; } }
        public static string MyName { get { return MySteam.UserName; } }

        public static float ServerSimulationRatio
        {
            get { return (MultiplayerActive && !IsServer) ? MyMultiplayer.Static.ServerSimulationRatio : MyPhysics.SimulationRatio; }
            set
            {
                Debug.Assert(!IsServer, "Server should not set simulation speed!");
                if (MultiplayerActive && !IsServer)
                    MyMultiplayer.Static.ServerSimulationRatio = value;
            }
        }

        /// <summary>
        /// Use this number to achieve same speed as server.
        /// </summary>
        public static float RelativeSimulationRatio
        {
            get
            {
                if (!MyFakes.ENABLE_MULTIPLAYER_CONSTRAINT_COMPENSATION)
                    return 1.0f;

                if (MyPhysics.SimulationRatio.IsZero(0.001f) || ServerSimulationRatio.IsZero(0.001f))
                {
                    Debug.Fail("Invalid simulation ratio");
                    return 1.0f;
                }
                else
                {
                    return ServerSimulationRatio / MyPhysics.SimulationRatio;
                }
            }
        }

        public static MyClientCollection Clients { get { return Layer == null ? null : Layer.Clients; } }
        public static MyPlayerCollection Players { get { return MySession.Static.Players; } }
        //public static MyPlayer Server { get { return Layer.Players[ServerId]; } }
        //public static MyPlayer Me { get { return Layer.Players[MyId]; } }

        public static bool IsProcessingBufferedMessages { get { return Layer.TransportLayer.IsProcessingBuffer; } }

        public static bool IsGameServer(this MyNetworkClient client)
        {
            return client != null && client.SteamUserId == ServerId;
        }
    }
}
