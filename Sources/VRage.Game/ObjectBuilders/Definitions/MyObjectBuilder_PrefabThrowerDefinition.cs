using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_PrefabThrowerDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        public float? Mass = null;

        [ProtoMember]
        public float MaxSpeed = 80; //m/sec

        [ProtoMember]
        public float MinSpeed = 1; //m/sec

        [ProtoMember]
        public float PushTime = 1; //sec

        [ProtoMember]
        public string PrefabToThrow;

        [ProtoMember]
        public string ThrowSound;
    }
}
