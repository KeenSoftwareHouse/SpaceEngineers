using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.Engine.Networking;
using SteamSDK;
using VRage.Serialization;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Engine.Utils;
using System.IO;
using VRage.Library.Collections;
using System.Threading;
using VRage.Game.Entity;

namespace Sandbox.Game.Multiplayer
{
    // Should provide loopback when offline
    public partial class MySyncLayer
    {
        static HashSet<IRegistrator> m_registrators = new HashSet<IRegistrator>();

        internal readonly MyTransportLayer TransportLayer;

        internal readonly MyClientCollection Clients;

        private readonly List<ulong> m_recipientsStorage = new List<ulong>();

        private List<ulong> m_recipients
        {
            get
            {
                Debug.Assert(Thread.CurrentThread == MySandboxGame.Static.UpdateThread, "Accessing recipients from wrong thread!");
                return m_recipientsStorage;
            }
        }

        internal bool AutoRegisterGameEvents { get; set; }

        internal MySyncLayer(MyTransportLayer transportLayer)
        {
            TransportLayer = transportLayer;
            Clients = new MyClientCollection();
            AutoRegisterGameEvents = true;
        }

        internal void RegisterClientEvents(MyMultiplayerBase multiplayer)
        {
            multiplayer.ClientJoined += multiplayer_ClientJoined;
            multiplayer.ClientLeft += multiplayer_ClientLeft;

            foreach (var p in multiplayer.Members)
            {
                if (p != Sync.MyId)
                {
                    multiplayer_ClientJoined(p);
                }
            }
        }

        void multiplayer_ClientJoined(ulong steamUserId)
        {
            if(!Clients.HasClient(steamUserId))
                Clients.AddClient(steamUserId);
        }

        void multiplayer_ClientLeft(ulong steamUserId, ChatMemberStateChangeEnum leaveReason)
        {
            Clients.RemoveClient(steamUserId);
        }

        internal void RegisterGameEvents()
        {
            foreach (var reg in m_registrators)
            {
                reg.Register(this);
            }
        }

        internal TSync GetSyncEntity<TSync, TMsg>(long entityId)
            where TSync : MySyncEntity
            where TMsg : struct, IEntityMessage
        {
            MyEntity entity;
            if (MyEntities.TryGetEntityById(entityId, out entity) && !entity.MarkedForClose)
            {
                Debug.Assert(entity.SyncObject != null, "Sync on entity is null");
                Debug.Assert(entity.SyncObject is TSync, string.Format("Sync on entity is different type, expected: {0}, got: {1}", typeof(TSync).Name, entity.SyncObject.GetType().Name));
                return entity.SyncObject as TSync;
            }
            return null;
        }

        public static bool CheckSendPermissions(ulong target, MyMessagePermissions permission)
        {
            bool success;
            switch (permission)
            {

                case MyMessagePermissions.ToServer | MyMessagePermissions.FromServer:
                    success = Sync.ServerId == target || Sync.IsServer;
                    break;

                case MyMessagePermissions.FromServer:
                    success = Sync.IsServer;
                    break;

                case MyMessagePermissions.ToServer:
                    success = Sync.ServerId == target;
                    break;

                default:
                    success = false;
                    break;
            }

            string format = "Permissions check failed, permission: {0}\nIsServer: {1}, SendingToServer: {2}\nMy id: {3}, server id: {4}, target id: {5}";
            Debug.Assert(success, String.Format(format, permission, Sync.IsServer, Sync.ServerId == target, Sync.MyId, Sync.ServerId, target));
            return success;
        }

        public static bool CheckReceivePermissions(ulong sender, MyMessagePermissions permission)
        {
            bool success;
            switch (permission)
            {

                case MyMessagePermissions.ToServer | MyMessagePermissions.FromServer:
                    success = Sync.ServerId == sender || Sync.IsServer;
                    break;

                case MyMessagePermissions.FromServer:
                    success = Sync.ServerId == sender;
                    break;

                case MyMessagePermissions.ToServer:
                    success = Sync.IsServer;
                    break;

                default:
                    success = false;
                    break;
            }

            string format = "Permissions check failed, permission: {0}\nIsServer: {1}, CameFromServer: {2}\nMy id: {3}, server id: {4}, sender id: {5}";
            Debug.Assert(success, String.Format(format, permission, Sync.IsServer, Sync.ServerId == sender, Sync.MyId, Sync.ServerId, sender));
            return success;
        }

        public static void RegisterMessage<TMsg>(Func<MySyncLayer, ITransportCallback<TMsg>> factory, MyTransportMessageEnum messageType = MyTransportMessageEnum.Request)
            where TMsg : struct
        {
            Debug.Assert(Attribute.IsDefined(typeof(TMsg), typeof(MessageIdAttribute)), "Message has no MessageId");
            m_registrators.Add(new Registrator<TMsg>((layer) => factory(layer), messageType));
        }

        public static void RegisterMessage<TMsg>(MessageCallbackTime<TMsg> callback, MyMessagePermissions permissions, MyTransportMessageEnum messageType = MyTransportMessageEnum.Request, ISerializer<TMsg> serializer = null)
            where TMsg : struct
        {
            RegisterMessage<TMsg>((layer) => new MyCallbackBase<TMsg>(layer, callback, permissions, serializer ?? GetSerializer<TMsg>()), messageType);
        }

        public static void RegisterMessage<TMsg>(MessageCallback<TMsg> callback, MyMessagePermissions permissions, MyTransportMessageEnum messageType = MyTransportMessageEnum.Request, ISerializer<TMsg> serializer = null)
            where TMsg : struct
        {
            RegisterMessage<TMsg>((layer) => new MyCallbackBase<TMsg>(layer, new MyCallbackTime<TMsg>(callback).Handle, permissions, serializer ?? GetSerializer<TMsg>()), messageType);
        }

        public static void RegisterEntityMessage<TSync, TMsg>(MessageCallback<TSync, TMsg> callback, MyMessagePermissions permissions, MyTransportMessageEnum messageType = MyTransportMessageEnum.Request, ISerializer<TMsg> serializer = null)
            where TSync : MySyncEntity
            where TMsg : struct, IEntityMessage
        {
            RegisterMessage<TMsg>((layer) => new MyCallbackBase<TMsg>(layer, new MyCallbackEntity<TSync, TMsg>(layer, callback).Handle, permissions, serializer ?? GetSerializer<TMsg>()), messageType);
        }

        public void RegisterMessageImmediate<TMsg>(MessageCallback<TMsg> callback, MyMessagePermissions permissions, MyTransportMessageEnum messageType = MyTransportMessageEnum.Request, ISerializer<TMsg> serializer = null)
            where TMsg : struct
        {
            TransportLayer.Register<TMsg>(new MyCallbackBase<TMsg>(this, new MyCallbackTime<TMsg>(callback).Handle, permissions, serializer ?? GetSerializer<TMsg>()), messageType);
        }

        internal static ISerializer<TMsg> GetSerializer<TMsg>()
        {
            if (Attribute.IsDefined(typeof(TMsg), typeof(ProtoContractAttribute)))
            {
#if !XB1 // XB1_NOPROTOBUF
                return CreateProto<TMsg>();
#else // XB1
                System.Diagnostics.Debug.Assert(false);
                return null;
#endif // XB1
            }
            else
            {
                return CreateBlittable<TMsg>();
            }
        }

#if !XB1 // XB1_NOPROTOBUF
        // Separate methods for each serializer, don't want to to run static constructor on both
        static ISerializer<TMsg> CreateProto<TMsg>()
        {
            return DefaultProtoSerializer<TMsg>.Default;
        }
#endif // !XB1

        // Separate methods for each serializer, don't want to to run static constructor on both
        static ISerializer<TMsg> CreateBlittable<TMsg>()
        {
            return BlitSerializer<TMsg>.Default;
        }

        public void SendMessageToServer<TMsg>(ref TMsg msg, MyTransportMessageEnum messageType = MyTransportMessageEnum.Request)
            where TMsg : struct
        {
            SendMessage(ref msg, Sync.ServerId, messageType);
        }

        public void SendMessage<TMsg>(ref TMsg msg, ulong sendTo, MyTransportMessageEnum messageType = MyTransportMessageEnum.Request)
            where TMsg : struct
        {
            if (sendTo == Sync.MyId)
            {
                m_recipients.Clear();
                TransportLayer.SendMessage(ref msg, m_recipients, messageType, true);
            }
            else
            {
                m_recipients.Clear();
                m_recipients.Add(sendTo);
                TransportLayer.SendMessage(ref msg, m_recipients, messageType, false);
            }
        }

        public void SendMessageToAllButOne<TMsg>(ref TMsg msg, ulong dontSendTo, MyTransportMessageEnum messageType = MyTransportMessageEnum.Request)
            where TMsg : struct
        {
            m_recipients.Clear();
            if (Sync.MultiplayerActive)
            {
                for (int i = 0; i < MyMultiplayer.Static.MemberCount; i++)
                {
                    var memberId = MyMultiplayer.Static.GetMemberByIndex(i);
                    if (memberId != dontSendTo && memberId != Sync.MyId)
                    {
                        m_recipients.Add(memberId);
                    }
                }
            }
            TransportLayer.SendMessage(ref msg, m_recipients, messageType, false);
        }

        public void SendMessageToAll<TMsg>(ref TMsg msg, MyTransportMessageEnum messageType = MyTransportMessageEnum.Request)
            where TMsg : struct
        {
            Debug.Assert(Sync.IsServer, "sending message to all not from server");
            SendMessageToRecipients<TMsg>(ref msg, messageType, false);
        }

        public void SendMessageToServerAndSelf<TMsg>(ref TMsg msg, MyTransportMessageEnum messageType = MyTransportMessageEnum.Request)
            where TMsg : struct
        {
            m_recipients.Clear();
            if (!Sync.IsServer)
            {
                m_recipients.Add(Sync.ServerId);
            }
            TransportLayer.SendMessage(ref msg, m_recipients, messageType, true);
        }

        /// <summary>
        /// Message is not proccessed immediately localy! Its enqueued, so for order/changes sensitive code you need to perform actions localy on your own.
        /// </summary>
        public void SendMessageToAllAndSelf<TMsg>(ref TMsg msg, MyTransportMessageEnum messageType = MyTransportMessageEnum.Request)
            where TMsg : struct
        {
            Debug.Assert(Sync.IsServer, "sending message to all not from server");
            SendMessageToRecipients(ref msg, messageType, true);
        }

        private void SendMessageToRecipients<TMsg>(ref TMsg msg, MyTransportMessageEnum messageType, bool includeSelf)
            where TMsg : struct
        {
            m_recipients.Clear();
            if (Sync.MultiplayerActive)
            {
                for (int i = 0; i < MyMultiplayer.Static.MemberCount; i++)
                {
                    var memberId = MyMultiplayer.Static.GetMemberByIndex(i);
                    if (memberId != Sync.MyId)
                    {
                        m_recipients.Add(memberId);
                    }
                }
            }
            if (TransportLayer != null)
            {
                TransportLayer.SendMessage(ref msg, m_recipients, messageType, includeSelf);
            }
        }
    }
}
