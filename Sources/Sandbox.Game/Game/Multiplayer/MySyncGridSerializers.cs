using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Serialization;
using VRageMath;

namespace Sandbox.Game.Multiplayer
{
    partial class MySyncGrid
    {
        class BuildBlocksAreaSuccessSerializer : ISerializer<BuildBlocksAreaSuccessMsg>
        {
            void ISerializer<BuildBlocksAreaSuccessMsg>.Serialize(VRage.ByteStream destination, ref BuildBlocksAreaSuccessMsg data)
            {
                BlitSerializer<long>.Default.Serialize(destination, ref data.GridEntityId);
                BlitSerializer<MyCubeGrid.MyBlockBuildArea>.Default.Serialize(destination, ref data.Area);
                BlitSerializer<long>.Default.Serialize(destination, ref data.OwnerId);
                BlitSerializer<int>.Default.Serialize(destination, ref data.EntityIdSeed);
                BlitCollectionSerializer<HashSet<Vector3UByte>, Vector3UByte>.Default.Serialize(destination, ref data.FailList);
            }

            void ISerializer<BuildBlocksAreaSuccessMsg>.Deserialize(VRage.ByteStream source, out BuildBlocksAreaSuccessMsg data)
            {
                BlitSerializer<long>.Default.Deserialize(source, out data.GridEntityId);
                BlitSerializer<MyCubeGrid.MyBlockBuildArea>.Default.Deserialize(source, out data.Area);
                BlitSerializer<long>.Default.Deserialize(source, out data.OwnerId);
                BlitSerializer<int>.Default.Deserialize(source, out data.EntityIdSeed);
                BlitCollectionSerializer<HashSet<Vector3UByte>, Vector3UByte>.Default.Deserialize(source, out data.FailList);
            }
        }

        class RazeBlocksAreaSuccessSerializer : ISerializer<RazeBlocksAreaSuccessMsg>
        {
            void ISerializer<RazeBlocksAreaSuccessMsg>.Serialize(VRage.ByteStream destination, ref RazeBlocksAreaSuccessMsg data)
            {
                BlitSerializer<long>.Default.Serialize(destination, ref data.GridEntityId);
                BlitSerializer<Vector3I>.Default.Serialize(destination, ref data.Pos);
                BlitSerializer<Vector3UByte>.Default.Serialize(destination, ref data.Size);
                BlitCollectionSerializer<HashSet<Vector3UByte>, Vector3UByte>.Default.Serialize(destination, ref data.FailList);
            }

            void ISerializer<RazeBlocksAreaSuccessMsg>.Deserialize(VRage.ByteStream source, out RazeBlocksAreaSuccessMsg data)
            {
                BlitSerializer<long>.Default.Deserialize(source, out data.GridEntityId);
                BlitSerializer<Vector3I>.Default.Deserialize(source, out data.Pos);
                BlitSerializer<Vector3UByte>.Default.Deserialize(source, out data.Size);
                BlitCollectionSerializer<HashSet<Vector3UByte>, Vector3UByte>.Default.Deserialize(source, out data.FailList);
            }
        }

        class BonesMsgSerializer : ISerializer<BonesMsg>
        {
            void ISerializer<BonesMsg>.Serialize(VRage.ByteStream destination, ref BonesMsg data)
            {
                BlitSerializer<long>.Default.Serialize(destination, ref data.GridEntityId);
                BlitSerializer<Vector3I>.Default.Serialize(destination, ref data.MinBone);
                BlitSerializer<Vector3I>.Default.Serialize(destination, ref data.MaxBone);

                Debug.Assert(data.Bones.Count <= ushort.MaxValue, "Increase bone array size");

                ushort count = (ushort)data.Bones.Count;
                BlitSerializer<ushort>.Default.Serialize(destination, ref count);
                destination.Write(data.Bones.GetInternalArray(), 0, data.Bones.Count);
            }

            void ISerializer<BonesMsg>.Deserialize(VRage.ByteStream source, out BonesMsg data)
            {
                BlitSerializer<long>.Default.Deserialize(source, out data.GridEntityId);
                BlitSerializer<Vector3I>.Default.Deserialize(source, out data.MinBone);
                BlitSerializer<Vector3I>.Default.Deserialize(source, out data.MaxBone);

                ushort count;
                BlitSerializer<ushort>.Default.Deserialize(source, out count);
                data.Bones = new List<byte>(count);
                source.Read(data.Bones.GetInternalArray(), 0, count);
                data.Bones.SetSize(count);
            }
        }
    }
}
