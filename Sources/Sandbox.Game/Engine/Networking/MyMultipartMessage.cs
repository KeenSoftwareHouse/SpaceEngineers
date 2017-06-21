using Sandbox.Engine.Utils;
using SteamSDK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Sandbox.Engine.Networking
{
    class MyMultipartMessage
    {
        public enum Status
        {
            InProgress,
            Finished,
            Error,
        }

        public const byte START_HEADER = 0xFF;
        public const byte DATA_HEADER = 0x00;

        const int MAX_WAITING_BLOCKS = 8;

        public readonly MemoryStream Stream = new MemoryStream();

        private bool IsHeaderReceived { get { return BlockSize > 0; } }

        public int BlockSize { get; private set; }
        public int BlockCount { get; private set; }
        public int ReceivedCount { get; private set; }
        public int ReceivedDatalength { get; private set; }
        public float Progress { get { return m_nextExpected / (float)BlockCount; } }

        private SortedList m_buffer = new SortedList();
        private int m_nextExpected;
        private int m_channel;

        struct BufferedPacket
        {
            public byte[] data;
        }

        public MyMultipartMessage(int channel)
        {
            m_channel = channel;
        }
        
        public void Reset()
        {
            BlockCount = 0;
            BlockSize = 0;
            ReceivedCount = 0;
            Stream.Position = 0;
        }

        public unsafe Status Compose(byte[] receivedData, int receivedDataSize, ulong sender)
        {
            byte header = receivedData[0];

            if (header == START_HEADER)
            {
                // Receive info
                fixed (byte* block = receivedData)
                {
                    BlockCount = ((int*)(&block[1]))[0];
                    BlockSize = ((int*)(&block[1]))[1];
                    ReceivedDatalength = ((int*)(&block[1]))[2];
                }
                ReceivedCount = 0;
                Stream.Position = 0;
                Sandbox.Engine.Multiplayer.MyMultiplayer.Static.SendHeaderAck(sender, m_channel);
                if (BlockCount > 0 && BlockSize >= 1024)
                    return Status.InProgress;
            }
            else if (header == DATA_HEADER)
            {
                int index = -1;
                fixed (byte* block = receivedData)
                {
                    index = ((int*)(&block[1]))[0];
                }

                // Receive data
                bool isLast = index == BlockCount - 1;
                if ((receivedDataSize == BlockSize + 5) || (isLast && receivedDataSize <= BlockSize + 5))
                {
                    ReceivedCount++;
                    if (index == m_nextExpected)
                    {
                        Stream.Write(receivedData, 5, receivedDataSize - 5);
                        m_nextExpected++;
                        CheckBuffered();
                    }
                    else if (index > m_nextExpected && m_buffer[index] == null)
                    {
                        m_buffer.Add(index, new BufferedPacket() { data = receivedData.Skip(5).ToArray() });
                    }
                    Sandbox.Engine.Multiplayer.MyMultiplayer.Static.SendAck(sender, m_channel, index, m_nextExpected - 1);

                    if (m_nextExpected == BlockCount && m_buffer.Count == 0)
                    {
                        BlockSize = 0;
                        return Status.Finished;
                    }
                    else
                    {
                        return Status.InProgress;
                    }
                }
            }

            Reset();
            return Status.Error;
        }

        private void CheckBuffered()
        {
            while (m_buffer[m_nextExpected] != null)
            {
                Stream.Write(((BufferedPacket)m_buffer[m_nextExpected]).data, 0, ((BufferedPacket)m_buffer[m_nextExpected]).data.Length);
                m_buffer.Remove(m_nextExpected);
                m_nextExpected++;
            }
        }
    }
}
