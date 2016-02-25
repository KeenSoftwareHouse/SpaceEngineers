using ProtoBuf;
using System.ComponentModel;
using VRage.ObjectBuilders;
using VRageMath;

namespace VRage.Game.ObjectBuilders.ComponentSystem
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_UseObjectsComponent : MyObjectBuilder_ComponentBase
    {
        [ProtoMember]
        public uint CustomDetectorsCount = 0;

        [ProtoMember, DefaultValue(null)]
        public string[] CustomDetectorsNames = null;

        [ProtoMember, DefaultValue(null)]
        public Matrix[] CustomDetectorsMatrices = null;
    }
}
