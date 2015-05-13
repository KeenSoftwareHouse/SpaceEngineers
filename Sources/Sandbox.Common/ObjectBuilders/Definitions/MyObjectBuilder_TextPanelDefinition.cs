using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_TextPanelDefinition : MyObjectBuilder_CubeBlockDefinition
    {     
        [ProtoMember(1)]
        public float RequiredPowerInput = 0.001f;

        [ProtoMember(2)]
        public int TextureResolution = 512;

        [ProtoMember(3)]
        public int TextureAspectRadio = 1;
    }
}
