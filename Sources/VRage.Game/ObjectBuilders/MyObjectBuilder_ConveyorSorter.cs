using ProtoBuf;
using System.Collections.Generic;
using System.ComponentModel;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
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
        }
    }
}
