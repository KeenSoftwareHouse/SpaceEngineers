using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_HandItemDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember(1)]
        public Quaternion LeftHandOrientation;

        [ProtoMember(2)]
        public Vector3 LeftHandPosition;

        [ProtoMember(3)]
        public Quaternion RightHandOrientation;

        [ProtoMember(4)]
        public Vector3 RightHandPosition;

        [ProtoMember(5)]
        public Quaternion ItemOrientation;

        [ProtoMember(6)]
        public Vector3 ItemPosition;

        [ProtoMember(7)]
        public Quaternion ItemWalkingOrientation;

        [ProtoMember(8)]
        public Vector3 ItemWalkingPosition;

        [ProtoMember(9)]
        public float BlendTime;

        [ProtoMember(10)]
        public float XAmplitudeOffset;
        [ProtoMember(11)]
        public float YAmplitudeOffset;
        [ProtoMember(12)]
        public float ZAmplitudeOffset;

        [ProtoMember(13)]
        public float XAmplitudeScale;
        [ProtoMember(14)]
        public float YAmplitudeScale;
        [ProtoMember(15)]
        public float ZAmplitudeScale;

        [ProtoMember(16)]
        public float RunMultiplier;

        [ProtoMember(17)]
        public Quaternion ItemWalkingOrientation3rd;

        [ProtoMember(18)]
        public Vector3 ItemWalkingPosition3rd;

        [ProtoMember(19)]
        public float AmplitudeMultiplier3rd;

        [ProtoMember(20), DefaultValue(true)]
        public bool SimulateLeftHand = true;

        [ProtoMember(21), DefaultValue(true)]
        public bool SimulateRightHand = true;

        [ProtoMember(22)]
        public Quaternion ItemOrientation3rd;

        [ProtoMember(23)]
        public Vector3 ItemPosition3rd;

        [ProtoMember(24)]
        public string FingersAnimation;

        [ProtoMember(25)]
        public Quaternion ItemShootOrientation;

        [ProtoMember(26)]
        public Vector3 ItemShootPosition;

        [ProtoMember(27)]
        public Quaternion ItemShootOrientation3rd;

        [ProtoMember(28)]
        public Vector3 ItemShootPosition3rd;

        [ProtoMember(29)]
        public float ShootBlend;

        [ProtoMember(30)]
        public Vector3 MuzzlePosition;

        [ProtoMember(31)]
        public Vector3 ShootScatter;

        [ProtoMember(32)]
        public float ScatterSpeed;

        [ProtoMember(33)]
        public SerializableDefinitionId PhysicalItemId;

        [ProtoMember(34)]
        public Vector4 LightColor;

        [ProtoMember(35)]
        public float LightFalloff;

        [ProtoMember(36)]
        public float LightRadius;

        [ProtoMember(37)]
        public float LightGlareSize;

        [ProtoMember(38)]
        public float LightIntensityLower;
        
        [ProtoMember(39)]
        public float LightIntensityUpper;

        [ProtoMember(40)]
        public float ShakeAmountTarget;

        [ProtoMember(41)]
        public float ShakeAmountNoTarget;
    }
}
