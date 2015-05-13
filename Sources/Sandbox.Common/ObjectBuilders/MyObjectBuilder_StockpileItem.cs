using ProtoBuf;
using System;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_StockpileItem : MyObjectBuilder_Base
    {
        [ProtoMember(1)]
        public int Amount;

        [ProtoMember(2)]
        public MyObjectBuilder_PhysicalObject PhysicalContent;
    }
}
