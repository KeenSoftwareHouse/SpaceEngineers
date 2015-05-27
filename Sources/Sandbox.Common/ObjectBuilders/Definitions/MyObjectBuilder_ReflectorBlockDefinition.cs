using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders.VRageData;
using VRage.Data;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ReflectorBlockDefinition : MyObjectBuilder_LightingBlockDefinition
    {
        [ProtoMember]
        [ModdableContentFile("dds")]
        public string ReflectorTexture = @"Textures\Lights\reflector_large.dds";
    }
}
