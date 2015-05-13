using ProtoBuf;
using System.ComponentModel;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Drill : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember(1)]
        public MyObjectBuilder_Inventory Inventory;

        [ProtoMember(2), DefaultValue(true)]
        public bool UseConveyorSystem = true;

        public MyObjectBuilder_Drill()
        {
            // Overriding base default value.
            Enabled = false;
        }

        public override void SetupForProjector()
        {
            base.SetupForProjector();
            if (Inventory != null)
                Inventory.Clear();
        }
    }
}
