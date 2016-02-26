using ProtoBuf;
using System.Collections.Generic;
using System.ComponentModel;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_ConveyorSorter : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public bool IsWhiteList;
        [ProtoMember]
        public HashSet<SerializableDefinitionId> DefinitionIds = new HashSet<SerializableDefinitionId>();
        [ProtoMember]
        public List<byte> DefinitionTypes = new List<byte>();
        [ProtoMember]
        public bool DrainAll;

        [ProtoMember]
        public MyObjectBuilder_Inventory Inventory;

        [ProtoMember, DefaultValue("")]
        public string ContainerType = "";
        public bool ShouldSerializeContainerType() { return ContainerType != ""; }


        public override void SetupForProjector()
        {
            base.SetupForProjector();
            if (Inventory != null)
                Inventory.Clear();
            if (ComponentContainer != null)
            {
                var comp = ComponentContainer.Components.Find((s) => s.Component.TypeId == typeof(MyObjectBuilder_Inventory));
                if (comp != null)
                    (comp.Component as MyObjectBuilder_Inventory).Clear();
            }
        }
    }
}
