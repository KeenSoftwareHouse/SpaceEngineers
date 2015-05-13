using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using System.Xml.Serialization;
using VRage.Data;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    public class MyJetpackThrustDefinition
    {
        [ProtoMember(1)]
        public string ThrustBone;

        [ProtoMember(2)]
        public Vector4 ThrustColor = new Vector4(Color.CornflowerBlue.ToVector3() * 0.7f, 0.75f);

        [ProtoMember(3)]
        public float ThrustGlareSize = 5.585f;

        [ProtoMember(4)]
        public string ThrustMaterial = "EngineThrustMiddle";

        [ProtoMember(5)]
        public float SideFlameOffset = 0.12f;

        [ProtoMember(6)]
        public float FrontFlameOffset = 0.04f;
    }

    [ProtoContract]
    public class MyBoneSetDefinition
    {
        [ProtoMember(1)]
        public string Name;

        [ProtoMember(2)]
        public string Bones;
    }

    [ProtoContract]
    public class MyMovementAnimationMapping
    {
        [ProtoMember(1), XmlAttribute]
        public string Name = null;

        [ProtoMember(2), XmlAttribute]
        public string AnimationSubtypeName;
    }

    [ProtoContract]
    public class MyObjectBuilder_MyFeetIKSettings
    {
        [ProtoMember(1)]
        public string MovementState;
        
        [ProtoMember(2)]
        public bool Enabled;

        [ProtoMember(3)]
        public float BelowReachableDistance; 

        [ProtoMember(4)]
        public float AboveReachableDistance; 

        [ProtoMember(5)]
        public float VerticalShiftUpGain;

        [ProtoMember(6)]
        public float VerticalShiftDownGain;

        [ProtoMember(7)]
        public float FootLenght;

        [ProtoMember(8)]
        public float FootWidth;

        [ProtoMember(9)]
        public float AnkleHeight;
    }


    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CharacterDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember(1)]
        public string Name;

        [ProtoMember(2)]
        [ModdableContentFile("mwm")]
        public string Model;

        [ProtoMember(3)]
        [ModdableContentFile("dds")]
        public string ReflectorTexture = @"Textures\Lights\reflector.dds";

        [ProtoMember(4)]
        public string LeftGlare = null;

        [ProtoMember(5)]
        public string RightGlare = null;

        [ProtoMember(6)]
        public string Skeleton = "Humanoid";

        [ProtoMember(7)]
        public float LightGlareSize = 0.02f;

        [ProtoMember(8)]
        public bool JetpackAvailable = false;

        [ProtoMember(9)]
        public float JetpackSlowdown = 0.975f;

        [ProtoMember(10), XmlArrayItem("Thrust")]
        public MyJetpackThrustDefinition[] Thrusts;

        [ProtoMember(11), XmlArrayItem("BoneSet")]
        public MyBoneSetDefinition[] BoneSets;

        [ProtoMember(12)]
        public string LeftLightBone = null;

        [ProtoMember(13)]
        public string RightLightBone = null;

        [ProtoMember(14)]
        public string HeadBone = null;

        [ProtoMember(15)]
        public string LeftHandIKStartBone = null;

        [ProtoMember(16)]
        public string LeftHandIKEndBone = null;

        [ProtoMember(17)]
        public string RightHandIKStartBone = null;

        [ProtoMember(18)]
        public string RightHandIKEndBone = null;

        [ProtoMember(19)]
        public string WeaponBone = null;

        [ProtoMember(20)]
        public string Camera3rdBone = null;

        [ProtoMember(21)]
        public string LeftHandItemBone = null;

        [ProtoMember(22)]
        public string LeftForearmBone = null;

        [ProtoMember(23)]
        public string LeftUpperarmBone = null;

        [ProtoMember(24)]
        public string RightForearmBone = null;

        [ProtoMember(25)]
        public string RightUpperarmBone = null;

        [ProtoMember(26)]
        public string SpineBone = null;

        [ProtoMember(27)]
        public float BendMultiplier1st = 1;

        [ProtoMember(28)]
        public float BendMultiplier3rd = 1;

        [ProtoMember(29)]
        [XmlArrayItem("Material")]
        public string[] MaterialsDisabledIn1st;

        [ProtoMember(30), XmlArrayItem("Mapping")]
        public MyMovementAnimationMapping[] AnimationMappings;

        [ProtoMember(31)]
        public float Mass = 100f;

        [ProtoMember(32)]
        public float MaxHealth = 100f;

        [ProtoMember(33)]
        public string ModelRootBoneName;

        [ProtoMember(34)]
        public string LeftHipBoneName;

        [ProtoMember(35)]
        public string LeftKneeBoneName;

        [ProtoMember(36)]
        public string LeftAnkleBoneName;

        [ProtoMember(37)]
        public string RightHipBoneName;

        [ProtoMember(38)]
        public string RightKneeBoneName;

        [ProtoMember(39)]
        public string RightAnkleBoneName;

        [ProtoMember(40)]
        public bool FeetIKEnabled = false;

        [ProtoMember(41), XmlArrayItem("FeetIKSettings")]
        public MyObjectBuilder_MyFeetIKSettings[] IKSettings;        

        [ProtoMember(42)]
        public string RightHandItemBone;

        [ProtoMember(43)]
        public bool NeedsOxygen = false;

        [ProtoMember(44)]
        public string RagdollDataFile;

        [ProtoMember(45), XmlArrayItem("BoneSet")]
        public MyBoneSetDefinition[] RagdollBonesMappings;

        [ProtoMember(46)]
        public float OxygenConsumption = 10f;

        [ProtoMember(47)]
        public float PressureLevelForLowDamage = 0.5f;

        [ProtoMember(48)]
        public float DamageAmountAtZeroPressure = 7f;

        [ProtoMember(49)]
        public float OxygenCapacity = 6000f;

        //Character control
        [ProtoMember(50)]
        public bool VerticalPositionFlyingOnly = false;
        [ProtoMember(51)]
        public bool UseOnlyWalking = true;

        [ProtoMember(52)]
        public float MaxSlope = 60;
        [ProtoMember(53)]
        public float MaxSprintSpeed = 11;

        [ProtoMember(54)]
        public float MaxRunSpeed = 11;
        [ProtoMember(55)]
        public float MaxBackrunSpeed = 11;
        [ProtoMember(56)]
        public float MaxRunStrafingSpeed = 11;

        [ProtoMember(57)]
        public float MaxWalkSpeed = 6;
        [ProtoMember(58)]
        public float MaxBackwalkSpeed = 6;
        [ProtoMember(59)]
        public float MaxWalkStrafingSpeed = 6;

        [ProtoMember(60)]
        public float MaxCrouchWalkSpeed = 4;
        [ProtoMember(61)]
        public float MaxCrouchBackwalkSpeed = 4;
        [ProtoMember(62)]
        public float MaxCrouchStrafingSpeed = 4;

        [ProtoMember(63)]
        public float CharacterHeadSize = 0.55f;
        [ProtoMember(64)]
        public float CharacterHeadHeight = 0.25f;
        [ProtoMember(65)]
        public float CharacterCollisionScale = 1.0f;

        [ProtoMember(66)]
        public string HelmetVariation;

        [ProtoMember(67)]
        public string DeathSoundName = "";

        [ProtoMember(68)]
        public bool VisibleOnHud = true;

        [ProtoMember(69)]
        public string RagdollRootBody = String.Empty;
    }
}
