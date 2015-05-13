using System.ComponentModel;
using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Reactor : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember(1)]
        public MyObjectBuilder_Inventory Inventory;

        [ProtoMember(2), DefaultValue(true)]
        public bool UseConveyorSystem = true;

        public override void SetupForProjector()
        {
            base.SetupForProjector();
            if (Inventory != null)
                Inventory.Clear();
        }
    }
}
