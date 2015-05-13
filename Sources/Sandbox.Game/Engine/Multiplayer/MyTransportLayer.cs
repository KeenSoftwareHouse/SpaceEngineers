using ProtoBuf;
using ProtoBuf.Meta;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage;
using VRage.Utils;
using VRage.Compiler;
using VRage.Trace;
using VRageRender;
using VRage.Plugins;

namespace Sandbox.Engine.Multiplayer
{
    public enum MyTransportMessageEnum
    {
        Request = 1,
        Success = 2,
        Failure = 3,
    }

    /// <summary>
    /// Message id, random number from 0 to ushort.max / 4
    /// Must be unique (there's assert for it to)
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct)]
    public class MessageIdAttribute : Attribute
    {
        public readonly ushort MessageId;
        public readonly P2PMessageEnum SendType;

        public MessageIdAttribute(ushort messageId, P2PMessageEnum sendType)
        {
            Debug.Assert(messageId < ushort.MaxValue / 4, "Message ID must be smaller than ushort.MaxValue/4, it's used for Request/Success/Failure flags");
            MessageId = messageId;
            SendType = sendType;
        }
    }

    public interface ITransportCallback
    {
        string MessageType { get; }
        void Receive(ByteStream source, ulong sender, TimeSpan timestamp);
    }

    public interface ITransportCallback<TMsg> : ITransportCallback
    {
        void Write(ByteStream destination, ref TMsg msg);
    }

    class CallbackInfo
    {
        public ITransportCallback Callback;
        public ushort MessageId;
        public P2PMessageEnum SendType;
        public Type MessageType;
        public MyTransportMessageEnum TransportEnum;
    }

    public class NetworkStat
    {
        public int UniqueMessageCount;
        public int MessageCount;
        public int TotalSize;
        public bool IsReliable;
        public void Clear() { TotalSize = MessageCount = UniqueMessageCount = 0; }
    }

    partial class MyTransportLayer
    {
        struct Request<T> { }
        struct Success<T> { }
        struct Failure<T> { }

        struct StatName<T> { public static string Name = typeof(T).Name + ": {0}x; {1} B"; }

        class Buffer
        {
            public byte[] Data;
            public ulong Sender;
        }

        Dictionary<ushort, CallbackInfo> m_callbacks = new Dictionary<ushort, CallbackInfo>();
        static Dictionary<Type, Tuple<ushort, P2PMessageEnum>> TypeMap;


        static MyTransportLayer()
        {
            TypeMap = new Dictionary<Type, Tuple<ushort, P2PMessageEnum>>();

            RegisterFromAssembly(typeof(MyTransportLayer).Assembly);

            if (MyPlugins.GameAssembly != null)
                RegisterFromAssembly(MyPlugins.GameAssembly);

            if (MyPlugins.UserAssembly != null)
                RegisterFromAssembly(MyPlugins.UserAssembly);            
        }

        static void RegisterFromAssembly(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                var attribute = Attribute.GetCustomAttribute(type, typeof(MessageIdAttribute)) as MessageIdAttribute;
                if (attribute != null)
                {
                    TypeMap.Add(type, Tuple.Create((ushort)(attribute.MessageId * 4), attribute.SendType));
                    TypeMap.Add(typeof(Request<>).MakeGenericType(type), Tuple.Create((ushort)(attribute.MessageId * 4 + 1), attribute.SendType));
                    TypeMap.Add(typeof(Success<>).MakeGenericType(type), Tuple.Create((ushort)(attribute.MessageId * 4 + 2), attribute.SendType));
                    TypeMap.Add(typeof(Failure<>).MakeGenericType(type), Tuple.Create((ushort)(attribute.MessageId * 4 + 3), attribute.SendType));
                }
            }
        }

        const ushort FlushMsgId = ushort.MaxValue;

        ByteStream m_sendStream;
        ByteStream m_receiveStream;

        bool m_isBuffering;
        int m_channel;

        List<Buffer> m_buffer;
        AddMessageDelegate m_loopback;

        public Dictionary<string, NetworkStat> SendStats = new Dictionary<string, NetworkStat>();
        public Dictionary<string, NetworkStat> ReceiveStats = new Dictionary<string, NetworkStat>();

        public long ByteCountSent { get; private set; }
        public long ByteCountReceived { get; private set; }

        public bool IsProcessingBuffer { get; private set; }

        /// <summary>
        /// Setting to false will process buffer
        /// </summary>
        public bool IsBuffering
        {
            get
            {
                return m_isBuffering;
            }
            set
            {
                m_isBuffering = value;
                if (m_isBuffering && m_buffer == null)
                {
                    m_buffer = new List<Buffer>();
                }
                else if (!m_isBuffering && m_buffer != null)
                {
                    ProcessBuffer();
                    m_buffer = null;
                }
            }
        }

        public int Channel { get { return m_channel; } }

        public MyTransportLayer(int channel)
        {
            m_channel = channel;
            m_sendStream = new ByteStream(64 * 1024, true);
            m_receiveStream = new ByteStream();

            m_loopback = MyNetworkReader.SetHandler(channel, HandleMessage, MyReceiveQueue.Mode.Timer, Timer);
        }

        private void OnMessageRecieved(byte[] data, uint length, ulong sender)
        {
            HandleMessage(data, (int)length, sender, TimeSpan.Zero);
        }

        TimeSpan Timer()
        {
            //if (Sync.Layer != null)
            //{
                //return Sync.Layer.Interpolation.Timer.CurrentTime;
            //}
            return TimeSpan.Zero;
        }

        public void SendFlush(ulong sendTo)
        {
            m_sendStream.Position = 0;
            m_sendStream.WriteUShort(FlushMsgId);

            SendMessage(m_sendStream, P2PMessageEnum.Reliable, sendTo, m_channel);
        }

        public void SendMessage<TMessage>(ref TMessage msg, List<ulong> recipients, MyTransportMessageEnum messageType, bool includeSelf)
            where TMessage : struct
        {
            if (recipients.Count == 0 && !includeSelf)
                return;

            var msgId = GetId<TMessage>(messageType);
            ITransportCallback<TMessage> c = (ITransportCallback<TMessage>)m_callbacks[msgId.Item1].Callback;
            P2PMessageEnum sendType = msgId.Item2;

            m_sendStream.Position = 0;
            m_sendStream.WriteUShort(msgId.Item1);
            c.Write(m_sendStream, ref msg);

            const int mtu = 1200;
            if ((sendType == P2PMessageEnum.Unreliable || sendType == P2PMessageEnum.UnreliableNoDelay) && m_sendStream.Position > mtu)
            {
                Debug.Fail("Sending unreliable message as reliable, because it's bigger than MTU, message type: " + FindDebugName(msgId.Item1));
                sendType = P2PMessageEnum.Reliable;
            }

            Stats.Network.WriteFormat(StatName<TMessage>.Name, (int)m_sendStream.Position, VRage.Stats.MyStatTypeEnum.CounterSum, 1000, 0);

            if (!MySandboxGame.IsDedicated)
                LogStats(SendStats, TypeNameHelper<TMessage>.Name, (int)m_sendStream.Position, recipients.Count, msgId.Item2);

            if (includeSelf)
            {
                MyTrace.Send(TraceWindow.Multiplayer, "Loopback: " + typeof(TMessage).Name);
                m_loopback(m_sendStream.Data, (int)m_sendStream.Position, Sync.MyId);
            }

            foreach (var sendTo in recipients)
            {
                Debug.Assert(Sync.MultiplayerActive);
                TraceMessage("Sending: ", msg.ToString(), sendTo, m_sendStream.Position, sendType);
                ByteCountSent += m_sendStream.Position;
                SendMessage(m_sendStream, sendType, sendTo, m_channel);
            }
        }

        private static void LogStats(Dictionary<string, NetworkStat> logTo, string name, int size, int recipientCount, P2PMessageEnum messageType)
        {
            NetworkStat stat;
            if (!logTo.TryGetValue(name, out stat))
            {
                stat = new NetworkStat();
                logTo.Add(name, stat);
            }
            stat.TotalSize += size * recipientCount;
            stat.MessageCount += recipientCount;
            stat.IsReliable = messageType == P2PMessageEnum.Reliable || messageType == P2PMessageEnum.ReliableWithBuffering;
            stat.UniqueMessageCount++;
        }

        static void SendMessage(ByteStream sendStream, P2PMessageEnum sendType, ulong sendTo, int channel)
        {
            SendHandler(sendTo, sendStream.Data, (int)sendStream.Position, sendType, channel);
        }

        static void SendHandler(ulong remoteUser, byte[] data, int byteCount, P2PMessageEnum msgType, int channel)
        {
            if (!Peer2Peer.SendPacket(remoteUser, data, byteCount, msgType, channel))
            {
                System.Diagnostics.Debug.Fail("P2P packet send fail");
            }
        }

        void ProcessBuffer()
        {
            try
            {
                IsProcessingBuffer = true;
                foreach (var b in m_buffer)
                {
                    HandleMessage(b.Data, b.Data.Length, b.Sender, TimeSpan.Zero);
                }
            }
            finally
            {
                IsProcessingBuffer = false;
            }
        }
        
        private string GetGroupName(string messageType)
        {
            var c = Char.ToUpperInvariant(messageType.Length > 0 ? messageType[0] : '_');
            if (c <= 'L')
            {
                return "0-L";
            }
            else
            {
                return "M-Z";
            }
        }

        private void HandleMessage(byte[] data, int dataSize, ulong sender, TimeSpan timestamp)
        {
            if (dataSize < sizeof(ushort)) // This would cause crash, message has to contain at least msgId
                return;

            if (sender != Sync.MyId)
                ByteCountReceived += dataSize;

            m_receiveStream.Reset(data, Math.Min(data.Length, dataSize));
            ushort msgId = m_receiveStream.ReadUShort();

            if (IsBuffering)
            {
                if (msgId == FlushMsgId)
                {
                    m_buffer.Clear();
                }
                else
                {
                    var buff = new Buffer();
                    buff.Sender = sender;
                    buff.Data = new byte[dataSize];
                    Array.Copy(data, buff.Data, dataSize);
                    m_buffer.Add(buff);
                }
                return;
            }
            else if (msgId == FlushMsgId)
            {
                if (m_buffer != null)
                    m_buffer.Clear();
                return;
            }

            CallbackInfo info;
            if (m_callbacks.TryGetValue(msgId, out info))
            {
                var handler = info.Callback;

                ProfilerShort.Begin(GetGroupName(handler.MessageType));
                ProfilerShort.Begin(handler.MessageType);

                if (sender != Sync.MyId)
                {
                    if (!MySandboxGame.IsDedicated)
                        LogStats(ReceiveStats, handler.MessageType, dataSize, 1, info.SendType);
                    // TODO: Log stats here
                    //info.Callback.MessageType;
                    //info.SendType;
                    //info.MessageId;
                }

                handler.Receive(m_receiveStream, sender, timestamp);

                ProfilerShort.End();
                ProfilerShort.End();
            }
            else
            {
                Debug.Fail(String.Format("No handler defined for message {0}, of type {1}", msgId, FindDebugName(msgId)));
            }
        }

        public Tuple<ushort, P2PMessageEnum> GetId<TMsg>(MyTransportMessageEnum messageType)
        {
            Type type;
            switch (messageType)
            {
                case MyTransportMessageEnum.Request:
                    type = typeof(Request<TMsg>);
                    break;

                case MyTransportMessageEnum.Success:
                    type = typeof(Success<TMsg>);
                    break;

                case MyTransportMessageEnum.Failure:
                    type = typeof(Failure<TMsg>);
                    break;

                default:
                    throw new InvalidOperationException();
            }

            Tuple<ushort, P2PMessageEnum> val;
            if (!TypeMap.TryGetValue(type, out val))
            {
                Debug.Fail("Unknown message type");
            }
            return val;
        }

        public bool IsRegistered<TMessage>(MyTransportMessageEnum messageType)
            where TMessage : struct
        {
            return m_callbacks.ContainsKey(GetId<TMessage>(messageType).Item1);
        }

        public void Register<TMsg>(ITransportCallback<TMsg> callback, MyTransportMessageEnum messageType)
            where TMsg : struct
        {
            var msgId = GetId<TMsg>(messageType);
            m_callbacks.Add(msgId.Item1, new CallbackInfo() { Callback = callback, MessageId = msgId.Item1, SendType = msgId.Item2, MessageType = typeof(TMsg), TransportEnum = messageType });
        }

        void ReceivedTrace(string messageText, ulong sender, long messageSize)
        {
            TraceMessage("Received: ", messageText, sender, messageSize, P2PMessageEnum.Unreliable);
        }

        [Conditional("DEBUG")]
        void TraceMessage(string text, string messageText, ulong userId, long messageSize, P2PMessageEnum sendType)
        {
            string playerName;
            Sandbox.Game.World.MyNetworkClient player;
            if (MyMultiplayer.Static != null && MyMultiplayer.Static.SyncLayer.Clients.TryGetClient(userId, out player))
            {
                playerName = player.DisplayName;
            }
            else
            {
                playerName = userId.ToString();
            }

            MyTrace.Send(TraceWindow.Multiplayer, text + messageText, playerName + ", " + messageSize + " B");
            if (sendType == P2PMessageEnum.Reliable || sendType == P2PMessageEnum.ReliableWithBuffering)
            {
                MyTrace.Send(TraceWindow.MultiplayerFiltered, text + messageText, playerName + ", " + messageSize + " B");
            }
        }

        public void Unregister<TMessage>(MyTransportMessageEnum messageType)
        {
            m_callbacks.Remove(GetId<TMessage>(messageType).Item1);
        }

        string FindDebugName(ushort msgId)
        {
            foreach (var t in TypeMap)
            {
                if (t.Value.Item1 == msgId)
                    return t.Key.Name;
            }
            return "<TYPE NOT REGISTERED>";
        }

        public void Clear()
        {
            MyNetworkReader.ClearHandler(MyMultiplayer.GameEventChannel);
        }

        public void ClearStats()
        {
            ByteCountReceived = ByteCountSent = 0;
            SendStats.Clear();
            ReceiveStats.Clear();
        }
    }
}
