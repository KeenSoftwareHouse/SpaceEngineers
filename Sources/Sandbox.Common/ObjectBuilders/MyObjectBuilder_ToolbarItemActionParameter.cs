using System;
using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ToolbarItemActionParameter : MyObjectBuilder_Base
    {
        [ProtoMember(1)] 
        public TypeCode TypeCode;

        [ProtoMember(2)] 
        public string Value;
    }
}