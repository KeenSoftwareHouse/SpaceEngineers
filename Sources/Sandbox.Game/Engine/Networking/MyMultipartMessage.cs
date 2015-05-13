using Sandbox.Engine.Utils;
using SteamSDK;
using System;
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
        public const byte PREEMBLE_HEADER = 0x01;

        const int MAX_WAITING_BLOCKS = 8;

        public readonly MemoryStream Stream = new MemoryStream();

        private bool IsHeaderReceived { get { return BlockSize > 0; } }

        public int BlockSize { get; private set; }
        public int BlockCount { get; private set; }
        public int ReceivedCount { get; private set; }
        public int ReceivedDatalength { get; private set; }
        public float Progress { get { return ReceivedCount / (float)BlockCount; } }

        public static unsafe bool SendPreemble(ulong sendTo, int channel)
        {
            bool ok = true;
            byte data = PREEMBLE_HEADER;
            ok &= Peer2Peer.SendPacket(sendTo, &data, 1, P2PMessageEnum.Reliable, channel);
            return ok;
        }
        
        public void Reset()
        {
            BlockCount = 0;
            BlockSize = 0;
            ReceivedCount = 0;
            Stream.Position = 0;
        }

        public unsafe Status Compose(byte[] receivedData, int receivedDataSize)
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
                if (BlockCount > 0 && BlockSize >= 1024)
                    return Status.InProgress;
            }
            else if (header == DATA_HEADER)
            {
                // Wait for header (skip parts of old message)
                if (!IsHeaderReceived)
                    return Status.InProgress;

                // Receive data
                bool isLast = ReceivedCount + 1 == BlockCount;
                if ((receivedDataSize == BlockSize + 1) || (isLast && receivedDataSize <= BlockSize + 1))
                {
                    ReceivedCount++;
                    Stream.Write(receivedData, 1, receivedDataSize - 1);
                    if (ReceivedCount == BlockCount)
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
            else if (header == PREEMBLE_HEADER)
            {
                return Status.InProgress;
            }

            Reset();
            return Status.Error;
        }
    }
}
