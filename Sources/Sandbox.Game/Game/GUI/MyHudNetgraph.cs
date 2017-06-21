using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using VRageMath;

namespace Sandbox.Game.Gui
{
    public class MyHudNetgraph
    {
        public const int NUMBER_OF_VISIBLE_PACKETS = 1500; // px count on x
        public static readonly Vector2 OPTIMAL_LENGTH_BAR_NORMALIZED = new Vector2(0, 0.25f); // normalized size of bar height

        // scale constants (progressive)
        public const int PACKET_SCALE_MAXIMUM = 25600; // Bytes
        public const int PACKET_SCALE_MINIMUM = 100; // Bytes
        public static readonly int PACKET_SCALE_MAXIMUM_MULTIPLIER = MathHelper.Log2(PACKET_SCALE_MAXIMUM / PACKET_SCALE_MINIMUM);
        public const float AVERAGE_SCALE_MAXIMUM = 20000.0f; // B/s
        public const float AVERAGE_SCALE_MINIMUM = 100.0f; // B/s
        public static readonly int AVERAGE_SCALE_MAXIMUM_MULTIPLIER = MathHelper.Log2((int)(AVERAGE_SCALE_MAXIMUM / AVERAGE_SCALE_MINIMUM));

        public const float SCALE_CHANGE_DELAY = 3.0f; // seconds

        // for average per bar
        public const int LINE_AVERAGE_COUNT = 30;

        // calculation constants
        internal const float REAL_SECONDS_MULTIPLIER = 1000.0f / (MyNetworkStats.NETGRAPH_UPDATE_TIME_MS * NUMBER_OF_VISIBLE_PACKETS);
        internal const float ONE_OVER_KB = 1.0f / 1024.0f;

        public class NetgraphLineData
        {
            public long ByteCountReliableReceived;
            public long ByteCountUnreliableReceived;
            public long ByteCountSent;

            public float AverageReceivedOnThisLine;
            public float AverageSentOnThisLine;

            public long TotalByteCountReceived
            {
                get { return ByteCountReliableReceived + ByteCountUnreliableReceived; }
            }

            public void Clear()
            {
                ByteCountReliableReceived = ByteCountUnreliableReceived = 0;
                ByteCountSent = 0;
                AverageReceivedOnThisLine = 0;
                AverageSentOnThisLine = 0;
            }
        }

        private int m_previousIndex;
        private int m_currentFirstIndex;
        private NetgraphLineData[] m_linesData;
        private long m_currentByteCountReceived;
        private long m_currentByteCountSent;

        private long m_lastPacketScaleChange;
        private long m_lastAverageScaleChange;

        private long m_byteMaximumForPacketScale;
        private float m_averageMaximum;
        private int m_overPacketScaleCounter;

        public int CurrentFirstIndex
        {
            get { return m_currentFirstIndex; }
        }

        public float AverageIncomingKBytes
        {
            get;
            private set;
        }

        public float AverageOutgoingKBytes
        {
            get;
            private set;
        }

        public long LastPacketBytesReceived
        {
            get { return m_linesData[m_previousIndex].TotalByteCountReceived; }
        }

        public long LastPacketBytesSent
        {
            get { return m_linesData[m_previousIndex].ByteCountSent; }
        }

        public long UpdatesPerSecond
        {
            get;
            set;
        }

        public long FramesPerSecond
        {
            get;
            set;
        }

        public long Ping
        {
            get;
            set;
        }

        public int CurrentPacketScaleMultiplier
        {
            get;
            private set;
        }

        public int CurrentAverageScaleMultiplier
        {
            get;
            private set;
        }

        public int CurrentPacketScaleMaximumValue
        {
            get { return MyHudNetgraph.PACKET_SCALE_MINIMUM * (1 << CurrentPacketScaleMultiplier);  }
        }

        public float CurrentPacketScaleInvertedMaximumValue
        {
            get { return 1.0f / CurrentPacketScaleMaximumValue; }
        }

        public int CurrentPacketScaleMinimumValue
        {
            get { return MyHudNetgraph.PACKET_SCALE_MINIMUM * (1 << (CurrentPacketScaleMultiplier - 1)); }
        }

        public float CurrentAverageScaleMaximumValue
        {
            get { return MyHudNetgraph.AVERAGE_SCALE_MINIMUM * (1 << CurrentAverageScaleMultiplier); }
        }

        public float CurrentAverageScaleInvertedMaximumValue
        {
            get { return 1.0f / CurrentAverageScaleMaximumValue; }
        }

        public float CurrentAverageScaleMinimumValue
        {
            get { return MyHudNetgraph.AVERAGE_SCALE_MINIMUM * (1 << (CurrentAverageScaleMultiplier - 1)); }
        }

        public MyHudNetgraph()
        {
            m_currentFirstIndex = m_previousIndex = 0;
            AverageIncomingKBytes = AverageOutgoingKBytes = 0;
            UpdatesPerSecond = 0;
            m_lastAverageScaleChange = m_lastPacketScaleChange = Stopwatch.GetTimestamp();
            m_averageMaximum = 0;
            m_linesData = new NetgraphLineData[NUMBER_OF_VISIBLE_PACKETS];
            for (int i = 0; i < NUMBER_OF_VISIBLE_PACKETS; i++)
            {
                m_linesData[i] = new NetgraphLineData();
            }
        }

        public void UpdateNextBar(long byteCountReceived, long byteCountSent, List<Tuple<string, NetworkStat>> data)
        {
            long byteCountReliableReceived = 0;
            long byteCountUnreliableReceived = 0;
            foreach (var tuple in data)
            {
                if (tuple.Item2.IsReliable)
                    byteCountReliableReceived += tuple.Item2.TotalSize;
                else
                    byteCountUnreliableReceived += tuple.Item2.TotalSize;
            }

            m_linesData[m_currentFirstIndex].ByteCountReliableReceived = byteCountReliableReceived;
            m_linesData[m_currentFirstIndex].ByteCountUnreliableReceived = byteCountUnreliableReceived;
            m_linesData[m_currentFirstIndex].ByteCountSent = byteCountSent - m_currentByteCountSent;          

            m_currentByteCountReceived = byteCountReceived;
            m_currentByteCountSent = byteCountSent;

            CalculateInAndOut();
            CalculateCurrentLineData();
            RecalculateAverageScaleLimit();
            RecalculatePacketScaleLimit();

            m_previousIndex = m_currentFirstIndex;
            m_currentFirstIndex++;
            if (m_currentFirstIndex == NUMBER_OF_VISIBLE_PACKETS)
                m_currentFirstIndex = 0;
        }

        private void CalculateInAndOut()
        {
            AverageIncomingKBytes = AverageOutgoingKBytes = 0;
            foreach (NetgraphLineData data in m_linesData)
            {
                AverageIncomingKBytes += data.TotalByteCountReceived;
                AverageOutgoingKBytes += data.ByteCountSent;
            }

            AverageIncomingKBytes = (AverageIncomingKBytes * ONE_OVER_KB * REAL_SECONDS_MULTIPLIER);
            AverageOutgoingKBytes = (AverageOutgoingKBytes * ONE_OVER_KB * REAL_SECONDS_MULTIPLIER);
        }

        private void CalculateCurrentLineData()
        {
            long averageReceivedBytes = 0;
            long averageSentBytes = 0;

            m_byteMaximumForPacketScale = 0;
            m_averageMaximum = 0;
            m_overPacketScaleCounter = 0;

            int startIndex = m_currentFirstIndex - LINE_AVERAGE_COUNT;
            if (startIndex < 0)
            {
                int secondStep = m_currentFirstIndex;
                startIndex = MyHudNetgraph.NUMBER_OF_VISIBLE_PACKETS + startIndex;

                for (int i = startIndex; i < MyHudNetgraph.NUMBER_OF_VISIBLE_PACKETS; i++)
                {
                    averageReceivedBytes += m_linesData[i].TotalByteCountReceived;
                    averageSentBytes += m_linesData[i].ByteCountSent;
                    m_byteMaximumForPacketScale = Math.Max(m_byteMaximumForPacketScale, m_linesData[i].TotalByteCountReceived + m_linesData[i].ByteCountSent);
                    m_averageMaximum = Math.Max(m_averageMaximum, Math.Max(m_linesData[i].AverageReceivedOnThisLine, m_linesData[i].AverageSentOnThisLine));
                }
                for (int i = 0; i < secondStep; i++)
                {
                    averageReceivedBytes += m_linesData[i].TotalByteCountReceived;
                    averageSentBytes += m_linesData[i].ByteCountSent;
                    m_byteMaximumForPacketScale = Math.Max(m_byteMaximumForPacketScale, m_linesData[i].TotalByteCountReceived + m_linesData[i].ByteCountSent);
                    m_averageMaximum = Math.Max(m_averageMaximum, Math.Max(m_linesData[i].AverageReceivedOnThisLine, m_linesData[i].AverageSentOnThisLine));
                }
            }
            else
            {
                for (int i = startIndex; i < startIndex + LINE_AVERAGE_COUNT; i++)
                {
                    averageReceivedBytes += m_linesData[i].TotalByteCountReceived;
                    averageSentBytes += m_linesData[i].ByteCountSent;
                    m_byteMaximumForPacketScale = Math.Max(m_byteMaximumForPacketScale, m_linesData[i].TotalByteCountReceived + m_linesData[i].ByteCountSent);
                    m_averageMaximum = Math.Max(m_averageMaximum, Math.Max(m_linesData[i].AverageReceivedOnThisLine, m_linesData[i].AverageSentOnThisLine));
                }
            }

            //ByteAverageForPacketScale = averageReceivedBytes;
            m_linesData[m_currentFirstIndex].AverageReceivedOnThisLine = averageReceivedBytes * (1000f / (MyNetworkStats.NETGRAPH_UPDATE_TIME_MS * LINE_AVERAGE_COUNT));
            m_linesData[m_currentFirstIndex].AverageSentOnThisLine = averageSentBytes * (1000f / (MyNetworkStats.NETGRAPH_UPDATE_TIME_MS * LINE_AVERAGE_COUNT));
        }

        public void ClearNetgraph()
        {
            AverageIncomingKBytes = AverageOutgoingKBytes = 0;
            foreach (NetgraphLineData data in m_linesData)
            {
                data.Clear();
            }
            m_previousIndex = m_currentFirstIndex = 0;
            m_currentByteCountReceived = m_currentByteCountSent = 0;
            UpdatesPerSecond = 0;
            FramesPerSecond = 0;
            CurrentAverageScaleMultiplier = 1;
            CurrentPacketScaleMultiplier = 1;
        }

        public NetgraphLineData GetNetgraphLineDataAtIndex(int i)
        {
            return m_linesData[i];
        }

        private void RecalculateAverageScaleLimit()
        {
            if ((Stopwatch.GetTimestamp() - m_lastAverageScaleChange) / Stopwatch.Frequency > SCALE_CHANGE_DELAY)
            {
                if (m_averageMaximum > CurrentAverageScaleMaximumValue)
                {
                    int multiplier = MathHelper.Log2((int)(m_averageMaximum / AVERAGE_SCALE_MINIMUM)) + 1;
                    if (AVERAGE_SCALE_MAXIMUM_MULTIPLIER < multiplier)
                        multiplier = AVERAGE_SCALE_MAXIMUM_MULTIPLIER;
                    CurrentAverageScaleMultiplier = multiplier;
                    m_lastAverageScaleChange = Stopwatch.GetTimestamp();
                }
                else if (m_averageMaximum < CurrentAverageScaleMinimumValue)
                {
                    int multiplier = MathHelper.Log2((int)(m_averageMaximum / AVERAGE_SCALE_MINIMUM)) + 1;
                    CurrentAverageScaleMultiplier = multiplier;
                    m_lastAverageScaleChange = Stopwatch.GetTimestamp();
                }
            }
        }

        private void RecalculatePacketScaleLimit()
        {
            if ((Stopwatch.GetTimestamp() - m_lastPacketScaleChange) / Stopwatch.Frequency > SCALE_CHANGE_DELAY)
            {
                if (m_byteMaximumForPacketScale > CurrentPacketScaleMaximumValue)
                {
                    int multiplier = MathHelper.Log2((int)(m_byteMaximumForPacketScale / PACKET_SCALE_MINIMUM)) + 1;
                    if (PACKET_SCALE_MAXIMUM_MULTIPLIER < multiplier)
                        multiplier = PACKET_SCALE_MAXIMUM_MULTIPLIER;
                    CurrentPacketScaleMultiplier = multiplier;
                    m_lastPacketScaleChange = Stopwatch.GetTimestamp();
                }
                else if (m_byteMaximumForPacketScale < CurrentPacketScaleMinimumValue)
                {
                    int multiplier = MathHelper.Log2((int)(m_byteMaximumForPacketScale / PACKET_SCALE_MINIMUM));
                    CurrentPacketScaleMultiplier = multiplier;
                    m_lastPacketScaleChange = Stopwatch.GetTimestamp();
                }
            }
        }

        private static readonly string[] BYTE_UNITS = new string[] { "[B]", "[kB]", "[MB]" };
        private static readonly string[] BYTE_PER_SECOND_UNITS = new string[] { "[B/s]", "[kB/s]", "[MB/s]" };
        private static readonly float[] BYTE_MULTIPLIERS = new float[] { 1f, 1024f, 1024f * 1024f };
        public void GetProperFormatAndValueForBytes(float input, out float formattedValue, StringBuilder outFormat, bool perSecond = false)
        {
            float inputAbs = Math.Abs(input);
            int i = 1;
            while (i < BYTE_MULTIPLIERS.Length)
            {
                if (inputAbs < BYTE_MULTIPLIERS[i])
                    break;
                i++;
            }
            i--;
            formattedValue = input / BYTE_MULTIPLIERS[i];
            formattedValue = (float)(Math.Truncate(100.0 * formattedValue) * 0.01f);
            if (!perSecond)
                outFormat.Append(BYTE_UNITS[i]);
            else
                outFormat.Append(BYTE_PER_SECOND_UNITS[i]);
        }
    }
}
