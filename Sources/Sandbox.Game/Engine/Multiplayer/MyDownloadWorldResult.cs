using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Networking;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Text;
using VRage;
using VRage.ObjectBuilders;
using VRage.Trace;

namespace Sandbox.Engine.Multiplayer
{
    public enum MyDownloadWorldStateEnum
    {
        InProgress,
        Established,
        InvalidMessage,
        DeserializationFailed,
        Canceled,
        Success,
        WorldNotAvailable,
        ConnectionFailed,
    }

    public class MyDownloadWorldResult
    {
        private readonly int m_channel;
        private readonly ulong m_sender;
        private readonly MyMultipartMessage m_receiveMsg = new MyMultipartMessage();

        public float Progress { get { return m_receiveMsg.Progress; } }
        public int WorldSize { get { return m_receiveMsg.BlockCount * m_receiveMsg.BlockSize; } }

        public int ReceivedBlockCount { get { return m_receiveMsg.ReceivedCount; } }
        public int ReceivedDatalength { get { return m_receiveMsg.ReceivedDatalength; } }

        public MyDownloadWorldStateEnum State { get; private set; }
        public P2PSessionErrorEnum ConnectionError { get; private set; }
        public MyObjectBuilder_World WorldData { get; private set; }
        public event Action<MyDownloadWorldResult> ProgressChanged;

        MyMultiplayerBase m_mp;

        public MyDownloadWorldResult(int channel, ulong sender, MyMultiplayerBase mp)
        {
            m_mp = mp;
            m_sender = sender;
            m_channel = channel;
            MyNetworkReader.SetHandler(m_channel, MyDownloadWorldResult_Received);
            SteamSDK.Peer2Peer.ConnectionFailed += Peer2Peer_ConnectionFailed;
        }

        void Peer2Peer_ConnectionFailed(ulong remoteUserId, P2PSessionErrorEnum error)
        {
            MyTrace.Send(TraceWindow.Multiplayer, "Connection failed");
            State = MyDownloadWorldStateEnum.ConnectionFailed;
            ConnectionError = error;
            Deregister();
            RaiseProgressChanged();
        }

        public void Cancel()
        {
            MyTrace.Send(TraceWindow.Multiplayer, "World download canceled");
            State = MyDownloadWorldStateEnum.Canceled;
            Deregister();
        }

        private void Deregister()
        {
            MyNetworkReader.ClearHandler(m_channel);
            Peer2Peer.ConnectionFailed -= Peer2Peer_ConnectionFailed;
        }

        void MyDownloadWorldResult_Received(byte[] data, int dataSize, ulong sender, TimeSpan timestamp)
        {
            ProfilerShort.Begin("DownloadWorldChunk");

            Debug.Assert(State == MyDownloadWorldStateEnum.Established || State == MyDownloadWorldStateEnum.InProgress, "This should not be called, find why it's called");
            if (m_sender == sender)
            {
                var status = m_receiveMsg.Compose(data, dataSize);
                switch (status)
                {
                    case MyMultipartMessage.Status.InProgress:
                        break;

                    case MyMultipartMessage.Status.Finished:
                        Deregister();

                        m_receiveMsg.Stream.Position = 0;
                        if (m_receiveMsg.Stream.Length > 0)
                        {
                            MyObjectBuilder_World worldData;
                            if (MyObjectBuilderSerializer.DeserializeGZippedXML(m_receiveMsg.Stream, out worldData))
                            {
                                WorldData = worldData;
                                State = MyDownloadWorldStateEnum.Success;

                                MySandboxGame.Log.WriteLineAndConsole(String.Format("World download progress status: {0}, {1}", State.ToString(), this.Progress));
                            }
                            else
                            {
                                MySandboxGame.Log.WriteLine("Deserialization failed during world download.");
                                State = MyDownloadWorldStateEnum.DeserializationFailed;
                            }
                        }
                        else
                        {
                            State = MyDownloadWorldStateEnum.WorldNotAvailable;
                        }
                        break;

                    case MyMultipartMessage.Status.Error:
                        Deregister();
                        MySandboxGame.Log.WriteLine("Invalid packet header.");
                        State = MyDownloadWorldStateEnum.InvalidMessage;
                        break;
                }

                m_mp.SendAck(sender);

                MyTrace.Send(TraceWindow.Multiplayer, String.Format("World download progress status: {0}, {1}", State.ToString(), this.Progress));
                RaiseProgressChanged();
            }

            ProfilerShort.End();
        }

        void RaiseProgressChanged()
        {
            Debug.Assert(State != MyDownloadWorldStateEnum.Canceled, "Canceled, events should not be raised");
            var handler = ProgressChanged;
            if (handler != null) handler(this);
        }
    }
}
