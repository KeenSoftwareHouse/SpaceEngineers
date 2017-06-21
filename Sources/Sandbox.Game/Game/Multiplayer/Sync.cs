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
        static bool? m_steamOnline;
        //public static MyTransportLayer Transport { get { return Layer.TransportLayer; } }

        public static ulong MyId 
        { 
            get 
            { 
                if(MyFakes.ENABLE_RUN_WITHOUT_STEAM && MySandboxGame.IsDedicated == false)         
                {
                    if (m_steamOnline.HasValue == false)
                    {
                        m_steamOnline = MySteam.IsOnline;
                    }

                    if (m_steamOnline.Value == false)
                    {
                        return 1234567891011;
                    }
                }

                return MySteam.UserId; 
            } 
        }
        public static string MyName { get { return MySteam.UserName; } }

        public static float ServerSimulationRatio
        {
            get 
            {
                //when there is crash on different thread multiplayer can be disposed during calls
                MyMultiplayerBase multiplayer = MyMultiplayer.Static;
                return (multiplayer != null && !multiplayer.IsServer) ? multiplayer.ServerSimulationRatio : MyPhysics.SimulationRatio; 
            }
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
                if (IsServer)
                {
                    return 1.0f;
                }

                if (!MyFakes.ENABLE_MULTIPLAYER_CONSTRAINT_COMPENSATION)
                    return 1.0f;

                if (MyPhysics.SimulationRatio.IsZero(0.001f) || ServerSimulationRatio.IsZero(0.001f))
                {
                    Debug.Fail("Invalid simulation ratio");
                    return 1.0f;
                }
                else
                {                 
                    return (float)Math.Round(ServerSimulationRatio / MyPhysics.SimulationRatio,2);
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
