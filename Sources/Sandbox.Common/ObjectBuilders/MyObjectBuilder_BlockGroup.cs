using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BlockGroup : MyObjectBuilder_Base
    {
        [ProtoMember]
        public String Name;

        [ProtoMember]
        public List<Vector3I> Blocks = new List<Vector3I>();
    }
}
