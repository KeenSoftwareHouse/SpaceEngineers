using ProtoBuf;
using System.ComponentModel;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_SmallGatlingGun : MyObjectBuilder_UserControllableGun
    {
        [ProtoMember(1)]
        public MyObjectBuilder_Inventory Inventory;

        [ProtoMember(2), DefaultValue(true)]
        public bool UseConveyorSystem = true;

        [ProtoMember(3)]
        public MyObjectBuilder_GunBase GunBase;

        public override void SetupForProjector()
        {
            base.SetupForProjector();
            if (Inventory != null)
                Inventory.Clear();
        }
    }
}
