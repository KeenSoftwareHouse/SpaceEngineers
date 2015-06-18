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

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    public static class MySyncScenario
    {
        [MessageId(5400, P2PMessageEnum.Reliable)]
        [ProtoContract]
        struct  PrepareScenarioFromLobbyMsg
        {
            [ProtoMember]
            public ulong ServerPlayerSteamId;
            [ProtoMember]
            public long ServerPlayerIdentityId;
            [ProtoMember]
            public long PreparationStartTime;
        }

        [MessageId(5401, P2PMessageEnum.Reliable)]
        [ProtoContract]
        struct PlayerReadyToStartScenarioMsg
        {
            [ProtoMember]
            public ulong PlayerSteamId;
        }

        [MessageId(5402, P2PMessageEnum.Reliable)]
        [ProtoContract]
        struct StartScenarioMsg
        {
            [ProtoMember]
            public ulong ServerPlayerSteamId;
            [ProtoMember]
            public long ServerPlayerIdentityId;
            [ProtoMember]
            public long GameStartTime;

        }

        [MessageId(5403, P2PMessageEnum.Reliable)]
        [ProtoContract]
        struct SetTimeoutMsg
        {
            [ProtoMember]
            public int Index;

        }

        [MessageId(5404, P2PMessageEnum.Reliable)]
        [ProtoContract]
        struct AskInfoMsg
        {
            [ProtoMember]
            public byte dummy;
        }

        [MessageId(5405, P2PMessageEnum.Reliable)]
        [ProtoContract]
        struct AnswerInfoMsg
        {
            [ProtoMember]
            public bool IsRunning;

            [ProtoMember]
            public bool CanJoin;

        }

        [MessageId(5406, P2PMessageEnum.Reliable)]
        [ProtoContract]
        struct SetJoinRunningMsg
        {
            [ProtoMember]
            public bool CanJoin;
        }

        internal static event Action<bool, bool> InfoAnswer;
        internal static event Action<long> PrepareScenario;
        internal static event Action<ulong> PlayerReadyToStartScenario;
        internal static event Action ClientWorldLoaded;
        internal static event Action<long> StartScenario;
        internal static event Action<int> TimeoutReceived;
        internal static event Action<bool> CanJoinRunningReceived;
        //internal static event Action<long, int> EndScenario;
        
        static MySyncScenario()
        {
            MySyncLayer.RegisterMessage<AskInfoMsg>(OnAskInfo, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<AnswerInfoMsg>(OnAnswerInfo, MyMessagePermissions.FromServer, MyTransportMessageEnum.Request);

            MySyncLayer.RegisterMessage<PrepareScenarioFromLobbyMsg>(OnPrepareScenarioFromLobby, MyMessagePermissions.FromServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<PlayerReadyToStartScenarioMsg>(OnPlayerReadyToStartScenario, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<PlayerReadyToStartScenarioMsg>(OnPlayerReadyToStartScenario, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterMessage<StartScenarioMsg>(OnStartScenario, MyMessagePermissions.FromServer, MyTransportMessageEnum.Request);
            //MySyncLayer.RegisterMessage<EndScenarioMsg>(OnEndScenario, MyMessagePermissions.FromServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<SetTimeoutMsg>(OnSetTimeout, MyMessagePermissions.FromServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<SetJoinRunningMsg>(OnSetJoinRunning, MyMessagePermissions.FromServer, MyTransportMessageEnum.Request);
        }

        //client connected, asks for latest info (like if the game is already running and gui should be used accordingly)
        internal static void AskInfo()
        {
            var msg = new AskInfoMsg();
            Sync.Layer.SendMessageToServer(ref msg);
        }
        private static void OnAskInfo(ref AskInfoMsg msg, MyNetworkClient sender)
        {
            var answer = new AnswerInfoMsg();
            answer.IsRunning = MyMultiplayer.Static.ScenarioStartTime > DateTime.MinValue;
            answer.CanJoin = !answer.IsRunning || MySession.Static.Settings.CanJoinRunning;
            Sync.Layer.SendMessage(ref answer, sender.SteamUserId);

            var timeoutMsg = new SetTimeoutMsg();
            timeoutMsg.Index = (int)MyGuiScreenScenarioMpBase.Static.TimeoutCombo.GetSelectedIndex();
            Sync.Layer.SendMessage(ref timeoutMsg, sender.SteamUserId);

            var outMsg = new SetJoinRunningMsg();
            outMsg.CanJoin = MySession.Static.Settings.CanJoinRunning;
            Sync.Layer.SendMessage(ref outMsg, sender.SteamUserId);
        }

        private static void OnAnswerInfo(ref AnswerInfoMsg msg, MyNetworkClient sender)
        {
            if (InfoAnswer != null)
                InfoAnswer(msg.IsRunning, msg.CanJoin);
        }

        /// <summary>
        /// Send message to all clients to prepare Scenario (Start Scenario has been pressed in lobby screen, clients will download world).
        /// </summary>
        internal static void PrepareScenarioFromLobby(long preparationStartTime)
        {
            Debug.Assert(Sync.IsServer);

            if (!Sync.IsServer)
                return;

            var msg = new PrepareScenarioFromLobbyMsg();
            msg.ServerPlayerSteamId = MySteam.UserId;
            msg.ServerPlayerIdentityId = MySession.LocalPlayerId;
            msg.PreparationStartTime = preparationStartTime;

            Sync.Layer.SendMessageToAll(ref msg);
        }

        private static void OnPrepareScenarioFromLobby(ref PrepareScenarioFromLobbyMsg msg, MyNetworkClient sender)
        {
            Debug.Assert(!Sync.IsServer);
            OnPrepareScenarioFromLobby(msg.PreparationStartTime);
        }
        public static void OnPrepareScenarioFromLobby(long PrepStartTime)
        {
            if (PrepareScenario != null)
                PrepareScenario(PrepStartTime);
            MyJoinGameHelper.DownloadScenarioWorld(MyMultiplayer.Static);
            MyGuiScreenLoadSandbox.ScenarioWorldLoaded += MyGuiScreenLoadSandbox_ScenarioWorldLoaded;
        }                    

        private static void MyGuiScreenLoadSandbox_ScenarioWorldLoaded()
        {
            Debug.Assert(!Sync.IsServer);

            MyGuiScreenLoadSandbox.ScenarioWorldLoaded -= MyGuiScreenLoadSandbox_ScenarioWorldLoaded;

            var msg = new PlayerReadyToStartScenarioMsg();
            msg.PlayerSteamId = MySteam.UserId;

            Sync.Layer.SendMessageToServer(ref msg);

            if (ClientWorldLoaded != null)
                ClientWorldLoaded();
        }

        private static void OnPlayerReadyToStartScenario(ref PlayerReadyToStartScenarioMsg msg, MyNetworkClient sender)
        {
            Debug.Assert(Sync.IsServer);

            if (PlayerReadyToStartScenario != null)
                PlayerReadyToStartScenario(msg.PlayerSteamId);

            if (Sync.IsServer)
                Sync.Layer.SendMessageToAll(ref msg);
        }

        //internal static void StartScenarioRequest()
        //{
        //    Debug.Assert(Sync.IsServer);

        //    if (!Sync.IsServer)
        //        return;

        //    var msg = new StartScenarioMsg();
        //    msg.ServerPlayerSteamId = MySteam.UserId;
        //    msg.ServerPlayerIdentityId = MySession.LocalPlayerId;

        //    Sync.Layer.SendMessageToAll(ref msg);
        //}

        internal static void StartScenarioRequest(ulong playerSteamId, long gameStartTime)
        {
            Debug.Assert(Sync.IsServer);

            var msg = new StartScenarioMsg();
            msg.ServerPlayerSteamId = MySteam.UserId;
            msg.ServerPlayerIdentityId = MySession.LocalPlayerId;
            msg.GameStartTime = gameStartTime;

            Sync.Layer.SendMessage(ref msg, playerSteamId);
        }

        private static void OnStartScenario(ref StartScenarioMsg msg, MyNetworkClient sender)
        {
            Debug.Assert(!Sync.IsServer);

            if (StartScenario != null)
                StartScenario(msg.GameStartTime);
        }


        public static void SetTimeout(int index)
        {
            Debug.Assert(Sync.IsServer);

            var msg = new SetTimeoutMsg();
            msg.Index=index;
            Sync.Layer.SendMessageToAll(ref msg);
        }
        private static void OnSetTimeout(ref SetTimeoutMsg msg, MyNetworkClient sender)
        {
            Debug.Assert(!Sync.IsServer);

            if (TimeoutReceived != null)
                TimeoutReceived(msg.Index);
        }

        public static void SetJoinRunning(bool canJoin)
        {
            Debug.Assert(Sync.IsServer);

            var msg = new SetJoinRunningMsg();
            msg.CanJoin = canJoin;
            Sync.Layer.SendMessageToAll(ref msg);
        }
        private static void OnSetJoinRunning(ref SetJoinRunningMsg msg, MyNetworkClient sender)
        {
            Debug.Assert(!Sync.IsServer);

            if (CanJoinRunningReceived != null)
                CanJoinRunningReceived(msg.CanJoin);
        }

    }
}

