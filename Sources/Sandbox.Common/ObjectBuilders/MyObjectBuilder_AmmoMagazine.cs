using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AmmoMagazine : MyObjectBuilder_PhysicalObject
    {
        [ProtoMember]
        public int ProjectilesCount;
    }
}
