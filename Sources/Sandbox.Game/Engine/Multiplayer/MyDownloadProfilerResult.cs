using Sandbox.Engine.Networking;
using VRage.Serialization;
using VRage.Profiler;
using VRage.ObjectBuilders;
using VRage.Game;
using System;
using VRage.Library.Utils;

namespace Sandbox.Engine.Multiplayer
{
    public class MyDownloadProfilerResult
    {
        private readonly int m_channel;
        private readonly ulong m_sender;
        private readonly MyMultipartMessage m_receiveMsg;
        MyMultiplayerBase m_mp;

        bool m_finished;

        public MyDownloadProfilerResult(int channel, ulong sender, MyMultiplayerBase mp)
        {
            m_mp = mp;
            m_sender = sender;
            m_channel = channel;
            m_receiveMsg = new MyMultipartMessage(channel);
            MyNetworkReader.SetHandler(m_channel, MyDownloadProfilerResult_Received, mp.DisconnectClient);
        }

        void MyDownloadProfilerResult_Received(byte[] data, int dataSize, ulong sender, MyTimeSpan timestamp, MyTimeSpan receivedTime)
        {
            if (m_finished)
            {
                m_mp.SendAck(sender, m_channel, m_receiveMsg.BlockCount - 1, m_receiveMsg.BlockCount);
                return;
            }

            if (m_sender == sender)
            {
                var status = m_receiveMsg.Compose(data, dataSize, sender);
                switch (status)
                {
                    case MyMultipartMessage.Status.InProgress:
                        break;

                    case MyMultipartMessage.Status.Finished:
                        MyNetworkReader.ClearHandler(m_channel);
                        
                        m_receiveMsg.Stream.Position = 0;
                        if (m_receiveMsg.Stream.Length > 0)
                        {
                            MyObjectBuilder_Profiler profilerBuilder;
                            MyObjectBuilderSerializer.DeserializeGZippedXML(m_receiveMsg.Stream, out profilerBuilder);
                            VRage.Profiler.MyRenderProfiler.SelectedProfiler = MyObjectBuilder_Profiler.Init(profilerBuilder);
                            VRage.Profiler.MyRenderProfiler.IsProfilerFromServer = true;
                        }
                        m_finished = true;
                        break;

                    case MyMultipartMessage.Status.Error:
                        MyNetworkReader.ClearHandler(m_channel);
                        break;
                }
            }
        }
    }
}
