﻿using ProtoBuf;
using System.Collections.Generic;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ConstructionStockpile : MyObjectBuilder_Base
    {
        [ProtoMember]
        public List<MyObjectBuilder_StockpileItem> Items = new List<MyObjectBuilder_StockpileItem>();
    }
}
