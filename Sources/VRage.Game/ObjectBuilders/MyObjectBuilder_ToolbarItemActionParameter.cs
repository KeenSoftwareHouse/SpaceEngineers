using System;
using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
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