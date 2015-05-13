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
        [ProtoMember(1), ModdableContentFile(".dds")]
        public string OverlayTexture;
        [ProtoMember(2)]
        public bool AiEnabled = true;
        [ProtoMember(3)]
        public int MinElevationDegrees = -180;
        [ProtoMember(4)]
        public int MaxElevationDegrees = 180;
        [ProtoMember(5)]
        public int MinAzimuthDegrees = -180;
        [ProtoMember(6)]
        public int MaxAzimuthDegrees = 180;
        [ProtoMember(7)]
        public bool IdleRotation = true;
        [ProtoMember(8)]
        public float MaxRangeMeters = 800.0f;
        [ProtoMember(9)]
        public float RotationSpeed = 0.005f;
        [ProtoMember(10)]
        public float ElevationSpeed = 0.005f; 
    }
}
