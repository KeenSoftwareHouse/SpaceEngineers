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
        [ProtoMember]
        public MyObjectBuilder_Inventory Inventory;

        [ProtoMember]
        public bool UseConveyorSystem = true;

        [ProtoMember]
        public MyObjectBuilder_GunBase GunBase;

        public override void SetupForProjector()
        {
            base.SetupForProjector();
            if (Inventory != null)
                Inventory.Clear();
        }
    }
}
