using ProtoBuf;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AutomaticRifle : MyObjectBuilder_EntityBase
    {
     //   [ProtoMember]
        public int CurrentAmmo;
        public bool ShouldSerializeCurrentAmmo() { return false; }

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public MyObjectBuilder_GunBase GunBase;
    }
}
