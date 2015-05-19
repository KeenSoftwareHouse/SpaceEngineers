using System;
using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ToolbarItemActionParameter : MyObjectBuilder_Base
    {
        [ProtoMember] 
        public TypeCode TypeCode;

        [ProtoMember] 
        public string Value;
    }
}