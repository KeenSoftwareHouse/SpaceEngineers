using VRage.Serialization;
#if !XB1 // XB1_NOPROTOBUF
using ProtoBuf.Meta;
#endif // !XB1
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Trace;
using Sandbox.Common.ObjectBuilders;
using VRage.Compiler;
using VRage.ObjectBuilders;
using VRage.Game.Entity;
using VRage.Library.Utils;

namespace Sandbox.Game.Multiplayer
{
    public interface IEntityMessage
    {
        long GetEntityId();
    }

    public static class EntityMessageExtensions
    {
        public static string GetEntityText(this IEntityMessage msg)
        {
            MyEntity entity;
            if (Sandbox.Game.Entities.MyEntities.TryGetEntityById(msg.GetEntityId(), out entity))
            {
                return entity.ToString();
            }
            return "null";
        }
    }

    public delegate void MessageCallbackTime<TMsg>(ref TMsg message, MyNetworkClient sender, MyTimeSpan timestamp)
        where TMsg : struct;

    public delegate void MessageCallback<TMsg>(ref TMsg message, MyNetworkClient sender)
        where TMsg : struct;

    public delegate void MessageCallback<TSync, TMsg>(TSync syncObject, ref TMsg message, MyNetworkClient sender)
        where TSync : MySyncEntity
        where TMsg : struct, IEntityMessage;

    [Flags]
    public enum MyMessagePermissions
    {
        FromServer = 1,
        ToServer = 2,
    }

    public partial class MySyncLayer
    {
        interface IRegistrator
        {
            void Register(MySyncLayer layer);
            void Unregister(MySyncLayer layer);
        }

#if !XB1 // XB1_NOPROTOBUF
        class DefaultProtoSerializer<T>
        {
            public static readonly ProtoSerializer<T> Default = new ProtoSerializer<T>(MyObjectBuilderSerializer.Serializer);
        }
#endif // !XB1

        class Registrator<TMsg> : IRegistrator
            where TMsg : struct
        {
            public readonly MyTransportMessageEnum MessageType;
            public readonly Func<MySyncLayer, ITransportCallback<TMsg>> Factory;

            public Registrator(Func<MySyncLayer, ITransportCallback<TMsg>> factory, MyTransportMessageEnum messageType)
            {
                this.MessageType = messageType;
                this.Factory = factory;
            }

            public void Register(MySyncLayer layer)
            {
                try
                {
                    layer.TransportLayer.Register<TMsg>(Factory(layer), MessageType);
                }
                catch (Exception e)
                {
                    var msgId = layer.TransportLayer.GetId<TMsg>(MessageType);
                    string msg = String.Format("Message registration failed, message type: {0}, message id: {1}", typeof(TMsg).Name, msgId != null ? msgId.Item1.ToString() : "-");

                    throw new InvalidOperationException(msg, e);
                }
            }

            public void Unregister(MySyncLayer layer)
            {
                layer.TransportLayer.Unregister<TMsg>(MessageType);
            }
        }

        internal class MyCallbackBase<TMsg> : ITransportCallback<TMsg>
            where TMsg : struct
        {
            internal readonly MySyncLayer Layer;
            internal readonly MyMessagePermissions Permission;
            internal readonly ISerializer<TMsg> Serializer;
            internal readonly MessageCallbackTime<TMsg> Callback;

            internal MyCallbackBase(MySyncLayer layer, MessageCallbackTime<TMsg> callback, MyMessagePermissions permission, ISerializer<TMsg> serializer)
            {
                this.Layer = layer;
                this.Permission = permission;
                this.Serializer = serializer;
                this.Callback = callback;
            }

            void OnHandle(ref TMsg msg, MyNetworkClient player, MyTimeSpan timestamp)
            {
                Callback(ref msg, player, timestamp);
            }

            void Handle(ref TMsg msg, ulong sender, MyTimeSpan timestamp)
            {              
                MyNetworkClient player;
                bool playerFound = Layer.Clients.TryGetClient(sender, out player);
                bool permissionsOk = MySyncLayer.CheckReceivePermissions(sender, Permission);

             /*   if (!playerFound && msg is ConnectedClientDataMsg)
                {
                    var m = (ConnectedClientDataMsg)(object)msg;
                    player = Layer.Clients.AddClient(sender);
                    playerFound = true;
                }*/

                //TODO: This should be ok if client loads the scene, buffers another player messages
                //and during that time is that player kicked
                //Debug.Assert(playerFound, "Player not found");

                if (playerFound && permissionsOk)
                {
                    OnHandle(ref msg, player, timestamp);
                }
            }

            void ITransportCallback<TMsg>.Write(ByteStream destination, ref TMsg msg)
            {
                Serializer.Serialize(destination, ref msg);
            }

            void ITransportCallback.Receive(ByteStream source, ulong sender, MyTimeSpan timestamp)
            {
                // TODO: msg pool as member in this class?

                TMsg msg;
                try
                {
                    Serializer.Deserialize(source, out msg);
                    MyTrace.Send(TraceWindow.Multiplayer, "Received: " + msg.ToString(), sender + ", " + source.Position + " B");
                }
                catch (Exception e)
                {
                    // Catch, add more info (what message) and write to log
                    MySandboxGame.Log.WriteLine(new Exception(String.Format("Error deserializing '{0}', message size '{1}'", typeof(TMsg).Name, source.Length), e));
                    return;
                }

                Handle(ref msg, sender, timestamp);
            }

            string ITransportCallback.MessageType
            {
                get { return TypeNameHelper<TMsg>.Name; }
            }
        }

        class MyCallbackTime<TMsg>
            where TMsg : struct
        {
            public readonly MessageCallback<TMsg> Callback;

            public MyCallbackTime(MessageCallback<TMsg> callback)
            {
                this.Callback = callback;
            }

            public void Handle(ref TMsg msg, MyNetworkClient player, MyTimeSpan timestamp)
            {
                Callback(ref msg, player);
            }
        }

        class MyCallbackEntity<TSync, TMsg>
            where TSync : MySyncEntity
            where TMsg : struct, IEntityMessage
        {
            public readonly MessageCallback<TSync, TMsg> Callback;
            public readonly MySyncLayer Layer;

            public MyCallbackEntity(MySyncLayer layer, MessageCallback<TSync, TMsg> callback)
            {
                this.Callback = callback;
                this.Layer = layer;
            }

            public void Handle(ref TMsg msg, MyNetworkClient player, MyTimeSpan timestamp)
            {
                TSync sync = Layer.GetSyncEntity<TSync, TMsg>(msg.GetEntityId());
                if (sync != null)
                {
                    Callback(sync, ref msg, player);
                }
            }
        }

        //class MyCallback<TMsg> : MyCallbackBase<TMsg>
        //    where TMsg : struct
        //{
        //    public readonly MessageCallback<TMsg> Callback;

        //    public MyCallback(MySyncLayer layer, MessageCallback<TMsg> callback, MyMessagePermissions permission, ISerializer<TMsg> serializer)
        //        : base(layer, permission, serializer)
        //    {
        //        Callback = callback;
        //    }

        //    protected override void OnHandle(ref TMsg msg, MyPlayer player)
        //    {
        //        Callback(ref msg, player);
        //    }
        //}

        //class MyCallback<TSync, TMsg> : MyCallbackBase<TMsg>
        //    where TSync : MySyncEntity
        //    where TMsg : struct, IEntityMessage
        //{
        //    public readonly MessageCallback<TSync, TMsg> Callback;

        //    public MyCallback(MySyncLayer layer, MessageCallback<TSync, TMsg> callback, MyMessagePermissions permission, ISerializer<TMsg> serializer)
        //        : base(layer, permission, serializer)
        //    {
        //        Callback = callback;
        //    }

        //    protected override void OnHandle(ref TMsg msg, MyPlayer player)
        //    {
        //        TSync sync = Layer.GetSyncEntity<TSync, TMsg>(msg.GetEntityId());
        //        if (sync != null)
        //        {
        //            Callback(sync, ref msg, player);
        //        }
        //    }
        //}
    }
}
