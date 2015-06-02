using ProtoBuf;
using VRage.ObjectBuilders;
using VRageMath;
using System.Collections.Generic;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BlockGroup : MyObjectBuilder_Base
    {
        [ProtoMember]
        public string Name;

        [ProtoMember]
        public List<Vector3I> Blocks = new List<Vector3I>();
    }
}
