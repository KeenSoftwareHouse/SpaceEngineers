using System.Collections.Generic;
using ProtoBuf;
using VRageMath;
using System.Xml.Serialization;
using VRage.Game;
using VRage.ObjectBuilders;
using VRage.Serialization;


namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_SmallMissileLauncher : MyObjectBuilder_UserControllableGun
    {
        [ProtoMember]
        public MyObjectBuilder_Inventory Inventory;

        [ProtoMember]
        public bool UseConveyorSystem = true;

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public MyObjectBuilder_GunBase GunBase;

        public override void SetupForProjector()
        {
            base.SetupForProjector();
            if (Inventory != null)
                Inventory.Clear();
            if (ComponentContainer != null)
            {
                var comp = ComponentContainer.Components.Find((s) => s.Component.TypeId == typeof(MyObjectBuilder_Inventory));
                if (comp != null)
                    (comp.Component as MyObjectBuilder_Inventory).Clear();
            }
        }
    }
}
