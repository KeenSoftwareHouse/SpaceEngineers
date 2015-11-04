using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Serialization;
using VRageMath;
using Sandbox.Definitions;
using VRage.Utils;
using VRage.Utils;
using VRage.Library.Utils;
using VRage.ObjectBuilders;

namespace Sandbox.Game.Multiplayer
{
    partial class MySyncGrid
    {
        class MySyncGridStockpileSerializer : ISerializer<StockpileChangedMsg>
        {
            private List<MyStockpileItem> m_tmpList = new List<MyStockpileItem>(16);

            void ISerializer<StockpileChangedMsg>.Serialize(VRage.ByteStream destination, ref StockpileChangedMsg data)
            {
                BlitSerializer<long>.Default.Serialize(destination, ref data.GridEntityId);
				BlitSerializer<ushort>.Default.Serialize(destination, ref data.SubBlockId);
                BlitSerializer<Vector3I>.Default.Serialize(destination, ref data.BlockPosition);

                Debug.Assert(data.Changes.Count() <= 255, "Too many component types in a block stockpile");
                byte size = (byte)data.Changes.Count(); // There shouldn't be so many component types on a single block so as to exceed one byte
                BlitSerializer<byte>.Default.Serialize(destination, ref size);

                foreach (var change in data.Changes)
                {
                    MyStockpileItem item = change;
                    BlitSerializer<int>.Default.Serialize(destination, ref item.Amount);
                    int subtypeId = (int)item.Content.SubtypeId;
                    BlitSerializer<int>.Default.Serialize(destination, ref subtypeId);
                    byte flags = (byte)item.Content.Flags;
                    BlitSerializer<byte>.Default.Serialize(destination, ref flags);
                    MyRuntimeObjectBuilderId typeId = (MyRuntimeObjectBuilderId)change.Content.TypeId;
                    BlitSerializer<MyRuntimeObjectBuilderId>.Default.Serialize(destination, ref typeId);
                }
            }

            void ISerializer<StockpileChangedMsg>.Deserialize(VRage.ByteStream source, out StockpileChangedMsg data)
            {
                BlitSerializer<long>.Default.Deserialize(source, out data.GridEntityId);
				BlitSerializer<ushort>.Default.Deserialize(source, out data.SubBlockId);
                BlitSerializer<Vector3I>.Default.Deserialize(source, out data.BlockPosition);

                byte size = 0;
                BlitSerializer<byte>.Default.Deserialize(source, out size);

                m_tmpList.Clear();
                for (int i = 0; i < (int)size; ++i)
                {
                    MyStockpileItem item = new MyStockpileItem();
                    BlitSerializer<int>.Default.Deserialize(source, out item.Amount);

                    MyStringHash subtypeId;
                    BlitSerializer<MyStringHash>.Default.Deserialize(source, out subtypeId);

                    byte flags = 0;
                    BlitSerializer<byte>.Default.Deserialize(source, out flags);

                    MyRuntimeObjectBuilderId typeId;
                    BlitSerializer<MyRuntimeObjectBuilderId>.Default.Deserialize(source, out typeId);
                    item.Content = (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(
                        (MyDefinitionId)new DefinitionIdBlit(typeId, subtypeId));

                    item.Content.Flags = (MyItemFlags)flags;

                    m_tmpList.Add(item);
                }
                data.Changes = m_tmpList;
            }
        }
    }
}
