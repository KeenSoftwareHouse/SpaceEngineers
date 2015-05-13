using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Data;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BlueprintClassDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember(4)]
        [ModdableContentFile("dds")]
        public string HighlightIcon;

        [ProtoMember(5)]
        [ModdableContentFile("dds")]
        public string InputConstraintIcon;

        [ProtoMember(6)]
        [ModdableContentFile("dds")]
        public string OutputConstraintIcon;
    }
}
