using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using VRage.Data;
using VRage.ObjectBuilders;
using System.ComponentModel;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    public class MyJetpackThrustDefinition
    {
        [ProtoMember]
        public string ThrustBone;

        [ProtoMember]
        public float SideFlameOffset = 0.12f;

        [ProtoMember]
        public float FrontFlameOffset = 0.04f;
    }

    [ProtoContract]
    public class MyObjectBuilder_JetpackDefinition
    {
        [ProtoMember]
        [XmlArrayItem("Thrust")]
        public List<MyJetpackThrustDefinition> Thrusts;

        [ProtoMember]
        public MyObjectBuilder_ThrustDefinition ThrustProperties;
    }

	[ProtoContract]
	public class SuitResourceDefinition
	{
		[ProtoMember]
		public SerializableDefinitionId Id;

		[ProtoMember]
		public float MaxCapacity;

		[ProtoMember]
		public float Throughput;
	}

    [ProtoContract]
    public class MyBoneSetDefinition
    {
        [ProtoMember]
        public string Name;

        [ProtoMember]
        public string Bones;
    }

    [ProtoContract]
    public class MyMovementAnimationMapping
    {
        [ProtoMember, XmlAttribute]
        public string Name = null;

        [ProtoMember, XmlAttribute]
        public string AnimationSubtypeName;
    }

    [ProtoContract]
    public class MyObjectBuilder_MyFeetIKSettings
    {
        [ProtoMember]
        public string MovementState;
        
        [ProtoMember]
        public bool Enabled;

        [ProtoMember]
        public float BelowReachableDistance; 

        [ProtoMember]
        public float AboveReachableDistance; 

        [ProtoMember]
        public float VerticalShiftUpGain;

        [ProtoMember]
        public float VerticalShiftDownGain;

        [ProtoMember]
        public float FootLenght;

        [ProtoMember]
        public float FootWidth;

        [ProtoMember]
        public float AnkleHeight;
    }


    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CharacterDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        public string Name;

        [ProtoMember]
        [ModdableContentFile("mwm")]
        public string Model;

        [ProtoMember]
        [ModdableContentFile("dds")]
        public string ReflectorTexture = @"Textures\Lights\reflector.dds";

        [ProtoMember]
        public string LeftGlare = null;

        [ProtoMember]
        public string RightGlare = null;

        [ProtoMember]
        public string Skeleton = "Humanoid";

        [ProtoMember]
        public float LightGlareSize = 0.02f;

        [ProtoMember]
        public MyObjectBuilder_JetpackDefinition Jetpack;

	    [ProtoMember]
		[XmlArrayItem("Resource")]
	    public List<SuitResourceDefinition> SuitResourceStorage;

		[ProtoMember, XmlArrayItem("BoneSet")]
        public MyBoneSetDefinition[] BoneSets;

        [ProtoMember, XmlArrayItem("BoneSet")]
        public MyBoneSetDefinition[] BoneLODs;

        [ProtoMember]
        public string LeftLightBone = null;

        [ProtoMember]
        public string RightLightBone = null;

        [ProtoMember]
        public string HeadBone = null;

        [ProtoMember]
        public string LeftHandIKStartBone = null;

        [ProtoMember]
        public string LeftHandIKEndBone = null;

        [ProtoMember]
        public string RightHandIKStartBone = null;

        [ProtoMember]
        public string RightHandIKEndBone = null;

        [ProtoMember]
        public string WeaponBone = null;

        [ProtoMember]
        public string Camera3rdBone = null;

        [ProtoMember]
        public string LeftHandItemBone = null;

        [ProtoMember]
        public string LeftForearmBone = null;

        [ProtoMember]
        public string LeftUpperarmBone = null;

        [ProtoMember]
        public string RightForearmBone = null;

        [ProtoMember]
        public string RightUpperarmBone = null;

        [ProtoMember]
        public string SpineBone = null;

        [ProtoMember]
        public float BendMultiplier1st = 1;

        [ProtoMember]
        public float BendMultiplier3rd = 1;

        [ProtoMember]
        [XmlArrayItem("Material")]
        public string[] MaterialsDisabledIn1st;

        [ProtoMember, XmlArrayItem("Mapping")]
        public MyMovementAnimationMapping[] AnimationMappings;

		[ProtoMember]
		public string Stats;

        [ProtoMember]
        public float Mass = 100f;

        [ProtoMember]
        public string ModelRootBoneName;

        [ProtoMember]
        public string LeftHipBoneName;

        [ProtoMember]
        public string LeftKneeBoneName;

        [ProtoMember]
        public string LeftAnkleBoneName;

        [ProtoMember]
        public string RightHipBoneName;

        [ProtoMember]
        public string RightKneeBoneName;

        [ProtoMember]
        public string RightAnkleBoneName;

        [ProtoMember]
        public bool FeetIKEnabled = false;

        [ProtoMember, XmlArrayItem("FeetIKSettings")]
        public MyObjectBuilder_MyFeetIKSettings[] IKSettings;        

        [ProtoMember]
        public string RightHandItemBone;

        [ProtoMember]
        public bool NeedsOxygen = false;

        [ProtoMember]
        public string RagdollDataFile;

        [ProtoMember, XmlArrayItem("BoneSet")]
        public MyBoneSetDefinition[] RagdollBonesMappings;

        [ProtoMember, XmlArrayItem("BoneSet")]
        public MyBoneSetDefinition[] RagdollPartialSimulations;

        [ProtoMember]
        public float OxygenConsumption = 10f;

        [ProtoMember]
        public float PressureLevelForLowDamage = 0.5f;

        [ProtoMember]
        public float DamageAmountAtZeroPressure = 7f;

        [ProtoMember]
        public float OxygenCapacity = 6000f;

        //Character control
        [ProtoMember]
        public bool VerticalPositionFlyingOnly = false;
        [ProtoMember]
        public bool UseOnlyWalking = true;

        [ProtoMember]
        public float MaxSlope = 60;
        [ProtoMember]
        public float MaxSprintSpeed = 11;

        [ProtoMember]
        public float MaxRunSpeed = 11;
        [ProtoMember]
        public float MaxBackrunSpeed = 11;
        [ProtoMember]
        public float MaxRunStrafingSpeed = 11;

        [ProtoMember]
        public float MaxWalkSpeed = 6;
        [ProtoMember]
        public float MaxBackwalkSpeed = 6;
        [ProtoMember]
        public float MaxWalkStrafingSpeed = 6;

        [ProtoMember]
        public float MaxCrouchWalkSpeed = 4;
        [ProtoMember]
        public float MaxCrouchBackwalkSpeed = 4;
        [ProtoMember]
        public float MaxCrouchStrafingSpeed = 4;

        [ProtoMember]
        public float CharacterHeadSize = 0.55f;
        [ProtoMember]
        public float CharacterHeadHeight = 0.25f;
        [ProtoMember]
        public float CharacterCollisionScale = 1.0f;
        [ProtoMember]
        public float CharacterCollisionWidth = 1.0f;
        [ProtoMember]
        public float CharacterCollisionHeight = 1.8f;
        [ProtoMember]
        public float CharacterCollisionCrouchHeight = 1.25f;

        [ProtoMember]
        public string HelmetVariation;

        [ProtoMember]
        public string DeathSoundName = "";

        [ProtoMember]
        public bool VisibleOnHud = true;

        [ProtoMember]
        public bool UsableByPlayer = true;

        [ProtoMember]
        public string RagdollRootBody = String.Empty;

        [ProtoMember, DefaultValue(null)]
        public MyObjectBuilder_InventoryDefinition Inventory;

        [ProtoMember]
        public string EnabledComponents;

        [ProtoMember]
        public bool EnableSpawnInventoryAsContainer = false;
        
        [ProtoMember, DefaultValue(null)]
        public SerializableDefinitionId? InventorySpawnContainerId;

        [ProtoMember]
        public float LootingTime = 5 * 60f; // default from SE

        [ProtoMember]
        public string InitialAnimation = "Idle";

        [ProtoMember]
        public float ImpulseLimit = float.PositiveInfinity;
    }
}
