using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities.Cube;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Serialization;
using VRageMath;

namespace Sandbox.Game.Multiplayer
{
    [MessageId(14440, P2PMessageEnum.Reliable)]
    struct CreateSplitsMsg : IEntityMessage
    {
        public long GridEntityId;
        public long GetEntityId() { return GridEntityId; }

        public List<Vector3I> SplitBlocks;
        public List<MyDisconnectHelper.Group> Groups;
    }

    class MySyncGridSplitsSerializer : MySyncGridListSerializer, ISerializer<CreateSplitsMsg>
    {
        List<Vector3I> m_split = new List<Vector3I>();
        List<MyDisconnectHelper.Group> m_groups = new List<MyDisconnectHelper.Group>();

        void ISerializer<CreateSplitsMsg>.Serialize(ByteStream destination, ref CreateSplitsMsg data)
        {
            BlitSerializer<long>.Default.Serialize(destination, ref data.GridEntityId);
            destination.Write7BitEncodedInt(data.Groups.Count);
            for (int i = 0; i < data.Groups.Count; i++)
            {
                var g = data.Groups[i];
                BlitSerializer<long>.Default.Serialize(destination, ref g.EntityId);
                BlitSerializer<int>.Default.Serialize(destination, ref g.BlockCount);
                BoolBlit isValidGroup = g.IsValid;
                BlitSerializer<BoolBlit>.Default.Serialize(destination, ref isValidGroup);
                
                // Segmentate by group, not possible to segmentate whole thing, because segmenter changes the order
                SerializeList(destination, data.SplitBlocks, g.FirstBlockIndex, g.BlockCount);
            }
        }

        void ISerializer<CreateSplitsMsg>.Deserialize(ByteStream source, out CreateSplitsMsg data)
        {
            m_split.Clear();
            m_groups.Clear();

            data.SplitBlocks = m_split;
            data.Groups = m_groups;

            BlitSerializer<long>.Default.Deserialize(source, out data.GridEntityId);

            int count = source.Read7BitEncodedInt();
            int index = 0;
            for (int i = 0; i < count; i++)
            {
                var g = new MyDisconnectHelper.Group();
                BlitSerializer<long>.Default.Deserialize(source, out g.EntityId);
                BlitSerializer<int>.Default.Deserialize(source, out g.BlockCount);
                BoolBlit isValid;
                BlitSerializer<BoolBlit>.Default.Deserialize(source, out isValid);
                g.IsValid = isValid;
                g.FirstBlockIndex = index;
                index += g.BlockCount;

                DeserializeList(source, data.SplitBlocks);
                data.Groups.Add(g);
            }
        }
    }
}
