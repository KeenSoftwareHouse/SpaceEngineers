using ProtoBuf;
using Sandbox.Common.ObjectBuilders.ComponentSystem;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using VRage;
using VRage.Network;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [Flags]
    public enum MyInventoryFlags
    {
        CanReceive = 1,
        CanSend = 2
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Inventory : MyObjectBuilder_InventoryBase
    {
        [ProtoMember]
        public List<MyObjectBuilder_InventoryItem> Items = new List<MyObjectBuilder_InventoryItem>();

        [ProtoMember]
        public uint nextItemId;

        [ProtoMember, DefaultValue(null)]
        public MyFixedPoint? Volume = null;

        [ProtoMember, DefaultValue(null)]
        public MyFixedPoint? Mass = null;

        [ProtoMember, DefaultValue(null)]
        public SerializableVector3? Size = null;

        [ProtoMember, DefaultValue(null)]
        public MyInventoryFlags? InventoryFlags = null;

        internal void Clear()
        {
            Items.Clear();
            nextItemId = 0;
        }
    }
}
