using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using VRage.Library.Utils;

namespace Sandbox.Engine.Networking
{
    public class MyMultipartSender
    {
        byte[] m_data;
        int m_dataLength;
        ulong m_sendTo;
        int m_channel;
        int m_blockSize;
        int m_blockCount;

        int m_currentPart = 0;

        int m_index;
        int m_lastSent;
        int m_lastAck;
        int m_lastAckTime;
        bool[] m_acks;
        bool m_reset;

        private const int TIMEOUT_MILLISECONDS = 3000;
        private const int DISCONNECT_TIMEOUT_MILLISECONDS = 30000;
        private const int MAX_PACKETS_PER_FRAME = 64;

        private int m_windowSize = 16;
        private int m_triplicateAck;

        public bool HeaderAck;

        public unsafe MyMultipartSender(byte[] data, int dataLength, ulong sendTo, int channel, int blockSize = 1150)
        {
            m_data = data;
            m_dataLength = dataLength;
            m_sendTo = sendTo;
            m_channel = channel;
            m_blockSize = blockSize;

            m_blockCount = (m_dataLength - 1) / m_blockSize + 1; // Round up

            m_acks = new bool[m_blockCount];
            m_lastAckTime = MySandboxGame.TotalTimeInMilliseconds;

            SendHeader();
            
        }

        unsafe private void SendHeader()
        {
            int headerSize = sizeof(byte) + sizeof(int) + sizeof(int) + sizeof(int);
            byte* block = stackalloc byte[headerSize]; // First byte is header

            block[0] = MyMultipartMessage.START_HEADER;
            ((int*)(&block[1]))[0] = m_blockCount;
            ((int*)(&block[1]))[1] = m_blockSize;
            ((int*)(&block[1]))[2] = m_dataLength;
            Peer2Peer.SendPacket(m_sendTo, block, headerSize, P2PMessageEnum.Reliable, m_channel);
        }

        public void ReceiveAck(int index, int head)
        {
            if (m_lastAck < head)
            {
                m_lastAck = head;
                m_windowSize = Math.Min(m_windowSize + 2, 512); // slow start to avoid congestion
                m_triplicateAck = 0;
            }
            else if (m_lastAck == head && m_currentPart > m_lastAck && !m_acks[index]) // if the same head comes 3 times, assume the packet was lost
            {
                m_triplicateAck++;
                if (m_triplicateAck == 3)
                {
                    m_reset = true;
                    m_triplicateAck = 0;
                }
            }
            m_acks[index] = true;
            m_lastAckTime = MySandboxGame.TotalTimeInMilliseconds;
        }

        public bool SendWhole()
        {
            if (HeaderAck)
            {
                VRage.Profiler.ProfilerShort.Begin("Sending multipart");
                bool wasSent = false;
                if (MySandboxGame.TotalTimeInMilliseconds - m_lastSent > TIMEOUT_MILLISECONDS)
                    m_reset = true;

                if (m_reset)
                {
                    m_currentPart = m_lastAck;
                    m_windowSize = Math.Max(m_windowSize / 2, 16);
                }

                int numSent = 0;
                while (m_currentPart < m_lastAck + m_windowSize && m_currentPart < m_blockCount && numSent < MAX_PACKETS_PER_FRAME)
                {
                    if (!m_acks[m_currentPart])
                    {
                        SendPart(m_currentPart);
                        numSent++;
                    }
                    m_currentPart++;
                    wasSent = true;
                }

                if (wasSent)
                {
                    m_lastSent = MySandboxGame.TotalTimeInMilliseconds;
                }
                m_reset = false;
                VRage.Profiler.ProfilerShort.End();
            }

            return m_lastAck == m_blockCount - 1 || MySandboxGame.TotalTimeInMilliseconds - m_lastAckTime > DISCONNECT_TIMEOUT_MILLISECONDS;
        }

        public unsafe bool SendPart(int part)
        {
            byte* block = stackalloc byte[m_blockSize + 5]; // Five more bytes for header and index
            block[0] = MyMultipartMessage.DATA_HEADER;
            ((int*)(&block[1]))[0] = part;

            IntPtr dataPtr = new IntPtr(&block[5]);

            if (part < m_blockCount - 1)
            {
#if XB1
                System.Diagnostics.Debug.Assert(false); // No Marshal.Copy on Xbox One
#else // !XB1
                Marshal.Copy(m_data, part * m_blockSize, dataPtr, m_blockSize);
#endif // !XB1
                Peer2Peer.SendPacket(m_sendTo, block, m_blockSize + 5, P2PMessageEnum.Unreliable, m_channel);
                return true;
            }
            else if (part == m_blockCount - 1)
            {
                int basePos = part * m_blockSize;
#if XB1
                System.Diagnostics.Debug.Assert(false); // No Marshal.Copy on Xbox One
#else // !XB1
                Marshal.Copy(m_data, basePos, dataPtr, m_dataLength - basePos);
#endif // !XB1
                Peer2Peer.SendPacket(m_sendTo, block, m_dataLength - basePos + 5, P2PMessageEnum.Unreliable, m_channel);
                return false;
            }
            else
            {
                return false;
            }
        }
    }
}
