using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_MyProgrammableBlock : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember(1)]
        public string Program = null;

        [ProtoMember(2)]
        public string Storage ="";

        [ProtoMember(3)]
        public string DefaultRunArgument = null;

        [ProtoMember(4)]
        public bool ClearArgumentOnRun = false;
    }
}
