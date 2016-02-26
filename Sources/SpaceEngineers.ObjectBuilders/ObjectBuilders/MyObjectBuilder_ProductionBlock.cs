using VRage.ObjectBuilders;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;
using System.ComponentModel;
using VRage;
using System.Xml.Serialization;
using VRage.Game;
using VRage.Serialization;
using VRage.Game.ObjectBuilders.ComponentSystem;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
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
        [Serialize(MyObjectFlags.Nullable)]
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
            if (ComponentContainer != null)
                foreach(var comp in ComponentContainer.Components)
                    if (comp.Component.TypeId == typeof(MyObjectBuilder_InventoryAggregate))
                        (comp.Component as MyObjectBuilder_InventoryAggregate).Clear();
        }

    }
}
