using Sandbox.Engine.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using VRage;
using VRage.Serialization;
using VRageMath;

namespace Sandbox.Game.Multiplayer
{
    public class MySyncGridListSerializer
    {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct Segment
        {
            public Vector3I Min;
            public Vector3Ushort Size;
        }

        MyVoxelSegmentation m_seg = new MyVoxelSegmentation();
        HashSet<Vector3I> m_tmp = new HashSet<Vector3I>();

        List<MyVoxelSegmentation.Segment> GetSegments(List<Vector3I> positions, int index, int count)
        {
            m_seg.ClearInput();
            count = Math.Min(count, positions.Count);
            for (int i = index; i < index + count; i++)
            {
                m_seg.AddInput(positions[i]);
            }
            return m_seg.FindSegments(MyVoxelSegmentationType.Simple);
        }

        public void SerializeList(ByteStream destination, List<Vector3I> positions, int index = 0, int count = int.MaxValue)
        {
            var segments = GetSegments(positions, index, count);
            ushort itemCount = (ushort)segments.Count;
            Segment seg;
            BlitSerializer<ushort>.Default.Serialize(destination, ref itemCount);
            for (int i = 0; i < itemCount; i++)
            {
                var s = segments[i];
                seg.Min = s.Min;
                var size = s.Size;
                seg.Size = new Vector3Ushort((ushort)size.X, (ushort)size.Y, (ushort)size.Z);
                BlitSerializer<Segment>.Default.Serialize(destination, ref seg);
            }
        }

        public void DeserializeList(ByteStream source, List<Vector3I> outList)
        {
            m_tmp.Clear();
            ushort count;
            Segment seg;
            Vector3I pos;
            BlitSerializer<ushort>.Default.Deserialize(source, out count);
            for (int i = 0; i < count; i++)
            {
                BlitSerializer<Segment>.Default.Deserialize(source, out seg);
                for (pos.X = 0; pos.X < seg.Size.X; pos.X++)
                {
                    for (pos.Y = 0; pos.Y < seg.Size.Y; pos.Y++)
                    {
                        for (pos.Z = 0; pos.Z < seg.Size.Z; pos.Z++)
                        {
                            m_tmp.Add(seg.Min + pos);
                        }
                    }
                }
            }

            foreach (var p in m_tmp)
            {
                outList.Add(p);
            }
        }
    }
}
