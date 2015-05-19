using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ButtonPanelDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember]
        public int ButtonCount;

        [ProtoMember]
        public string[] ButtonSymbols;

        [ProtoMember]
        public Vector4[] ButtonColors;

        [ProtoMember]
        public Vector4 UnassignedButtonColor;
    }
}
