using VRage.ObjectBuilders;
using ProtoBuf;
using VRageMath;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CargoContainerDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        //TODO: remove - this is obsolete and should not be used, instead MyObjectBuilder_InventoryComponentDefinition should be used together with entity container definition.
        [ProtoMember]
        public Vector3 InventorySize;
    }
}
