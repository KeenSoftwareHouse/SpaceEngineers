using VRage.ObjectBuilders;
using ProtoBuf;
using VRageMath;

namespace VRage.Game
{
    // MZ: Move conveyor definitions to space? Currently conveyor obj builders are referenced by cube grids, so i am leaving definition here
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ConveyorSorterDefinition : MyObjectBuilder_CubeBlockDefinition
    {
	    [ProtoMember]
	    public string ResourceSinkGroup;

        [ProtoMember]
        public float PowerInput = 0.001f;

        [ProtoMember]
        public Vector3 InventorySize;
    }
}
