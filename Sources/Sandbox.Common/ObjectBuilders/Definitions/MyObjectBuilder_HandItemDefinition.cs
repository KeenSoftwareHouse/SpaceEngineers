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
        [ProtoMember]
        public Quaternion LeftHandOrientation;

        [ProtoMember]
        public Vector3 LeftHandPosition;

        [ProtoMember]
        public Quaternion RightHandOrientation;

        [ProtoMember]
        public Vector3 RightHandPosition;

        [ProtoMember]
        public Quaternion ItemOrientation;

        [ProtoMember]
        public Vector3 ItemPosition;

        [ProtoMember]
        public Quaternion ItemWalkingOrientation;

        [ProtoMember]
        public Vector3 ItemWalkingPosition;

        [ProtoMember]
        public float BlendTime;

        [ProtoMember]
        public float XAmplitudeOffset;
        [ProtoMember]
        public float YAmplitudeOffset;
        [ProtoMember]
        public float ZAmplitudeOffset;

        [ProtoMember]
        public float XAmplitudeScale;
        [ProtoMember]
        public float YAmplitudeScale;
        [ProtoMember]
        public float ZAmplitudeScale;

        [ProtoMember]
        public float RunMultiplier;

        [ProtoMember]
        public Quaternion ItemWalkingOrientation3rd;

        [ProtoMember]
        public Vector3 ItemWalkingPosition3rd;

        [ProtoMember]
        public float AmplitudeMultiplier3rd;

        [ProtoMember, DefaultValue(true)]
        public bool SimulateLeftHand = true;

        [ProtoMember, DefaultValue(true)]
        public bool SimulateRightHand = true;

        [ProtoMember]
        public Quaternion ItemOrientation3rd;

        [ProtoMember]
        public Vector3 ItemPosition3rd;

        [ProtoMember]
        public string FingersAnimation;

        [ProtoMember]
        public Quaternion ItemShootOrientation;

        [ProtoMember]
        public Vector3 ItemShootPosition;

        [ProtoMember]
        public Quaternion ItemShootOrientation3rd;

        [ProtoMember]
        public Vector3 ItemShootPosition3rd;

        [ProtoMember]
        public float ShootBlend;

        [ProtoMember]
        public Vector3 MuzzlePosition;

        [ProtoMember]
        public Vector3 ShootScatter;

        [ProtoMember]
        public float ScatterSpeed;

        [ProtoMember]
        public SerializableDefinitionId PhysicalItemId;

        [ProtoMember]
        public Vector4 LightColor;

        [ProtoMember]
        public float LightFalloff;

        [ProtoMember]
        public float LightRadius;

        [ProtoMember]
        public float LightGlareSize;

        [ProtoMember]
        public float LightIntensityLower;
        
        [ProtoMember]
        public float LightIntensityUpper;

        [ProtoMember]
        public float ShakeAmountTarget;

        [ProtoMember]
        public float ShakeAmountNoTarget;
    }
}
