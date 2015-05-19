using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;
using System.ComponentModel;
using VRage;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public abstract class MyObjectBuilder_ProductionBlock : MyObjectBuilder_FunctionalBlock
    {
        [ProtoContract]
        public struct QueueItem
        {
            [ProtoMember]
            public SerializableDefinitionId Id;

            [ProtoMember]
            public MyFixedPoint Amount;

            [ProtoMember]
            public uint? ItemId;
        }

        /// <summary>
        /// Don't use. Backward compatibility only. Use InputInventory and OutputInventory instead.
        /// </summary>
        public MyObjectBuilder_Inventory Inventory
        {
            get { return InputInventory; }
            set { InputInventory = value; }
        }
        public bool ShouldSerializeInventory() { return false; }

        [ProtoMember]
        public MyObjectBuilder_Inventory InputInventory;

        [ProtoMember]
        public MyObjectBuilder_Inventory OutputInventory;

        [ProtoMember]
        [XmlArrayItem("Item")]
        public QueueItem[] Queue;

        [ProtoMember, DefaultValue(true)]
        public bool UseConveyorSystem = true;

        [ProtoMember, DefaultValue(0)]
        public uint NextItemId = 0;

        public override void SetupForProjector()
        {
            base.SetupForProjector();
            if (Inventory != null)
                Inventory.Clear();
            if (OutputInventory != null)
                OutputInventory.Clear();
        }

    }
}
