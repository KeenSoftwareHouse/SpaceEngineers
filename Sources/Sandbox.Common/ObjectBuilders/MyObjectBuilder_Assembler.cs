using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Sandbox.Common.ObjectBuilders
{

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Assembler : MyObjectBuilder_ProductionBlock
    {
        //[ProtoMember, DefaultValue(null)]
        //public MyDefinitionId? CurrentBlueprint = null;
        //public bool ShouldSerializeCurrentBlueprintResult() { return CurrentBlueprint.HasValue; }

        [ProtoMember]
        public float CurrentProgress;

        [ProtoMember]
        public bool DisassembleEnabled;

        [ProtoMember]
        [XmlArrayItem("Item")]
        public QueueItem[] OtherQueue;

        [ProtoMember]
        public bool RepeatAssembleEnabled;

        [ProtoMember]
        public bool RepeatDisassembleEnabled;

        [ProtoMember]
        public bool SlaveEnabled;
    }
}
