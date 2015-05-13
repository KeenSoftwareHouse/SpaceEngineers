using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using VRage.Data;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CameraBlockDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember(1)]
        public float RequiredPowerInput;
        [ProtoMember(2), ModdableContentFile(".dds")]
        public string OverlayTexture;
        [ProtoMember(3)]
        public float MinFov = 0.1f;
        [ProtoMember(4)]
        public float MaxFov = 1.04719755f;
    }
}
