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
        [ProtoMember(1)]
        public int ButtonCount;

        [ProtoMember(2)]
        public string[] ButtonSymbols;

        [ProtoMember(3)]
        public Vector4[] ButtonColors;

        [ProtoMember(4)]
        public Vector4 UnassignedButtonColor;
    }
}
