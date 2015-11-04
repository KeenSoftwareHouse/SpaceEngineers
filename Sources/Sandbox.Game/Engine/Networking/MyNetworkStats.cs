using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Engine.Networking
{
    class MyNetworkStats
    {

        public static MyNetworkStats Static { get; private set; }

        public const float NETGRAPH_UPDATE_TIME_MS = 50;
        public const float NETGRAPH_PING_TIME_S = 4;

        private DateTime m_lastStatMeasurePerUpdateTime;
        private DateTime m_lastPingSent;
        private long? m_lastSentPingInTicks;   
        
        private Dictionary<string, NetworkStat> m_helper = new Dictionary<string, NetworkStat>();
        private List<Tuple<string, NetworkStat>> m_sendStatsCopy = new List<Tuple<string, NetworkStat>>();
        private List<Tuple<string, NetworkStat>> m_receivedStatsCopy = new List<Tuple<string, NetworkStat>>();

        static MyNetworkStats()
        {
            Static = new MyNetworkStats();

        }
  
        public void LogNetworkStats()
        {
            if (MySession.Static == null || MySession.Static.SyncLayer == null || MyMultiplayer.Static == null)
                return;

            if ((DateTime.UtcNow - m_lastStatMeasurePerUpdateTime).TotalMilliseconds > NETGRAPH_UPDATE_TIME_MS)
            {
                m_lastStatMeasurePerUpdateTime = DateTime.UtcNow;
                CopyStats(MySession.Static.SyncLayer.TransportLayer.SendStats, m_sendStatsCopy);
                CopyStats(MySession.Static.SyncLayer.TransportLayer.ReceiveStats, m_receivedStatsCopy);

                if (MyFakes.ENABLE_NETGRAPH && !MySandboxGame.IsDedicated)
                {
                    MyHud.Netgraph.UpdateNextBar(
                        MySession.Static.SyncLayer.TransportLayer.ByteCountReceived,
                        MySession.Static.SyncLayer.TransportLayer.ByteCountSent,
                        m_receivedStatsCopy);

                    if (!Sync.IsServer && (DateTime.UtcNow - m_lastPingSent).TotalSeconds > NETGRAPH_PING_TIME_S)
                    {
                        m_lastPingSent = DateTime.UtcNow;
                        if (Sync.Layer != null && m_lastSentPingInTicks == null)
                        {
                            m_lastSentPingInTicks = DateTime.UtcNow.Ticks;
                            MyMultiplayer.Static.SendPingToServer();
                        }      
                    }
                }
            }
            WriteStats(m_sendStatsCopy, "Total size - sent");
            WriteStats(m_receivedStatsCopy, "Total size - received");
        }

        public void ClearStats()
        {
            m_lastSentPingInTicks = null;
            m_lastPingSent = m_lastStatMeasurePerUpdateTime = DateTime.UtcNow;
            m_receivedStatsCopy.Clear();
            m_sendStatsCopy.Clear();
        }

        [Conditional(ProfilerShort.PerformanceProfilingSymbol)]
        private void WriteStats(List<Tuple<string, NetworkStat>> stats, string name)
        {
            ProfilerShort.Begin(name);
            int totalSize = 0;
            int totalCount = 0;

            int partSize = 0;
            int partCount = 0;

            bool needsEnd = false;

            for (int i = 0; i < stats.Count; i++)
            {
                if (i > 0 && (i % 15) == 0)
                {
                    if (needsEnd)
                        ProfilerShort.End(partCount, partSize * 1.024f / 1000, "{0:.00} KB/s", "Count: {0:.}");

                    partCount = partSize = 0;
                    ProfilerShort.Begin("Additional " + i / 15);
                    needsEnd = true;
                }

                Tuple<string, NetworkStat> stat = stats[i];
                partSize += stat.Item2.TotalSize;
                partCount += stat.Item2.MessageCount;

                totalSize += stat.Item2.TotalSize;
                totalCount += stat.Item2.MessageCount;

                ProfilerShort.CustomValue(stat.Item1, stat.Item2.MessageCount, stat.Item2.TotalSize * 1.024f / 1000, "{0:.00} KB/s", "Count: {0:.}");
            }
            if (needsEnd)
                ProfilerShort.End(partCount, partSize * 1.024f / 1000, "{0:.00} KB/s", "Count: {0:.}");

            ProfilerShort.End(totalCount, totalSize * 1.024f / 1000, "{0:.00} KB/s", "Count: {0:.}");
        }

        private void CopyStats(Dictionary<string, NetworkStat> from, List<Tuple<string, NetworkStat>> to)
        {
            try
            {
                foreach (var stat in from)
                {
                    m_helper.Add(stat.Key, stat.Value);
                }

                foreach (var item in to) // Copy existing stats
                {
                    NetworkStat stat;
                    if (m_helper.TryGetValue(item.Item1, out stat))
                    {
                        m_helper.Remove(item.Item1);
                        item.Item2.MessageCount = stat.MessageCount;
                        item.Item2.TotalSize = stat.TotalSize;
                        item.Item2.UniqueMessageCount = stat.UniqueMessageCount;
                        item.Item2.IsReliable = stat.IsReliable;
                        stat.Clear();
                    }
                }

                foreach (var stat in m_helper) // Copy newly added stats
                {
                    NetworkStat myStat = new NetworkStat();
                    to.Add(new Tuple<string, NetworkStat>(stat.Key, myStat));
                    myStat.MessageCount = stat.Value.MessageCount;
                    myStat.TotalSize = stat.Value.TotalSize;
                    myStat.UniqueMessageCount = stat.Value.UniqueMessageCount;
                    myStat.IsReliable = stat.Value.IsReliable;
                    stat.Value.Clear();
                }
            }
            finally
            {
                m_helper.Clear();
            }
        }

        public void OnPingSuccess()
        {
            MyDebug.AssertDebug(!Sync.IsServer && MyNetworkStats.Static.m_lastSentPingInTicks != null, "Ping failed!");

            if (MyNetworkStats.Static.m_lastSentPingInTicks == null)
                return;

            long diff = DateTime.UtcNow.Ticks - MyNetworkStats.Static.m_lastSentPingInTicks.Value;

			if (MyHud.Netgraph != null)
                MyHud.Netgraph.Ping = (long)TimeSpan.FromTicks(diff).TotalMilliseconds;
            MyNetworkStats.Static.m_lastSentPingInTicks = null;
        }
    }
}
