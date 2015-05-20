using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Data;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_LargeTurretBaseDefinition : MyObjectBuilder_WeaponBlockDefinition
    {
        [ProtoMember, ModdableContentFile(".dds")]
        public string OverlayTexture;
        [ProtoMember]
        public bool AiEnabled = true;
        [ProtoMember]
        public int MinElevationDegrees = -180;
        [ProtoMember]
        public int MaxElevationDegrees = 180;
        [ProtoMember]
        public int MinAzimuthDegrees = -180;
        [ProtoMember]
        public int MaxAzimuthDegrees = 180;
        [ProtoMember]
        public bool IdleRotation = true;
        [ProtoMember]
        public float MaxRangeMeters = 800.0f;
        [ProtoMember]
        public float RotationSpeed = 0.005f;
        [ProtoMember]
        public float ElevationSpeed = 0.005f; 
    }
}
