using ProtoBuf;
using System.Xml.Serialization;
using VRage.Game;
using VRage.Game.ObjectBuilders;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_AutomaticRifle : MyObjectBuilder_EntityBase, IMyObjectBuilder_GunObject<MyObjectBuilder_GunBase>
    {
     //   [ProtoMember]
        public int CurrentAmmo;
        public bool ShouldSerializeCurrentAmmo() { return false; }

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public MyObjectBuilder_GunBase GunBase;

        MyObjectBuilder_DeviceBase IMyObjectBuilder_GunObject<MyObjectBuilder_GunBase>.DeviceBase
        {
            get
            {
                return GunBase;
            }
            set
            {
                GunBase = value as MyObjectBuilder_GunBase;
            }
        }
    }
}
