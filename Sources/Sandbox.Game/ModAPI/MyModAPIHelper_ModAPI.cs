using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.ModAPI
{ 
    public static class MyModAPIHelper
    {
        public static void OnSessionLoaded()
        {
            MyAPIGateway.Session = MySession.Static;
            MyAPIGateway.Entities = new MyEntitiesHelper_ModAPI();
            MyAPIGateway.Players = Sync.Players;
            MyAPIGateway.CubeBuilder = MyCubeBuilder.Static;
            MyAPIGateway.TerminalActionsHelper = MyTerminalControlFactoryHelper.Static;
            MyAPIGateway.Utilities = MyAPIUtilities.Static;
            MyAPIGateway.Parallel = MyParallelTask.Static;
            MyAPIGateway.Multiplayer = MyMultiplayer.Static;
            MyAPIGateway.PrefabManager = MyPrefabManager.Static;
        }
        [PreloadRequired]
        public class MyMultiplayerSyncObject
        {
            [ProtoBuf.ProtoContract]
            [MessageIdAttribute(16295, P2PMessageEnum.Reliable)]
            struct CustomModMsg
            {
                [ProtoBuf.ProtoMember]
                public ushort ModID;
                [ProtoBuf.ProtoMember]
                public byte[] Message;
            }
            [ProtoBuf.ProtoContract]
            [MessageIdAttribute(16296, P2PMessageEnum.Unreliable)]
            struct CustomModMsgUnreliable
            {
                [ProtoBuf.ProtoMember]
                public ushort ModID;
                [ProtoBuf.ProtoMember]
                public byte[] Message;
            }

            static Dictionary<ushort, List<Action<byte[]>>> m_registeredListeners = new Dictionary<ushort, List<Action<byte[]>>>();

            static MyMultiplayerSyncObject()
            {
                MySyncLayer.RegisterMessage<CustomModMsg>(ModMessageRecieved, MyMessagePermissions.Any, MyTransportMessageEnum.Success);
                MySyncLayer.RegisterMessage<CustomModMsgUnreliable>(ModMessageRecievedUnreliable, MyMessagePermissions.Any, MyTransportMessageEnum.Success);
            }

            static void ModMessageRecieved(ref CustomModMsg msg, MyNetworkClient sender)
            {
                List<Action<byte[]>> actionsList = null;
                if (m_registeredListeners.TryGetValue(msg.ModID, out actionsList) && actionsList != null)
                {
                    foreach (var action in actionsList)
                    {
                        action(msg.Message);
                    }
                }
            }
            static void ModMessageRecievedUnreliable(ref CustomModMsgUnreliable msg, MyNetworkClient sender)
            {
                List<Action<byte[]>> actionsList = null;
                if (m_registeredListeners.TryGetValue(msg.ModID, out actionsList) && actionsList != null)
                {
                    foreach (var action in actionsList)
                    {
                        action(msg.Message);
                    }
                }
            }

            public void SendMessageTo(ushort id, byte[] message, ulong recipient, bool reliable)
            {
                if (reliable)
                {
                    CustomModMsg msg = new CustomModMsg();
                    msg.ModID = id;
                    msg.Message = message;
                    Sync.Layer.SendMessage(ref msg, recipient, MyTransportMessageEnum.Success);
                }
                else
                {
                    CustomModMsgUnreliable msg = new CustomModMsgUnreliable();
                    msg.ModID = id;
                    msg.Message = message;
                    Sync.Layer.SendMessage(ref msg, recipient, MyTransportMessageEnum.Success);
                }
            }

            public void SendMessageToOthers(ushort id, byte[] message, bool reliable)
            {
                if (reliable)
                {
                    CustomModMsg msg = new CustomModMsg();
                    msg.ModID = id;
                    msg.Message = message;
                    Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
                }
                else
                {
                    CustomModMsgUnreliable msg = new CustomModMsgUnreliable();
                    msg.ModID = id;
                    msg.Message = message;
                    Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
                }
            }

            public void SendMessageToServer(ushort id, byte[] message, bool reliable)
            {
                if (reliable)
                {
                    CustomModMsg msg = new CustomModMsg();
                    msg.ModID = id;
                    msg.Message = message;
                    Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Success);
                }
                else
                {
                    CustomModMsgUnreliable msg = new CustomModMsgUnreliable();
                    msg.ModID = id;
                    msg.Message = message;
                    Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Success);
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

        }

        public class MyMultiplayer : IMyMultiplayer
        {

            public static MyMultiplayer Static;
            static MyMultiplayerSyncObject SyncObject = null;
            const int MAX_MESSAGE_SIZE = 4096;

            static MyMultiplayer()
            {
                Static = new MyMultiplayer();
                SyncObject = new MyMultiplayerSyncObject();
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

            public void SendEntitiesCreated(List<Sandbox.Common.ObjectBuilders.MyObjectBuilder_EntityBase> objectBuilders)
            {
                MySyncCreate.SendEntitiesCreated(objectBuilders);
            }

            public bool SendMessageToServer(ushort id, byte[] message, bool reliable)
            {
                if (message.Length > MAX_MESSAGE_SIZE)
                {
                    return false;
                }
                SyncObject.SendMessageToServer(id, message, reliable);
                return true;
            }

            public bool SendMessageToOthers(ushort id, byte[] message, bool reliable)
            {
                if (message.Length > MAX_MESSAGE_SIZE)
                {
                    return false;
                }
                SyncObject.SendMessageToOthers(id, message, reliable);
                return true;
            }

            public bool SendMessageTo(ushort id, byte[] message, ulong recipient, bool reliable)
            {
                if (message.Length > MAX_MESSAGE_SIZE)
                {
                    return false;
                }
                SyncObject.SendMessageTo(id, message, recipient, reliable);
                return true;
            }
       
            public void RegisterMessageHandler(ushort id, Action<byte[]> messageHandler)
            {
                SyncObject.RegisterMessageHandler(id, messageHandler);
            }

            public void UnregisterMessageHandler(ushort id, Action<byte[]> messageHandler)
            {
                SyncObject.UnregisterMessageHandler(id, messageHandler);       
            }
        }
    }
}
