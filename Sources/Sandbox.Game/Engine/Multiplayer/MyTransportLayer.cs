using Sandbox.Engine.Networking;
using Sandbox.Game.Multiplayer;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using VRage;
using VRage.Compiler;
using VRage.Trace;
using VRage.Plugins;
using VRage.Library.Collections;
using VRage.Network;
using VRage.Library.Utils;
using Sandbox.Game.World;
using VRage.Profiler;
using VRageRender.Utils;

namespace Sandbox.Engine.Multiplayer
{
    public enum MyTransportMessageEnum
    {
        Request = 1,
        Success = 2,
        Failure = 3,
    }

    /// <summary>
    /// Message id, random number from 0 to 16383
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
        void Receive(ByteStream source, ulong sender, MyTimeSpan timestamp);
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
#if XB1 // XB1_ALLINONEASSEMBLY
        private static bool m_registered = false;
#endif // XB1

        struct Request<T> { }
        struct Success<T> { }
        struct Failure<T> { }

        struct StatName<T> { public static readonly string Name = typeof(T).Name + ": {0}x; {1} B"; }

        class Buffer
        {
            public byte[] Data;
            public ulong Sender;
            public MyTimeSpan ReceivedTime;
        }

        readonly Dictionary<ushort, CallbackInfo> m_callbacks = new Dictionary<ushort, CallbackInfo>();
        readonly HashSet<ulong> m_pendingFlushes = new HashSet<ulong>();
        static readonly Dictionary<Type, Tuple<ushort, P2PMessageEnum>> TypeMap;

        static readonly int m_messageTypeCount = (int)MyEnum<MyMessageId>.Range.Max + 1;
        readonly Queue<int>[] m_slidingWindows = Enumerable.Range(0, m_messageTypeCount).Select(s => new Queue<int>(120)).ToArray();
        readonly int[] m_thisFrameTraffic = new int[m_messageTypeCount];

        static MyTransportLayer()
        {
            TypeMap = new Dictionary<Type, Tuple<ushort, P2PMessageEnum>>();

#if XB1 // XB1_ALLINONEASSEMBLY
            RegisterFromAssembly(MyAssembly.AllInOneAssembly);
#else // !XB1
            RegisterFromAssembly(typeof(MyTransportLayer).Assembly);

            if (MyPlugins.GameAssembly != null)
                RegisterFromAssembly(MyPlugins.GameAssembly);

            if (MyPlugins.UserAssembly != null)
                RegisterFromAssembly(MyPlugins.UserAssembly);
#endif // !XB1
        }

        static void RegisterFromAssembly(Assembly assembly)
        {
#if XB1 // XB1_ALLINONEASSEMBLY
            System.Diagnostics.Debug.Assert(m_registered == false);
            if (m_registered == true)
                return;
            m_registered = true;
            foreach (var type in MyAssembly.GetTypes())
#else // !XB1
            foreach (var type in assembly.GetTypes())
#endif // !XB1
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

        readonly ByteStream m_sendStream;
        readonly ByteStream m_receiveStream;

        bool m_isBuffering;
        readonly int m_channel;

        List<Buffer> m_buffer;
        AddMessageDelegate m_loopback;
        Dictionary<MyMessageId, Action<MyPacket>> m_handlers = new Dictionary<MyMessageId, Action<MyPacket>>();

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
        public Action<ulong> DisconnectPeerOnError { get; set; }

        public MyTransportLayer(int channel)
        {
            m_handlers.Add(MyMessageId.OLD_GAME_EVENT, HandleOldGameEvent);

            m_channel = channel;
            m_sendStream = new ByteStream(64 * 1024, true);
            m_receiveStream = new ByteStream();
            DisconnectPeerOnError = null;

            m_loopback = MyNetworkReader.SetHandler(channel, HandleMessage, (x) => DisconnectPeerOnError(x), MyReceiveQueue.Mode.Timer, Timer);
        }

        /*private void OnMessageRecieved(byte[] data, uint length, ulong sender)
        {
            HandleMessage(data, (int)length, sender, MyTimeSpan.Zero);
        }*/

        MyTimeSpan Timer()
        {
            //if (Sync.Layer != null)
            //{
            //return Sync.Layer.Interpolation.Timer.CurrentTime;
            //}
            return MyTimeSpan.Zero;
        }

        public void SendFlush(ulong sendTo)
        {
            m_sendStream.Position = 0;
            m_sendStream.WriteByte((byte)MyMessageId.OLD_GAME_EVENT_FLUSH);
            SendMessage(m_sendStream, P2PMessageEnum.ReliableWithBuffering, sendTo, m_channel);
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
            m_sendStream.WriteByte((byte)MyMessageId.OLD_GAME_EVENT);
            m_sendStream.WriteUShort(msgId.Item1);
            c.Write(m_sendStream, ref msg);

            const int mtu = 1200;
            if ((sendType == P2PMessageEnum.Unreliable || sendType == P2PMessageEnum.UnreliableNoDelay) && m_sendStream.Position > mtu)
            {
                Debug.Fail("Sending unreliable message as reliable, because it's bigger than MTU, message type: " + FindDebugName(msgId.Item1));
                sendType = P2PMessageEnum.ReliableWithBuffering;
            }

            if (sendType == P2PMessageEnum.Reliable) // Always with buffering
                sendType = P2PMessageEnum.ReliableWithBuffering;

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

        void SendMessage(ByteStream sendStream, P2PMessageEnum sendType, ulong sendTo, int channel)
        {
            SendHandler(sendTo, sendStream.Data, (int)sendStream.Position, sendType, channel);
        }

        public unsafe void SendMessage(MyMessageId id, BitStream stream, bool reliable, EndpointId endpoint)
        {
            m_sendStream.Position = 0;
            m_sendStream.WriteByte((byte)id);
            if (stream != null)
            {
                m_sendStream.WriteNoAlloc((byte*)(void*)stream.DataPointer, 0, stream.BytePosition);
            }

            SendMessage(m_sendStream, reliable ? P2PMessageEnum.ReliableWithBuffering : P2PMessageEnum.Unreliable, endpoint.Value, m_channel);
        }

        public unsafe void Tick()
        {
            foreach (var steamId in m_pendingFlushes)
            {
                byte data = 0;
                if (!Peer2Peer.SendPacket(steamId, &data, 0, P2PMessageEnum.Reliable, m_channel))
                {
                    System.Diagnostics.Debug.Fail("P2P packet send fail (flush)");
                }
            }
            m_pendingFlushes.Clear();

            int totalSum = 0;
            NetProfiler.Begin("Avg per frame (60 frames window)");
            for (int i = 0; i < m_messageTypeCount; i++)
            {
                var window = m_slidingWindows[i];
                window.Enqueue(m_thisFrameTraffic[i]);
                m_thisFrameTraffic[i] = 0;

                while (window.Count > 60)
                    window.Dequeue();

                int sum = 0;
                foreach (var item in window)
                {
                    sum += item;
                }
                if (sum > 0)
                {
                    NetProfiler.Begin(MyEnum<MyMessageId>.GetName((MyMessageId)i));
                    NetProfiler.End(sum / 60.0f, sum / 1024.0f, "{0} KB/s");
                }
                totalSum += sum;
            }
            NetProfiler.End(totalSum / 60.0f, totalSum / 1024.0f, "{0} KB/s");
        }

        void SendHandler(ulong remoteUser, byte[] data, int byteCount, P2PMessageEnum msgType, int channel)
        {
            if (msgType == P2PMessageEnum.ReliableWithBuffering)
            {
                m_pendingFlushes.Add(remoteUser);
            }

            ByteCountSent += byteCount;
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
                NetProfiler.Begin("Processing buffered events");
                foreach (var b in m_buffer)
                {
                    ProcessMessage(b.Data, b.Data.Length, b.Sender, MyTimeSpan.Zero, b.ReceivedTime);
                }
                NetProfiler.End();
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

        private void HandleMessage(byte[] data, int dataSize, ulong sender, MyTimeSpan timestamp, MyTimeSpan receivedTime)
        {
            if (dataSize < sizeof(byte)) // This would cause crash, message has to contain at least MyMessageId
                return;

            ProfilerShort.Begin("Handle message");

            if (sender != Sync.MyId)
                ByteCountReceived += dataSize;

            MyMessageId id = (MyMessageId)data[0];
            
            LogStats(ReceiveStats, "", dataSize, 1, P2PMessageEnum.Reliable);


            m_thisFrameTraffic[(int)id] += dataSize;

            if (id == MyMessageId.OLD_GAME_EVENT_FLUSH) // Flush buffer
            {
                if (m_buffer != null)
                    m_buffer.Clear();
            }
            else if (IsBuffering && id != MyMessageId.JOIN_RESULT && id != MyMessageId.WORLD_DATA && id !=  MyMessageId.WORLD_BATTLE_DATA) // Buffer event
            {
                var buff = new Buffer();
                buff.Sender = sender;
                buff.Data = new byte[dataSize];
                Array.Copy(data, buff.Data, dataSize);
                buff.ReceivedTime = MyTimeSpan.FromTicks(Stopwatch.GetTimestamp());
                m_buffer.Add(buff);
            }
            else // Process event
            {
                NetProfiler.Begin("Live data", 0);
                ProcessMessage(data, dataSize, sender, timestamp, receivedTime);
                NetProfiler.End();
            }

            ProfilerShort.End();
        }

        private void ProcessMessage(byte[] data, int dataSize, ulong sender, MyTimeSpan timestamp, MyTimeSpan receivedTime)
        {
            Debug.Assert(data.Length >= dataSize, "Wrong size");

            MyMessageId id = (MyMessageId)data[0];

            if (id == MyMessageId.CLIENT_CONNNECTED)
            {
                MyNetworkClient player;
                if (Sync.Layer != null && Sync.Layer.Clients != null)
                {
                    bool playerFound = Sync.Layer.Clients.TryGetClient(sender, out player);

                    if (!playerFound)
                    {
                        Sync.Layer.Clients.AddClient(sender);
                    }
                }
            }

            MyPacket p = new MyPacket
            {
                Data = data,
                // First byte is message id
                PayloadOffset = 1,
                PayloadLength = dataSize - 1,
                Sender = new VRage.Network.EndpointId(sender),
                Timestamp = timestamp,
                ReceivedTime = receivedTime
            };

            Action<MyPacket> handler;
            if (m_handlers.TryGetValue(id, out handler))
            {
                ProfilerShort.Begin(MyEnum<MyMessageId>.GetName(id));
                NetProfiler.Begin(MyEnum<MyMessageId>.GetName(id));
                handler(p);
                NetProfiler.End(p.PayloadLength);
                ProfilerShort.End();
            }
            else
            {
                Debug.Fail("No handler for message type: " + id);
            }
        }

        public void HandleOldGameEvent(MyPacket packet)
        {
            m_receiveStream.Reset(packet.Data, packet.PayloadOffset + packet.PayloadLength);
            m_receiveStream.Position = packet.PayloadOffset;

            ushort msgId = m_receiveStream.ReadUShort();

            CallbackInfo info;
            if (m_callbacks.TryGetValue(msgId, out info))
            {
                var handler = info.Callback;

                ProfilerShort.Begin(GetGroupName(handler.MessageType));
                ProfilerShort.Begin(handler.MessageType);
                NetProfiler.Begin(handler.MessageType);

                if (packet.Sender.Value != Sync.MyId)
                {
                    if (!MySandboxGame.IsDedicated)
                        LogStats(ReceiveStats, handler.MessageType, packet.PayloadOffset + packet.PayloadLength, 1, info.SendType);

                    // TODO: Log stats here
                    //info.Callback.MessageType;
                    //info.SendType;
                    //info.MessageId;
                }

                handler.Receive(m_receiveStream, packet.Sender.Value, packet.Timestamp);

                NetProfiler.End((int)m_receiveStream.Length);
                ProfilerShort.End();
                ProfilerShort.End();
            }
            else
            {
                Debug.Fail(String.Format("No handler defined for message {0}, of type {1}", msgId, FindDebugName(msgId)));
            }
        }

        public ITransportCallback<TMsg> GetCallback<TMsg>(MyTransportMessageEnum messageType)
        {
            var msgId = GetId<TMsg>(messageType);
            return (ITransportCallback<TMsg>)m_callbacks[msgId.Item1].Callback;
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

        public bool IsRegistered(MyMessageId messageId)
        {
            return m_handlers.ContainsKey(messageId);
        }

        public void Register(MyMessageId messageId, Action<MyPacket> handler)
        {
            m_handlers.Add(messageId, handler);
        }

        public void Unregister(MyMessageId messageId)
        {
            m_handlers.Remove(messageId);
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
            if(m_buffer != null)
            {
                m_buffer.Clear();
            }
        }

        public void ClearStats()
        {
            ByteCountReceived = ByteCountSent = 0;
            SendStats.Clear();
            ReceiveStats.Clear();
        }
    }
}
