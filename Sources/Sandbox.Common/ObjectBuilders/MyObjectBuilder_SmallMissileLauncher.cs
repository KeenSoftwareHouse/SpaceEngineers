using System.Collections.Generic;
using ProtoBuf;
using VRageMath;
using System.Xml.Serialization;


namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_SmallMissileLauncher : MyObjectBuilder_UserControllableGun
    {
        [ProtoMember(1)]
        public MyObjectBuilder_Inventory Inventory;

        [ProtoMember(2)]
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
