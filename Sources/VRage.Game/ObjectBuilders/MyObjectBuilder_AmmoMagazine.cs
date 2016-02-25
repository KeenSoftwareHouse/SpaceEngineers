using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AmmoMagazine : MyObjectBuilder_PhysicalObject
    {
        [ProtoMember]
        public int ProjectilesCount;
    }
}
