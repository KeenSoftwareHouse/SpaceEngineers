using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Game.ModAPI;
using VRage.Network;
using VRage.ObjectBuilders;
using VRage.Scripting;
using MyMultiplayerMain = Sandbox.Engine.Multiplayer.MyMultiplayer;
using Sandbox.Game.Gui;

namespace Sandbox.ModAPI
{
    public static class MyModAPIHelper
    {
        public static void OnSessionLoaded()
        {
            MySandboxGame.EnableSimSpeedLocking = true;
            MyAPIGateway.Session = MySession.Static;
            MyAPIGateway.Entities = new MyEntitiesHelper_ModAPI();
            MyAPIGateway.Players = Sync.Players;
            MyAPIGateway.CubeBuilder = MyCubeBuilder.Static;
            MyAPIGateway.IngameScripting = MyIngameScripting.Static;
            MyAPIGateway.TerminalActionsHelper = MyTerminalControlFactoryHelper.Static;
            MyAPIGateway.Utilities = MyAPIUtilities.Static;
            MyAPIGateway.Parallel = MyParallelTask.Static;
            MyAPIGateway.Physics = Physics.MyPhysics.Static;
            MyAPIGateway.Multiplayer = MyMultiplayer.Static;
            MyAPIGateway.PrefabManager = MyPrefabManager.Static;
            MyAPIGateway.Input = (VRage.ModAPI.IMyInput)VRage.Input.MyInput.Static;
            MyAPIGateway.TerminalControls = MyTerminalControls.Static;
            MyAPIGateway.Gui = new MyGuiModHelpers();
        }

        [StaticEventOwner]
        public class MyMultiplayer : IMyMultiplayer
        {

            public static MyMultiplayer Static;
            const int MAX_MESSAGE_SIZE = 4096;

            static Dictionary<ushort, List<Action<byte[]>>> m_registeredListeners = new Dictionary<ushort, List<Action<byte[]>>>();

            static MyMultiplayer()
            {
                Static = new MyMultiplayer();
            }

            public bool MultiplayerActive
            {
                get { return Sync.MultiplayerActive; }
            }

            public bool IsServer
            {
                get { return Sync.IsServer; }
            }

            public ulong ServerId
            {
                get { return Sync.ServerId; }
            }

            public ulong MyId
            {
                get { return Sync.MyId; }
            }

            public string MyName
            {
                get { return Sync.MyName; }
            }

            public IMyPlayerCollection Players
            {
                get { return Sync.Players; }
            }

            public bool IsServerPlayer(IMyNetworkClient player)
            {
                if(player is MyNetworkClient)
                    return (player as MyNetworkClient).IsGameServer();
                return false;
            }

            public void SendEntitiesCreated(List<MyObjectBuilder_EntityBase> objectBuilders)
            {
               // MySyncCreate.SendEntitiesCreated(objectBuilders);
            }

            public bool SendMessageToServer(ushort id, byte[] message, bool reliable)
            {
                if (message.Length > MAX_MESSAGE_SIZE)
                    return false;

                if (reliable)
                    MyMultiplayerMain.RaiseStaticEvent(s => MyMultiplayer.ModMessageServerReliable, id, message, Sync.ServerId);
                else
                    MyMultiplayerMain.RaiseStaticEvent(s => MyMultiplayer.ModMessageServerUnreliable, id, message, Sync.ServerId);

                return true;
            }

            public bool SendMessageToOthers(ushort id, byte[] message, bool reliable)
            {
                if (message.Length > MAX_MESSAGE_SIZE)
                    return false;

                if (reliable)
                    MyMultiplayerMain.RaiseStaticEvent(s => MyMultiplayer.ModMessageBroadcastReliable, id, message);
                else
                    MyMultiplayerMain.RaiseStaticEvent(s => MyMultiplayer.ModMessageBroadcastUnreliable, id, message);

                return true;
            }

            public bool SendMessageTo(ushort id, byte[] message, ulong recipient, bool reliable)
            {
                if (message.Length > MAX_MESSAGE_SIZE)
                    return false;

                if (reliable)
                    MyMultiplayerMain.RaiseStaticEvent(s => MyMultiplayer.ModMessageClientReliable, id, message, recipient, new EndpointId(recipient));
                else
                    MyMultiplayerMain.RaiseStaticEvent(s => MyMultiplayer.ModMessageClientUnreliable, id, message, recipient, new EndpointId(recipient));

                return true;
            }

            public void JoinServer(string address)
            {
                if (MySandboxGame.IsDedicated && IsServer)
                    return;

                System.Net.IPEndPoint endpoint;
                if (System.Net.IPAddressExtensions.TryParseEndpoint(address, out endpoint))
                {
                    MySessionLoader.UnloadAndExitToMenu();
                    MySandboxGame.Services.SteamService.SteamAPI.PingServer(System.Net.IPAddressExtensions.ToIPv4NetworkOrder(endpoint.Address), (ushort)endpoint.Port);
                }
            }

            public void RegisterMessageHandler(ushort id, Action<byte[]> messageHandler)
            {
                List<Action<byte[]>> actionList = null;
                if (m_registeredListeners.TryGetValue(id, out actionList))
                {
                    actionList.Add(messageHandler);
                }
                else
                {
                    m_registeredListeners[id] = new List<Action<byte[]>>();
                    m_registeredListeners[id].Add(messageHandler);
                }
            }

            public void UnregisterMessageHandler(ushort id, Action<byte[]> messageHandler)
            {
                List<Action<byte[]>> actionList = null;
                if (m_registeredListeners.TryGetValue(id, out actionList))
                {
                    actionList.Remove(messageHandler);
                }     
            }

            [Event, Reliable, Server]
            static void ModMessageServerReliable(ushort id, byte[] message, ulong recipient)
            {
                HandleMessageClient(id, message, recipient);
            }

            [Event, Server]
            static void ModMessageServerUnreliable(ushort id, byte[] message, ulong recipient)
            {
                HandleMessageClient(id, message, recipient);
            }

            [Event, Reliable, Server, Client]
            static void ModMessageClientReliable(ushort id, byte[] message, ulong recipient)
            {
                HandleMessageClient(id, message, recipient);
            }

            [Event, Server, Client]
            static void ModMessageClientUnreliable(ushort id, byte[] message, ulong recipient)
            {
                HandleMessageClient(id, message, recipient);
            }

            [Event, Reliable, Server, BroadcastExcept]
            static void ModMessageBroadcastReliable(ushort id, byte[] message)
            {
                HandleMessage(id, message);
            }

            [Event, Server, BroadcastExcept]
            static void ModMessageBroadcastUnreliable(ushort id, byte[] message)
            {
                HandleMessage(id, message);
            }

            static void HandleMessageClient(ushort id, byte[] message, ulong recipient)
            {
                if (recipient != Sync.MyId)
                {
                    // This should just be the case of SendMessageTo(): server should
                    // not invoke this code
                    Debug.Assert(Sync.IsServer);
                    return;
                }

                HandleMessage(id, message);
            }

            static void HandleMessage(ushort id, byte[] message)
            {
                List<Action<byte[]>> actionsList = null;
                if (m_registeredListeners.TryGetValue(id, out actionsList) && actionsList != null)
                {
                    foreach (var action in actionsList)
                    {
                        action(message);
                    }
                }
            }
        }
    }
}
