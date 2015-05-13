using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AutomaticRifle : MyObjectBuilder_EntityBase
    {
     //   [ProtoMember(1)]
        public int CurrentAmmo;
        public bool ShouldSerializeCurrentAmmo() { return false; }

        [ProtoMember(1)]
        public MyObjectBuilder_GunBase GunBase;
    }
}
