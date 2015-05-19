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
        [ProtoMember]
        public float RequiredPowerInput;
        [ProtoMember, ModdableContentFile(".dds")]
        public string OverlayTexture;
        [ProtoMember]
        public float MinFov = 0.1f;
        [ProtoMember]
        public float MaxFov = 1.04719755f;
    }
}
