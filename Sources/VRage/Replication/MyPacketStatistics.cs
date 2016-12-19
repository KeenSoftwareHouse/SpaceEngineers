using VRage.Library.Collections;

namespace VRage.Replication
{
    public struct MyPacketStatistics
    {
        public int Duplicates;
        public int Drops;
        public int OutOfOrder;

        public void Reset()
        {
            Duplicates = OutOfOrder = Drops = 0;
        }

        public void Update(MyPacketTracker.OrderType type)
        {
            switch (type)
            {
                case MyPacketTracker.OrderType.InOrder:
                    break;
                case MyPacketTracker.OrderType.Duplicate:
                    Duplicates++;
                    break;
                case MyPacketTracker.OrderType.OutOfOrder:
                    OutOfOrder++;
                    break;
                default:
                    Drops += type - MyPacketTracker.OrderType.Drop1 + 1;
                    break;
            }
        }

        public void Write(BitStream sendStream)
        {
            sendStream.WriteByte((byte)Duplicates);
            sendStream.WriteByte((byte)OutOfOrder);
            sendStream.WriteByte((byte)Drops);
        }

        public void Read(BitStream receiveStream)
        {
            Duplicates = receiveStream.ReadByte();
            OutOfOrder = receiveStream.ReadByte();
            Drops = receiveStream.ReadByte();
        }

        public void Add(MyPacketStatistics statistics)
        {
            Duplicates += statistics.Duplicates;
            OutOfOrder += statistics.OutOfOrder;
            Drops += statistics.Drops;
        }
    }
}
