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
        [ProtoMember]
        public float RequiredPowerInput = 0.001f;

        [ProtoMember]
        public int TextureResolution = 512;

        [ProtoMember]
        public int TextureAspectRadio = 1;
    }
}
