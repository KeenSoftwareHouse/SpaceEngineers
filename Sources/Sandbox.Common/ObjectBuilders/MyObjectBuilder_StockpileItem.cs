using ProtoBuf;
using System;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_StockpileItem : MyObjectBuilder_Base
    {
        [ProtoMember]
        public int Amount;

        [ProtoMember]
        public MyObjectBuilder_PhysicalObject PhysicalContent;
    }
}
