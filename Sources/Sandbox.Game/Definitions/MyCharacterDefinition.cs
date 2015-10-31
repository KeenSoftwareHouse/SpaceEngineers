﻿using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Entities.Character;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_CharacterDefinition))]
    public class MyCharacterDefinition : MyDefinitionBase
    {
        public string Name;
        public string Model;
        public string ReflectorTexture;
        public string LeftGlare;
        public string RightGlare;
        public string LeftLightBone;
        public string RightLightBone;
        public float LightGlareSize;
        public string HeadBone;
        public string Camera3rdBone;
        public string LeftHandIKStartBone;
        public string LeftHandIKEndBone;
        public string RightHandIKStartBone;
        public string RightHandIKEndBone;
        public string WeaponBone;
        public string LeftHandItemBone;
        public string Skeleton;
        public string LeftForearmBone;
        public string LeftUpperarmBone;
        public string RightForearmBone;
        public string RightUpperarmBone;
        public string SpineBone;
        public float BendMultiplier1st;
        public float BendMultiplier3rd;
        public bool NeedsOxygen;
        public float OxygenConsumption;
        public float PressureLevelForLowDamage;
        public float DamageAmountAtZeroPressure;
        public float OxygenCapacity;
        public string HelmetVariation;
        public string DeathSoundName;
        public bool VisibleOnHud;
        public bool UsableByPlayer;

        // Bones for foot placement IK
        public bool FeetIKEnabled = false;
        public string ModelRootBoneName;
        public string LeftHipBoneName;
        public string LeftKneeBoneName;
        public string LeftAnkleBoneName;
        public string RightHipBoneName;
        public string RightKneeBoneName;
        public string RightAnkleBoneName;
        
        // Ragdoll data
        public string RagdollDataFile;
        public Dictionary<string, string[]> RagdollBonesMappings = new Dictionary<string, string[]>();
        public Dictionary<string, string[]> RagdollPartialSimulations = new Dictionary<string, string[]>();
        
        public string RagdollRootBody;

        public Dictionary<MyCharacterMovementEnum, MyFeetIKSettings> FeetIKSettings;

	    public List<SuitResourceDefinition> SuitResourceStorage = null;
        public MyObjectBuilder_JetpackDefinition Jetpack = null;
        public Dictionary<string, string[]> BoneSets = new Dictionary<string, string[]>();
        public Dictionary<float, string[]> BoneLODs = new Dictionary<float, string[]>();
        public Dictionary<string, string> AnimationNameToSubtypeName = new Dictionary<string, string>();
        public string[] MaterialsDisabledIn1st;
		public string Stats;

        public float Mass;
        public float ImpulseLimit;
        public string RighHandItemBone;

        //Character control
        public bool VerticalPositionFlyingOnly;
        public bool UseOnlyWalking;
                
        public float MaxSlope;
        public float MaxSprintSpeed;

        public float MaxRunSpeed;
        public float MaxBackrunSpeed;
        public float MaxRunStrafingSpeed;

        public float MaxWalkSpeed;
        public float MaxBackwalkSpeed;
        public float MaxWalkStrafingSpeed;

        public float MaxCrouchWalkSpeed;
        public float MaxCrouchBackwalkSpeed;
        public float MaxCrouchStrafingSpeed;

        public float CharacterHeadSize;
        public float CharacterHeadHeight;
        public float CharacterCollisionScale;
        public float CharacterCollisionHeight;
        public float CharacterCollisionWidth;
        public float CharacterCollisionCrouchHeight;

        public float CharacterWidth;
        public float CharacterHeight;
        public float CharacterLength;

        public MyObjectBuilder_InventoryDefinition InventoryDefinition;        
        public bool EnableSpawnInventoryAsContainer = false;
        public MyDefinitionId? InventorySpawnContainerId;

        public List<String> EnabledComponents = new List<String>();

        public float LootingTime;

        public string InitialAnimation;

        protected override void Init(MyObjectBuilder_DefinitionBase objectBuilder)
        {
            base.Init(objectBuilder);

            var builder = (MyObjectBuilder_CharacterDefinition)objectBuilder;
            Name = builder.Name;
            Model = builder.Model;
            ReflectorTexture = builder.ReflectorTexture;
            LeftGlare = builder.LeftGlare;
            RightGlare = builder.RightGlare;
            LeftLightBone = builder.LeftLightBone;
            RightLightBone = builder.RightLightBone;
            LightGlareSize = builder.LightGlareSize;
            HeadBone = builder.HeadBone;
            Camera3rdBone = builder.Camera3rdBone;
            LeftHandIKStartBone = builder.LeftHandIKStartBone;
            LeftHandIKEndBone= builder.LeftHandIKEndBone;
            RightHandIKStartBone= builder.RightHandIKStartBone;
            RightHandIKEndBone= builder.RightHandIKEndBone;
            WeaponBone = builder.WeaponBone;
            LeftHandItemBone = builder.LeftHandItemBone;
            RighHandItemBone = builder.RightHandItemBone;
            Skeleton = builder.Skeleton;
            LeftForearmBone = builder.LeftForearmBone;
            LeftUpperarmBone = builder.LeftUpperarmBone;
            RightForearmBone = builder.RightForearmBone;
            RightUpperarmBone = builder.RightUpperarmBone;
            SpineBone = builder.SpineBone;
            BendMultiplier1st = builder.BendMultiplier1st;
            BendMultiplier3rd = builder.BendMultiplier3rd;
            MaterialsDisabledIn1st = builder.MaterialsDisabledIn1st;
			Stats = builder.Stats;
            FeetIKEnabled = builder.FeetIKEnabled;
            ModelRootBoneName = builder.ModelRootBoneName;
            LeftHipBoneName = builder.LeftHipBoneName;
            LeftKneeBoneName = builder.LeftKneeBoneName;
            LeftAnkleBoneName = builder.LeftAnkleBoneName;
            RightHipBoneName = builder.RightHipBoneName;
            RightKneeBoneName = builder.RightKneeBoneName;
            RightAnkleBoneName = builder.RightAnkleBoneName;     
            NeedsOxygen = builder.NeedsOxygen;
            OxygenConsumption = builder.OxygenConsumption;
            PressureLevelForLowDamage = builder.PressureLevelForLowDamage;
            DamageAmountAtZeroPressure = builder.DamageAmountAtZeroPressure;
            RagdollDataFile = builder.RagdollDataFile;
            HelmetVariation = builder.HelmetVariation;
            DeathSoundName = builder.DeathSoundName;
            VisibleOnHud = builder.VisibleOnHud;
            UsableByPlayer = builder.UsableByPlayer;
            RagdollRootBody = builder.RagdollRootBody;
            InitialAnimation = builder.InitialAnimation;

            FeetIKSettings = new Dictionary<MyCharacterMovementEnum,MyFeetIKSettings>();
            if (builder.IKSettings != null)
            {
                foreach (var feetSettings in builder.IKSettings)
                {
                    
                    string[] states = feetSettings.MovementState.Split(',');
                    
                    foreach (string stateSet in states) 
                    {
                        string stateDef = stateSet.Trim();
                        if (stateDef != "")
                        {
                            Debug.Assert(Enum.GetNames(typeof(MyCharacterMovementEnum)).Contains(stateDef), "State " + stateDef + " is not defined in Character Movement States");
                            MyCharacterMovementEnum state;
                            if (Enum.TryParse(stateDef, true, out state))
                            {
                                MyFeetIKSettings fSettings = new MyFeetIKSettings();
                                fSettings.Enabled = feetSettings.Enabled;
                                fSettings.AboveReachableDistance = feetSettings.AboveReachableDistance;
                                fSettings.BelowReachableDistance = feetSettings.BelowReachableDistance;
                                fSettings.VerticalShiftDownGain = feetSettings.VerticalShiftDownGain;
                                fSettings.VerticalShiftUpGain = feetSettings.VerticalShiftUpGain;
                                fSettings.FootSize = new Vector3(feetSettings.FootWidth, feetSettings.AnkleHeight, feetSettings.FootLenght);
                                FeetIKSettings.Add(state, fSettings);
                            }
                        }
                    }
                }
            }

	        SuitResourceStorage = builder.SuitResourceStorage;
            Jetpack = builder.Jetpack;

            if (builder.BoneSets != null)
            {
                BoneSets = builder.BoneSets.ToDictionary(x => x.Name, x => x.Bones.Split(' '));
            }

            if (builder.BoneLODs != null)
            {
                BoneLODs = builder.BoneLODs.ToDictionary(x => Convert.ToSingle(x.Name), x => x.Bones.Split(' '));
            }

            if (builder.AnimationMappings != null)
            {
                AnimationNameToSubtypeName = builder.AnimationMappings.ToDictionary(mapping => mapping.Name, mapping => mapping.AnimationSubtypeName);
            }

            if (builder.RagdollBonesMappings != null)
            {
                RagdollBonesMappings = builder.RagdollBonesMappings.ToDictionary(x => x.Name, x => x.Bones.Split(' '));
            }

            if (builder.RagdollPartialSimulations != null)
            {
                RagdollPartialSimulations = builder.RagdollPartialSimulations.ToDictionary(x => x.Name, x => x.Bones.Split(' '));
            }

            Mass = builder.Mass;
            ImpulseLimit = builder.ImpulseLimit;
            OxygenCapacity = builder.OxygenCapacity;

            VerticalPositionFlyingOnly = builder.VerticalPositionFlyingOnly;
            UseOnlyWalking = builder.UseOnlyWalking;
                
            MaxSlope = builder.MaxSlope;
            MaxSprintSpeed = builder.MaxSprintSpeed;
            
            MaxRunSpeed = builder.MaxRunSpeed;
            MaxBackrunSpeed = builder.MaxBackrunSpeed;
            MaxRunStrafingSpeed = builder.MaxRunStrafingSpeed;
            
            MaxWalkSpeed = builder.MaxWalkSpeed;
            MaxBackwalkSpeed = builder.MaxBackwalkSpeed;
            MaxWalkStrafingSpeed = builder.MaxWalkStrafingSpeed;
            
            MaxCrouchWalkSpeed = builder.MaxCrouchWalkSpeed;
            MaxCrouchBackwalkSpeed = builder.MaxCrouchBackwalkSpeed;
            MaxCrouchStrafingSpeed = builder.MaxCrouchStrafingSpeed;
            
            CharacterHeadSize = builder.CharacterHeadSize;
            CharacterHeadHeight = builder.CharacterHeadHeight;
            CharacterCollisionScale = builder.CharacterCollisionScale;
            CharacterCollisionWidth = builder.CharacterCollisionWidth;
            CharacterCollisionHeight = builder.CharacterCollisionHeight;
            CharacterCollisionCrouchHeight = builder.CharacterCollisionCrouchHeight;

            if (builder.Inventory == null)
            {
                InventoryDefinition = new MyObjectBuilder_InventoryDefinition();
            }           
            else
            {
                InventoryDefinition = builder.Inventory;
            }

            if (builder.EnabledComponents != null)
                EnabledComponents = builder.EnabledComponents.Split(' ').ToList();

            EnableSpawnInventoryAsContainer = builder.EnableSpawnInventoryAsContainer;        
            if (EnableSpawnInventoryAsContainer)
            {
                Debug.Assert(builder.InventorySpawnContainerId.HasValue, "Enabled spawning inventory as container, but type id is null");
                if (builder.InventorySpawnContainerId.HasValue)
                {
                    InventorySpawnContainerId = builder.InventorySpawnContainerId.Value;
                }
            }

            LootingTime = builder.LootingTime;
        }

        public override MyObjectBuilder_DefinitionBase GetObjectBuilder()
        {
            var ob = (MyObjectBuilder_CharacterDefinition)base.GetObjectBuilder();
            ob.Name = Name;
            ob.Model = Model;
            ob.ReflectorTexture = ReflectorTexture;
            ob.LeftGlare = LeftGlare;
            ob.RightGlare = RightGlare;
            ob.LightGlareSize = LightGlareSize;
            ob.Skeleton = Skeleton;
            ob.LeftForearmBone = LeftForearmBone;
            ob.LeftUpperarmBone = LeftUpperarmBone;
            ob.RightForearmBone = RightForearmBone;
            ob.RightUpperarmBone = RightUpperarmBone;
            ob.SpineBone = SpineBone;
            ob.MaterialsDisabledIn1st = MaterialsDisabledIn1st;
            ob.NeedsOxygen = NeedsOxygen;
            ob.OxygenConsumption = OxygenConsumption;
            ob.PressureLevelForLowDamage = PressureLevelForLowDamage;
            ob.DamageAmountAtZeroPressure = DamageAmountAtZeroPressure;
            ob.OxygenCapacity = OxygenCapacity;
            ob.HelmetVariation = HelmetVariation;
            ob.DeathSoundName = DeathSoundName;
            ob.VisibleOnHud = VisibleOnHud;
            ob.UsableByPlayer = UsableByPlayer;

			ob.Stats = Stats;

            ob.SuitResourceStorage = SuitResourceStorage;
			ob.Jetpack = Jetpack;

            //TODO BoneSets serialization

            ob.VerticalPositionFlyingOnly = VerticalPositionFlyingOnly;
            ob.UseOnlyWalking = UseOnlyWalking;

            ob.MaxSlope = MaxSlope;
            ob.MaxSprintSpeed = MaxSprintSpeed;

            ob.MaxRunSpeed = MaxRunSpeed;
            ob.MaxBackrunSpeed = MaxBackrunSpeed;
            ob.MaxRunStrafingSpeed = MaxRunStrafingSpeed;

            ob.MaxWalkSpeed = MaxWalkSpeed;
            ob.MaxBackwalkSpeed = MaxBackwalkSpeed;
            ob.MaxWalkStrafingSpeed = MaxWalkStrafingSpeed;

            ob.MaxCrouchWalkSpeed = MaxCrouchWalkSpeed;
            ob.MaxCrouchBackwalkSpeed = MaxCrouchBackwalkSpeed;
            ob.MaxCrouchStrafingSpeed = MaxCrouchStrafingSpeed;

            ob.CharacterHeadSize = CharacterHeadSize;
            ob.CharacterHeadHeight = CharacterHeadHeight;
            ob.CharacterCollisionScale = CharacterCollisionScale;
            ob.CharacterCollisionWidth = CharacterCollisionWidth;
            ob.CharacterCollisionHeight = CharacterCollisionHeight;
            ob.CharacterCollisionCrouchHeight = CharacterCollisionCrouchHeight;

            ob.Inventory = InventoryDefinition;

            ob.EnabledComponents = String.Join(" ",EnabledComponents);

            ob.EnableSpawnInventoryAsContainer = EnableSpawnInventoryAsContainer;
            if (EnableSpawnInventoryAsContainer)
            {
                Debug.Assert(InventorySpawnContainerId.HasValue, "Enabled spawning inventory as container, but type id is null");
                if (InventorySpawnContainerId.HasValue)
                {
                    ob.InventorySpawnContainerId = InventorySpawnContainerId.Value;
                }
            }

            ob.LootingTime = LootingTime;

            return ob;
        }
    }
}
