using VRage.Serialization;
using Sandbox.Engine.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using VRage;
using VRageMath;

namespace Sandbox.Game.Multiplayer
{
    partial class MySyncGrid
    {
        class MySyncGridRemoveSerializer : ISerializer<RemoveBlocksMsg>
        {
            [StructLayout(LayoutKind.Sequential, Pack = 4)]
            struct Segment
            {
                public Vector3I Min;
                public Vector3Ushort Size;
            }

            MyVoxelSegmentation m_seg = new MyVoxelSegmentation();
            List<Vector3I> m_removeWithGenerator = new List<Vector3I>();
            List<Vector3I> m_removeWithoutGenerator = new List<Vector3I>();
            List<Vector3I> m_deform = new List<Vector3I>();
            List<Vector3I> m_destroy = new List<Vector3I>();
            HashSet<Vector3I> m_tmp = new HashSet<Vector3I>();

            List<MyVoxelSegmentation.Segment> GetSegments(List<Vector3I> positions)
            {
                m_seg.ClearInput();
                foreach (var p in positions)
                {
                    m_seg.AddInput(p);
                }
                return m_seg.FindSegments(MyVoxelSegmentationType.Simple);
            }

            void SerializeList(ByteStream destination, List<Vector3I> positions)
            {
                var segments = GetSegments(positions);
                ushort count = (ushort)segments.Count;
                Segment seg;
                BlitSerializer<ushort>.Default.Serialize(destination, ref count);
                for (int i = 0; i < count; i++)
                {
                    var s = segments[i];
                    seg.Min = s.Min;
                    var size = s.Size;
                    seg.Size = new Vector3Ushort((ushort)size.X, (ushort)size.Y, (ushort)size.Z);
                    BlitSerializer<Segment>.Default.Serialize(destination, ref seg);
                }
            }

            void DeserializeList(ByteStream source, List<Vector3I> outList)
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

                outList.Clear();
                foreach (var p in m_tmp)
                {
                    outList.Add(p);
                }
            }

            void ISerializer<RemoveBlocksMsg>.Serialize(ByteStream destination, ref RemoveBlocksMsg data)
            {
                BlitSerializer<long>.Default.Serialize(destination, ref data.GridEntityId);
                SerializeList(destination, data.LocationsWithGenerator);
                SerializeList(destination, data.LocationsWithoutGenerator);
                SerializeList(destination, data.DestroyLocations);
                SerializeList(destination, data.DestructionDeformationLocations);
            }

            void ISerializer<RemoveBlocksMsg>.Deserialize(ByteStream source, out RemoveBlocksMsg data)
            {
                data.LocationsWithGenerator = m_removeWithGenerator;
                data.LocationsWithoutGenerator = m_removeWithoutGenerator;
                data.DestroyLocations = m_destroy;
                data.DestructionDeformationLocations = m_deform;

                BlitSerializer<long>.Default.Deserialize(source, out data.GridEntityId);
                DeserializeList(source, data.LocationsWithGenerator);
                DeserializeList(source, data.LocationsWithoutGenerator);
                DeserializeList(source, data.DestroyLocations);
                DeserializeList(source, data.DestructionDeformationLocations);
            }
        }
    }
}
