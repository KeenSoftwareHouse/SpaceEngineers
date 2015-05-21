using ProtoBuf;

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
        public MyObjectBuilder_GunBase GunBase;
    }
}
