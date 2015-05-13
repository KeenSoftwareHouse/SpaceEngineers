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
        //[ProtoMember(1), DefaultValue(null)]
        //public MyDefinitionId? CurrentBlueprint = null;
        //public bool ShouldSerializeCurrentBlueprintResult() { return CurrentBlueprint.HasValue; }

        [ProtoMember(2)]
        public float CurrentProgress;

        [ProtoMember(4)]
        public bool DisassembleEnabled;

        [ProtoMember(5)]
        [XmlArrayItem("Item")]
        public QueueItem[] OtherQueue;

        [ProtoMember(3)]
        public bool RepeatAssembleEnabled;

        [ProtoMember(6)]
        public bool RepeatDisassembleEnabled;

        [ProtoMember(7)]
        public bool SlaveEnabled;
    }
}
