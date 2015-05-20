using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using System.Collections.Generic;
using System.ComponentModel;

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

        [ProtoMember, DefaultValue(null)]
        public string ContainerType = null;
        public bool ShouldSerializeContainerType() { return ContainerType != null; }


        public override void SetupForProjector()
        {
            base.SetupForProjector();
            if (Inventory != null)
                Inventory.Clear();
        }
    }
}
