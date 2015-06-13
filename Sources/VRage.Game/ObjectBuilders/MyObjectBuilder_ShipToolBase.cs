using ProtoBuf;
using VRage.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using System.ComponentModel;

namespace Sandbox.Common.ObjectBuilders
{    
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ShipToolBase : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public MyObjectBuilder_Inventory Inventory;

        [ProtoMember, DefaultValue(true)]
        public bool UseConveyorSystem = true;

        public MyObjectBuilder_ShipToolBase()
        {
            // Overriding base default value.
            Enabled = false;
            DeformationRatio = 0.5f;
        }

        public override void SetupForProjector()
        {
            base.SetupForProjector();
            if (Inventory != null)
                Inventory.Clear();
        }
    }
}
