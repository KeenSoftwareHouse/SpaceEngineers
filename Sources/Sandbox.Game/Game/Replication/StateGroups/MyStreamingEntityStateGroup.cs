using Sandbox.Engine.Multiplayer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using VRage.Library.Utils;
using VRage.Network;
using VRage.Profiler;
using VRage.Utils;

namespace Sandbox.Game.Replication.StateGroups
{
    internal static class MemoryCompressor
    {
        public static void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }
        public static byte[] Compress(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    //msi.CopyTo(gs);
                    CopyTo(msi, gs);
                }

                return mso.ToArray();
            }
        }
        public static byte[] Decompress(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    //gs.CopyTo(mso);
                    CopyTo(gs, mso);
                }

                return mso.ToArray();
            }
        }

    }
    class MyStreamingEntityStateGroup<T> : IMyStateGroup where T : IMyStreamableReplicable
    {
        int m_streamSize = 8000;
        const int HEADER_SIZE = 97;
        const int SAFE_VALUE = 128;

        public T Instance { get; private set; }

        bool m_streamed = false;

        public IMyReplicable Owner { get; private set; }

        public MyStreamingEntityStateGroup(T obj, IMyReplicable owner)
        {
            Instance = obj;
            Owner = owner;
        }

        class StreamPartInfo : IComparable<StreamPartInfo>
        {
            public int StartIndex;
            public int NumBits;
            public short Position;

            public int CompareTo(StreamPartInfo b)
            {
                return this.StartIndex.CompareTo(b.StartIndex);
            }
        }

        class StreamClientData
        {
            public short CurrentPart;
            public short NumParts;
            public int LastPosition;
            public byte[] ObjectData;
            public bool Dirty;
            public int RemainingBits;
            public int UncompressedSize;
            public bool ForceSend = false;
            public Dictionary<byte, StreamPartInfo> SendPackets = new Dictionary<byte, StreamPartInfo>();
            public List<StreamPartInfo> FailedIncompletePackets = new List<StreamPartInfo>();
        }

        Dictionary<ulong, StreamClientData> m_clientStreamData;

        SortedList<StreamPartInfo, byte[]> m_receivedParts;

        short m_numPartsToReceive = 0;

        int m_receivedBytes = 0;

        int m_uncompressedSize = 0;

        public StateGroupEnum GroupType
        {
            get { return StateGroupEnum.Streaming; }
        }

        public void CreateClientData(MyClientStateBase forClient)
        {
            if (m_clientStreamData == null)
            {
                m_clientStreamData = new Dictionary<ulong, StreamClientData>();
            }

            StreamClientData data;
            if (m_clientStreamData.TryGetValue(forClient.EndpointId.Value, out data) == false)
            {
                m_clientStreamData[forClient.EndpointId.Value] = new StreamClientData();
            }
            m_clientStreamData[forClient.EndpointId.Value].Dirty = true;
        }

        public void DestroyClientData(MyClientStateBase forClient)
        {
            if (m_clientStreamData != null)
            {
                m_clientStreamData.Remove(forClient.EndpointId.Value);
            }
        }

        public void ClientUpdate(MyTimeSpan clientTimestamp)
        {

        }

        public void Destroy()
        {
            if (m_receivedParts != null)
            {
                m_receivedParts.Clear();
                m_receivedParts = null;
            }
        }

        public float GetGroupPriority(int frameCountWithoutSync, MyClientInfo forClient)
        {
            ProfilerShort.Begin("MyStreamingEntityStateGroup::GetGroupPriority");

            StreamClientData clientData = m_clientStreamData[forClient.EndpointId.Value];
            if (forClient.IsReplicableReady(Instance as IMyReplicable))
            {
                clientData.CurrentPart = 0;
                clientData.ForceSend = false;
                clientData.ObjectData = null;
                clientData.FailedIncompletePackets.Clear();
                clientData.SendPackets.Clear();
                ProfilerShort.End();
                return 0.0f;
            }

            float priority = forClient.HasReplicable(Instance as IMyReplicable) && clientData.Dirty ? Instance.GetPriority(forClient, false) * Instance.PriorityScale() : 0.0f;

            if (priority < 0.01f && (clientData.ForceSend || clientData.FailedIncompletePackets.Count > 0))
            {
                priority = Instance.PriorityScale();
            }

            ProfilerShort.End();
            return priority;
        }

        private bool ReadPart(ref VRage.Library.Collections.BitStream stream)
        {
            m_numPartsToReceive = stream.ReadInt16();
            short currentPacket = stream.ReadInt16();
            int bitsToReceive = stream.ReadInt32();
            int bytesToRecieve = MyLibraryUtils.GetDivisionCeil(bitsToReceive, 8);

            int numBitsInStream = stream.BitLength - stream.BitPosition;
            if (numBitsInStream < bitsToReceive)
            {
                Debug.Fail("trying to read more than there is in stream. Why ?");
                MyLog.Default.WriteLine("trying to read more than there is in stream. Total num parts : " + m_numPartsToReceive.ToString() + " current part : " + currentPacket.ToString() + " bits to read : " + bitsToReceive.ToString() + " bits in stream : " + numBitsInStream.ToString() + " replicable : " + Instance.ToString());
                //what now ?
                return false;
            }

            if (m_receivedParts == null)
            {
                m_receivedParts = new SortedList<StreamPartInfo, byte[]>();
            }


            m_receivedBytes += bytesToRecieve;
            byte[] partData = new byte[bytesToRecieve];

            unsafe
            {
                fixed (byte* dataPtr = partData)
                {
                    stream.ReadMemory(dataPtr, bitsToReceive);
                }
            }

            StreamPartInfo info = new StreamPartInfo();
            info.NumBits = bitsToReceive;
            info.StartIndex = currentPacket;
            m_receivedParts[info] = partData;

            return true;
        }

        private void ProcessRead(ref VRage.Library.Collections.BitStream stream)
        {
            //becaouse client state group is unreliable, it can happen, that some acks are not recieved at server
            //and server sends them again. In streaming this is not good, so we need to drop streaming when we got stream again
            if (stream.BitLength == stream.BitPosition || m_streamed)
            {
                return;
            }

            bool hasData = stream.ReadBool();
            if (hasData)
            {
                m_uncompressedSize = stream.ReadInt32();

                if (ReadPart(ref stream) == false)
                {
                    //something wrong happened, server send bad data, don't know what to do,
                    //cancel replicable on server
                    m_receivedParts = null;
                    Instance.LoadCancel();
                    return;
                }

                if (m_receivedParts.Count == m_numPartsToReceive)
                {
                    m_streamed = true;
                    CreateReplicable(m_uncompressedSize);
                }
            }
            else
            {
                Debug.Assert(false, "testing assert to find issue, if you got this and game is running, please ignore it");
                MyLog.Default.WriteLine("received empty state group");
                //something went wrong we need to cancel this replicable and hope next time it will be ok 
                if (m_receivedParts != null)
                {
                    m_receivedParts.Clear();
                }
                m_receivedParts = null;
                Instance.LoadCancel();
            }
        }

        private void CreateReplicable(int uncompressedSize)
        {
            byte[] ret = new byte[m_receivedBytes];
            int offset = 0;
            foreach (var part in m_receivedParts)
            {
                Buffer.BlockCopy(part.Value, 0, ret, offset, part.Value.Length);
                offset += part.Value.Length;
            }

            byte[] decompressed = MemoryCompressor.Decompress(ret);
            VRage.Library.Collections.BitStream str = new VRage.Library.Collections.BitStream();
            str.ResetWrite();

            unsafe
            {
                fixed (byte* dataPtr = decompressed)
                {
                    str.SerializeMemory(dataPtr, uncompressedSize);
                }
            }

            str.ResetRead();
            Instance.LoadDone(str);
            str.CheckTerminator();
            str.Dispose();
            if (m_receivedParts != null)
            {
                m_receivedParts.Clear();
            }
            m_receivedParts = null;
            m_receivedBytes = 0;
        }

        private void ProcessWrite(int maxBitPosition, ref VRage.Library.Collections.BitStream stream, EndpointId forClient, byte packetId)
        {
            m_streamSize = MyLibraryUtils.GetDivisionCeil(maxBitPosition - stream.BitPosition - HEADER_SIZE - SAFE_VALUE, 8) * 8;
            StreamClientData clientData = m_clientStreamData[forClient.Value];

            if (clientData.FailedIncompletePackets.Count > 0)
            {
                stream.WriteBool(true);
                WriteIncompletePacket(clientData, packetId, ref stream);
                return;
            }

            int bitsToSend = 0;
            bool incomplete = false;

            if (clientData.ObjectData == null)
            {
                SaveReplicable(clientData);
            }
            else
            {
                incomplete = true;
            }

            clientData.NumParts = (short)MyLibraryUtils.GetDivisionCeil(clientData.ObjectData.Length * 8, m_streamSize);

            bitsToSend = clientData.RemainingBits;

            if (bitsToSend == 0)
            {
                clientData.ForceSend = false;
                clientData.Dirty = false;

                stream.WriteBool(false);
                return;
            }

            stream.WriteBool(true);
            stream.WriteInt32(clientData.UncompressedSize);

            if (bitsToSend > m_streamSize || incomplete)
            {
                WritePart(ref bitsToSend, clientData, packetId, ref stream);
            }
            else
            {
                WriteWhole(bitsToSend, clientData, packetId, ref stream);
            }

            if (clientData.RemainingBits == 0)
            {
                clientData.Dirty = false;
                clientData.ForceSend = false;
            }
        }

        private void WriteIncompletePacket(StreamClientData clientData, byte packetId, ref VRage.Library.Collections.BitStream stream)
        {
            if (clientData.ObjectData == null)
            {
                clientData.FailedIncompletePackets.Clear();
                return;
            }

            StreamPartInfo failedPart = clientData.FailedIncompletePackets[0];
            clientData.FailedIncompletePackets.Remove(failedPart);
            clientData.SendPackets[packetId] = failedPart;

            stream.WriteInt32(clientData.UncompressedSize);
            stream.WriteInt16(clientData.NumParts);
            stream.WriteInt16(failedPart.Position);
            stream.WriteInt32(failedPart.NumBits);

            unsafe
            {
                fixed (byte* dataPtr = &clientData.ObjectData[failedPart.StartIndex])
                {
                    stream.WriteMemory(dataPtr, failedPart.NumBits);
                }
            }
        }

        private void WritePart(ref int bitsToSend, StreamClientData clientData, byte packetId, ref VRage.Library.Collections.BitStream stream)
        {
            bitsToSend = Math.Min(m_streamSize, clientData.RemainingBits);
            StreamPartInfo info = new StreamPartInfo();

            info.StartIndex = clientData.LastPosition;
            info.NumBits = bitsToSend;

            clientData.LastPosition = info.StartIndex + MyLibraryUtils.GetDivisionCeil(m_streamSize, 8);
            clientData.SendPackets[packetId] = info;
            clientData.RemainingBits = Math.Max(0, clientData.RemainingBits - m_streamSize);

            stream.WriteInt16(clientData.NumParts);
            stream.WriteInt16(clientData.CurrentPart);
            info.Position = clientData.CurrentPart;

            clientData.CurrentPart++;

            stream.WriteInt32(bitsToSend);

            unsafe
            {

                fixed (byte* dataPtr = &clientData.ObjectData[info.StartIndex])
                {
                    stream.WriteMemory(dataPtr, bitsToSend);
                }
            }
        }

        private void WriteWhole(int bitsToSend, StreamClientData clientData, byte packetId, ref VRage.Library.Collections.BitStream stream)
        {
            StreamPartInfo info = new StreamPartInfo();
            info.StartIndex = 0;
            info.NumBits = bitsToSend;
            info.Position = 0;

            clientData.SendPackets[packetId] = info;
            clientData.RemainingBits = 0;

            clientData.Dirty = false;
            clientData.ForceSend = false;
            stream.WriteInt16(1);
            stream.WriteInt16(0);
            stream.WriteInt32(bitsToSend);

            unsafe
            {
                fixed (byte* dataPtr = clientData.ObjectData)
                {
                    stream.WriteMemory(dataPtr, bitsToSend);
                }
            }
        }

        public void Serialize(VRage.Library.Collections.BitStream stream, EndpointId forClient, MyTimeSpan timestamp, byte packetId, int maxBitPosition)
        {
            if (stream.Reading)
            {
                ProcessRead(ref stream);
            }
            else
            {
                ProcessWrite(maxBitPosition, ref stream, forClient, packetId);
            }
        }

        private void SaveReplicable(StreamClientData clientData)
        {
            VRage.Library.Collections.BitStream str = new VRage.Library.Collections.BitStream();
            str.ResetWrite();
            Instance.Serialize(str);
            str.Terminate();
            str.ResetRead();

            int numObjectBits = str.BitLength;
            byte[] uncompressedData = new byte[str.ByteLength];

            unsafe
            {
                fixed (byte* dataPtr = uncompressedData)
                {
                    str.SerializeMemory(dataPtr, numObjectBits);
                }
            }
            str.Dispose();

            clientData.CurrentPart = 0;
            clientData.ObjectData = MemoryCompressor.Compress(uncompressedData);
            clientData.UncompressedSize = numObjectBits;
            clientData.RemainingBits = clientData.ObjectData.Length * 8;
        }

        public void OnAck(MyClientStateBase forClient, byte packetId, bool delivered)
        {
            //streaming  is reliable don't care
            return;

            StreamClientData clientData = m_clientStreamData[forClient.EndpointId.Value];
            StreamPartInfo packetInfo;
            if (clientData.SendPackets.TryGetValue(packetId, out packetInfo))
            {
                if (delivered)
                {
                    clientData.SendPackets.Remove(packetId);
                    if (clientData.SendPackets.Count == 0 && clientData.RemainingBits == 0)
                    {
                        clientData.Dirty = false;
                        clientData.ForceSend = false;
                    }
                }
                else
                {
                    if (clientData.ObjectData != null)
                    {
                        clientData.FailedIncompletePackets.Add(packetInfo);
                        clientData.Dirty = true;
                        clientData.SendPackets.Remove(packetId);
                    }
                }
            }
        }

        public void ForceSend(MyClientStateBase clientData)
        {
            StreamClientData streamData = m_clientStreamData[clientData.EndpointId.Value];
            streamData.ForceSend = true;
            SaveReplicable(streamData);
        }

        public void TimestampReset(MyTimeSpan timestamp)
        {
        }

        public bool IsStillDirty(EndpointId forClient)
        {
            StreamClientData clientData = m_clientStreamData[forClient.Value];
            return clientData.Dirty;
        }
    }
}
