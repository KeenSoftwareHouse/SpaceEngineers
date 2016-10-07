using SteamSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

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

        public unsafe MyMultipartSender(byte[] data, int dataLength, ulong sendTo, int channel, int blockSize = 16384)
        {
            m_data = data;
            m_dataLength = dataLength;
            m_sendTo = sendTo;
            m_channel = channel;
            m_blockSize = blockSize;

            m_blockCount = (m_dataLength - 1) / m_blockSize + 1; // Round up

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
            Peer2Peer.SendPacket(m_sendTo, block, headerSize, P2PMessageEnum.ReliableWithBuffering, m_channel);
        }

        public unsafe void SendWhole()
        {
            while (SendPart()) ;
        }

        public unsafe bool SendPart()
        {
            byte* block = stackalloc byte[m_blockSize + 1]; // One more byte for header            
            block[0] = MyMultipartMessage.DATA_HEADER;
            IntPtr dataPtr = new IntPtr(&block[1]);

            if (m_currentPart < m_blockCount - 1)
            {
#if XB1
                System.Diagnostics.Debug.Assert(false); // No Marshal.Copy on Xbox One
#else // !XB1
                Marshal.Copy(m_data, m_currentPart * m_blockSize, dataPtr, m_blockSize);
#endif // !XB1
                Peer2Peer.SendPacket(m_sendTo, block, m_blockSize + 1, P2PMessageEnum.Reliable, m_channel);
                m_currentPart++;
                return true;
            }
            else if (m_currentPart == m_blockCount - 1)
            {
                int basePos = m_currentPart * m_blockSize;
#if XB1
                System.Diagnostics.Debug.Assert(false); // No Marshal.Copy on Xbox One
#else // !XB1
                Marshal.Copy(m_data, basePos, dataPtr, m_dataLength - basePos);
#endif // !XB1
                Peer2Peer.SendPacket(m_sendTo, block, m_dataLength - basePos + 1, P2PMessageEnum.Reliable, m_channel);
                m_currentPart++;
                return false;
            }
            else
            {
                return false;
            }
        }
    }
}
