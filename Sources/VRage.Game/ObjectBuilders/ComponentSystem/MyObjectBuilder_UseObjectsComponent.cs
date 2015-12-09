using ProtoBuf;
using Sandbox.Common.ObjectBuilders.ComponentSystem;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
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
