using Sandbox.Engine.Networking;
using SteamSDK;
using System;
using System.Diagnostics;
using VRage.Game;
using VRage.Library.Utils;
using VRage.ObjectBuilders;
using VRage.Profiler;
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
        private readonly MyMultipartMessage m_receiveMsg;

        public float Progress { get { return m_receiveMsg.Progress; } }
        public int WorldSize { get { return m_receiveMsg.BlockCount * m_receiveMsg.BlockSize; } }

        public int ReceivedBlockCount { get { return m_receiveMsg.ReceivedCount; } }
        public int ReceivedDatalength { get { return m_receiveMsg.ReceivedDatalength; } }

        public MyDownloadWorldStateEnum State { get; private set; }
        public P2PSessionErrorEnum ConnectionError { get; private set; }
        public MyObjectBuilder_World WorldData { get; private set; }
        public event Action<MyDownloadWorldResult> ProgressChanged;

        readonly MyMultiplayerBase m_mp;

        public MyDownloadWorldResult(int channel, ulong sender, MyMultiplayerBase mp)
        {
            m_mp = mp;
            m_sender = sender;
            m_channel = channel;
            m_receiveMsg = new MyMultipartMessage(channel);
            MyNetworkReader.SetHandler(m_channel, MyDownloadWorldResult_Received, mp.DisconnectClient);
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

        void MyDownloadWorldResult_Received(byte[] data, int dataSize, ulong sender, MyTimeSpan timestamp, MyTimeSpan receivedTime)
        {
            ProfilerShort.Begin("DownloadWorldChunk");

            //Server didn't get the memo that we're finished (ack could've been lost)
            if (State == MyDownloadWorldStateEnum.Success)
            {
                m_mp.SendAck(sender, m_channel, m_receiveMsg.BlockCount - 1, m_receiveMsg.BlockCount);
                ProfilerShort.End();
                return;
            }

            Debug.Assert(State == MyDownloadWorldStateEnum.Established || State == MyDownloadWorldStateEnum.InProgress, "This should not be called, find why it's called");
            if (m_sender == sender)
            {
                var status = m_receiveMsg.Compose(data, dataSize, sender);
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
