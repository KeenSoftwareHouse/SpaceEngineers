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
        [ProtoMember(1)]
        public bool IsWhiteList;
        [ProtoMember(2)]
        public HashSet<SerializableDefinitionId> DefinitionIds = new HashSet<SerializableDefinitionId>();
        [ProtoMember(3)]
        public List<byte> DefinitionTypes = new List<byte>();
        [ProtoMember(4)]
        public bool DrainAll;

        [ProtoMember(5)]
        public MyObjectBuilder_Inventory Inventory;

        [ProtoMember(6), DefaultValue(null)]
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
