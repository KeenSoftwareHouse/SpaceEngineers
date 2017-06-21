using VRage.ObjectBuilders;
using ProtoBuf;
using VRageMath;
using System.ComponentModel;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace VRage.Game
{
    /// <summary>
    /// Enumeration defining where to get the weapon transform from.
    /// This does not include behavior of arms (anim/ik), which is driven separately by variables SimulateLeftHand and SimulateRightHand.
    /// </summary>
    public enum MyItemPositioningEnum
    {
        /// <summary>
        /// Weapon is placed according to sbc data file.
        /// </summary>
        TransformFromData,
        /// <summary>
        /// Weapon is placed according to animation.
        /// </summary>
        TransformFromAnim
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_HandItemDefinition : MyObjectBuilder_DefinitionBase
    {
        // position and orientation of hands
        [ProtoMember]
        public Quaternion LeftHandOrientation = Quaternion.Identity;
        [ProtoMember]
        public Vector3 LeftHandPosition;
        [ProtoMember]
        public Quaternion RightHandOrientation = Quaternion.Identity;
        [ProtoMember]
        public Vector3 RightHandPosition;

        // 1st person positioning (idle)
        [ProtoMember]
        public Quaternion ItemOrientation = Quaternion.Identity;
        [ProtoMember]
        public Vector3 ItemPosition;
        // 1st person positioning (walk/run/sprint)
        [ProtoMember]
        public Quaternion ItemWalkingOrientation = Quaternion.Identity;
        [ProtoMember]
        public Vector3 ItemWalkingPosition;
        // 1st person positioning (shoot)
        [ProtoMember]
        public Quaternion ItemShootOrientation = Quaternion.Identity;
        [ProtoMember]
        public Vector3 ItemShootPosition;
        // 1st person positioning (ironsight)
        [ProtoMember]
        public Quaternion ItemIronsightOrientation = Quaternion.Identity;
        [ProtoMember]
        public Vector3 ItemIronsightPosition;
        // 3rd person positioning (idle)
        [ProtoMember]
        public Quaternion ItemOrientation3rd = Quaternion.Identity;
        [ProtoMember]
        public Vector3 ItemPosition3rd;
        // 3rd person positioning (walk/run/sprint)
        [ProtoMember]
        public Quaternion ItemWalkingOrientation3rd = Quaternion.Identity;
        [ProtoMember]
        public Vector3 ItemWalkingPosition3rd;
        // 3rd person positioning (shoot)
        [ProtoMember]
        public Quaternion ItemShootOrientation3rd = Quaternion.Identity;
        [ProtoMember]
        public Vector3 ItemShootPosition3rd;
        
        // blending time between states
        [ProtoMember]
        public float BlendTime;
        // blending time to shooting (usually shorter than BlendTime)
        [ProtoMember]
        public float ShootBlend;

        // weapon shaking amplitude and offset in all directions
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
        // weapon shaking amplitude multiplier for sprinting
        [ProtoMember]
        public float RunMultiplier;
        // weapon shaking amplitude multiplier for 3rd person
        [ProtoMember]
        public float AmplitudeMultiplier3rd;

        // left hand inverse kinematics - if true, IK for left hand is turned on
        [ProtoMember, DefaultValue(true)]
        public bool SimulateLeftHand = true;
        // right hand inverse kinematics - if true, IK for right hand is turned on
        [ProtoMember, DefaultValue(true)]
        public bool SimulateRightHand = true;
        // old anim system: animation that should be played for fingers
        // new anim system: name of animation event that is triggered after switching to this weapon
        [ProtoMember]
        public string FingersAnimation;

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

        [ProtoMember]
        public List<ToolSound> ToolSounds;

        [ProtoMember]
        public string ToolMaterial = "Grinder";

        // item positioning - take position from this SBC data file or from animation?
        [ProtoMember]
        public MyItemPositioningEnum ItemPositioning = MyItemPositioningEnum.TransformFromData;
        [ProtoMember]
        public MyItemPositioningEnum ItemPositioning3rd = MyItemPositioningEnum.TransformFromData;
        [ProtoMember]
        public MyItemPositioningEnum ItemPositioningWalk = MyItemPositioningEnum.TransformFromData;
        [ProtoMember]
        public MyItemPositioningEnum ItemPositioningWalk3rd = MyItemPositioningEnum.TransformFromData;
        [ProtoMember]
        public MyItemPositioningEnum ItemPositioningShoot = MyItemPositioningEnum.TransformFromData;
        [ProtoMember]
        public MyItemPositioningEnum ItemPositioningShoot3rd = MyItemPositioningEnum.TransformFromData;
    }

    [ProtoContract, XmlType("ToolSound")]
    public struct ToolSound
    {
        [ProtoMember, XmlAttribute]
        public string type;

        [ProtoMember, XmlAttribute]
        public string subtype;

        [ProtoMember, XmlAttribute]
        public string sound;
    }
}
