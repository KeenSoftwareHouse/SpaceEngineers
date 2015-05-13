using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamSDK;
using System.Diagnostics;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using VRage.Utils;


namespace Sandbox.Engine.Networking
{
    delegate void NetworkMessageDelegate(byte[] data, int dataSize, ulong sender, TimeSpan timestamp);
    delegate void AddMessageDelegate(byte[] data, int dataSize, ulong sender);

    class MyNetworkReader
    {
        class ChannelInfo
        {
            public MyReceiveQueue Queue;
            public NetworkMessageDelegate Handler;
        }

        static Dictionary<int, ChannelInfo> m_channels = new Dictionary<int, ChannelInfo>();
        static List<ChannelInfo> m_tmpList = new List<ChannelInfo>();

        public static AddMessageDelegate SetHandler(int channel, NetworkMessageDelegate handler, MyReceiveQueue.Mode mode = MyReceiveQueue.Mode.Synchronized, Func<TimeSpan> timestampProvider = null)
        {
            ChannelInfo info;
            if (m_channels.TryGetValue(channel, out info))
            {
                if (info.Queue.ReadMode == mode)
                {
                    info.Handler = handler;
                    return info.Queue.AddMessage;
                }
                else
                {
                    info.Queue.Dispose();
                }
            }
            info = new ChannelInfo();
            info.Handler = handler;
            info.Queue = new MyReceiveQueue(channel, mode, mode == MyReceiveQueue.Mode.Synchronized ? 1 : 50, timestampProvider);
            m_channels[channel] = info;

            return info.Queue.AddMessage;
        }

        public static void ClearHandler(int channel)
        {
            ChannelInfo info;
            if (m_channels.TryGetValue(channel, out info))
            {
                info.Queue.Dispose();
            }
            m_channels.Remove(channel);
        }

        public static void Clear()
        {
            foreach (var kv in m_channels)
            {
                kv.Value.Queue.Dispose();
            }
            m_channels.Clear();
            MyLog.Default.WriteLine("Network readers disposed");
        }

        public static void Process(TimeSpan lag)
        {
            try
            {
                foreach (var item in m_channels)
                {
                    m_tmpList.Add(item.Value);
                }

                foreach (var chan in m_tmpList)
                {
                    chan.Queue.Process(chan.Handler, lag);
                }
            }
            finally
            {
                m_tmpList.Clear();
            }
        }
    }
}
