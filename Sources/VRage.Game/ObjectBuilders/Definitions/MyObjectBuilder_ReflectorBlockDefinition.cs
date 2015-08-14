using System;
using VRage.ObjectBuilders;
using VRage.Data;
using ProtoBuf;

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
