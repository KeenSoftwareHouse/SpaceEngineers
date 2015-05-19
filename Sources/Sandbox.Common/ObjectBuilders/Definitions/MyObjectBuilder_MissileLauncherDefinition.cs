using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders.VRageData;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_MissileLauncherDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember]
        public string ProjectileMissile;
    }
}
