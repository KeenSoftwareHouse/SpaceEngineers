using ProtoBuf;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
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

        internal static event Action<long> PrepareScenario;
        internal static event Action<ulong> PlayerReadyToStartScenario;
        internal static event Action ClientWorldLoaded;
        internal static event Action<long> StartScenario;
        internal static event Action<int> TimeoutReceived;
        //internal static event Action<long, int> EndScenario;


        static MySyncScenario()
        {
            MySyncLayer.RegisterMessage<PrepareScenarioFromLobbyMsg>(OnPrepareScenarioFromLobby, MyMessagePermissions.FromServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<PlayerReadyToStartScenarioMsg>(OnPlayerReadyToStartScenario, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<PlayerReadyToStartScenarioMsg>(OnPlayerReadyToStartScenario, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterMessage<StartScenarioMsg>(OnStartScenario, MyMessagePermissions.FromServer, MyTransportMessageEnum.Request);
            //MySyncLayer.RegisterMessage<EndScenarioMsg>(OnEndScenario, MyMessagePermissions.FromServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<SetTimeoutMsg>(OnSetTimeout, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
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


            if (PrepareScenario != null)
                PrepareScenario(msg.PreparationStartTime);

            //MyJoinGameHelper.DownloadScenarioWorld(MyMultiplayer.Static);
            //MyGuiScreenLoadSandbox.ScenarioWorldLoaded += MyGuiScreenLoadSandbox_ScenarioWorldLoaded;
        }                    

        private static void MyGuiScreenLoadSandbox_ScenarioWorldLoaded()
        {
            Debug.Assert(!Sync.IsServer);

            //MyGuiScreenLoadSandbox.ScenarioWorldLoaded -= MyGuiScreenLoadSandbox_ScenarioWorldLoaded;

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

    }
}

