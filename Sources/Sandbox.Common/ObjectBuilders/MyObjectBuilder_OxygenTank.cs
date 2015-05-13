using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_OxygenTank : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember(1)]
        public bool IsStockpiling;
        [ProtoMember(2)]
        public float FilledRatio;
        [ProtoMember(3)]
        public MyObjectBuilder_Inventory Inventory;
        [ProtoMember(4)]
        public bool AutoRefill;

        public override void SetupForProjector()
        {
            base.SetupForProjector();
            FilledRatio = 0f;

            if (Inventory != null)
            {
                Inventory.Clear();
            }
        }
    }
}
