using ProtoBuf;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens;
using Sandbox.Game.World;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Network;

namespace Sandbox.Game.Multiplayer
{
    [StaticEventOwner]
    [PreloadRequired]
    public static class MySyncScenario
    {
        internal static event Action<bool, bool> InfoAnswer;
        internal static event Action<long> PrepareScenario;
        internal static event Action<ulong> PlayerReadyToStartScenario;
        internal static event Action ClientWorldLoaded;
        internal static event Action<long> StartScenario;
        internal static event Action<int> TimeoutReceived;
        internal static event Action<bool> CanJoinRunningReceived;
        //internal static event Action<long, int> EndScenario;

        //client connected, asks for latest info (like if the game is already running and gui should be used accordingly)
        internal static void AskInfo()
        {
            MyMultiplayer.RaiseStaticEvent(s => MySyncScenario.OnAskInfo);
        }

        [Event, Reliable, Server]
        private static void OnAskInfo()
        {
            EndpointId sender;
            if (MyEventContext.Current.IsLocallyInvoked)
                sender = new EndpointId(Sync.MyId);
            else
                sender = MyEventContext.Current.Sender;

            bool isRunning = MyMultiplayer.Static.ScenarioStartTime > DateTime.MinValue;
            bool canJoin = !isRunning || MySession.Static.Settings.CanJoinRunning;
            MyMultiplayer.RaiseStaticEvent(s => MySyncScenario.OnAnswerInfo, isRunning, canJoin, sender);

            int index = (int)MyGuiScreenScenarioMpBase.Static.TimeoutCombo.GetSelectedIndex();
            MyMultiplayer.RaiseStaticEvent(s => MySyncScenario.OnSetTimeoutClient, index, sender);

            bool canJoinRunning = MySession.Static.Settings.CanJoinRunning;
            MyMultiplayer.RaiseStaticEvent(s => MySyncScenario.OnSetJoinRunningClient, canJoinRunning, sender);
        }

        [Event, Reliable, Client]
        private static void OnAnswerInfo(bool isRunning, bool canJoin)
        {
            if (InfoAnswer != null)
                InfoAnswer(isRunning, canJoin);
        }

        [Event, Reliable, Client]
        private static void OnSetTimeoutClient(int index)
        {
            OnSetTimeout(index);
        }

        [Event, Reliable, Client]
        private static void OnSetJoinRunningClient(bool canJoin)
        {
            OnSetJoinRunning(canJoin);
        }

        /// <summary>
        /// Send message to all clients to prepare Scenario (Start Scenario has been pressed in lobby screen, clients will download world).
        /// </summary>
        internal static void PrepareScenarioFromLobby(long preparationStartTime)
        {
            Debug.Assert(Sync.IsServer);

            if (!Sync.IsServer)
                return;

            MyMultiplayer.RaiseStaticEvent(s => MySyncScenario.OnPrepareScenarioFromLobby, preparationStartTime);
        }

        [Event, Reliable, Broadcast]
        public static void OnPrepareScenarioFromLobby(long PrepStartTime)
        {
            if (PrepareScenario != null)
                PrepareScenario(PrepStartTime);

            MyJoinGameHelper.DownloadScenarioWorld(MyMultiplayer.Static);
            MySessionLoader.ScenarioWorldLoaded += MyGuiScreenLoadSandbox_ScenarioWorldLoaded;
        }                    

        private static void MyGuiScreenLoadSandbox_ScenarioWorldLoaded()
        {
            Debug.Assert(!Sync.IsServer);

            MySessionLoader.ScenarioWorldLoaded -= MyGuiScreenLoadSandbox_ScenarioWorldLoaded;

            MyMultiplayer.RaiseStaticEvent(s => MySyncScenario.OnPlayerReadyToStartScenario, Sync.MyId);

            if (ClientWorldLoaded != null)
                ClientWorldLoaded();
        }

        // NOTE: Before the converstion to new MP events, at rev 68662, the code was really
        // confusing. By static analysis and verify on specifc behavior with debugging, this
        // is really a server only event and *not* client/broadcast
        [Event, Reliable, Server]
        private static void OnPlayerReadyToStartScenario(ulong playerSteamId)
        {
            if (PlayerReadyToStartScenario != null)
                PlayerReadyToStartScenario(playerSteamId);
        }

        //internal static void StartScenarioRequest()
        //{
        //    Debug.Assert(Sync.IsServer);

        //    if (!Sync.IsServer)
        //        return;

        //    var msg = new StartScenarioMsg();
        //    msg.ServerPlayerSteamId = Sync.MyId;
        //    msg.ServerPlayerIdentityId = MySession.Static.LocalPlayerId;

        //    Sync.Layer.SendMessageToAll(ref msg);
        //}

        internal static void StartScenarioRequest(ulong playerSteamId, long gameStartTime)
        {
            Debug.Assert(Sync.IsServer);

            MyMultiplayer.RaiseStaticEvent(s => MySyncScenario.OnStartScenario, gameStartTime, new EndpointId(playerSteamId));
        }

        [Event, Reliable, Client]
        private static void OnStartScenario(long gameStartTime)
        {
            if (StartScenario != null)
                StartScenario(gameStartTime);
        }

        public static void SetTimeout(int index)
        {
            Debug.Assert(Sync.IsServer);
            MyMultiplayer.RaiseStaticEvent(s => MySyncScenario.OnSetTimeoutBroadcast, index);
        }

        [Event, Reliable, Broadcast]
        private static void OnSetTimeoutBroadcast(int index)
        {
            OnSetTimeout(index);
        }

        private static void OnSetTimeout(int index)
        {
            if (TimeoutReceived != null)
                TimeoutReceived(index);
        }

        public static void SetJoinRunning(bool canJoin)
        {
            Debug.Assert(Sync.IsServer);
            MyMultiplayer.RaiseStaticEvent(s => MySyncScenario.OnSetJoinRunningBroadcast, canJoin);
        }

        [Event, Reliable, Broadcast]
        private static void OnSetJoinRunningBroadcast(bool canJoin)
        {
            OnSetJoinRunning(canJoin);
        }

        private static void OnSetJoinRunning(bool canJoin)
        {
            if (CanJoinRunningReceived != null)
                CanJoinRunningReceived(canJoin);
        }

    }
}

