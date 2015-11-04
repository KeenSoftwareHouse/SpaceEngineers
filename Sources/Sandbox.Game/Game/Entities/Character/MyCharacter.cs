#region Using

using Havok;
using Sandbox.Common;
using Sandbox.Common.ModAPI;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Models;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Components;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.Character.Components;
using Sandbox.Game.Entities.Inventory;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.Graphics.TransparentGeometry.Particles;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Sandbox.Engine.Networking;
using VRage;
using VRage.Components;
using VRage.Game.Entity.UseObject;
using VRage.Input;
using VRage.Library.Utils;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using VRageRender;
using IMyModdingControllableEntity = Sandbox.ModAPI.Interfaces.IMyControllableEntity;
using VRage.Network;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Replicables;

#endregion

namespace Sandbox.Game.Entities.Character
{
    #region Enums

    [Flags]
    public enum MyCharacterMovementFlags
    {  //Default movement is RUN
        Jump = 1,
        Sprint = 2,
        FlyUp = 4,
        FlyDown = 8,
        Crouch = 16,
        Walk = 32
    }

    public enum MyZoomModeEnum
    {
        Classic,
        IronSight,
    }

    enum DamageImpactEnum
    {
        NoDamage,
        SmallDamage,
        MediumDamage,
        CriticalDamage,
        DeadlyDamage,
    }

    #endregion

    [MyEntityType(typeof(MyObjectBuilder_Character))]
    public partial class MyCharacter : 
        MySkinnedEntity,
        IMyCameraController, 
        IMyControllableEntity, 
        IMyInventoryOwner, 
        IMyComponentOwner<MyDataBroadcaster>, 
        IMyComponentOwner<MyDataReceiver>, 
        IMyUseObject, 
        IMyDestroyableObject, 
        Sandbox.ModAPI.IMyCharacter
    {

        #region Consts

        public const float CAMERA_NEAR_DISTANCE = 60.0f;   

        public const float CHARACTER_GRAVITY_MULTIPLIER = 2.0f;
        internal const float CHARACTER_X_ROTATION_SPEED = 0.13f;
        const float CHARACTER_Y_ROTATION_SPEED = 0.0026f;

        public const float MINIMAL_SPEED = 0.001f;

        const float JUMP_TIME = 1; //m/ss

        const float SHOT_TIME = 0.1f;  //s

        const float FALL_TIME = 0.3f; //s
        const float RESPAWN_TIME = 5.0f; //s

	    internal const float MIN_HEAD_LOCAL_X_ANGLE = -80;
        internal const float MAX_HEAD_LOCAL_X_ANGLE = 85;

        #endregion

        #region Fields

        float m_currentShotTime = 0;
        float m_currentShootPositionTime = 0;
        float m_cameraDistance = 0.0f;
        float m_currentSpeed = 0;
        float m_currentDecceleration = 0;
        float m_currentJump = 0;

        bool m_canJump = true;
		internal bool CanJump { get { return m_canJump; } set { m_canJump = value; } }

        float m_currentWalkDelay = 0;
		internal float CurrentWalkDelay { get { return m_currentWalkDelay; } set { m_currentWalkDelay = value; } }

        //Weapon
        public static MyHudNotification OutOfAmmoNotification;
        int m_weaponBone = -1;
        public int WeaponBone { get { return m_weaponBone; } }
        public event Action<IMyHandheldGunObject<MyDeviceBase>> WeaponEquiped;
        static readonly Vector3 m_weaponIronsightTranslation = new Vector3(0.0f, -0.11f, -0.22f);
        IMyHandheldGunObject<MyDeviceBase> m_currentWeapon;

        static readonly Vector3 m_toolIronsightTranslation = new Vector3(0.0f, -0.21f, -0.25f);

        public bool DebugMode = false;

        float m_headLocalXAngle = 0;
        float m_headLocalYAngle = 0;
        
		public float HeadLocalXAngle
		{
			get { return m_headLocalXAngle; }
			set { m_headLocalXAngle = value; }
		}

		public float HeadLocalYAngle
		{
			get { return m_headLocalYAngle; }
			set { m_headLocalYAngle = value; }
		}
        
        int m_headBoneIndex = -1;
        int m_camera3rdBoneIndex = -1;
        int m_leftHandIKStartBone = -1;
        int m_leftHandIKEndBone = -1;
        int m_rightHandIKStartBone = -1;
        int m_rightHandIKEndBone = -1;
        int m_leftUpperarmBone = -1;
        int m_leftForearmBone = -1;
        int m_rightUpperarmBone = -1;
        int m_rightForearmBone = -1;        
        int m_leftHandItemBone = -1;
        int m_rightHandItemBone = -1;
        int m_spineBone = -1;       

        float m_currentAnimationChangeDelay = 0;
        float SAFE_DELAY_FOR_ANIMATION_BLEND = 0.1f;

        MyCharacterMovementEnum m_currentMovementState = MyCharacterMovementEnum.Standing;
        MyCharacterMovementEnum m_previousMovementState = MyCharacterMovementEnum.Standing;
		public event CharacterMovementStateDelegate OnMovementStateChanged;
        
        MyEntity m_leftHandItem;
        MyHandItemDefinition m_handItemDefinition;
        MyZoomModeEnum m_zoomMode = MyZoomModeEnum.Classic;
        public MyZoomModeEnum ZoomMode { get { return m_zoomMode; } }

        float m_currentHandItemWalkingBlend = 0;
        float m_currentHandItemShootBlend = 0;
        float m_currentScatterBlend = 0;
        Vector3 m_currentScatterPos;
        Vector3 m_lastScatterPos;

        /// <summary>
        /// This is now generated dynamically as some character's don't have the same skeleton as human characters.
        /// m_bodyCapsules[0] will always be head capsule
        /// If the model has ragdoll model, the capsules are generated from the ragdoll
        /// If the model is missing the ragdoll, the capsules are generated with dynamically determined parameters, which may not always be correct
        /// </summary>
        CapsuleD[] m_bodyCapsules = new CapsuleD[1];
        MatrixD m_headMatrix = MatrixD.CreateTranslation(0, 1.65, 0);

        MyHudNotification m_pickupObjectNotification;        
        MyHudNotification m_broadcastingNotification;
        
        HkCharacterStateType m_currentCharacterState;
        bool m_isFalling = false;
        bool m_isFallingAnimationPlayed = false;
        float m_currentFallingTime = 0;
        bool m_crouchAfterFall = false;

        MyCharacterMovementFlags m_movementFlags;
        bool m_movementsFlagsChanged;

        string m_characterModel;

	    private MyInventory m_inventory = null;

        public MyInventory Inventory
        {
            get 
            {
                if (m_inventory == null && Components.Has<MyInventoryBase>())
                {
                    m_inventory = FindInventory();
                }
                return m_inventory;
            }
            set 
            {
                if (m_inventory != null)
                {
                    m_inventory.ContentsChanged -= inventory_OnContentsChanged;
                    m_inventory.ContentsChanged -= MyToolbarComponent.CurrentToolbar.CharacterInventory_OnContentsChanged;
                }
                m_inventory = value;
                if (m_inventory != null)
                {
                    m_inventory.ContentsChanged += inventory_OnContentsChanged;
                    m_inventory.ContentsChanged += MyToolbarComponent.CurrentToolbar.CharacterInventory_OnContentsChanged;
                }
            }            
        }

        MyBattery m_suitBattery;
        MyResourceDistributorComponent m_suitResourceDistributor;
	    internal MyResourceDistributorComponent SuitRechargeDistributor
	    {
		    get { return m_suitResourceDistributor; }
			set 
			{ 
				if(Components.Contains(typeof(MyResourceDistributorComponent))) 
					Components.Remove<MyResourceDistributorComponent>(); 
				Components.Add<MyResourceDistributorComponent>(value);
				m_suitResourceDistributor = value;
			}
	    }

	    private MyResourceSinkComponent m_sinkComp;
	    public MyResourceSinkComponent SinkComp
	    {
		    get { return m_sinkComp; }
			set 
			{ 
				if (Components.Has<MyResourceSinkComponent>())
					Components.Remove<MyResourceSinkComponent>();
				Components.Add<MyResourceSinkComponent>(value);
				m_sinkComp = value;
			}
	    }

        MyEntity m_topGrid;
        MyEntity m_usingEntity;

        bool m_enableBag = true;

        //Light
		public const float REFLECTOR_RANGE = 60;
		public const float REFLECTOR_CONE_ANGLE = 0.373f;
		public const float REFLECTOR_BILLBOARD_LENGTH = 40f;
		public const float REFLECTOR_BILLBOARD_THICKNESS = 6f;

        public static Vector4 REFLECTOR_COLOR = Vector4.One;
        public const float REFLECTOR_INTENSITY = 1;
        public static Vector4 POINT_COLOR = Vector4.One;
        public static Vector4 POINT_COLOR_SPECULAR = Vector4.One;
        public const float POINT_LIGHT_RANGE = 1.231f;
        public const float POINT_LIGHT_INTENSITY = 0.464f;
        public const float REFLECTOR_DIRECTION = -3.5f;

        public const float LIGHT_GLARE_MAX_DISTANCE = 40;

        float m_currentLightPower = 0; //0..1
        public float CurrentLightPower { get { return m_currentLightPower; } }
        float m_lightPowerFromProducer = 0;
        float m_lightTurningOnSpeed = 0.05f;
        float m_lightTurningOffSpeed = 0.05f;
        bool m_lightEnabled = true;

        //Needed to check relation between character and remote players when controlling a remote control
        private MyEntityController m_oldController;

        float m_currentHeadAnimationCounter = 0;

        float m_currentLocalHeadAnimation = -1;
        float m_localHeadAnimationLength = -1;
        Vector2? m_localHeadAnimationX = null;
        Vector2? m_localHeadAnimationY = null;

        List<List<int>> m_bodyCapsuleBones = new List<List<int>>();        
         
        MyCameraHeadShake m_cameraShake;
        MyCameraSpring m_cameraSpring;
        Vector3 m_cameraShakeOffset;
        Vector3 m_cameraShakeDir;

        HashSet<uint> m_shapeContactPoints = new HashSet<uint>();

        float m_currentRespawnCounter = 0;
        public float CurrentRespawnCounter { get { return m_currentRespawnCounter; } }
        MyHudNotification m_respawnNotification;

		MyStringHash manipulationToolId = MyStringHash.GetOrCompute("ManipulationTool");

        MyCameraControllerSettings m_storedCameraSettings;
        Queue<Vector3> m_bobQueue = new Queue<Vector3>();

        private bool m_dieAfterSimulation;

        MyRadioReceiver m_radioReceiver;
        MyRadioBroadcaster m_radioBroadcaster;        

        float m_currentLootingCounter = 0;
        MyEntityCameraSettings m_cameraSettingsWhenAlive;

        public StringBuilder CustomNameWithFaction { get; private set; }

		internal new MyRenderComponentCharacter Render
		{
			get { return (MyRenderComponentCharacter)base.Render; }
			set { base.Render = value; }
		}

		public MyCharacterSoundComponent SoundComp
		{
			get { return Components.Get<MyCharacterSoundComponent>(); }
			set { if (Components.Has<MyCharacterSoundComponent>()) Components.Remove<MyCharacterSoundComponent>(); Components.Add<MyCharacterSoundComponent>(value); }
		}

		public MyCharacterStatComponent StatComp
		{
			get { return Components.Get<MyEntityStatComponent>() as MyCharacterStatComponent; }
			set { if (Components.Has<MyEntityStatComponent>()) Components.Remove<MyEntityStatComponent>(); Components.Add<MyEntityStatComponent>(value); }
		}

	    public MyCharacterJetpackComponent JetpackComp
	    {
			get { return Components.Get<MyCharacterJetpackComponent>(); }
			set { if (Components.Has<MyCharacterJetpackComponent>()) Components.Remove<MyCharacterJetpackComponent>(); Components.Add(value); }
	    }

		float IMyCharacter.BaseMass { get { return this.BaseMass; } }
		float IMyCharacter.CurrentMass { get { return this.CurrentMass; } }
		public float BaseMass { get { return Physics.Mass; } }
		public float CurrentMass
		{ 
			get
			{
				float carriedMass = 0.0f;
				if (ManipulatedEntity != null && ManipulatedEntity.Physics != null)
					carriedMass = ManipulatedEntity.Physics.Mass;
				return BaseMass + (float)Inventory.CurrentMass + carriedMass;
			}
		}

        bool m_useAnimationForWeapon = true;
        Matrix m_relativeWeaponMatrix = Matrix.Identity;
        float m_animationToIKDelay = 0.3f; //s
        float m_currentAnimationToIKTime = 0.3f;
        int m_animationToIKState = 0; //0 - none, -1 IK to Animation, 1 AnimationToIK

        MyCharacterDefinition m_characterDefinition;

        public MyCharacterDefinition Definition
        {
            get { return m_characterDefinition; }
        }

        bool UseAnimationForWeapon
        {
            get { return m_useAnimationForWeapon; }
            set
            {
                if (m_useAnimationForWeapon != value)
                {
                    if (value)
                    {
                        m_animationToIKState = -1;
                        m_currentAnimationToIKTime = m_animationToIKDelay;
                    }
                    else
                    {
                        m_animationToIKState = 1;
                        m_currentAnimationToIKTime = 0;
                    }
                }
            }
        }

              

        //Backwards compatibility for MyThirdPersonSpectator
        //Default needs to be true
        private bool m_isInFirstPersonView = true;
        public bool IsInFirstPersonView
        {
            //users connected from different client aren't in first person for local player
            get { return ForceFirstPersonCamera || (m_isInFirstPersonView && this == MySession.LocalCharacter); }
            set
            {
                m_isInFirstPersonView = value;
            }
        }

        bool m_targetFromCamera = false;
        public bool TargetFromCamera
        {
            get
            {
                if (MySession.ControlledEntity == this)
                    return MySession.GetCameraControllerEnum() == MyCameraControllerEnum.ThirdPersonSpectator;

                if (MySandboxGame.IsDedicated)
                    return false;
                
                return m_targetFromCamera;
            }
            set
            {
                m_targetFromCamera = value;
            }
        }

        private float m_switchBackToSpectatorTimer;
        private float m_switchBackToFirstPersonTimer;
        private const float CAMERA_SWITCH_DELAY = 0.2f;

        private bool m_forceFirstPersonCamera;
        public bool ForceFirstPersonCamera
        {
            get { return m_forceFirstPersonCamera; }
            set { m_forceFirstPersonCamera = value; }
        }

        public bool UpdateCalled()
        {
            bool updateCalled = m_actualUpdateFrame != m_actualDrawFrame;
            m_actualDrawFrame = m_actualUpdateFrame;
            return updateCalled;
        }

        public bool IsCameraNear
        {
            get
            {
                if (MyFakes.ENABLE_PERMANENT_SIMULATIONS_COMPUTATION) return true;
                return Render.IsVisible() && m_cameraDistance <= CAMERA_NEAR_DISTANCE;
            }
        }
        
        public event EventHandler OnWeaponChanged;

        public event Action<MyCharacter> CharacterDied;

        public MyInventoryAggregate InventoryAggregate
        {
            get
            {
                var aggregate = Components.Get<MyInventoryBase>() as MyInventoryAggregate;
                return aggregate;
            }
            set 
            {
                if (Components.Has<MyInventoryBase>())
                {
                    Components.Remove<MyInventoryBase>();
                }
                Components.Add<MyInventoryBase>(value);
            }
        }

        public MyCharacterOxygenComponent OxygenComponent
        {
            get;
            private set;
        }

        public Vector3 MoveIndicator
        {
            get;
            set;
        }

        public Vector2 RotationIndicator
        {
            get;
            set;
        }

        public float Roll
        {
            get;
            set;
        }

        bool m_moveAndRotateCalled;
        #endregion

        #region Init

        private static readonly Vector3[] m_defaultColors = new Vector3[]
        {
            new Vector3(0f, -1f, 0f),
            new Vector3(0f, -0.96f, -0.5f),
            new Vector3(0.575f, 0.15f, 0.2f),
            new Vector3(0.333f, -0.33f, -0.05f),
            new Vector3(0f, 0f, 0.05f),
            new Vector3(0f, -0.8f, 0.6f),
            new Vector3(0.122f, 0.05f, 0.46f)
        };

        public static readonly string DefaultModel = "Default_Astronaut";

        public static MyObjectBuilder_Character Random()
        {
            return new MyObjectBuilder_Character()
            {
                CharacterModel = DefaultModel,
                ColorMaskHSV = m_defaultColors[MyUtils.GetRandomInt(0, m_defaultColors.Length)]
            };
        }

        public MyCharacter()
        {
            ControllerInfo.ControlAcquired += OnControlAcquired;
            ControllerInfo.ControlReleased += OnControlReleased;

            m_radioReceiver = new MyRadioReceiver(this);
            m_radioBroadcaster = new MyRadioBroadcaster(this);
            m_radioBroadcaster.BroadcastRadius = 200;
            CustomNameWithFaction = new StringBuilder();
            PositionComp = new MyCharacterPosition();
            (PositionComp as MyPositionComponent).WorldPositionChanged = WorldPositionChanged;			
            
            Render = new MyRenderComponentCharacter();
            Render.EnableColorMaskHsv = true;
            Render.NeedsDraw = true;
            Render.CastShadows = true;
            Render.NeedsResolveCastShadow = false;
            Render.SkipIfTooSmall = false;

            SinkComp = new MyResourceSinkComponent();

            AddDebugRenderComponent(new MyDebugRenderComponentCharacter(this));

            if (MyPerGameSettings.CharacterDetectionComponent != null)
                Components.Add<MyCharacterDetectorComponent>((MyCharacterDetectorComponent)Activator.CreateInstance(MyPerGameSettings.CharacterDetectionComponent));
            else
                Components.Add<MyCharacterDetectorComponent>(new MyCharacterRaycastDetectorComponent());

            //Components.Add<MyCharacterDetectorComponent>(new MyCharacterShapecastDetectorComponent());
        }

        /// <summary>
        /// Backwards compatibility for old character model.
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        private static string GetRealModel(string asset, ref Vector3 colorMask)
        {
            if (MyObjectBuilder_Character.CharacterModels.ContainsKey(asset))
            {
                colorMask = MyObjectBuilder_Character.CharacterModels[asset];
                asset = DefaultModel;
            }
            return asset;
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            SyncFlag = true;
            base.Init(objectBuilder);

            MyObjectBuilder_Character characterOb = (MyObjectBuilder_Character)objectBuilder;

            SyncObject.MarkPhysicsDirty();

            Render.ColorMaskHsv = characterOb.ColorMaskHSV;
            m_currentAnimationChangeDelay = 0;

            Vector3 colorMask = Render.ColorMaskHsv;
            GetModelAndDefinition(characterOb, out m_characterModel, out m_characterDefinition, ref colorMask);
            if (Render.ColorMaskHsv != colorMask) Render.ColorMaskHsv = colorMask;

			SoundComp = new MyCharacterSoundComponent();

            m_radioBroadcaster.WantsToBeEnabled = characterOb.EnableBroadcasting && Definition.VisibleOnHud;
            if (MyFakes.ENABLE_BATTLE_SYSTEM && MySession.Static.Battle)
            {
                m_radioBroadcaster.Enabled = false;
                m_radioBroadcaster.WantsToBeEnabled = false;
            }

            Init(new StringBuilder(characterOb.DisplayName), m_characterDefinition.Model, null, null);

            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
 
            PositionComp.LocalAABB = new BoundingBox(-new Vector3(0.3f, 0.0f, 0.3f), new Vector3(0.3f, 1.8f, 0.3f));

            m_currentLootingCounter = characterOb.LootingCounter;

            if (m_currentLootingCounter <= 0)
                UpdateCharacterPhysics(!characterOb.AIMode);

            m_currentMovementState = characterOb.MovementState;

            InitAnimations();
            ValidateBonesProperties();
            CalculateTransforms(0);

            if (m_currentLootingCounter > 0)
            {
                InitDeadBodyPhysics();
                if (m_currentMovementState != MyCharacterMovementEnum.Died) SetCurrentMovementState(MyCharacterMovementEnum.Died);                
                SwitchAnimation(MyCharacterMovementEnum.Died, false);
            }

            InitInventory(characterOb);

            Physics.Enabled = true;

            SetHeadLocalXAngle(characterOb.HeadAngle.X);
            SetHeadLocalYAngle(characterOb.HeadAngle.Y);

            Render.InitLight(m_characterDefinition);
            Render.InitJetpackThrusts(m_characterDefinition);

            m_useAnimationForWeapon = MyPerGameSettings.UseAnimationInsteadOfIK;

            m_lightEnabled = characterOb.LightEnabled;

            Physics.LinearVelocity = characterOb.LinearVelocity;

            if (Physics.CharacterProxy != null)
            {
                Physics.CharacterProxy.ContactPointCallbackEnabled = true;
                Physics.CharacterProxy.ContactPointCallback += RigidBody_ContactPointCallback;
            }

            Render.UpdateLightProperties(m_currentLightPower);

            IsInFirstPersonView = characterOb.IsInFirstPersonView || MySession.Static.Settings.Enable3rdPersonView == false;

			m_breath = new MyCharacterBreath(this);

			InitStatComponent(characterOb);

			MyStatsDefinition statsDefinition = null;
			if (MyDefinitionManager.Static.TryGetDefinition(new MyDefinitionId(typeof(MyObjectBuilder_StatsDefinition), Definition.Stats), out statsDefinition))
				StatComp.InitStats(statsDefinition);

			var health = StatComp.Health;
			if (health != null)
			{
				if(characterOb.Health.HasValue)
					health.Value = characterOb.Health.Value;
				health.OnStatChanged += StatComp.OnHealthChanged;
			}
			m_breath.ForceUpdate();

            Debug.Assert(m_currentLootingCounter <= 0 || m_currentLootingCounter > 0);
            
            m_broadcastingNotification = new MyHudNotification();

            if (InventoryAggregate != null) InventoryAggregate.Init();

            UseDamageSystem = true;
           
            if (characterOb.EnabledComponents == null)
            {
                characterOb.EnabledComponents = new List<string>();
                characterOb.EnabledComponents.AddList(m_characterDefinition.EnabledComponents);
            }

            foreach (var componentName in characterOb.EnabledComponents)
            {
                Type componentType;
                if (MyCharacterComponentTypes.CharacterComponents.TryGetValue(MyStringId.GetOrCompute(componentName), out componentType))
                {
                    MyCharacterComponent component = Activator.CreateInstance(componentType) as MyCharacterComponent;
                    Components.Add(componentType,component);
                }
            }
            OxygenComponent = new MyCharacterOxygenComponent();
            Components.Add(OxygenComponent);
            OxygenComponent.Init(characterOb);

            var sinkData = new List<MyResourceSinkInfo>();
            if (MySession.Static.Settings.EnableOxygen)
                sinkData.Add(new MyResourceSinkInfo
                {
                    ResourceTypeId = MyCharacterOxygenComponent.OxygenId,
                    MaxRequiredInput = (Definition.OxygenCapacity + (!Definition.NeedsOxygen ? Definition.OxygenConsumption : 0f))*MyEngineConstants.UPDATE_STEPS_PER_SECOND/100f,
                    RequiredInputFunc = () => (OxygenComponent.SuitOxygenAmountMissing + (!Definition.NeedsOxygen ? Definition.OxygenConsumption : 0f))*MyEngineConstants.UPDATE_STEPS_PER_SECOND/100f
                });

            var sourceData = new List<MyResourceSourceInfo>();

            OxygenComponent.AppendSinkData(sinkData);
            OxygenComponent.AppendSourceData(sourceData);

            m_suitBattery = new MyBattery(this);
            m_suitBattery.Init(characterOb.Battery, sinkData, sourceData);
            OxygenComponent.CharacterGasSink = m_suitBattery.ResourceSink;
            OxygenComponent.CharacterGasSource = m_suitBattery.ResourceSource;

            sinkData.Clear();

            sinkData.Add(
                new MyResourceSinkInfo {
                    ResourceTypeId = MyResourceDistributorComponent.ElectricityId,
                    MaxRequiredInput = MyEnergyConstants.REQUIRED_INPUT_LIFE_SUPPORT + MyEnergyConstants.REQUIRED_INPUT_CHARACTER_LIGHT,
                    RequiredInputFunc = ComputeRequiredPower});

            SinkComp.Init(
                MyStringHash.GetOrCompute("Utility"),
                sinkData);
            SinkComp.CurrentInputChanged += delegate
            {
                SetPowerInput(SinkComp.CurrentInputByType(MyResourceDistributorComponent.ElectricityId));
            };

            SuitRechargeDistributor = new MyResourceDistributorComponent();
            SuitRechargeDistributor.AddSource(m_suitBattery.ResourceSource);
            SuitRechargeDistributor.AddSink(SinkComp);
            SinkComp.Update();

            bool isJetpackAvailable = !((MySession.Static.SurvivalMode && MyPerGameSettings.Game == GameEnum.ME_GAME) || MySession.Static.Battle);
            isJetpackAvailable = isJetpackAvailable && (m_characterDefinition.Jetpack != null);

            if (isJetpackAvailable)
            {
                JetpackComp = new MyCharacterJetpackComponent();
                JetpackComp.Init(characterOb);
            }

            InitWeapon(characterOb.HandWeapon);

            RecalculatePowerRequirement(true);

            if (Definition.RagdollBonesMappings.Count > 0)
                CreateBodyCapsulesForHits(Definition.RagdollBonesMappings);
            else
                m_bodyCapsuleBones.Clear();

            PlayCharacterAnimation(Definition.InitialAnimation, MyBlendOption.Immediate, MyFrameOption.JustFirstFrame, 0.0f);
        }

        public static void GetModelAndDefinition(MyObjectBuilder_Character characterOb, out string characterModel, out MyCharacterDefinition characterDefinition, ref Vector3 colorMask)
        {
            characterModel = GetRealModel(characterOb.CharacterModel, ref colorMask);
            characterDefinition = null;

            if (!MyDefinitionManager.Static.Characters.TryGetValue(characterModel, out characterDefinition))
            {
                //System.Diagnostics.Debug.Fail("Character model " + m_characterModel + " not found!");
                characterDefinition = MyDefinitionManager.Static.Characters.First();
                characterModel = characterDefinition.Model;
            }
        }

        private void InitInventory(MyObjectBuilder_Character characterOb)
        {
            bool inventoryAlreadyExists = false;

            if (MyFakes.ENABLE_MEDIEVAL_INVENTORY && InventoryAggregate != null)
            {
                Inventory = FindInventory();                
                inventoryAlreadyExists = true;
            }

            if (!inventoryAlreadyExists)
            {
                if (m_characterDefinition.InventoryDefinition == null)
                {
                    m_characterDefinition.InventoryDefinition = new MyObjectBuilder_InventoryDefinition();
                }
                Inventory = new MyInventory(m_characterDefinition.InventoryDefinition, 0, this);
                Inventory.Init(characterOb.Inventory);
                MyCubeBuilder.BuildComponent.AfterCharacterCreate(this);
                if (MyFakes.ENABLE_MEDIEVAL_INVENTORY && InventoryAggregate != null)
                {
                    var internalAggregate = InventoryAggregate.GetInventory(MyStringId.GetOrCompute("Internal")) as MyInventoryAggregate;
                    if (internalAggregate != null)
                    {
                        internalAggregate.AddComponent(Inventory);
                    }
                    else
                    {
                        InventoryAggregate.AddComponent(Inventory);
                    }
                }                
            }
        }

        private Queue<MyInventoryAggregate> m_tmpQueue = new Queue<MyInventoryAggregate>();
        public MyInventory FindInventory()
        {
            MyInventoryBase baseInventory = null;
            MyInventory foundInventory = null;
            Components.TryGet(out baseInventory);

            if (baseInventory == null)
            {
                return null;
            }

            m_tmpQueue.Enqueue(baseInventory as MyInventoryAggregate);

            while (foundInventory == null && m_tmpQueue.Count > 0)
            {
                var aggregate = m_tmpQueue.Dequeue();

                foreach (var inventory in aggregate.ChildList.Reader)
                {
                    if (inventory is MyInventory)
                    {
                        foundInventory = inventory as MyInventory;
                    }
                    else if (inventory is MyInventoryAggregate)
                    {
                        m_tmpQueue.Enqueue(inventory as MyInventoryAggregate);
                    }
                }
            }
            m_tmpQueue.Clear();

            return foundInventory;
        }

		private void InitStatComponent(MyObjectBuilder_Character characterOb)
		{
			if(characterOb.Health.HasValue || StatComp == null)	// Old save
			{
				StatComp = new MyCharacterStatComponent();
			}
		}

        private void CreateBodyCapsulesForHits(Dictionary<string, string[]> bonesMappings)
        {
            m_bodyCapsuleBones.Clear();
            m_bodyCapsules = new CapsuleD[bonesMappings.Count];           
            foreach (var boneSet in bonesMappings)
            {
                try
                {                    
                    String[] boneNames = boneSet.Value;
                    int firstBone;
                    int lastBone;					
                    Debug.Assert(boneNames.Length >= 2, "In ragdoll model definition of bonesets is only one bone, can not create body capsule properly! Model:" + ModelName + " BoneSet:" + boneSet.Key);
                    FindBone(boneNames.First(), out firstBone);
                    FindBone(boneNames.Last(), out lastBone);     
                    List<int> boneList = new List<int>(2);
                    boneList.Add(firstBone);
                    boneList.Add(lastBone);
                    m_bodyCapsuleBones.Add(boneList);                    
                }
                catch (Exception e)
                {
                    Debug.Fail(e.Message);                   
                }
            }    
            // locating the head bone and moving as the first in the list
            for (int i = 0; i < m_bodyCapsuleBones.Count; ++i)
            {
                var boneList = m_bodyCapsuleBones[i];
                if (boneList.FirstOrDefault() == m_headBoneIndex)
                {
                    m_bodyCapsuleBones.Move(i,0);                    
                    break;
                }
            }
        }

        
        void Toolbar_ItemChanged(MyToolbar toolbar, MyToolbar.IndexArgs index)
        {
            var item = toolbar.GetItemAtIndex(index.ItemIndex);
            if (item != null)
            {
                var def = item as MyToolbarItemDefinition;
                if (def != null)
                {
                    var defId = def.Definition.Id;
                    if (defId != null)
                    {
						if (defId.TypeId != typeof(MyObjectBuilder_PhysicalGunObject))
							MyToolBarCollection.RequestChangeSlotItem(MySession.LocalHumanPlayer.Id, index.ItemIndex, defId);
						else
							MyToolBarCollection.RequestChangeSlotItem(MySession.LocalHumanPlayer.Id, index.ItemIndex, item.GetObjectBuilder());
                    }
                }
            }
            else if (MySandboxGame.IsGameReady)
            {
                MyToolBarCollection.RequestClearSlot(MySession.LocalHumanPlayer.Id, index.ItemIndex);
            }
        }

        void inventory_OnContentsChanged(MyInventoryBase inventory)
        {
            // Switch away from the weapon if we don't have it; Cube placer is an exception
            if (m_currentWeapon != null && WeaponTakesBuilderFromInventory(m_currentWeapon.DefinitionId)
                && inventory != null && inventory is MyInventory && !(inventory as MyInventory).ContainItems(1, m_currentWeapon.PhysicalObject))
                SwitchToWeapon(null);
        }

        void RigidBody_ContactPointCallback(ref HkContactPointEvent value)
        {          
            if (IsDead)
                return;

            if (Physics.CharacterProxy == null)
                return;

            if (!MySession.Ready)
                return;

            if (value.Base.BodyA == null || value.Base.BodyB == null)
                return;

            if (value.Base.BodyA.UserObject == null || value.Base.BodyB.UserObject == null)
                return;

            if (value.Base.BodyA.HasProperty(HkCharacterRigidBody.MANIPULATED_OBJECT) || value.Base.BodyB.HasProperty(HkCharacterRigidBody.MANIPULATED_OBJECT))
                return;

            //MyCharacter charA = null;//((MyPhysicsBody)value.Base.BodyA.UserObject).Entity as MyCharacter;
            //MyCharacter charB = null;//((MyPhysicsBody)value.Base.BodyB.UserObject).Entity as MyCharacter;

            //if (charA != null && charA.AIMode)
            //    return;

            //if (charB != null && charB.AIMode)
            //    return;

			Render.TrySpawnWalkingParticles(ref value);

            // DAMAGE COMPUTATION TO THE CHARACTER
            // GET THE OTHER COLLIDING BODY AND COMPUTE DAMAGE BASED ON BODIES MASS AND VELOCITIES
            if (MyPerGameSettings.EnableCharacterCollisionDamage && !MyFakes.NEW_CHARACTER_DAMAGE)
            {
                CalculateDamageAfterCollision(ref value);
            }
            //// ORIGINAL DAMAGE COMPUTATION
            else
            {
                float impact = 0;

                if (MyFakes.NEW_CHARACTER_DAMAGE)
                {
                    var normal = value.ContactPoint.Normal;
                    MyEntity other = value.GetPhysicsBody(0).Entity as MyEntity;

                    HkRigidBody otherRb = value.Base.BodyA;
                    if (other == this)
                    {
                        other = value.GetPhysicsBody(1).Entity as MyEntity;
                        otherRb = value.Base.BodyB;
                        normal = -normal;
                    }

                    var otherChar = (other as MyCharacter);
                    if (otherChar != null)
                    {
                        if ((other as MyCharacter).IsDead)
                        {
                            if (otherChar.Physics.Ragdoll.GetRootRigidBody().HasProperty(HkCharacterRigidBody.MANIPULATED_OBJECT))
                                return;
                        }
                        else
                        {
                            if (Physics.CharacterProxy.Supported && otherChar.Physics.CharacterProxy.Supported)
                                return;
                        }
                    }

                    var vel = Math.Abs(value.SeparatingVelocity);

                    bool enoughSpeed = vel > 3;

                    Vector3 velocity1 = Physics.LinearVelocity;
                    Vector3 velocity2 = otherRb.GetVelocityAtPoint(value.ContactPoint.Position);

                    float speed1 = Math.Max(velocity1.Length() - (MyFakes.ENABLE_CUSTOM_CHARACTER_IMPACT ? 12.6f : 17.0f), 0);//treshold for falling dmg
                    float speed2 = velocity2.Length() - 2.0f;

                    Vector3 dir1 = speed1 > 0 ? Vector3.Normalize(velocity1) : Vector3.Zero;
                    Vector3 dir2 = speed2 > 0 ? Vector3.Normalize(velocity2) : Vector3.Zero;

                    float dot1withNormal = speed1 > 0 ? Vector3.Dot(dir1, normal) : 0;
                    float dot2withNormal = speed2 > 0 ? -Vector3.Dot(dir2, normal) : 0;

                    speed1 *= dot1withNormal;
                    speed2 *= dot2withNormal;

                    vel = speed1 + speed2;
                    if (vel < 0)
                        return;

                    float mass1 = MyDestructionHelper.MassFromHavok(Physics.Mass);
                    float mass2 = MyDestructionHelper.MassFromHavok(otherRb.Mass);

                    float impact1 = (speed1 * speed1 * mass1) * 0.5f;
                    float impact2 = (speed2 * speed2 * mass2) * 0.5f;


                    float mass;
                    if (Physics.Mass > otherRb.Mass && !other.Physics.IsStatic)
                    {
                        mass = otherRb.Mass;
                        //impact = impact2;
                    }
                    else
                    {
                        mass = 70 / 25.0f;// Physics.Mass;
                        if (Physics.CharacterProxy.Supported && !other.Physics.IsStatic)
                            mass += Math.Abs(Vector3.Dot(Vector3.Normalize(velocity2), Physics.CharacterProxy.SupportNormal)) * otherRb.Mass / 10;
                    }
                    mass = MyDestructionHelper.MassFromHavok(mass);

                    impact = (mass * vel * vel) / 2;
                    if (speed2 > 2) //dont reduce pure fall damage
                        impact -= 400;
                    impact /= 10; //scaling damage
                    if (impact < 1)
                        return;
                }

                //int bodyId = value.Base.BodyA == Physics.CharacterProxy.GetRigidBody() ? 1 : 0;
                // Ca
                //uint shapeKey = value.GetShapeKey(bodyId);
                //if (shapeKey != uint.MaxValue)
                {

                    //m_shapeContactPoints.Add(shapeKey);

                    //MyTrace.Send(TraceWindow.Default, "Velocity: " + value.SeparatingVelocity.ToString());
                    //if (Math.Abs(value.SeparatingVelocity) > 0)
                    //{
                    //}

                    //2 large blocks (14.3m/s)
                    //3 large blocks (17.0m/s)
                    //5 large blocks (22.3m/s)                
                    //6 large blocks (24.2m/s)
                    float damageImpact = (Math.Abs(value.SeparatingVelocity) - 17.0f) * 14.0f;



                    if (MyFakes.ENABLE_CUSTOM_CHARACTER_IMPACT)
                    {
                        // 1.5 ~ start damage (12.6m/s)
                        // 3 blocks - dead (17.0)
                        damageImpact = (Math.Abs(value.SeparatingVelocity) - 12.6f) * 25f;
                    }
                    if(MyFakes.NEW_CHARACTER_DAMAGE)
                        damageImpact = impact;
                    if (damageImpact > 0)
                    {
                        if (this.ControllerInfo.IsLocallyControlled() || Sync.IsServer)
                        {
                            IMyEntity other = value.GetPhysicsBody(0).Entity;
                            if (other == this)
                                other = value.GetPhysicsBody(1).Entity;

                            DoDamage(damageImpact, MyDamageType.Environment, true, other != null ? other.EntityId : 0);
                        }
                    }
                }
            }
        }

        private void CalculateDamageAfterCollision(ref HkContactPointEvent value)
        {           
            // Are bodies moving one to another? if not we do not apply damage
            if (value.SeparatingVelocity < 0)
            {
                if (!Sync.IsServer) return;

                DamageImpactEnum damageImpact = DamageImpactEnum.NoDamage;

                // Get the colliding object and skip collisions between characters
                HkRigidBody collidingBody;
                int collidingBodyIdx = 0;
                if (value.Base.BodyA == Physics.CharacterProxy.GetHitRigidBody())
                {
                    collidingBody = value.Base.BodyB;
                    collidingBodyIdx = 1;
                }
                else 
                    collidingBody = value.Base.BodyA;
                MyEntity collidingEntity = value.GetPhysicsBody(collidingBodyIdx).Entity as MyEntity;                
                if (collidingEntity == null || collidingEntity is MyCharacter) return;

                if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_SHOW_DAMAGE)
                {
                    MatrixD worldMatrix = collidingEntity.Physics.GetWorldMatrix();
                    int index = 0;
                    MyPhysicsDebugDraw.DrawCollisionShape(collidingBody.GetShape(), worldMatrix, 1, ref index, "hit");
                }

                damageImpact = GetDamageFromFall(collidingBody, collidingEntity, ref value);

                if (damageImpact != DamageImpactEnum.NoDamage) ApplyDamage(damageImpact, MyDamageType.Fall);

                damageImpact = GetDamageFromHit(collidingBody, collidingEntity, ref value);

                if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_SHOW_DAMAGE)                
                {
                    if (damageImpact != DamageImpactEnum.NoDamage)
                    {
                            MatrixD worldMatrix = collidingEntity.Physics.GetWorldMatrix();
                            VRageRender.MyRenderProxy.DebugDrawSphere(worldMatrix.Translation, collidingBody.Mass, Color.Red, 1, false);
                            VRageRender.MyRenderProxy.DebugDrawText3D(worldMatrix.Translation, "MASS: " + collidingBody.Mass, Color.Red, 1, false);
                    }
                }

                if (damageImpact != DamageImpactEnum.NoDamage) ApplyDamage(damageImpact, MyDamageType.Environment);

                damageImpact = GetDamageFromSqueeze(collidingBody, collidingEntity, ref value);

                if (damageImpact != DamageImpactEnum.NoDamage) ApplyDamage(damageImpact, MyDamageType.Squeez);

            }
        }

        private DamageImpactEnum GetDamageFromSqueeze(HkRigidBody collidingBody, MyEntity collidingEntity, ref HkContactPointEvent value)
        {
            if (collidingBody.IsFixed || collidingBody.Mass < MyPerGameSettings.CharacterSqueezeMinMass) return DamageImpactEnum.NoDamage;

            if (value.ContactProperties.IsNew) return DamageImpactEnum.NoDamage;

            // the object has to be moving towards the character even slowly and that also the character is not moving away from it
            Vector3 direction = Physics.CharacterProxy.GetHitRigidBody().Position - collidingBody.Position;
            Vector3 gravity = MyGravityProviderSystem.CalculateTotalGravityInPoint(PositionComp.WorldAABB.Center) + Physics.HavokWorld.Gravity;
            direction.Normalize();
            gravity.Normalize();

            float resultToPlayer = Vector3.Dot(direction, gravity);
            
            if (resultToPlayer < 0.5f) return DamageImpactEnum.NoDamage;

            if (m_squeezeDamageTimer > 0)
            {
                m_squeezeDamageTimer -= MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                return DamageImpactEnum.NoDamage;
            }
            m_squeezeDamageTimer = MyPerGameSettings.CharacterSqueezeDamageDelay;

            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_SHOW_DAMAGE)
            {
                MatrixD worldMatrix = collidingEntity.Physics.GetWorldMatrix();
                int index = 2;
                MyPhysicsDebugDraw.DrawCollisionShape(collidingBody.GetShape(), worldMatrix, 1, ref index);
                VRageRender.MyRenderProxy.DebugDrawText3D(worldMatrix.Translation, "SQUEEZE, MASS:" + collidingBody.Mass, Color.Yellow, 2, false);
            }

            if (collidingBody.Mass > MyPerGameSettings.CharacterSqueezeDeadlyDamageMass) return DamageImpactEnum.DeadlyDamage;

            if (collidingBody.Mass > MyPerGameSettings.CharacterSqueezeCriticalDamageMass) return DamageImpactEnum.CriticalDamage;

            if (collidingBody.Mass > MyPerGameSettings.CharacterSqueezeMediumDamageMass) return DamageImpactEnum.MediumDamage;

            return DamageImpactEnum.SmallDamage;
        }

        private DamageImpactEnum GetDamageFromHit(HkRigidBody collidingBody, MyEntity collidingEntity, ref HkContactPointEvent value)
        {
            if (collidingBody.LinearVelocity.Length() < MyPerGameSettings.CharacterDamageHitObjectMinVelocity) return DamageImpactEnum.NoDamage;

            if (collidingEntity == ManipulatedEntity) return DamageImpactEnum.NoDamage;

            if (collidingBody.HasProperty(HkCharacterRigidBody.MANIPULATED_OBJECT)) return DamageImpactEnum.NoDamage;

            var mass = (MyPerGameSettings.Destruction ? MyDestructionHelper.MassFromHavok(collidingBody.Mass) : collidingBody.Mass);
            if (mass < MyPerGameSettings.CharacterDamageHitObjectMinMass) return DamageImpactEnum.NoDamage;

            // Get the objects energies to calculate the damage - must be higher above treshold
            float objectEnergy = Math.Abs(value.SeparatingVelocity) * mass;

            if (objectEnergy > MyPerGameSettings.CharacterDamageHitObjectDeadlyEnergy) return DamageImpactEnum.DeadlyDamage;
            if (objectEnergy > MyPerGameSettings.CharacterDamageHitObjectCriticalEnergy) return DamageImpactEnum.CriticalDamage;
            if (objectEnergy > MyPerGameSettings.CharacterDamageHitObjectMediumEnergy) return DamageImpactEnum.MediumDamage;
            if (objectEnergy > MyPerGameSettings.CharacterDamageHitObjectSmallEnergy) return DamageImpactEnum.SmallDamage;

            return DamageImpactEnum.NoDamage;
        }

        private void ApplyDamage(DamageImpactEnum damageImpact, MyStringHash myDamageType)
        {
            if (!Sync.IsServer) return;

            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_SHOW_DAMAGE)
            {
                if (damageImpact != DamageImpactEnum.NoDamage)
                {
                    VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(100, 100), "DAMAGE! TYPE: " + myDamageType.ToString() + " IMPACT: " + damageImpact.ToString(), Color.Red, 1);
                }
            }
            
            switch (damageImpact)
            {
                case DamageImpactEnum.SmallDamage:
                    DoDamage(MyPerGameSettings.CharacterSmallDamage, myDamageType, true);
                    break;
                case DamageImpactEnum.MediumDamage:
                    DoDamage(MyPerGameSettings.CharacterMediumDamage, myDamageType, true);
                    break;
                case DamageImpactEnum.CriticalDamage:
                    DoDamage(MyPerGameSettings.CharacterCriticalDamage, myDamageType, true);
                    break;
                case DamageImpactEnum.DeadlyDamage:
                    DoDamage(MyPerGameSettings.CharacterDeadlyDamage, myDamageType, true);
                    break;
                case DamageImpactEnum.NoDamage:
                default:
                    break;
            }
        }

        private DamageImpactEnum GetDamageFromFall(HkRigidBody collidingBody, MyEntity collidingEntity, ref HkContactPointEvent value)
        {
            //if (m_currentMovementState != MyCharacterMovementEnum.Falling || m_currentMovementState != MyCharacterMovementEnum.Jump) return DamageImpactEnum.NoDamage;
            //if (!collidingBody.IsFixed && collidingBody.Mass < Physics.Mass * 50) return DamageImpactEnum.NoDamage;

            float dotProd = Vector3.Dot(value.ContactPoint.Normal, Vector3.Normalize(Physics.HavokWorld.Gravity));

            bool falledOnEntity = dotProd <= 0.0f;

            if (!falledOnEntity) return DamageImpactEnum.NoDamage;

            if (Math.Abs(value.SeparatingVelocity * dotProd) < MyPerGameSettings.CharacterDamageMinVelocity) return DamageImpactEnum.NoDamage;

            if (Math.Abs(value.SeparatingVelocity * dotProd) > MyPerGameSettings.CharacterDamageDeadlyDamageVelocity) return DamageImpactEnum.DeadlyDamage;

            if (Math.Abs(value.SeparatingVelocity * dotProd) > MyPerGameSettings.CharacterDamageMediumDamageVelocity) return DamageImpactEnum.MediumDamage;

            return DamageImpactEnum.SmallDamage;
        }

        private void InitWeapon(MyObjectBuilder_EntityBase weapon)
        {
            if ((m_rightHandItemBone == -1 || weapon != null) && m_currentWeapon != null)
            {
                // First, dispose of the old weapon
                DisposeWeapon();
            }

            if (m_rightHandItemBone != -1 && weapon != null)
            {
                EquipWeapon(CreateGun(weapon));
            }
        }

        private void ValidateBonesProperties()
        { // in case of invalid character model
            if (m_rightHandItemBone == -1 && m_currentWeapon != null)
            {
                DisposeWeapon();
            }
        }

        private void DisposeWeapon()
        {
            var weaponEntity = m_currentWeapon as MyEntity;
            Debug.Assert(weaponEntity != null);
            if (weaponEntity != null)
            {
                weaponEntity.EntityId = 0;
                weaponEntity.Close();
                m_currentWeapon = null;
            }
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            MyObjectBuilder_Character objectBuilder = (MyObjectBuilder_Character)base.GetObjectBuilder(copy);

            objectBuilder.CharacterModel = m_characterModel;
            objectBuilder.ColorMaskHSV = ColorMask;

            if (Inventory != null && !MyFakes.ENABLE_MEDIEVAL_INVENTORY)
            {
                objectBuilder.Inventory = Inventory.GetObjectBuilder();
            }
            else
            {
                objectBuilder.Inventory = null;
            }

            if (m_currentWeapon != null)
                objectBuilder.HandWeapon = ((MyEntity)m_currentWeapon).GetObjectBuilder();

            objectBuilder.Battery = m_suitBattery.GetObjectBuilder();
            objectBuilder.LightEnabled = m_lightEnabled;
            objectBuilder.HeadAngle = new Vector2(m_headLocalXAngle, m_headLocalYAngle);

            objectBuilder.LinearVelocity = Physics != null ? Physics.LinearVelocity : Vector3.Zero;

			objectBuilder.Health = null;

            objectBuilder.LootingCounter = m_currentLootingCounter;
            objectBuilder.DisplayName = DisplayName;

            // ds sends IsInFirstPersonView to clients  as false
            objectBuilder.IsInFirstPersonView = !MySandboxGame.IsDedicated ? m_isInFirstPersonView : true;

            objectBuilder.EnableBroadcasting = m_radioBroadcaster.WantsToBeEnabled;

            objectBuilder.MovementState = m_currentMovementState;

            if (Components != null)
            {
                if (objectBuilder.EnabledComponents == null)
                {
                    objectBuilder.EnabledComponents = new List<string>();
                }
                foreach (var component in Components)
                {
	                if (!(component is MyCharacterComponent))
						continue;

	                if (!MyCharacterComponentTypes.CharacterComponents.Values.Contains(component.GetType()))
						continue;

                            var pair = MyCharacterComponentTypes.CharacterComponents.FirstOrDefault(x => x.Value == component.GetType());
                            objectBuilder.EnabledComponents.Add(pair.Key.ToString());
                        }
				if(JetpackComp != null)
		            JetpackComp.GetObjectBuilder(objectBuilder);

                OxygenComponent.GetObjectBuilder(objectBuilder);
            }

            return objectBuilder;
        }

        protected override void Closing()
        {
            CloseInternal();

            if (m_breath != null)
                m_breath.Close();

            base.Closing();
        }

        private void CloseInternal()
        {
            if (m_currentWeapon != null)
            {
                ((MyEntity)m_currentWeapon).Close();
                m_currentWeapon = null;
            }
            if (m_leftHandItem != null)
            {
                m_leftHandItem.Close();
                m_leftHandItem = null;
            }

            RemoveNotifications();

            m_radioBroadcaster.Enabled = false;

            MyToolbarComponent.CharacterToolbar.ItemChanged -= Toolbar_ItemChanged;
        }

        protected override void BeforeDelete()
        {
            Render.CleanLights();
        }

        #endregion

        #region Simulation

        bool m_wasInFirstPerson = false;
        bool m_isInFirstPerson = false;

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();

            System.Diagnostics.Debug.Assert(MySession.Static != null);

            if (MySession.Static == null)
                return;

            m_actualUpdateFrame++;

            m_isInFirstPerson = (MySession.Static.CameraController == this) && IsInFirstPersonView;

            if (m_wasInFirstPerson != m_isInFirstPerson && m_currentMovementState != MyCharacterMovementEnum.Sitting)
            {
                EnableHead(!m_isInFirstPerson);
                MySector.MainCamera.Zoom.ApplyToFov = m_isInFirstPerson;

                if (!ForceFirstPersonCamera)
                {
                    UpdateNearFlag();
                }

                SendFlags();
            }

            m_wasInFirstPerson = m_isInFirstPerson;

            UpdateLightPower();

			SoundComp.FindAndPlayStateSound();

            if (!IsDead && m_currentMovementState != MyCharacterMovementEnum.Sitting && (!ControllerInfo.IsRemotelyControlled() || (MyFakes.CHARACTER_SERVER_SYNC)))
            {
                if (!MySandboxGame.IsPaused)//this update is called even in pause (jetpack, model update)
                {
                    if (Physics.CharacterProxy != null)
                        Physics.CharacterProxy.StepSimulation(MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS);
                }
            }

            m_currentAnimationChangeDelay += MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            
            if (Sync.IsServer && !IsDead && m_currentMovementState != MyCharacterMovementEnum.Sitting && !MyEntities.IsInsideWorld(PositionComp.GetPosition()))
            {
                if (MySession.Static.SurvivalMode)
                    DoDamage(1000, MyDamageType.Suicide, true, EntityId);
            }           

            foreach (var component in Components)
            {
                var characterComponent = component as MyCharacterComponent;
                if (characterComponent != null && characterComponent.NeedsUpdateBeforeSimulation)
                {
                    characterComponent.UpdateBeforeSimulation();
                }
            }

	        var jetpack = JetpackComp;
	        if (jetpack != null)
		        jetpack.UpdateBeforeSimulation();
            //MyRenderProxy.DebugDrawText3D(WorldMatrix.Translation + WorldMatrix.Up * 2.0f, m_currentMovementState.ToString(), Color.Red, 1.0f, false);

            //if (m_hitCapsule != null)
            //    MyRenderProxy.DebugDrawCapsule(m_hitCapsule.Value.P0, m_hitCapsule.Value.P1, m_hitCapsule.Value.Radius, Color.Red, false, false);

            //if (m_hitInfo != null)
            //    MyRenderProxy.DebugDrawSphere(m_hitInfo.Value.IntersectionPointInWorldSpace, 0.1f, Color.White, 1f, false);
        }

        public void UpdateLightPower(bool chargeImmediately = false)
        {
            float oldPower = m_currentLightPower;

            if (m_lightPowerFromProducer > 0 && m_lightEnabled)
            {
                if (chargeImmediately)
                    m_currentLightPower = 1;
                else
                    m_currentLightPower = MathHelper.Clamp(m_currentLightPower + m_lightTurningOnSpeed, 0, 1);
            }
            else
            {
                m_currentLightPower = chargeImmediately ? 0 : MathHelper.Clamp(m_currentLightPower - m_lightTurningOffSpeed, 0, 1);
            }

            Render.UpdateLight(m_currentLightPower, oldPower != m_currentLightPower);

            if (m_radioBroadcaster.WantsToBeEnabled)
            {
                m_radioBroadcaster.Enabled = m_suitBattery.ResourceSource.CurrentOutput > 0;
            }
            if (!m_radioBroadcaster.WantsToBeEnabled)
            {
                m_radioBroadcaster.Enabled = false;
            }
        }

        public override void UpdateBeforeSimulation10()
        {
            base.UpdateBeforeSimulation10();
            ProfilerShort.Begin("SuitRecharge");
			SuitRechargeDistributor.UpdateBeforeSimulation10();
            ProfilerShort.BeginNextBlock("Radio");
            m_radioReceiver.UpdateBroadcastersInRange();
            if (this == MySession.ControlledEntity )
            {
                ProfilerShort.BeginNextBlock("Hud");
                m_radioReceiver.UpdateHud();
            }
            ProfilerShort.End();
        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();

			SoundComp.UpdateBeforeSimulation100();

            m_suitBattery.UpdateOnServer();

            if (!m_suitBattery.ResourceSource.HasCapacityRemaining && !MySession.Static.Settings.EnableOxygen)
            {
                DoDamage(5, MyDamageType.Environment, true);
            }

            UpdateChat();

            foreach (var component in Components)
            {
                var characterComponent = component as MyCharacterComponent;
                if (characterComponent != null && characterComponent.NeedsUpdateBeforeSimulation100)
                {
                    characterComponent.UpdateBeforeSimulation100();
                }
            }
        }

		public override void UpdateAfterSimulation10()
		{
			base.UpdateAfterSimulation10();

            foreach (var component in Components)
            {
                var characterComponent = component as MyCharacterComponent;
                if (characterComponent != null && characterComponent.NeedsUpdateAfterSimulation10)
                {
                    characterComponent.UpdateAfterSimulation10();
                }
            }

            UpdateCameraDistance();
		}

        private void UpdateCameraDistance()
        {
            m_cameraDistance = (float)Vector3D.Distance(MySector.MainCamera.Position, WorldMatrix.Translation);
        }

        public void DrawHud(IMyCameraController camera, long playerId)
        {
            MyHud.Crosshair.Position = MyHudCrosshair.ScreenCenter;

            if (m_currentWeapon != null)
            {
                m_currentWeapon.DrawHud(camera, playerId);
            }
        }

        public override void UpdateAfterSimulation()
        {
            //var measureStart = new MyTimeSpan(Stopwatch.GetTimestamp());

            base.UpdateAfterSimulation();

			VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update Stats");
			UpdateStats();
			VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update diyng");
            UpdateDiyng();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update zero movement");
            UpdateZeroMovement();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update Shake");
            UpdateShake();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update physical movement");
            UpdatePhysicalMovement();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update Fall And Spine");
            UpdateFallAndSpine();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update Animation");
            UpdateAnimation(m_cameraDistance);
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
                       
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update Light Position");
            Render.UpdateLightPosition();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update BOB Queue");
            UpdateBobQueue();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update Character State");
            UpdateCharacterStateChange();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update Respawn and Looting");
            UpdateRespawnAndLooting();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update IK Transitions");
            UpdateIKTransitions();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update shooting");
            UpdateShooting();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            foreach (var component in Components)
            {
                var characterComponent = component as MyCharacterComponent;
                if (characterComponent != null && characterComponent.NeedsUpdateAfterSimulation)
                {
                    characterComponent.UpdateAfterSimulation();
                }
            }

            if (MyFakes.CHARACTER_SERVER_SYNC || (ControllerInfo.IsLocallyControlled()))
            {
                MoveAndRotateInternal(MoveIndicator, RotationIndicator, Roll);
            }

            m_moveAndRotateCalled = false;
        }

		private void UpdateStats()
		{
			StatComp.Update();
		}


        private void UpdateCharacterStateChange()
        {
            if (!ControllerInfo.IsRemotelyControlled() || (MyFakes.CHARACTER_SERVER_SYNC))
            {
                if (!IsDead && Physics.CharacterProxy != null && m_currentCharacterState != Physics.CharacterProxy.GetState())
                {
                    OnCharacterStateChanged(Physics.CharacterProxy.GetState());
                }
            }
        }

        private void UpdateRespawnAndLooting()
        {
            if (m_currentRespawnCounter > 0)
            {
                if (ControllerInfo.Controller != null && !MySessionComponentMissionTriggers.CanRespawn(this.ControllerInfo.Controller.Player.Id))
                {
                    if (m_respawnNotification != null)
                        m_respawnNotification.m_lifespanMs = 0;
                    m_currentRespawnCounter = -1;
                }

                m_currentRespawnCounter -= MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                if (m_respawnNotification != null)
                    m_respawnNotification.SetTextFormatArguments((int)m_currentRespawnCounter);

                if (m_currentRespawnCounter <= 0)
                {
                    if (Sync.IsServer)
                    {
                        if (ControllerInfo.Controller != null)
                            Sync.Players.KillPlayer(ControllerInfo.Controller.Player);

                        if (ControllerInfo.IsLocallyHumanControlled())
                            MyPlayerCollection.RequestLocalRespawn();
                    }
                }
            }

            if (m_currentLootingCounter > 0)
            {
                m_currentLootingCounter -= MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                if (m_currentLootingCounter <= 0)
                {
                    SyncObject.SendCloseRequest();
                    Save = false;
                }
            }
        }

        private void UpdateIKTransitions()
        {
            if (m_animationToIKState > 0)
            {
                m_currentAnimationToIKTime += MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                if (m_currentAnimationToIKTime >= m_animationToIKDelay)
                {
                    m_currentAnimationToIKTime = m_animationToIKDelay;
                    m_useAnimationForWeapon = false;
                    m_animationToIKState = 0;
                }
            }
            else
                if (m_animationToIKState < 0)
                {
                    m_currentAnimationToIKTime -= MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                    if (m_currentAnimationToIKTime <= 0)
                    {
                        m_currentAnimationToIKTime = 0;
                        m_useAnimationForWeapon = true;
                        m_animationToIKState = 0;
                    }
                }
        }

        private void UpdateBobQueue()
        {
            int headBone = IsInFirstPersonView ? m_headBoneIndex : m_camera3rdBoneIndex;

            if (headBone != -1)
            {
                m_bobQueue.Enqueue(BoneAbsoluteTransforms[headBone].Translation);

                int bobMax = m_currentMovementState == MyCharacterMovementEnum.Standing ||
                             m_currentMovementState == MyCharacterMovementEnum.Sitting ||
                             m_currentMovementState == MyCharacterMovementEnum.Crouching ||
                             m_currentMovementState == MyCharacterMovementEnum.RotatingLeft ||
                             m_currentMovementState == MyCharacterMovementEnum.RotatingRight ||
                             m_currentMovementState == MyCharacterMovementEnum.Died ? 5 : 100;

                if (WantsCrouch)
                    bobMax = 3;

                while (m_bobQueue.Count > bobMax)
                    m_bobQueue.Dequeue();
            }
        }

        private void UpdateFallAndSpine()
        {
	        if (ControllerInfo.IsRemotelyControlled() && (!MyFakes.CHARACTER_SERVER_SYNC))
				return;

	        var jetpack = JetpackComp;
			if(jetpack != null)
				jetpack.UpdateFall();

                if (m_isFalling)
                {
		        if (jetpack == null || !jetpack.Running)
                    {
                        m_currentFallingTime += MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
			        if (m_currentFallingTime > FALL_TIME && !m_isFallingAnimationPlayed)
                        {
                            SwitchAnimation(MyCharacterMovementEnum.Falling, false);
                            m_isFallingAnimationPlayed = true;
                        }
                    }
                }

	        if ((jetpack == null || !jetpack.Running || (jetpack.Running && (IsLocalHeadAnimationInProgress() || Definition.VerticalPositionFlyingOnly))) && !IsDead && !IsSitting)
                {
		        float spineRotation = MathHelper.Clamp(-m_headLocalXAngle, -45, MAX_HEAD_LOCAL_X_ANGLE);

                    float bendMultiplier = IsInFirstPersonView ? m_characterDefinition.BendMultiplier1st : m_characterDefinition.BendMultiplier3rd;
		        var usedSpineRotation = Quaternion.CreateFromAxisAngle(Vector3.Backward, MathHelper.ToRadians(bendMultiplier * spineRotation));

                    Quaternion clientsSpineRotation = Quaternion.CreateFromAxisAngle(Vector3.Backward, MathHelper.ToRadians(m_characterDefinition.BendMultiplier3rd * spineRotation));

                    SetSpineAdditionalRotation(usedSpineRotation, clientsSpineRotation);
                }
                else
                    SetSpineAdditionalRotation(Quaternion.CreateFromAxisAngle(Vector3.Backward, 0), Quaternion.CreateFromAxisAngle(Vector3.Backward, 0));

	        if (m_currentWeapon == null && !IsDead && (jetpack == null || !jetpack.Running) && !IsSitting)
                {
                    if (m_headLocalXAngle < -11)
                    {
                        // THIS CAUSES CHARACTER HAND GLITCH WITH NEW MODEL
                        //SetHandAdditionalRotation(Quaternion.CreateFromAxisAngle(Vector3.Forward, MathHelper.ToRadians(0)));
                        //SetUpperHandAdditionalRotation(Quaternion.CreateFromAxisAngle(Vector3.Normalize(Vector3.Down + Vector3.Right - 0.4f * Vector3.Forward), -MathHelper.ToRadians(((m_headLocalXAngle + 11) * 0.8f))));
                    }
                    if (m_headLocalXAngle > -11)
                    {
                        //SetHandAdditionalRotation(Quaternion.CreateFromAxisAngle(Vector3.Forward, MathHelper.ToRadians(0)));
                        //SetUpperHandAdditionalRotation(Quaternion.CreateFromAxisAngle(Vector3.Forward, MathHelper.ToRadians(0)));
                    }
                }
                else
                {
                    SetHandAdditionalRotation(Quaternion.CreateFromAxisAngle(Vector3.Forward, MathHelper.ToRadians(0)));
                    SetUpperHandAdditionalRotation(Quaternion.CreateFromAxisAngle(Vector3.Forward, MathHelper.ToRadians(0)));
                }
        }

        private void UpdateShooting()
        {
            if (m_currentWeapon != null)
            {
                //UpdateWeaponPosition();

                if (m_currentWeapon.IsShooting)
                {
                    m_currentShootPositionTime = SHOT_TIME;
                }

                ShootInternal();
                // CH: Warning, m_currentWeapon can be null after ShootInternal because of autoswitch!
            }

            if (m_currentShotTime > 0)
            {
                m_currentShotTime -= MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                if (m_currentShotTime <= 0)
                {
                    m_currentShotTime = 0;
                }
            }

            if (m_currentShootPositionTime > 0)
            {
                m_currentShootPositionTime -= MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                if (m_currentShootPositionTime <= 0)
                {
                    m_currentShootPositionTime = 0;
                }
            }
        }

        private void UpdatePhysicalMovement()
        {
            if (!MySandboxGame.IsGameReady || Physics == null || !Physics.Enabled || !MySession.Ready || Physics.HavokWorld == null)
                return;

            //if (!ControllerInfo.IsRemotelyControlled() || (Sync.IsServer && false))
            if (ControllerInfo.IsLocallyControlled() || (Sync.IsServer && ControllerInfo.Controller == null) || MyFakes.CHARACTER_SERVER_SYNC)
            {
	            var jetpack = JetpackComp;
                bool jetpackNotActive = (jetpack == null || !jetpack.UpdatePhysicalMovement());	//Solve Y orientation and gravity only in non flying mode
                if (jetpackNotActive && !IsDead && Physics.CharacterProxy != null)
                {
                    if (!Physics.CharacterProxy.Up.IsValid())
                    {
                        Debug.Fail("Character Proxy Up vector is invalid! Can not to solve gravity influence on character. Character type: " + this.GetType().ToString() );
                        Physics.CharacterProxy.Up = WorldMatrix.Up;
                    }

                    if (!Physics.CharacterProxy.Forward.IsValid())
                    {
                        Debug.Fail("Character Proxy Forward vector is invalid! Can not to solve gravity influence on character. Character type: " + this.GetType().ToString() );
                        Physics.CharacterProxy.Forward = WorldMatrix.Forward;
                    }

                    Vector3 gravity = MyGravityProviderSystem.CalculateTotalGravityInPoint(PositionComp.WorldAABB.Center) + Physics.HavokWorld.Gravity;
                    Physics.CharacterProxy.Gravity = gravity * CHARACTER_GRAVITY_MULTIPLIER;
                    Vector3 oldUp = Physics.CharacterProxy.Up;
                    Vector3 newUp = Physics.CharacterProxy.Up;
                    Vector3 newForward = Physics.CharacterProxy.Forward;

                    // If there is valid non-zero gravity
                    if ((gravity.LengthSquared() > 0.1f) && (oldUp != Vector3.Zero) && (gravity.IsValid()) && !Definition.VerticalPositionFlyingOnly)
                    {
                        UpdateStandup(ref gravity, ref oldUp, ref newUp, ref newForward);
						if(jetpack != null)
						  jetpack.CurrentAutoEnableDelay = 0;
                    }
                    // Zero-G
                    else
                    {
                        if (jetpack != null && jetpack.CurrentAutoEnableDelay != -1)
                            jetpack.CurrentAutoEnableDelay += MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                    }

                    Physics.CharacterProxy.Forward = newForward;
                    Physics.CharacterProxy.Up = newUp;
                }
                else if (IsDead)
                {
                    if (Physics == null) Debugger.Break();

                    if (Physics.RigidBody.IsActive)
                    {
                        Vector3 gravity = MyGravityProviderSystem.CalculateTotalGravityInPoint(PositionComp.WorldAABB.Center) + Physics.HavokWorld.Gravity;
                        Physics.RigidBody.Gravity = gravity;
                    }
                }
            }

            MatrixD worldMatrix = Physics.GetWorldMatrix();

            //Include foot error
            if (m_currentMovementState == MyCharacterMovementEnum.Standing)
            {
                m_cummulativeVerticalFootError += m_verticalFootError * 0.2f;
                m_cummulativeVerticalFootError = MathHelper.Clamp(m_cummulativeVerticalFootError, -0.75f, 0.75f);
            }
            else
                m_cummulativeVerticalFootError = 0;

            worldMatrix.Translation = worldMatrix.Translation + worldMatrix.Up * m_cummulativeVerticalFootError;

            if (WorldMatrix.Translation != worldMatrix.Translation ||
                WorldMatrix.Forward != worldMatrix.Forward ||
                WorldMatrix.Up != worldMatrix.Up)
            {
                PositionComp.SetWorldMatrix(worldMatrix, Physics);
            }

            if (ControllerInfo.IsLocallyControlled())
            {
                Physics.UpdateAccelerations();
            }
            else if (MyFakes.CHARACTER_SERVER_SYNC)
            {
                Physics.UpdateAccelerations();
            } //otherwise OnPositionUpdate message it is updated
        }

        private void UpdateStandup(ref Vector3 gravity, ref Vector3 oldUp, ref Vector3 newUp, ref Vector3 newForward)
        {
            Vector3 testUp = -Vector3.Normalize(gravity);
            var dotProd = Vector3.Dot(oldUp, testUp);
            var lenProd = oldUp.Length() * testUp.Length();
            var divOperation = dotProd / lenProd;

            // check-up proper division result and for NaN
            if (float.IsNaN(divOperation) || float.IsNegativeInfinity(divOperation) || float.IsPositiveInfinity(divOperation)) divOperation = 1;
            divOperation = MathHelper.Clamp(divOperation, -1.0f, 1.0f);

            const float standUpSpeedAnglePerFrame = 0.04f;

            if (!MyUtils.IsZero(divOperation - 1, 0.00001f))
            {
                float angle = 0;
                //if direct opposite
                if (MyUtils.IsZero(divOperation + 1, 0.00001f))
                    angle = 0.1f;
                else
                    angle = (float)(Math.Acos(divOperation));

                angle = System.Math.Min(Math.Abs(angle), standUpSpeedAnglePerFrame) * Math.Sign(angle);

                Vector3 normal = Vector3.Cross(oldUp, testUp);

                if (normal.LengthSquared() > 0)
                {
                    normal = Vector3.Normalize(normal);
                    newUp = Vector3.TransformNormal(WorldMatrix.Up, Matrix.CreateFromAxisAngle(normal, angle));
                    newForward = Vector3.TransformNormal(WorldMatrix.Forward, Matrix.CreateFromAxisAngle(normal, angle));
                }
            }
        }

        private void UpdateShake()
        {
            if (MySession.LocalHumanPlayer == null) 
                return;

            if (this == MySession.LocalHumanPlayer.Identity.Character)
            {
                if (m_cameraShake != null)
                {
                    m_cameraSpring.Update(MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS, PositionComp.GetWorldMatrixNormalizedInv(), ref m_cameraShakeOffset);
                    m_cameraShake.UpdateShake(MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS, ref m_cameraShakeOffset, ref m_cameraShakeDir);
                }

                UpdateHudCharacterInfo();

                if (
                    (m_currentMovementState == MyCharacterMovementEnum.Standing) ||
                    (m_currentMovementState == MyCharacterMovementEnum.Crouching) ||
                    (m_currentMovementState == MyCharacterMovementEnum.Flying))
                    m_currentHeadAnimationCounter += MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                else
                    m_currentHeadAnimationCounter = 0;

                if (m_currentLocalHeadAnimation >= 0)
                {
                    m_currentLocalHeadAnimation += MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

                    float ratio = m_currentLocalHeadAnimation / m_localHeadAnimationLength;

                    if (m_currentLocalHeadAnimation > m_localHeadAnimationLength)
                    {
                        m_currentLocalHeadAnimation = -1;
                        ratio = 1;
                    }

                    if (m_localHeadAnimationX.HasValue)
                        SetHeadLocalXAngle(MathHelper.Lerp(m_localHeadAnimationX.Value.X, m_localHeadAnimationX.Value.Y, ratio));
                    if (m_localHeadAnimationY.HasValue)
                        SetHeadLocalYAngle(MathHelper.Lerp(m_localHeadAnimationY.Value.X, m_localHeadAnimationY.Value.Y, ratio));
                }
            }
        }

        public void UpdateZeroMovement()
        {
            if ((ControllerInfo.IsLocallyControlled()))
            {
                if (m_moveAndRotateCalled == false)
                {   //Stop character because MoveAndRotate was not called
                    MoveAndRotate(Vector3.Zero, Vector2.Zero, 0);
                }
            }
        }

        private void UpdateDiyng()
        {
            if (m_dieAfterSimulation)
            {
                DieInternal();
                m_dieAfterSimulation = false;
            }
        }

        internal void SetHeadLocalXAngle(float angle, bool updateSync = true)
        {
            if (m_headLocalXAngle != angle)
            {
                m_headLocalXAngle = angle;
                if (updateSync)
                {
                    SyncObject.ChangeHeadOrSpine(m_headLocalXAngle, m_headLocalYAngle,
                        Quaternion.Zero,
                        GetAdditionalRotation(Definition.HeadBone), GetAdditionalRotation(Definition.LeftForearmBone), GetAdditionalRotation(Definition.LeftUpperarmBone));
                }
            }
        }

        private void SetHeadLocalYAngle(float angle, bool updateSync = true)
        {
            if (m_headLocalYAngle != angle)
            {
                m_headLocalYAngle = angle;
                if (updateSync)
                {
                    SyncObject.ChangeHeadOrSpine(m_headLocalXAngle, m_headLocalYAngle,
                        Quaternion.Zero,
                        GetAdditionalRotation(Definition.HeadBone), GetAdditionalRotation(Definition.LeftForearmBone), GetAdditionalRotation(Definition.LeftUpperarmBone));
                }
            }
        }


        bool ShouldUseAnimatedHeadRotation()
        {
            //if (m_currentHeadAnimationCounter > 0.15f)
            //  return true;

            return false;
        }

        Vector3D m_crosshairPoint;

        /// <summary>
        /// For characters, which are not controlled by player, this will set the aimed point, otherwise the aimed point is determined from camera matrix
        /// </summary>
        public Vector3D AimedPoint
        {
            get
            {
                return m_aimedPoint;
            }
            set
            {
                m_aimedPoint = value;
            }
        }

        Vector3D m_aimedPoint;

        Vector3D GetAimedPointFromHead()
        {
            MatrixD headMatrix = GetHeadMatrix(false);
            var endPoint = headMatrix.Translation + headMatrix.Forward * 25000;
            // Same optimization as the one in GetAimedPointFromCamera.
            return endPoint;

            if (MySession.ControlledEntity == this)
            {
                LineD line = new LineD(headMatrix.Translation, endPoint);
                //Line line = new Line(MySector.MainCamera.Position, MySector.MainCamera.Position + MySector.MainCamera.ForwardVector * 1000);
                var intersection = MyEntities.GetIntersectionWithLine(ref line, this, (MyEntity)m_currentWeapon);

                if (intersection.HasValue)
                {
                    return intersection.Value.IntersectionPointInWorldSpace;
                }
                else
                {
                    return (Vector3D)line.To;
                }
            }
            else
            {
                return endPoint;
            }
        }

        Vector3D GetAimedPointFromCamera()
        {
            Vector3D endPoint = MySector.MainCamera.Position + MySector.MainCamera.ForwardVector * 25000;

            // There doesn't seem to be any difference between doing the raycast and just
            // returning the end point. However, 25km raycast causes distant voxel maps to
            // generate geometry along the ray path, unless it is already cached (which it usually isn't),
            // and that can take very long time.
            return endPoint;
        }

        #endregion

        #region Movement


        public void Rotate(Vector2 rotationIndicator, float roll)
        {
            if (!IsInFirstPersonView)
            {
                RotateHead(rotationIndicator);
                MyThirdPersonSpectator.Static.Rotate(rotationIndicator, roll);
            }
            else
            {
                MoveAndRotate(Vector3.Zero, rotationIndicator, roll);
            }
        }

        public void RotateStopped()
        {

        }

        public void MoveAndRotateStopped()
        {
        }


        public void MoveAndRotate(Vector3 moveIndicator, Vector2 rotationIndicator, float roll)
        {
            if(MoveIndicator.Length() >0)
            {

            }
            MoveIndicator = moveIndicator;
            RotationIndicator = rotationIndicator;
            Roll = roll;
            m_moveAndRotateCalled = true;
        }

        private void MoveAndRotateInternal(Vector3 moveIndicator, Vector2 rotationIndicator, float roll)
        {
            if (Physics == null)
                return;

            if (DebugMode)
                return;

            //Died character
            if (Physics.CharacterProxy == null)
            {
                moveIndicator = Vector3.Zero;
                rotationIndicator = Vector2.Zero;
                roll = 0;
            }

	        var jetpack = JetpackComp;

            bool sprint = moveIndicator.Z != 0 && WantsSprint;
            bool walk = WantsWalk;
            bool jump = WantsJump;
	        bool jetpackRunning = jetpack != null && jetpack.Running;
            bool canMove = !jetpackRunning && !((m_currentCharacterState == HkCharacterStateType.HK_CHARACTER_IN_AIR || (int)m_currentCharacterState == 5) && (m_currentJump <= 0)) && (m_currentMovementState != MyCharacterMovementEnum.Died);
			bool canRotate = (jetpackRunning || !((m_currentCharacterState == HkCharacterStateType.HK_CHARACTER_IN_AIR || (int)m_currentCharacterState == 5) && (m_currentJump <= 0))) && (m_currentMovementState != MyCharacterMovementEnum.Died);

            float acceleration = 0;

	        if (jetpackRunning)
            {
		        jetpack.MoveAndRotate(ref moveIndicator, ref rotationIndicator, canRotate);
	        }
            else if (canMove || m_movementsFlagsChanged)
				{
                if (moveIndicator.LengthSquared() > 0)
                    moveIndicator = Vector3.Normalize(moveIndicator);

                MyCharacterMovementEnum newMovementState = GetNewMovementState(ref moveIndicator, ref rotationIndicator, ref acceleration, sprint, walk, canMove, m_movementsFlagsChanged);

                SwitchAnimation(newMovementState);

                SetCurrentMovementState(newMovementState);

                if (!IsIdle)
                    m_currentWalkDelay = MathHelper.Clamp(m_currentWalkDelay - MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS, 0, m_currentWalkDelay);

                if (canMove)
                    m_currentSpeed = LimitMaxSpeed(m_currentSpeed + (m_currentWalkDelay <= 0 ? acceleration : 0), m_currentMovementState);

                    if (Physics.CharacterProxy != null)
                    {
                    Physics.CharacterProxy.PosX = m_currentMovementState != MyCharacterMovementEnum.Sprinting ? -moveIndicator.X : 0;
                    Physics.CharacterProxy.PosY = moveIndicator.Z;
                        Physics.CharacterProxy.Elevate = 0;
                    }

	            if (canMove && m_currentMovementState != MyCharacterMovementEnum.Jump)
                {
                        int sign = Math.Sign(m_currentSpeed);
                        m_currentSpeed += -sign * m_currentDecceleration * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

                        if (Math.Sign(sign) != Math.Sign(m_currentSpeed))
                            m_currentSpeed = 0;
                    }

				if(Physics.CharacterProxy != null)
					Physics.CharacterProxy.Speed = m_currentMovementState != MyCharacterMovementEnum.Died ? m_currentSpeed : 0;

	            if ((jump && m_currentMovementState != MyCharacterMovementEnum.Jump))
                {
                    PlayCharacterAnimation("Jump", MyBlendOption.Immediate, MyFrameOption.StayOnLastFrame, 0.0f, 1.3f);
					StatComp.DoAction("Jump");
                    m_currentJump = 0.55f;
                    SetCurrentMovementState(MyCharacterMovementEnum.Jump);
                    m_canJump = true;
                    if(Physics.CharacterProxy != null)
                    {
                        Physics.CharacterProxy.GetHitRigidBody().ApplyForce(1, WorldMatrix.Up * 5 * Physics.Mass);
                    }

                    //VRage.Trace.MyTrace.Send( VRage.Trace.TraceWindow.Default, "jump");
                }

                if (m_currentJump > 0)
                {
                    m_currentJump -= MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                    if (m_currentJump < 0.8f && m_canJump)
                    {
                        if (Physics.CharacterProxy != null)
                            Physics.CharacterProxy.Jump = true;

                        //Physics.CharacterProxy.Gravity = new Vector3(0, -40, 0);
                        m_canJump = false;
                    }


                    if (m_currentJump <= 0)
                    {
                        MyCharacterMovementEnum afterJumpState = MyCharacterMovementEnum.Standing;

						if ((Physics.CharacterProxy != null && Physics.CharacterProxy.GetState() == HkCharacterStateType.HK_CHARACTER_IN_AIR) || (Physics.CharacterProxy != null && (int)Physics.CharacterProxy.GetState() == 5))
                            StartFalling();
                        else
                            {
                                if ((moveIndicator.X != 0 || moveIndicator.Z != 0))
                                {
                                    if (!WantsCrouch)
                                    {
                                        if (moveIndicator.Z < 0)
                                        {
                                            if (sprint)
                                            {
                                                afterJumpState = MyCharacterMovementEnum.Sprinting;
                                                PlayCharacterAnimation("Sprint", MyBlendOption.WaitForPreviousEnd, MyFrameOption.Loop, 0.2f);
                                            }
                                            else
                                            {
                                                afterJumpState = MyCharacterMovementEnum.Walking;
                                                PlayCharacterAnimation("Walk", MyBlendOption.WaitForPreviousEnd, MyFrameOption.Loop, 0.5f);
                                            }
                                        }
                                        else
                                        {
                                            afterJumpState = MyCharacterMovementEnum.BackWalking;
                                            PlayCharacterAnimation("WalkBack", MyBlendOption.WaitForPreviousEnd, MyFrameOption.Loop, 0.5f);
                                        }
                                    }
                                    else
                                    {
                                        if (moveIndicator.Z < 0)
                                        {
                                            afterJumpState = MyCharacterMovementEnum.CrouchWalking;
                                            PlayCharacterAnimation("CrouchWalk", MyBlendOption.WaitForPreviousEnd, MyFrameOption.Loop, 0.2f);
                                        }
                                        else
                                        {
                                            afterJumpState = MyCharacterMovementEnum.CrouchBackWalking;
                                            PlayCharacterAnimation("CrouchWalkBack", MyBlendOption.WaitForPreviousEnd, MyFrameOption.Loop, 0.2f);
                                        }
                                    }
                                }
                                else
                                {
                                    afterJumpState = MyCharacterMovementEnum.Standing;
                                    PlayCharacterAnimation("Idle", MyBlendOption.WaitForPreviousEnd, MyFrameOption.Loop, 0.2f);
                                }

								SoundComp.PlayFallSound();
                                m_canJump = true;

                                SetCurrentMovementState(afterJumpState);
                            }

                        m_currentJump = 0;
                    }
                }
            }
            else if (Physics.CharacterProxy != null)
            {
                Physics.CharacterProxy.Elevate = 0;
            }

            if (rotationIndicator.Y != 0 && (canRotate || m_isFalling || m_currentJump > 0))
            {
                if (jetpackRunning)	// TODO: Move to jetpack
                {
                    MatrixD rotationMatrix = WorldMatrix.GetOrientation();
                    Vector3D translationDraw = WorldMatrix.Translation;
                    Vector3D translationPhys = Physics.GetWorldMatrix().Translation;

                    rotationMatrix = rotationMatrix * MatrixD.CreateFromAxisAngle(WorldMatrix.Up, -rotationIndicator.Y * CHARACTER_Y_ROTATION_SPEED);

                    rotationMatrix.Translation = translationPhys;

                    WorldMatrix = rotationMatrix;

                    rotationMatrix.Translation = translationDraw;
                    PositionComp.SetWorldMatrix(rotationMatrix, Physics);
                }
                else
                {
                    Matrix rotationMatrix = Matrix.CreateRotationY(-rotationIndicator.Y * CHARACTER_Y_ROTATION_SPEED);
                    Matrix characterMatrix = Matrix.CreateWorld(Physics.CharacterProxy.Position, Physics.CharacterProxy.Forward, Physics.CharacterProxy.Up);

                    characterMatrix = rotationMatrix * characterMatrix;

                    Physics.CharacterProxy.Forward = characterMatrix.Forward;
                    Physics.CharacterProxy.Up = characterMatrix.Up;
                }

              
            }

            m_movementsFlagsChanged = false;

	        if (rotationIndicator.X != 0 && !jetpackRunning)
            {
                    if (((m_currentMovementState == MyCharacterMovementEnum.Died) && !m_isInFirstPerson)
                        ||
                        (m_currentMovementState != MyCharacterMovementEnum.Died))
                    {
			        SetHeadLocalXAngle(MathHelper.Clamp(m_headLocalXAngle - rotationIndicator.X*CHARACTER_X_ROTATION_SPEED, MIN_HEAD_LOCAL_X_ANGLE, MAX_HEAD_LOCAL_X_ANGLE));
                        //CalculateDependentMatrices();

                        int headBone = IsInFirstPersonView ? m_headBoneIndex : m_camera3rdBoneIndex;

                        if (headBone != -1)
                        {
                            m_bobQueue.Clear();
                            m_bobQueue.Enqueue(BoneAbsoluteTransforms[headBone].Translation);
                        }
                    }
                }

	        if (roll != 0 && jetpackRunning && !Definition.VerticalPositionFlyingOnly)	// TODO: Move to jetpack
                    {
                    MatrixD rotationMatrix = WorldMatrix.GetOrientation();
                    Vector3D translation = WorldMatrix.Translation + WorldMatrix.Up;

                    rotationMatrix = rotationMatrix * MatrixD.CreateFromAxisAngle(WorldMatrix.Forward, roll * 0.02f);

                    rotationMatrix.Translation = translation - rotationMatrix.Up;

                    //   bool canPlaceCharacter = CanPlaceCharacter(ref rotationMatrix);

                    //if (m_shapeContactPoints.Count < 2)
                    {
                        WorldMatrix = rotationMatrix;
                        m_shapeContactPoints.Clear();
                    }
                }

            if (Physics.CharacterProxy != null)
            {
                if (Physics.CharacterProxy.LinearVelocity.LengthSquared() > 0.1f)
                    m_shapeContactPoints.Clear();
            }

            WantsJump = false;
            WantsSprint = false;
            WantsFlyUp = false;
            WantsFlyDown = false;
            }

        private void RotateHead(Vector2 rotationIndicator)
        {
            const float sensitivity = 0.5f;
            if (rotationIndicator.X != 0)
                SetHeadLocalXAngle(MathHelper.Clamp(m_headLocalXAngle - rotationIndicator.X * sensitivity, MIN_HEAD_LOCAL_X_ANGLE, MAX_HEAD_LOCAL_X_ANGLE));

            if (rotationIndicator.Y != 0)
            {
                SetHeadLocalYAngle(m_headLocalYAngle - rotationIndicator.Y * sensitivity);
            }
        }

        public bool IsIdle
        {
            get { return m_currentMovementState == MyCharacterMovementEnum.Standing || m_currentMovementState == MyCharacterMovementEnum.Crouching; }
        }

		List<HkBodyCollision> m_penetrationList = new List<HkBodyCollision>();

        public bool CanPlaceCharacter(ref MatrixD worldMatrix, bool useCharacterCenter = false, bool checkCharacters = false)
        {
            Vector3D translation = worldMatrix.Translation;
            Quaternion rotation = Quaternion.CreateFromRotationMatrix(worldMatrix);

            if (Physics == null || Physics.CharacterProxy == null && Physics.RigidBody == null)
                return true;


            m_penetrationList.Clear();

            if (!useCharacterCenter)
            {
                Vector3D transformedCenter = Vector3D.TransformNormal(Physics.Center, WorldMatrix);
                translation += transformedCenter;
            }

            MyPhysics.GetPenetrationsShape(Physics.CharacterProxy != null ? Physics.CharacterProxy.GetCollisionShape() : Physics.RigidBody.GetShape(), ref translation, ref rotation, m_penetrationList, MyPhysics.CharacterCollisionLayer);
            bool somethingHit = false;
            foreach (var collision in m_penetrationList)
            {
                if (collision.GetCollisionEntity() == null || !collision.GetCollisionEntity().Physics.IsPhantom)
                {
                    somethingHit = true;
                    break;
                }
                else if (checkCharacters)
                {
                    somethingHit = true;
                    break;
                }
            }

            if (MySession.Static.VoxelMaps == null)
                return true;

            if (!somethingHit)
            { //test voxels
                BoundingSphereD sphere = new BoundingSphereD(worldMatrix.Translation, 0.75f);
                var overlappedVoxelmap = MySession.Static.VoxelMaps.GetOverlappingWithSphere(ref sphere);
                somethingHit = overlappedVoxelmap != null;
            }

            return !somethingHit;
        }

        public MyCharacterMovementEnum GetCurrentMovementState()
        {
            return m_currentMovementState;
        }

		public MyCharacterMovementEnum GetPreviousMovementState()
		{
			return m_previousMovementState;
		}

		public void SetPreviousMovementState(MyCharacterMovementEnum previousMovementState)
		{
			m_previousMovementState = previousMovementState;
		}

		internal void SetCurrentMovementState(MyCharacterMovementEnum state, bool updateSync = true)
		{
			System.Diagnostics.Debug.Assert(m_currentMovementState != MyCharacterMovementEnum.Died || m_currentMovementState == state,"Trying to set a new movement state, but character is in dead state!");
			//System.Diagnostics.Debug.Assert(!updateSync || (updateSync && ControllerInfo.IsLocalPlayer()));

			if (m_currentMovementState == state)
				return;

			if (Physics.CharacterProxy != null)
			{
				switch (state)
				{
					case MyCharacterMovementEnum.Crouching:
						Physics.CharacterProxy.SetShapeForCrouch(Physics.HavokWorld, true);
						break;

					case MyCharacterMovementEnum.CrouchRotatingLeft:
					case MyCharacterMovementEnum.CrouchRotatingRight:
					case MyCharacterMovementEnum.CrouchWalking:
					case MyCharacterMovementEnum.CrouchBackWalking:
					case MyCharacterMovementEnum.CrouchWalkingLeftBack:
					case MyCharacterMovementEnum.CrouchWalkingRightBack:
					case MyCharacterMovementEnum.CrouchWalkingLeftFront:
					case MyCharacterMovementEnum.CrouchWalkingRightFront:
					case MyCharacterMovementEnum.CrouchStrafingLeft:
					case MyCharacterMovementEnum.CrouchStrafingRight:
						Physics.CharacterProxy.SetShapeForCrouch(Physics.HavokWorld, true);
						break;

					default:
						Physics.CharacterProxy.SetShapeForCrouch(Physics.HavokWorld, false);
						break;
				}
			}

			m_previousMovementState = m_currentMovementState;
			m_currentMovementState = state;
			if (OnMovementStateChanged != null)
				OnMovementStateChanged(m_previousMovementState, m_currentMovementState);

			if (updateSync && SyncObject != null)
				SyncObject.ChangeMovementState(state);
		}


        float GetMovementAcceleration(MyCharacterMovementEnum movement)
        {
            switch (movement)
            {
                case MyCharacterMovementEnum.Standing:
                case MyCharacterMovementEnum.Crouching:
                    {
                        return MyPerGameSettings.CharacterMovement.WalkAcceleration;
                    }
                case MyCharacterMovementEnum.Walking:
                case MyCharacterMovementEnum.BackWalking:
                case MyCharacterMovementEnum.WalkingLeftBack:
                case MyCharacterMovementEnum.WalkingLeftFront:
                case MyCharacterMovementEnum.WalkingRightBack:
                case MyCharacterMovementEnum.WalkingRightFront:
                case MyCharacterMovementEnum.Running:
                case MyCharacterMovementEnum.Backrunning:
                case MyCharacterMovementEnum.RunningLeftBack:
                case MyCharacterMovementEnum.RunningLeftFront:
                case MyCharacterMovementEnum.RunningRightBack:
                case MyCharacterMovementEnum.RunningRightFront:
                case MyCharacterMovementEnum.CrouchWalking:
                case MyCharacterMovementEnum.CrouchBackWalking:
                case MyCharacterMovementEnum.CrouchWalkingLeftBack:
                case MyCharacterMovementEnum.CrouchWalkingLeftFront:
                case MyCharacterMovementEnum.CrouchWalkingRightBack:
                case MyCharacterMovementEnum.CrouchWalkingRightFront:
                    {
                        return MyPerGameSettings.CharacterMovement.WalkAcceleration;
                    }
                case MyCharacterMovementEnum.Sprinting:
                    {
                        return MyPerGameSettings.CharacterMovement.SprintAcceleration;
                    }

                case MyCharacterMovementEnum.Jump:
                    {
                        return 0;
                    }

                case MyCharacterMovementEnum.WalkStrafingLeft:
                case MyCharacterMovementEnum.RunStrafingLeft:
                case MyCharacterMovementEnum.CrouchStrafingLeft:
                    {
                        return MyPerGameSettings.CharacterMovement.WalkAcceleration;
                    }

                case MyCharacterMovementEnum.WalkStrafingRight:
                case MyCharacterMovementEnum.RunStrafingRight:
                case MyCharacterMovementEnum.CrouchStrafingRight:
                    {
                        return MyPerGameSettings.CharacterMovement.WalkAcceleration;
                    }

                default:
                    System.Diagnostics.Debug.Assert(false, "Unknown walking state");
                    break;
            }

            return 0;
        }

        bool IsWalkingState(MyCharacterMovementEnum state)
        {
            switch (state)
            {
                case MyCharacterMovementEnum.Walking:
                case MyCharacterMovementEnum.BackWalking:
                case MyCharacterMovementEnum.WalkingLeftBack:
                case MyCharacterMovementEnum.WalkingRightBack:
                case MyCharacterMovementEnum.WalkStrafingLeft:
                case MyCharacterMovementEnum.WalkStrafingRight:
                case MyCharacterMovementEnum.WalkingLeftFront:
                case MyCharacterMovementEnum.WalkingRightFront:
                case MyCharacterMovementEnum.Running:
                case MyCharacterMovementEnum.Backrunning:
                case MyCharacterMovementEnum.RunningLeftBack:
                case MyCharacterMovementEnum.RunningRightBack:
                case MyCharacterMovementEnum.RunStrafingLeft:
                case MyCharacterMovementEnum.RunStrafingRight:
                case MyCharacterMovementEnum.RunningLeftFront:
                case MyCharacterMovementEnum.RunningRightFront:
                case MyCharacterMovementEnum.CrouchWalking:
                case MyCharacterMovementEnum.CrouchWalkingLeftFront:
                case MyCharacterMovementEnum.CrouchWalkingRightFront:
                case MyCharacterMovementEnum.CrouchBackWalking:
                case MyCharacterMovementEnum.CrouchWalkingLeftBack:
                case MyCharacterMovementEnum.CrouchWalkingRightBack:
                case MyCharacterMovementEnum.CrouchStrafingLeft:
                case MyCharacterMovementEnum.CrouchStrafingRight:
                case MyCharacterMovementEnum.Sprinting:
                    return true;
                    break;

                default:
                    return false;
            }
        }


        internal void SwitchAnimation(MyCharacterMovementEnum movementState, bool checkState = true)
        {
            if (checkState && m_currentMovementState == movementState)
                return;

            bool oldIsWalkingState = IsWalkingState(m_currentMovementState);
            bool newIsWalkingState = IsWalkingState(movementState);

            if (oldIsWalkingState != newIsWalkingState)
            {
                m_currentHandItemWalkingBlend = 0;
            }

            switch (movementState)
            {
                case MyCharacterMovementEnum.Walking:
                    PlayCharacterAnimation("Walk", AdjustSafeAnimationEnd(MyBlendOption.WaitForPreviousEnd), MyFrameOption.Loop, AdjustSafeAnimationBlend(0.1f));
                    break;

                case MyCharacterMovementEnum.BackWalking:
                    PlayCharacterAnimation("WalkBack", AdjustSafeAnimationEnd(MyBlendOption.WaitForPreviousEnd), MyFrameOption.Loop, AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.WalkingLeftBack:
                    PlayCharacterAnimation("WalkLeftBack", AdjustSafeAnimationEnd(MyBlendOption.WaitForPreviousEnd), MyFrameOption.Loop, AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.WalkingRightBack:
                    PlayCharacterAnimation("WalkRightBack", AdjustSafeAnimationEnd(MyBlendOption.WaitForPreviousEnd), MyFrameOption.Loop, AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.WalkStrafingLeft:
                    PlayCharacterAnimation("StrafeLeft", AdjustSafeAnimationEnd(MyBlendOption.WaitForPreviousEnd), MyFrameOption.Loop, AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.WalkStrafingRight:
                    PlayCharacterAnimation("StrafeRight", AdjustSafeAnimationEnd(MyBlendOption.WaitForPreviousEnd), MyFrameOption.Loop, AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.WalkingLeftFront:
                    PlayCharacterAnimation("WalkLeftFront", AdjustSafeAnimationEnd(MyBlendOption.WaitForPreviousEnd), MyFrameOption.Loop, AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.WalkingRightFront:
                    PlayCharacterAnimation("WalkRightFront", AdjustSafeAnimationEnd(MyBlendOption.WaitForPreviousEnd), MyFrameOption.Loop, AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.Running:
                    PlayCharacterAnimation("Run", AdjustSafeAnimationEnd(MyBlendOption.WaitForPreviousEnd), MyFrameOption.Loop, AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.Backrunning:
                    PlayCharacterAnimation("RunBack", AdjustSafeAnimationEnd(MyBlendOption.WaitForPreviousEnd), MyFrameOption.Loop, AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.RunningLeftBack:
                    PlayCharacterAnimation("RunLeftBack", AdjustSafeAnimationEnd(MyBlendOption.WaitForPreviousEnd), MyFrameOption.Loop, AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.RunningRightBack:
                    PlayCharacterAnimation("RunRightBack", AdjustSafeAnimationEnd(MyBlendOption.WaitForPreviousEnd), MyFrameOption.Loop, AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.RunStrafingLeft:
                    PlayCharacterAnimation("RunLeft", AdjustSafeAnimationEnd(MyBlendOption.WaitForPreviousEnd), MyFrameOption.Loop, AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.RunStrafingRight:
                    PlayCharacterAnimation("RunRight", AdjustSafeAnimationEnd(MyBlendOption.WaitForPreviousEnd), MyFrameOption.Loop, AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.RunningLeftFront:
                    PlayCharacterAnimation("RunLeftFront", AdjustSafeAnimationEnd(MyBlendOption.WaitForPreviousEnd), MyFrameOption.Loop, AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.RunningRightFront:
                    PlayCharacterAnimation("RunRightFront", AdjustSafeAnimationEnd(MyBlendOption.WaitForPreviousEnd), MyFrameOption.Loop, AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.CrouchWalking:
                    PlayCharacterAnimation("CrouchWalk", AdjustSafeAnimationEnd(MyBlendOption.WaitForPreviousEnd), MyFrameOption.Loop, AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.CrouchWalkingLeftFront:
                    PlayCharacterAnimation("CrouchWalkLeftFront", AdjustSafeAnimationEnd(MyBlendOption.WaitForPreviousEnd), MyFrameOption.Loop, AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.CrouchWalkingRightFront:
                    PlayCharacterAnimation("CrouchWalkRightFront", AdjustSafeAnimationEnd(MyBlendOption.WaitForPreviousEnd), MyFrameOption.Loop, AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.CrouchBackWalking:
                    PlayCharacterAnimation("CrouchWalkBack", AdjustSafeAnimationEnd(MyBlendOption.WaitForPreviousEnd), MyFrameOption.Loop, AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.CrouchWalkingLeftBack:
                    PlayCharacterAnimation("CrouchWalkLeftBack", AdjustSafeAnimationEnd(MyBlendOption.WaitForPreviousEnd), MyFrameOption.Loop, AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.CrouchWalkingRightBack:
                    PlayCharacterAnimation("CrouchWalkRightBack", AdjustSafeAnimationEnd(MyBlendOption.WaitForPreviousEnd), MyFrameOption.Loop, AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.CrouchStrafingLeft:
                    PlayCharacterAnimation("CrouchStrafeLeft", AdjustSafeAnimationEnd(MyBlendOption.WaitForPreviousEnd), MyFrameOption.Loop, AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.CrouchStrafingRight:
                    PlayCharacterAnimation("CrouchStrafeRight", AdjustSafeAnimationEnd(MyBlendOption.WaitForPreviousEnd), MyFrameOption.Loop, AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.Sprinting:
                    PlayCharacterAnimation("Sprint", AdjustSafeAnimationEnd(MyBlendOption.WaitForPreviousEnd), MyFrameOption.Loop, AdjustSafeAnimationBlend(0.1f));
                    break;

                case MyCharacterMovementEnum.Standing:
                        PlayCharacterAnimation("Idle", AdjustSafeAnimationEnd(MyBlendOption.WaitForPreviousEnd), MyFrameOption.Loop, AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.Crouching:
                    PlayCharacterAnimation("CrouchIdle", AdjustSafeAnimationEnd(MyBlendOption.WaitForPreviousEnd), MyFrameOption.Loop, AdjustSafeAnimationBlend(0.1f));
                    break;

                case MyCharacterMovementEnum.Flying:
                    PlayCharacterAnimation("Jetpack", AdjustSafeAnimationEnd(MyBlendOption.Immediate), MyFrameOption.Loop, AdjustSafeAnimationBlend(0.0f));
                    break;

                //Multiplayer
                case MyCharacterMovementEnum.Jump:
                    PlayCharacterAnimation("Jump", AdjustSafeAnimationEnd(MyBlendOption.Immediate), MyFrameOption.Default, AdjustSafeAnimationBlend(0.0f), 1.3f);
                    break;

                case MyCharacterMovementEnum.Falling:
                        PlayCharacterAnimation("FreeFall", AdjustSafeAnimationEnd(MyBlendOption.Immediate), MyFrameOption.Loop, AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.CrouchRotatingLeft:
                    PlayCharacterAnimation("CrouchLeftTurn",AdjustSafeAnimationEnd( MyBlendOption.WaitForPreviousEnd), MyFrameOption.Loop, AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.CrouchRotatingRight:
                    PlayCharacterAnimation("CrouchRightTurn", AdjustSafeAnimationEnd(MyBlendOption.WaitForPreviousEnd), MyFrameOption.Loop, AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.RotatingLeft:
                    PlayCharacterAnimation("StandLeftTurn", AdjustSafeAnimationEnd(MyBlendOption.WaitForPreviousEnd), MyFrameOption.Loop, AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.RotatingRight:
                    PlayCharacterAnimation("StandRightTurn", AdjustSafeAnimationEnd(MyBlendOption.WaitForPreviousEnd), MyFrameOption.Loop, AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.Died:
                    PlayCharacterAnimation("Died", AdjustSafeAnimationEnd(MyBlendOption.Immediate), MyFrameOption.Default, AdjustSafeAnimationBlend(0.5f));
                    break;

                case MyCharacterMovementEnum.Sitting:
                    break;


                default:
                    System.Diagnostics.Debug.Assert(false, "Unknown movement state");
                    break;
            }
        }

        MyCharacterMovementEnum GetNewMovementState(ref Vector3 moveIndicator, ref Vector2 rotationIndicator, ref float acceleration, bool sprint, bool walk, bool canMove, bool movementFlagsChanged)
        {
            // OM: Once dead, always dead, no resurrection in the game :-)
            if (m_currentMovementState == MyCharacterMovementEnum.Died)
            {
                return MyCharacterMovementEnum.Died;
            }

            MyCharacterMovementEnum newMovementState = m_currentMovementState;

            if (Definition.UseOnlyWalking)
                walk = true;

            if (m_currentJump > 0f)
                return MyCharacterMovementEnum.Jump;

	        var jetpack = JetpackComp;
            if (jetpack != null && jetpack.Running)
                return MyCharacterMovementEnum.Flying;

			bool canWalk = true;
			bool canRun = true;
			bool canSprint = true;
			bool canMoveInternal = true;
			if (StatComp != null)
			{
				canWalk = StatComp.CanDoAction("Walk");
				canRun = StatComp.CanDoAction("Run");
				canSprint = StatComp.CanDoAction("Sprint");
				canMoveInternal = canWalk || canRun || canSprint;
			}

			bool moving = ((moveIndicator.X != 0 || moveIndicator.Z != 0) && canMove && canMoveInternal);
            bool rotating = rotationIndicator.X != 0 || rotationIndicator.Y != 0;

            if (moving || movementFlagsChanged)
            {
				if (sprint && canSprint)
				{
					newMovementState = GetSprintState(ref moveIndicator);
				}
				else
				{
					if (moving)
					{
						if (walk && canWalk)
							newMovementState = GetWalkingState(ref moveIndicator);
						else if(canRun)
							newMovementState = GetRunningState(ref moveIndicator);
						else
							newMovementState = GetWalkingState(ref moveIndicator);
					}
					else
					{
						newMovementState = GetIdleState();
					}
				}

                acceleration = GetMovementAcceleration(newMovementState);
                m_currentDecceleration = 0;
            }
            else
                if (rotating)
                {
                       const float ANGLE_FOR_ROTATION_ANIMATION = 20;

                       if ((Math.Abs(rotationIndicator.Y) > ANGLE_FOR_ROTATION_ANIMATION) &&
                           (m_currentMovementState == MyCharacterMovementEnum.Standing || m_currentMovementState == MyCharacterMovementEnum.Crouching)
                           )
                       {
                           if (WantsCrouch)
                           {
                               if (rotationIndicator.Y > 0)
                               {
                                   newMovementState = MyCharacterMovementEnum.CrouchRotatingRight;
                               }
                               else
                               {
                                   newMovementState = MyCharacterMovementEnum.CrouchRotatingLeft;
                               }
                           }
                           else
                           {
                               if (rotationIndicator.Y > 0)
                               {
                                   newMovementState = MyCharacterMovementEnum.RotatingRight;
                               }
                               else
                               {
                                   newMovementState = MyCharacterMovementEnum.RotatingLeft;
                               }
                           }
                       }
                }
                else
                {

                    switch (m_currentMovementState)
                    {
                        case MyCharacterMovementEnum.Walking:
                        case MyCharacterMovementEnum.WalkingLeftFront:
                        case MyCharacterMovementEnum.WalkingRightFront:
                        case MyCharacterMovementEnum.BackWalking:
                        case MyCharacterMovementEnum.WalkingLeftBack:
                        case MyCharacterMovementEnum.WalkingRightBack:
                        case MyCharacterMovementEnum.WalkStrafingLeft:
                        case MyCharacterMovementEnum.WalkStrafingRight:
                        case MyCharacterMovementEnum.Running:
                        case MyCharacterMovementEnum.RunningLeftFront:
                        case MyCharacterMovementEnum.RunningRightFront:
                        case MyCharacterMovementEnum.Backrunning:
                        case MyCharacterMovementEnum.RunningLeftBack:
                        case MyCharacterMovementEnum.RunningRightBack:
                        case MyCharacterMovementEnum.RunStrafingLeft:
                        case MyCharacterMovementEnum.RunStrafingRight:
                        case MyCharacterMovementEnum.CrouchWalking:
                        case MyCharacterMovementEnum.CrouchWalkingLeftFront:
                        case MyCharacterMovementEnum.CrouchWalkingRightFront:
                        case MyCharacterMovementEnum.CrouchBackWalking:
                        case MyCharacterMovementEnum.CrouchWalkingLeftBack:
                        case MyCharacterMovementEnum.CrouchWalkingRightBack:
                        case MyCharacterMovementEnum.CrouchStrafingLeft:
                        case MyCharacterMovementEnum.CrouchStrafingRight:
                            {
                                newMovementState = GetIdleState();
                                m_currentDecceleration = MyPerGameSettings.CharacterMovement.WalkDecceleration;

                                break;
                            }

                        case MyCharacterMovementEnum.Sprinting:
                            {
                                newMovementState = GetIdleState();

                                m_currentDecceleration = MyPerGameSettings.CharacterMovement.SprintDecceleration;

                                break;
                            }

                        case MyCharacterMovementEnum.Standing:
                            {
                                if (WantsCrouch)
                                    newMovementState = GetIdleState();

                                break;
                            }

                        case MyCharacterMovementEnum.Crouching:
                            {
                                if (!WantsCrouch)
                                    newMovementState = GetIdleState();

                                break;
                            }

                        case MyCharacterMovementEnum.Flying:
                        case MyCharacterMovementEnum.Jump:
                            break;

                        case MyCharacterMovementEnum.RotatingLeft:
                        case MyCharacterMovementEnum.RotatingRight:
                        case MyCharacterMovementEnum.CrouchRotatingLeft:
                        case MyCharacterMovementEnum.CrouchRotatingRight:
                            newMovementState = GetIdleState();
                            break;

                        default:
                            //System.Diagnostics.Debug.Assert(false, "Unknown movement state");
                            break;
                    }
                }

            return newMovementState;
        }

        internal float LimitMaxSpeed(float currentSpeed, MyCharacterMovementEnum movementState)
        {
			float limitedSpeed = currentSpeed;
			switch (movementState)
            {
                case MyCharacterMovementEnum.Running:
                case MyCharacterMovementEnum.Flying:
                    {
						limitedSpeed = MathHelper.Clamp(currentSpeed, -Definition.MaxRunSpeed, Definition.MaxRunSpeed);
                        break;
                    }

                case MyCharacterMovementEnum.Walking:
                    {
						limitedSpeed = MathHelper.Clamp(currentSpeed, -Definition.MaxWalkSpeed, Definition.MaxWalkSpeed);
                        break;
                    }

                case MyCharacterMovementEnum.BackWalking:
                case MyCharacterMovementEnum.WalkingLeftBack:
                case MyCharacterMovementEnum.WalkingRightBack:
                    {
						limitedSpeed = MathHelper.Clamp(currentSpeed, -Definition.MaxBackwalkSpeed, Definition.MaxBackwalkSpeed);
                        break;
                    }

                case MyCharacterMovementEnum.WalkStrafingLeft:
                case MyCharacterMovementEnum.WalkStrafingRight:
                case MyCharacterMovementEnum.WalkingLeftFront:
                case MyCharacterMovementEnum.WalkingRightFront:
                    {
						limitedSpeed = MathHelper.Clamp(currentSpeed, -Definition.MaxWalkStrafingSpeed, Definition.MaxWalkStrafingSpeed);
                        break;
                    }


                case MyCharacterMovementEnum.Backrunning:
                case MyCharacterMovementEnum.RunningLeftBack:
                case MyCharacterMovementEnum.RunningRightBack:
                    {
						limitedSpeed = MathHelper.Clamp(currentSpeed, -Definition.MaxBackrunSpeed, Definition.MaxBackrunSpeed);
                        break;
                    }

                case MyCharacterMovementEnum.RunStrafingLeft:
                case MyCharacterMovementEnum.RunStrafingRight:
                case MyCharacterMovementEnum.RunningLeftFront:
                case MyCharacterMovementEnum.RunningRightFront:
                    {
						limitedSpeed = MathHelper.Clamp(currentSpeed, -Definition.MaxRunStrafingSpeed, Definition.MaxRunStrafingSpeed);
                        break;
                    }


                case MyCharacterMovementEnum.CrouchWalking:
                    {
						limitedSpeed = MathHelper.Clamp(currentSpeed, -Definition.MaxCrouchWalkSpeed, Definition.MaxCrouchWalkSpeed);
                        break;
                    }

                case MyCharacterMovementEnum.CrouchStrafingLeft:
                case MyCharacterMovementEnum.CrouchStrafingRight:
                case MyCharacterMovementEnum.CrouchWalkingLeftFront:
                case MyCharacterMovementEnum.CrouchWalkingRightFront:
                    {
						limitedSpeed = MathHelper.Clamp(currentSpeed, -Definition.MaxCrouchStrafingSpeed, Definition.MaxCrouchStrafingSpeed);
                        break;
                    }

                case MyCharacterMovementEnum.CrouchBackWalking:
                case MyCharacterMovementEnum.CrouchWalkingLeftBack:
                case MyCharacterMovementEnum.CrouchWalkingRightBack:
                    {
						limitedSpeed = MathHelper.Clamp(currentSpeed, -Definition.MaxCrouchBackwalkSpeed, Definition.MaxCrouchBackwalkSpeed);
                        break;
                    }

                case MyCharacterMovementEnum.Sprinting:
                    {
						limitedSpeed = MathHelper.Clamp(currentSpeed, -Definition.MaxSprintSpeed, Definition.MaxSprintSpeed);
                        break;
                    }

                case MyCharacterMovementEnum.Jump:
                    {
                        break;
                    }

                case MyCharacterMovementEnum.Standing:
                case MyCharacterMovementEnum.Crouching:
                case MyCharacterMovementEnum.Sitting:
                case MyCharacterMovementEnum.RotatingLeft:
                case MyCharacterMovementEnum.RotatingRight:
                case MyCharacterMovementEnum.CrouchRotatingLeft:
                case MyCharacterMovementEnum.CrouchRotatingRight:
                case MyCharacterMovementEnum.Falling:
                    break;

                default:
                    {
                        System.Diagnostics.Debug.Assert(false);
                        break;
                    }
            }
			return limitedSpeed;
        }

        private float AdjustSafeAnimationBlend(float idealBlend)
        {
            float blend = 0;
            if (m_currentAnimationChangeDelay > SAFE_DELAY_FOR_ANIMATION_BLEND)
                blend = idealBlend;
            m_currentAnimationChangeDelay = 0;
            return blend;
        }

        private MyBlendOption AdjustSafeAnimationEnd(MyBlendOption idealEnd)
        {
            //wait for previous end is important ie. for turning animation. You must wait until previous turning animation ends

            MyBlendOption end = MyBlendOption.Immediate;
            if (m_currentAnimationChangeDelay > SAFE_DELAY_FOR_ANIMATION_BLEND)
                end = idealEnd;

            return end;
        }

        private MyCharacterMovementEnum GetWalkingState(ref Vector3 moveIndicator)
        {
            double tan23 = Math.Tan(MathHelper.ToRadians(23));
            if (Math.Abs(moveIndicator.X) < tan23 * Math.Abs(moveIndicator.Z))
            {
                if (moveIndicator.Z < 0)
                {
                    if (!WantsCrouch)
                    {
                        return MyCharacterMovementEnum.Walking;
                    }
                    else
                    {
                        return MyCharacterMovementEnum.CrouchWalking;
                    }
                }
                else
                {
                    if (!WantsCrouch)
                    {
                        return MyCharacterMovementEnum.BackWalking;
                    }
                    else
                    {
                        return MyCharacterMovementEnum.CrouchBackWalking;
                    }
                }
            }
            else if (Math.Abs(moveIndicator.X) * tan23 > Math.Abs(moveIndicator.Z))
            {
                if (moveIndicator.X > 0)
                {
                    if (!WantsCrouch)
                    {
                        return MyCharacterMovementEnum.WalkStrafingRight;
                    }
                    else
                    {
                        return MyCharacterMovementEnum.CrouchStrafingRight;
                    }
                }
                else
                {
                    if (!WantsCrouch)
                    {
                        return MyCharacterMovementEnum.WalkStrafingLeft;
                    }
                    else
                    {
                        return MyCharacterMovementEnum.CrouchStrafingLeft;
                    }
                }
            }
            else
            {
                if (moveIndicator.X > 0)
                {
                    if (moveIndicator.Z < 0)
                    {
                        if (!WantsCrouch)
                        {
                            return MyCharacterMovementEnum.WalkingRightFront;
                        }
                        else
                        {
                            return MyCharacterMovementEnum.CrouchWalkingRightFront;
                        }
                    }
                    else
                    {
                        if (!WantsCrouch)
                        {
                            return MyCharacterMovementEnum.WalkingRightBack;
                        }
                        else
                        {
                            return MyCharacterMovementEnum.CrouchWalkingRightBack;
                        }
                    }
                }
                else
                {
                    if (moveIndicator.Z < 0)
                    {
                        if (!WantsCrouch)
                        {
                            return MyCharacterMovementEnum.WalkingLeftFront;
                        }
                        else
                        {
                            return MyCharacterMovementEnum.CrouchWalkingLeftFront;
                        }
                    }
                    else
                    {
                        if (!WantsCrouch)
                        {
                            return MyCharacterMovementEnum.WalkingLeftBack;
                        }
                        else
                        {
                            return MyCharacterMovementEnum.CrouchWalkingLeftBack;
                        }
                    }
                }
            }
        }

        private MyCharacterMovementEnum GetRunningState(ref Vector3 moveIndicator)
        {
            double tan23 = Math.Tan(MathHelper.ToRadians(23));
            if (Math.Abs(moveIndicator.X) < tan23 * Math.Abs(moveIndicator.Z))
            {
                if (moveIndicator.Z < 0)
                {
                    if (!WantsCrouch)
                    {
                        return MyCharacterMovementEnum.Running;
                    }
                    else
                    {
                        return MyCharacterMovementEnum.CrouchWalking;
                    }
                }
                else
                {
                    if (!WantsCrouch)
                    {
                        return MyCharacterMovementEnum.Backrunning;
                    }
                    else
                    {
                        return MyCharacterMovementEnum.CrouchBackWalking;
                    }
                }
            }
            else if (Math.Abs(moveIndicator.X) * tan23 > Math.Abs(moveIndicator.Z))
            {
                if (moveIndicator.X > 0)
                {
                    if (!WantsCrouch)
                    {
                        return MyCharacterMovementEnum.RunStrafingRight;
                    }
                    else
                    {
                        return MyCharacterMovementEnum.CrouchStrafingRight;
                    }
                }
                else
                {
                    if (!WantsCrouch)
                    {
                        return MyCharacterMovementEnum.RunStrafingLeft;
                    }
                    else
                    {
                        return MyCharacterMovementEnum.CrouchStrafingLeft;
                    }
                }
            }
            else
            {
                if (moveIndicator.X > 0)
                {
                    if (moveIndicator.Z < 0)
                    {
                        if (!WantsCrouch)
                        {
                            return MyCharacterMovementEnum.RunningRightFront;
                        }
                        else
                        {
                            return MyCharacterMovementEnum.CrouchWalkingRightFront;
                        }
                    }
                    else
                    {
                        if (!WantsCrouch)
                        {
                            return MyCharacterMovementEnum.RunningRightBack;
                        }
                        else
                        {
                            return MyCharacterMovementEnum.CrouchWalkingRightBack;
                        }
                    }
                }
                else
                {
                    if (moveIndicator.Z < 0)
                    {
                        if (!WantsCrouch)
                        {
                            return MyCharacterMovementEnum.RunningLeftFront;
                        }
                        else
                        {
                            return MyCharacterMovementEnum.CrouchWalkingLeftFront;
                        }
                    }
                    else
                    {
                        if (!WantsCrouch)
                        {
                            return MyCharacterMovementEnum.RunningLeftBack;
                        }
                        else
                        {
                            return MyCharacterMovementEnum.CrouchWalkingLeftBack;
                        }
                    }
                }
            }

            System.Diagnostics.Debug.Assert(false, "Non moving character cannot get here");
            return MyCharacterMovementEnum.Standing;
        }

        private MyCharacterMovementEnum GetSprintState(ref Vector3 moveIndicator)
        {
            if (moveIndicator.X == 0 && moveIndicator.Z < 0)
            {
                return MyCharacterMovementEnum.Sprinting;
            }
            else
            {
                return GetRunningState(ref moveIndicator);
            }            
        }

        private MyCharacterMovementEnum GetIdleState()
        {
            if (!WantsCrouch)
            {
                return MyCharacterMovementEnum.Standing;
            }
            else
            {
                return MyCharacterMovementEnum.Crouching;
            }
        }

        void UpdateCapsuleBones()
        {
            if (m_bodyCapsuleBones == null) return;
            if (m_bodyCapsuleBones.Count == 0) return;

            if (Physics.Ragdoll != null && Components.Has<MyCharacterRagdollComponent>())
            {
                // TODO: OM - This needs to be changed..
                // Create capsules with help of ragdoll model
                var ragdollComponent = Components.Get<MyCharacterRagdollComponent>();
                int i = 0;
                foreach (var boneList in m_bodyCapsuleBones)
                {                    
                    var rigidBody = ragdollComponent.RagdollMapper.GetBodyBindedToBone(Bones[boneList.First()]);

                    MatrixD transformationMatrix = Bones[boneList.First()].AbsoluteTransform * WorldMatrix;

                    var shape = rigidBody.GetShape();
                    
                    m_bodyCapsules[i].P0 = transformationMatrix.Translation;
                    m_bodyCapsules[i].P1 = (Bones[boneList.Last()].AbsoluteTransform * WorldMatrix).Translation;
                    Vector3 difference = m_bodyCapsules[i].P0 - m_bodyCapsules[i].P1;

                    if (difference.LengthSquared() < 0.05f)
                    {
                        if (shape.ShapeType == HkShapeType.Capsule)
                        {
                            var capsuleShape = (HkCapsuleShape)shape;
                            m_bodyCapsules[i].P0 = Vector3.Transform(capsuleShape.VertexA, transformationMatrix);
                            m_bodyCapsules[i].P1 = Vector3.Transform(capsuleShape.VertexB, transformationMatrix);
                            m_bodyCapsules[i].Radius = capsuleShape.Radius * 0.8f;
                            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_SHOW_DAMAGE)
                            {
                                MyRenderProxy.DebugDrawCapsule(m_bodyCapsules[i].P0, m_bodyCapsules[i].P1, m_bodyCapsules[i].Radius, Color.Green, false, false);
                            }
                        }
                        else
                        {
                            Vector4 min4, max4;
                            shape.GetLocalAABB(0.0001f, out min4, out max4);
                            float distance = Math.Max(Math.Max(max4.X - min4.X, max4.Y - min4.Y), max4.Z - min4.Z) * 0.5f; // scalling because the aabb is always bigger

                            m_bodyCapsules[i].P0 = transformationMatrix.Translation + (transformationMatrix.Left * distance * 0.25f);
                            m_bodyCapsules[i].P1 = transformationMatrix.Translation + (transformationMatrix.Left * distance * 0.5f);
                            m_bodyCapsules[i].Radius = distance * 0.25f;
                            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_SHOW_DAMAGE)
                            {
                                MyRenderProxy.DebugDrawCapsule(m_bodyCapsules[i].P0, m_bodyCapsules[i].P1, m_bodyCapsules[i].Radius, Color.Blue, false, false);
                            }
                        }
                    }
                    else
                    {
                        if (shape.ShapeType == HkShapeType.Capsule)
                        {
                            var capsuleShape = (HkCapsuleShape)shape;
                            m_bodyCapsules[i].Radius = capsuleShape.Radius;
                        }
                        else
                        {
                            m_bodyCapsules[i].Radius = difference.Length() * 0.28f;
                        }                        
                        if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_SHOW_DAMAGE)
                        {
                            MyRenderProxy.DebugDrawCapsule(m_bodyCapsules[i].P0, m_bodyCapsules[i].P1, m_bodyCapsules[i].Radius, Color.Yellow, false, false);
                        }
                    }                    

                    i++;
                }
            }
            else
            {
                // Fallback to dynamically determined values for capsules
                int i = 0;
                foreach (var boneList in m_bodyCapsuleBones)
                {
                    m_bodyCapsules[i].P0 = (Bones[boneList.First()].AbsoluteTransform * WorldMatrix).Translation;
                    m_bodyCapsules[i].P1 = (Bones[boneList.Last()].AbsoluteTransform * WorldMatrix).Translation;
                    Vector3 difference = m_bodyCapsules[i].P0 - m_bodyCapsules[i].P1;

                    if (difference.LengthSquared() < 0.05f)
                    {
                        m_bodyCapsules[i].P1 = m_bodyCapsules[i].P0 + (Bones[boneList.First()].AbsoluteTransform * WorldMatrix).Left * 0.1f;
                        m_bodyCapsules[i].Radius = 0.1f;
                    }
                    else
                    {
                        m_bodyCapsules[i].Radius = difference.Length() * 0.3f;
                    }

                    if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_SHOW_DAMAGE)
                    {
                        MyRenderProxy.DebugDrawCapsule(m_bodyCapsules[i].P0, m_bodyCapsules[i].P1, m_bodyCapsules[i].Radius, Color.Green, false, false);
                    }

                    i++;
                }
            }
            m_characterBonesReady = true;
        }
        #endregion

        #region Debug draw

        private MatrixD GetHeadMatrixInternal(int headBone, bool includeY, bool includeX = true, bool forceHeadAnim = false, bool forceHeadBone = false)
        {
            if (PositionComp == null)
                return MatrixD.Identity;
            //Matrix matrixRotation = Matrix.Identity;
            MatrixD matrixRotation = MatrixD.Identity;

	        var jetpack = JetpackComp;
	        bool canFly = jetpack != null && jetpack.Running;
            bool useAnimationInsteadX = ShouldUseAnimatedHeadRotation() && (!canFly || IsLocalHeadAnimationInProgress()) || forceHeadAnim;

            if (includeX && !useAnimationInsteadX)
                matrixRotation = MatrixD.CreateFromAxisAngle(Vector3D.Right, MathHelper.ToRadians(m_headLocalXAngle));

            if (includeY)
                matrixRotation = matrixRotation * Matrix.CreateFromAxisAngle(Vector3.Up, MathHelper.ToRadians(m_headLocalYAngle));           

            Vector3 averageBob = Vector3.Zero;
            if (MySandboxGame.Config.DisableHeadbob && !forceHeadBone && !ForceFirstPersonCamera)
            {
                foreach (var headTranslation in m_bobQueue)
                {
                    averageBob += headTranslation;
                }
                if (m_bobQueue.Count > 0)
                    averageBob /= m_bobQueue.Count;
            }
            else
            {
                if (headBone != -1)
                {
                    averageBob = BoneAbsoluteTransforms[headBone].Translation;
                }
            }


            if (useAnimationInsteadX && headBone != -1)
            {
                //m_headMatrix = Matrix.CreateRotationX(-(float)Math.PI * 0.5f) * /* Matrix.CreateRotationY(-(float)Math.PI * 0.5f) */ Matrix.Normalize(BoneTransformsWrite[HEAD_DUMMY_BONE]);
                Matrix hm = Matrix.Normalize(BoneAbsoluteTransforms[headBone]);
                hm.Translation = averageBob;
                m_headMatrix = MatrixD.CreateRotationX(-Math.PI * 0.5) * hm;
            }
            else
            {
                //m_headMatrix = Matrix.CreateTranslation(BoneTransformsWrite[HEAD_DUMMY_BONE].Translation);
                m_headMatrix = MatrixD.CreateTranslation(averageBob);
                m_headMatrix.Translation = new Vector3D(0, m_headMatrix.Translation.Y, m_headMatrix.Translation.Z);
            }

            m_headMatrix.Translation += m_cameraShakeOffset;

            MatrixD matrix = WorldMatrix;
            MatrixD headPosition = matrixRotation * m_headMatrix * WorldMatrix;
            //headPosition.Translation += WorldMatrix.Up * 0.04f;
            //Vector3 translation = Vector3.Transform(headPosition, WorldMatrix);

            //Orient direction to some point in front of char
            Vector3D imagPoint = PositionComp.GetPosition() + WorldMatrix.Up + WorldMatrix.Forward * 10;
            Vector3 imagDir = Vector3.Normalize(imagPoint - headPosition.Translation);

            MatrixD orientMatrix = Definition.VerticalPositionFlyingOnly ? WorldMatrix : MatrixD.CreateFromDir((Vector3D)imagDir, WorldMatrix.Up);

            matrix.Translation = Vector3D.Zero;
            matrix = m_headMatrix * matrixRotation * orientMatrix;

            matrix.Translation = headPosition.Translation;

            return matrix;
        }

        public MatrixD GetHeadMatrix(bool includeY, bool includeX = true, bool forceHeadAnim = false, bool forceHeadBone = false)
        {
            int headBone = IsInFirstPersonView || forceHeadBone || ForceFirstPersonCamera ? m_headBoneIndex : m_camera3rdBoneIndex;
            return GetHeadMatrixInternal(headBone, includeY, includeX, forceHeadAnim, forceHeadBone);
        }

        public MatrixD Get3rdCameraMatrix(bool includeY, bool includeX = true)
        {
            return Matrix.Invert(Get3rdBoneMatrix(includeY, includeX));
        }

        public MatrixD Get3rdBoneMatrix(bool includeY, bool includeX = true)
        {
            return GetHeadMatrixInternal(m_camera3rdBoneIndex, includeY, includeX);
        }

        public override MatrixD GetViewMatrix()
        {
            if (IsDead && MyPerGameSettings.SwitchToSpectatorCameraAfterDeath)
            {                
                m_isInFirstPersonView = false;             
                if (m_lastCorrectSpectatorCamera == MatrixD.Zero)
                {
                    m_lastCorrectSpectatorCamera = MatrixD.CreateLookAt(WorldMatrix.Translation + 2 * Vector3.Up - 2 * Vector3.Forward, WorldMatrix.Translation, Vector3.Up);
                }
                Vector3 camPosition = MatrixD.Invert(m_lastCorrectSpectatorCamera).Translation;
                Vector3 target = WorldMatrix.Translation;
                if (m_headBoneIndex != -1)
                {
                    target = Vector3.Transform(Bones[m_headBoneIndex].AbsoluteTransform.Translation, WorldMatrix);
                }
                MatrixD viewMatrix = MatrixD.CreateLookAt(camPosition, target, Vector3.Up);
                return viewMatrix.IsValid() && viewMatrix != MatrixD.Zero ? viewMatrix : m_lastCorrectSpectatorCamera;
            }

            if (!m_isInFirstPersonView)
            {
                Matrix viewMatrix = Get3rdCameraMatrix(false, true);
                ForceFirstPersonCamera = !MyThirdPersonSpectator.Static.IsCameraPositionOk(viewMatrix);
                if (!ForceFirstPersonCamera)
                {                                       
                    if (m_switchBackToSpectatorTimer > 0)
                    {
                        m_switchBackToSpectatorTimer -= MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                        ForceFirstPersonCamera = true;
                    }
                    else
                    {
                        m_switchBackToFirstPersonTimer = CAMERA_SWITCH_DELAY;
                        return MyThirdPersonSpectator.Static.GetViewMatrix();
                    }
                }
                else
                {                    
                    if (m_switchBackToFirstPersonTimer > 0)
                    {
                       m_switchBackToFirstPersonTimer -= MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                       ForceFirstPersonCamera = false;
                       return MyThirdPersonSpectator.Static.GetViewMatrix();
                    }
                    else
                    {
                        m_switchBackToSpectatorTimer =  CAMERA_SWITCH_DELAY;
                    }
                }
            }

            MatrixD matrix = GetHeadMatrix(false, true);

            m_lastCorrectSpectatorCamera = MatrixD.Zero;

            return MatrixD.Invert(matrix);
        }

        internal override bool GetIntersectionWithLine(ref LineD line, out MyIntersectionResultLineTriangleEx? t, IntersectionFlags flags = IntersectionFlags.ALL_TRIANGLES)
        {
            bool hitHead;
            return GetIntersectionWithLine(ref line, out t, out hitHead);
        }

        // For debug draw only
        CapsuleD? m_hitCapsule;
        MyIntersectionResultLineTriangleEx? m_hitInfo;

        /// <summary>
        /// Returns closest hit from line start position.
        /// </summary>
        public bool GetIntersectionWithLine(ref LineD line, out MyIntersectionResultLineTriangleEx? t, out bool hitHead)
        {
            // TODO: This now uses caspule of physics rigid body on the character, it needs to be changed to ragdoll
            //       Currently this approach will be used to support Characters with different skeleton than humanoid

            t = null;
            hitHead = false;

            UpdateCapsuleBones();

            if (m_characterBonesReady == false)
                return false;

            double closestDistanceToHit = double.MaxValue;
            int hitCapsule = -1;

            m_hitCapsule = null;
            m_hitInfo = null;

            Vector3D hitPosition = Vector3D.Zero;
            Vector3D hitPosition2 = Vector3D.Zero;
            Vector3 hitNormal = Vector3.Zero;
            Vector3 hitNormal2 = Vector3.Zero;

            for (int i = 0; i < m_bodyCapsules.Length; i++)
            {
                CapsuleD capsule = m_bodyCapsules[i];
                //if (capsule.IsIntersected(line, out hitVector, out hitVector2, out hitVector3))
                if (capsule.Intersect(line, ref hitPosition, ref hitPosition2, ref hitNormal, ref hitNormal2))
                {
                    double distanceToHit = Vector3.Distance(hitPosition, line.From);
                    if (distanceToHit >= closestDistanceToHit)
                        continue;

                    closestDistanceToHit = distanceToHit;

                    hitCapsule = i;

                    MyTriangle_Vertexes vertexes = new MyTriangle_Vertexes();
                    //TODO: Make correct alg. to make triangle from capsule intersection
                    vertexes.Vertex0 = hitPosition + line.Direction * 0.5f;
                    vertexes.Vertex1 = hitPosition + hitNormal * 0.5f;
                    vertexes.Vertex2 = hitPosition - hitNormal * 0.8f;

                    t = new MyIntersectionResultLineTriangleEx(
                        new MyIntersectionResultLineTriangle(
                        ref vertexes,
                        ref hitNormal,
                        Vector3.Distance(hitPosition, line.From)),
                        this, ref line,
                        (Vector3D)hitPosition,
                        hitNormal);
                }
            }

            if (t != null)
            {
                hitHead = hitCapsule == 0 && m_bodyCapsules.Length > 1;

                m_hitCapsule = m_bodyCapsules[hitCapsule];
                m_hitInfo = t;

                if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW)
                {
                    CapsuleD capsule = m_bodyCapsules[hitCapsule];
                    MyRenderProxy.DebugDrawCapsule(capsule.P0, capsule.P1, capsule.Radius, Color.Red, false, false);
                    MyRenderProxy.DebugDrawSphere(hitPosition, 0.1f, Color.White, 1f, false);
                }

                return true;
            }

            return false;
        }


        #endregion

        #region Input handling

        public void BeginShoot(MyShootActionEnum action)
        {
            if (m_currentWeapon == null || m_currentMovementState == MyCharacterMovementEnum.Died) return;

            if (!m_currentWeapon.EnabledInWorldRules)
            {
                MyHud.Notifications.Add(MyNotificationSingletons.WeaponDisabledInWorldSettings);
                return;
            }

            SyncObject.BeginShoot(m_currentWeapon.DirectionToTarget(m_aimedPoint), action);
        }

        public void OnBeginShoot(MyShootActionEnum action)
        {
            if (ControllerInfo.Controller == null) return;
            if (m_currentWeapon == null) return;

            MyGunStatusEnum status = MyGunStatusEnum.OK;
            m_currentWeapon.CanShoot(action, ControllerInfo.ControllingIdentityId, out status);

            if (status == MyGunStatusEnum.OutOfAmmo)
            {
                // mw:TODO should auto change be implemented or not (uncomment code below)
                //if (m_currentWeapon.GunBase.SwitchAmmoMagazineToFirstAvailable())
                //    status = MyGunStatusEnum.OK;
            }

            if (status != MyGunStatusEnum.OK && status != MyGunStatusEnum.Cooldown)
            {
                ShootBeginFailed(action, status);
            }
        }

        void ShootInternal()
        {
            MyGunStatusEnum status = MyGunStatusEnum.OK;
            MyShootActionEnum? shootingAction = SyncObject.GetShootingAction();

            if (ControllerInfo == null || ControllerInfo.Controller == null) return;

            if (shootingAction.HasValue && m_currentWeapon.CanShoot(shootingAction.Value, ControllerInfo.ControllingIdentityId, out status))
            {
                m_currentWeapon.Shoot(shootingAction.Value, SyncObject.ShootDirection);
                UseAnimationForWeapon = MyPerGameSettings.UseAnimationInsteadOfIK;
                //if(!UseAnimationForWeapon)
                   // StopUpperCharacterAnimation(0);
            }

            if (MySession.ControlledEntity == this)
            {
                if (status != MyGunStatusEnum.OK && status != MyGunStatusEnum.Cooldown)
                {
                    ShootFailedLocal(shootingAction.Value, status);
                }
                else if (shootingAction.HasValue && m_currentWeapon.IsShooting && status == MyGunStatusEnum.OK)
                {
                    ShootSuccessfulLocal(shootingAction.Value);
                }

                SyncObject.UpdateShootDirection(m_currentWeapon.DirectionToTarget(m_aimedPoint), m_currentWeapon.ShootDirectionUpdateTime);
            }

            if (m_autoswitch != null)
            {
                SwitchToWeapon(m_autoswitch);
                m_autoswitch = null;
            }
        }

        private void ShootFailedLocal(MyShootActionEnum action, MyGunStatusEnum status)
        {
            if (status == MyGunStatusEnum.OutOfAmmo)
            {
                ShowOutOfAmmoNotification();
            }

            m_currentWeapon.ShootFailReactionLocal(action, status);
        }

        private void ShootBeginFailed(MyShootActionEnum action, MyGunStatusEnum status)
        {
            m_currentWeapon.BeginFailReaction(action, status);

            if (MySession.ControlledEntity == this)
            {
                m_currentWeapon.BeginFailReactionLocal(action, status);
            }
        }

        private void ShootSuccessfulLocal(MyShootActionEnum action)
        {
            m_currentShotTime = SHOT_TIME;

            if (m_cameraShake != null && m_currentWeapon.ShakeAmount != 0.0f)
                m_cameraShake.AddShake(MyUtils.GetRandomFloat(1.5f, m_currentWeapon.ShakeAmount));

	        var jetpack = JetpackComp;
            if (m_currentWeapon.BackkickForcePerSecond > 0 && ((jetpack != null && jetpack.Running) || m_isFalling))
            {
                Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE,
                    -m_currentWeapon.BackkickForcePerSecond * (Vector3)(m_currentWeapon as MyEntity).WorldMatrix.Forward, (Vector3)PositionComp.GetPosition(), null);
            }
        }

        public void SetupAutoswitch(MyDefinitionId? switchToNow, MyDefinitionId? switchOnEndShoot)
        {
            m_autoswitch = switchToNow;
            m_endShootAutoswitch = switchOnEndShoot;
        }

        private void EndShootAll()
        {
            foreach (MyShootActionEnum action in MyEnum<MyShootActionEnum>.Values)
            {
                if (SyncObject.IsShooting(action))
                    EndShoot(action);
            }
        }

        public void EndShoot(MyShootActionEnum action)
        {
            if (m_currentMovementState != MyCharacterMovementEnum.Died && m_currentWeapon != null)
            {
                SyncObject.EndShoot(action);
            }
        }

        public void OnEndShoot(MyShootActionEnum action)
        {
            if (m_currentMovementState != MyCharacterMovementEnum.Died && m_currentWeapon != null)
            {
                m_currentWeapon.EndShoot(action);

                // CH:TODO: End on which shoot? Primary or secondary?
                if (m_endShootAutoswitch != null)
                {
                    SwitchToWeapon(m_endShootAutoswitch);
                    m_endShootAutoswitch = null;
                }
            }
        }

        public void Zoom(bool newKeyPress, bool hideCrosshairWhenAiming = true)
        {
            switch (m_zoomMode)
            {
                case MyZoomModeEnum.Classic:
                    {
                        if (m_currentWeapon != null)
                        {
                            //m_secondarySoundEmitter.PlaySound(MySoundCuesEnum.ArcPlayIronSight);
                            //MyAudio.Static.PlayCue(MySoundCuesEnum.ArcPlayIronSightActivate, m_secondarySoundEmitter, Common.ObjectBuilders.Audio.MyAudioHelpers.Dimensions.D3);
                            //MyAudio.Static.PlayCue(MySoundCuesEnum.ArcPlayIronSightActivate);
							SoundComp.PlaySecondarySound(CharacterSoundsEnum.IRONSIGHT_ACT_SOUND, true);
                            EnableIronsight(true, newKeyPress, true, hideCrosshairWhenAiming: hideCrosshairWhenAiming);
                            //else if (MySession.Static.CreativeMode)
                            //{
                            //    ShootInternal(m_aimedPoint, true, false);
                            //}
                        }
                    }
                    break;
                case MyZoomModeEnum.IronSight:
                    {
                        //MyAudio.Static.PlayCue(MySoundCuesEnum.ArcPlayIronSightDeactivate, m_secondarySoundEmitter, Common.ObjectBuilders.Audio.MyAudioHelpers.Dimensions.D3);
                        //MyAudio.Static.PlayCue(MySoundCuesEnum.ArcPlayIronSightDeactivate);
						SoundComp.PlaySecondarySound(CharacterSoundsEnum.IRONSIGHT_DEACT_SOUND, true);
                        EnableIronsight(false, newKeyPress, true);
                    }
                    break;
            }
        }

        void EnableIronsight(bool enable, bool newKeyPress, bool changeCamera, bool updateSync = true, bool hideCrosshairWhenAiming = true)
        {
            if (enable)
            {
                if (m_currentWeapon != null && /*m_currentWeapon.Zoom(newKeyPress) &&*/ m_zoomMode != MyZoomModeEnum.IronSight)
                {
                    m_zoomMode = MyZoomModeEnum.IronSight;

                    if (changeCamera)
                    {
                        m_storedCameraSettings.Controller = MySession.GetCameraControllerEnum();
                        m_storedCameraSettings.Distance = MySession.GetCameraTargetDistance();

                        MySession.SetCameraController(MyCameraControllerEnum.Entity, this);

                        if (hideCrosshairWhenAiming)
                            MyHud.Crosshair.Hide();

                        MySector.MainCamera.Zoom.SetZoom(MyCameraZoomOperationType.ZoomingIn);
                    }
                }
            }
            else
            {
                m_zoomMode = MyZoomModeEnum.Classic;

                ForceFirstPersonCamera = false;

                if (changeCamera)
                {
                    MyHud.Crosshair.Show(null);
                    MySector.MainCamera.Zoom.SetZoom(MyCameraZoomOperationType.ZoomingOut);

                    MySession.SetCameraController(m_storedCameraSettings.Controller, this);
                    MySession.SetCameraTargetDistance(m_storedCameraSettings.Distance);
                }
            }

            if (updateSync)
            {
                SendFlags();
            }
        }

        internal void SendFlags()
        {
	        var jetpack = JetpackComp;
	        bool jetpackEnabled = jetpack != null && jetpack.TurnedOn;
	        bool dampenersEnabled = jetpack != null && jetpack.DampenersTurnedOn;
            SyncObject.ChangeFlags(
                jetpackEnabled, 
                dampenersEnabled, 
                LightEnabled, 
                m_zoomMode == MyZoomModeEnum.IronSight, 
                m_radioBroadcaster.WantsToBeEnabled, 
                TargetFromCamera);
        }

        IMyHandheldGunObject<MyDeviceBase> CreateGun(MyObjectBuilder_EntityBase gunEntity)
        {
            MyEntity entity = MyEntityFactory.CreateEntity(gunEntity);

            try
            {
                entity.Init(gunEntity);
            }
            catch (Exception)
            {
                return null;
            }
            return (IMyHandheldGunObject<MyDeviceBase>)entity;
        }

        /// <summary>
        /// This method finds the given weapon in the character's inventory. The weapon type has to be supplied
        /// either as PhysicalGunObject od weapon entity (e.g. Welder, CubePlacer, etc...).
        /// </summary>
        private MyPhysicalInventoryItem? FindWeaponByDefinition(MyDefinitionId weaponDefinition)
        {
            MyPhysicalInventoryItem? item = null;
            if (weaponDefinition.TypeId != typeof(MyObjectBuilder_PhysicalGunObject))
            {
                var physicalItemId = MyDefinitionManager.Static.GetPhysicalItemForHandItem(weaponDefinition).Id;
                item = Inventory.FindItem(physicalItemId);
            }
            else
            {
                item = Inventory.FindItem(weaponDefinition);
            }
            return item;
        }

        public bool CanSwitchToWeapon(MyDefinitionId? weaponDefinition)
        {
            if (!WeaponTakesBuilderFromInventory(weaponDefinition)) return true;
            var item = FindWeaponByDefinition(weaponDefinition.Value);
            if (item.HasValue) return true;
            return false;
        }

        public bool WeaponTakesBuilderFromInventory(MyDefinitionId? weaponDefinition)
        {
            if (weaponDefinition == null) return false;
            if (weaponDefinition.Value.TypeId == typeof(MyObjectBuilder_CubePlacer) ||
				(weaponDefinition.Value.TypeId == typeof(MyObjectBuilder_PhysicalGunObject) && weaponDefinition.Value.SubtypeId == manipulationToolId))
                return false;
            return !MySession.Static.CreativeMode && !MyFakes.ENABLE_SURVIVAL_SWITCHING;
        }

        public void SwitchToWeapon(MyDefinitionId weaponDefinition)
        {
            SwitchToWeapon(weaponDefinition, true);
        }

		public void SwitchToWeapon(MyToolbarItemWeapon weapon)
		{
			SwitchToWeapon(weapon, true);
		}

        public void SwitchAmmoMagazine()
        {
            SwitchAmmoMagazineInternal(true);
        }

        public bool CanSwitchAmmoMagazine()
        {
            return m_currentWeapon != null
                && m_currentWeapon.GunBase != null
                && m_currentWeapon.GunBase.CanSwitchAmmoMagazine();
        }

        void SwitchAmmoMagazineInternal(bool sync)
        {
            if (sync)
            {
                SyncObject.RequestSwitchAmmoMagazine();
                return;
            }

            if (!IsDead && CurrentWeapon != null)
            {
                CurrentWeapon.GunBase.SwitchAmmoMagazineToNextAvailable();
            }
        }

        void SwitchAmmoMagazineSuccess()
        {
            SwitchAmmoMagazineInternal(false);
        }

        public void SwitchToWeapon(MyDefinitionId? weaponDefinition, bool sync = true)
        {
            //if (CurrentWeapon == null)
            //{
            //    if (!weaponDefinition.HasValue) return;
            //}
            /*else
            {
                if (weaponDefinition.HasValue)
                {
                    if (MyDefinitionManager.Static.HandItemExistsFor(weaponDefinition.Value))
                    {
                        var handItemId = MyDefinitionManager.Static.GetHandItemForPhysicalItem(weaponDefinition.Value).Id;
                        if (CurrentWeapon.DefinitionId == handItemId)
                            return;
            }
                }
            }*/
            // CH:TODO: This part of code seems to do nothing
            if (weaponDefinition.HasValue && m_rightHandItemBone == -1)
                return;

            if (WeaponTakesBuilderFromInventory(weaponDefinition))
            {
                var item = FindWeaponByDefinition(weaponDefinition.Value);
                // This can pop-up after inventory truncation, which is OK. Uncomment for debugging
                //Debug.Assert(item != null, "Character switched to a weapon not in the inventory");
                if (item == null)
                    return;

                Debug.Assert(item.Value.Content != null, "item.Value.Content was null in MyCharacter.SwitchToWeapon");
                if (item.Value.Content == null)
                {
                    MySandboxGame.Log.WriteLine("item.Value.Content was null in MyCharacter.SwitchToWeapon");
                    MySandboxGame.Log.WriteLine("item.Value = " + item.Value);
                    MySandboxGame.Log.WriteLine("weaponDefinition.Value = " + weaponDefinition);
                    return;
                }

                var physicalGunObject = item.Value.Content as MyObjectBuilder_PhysicalGunObject;
                var gunEntity = physicalGunObject.GunEntity;
                if (gunEntity == null)
                {
                    var handItemId = MyDefinitionManager.Static.TryGetHandItemForPhysicalItem(physicalGunObject.GetId()).Id;
                    gunEntity = (MyObjectBuilder_EntityBase)MyObjectBuilderSerializer.CreateNewObject(handItemId);
                }
                else
                {
                    gunEntity.EntityId = 0;
                }

                SwitchToWeaponInternal(weaponDefinition, sync, true, gunEntity, 0);
            }
            else
            {
                SwitchToWeaponInternal(weaponDefinition, sync, true, null, 0);
            }
        }

		public void SwitchToWeapon(MyToolbarItemWeapon weapon, bool sync = true)
		{
			MyDefinitionId? weaponDefinition = null;
			if (weapon != null)
				weaponDefinition = weapon.Definition.Id;
			// CH:TODO: This part of code seems to do nothing
			if (weaponDefinition.HasValue && m_rightHandItemBone == -1)
				return;

			if (WeaponTakesBuilderFromInventory(weaponDefinition))
			{
				var item = FindWeaponByDefinition(weaponDefinition.Value);
				// This can pop-up after inventory truncation, which is OK. Uncomment for debugging
				//Debug.Assert(item != null, "Character switched to a weapon not in the inventory");
				if (item == null)
					return;

				Debug.Assert(item.Value.Content != null, "item.Value.Content was null in MyCharacter.SwitchToWeapon");
				if (item.Value.Content == null)
				{
					MySandboxGame.Log.WriteLine("item.Value.Content was null in MyCharacter.SwitchToWeapon");
					MySandboxGame.Log.WriteLine("item.Value = " + item.Value);
					MySandboxGame.Log.WriteLine("weaponDefinition.Value = " + weaponDefinition);
					return;
				}

				var physicalGunObject = item.Value.Content as MyObjectBuilder_PhysicalGunObject;
				var gunEntity = physicalGunObject.GunEntity;
				if (gunEntity == null)
				{
					var handItemId = MyDefinitionManager.Static.TryGetHandItemForPhysicalItem(physicalGunObject.GetId()).Id;
					gunEntity = (MyObjectBuilder_EntityBase)MyObjectBuilderSerializer.CreateNewObject(handItemId);
				}
				else
				{
					gunEntity.EntityId = 0;
				}

                SwitchToWeaponInternal(weaponDefinition, sync, true, gunEntity, 0);
			}
			else
			{
				SwitchToWeaponInternal(weaponDefinition, sync, true, null, 0);
			}
		}

        void SwitchToWeaponInternal(MyDefinitionId? weaponDefinition, bool updateSync, bool checkInventory, MyObjectBuilder_EntityBase gunBuilder, long weaponEntityId)
        {
            if (updateSync)
            {
                //Because while waiting for answer we dont want to shoot from old weapon
                UnequipWeapon();

                //We want have same ID on all clients
                long weaponEntityIdSync = weaponEntityId;
                if (weaponDefinition != null && weaponEntityIdSync == 0)
                {
                    weaponEntityIdSync = MyEntityIdentifier.AllocateId(MyEntityIdentifier.ID_OBJECT_TYPE.ENTITY);
                }

                SyncObject.RequestSwitchToWeapon(weaponDefinition, gunBuilder, weaponEntityIdSync);
                return;
            }

            UnequipWeapon();

            StopCurrentWeaponShooting();

            MyObjectBuilder_EntityBase weaponEntityBuilder = gunBuilder;
            UseAnimationForWeapon = MyPerGameSettings.UseAnimationInsteadOfIK;

            if (weaponDefinition.HasValue)
            {
                if (checkInventory)
                {
                    var item = Inventory.FindItem(weaponDefinition.Value);
                    if (item.HasValue)
                    {
                        var physicalGunObject = item.Value.Content as MyObjectBuilder_PhysicalGunObject;
                        physicalGunObject.GunEntity = gunBuilder;
                        var gun = CreateGun(gunBuilder);
                        weaponEntityBuilder = gun.PhysicalObject.GunEntity;
                        EquipWeapon(gun);
                    }
                }
                else
                {
                    if (!WeaponTakesBuilderFromInventory(weaponDefinition) && weaponEntityBuilder == null && weaponDefinition.Value.TypeId == typeof(MyObjectBuilder_PhysicalGunObject))
                    {
                        var handItemDef = MyDefinitionManager.Static.TryGetHandItemForPhysicalItem(weaponDefinition.Value);
                        if (handItemDef != null)
                        {
                            weaponEntityBuilder = (MyObjectBuilder_EntityBase)MyObjectBuilderSerializer.CreateNewObject(handItemDef.Id);
                            weaponEntityBuilder.EntityId = weaponEntityId;
                        }
                    }
                    else
                    {
                        if (weaponEntityBuilder == null)
                            weaponEntityBuilder = MyObjectBuilderSerializer.CreateNewObject(weaponDefinition.Value.TypeId) as MyObjectBuilder_EntityBase;
                        if (weaponEntityBuilder != null)
                        {
                            weaponEntityBuilder.EntityId = weaponEntityId;
                            if (WeaponTakesBuilderFromInventory(weaponDefinition))
                            {
                                var item = Inventory.FindItem(weaponDefinition.Value);
                                if (item.HasValue)
	                            {
		                            var physicalGunBuilder = item.Value.Content as MyObjectBuilder_PhysicalGunObject;
									if(physicalGunBuilder != null)
										physicalGunBuilder.GunEntity = gunBuilder;
                            }
                        }
                        }
                        else
                        {
                            Debug.Fail("Couldn't create builder for weapon! typeID: " + weaponDefinition.Value.TypeId.ToString());
                        }
                    }

                    if (weaponEntityBuilder != null)
                    {
                        var gun = CreateGun(weaponEntityBuilder);
                        EquipWeapon(gun);
                    }
                }
            }

            UpdateShadowIgnoredObjects();
        }

        private void StopCurrentWeaponShooting()
        {
            if (m_currentWeapon != null)
            {
                foreach (MyShootActionEnum action in MyEnum<MyShootActionEnum>.Values)
                {
                    if (SyncObject.IsShooting(action))
                    {
                        m_currentWeapon.EndShoot(action);
                    }
                }
            }
        }

        void UpdateShadowIgnoredObjects()
        {
            Render.UpdateShadowIgnoredObjects();
            if (m_currentWeapon != null)
                UpdateShadowIgnoredObjects((MyEntity)m_currentWeapon);
            if (m_leftHandItem != null)
                UpdateShadowIgnoredObjects(m_leftHandItem);
        }

        void UpdateShadowIgnoredObjects(IMyEntity parent)
        {
            Render.UpdateShadowIgnoredObjects(parent);
            foreach (var child in parent.Hierarchy.Children)
            {
                UpdateShadowIgnoredObjects(child.Container.Entity);
            }
        }

        public void Use()
        {
            if (m_currentMovementState != MyCharacterMovementEnum.Died)
            {
                MyCharacterDetectorComponent detectorComponent = Components.Get<MyCharacterDetectorComponent>();

                if (detectorComponent != null && detectorComponent.UseObject != null && detectorComponent.UseObject.IsActionSupported(UseActionEnum.Manipulate))
                {
                    if (detectorComponent.UseObject.PlayIndicatorSound)
                    {
                        MyGuiAudio.PlaySound(MyGuiSounds.HudUse);

                        // stop jetpack loop sound, if playing
						SoundComp.StopStateSound(true);
                    }

                    detectorComponent.UseObject.Use(UseActionEnum.Manipulate, this);
                }
                else
                {
                    IMyUseObject useObject = CurrentWeapon as IMyUseObject;

                    if (MyFakes.ENABLE_GATHERING && useObject == null && detectorComponent != null && detectorComponent.DetectedEntity != null) 
                    {
                        var inventoryAggregate = Components.Get<MyInventoryBase>() as MyInventoryAggregate;

                        if (inventoryAggregate == null)
                        {
                            return;
                        }
                        var inventory = inventoryAggregate.GetInventory(MyStringId.Get("Inventory")) as MyInventory;

                        if (inventory != null)
                        {
                            inventory.AddEntity(detectorComponent.DetectedEntity);
                        }
                    }
                    // TODO: When this is tested, remove the MyFake and enable this behaviour by default
                    else if (MyFakes.ENABLE_WEAPON_USE && useObject != null)
                    {
                        useObject.Use(UseActionEnum.Manipulate, this);
                    }
                }
            }
        }

        public void UseContinues()
        {
            MyCharacterDetectorComponent detectorComponent = Components.Get<MyCharacterDetectorComponent>();

            if (detectorComponent != null && detectorComponent.UseObject != null && detectorComponent.UseObject.IsActionSupported(UseActionEnum.Manipulate) && detectorComponent.UseObject.ContinuousUsage)
            {
                detectorComponent.UseObject.Use(UseActionEnum.Manipulate, this);
                
            }
        }

        public void UseTerminal()
        {
            MyCharacterDetectorComponent detectorComponent = Components.Get<MyCharacterDetectorComponent>();

            if (detectorComponent.UseObject != null && detectorComponent.UseObject.IsActionSupported(UseActionEnum.OpenTerminal))
            {
                detectorComponent.UseObject.Use(UseActionEnum.OpenTerminal, this);
                detectorComponent.UseContinues();
            }
        }

        public void UseFinished()
        {
            MyCharacterDetectorComponent detectorComponent = Components.Get<MyCharacterDetectorComponent>();

            if (detectorComponent.UseObject != null && detectorComponent.UseObject.IsActionSupported(UseActionEnum.UseFinished))
            {
                detectorComponent.UseObject.Use(UseActionEnum.UseFinished, this);
            }
        }
   
        public void Crouch()
        {
	        if (m_currentMovementState == MyCharacterMovementEnum.Died)
				return;

	        if ((JetpackComp == null || !JetpackComp.Running) && !m_isFalling)
            {
                    WantsCrouch = !WantsCrouch;
                }
            }

        public void Down()
        {
            if (WantsFlyUp)
            {
                WantsFlyDown = false;
                WantsFlyUp = false;
            }
            else
                WantsFlyDown = true;
        }

        public void Up()
        {
            if (WantsFlyDown)
            {
                WantsFlyUp = false;
                WantsFlyDown = false;
            }
            else
                WantsFlyUp = true;
        }

        public void Sprint()
        {
            WantsSprint = true;
        }

        public void SwitchWalk()
        {
            WantsWalk = !WantsWalk;
        }

        public void Jump()
        {
			if (m_currentMovementState != MyCharacterMovementEnum.Died && StatComp.CanDoAction("Jump"))
            {
                WantsJump = true;
            }
        }

        public void ShowInventory()
        {
            if (m_currentMovementState != MyCharacterMovementEnum.Died)
            {
                MyCharacterDetectorComponent detectorComponent = Components.Get<MyCharacterDetectorComponent>();

                if (detectorComponent.UseObject != null && detectorComponent.UseObject.IsActionSupported(UseActionEnum.OpenInventory))
                {
                    detectorComponent.UseObject.Use(UseActionEnum.OpenInventory, this);
                }
                else if (MyPerGameSettings.TerminalEnabled)
                {
                    MyGuiScreenTerminal.Show(MyTerminalPageEnum.Inventory, this, null);
                }
                else
                {
                    ShowAggregateInventoryScreen();
                }
            }
        }

        public MyGuiScreenBase ShowAggregateInventoryScreen(MyInventoryBase rightSelectedInventory = null)
        {           
            if (MyPerGameSettings.GUI.InventoryScreen != null)
            {
                if (InventoryAggregate != null)
                {
                    InventoryAggregate.Init();
                    m_InventoryScreen = MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.InventoryScreen, InventoryAggregate, rightSelectedInventory);
                    MyGuiSandbox.AddScreen(m_InventoryScreen);
                    m_InventoryScreen.Closed += (scr) => { if (InventoryAggregate != null) { InventoryAggregate.DetachCallbacks(); } m_InventoryScreen = null; };
                }
            }
            return m_InventoryScreen;
        }

        public void ShowTerminal()
        {
            if (m_currentMovementState != MyCharacterMovementEnum.Died)
            {
                MyCharacterDetectorComponent detectorComponent = Components.Get<MyCharacterDetectorComponent>();

                if (detectorComponent.UseObject != null && detectorComponent.UseObject.IsActionSupported(UseActionEnum.OpenTerminal))
                    detectorComponent.UseObject.Use(UseActionEnum.OpenTerminal, this);
                else if (MyPerGameSettings.TerminalEnabled)
                    MyGuiScreenTerminal.Show(MyTerminalPageEnum.Inventory, this, null);
                else if (MyPerGameSettings.GUI.GameplayOptionsScreen != null)
                {
                    if (!MySession.Static.SurvivalMode || (MyMultiplayer.Static != null && MyMultiplayer.Static.IsAdmin(ControllerInfo.Controller.Player.Id.SteamId)))
                    {
                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.GameplayOptionsScreen));
                    }
                }
            }
        }

        public void SwitchLights()
        {
            if (m_currentMovementState != MyCharacterMovementEnum.Died)
            {
                EnableLights(!LightEnabled);
                RecalculatePowerRequirement();
            }
        }

        public void SwitchReactors()
        {
        }

        public void SwitchBroadcasting()
        {
            if (m_currentMovementState != MyCharacterMovementEnum.Died)
            {
                EnableBroadcasting(!m_radioBroadcaster.WantsToBeEnabled);

                m_broadcastingNotification.Text = (m_radioBroadcaster.Enabled ? MySpaceTexts.NotificationCharacterBroadcastingOn : MySpaceTexts.NotificationCharacterBroadcastingOff);
                MyHud.Notifications.Add(m_broadcastingNotification);
            }
        }

        public override void OnRemovedFromScene(object source)
        {
            base.OnRemovedFromScene(source);
            if (m_currentWeapon != null)
                ((MyEntity)m_currentWeapon).OnRemovedFromScene(source);
            if (m_leftHandItem != null)
                m_leftHandItem.OnRemovedFromScene(source);
        }

        #endregion

        #region Sensor

        public void RemoveNotification(ref MyHudNotification notification)
        {
            if (notification != null)
            {
                MyHud.Notifications.Remove(notification);
                notification = null;
            }
        }

        void RemoveNotifications()
        {
            RemoveNotification(ref m_pickupObjectNotification);
            RemoveNotification(ref m_respawnNotification);
        }


        internal void OnControlAcquired(MyEntityController controller)
        {
            if (controller.Player.IsLocalPlayer)
            {
                bool isHuman = controller.Player == MySession.LocalHumanPlayer;
                if (isHuman)
                {
                    MyHud.HideAll();
                    MyHud.Crosshair.Show(null);
                    MyHud.Crosshair.Position = MyHudCrosshair.ScreenCenter;

                    if (MyGuiScreenGamePlay.Static != null)
                        MySession.Static.CameraAttachedToChanged += Static_CameraAttachedToChanged;

                    if (MySession.Static.CameraController is MyEntity)
                        MySession.SetCameraController(MyCameraControllerEnum.Entity, this);

                    m_cameraShake = new MyCameraHeadShake();
                    m_cameraSpring = new MyCameraSpring(this.Physics);

                    MyHud.GravityIndicator.Entity = this;
                    MyHud.GravityIndicator.Show(null);
                    MyHud.CharacterInfo.Show(null);
                    MyHud.OreMarkers.Visible = true;
                    MyHud.LargeTurretTargets.Visible = true;
                    if (MySession.Static.IsScenario)
                        MyHud.ScenarioInfo.Show(null);
                }

                //Enable features for local player
	            var jetpack = JetpackComp;
				if(jetpack != null)
					jetpack.TurnOnJetpack(jetpack.TurnedOn);

                m_suitBattery.OwnedByLocalPlayer = true;
                DisplayName = controller.Player.Identity.DisplayName;
            }
            else
            {
                DisplayName = controller.Player.Identity.DisplayName;
                UpdateHudMarker();
            }

            if (StatComp != null && StatComp.Health != null && StatComp.Health.Value <= 0.0f)
            {
                m_dieAfterSimulation = true;
                return;
            }

            if (m_currentWeapon != null)
                m_currentWeapon.OnControlAcquired(this);

            UpdateCharacterPhysics(controller.Player.IsLocalPlayer);

            // Note: This code was part of the init, this event got registered for all characters, and never unregistered..            
            if (this == MySession.ControlledEntity) 
            {
                MyToolbarComponent.CharacterToolbar.ItemChanged -= Toolbar_ItemChanged;  // OM: The Init or this can be called on one instance several times (changing color etc.). We need to unregister first, otherwise we get this event registered even more than 11 times for one instance..
                MyToolbarComponent.CharacterToolbar.ItemChanged += Toolbar_ItemChanged;
        }
        }

        private void UpdateHudMarker()
        {
            if (!MyFakes.ENABLE_RADIO_HUD)
            {
                MyHud.LocationMarkers.RegisterMarker(this, new MyHudEntityParams()
                {
                    FlagsEnum = MyHudIndicatorFlagsEnum.SHOW_TEXT,
                    Text = new StringBuilder(ControllerInfo.Controller.Player.Identity.DisplayName),
                    ShouldDraw = MyHud.CheckShowPlayerNamesOnHud,
                    MustBeDirectlyVisible = true,
                });
            }
        }

        public string GetFactionTag()
        {
            if (ControllerInfo.Controller == null)
            {
                if (CurrentRemoteControl == null || CurrentRemoteControl.ControllerInfo.Controller == null)
                {
                    return "";
                }
                else
                {
                    return GetFactionTag(CurrentRemoteControl.ControllerInfo.ControllingIdentityId);
                }
            }

            return GetFactionTag(ControllerInfo.ControllingIdentityId);
        }

        private string GetFactionTag(long playerId)
        {
            IMyFaction faction = MySession.Static.Factions.TryGetPlayerFaction(playerId);
            if (faction == null)
                return "";

            return faction.Tag;
        }

        public StringBuilder UpdateCustomNameWithFaction()
        {
            CustomNameWithFaction.Clear();

            if (!string.IsNullOrEmpty(GetFactionTag()))
            {
                CustomNameWithFaction.Append(GetFactionTag());
                CustomNameWithFaction.Append(".");
            }

            CustomNameWithFaction.Append(ControllerInfo.Controller != null ? ControllerInfo.Controller.Player.Identity.DisplayName : DisplayName);

            return CustomNameWithFaction;
        }

	    internal void ClearShapeContactPoints()
	    {
		    m_shapeContactPoints.Clear();
	    }

        public override List<MyHudEntityParams> GetHudParams(bool allowBlink)
        {
            UpdateCustomNameWithFaction();

            m_hudParams.Clear();

            if (MySession.LocalHumanPlayer == null) return m_hudParams;

            m_hudParams.Add(new MyHudEntityParams()
            {
                FlagsEnum = MyHudIndicatorFlagsEnum.SHOW_TEXT,
                Text = CustomNameWithFaction,
                ShouldDraw = MyHud.CheckShowPlayerNamesOnHud,
                MustBeDirectlyVisible = false,
                TargetMode = GetRelationTo(MySession.LocalHumanPlayer.Identity.IdentityId),
                Entity = this
            });
            return m_hudParams;
        }

        internal void OnControlReleased(MyEntityController controller)
        {
            Static_CameraAttachedToChanged(null, null);
            m_oldController = controller;

            if (MySession.LocalHumanPlayer == controller.Player)
            {
                MyHud.SelectedObjectHighlight.Visible = false;

                RemoveNotifications();

                if (MyGuiScreenGamePlay.Static != null)
                    MySession.Static.CameraAttachedToChanged -= Static_CameraAttachedToChanged;

                m_cameraShake = null;
                m_cameraSpring = null;

                MyHud.GravityIndicator.Hide();
                MyHud.CharacterInfo.Hide();
                m_suitBattery.OwnedByLocalPlayer = false;
                MyHud.LargeTurretTargets.Visible = false;
                MyHud.OreMarkers.Visible = false;
                m_radioReceiver.Clear();
				if (MyGuiScreenGamePlay.ActiveGameplayScreen != null)
					MyGuiScreenGamePlay.ActiveGameplayScreen.CloseScreen();

                ResetMovement();
            }
            else
            {
                if (!MyFakes.ENABLE_RADIO_HUD)
                {
                    MyHud.LocationMarkers.UnregisterMarker(this);
                }
            }

			SoundComp.StopStateSound(true);

            // Note: This event was currently registered in init and never unregistered, when the character control is released, we unregister from the event handler
            {
                MyToolbarComponent.CharacterToolbar.ItemChanged -= Toolbar_ItemChanged;  // OM: The Init can be called on one instance several times (changing color etc.). We need to unregister first, otherwise we get this event registered even more than 11 times for one instance..
        }
        }

        void Static_CameraAttachedToChanged(IMyCameraController oldController, IMyCameraController newController)
        {
            if (oldController != newController && MySession.ControlledEntity == this && newController != this)
            {
                ResetMovement();
                EndShootAll();
            }

            UpdateNearFlag();

            if (!Render.NearFlag && MySector.MainCamera != null) // During unload, camera is null?
            //MySector.MainCamera.Zoom.ResetZoom();
            {
                if (m_zoomMode == MyZoomModeEnum.IronSight)
                    EnableIronsight(false, true, false);
            }
            else
            {
                if (oldController != newController)
                {
                    ResetHeadRotation();
                }
            }
        }

        public void OnAssumeControl(IMyCameraController previousCameraController)
        {
            if (Parent is MyCockpit)
            {
                var cockpit = Parent as MyCockpit;
                if (cockpit.Pilot == this)
                {
                    MySession.SetCameraController(MyCameraControllerEnum.Entity, cockpit);
                }

                return;
            }
        }

        public void OnReleaseControl(IMyCameraController newCameraController)
        {
        }

        private void ResetHeadRotation()
        {
            if (m_actualUpdateFrame > 0)
            {
                m_headLocalYAngle = 0;
                m_headLocalXAngle = 0;
            }
        }

        private void UpdateNearFlag()
        {
            bool nearFlag = ControllerInfo.IsLocallyControlled() && m_isInFirstPerson;

            if (m_currentWeapon != null)
            {
                ((MyEntity)m_currentWeapon).Render.NearFlag = nearFlag;
            }
            if (m_leftHandItem != null)
                m_leftHandItem.Render.NearFlag = nearFlag;

            Render.NearFlag = nearFlag;

            m_bobQueue.Clear();
        }

        private Vector3D m_lastProceduralGeneratorPosition = Vector3D.PositiveInfinity;

        private void WorldPositionChanged(object source)
        {
            //if (BoneTransforms != null)
              //  CalculateDependentMatrices();

            if (m_radioBroadcaster != null)
                m_radioBroadcaster.MoveBroadcaster();

            Render.UpdateLightPosition();
        }

        void OnCharacterStateChanged(HkCharacterStateType newState)
        {
            if (m_currentMovementState != MyCharacterMovementEnum.Died)
            {
	            var jetpack = JetpackComp;
	            bool canFly = jetpack != null && jetpack.Running;
                if (!canFly)
                {
                    if (m_currentJump == 0 && (newState == HkCharacterStateType.HK_CHARACTER_IN_AIR) || ((int)newState == 5))
                    {
                        StartFalling();
                    }
                    else
                    {
                        if (m_isFalling)
                        {
                            StopFalling();
                        }
                    }
                }
            }

            m_currentCharacterState = newState;

            //MyTrace.Watch("CharacterState", newState.ToString());
        }

        internal void StartFalling()
        {
			var jetpack = JetpackComp;
			bool canFly = jetpack != null && jetpack.Running;
	        if (canFly || m_currentMovementState == MyCharacterMovementEnum.Died || m_currentMovementState == MyCharacterMovementEnum.Sitting)
				return;

                if (m_currentCharacterState == HkCharacterStateType.HK_CHARACTER_JUMPING)
                {
		        m_currentFallingTime = -JUMP_TIME;
                }
                else
                    m_currentFallingTime = 0;

                m_isFalling = true;
                m_crouchAfterFall = WantsCrouch;
                WantsCrouch = false;

                SetCurrentMovementState(MyCharacterMovementEnum.Falling);
            }

        internal void StopFalling()
        {
            if (m_currentMovementState == MyCharacterMovementEnum.Died)
                return;

	        var jetpack = JetpackComp;
            if(m_isFalling && m_previousMovementState != MyCharacterMovementEnum.Flying && (jetpack == null || !(jetpack.TurnedOn && jetpack.IsPowered)))
                SoundComp.PlayFallSound();

            if (Physics.CharacterProxy != null)
            {
                if (m_isFalling)
                {
                    m_movementsFlagsChanged = true;
                    //PlayCharacterAnimation("Idle", MyBlendOption.Immediate, MyFrameOption.Loop, 0.2f);
                    //Physics.CharacterProxy.PosX = 0;
                    //Physics.CharacterProxy.PosY = 0;
                    //SetCurrentMovementState(MyCharacterMovementEnum.Standing);
                }
            }

            m_isFalling = false;
            m_isFallingAnimationPlayed = false;
            m_currentFallingTime = 0;
            m_canJump = true;
            WantsCrouch = m_crouchAfterFall;
            m_crouchAfterFall = false;
        }

        #endregion

        #region Inventory

        public int InventoryCount { get { return 1; } }

        String IMyInventoryOwner.DisplayNameText
        {
            get { return DisplayNameText.ToString(); }
        }

        public MyInventory GetInventory(int index = 0)
        {
            Debug.Assert(index == 0);
            return Inventory;
        }

        public void SetInventory(MyInventory inventory, int index)
        {
            Inventory = inventory;
        }

        public MyInventoryOwnerTypeEnum InventoryOwnerType
        {
            get { return MyInventoryOwnerTypeEnum.Character; }
        }

        public bool CanStartConstruction(MyCubeBlockDefinition blockDefinition)
        {
            if (blockDefinition == null) return false;

            Debug.Assert(Inventory != null, "Inventory is null!");
            Debug.Assert(blockDefinition.Components.Length != 0, "Missing components!");

			var inventory = MyCubeBuilder.BuildComponent.GetBuilderInventory(this);
			if(inventory == null)
				return false;

			return (inventory.GetItemAmount(blockDefinition.Components[0].Definition.Id) >= 1);
        }

        public bool CanStartConstruction(Dictionary<MyDefinitionId, int> constructionCost)
        {
            Debug.Assert(Inventory != null, "Inventory is null!");
            var inventory = MyCubeBuilder.BuildComponent.GetBuilderInventory(this);
            foreach (var entry in constructionCost)
            {
				if (inventory.GetItemAmount(entry.Key) < entry.Value) return false;
            }
            return true;
        }

        bool IMyInventoryOwner.UseConveyorSystem
        {
            get
            {
                return false;
            }
            set
            {
                throw new NotImplementedException();
            }
        }


        Sandbox.ModAPI.Interfaces.IMyInventory Sandbox.ModAPI.Interfaces.IMyInventoryOwner.GetInventory(int index)
        {
            return GetInventory(index);
        }

        bool ModAPI.Interfaces.IMyInventoryOwner.UseConveyorSystem
        {
            get
            {
                return (this as IMyInventoryOwner).UseConveyorSystem;
            }
            set
            {
                (this as IMyInventoryOwner).UseConveyorSystem = value;
            }
        }
        #endregion

        #region Interactive

        public MyEntity IsUsing
        {
            get
            {
                return m_usingEntity as MyEntity;
            }
            set
            {
                m_usingEntity = value;
            }
        }


        private void UnequipWeapon()
        {
            if (m_leftHandItem != null)
            {
                (m_leftHandItem as IMyHandheldGunObject<MyDeviceBase>).OnControlReleased();
                m_leftHandItem.Close(); // no dual wielding now
                m_leftHandItem = null;
            }

            if (m_currentWeapon != null)
            {
				var weaponEntity = m_currentWeapon as MyEntity;
                // save weapon ammo amount in builder
                if (m_currentWeapon.PhysicalObject != null)
                {
                    var inventory = Inventory;
                    if (inventory != null)
                    {
                        var item = inventory.FindItem(m_currentWeapon.PhysicalObject.GetId());
                        if (item.HasValue && (item.Value.Content is MyObjectBuilder_PhysicalGunObject))
                        {
							(item.Value.Content as MyObjectBuilder_PhysicalGunObject).GunEntity = weaponEntity.GetObjectBuilder();
                        }
                    }
                }

                m_currentWeapon.OnControlReleased();
	            
				var weaponSink = weaponEntity.Components.Get<MyResourceSinkComponent>();
				if (weaponSink != null)
					SuitRechargeDistributor.RemoveSink(weaponSink);

				weaponEntity.OnClose -= gunEntity_OnClose;

				MyEntities.Remove(weaponEntity);

				weaponEntity.Close();
                m_currentWeapon = null;

                if (ControllerInfo.IsLocallyHumanControlled() && MySector.MainCamera != null)
                {
                    MySector.MainCamera.Zoom.ResetZoom();
                }

                if (MyPerGameSettings.UseAnimationInsteadOfIK)
                {
                    StopUpperAnimation(0.2f);
                    SwitchAnimation(GetCurrentMovementState(), false);
                }
            }

            if (m_currentShotTime <= 0)
            {
                // StopUpperCharacterAnimation(0.2f);
            }

            //MyHud.Crosshair.Hide();

            m_currentWeapon = null;
            StopFingersAnimation(0);
        }

        private void EquipWeapon(IMyHandheldGunObject<MyDeviceBase> newWeapon, bool showNotification = false)
        {
            Debug.Assert(newWeapon != null);
            if (newWeapon == null)
                return;

            MyEntity gunEntity = (MyEntity)newWeapon;
            gunEntity.Render.CastShadows = true;
            gunEntity.Render.NeedsResolveCastShadow = false;
            gunEntity.Save = false;
            gunEntity.OnClose += gunEntity_OnClose;

            MyEntities.Add(gunEntity);

            m_handItemDefinition = null;
            m_currentWeapon = newWeapon;
            m_currentWeapon.OnControlAcquired(this);

            if (WeaponEquiped != null)
                WeaponEquiped(m_currentWeapon);

            MyAnalyticsHelper.ReportActivityEnd(this, "item_equip");
            MyAnalyticsHelper.ReportActivityStart(this, "item_equip", "character", "toolbar_item_usage", m_currentWeapon.GetType().Name);

            // CH:TODO: The hand item definitions should be changed to handheld gun object definitions and should be taken according to m_currentWeapon typeId
            if (m_currentWeapon.PhysicalObject != null)
            {
                var handItemId = m_currentWeapon.PhysicalObject.GetId();

                m_handItemDefinition = MyDefinitionManager.Static.TryGetHandItemForPhysicalItem(handItemId);
                System.Diagnostics.Debug.Assert(m_handItemDefinition != null, "Create definition for this hand item!");
            }
            else if (m_currentWeapon.DefinitionId.TypeId == typeof(MyObjectBuilder_CubePlacer))
            {
                var gunID = new MyDefinitionId(typeof(MyObjectBuilder_CubePlacer));

                m_handItemDefinition = MyDefinitionManager.Static.TryGetHandItemDefinition(ref gunID);
                System.Diagnostics.Debug.Assert(m_handItemDefinition != null, "Create definition for this hand item!");
            }

            //Setup correct worldmatrix to weapon
            //CalculateDependentMatrices();

            if (m_handItemDefinition != null && !string.IsNullOrEmpty(m_handItemDefinition.FingersAnimation))
            {
                string animationSubtype;
                if (!m_characterDefinition.AnimationNameToSubtypeName.TryGetValue(m_handItemDefinition.FingersAnimation, out animationSubtype))
                {
                    animationSubtype = m_handItemDefinition.FingersAnimation;
                }
                var def = MyDefinitionManager.Static.TryGetAnimationDefinition(animationSubtype);
                if (!def.LeftHandItem.TypeId.IsNull)
                {
                    m_currentWeapon.OnControlReleased();
                    (m_currentWeapon as MyEntity).Close(); //no dual wielding now
                    m_currentWeapon = null;                    
                }
                                    
                PlayCharacterAnimation(m_handItemDefinition.FingersAnimation, MyBlendOption.Immediate, def.Loop ? MyFrameOption.Loop : MyFrameOption.PlayOnce, 1.0f, 1, false, null);                
            }
            else
            {
                StopFingersAnimation(0);
            }

			var consumer = gunEntity.Components.Get<MyResourceSinkComponent>();
            if (consumer != null && SuitRechargeDistributor != null)
				SuitRechargeDistributor.AddSink(consumer);

            if (showNotification)
            {
                var notificationUse = new MyHudNotification(MySpaceTexts.NotificationUsingWeaponType, 2000);
                notificationUse.SetTextFormatArguments(MyDeviceBase.GetGunNotificationName(newWeapon.DefinitionId));
                MyHud.Notifications.Add(notificationUse);
            }

            Static_CameraAttachedToChanged(null, null);
            MyHud.Crosshair.Show(null);

        }

        void gunEntity_OnClose(MyEntity obj)
        {
            if (m_currentWeapon == obj)
                m_currentWeapon = null;
        }

        public float InteractiveDistance
        {
            get { return MyConstants.DEFAULT_INTERACTIVE_DISTANCE; }
        }

        #endregion

        #region Power consumer

        private void SetPowerInput(float input)
        {
            if (LightEnabled && input >= MyEnergyConstants.REQUIRED_INPUT_CHARACTER_LIGHT)
            {
                m_lightPowerFromProducer = MyEnergyConstants.REQUIRED_INPUT_CHARACTER_LIGHT;
                input -= MyEnergyConstants.REQUIRED_INPUT_CHARACTER_LIGHT;
            }
            else
                m_lightPowerFromProducer = 0;
            }

        private float ComputeRequiredPower()
        {
            float result = MyEnergyConstants.REQUIRED_INPUT_LIFE_SUPPORT;
            if (Definition != null && Definition.NeedsOxygen)
            {
                result = MyEnergyConstants.REQUIRED_INPUT_LIFE_SUPPORT_WITHOUT_HELMET;
            }
            if (m_lightEnabled)
                result += MyEnergyConstants.REQUIRED_INPUT_CHARACTER_LIGHT;
            
            return result;
        }

        internal void RecalculatePowerRequirement(bool chargeImmediatelly = false)
        {
            SinkComp.Update();
            UpdateLightPower(chargeImmediatelly);
        }

        public bool LightEnabled
        {
            get { return m_lightEnabled; }
        }

        public void EnableLights(bool enable, bool updateSync = true)
        {
            if (m_lightEnabled != enable)
            {
                m_lightEnabled = enable;
                if (updateSync)
                    SendFlags();
            }

            RecalculatePowerRequirement();
            Render.UpdateLightPosition();
        }

        public void EnableBroadcasting(bool enable, bool updateSync = true)
        {
            if (m_radioBroadcaster.WantsToBeEnabled != enable)
            {
                m_radioBroadcaster.WantsToBeEnabled = enable;
                m_radioBroadcaster.Enabled = enable;
                if (updateSync)
                {
                    SendFlags();
                }
            }
        }

        public bool IsCrouching
        {
            get { return m_currentMovementState.GetMode() == MyCharacterMovement.Crouching; }
        }

        public bool IsSprinting
        {
            get { return m_currentMovementState == MyCharacterMovementEnum.Sprinting; }
        }

        public bool IsFalling
        {
            get { return m_isFalling; }
        }

        public bool IsJumping
        {
            get { return m_currentMovementState == MyCharacterMovementEnum.Jump; }
        }


        public void Sit(bool enableFirstPerson, bool playerIsPilot, bool enableBag, string animation)
        {
            EndShootAll();

            SwitchToWeaponInternal(weaponDefinition: null, updateSync: false, checkInventory: false, gunBuilder: null, weaponEntityId: 0);

            Render.NearFlag = enableFirstPerson && playerIsPilot;
            m_isFalling = false;

            PlayCharacterAnimation(animation, MyBlendOption.Immediate, MyFrameOption.Loop, 0);

            StopUpperCharacterAnimation(0);
            StopFingersAnimation(0);

            SetHandAdditionalRotation(Quaternion.CreateFromAxisAngle(Vector3.Forward, MathHelper.ToRadians(0)));
            SetUpperHandAdditionalRotation(Quaternion.CreateFromAxisAngle(Vector3.Forward, MathHelper.ToRadians(0)));
            SetSpineAdditionalRotation(Quaternion.CreateFromAxisAngle(Vector3.Forward, 0), Quaternion.CreateFromAxisAngle(Vector3.Forward, 0));
            SetHeadAdditionalRotation(Quaternion.Identity, false);

            FlushAnimationQueue();

            UpdateAnimation(0);

           // SuitBattery.ResourceSource.Enabled = false;
            SinkComp.Update();
            UpdateLightPower(true);

            EnableBag(enableBag);
            //EnableHead(true);

            SetCurrentMovementState(MyCharacterMovementEnum.Sitting);

            //Because of legs visible first frame after sitting
            if(!MySandboxGame.IsDedicated)
                Render.Draw();
        }

        void EnableBag(bool enabled)
        {
            m_enableBag = enabled;
            if (InScene)
            {
                VRageRender.MyRenderProxy.UpdateModelProperties(
                Render.RenderObjectIDs[0],
                0,
                null,
                -1,
                "Bag",
                enabled,
                null,
                null,
                null,
                null);
            }
        }

        public void EnableHead(bool enabled)
        {
            if (InScene && m_characterDefinition.MaterialsDisabledIn1st != null)
            {
                foreach (var material in m_characterDefinition.MaterialsDisabledIn1st)
                {
                    VRageRender.MyRenderProxy.UpdateModelProperties(
                      Render.RenderObjectIDs[0],
                     0,
                     null,
                     -1,
                     material,
                     enabled,
                     null,
                     null,
                     null,
                     null);
                }
            }
        }

        public void Stand()
        {
            PlayCharacterAnimation("Idle", MyBlendOption.Immediate, MyFrameOption.Loop, 0);

            Render.NearFlag = false;

            StopUpperCharacterAnimation(0);

            //SuitBattery.ResourceSource.Enabled = true;
            RecalculatePowerRequirement();

            EnableBag(true);
            EnableHead(true);

            SetCurrentMovementState(MyCharacterMovementEnum.Standing);
            m_wasInFirstPerson = false;
            IsUsing = null;
            //NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

	    private void explosionEffect_OnUpdate(object sender, EventArgs e)
        {
            MyParticleEffect effect = sender as MyParticleEffect;
		    if (effect == null || effect.GetElapsedTime() <= 0.2f)
			    return;

                effect.OnUpdate -= explosionEffect_OnUpdate;
                effect.Stop();
            }

		public void ForceUpdateBreath()
		{
			if (m_breath != null)
				m_breath.ForceUpdate();
		}

		public void DoDamage(float damage, MyStringHash damageType, bool updateSync, long attackerId = 0)
		{
			if ((!CharacterCanDie && !(damageType == MyDamageType.Suicide && MyPerGameSettings.CharacterSuicideEnabled)) || StatComp == null)
				return;
            
            MyEntity attacker;
            if (MyEntities.TryGetEntityById(attackerId, out attacker))
            {   // Checking friendly fire using faction's 
                              
                var localPlayer = MyPlayer.GetPlayerFromCharacter(this);
                MyPlayer otherPlayer = null;
                if (attacker is MyCharacter)
                {
                    otherPlayer = MyPlayer.GetPlayerFromCharacter(attacker as MyCharacter);
                }
                else if (attacker is IMyGunBaseUser)
                {
                    otherPlayer = MyPlayer.GetPlayerFromWeapon(attacker as IMyGunBaseUser);
                }
                if (localPlayer != null && otherPlayer != null)
                {
                    var localPlayerFaction = MySession.Static.Factions.TryGetPlayerFaction(localPlayer.Identity.IdentityId) as MyFaction;
                    if (localPlayerFaction != null && !localPlayerFaction.EnableFriendlyFire && localPlayerFaction.IsMember(otherPlayer.Identity.IdentityId))
                    {
                        return; // No Friendly Fire Enabled!
                    }
                }
            }            

            MyDamageInformation damageInfo = new MyDamageInformation(false, damage, damageType, attackerId);
            if (UseDamageSystem && !(m_dieAfterSimulation || IsDead))
                MyDamageSystem.Static.RaiseBeforeDamageApplied(this, ref damageInfo);

            if (damageInfo.Amount <= 0f)
                return;

			StatComp.DoDamage(damage, updateSync, damageInfo);

            // Cache the last damage information for the analytics module.
		    MyAnalyticsHelper.SetLastDamageInformation(damageInfo);

            if (UseDamageSystem)
                MyDamageSystem.Static.RaiseAfterDamageApplied(this, damageInfo);
		}

		void Sandbox.ModAPI.IMyCharacter.Kill(object statChangeData)
		{
            MyDamageInformation damageInfo = new MyDamageInformation();
            if (statChangeData != null)
                damageInfo = (MyDamageInformation)statChangeData;

            Kill(true, damageInfo);
		}

		public void Kill(bool sync, MyDamageInformation damageInfo)
		{
			if (m_dieAfterSimulation || IsDead || (MyFakes.DEVELOPMENT_PRESET && damageInfo.Type != MyDamageType.Suicide))
				return;

			if(sync)
			{
				MySyncHelper.KillCharacter(this, damageInfo);
				return;
			}

            if (UseDamageSystem)
                MyDamageSystem.Static.RaiseDestroyed(this, damageInfo);

		    MyAnalyticsHelper.SetLastDamageInformation(damageInfo);

			m_dieAfterSimulation = true;
		}

        public void Die()
        {
            if ((CharacterCanDie || MyPerGameSettings.CharacterSuicideEnabled) && !IsDead )
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                buttonType: MyMessageBoxButtonsType.YES_NO,
                messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionPleaseConfirm),
                messageText: MyTexts.Get(MySpaceTexts.MessageBoxTextSuicide),
                focusedResult: MyGuiScreenMessageBox.ResultEnum.NO,
                callback: delegate(MyGuiScreenMessageBox.ResultEnum retval)
                {
                    if (retval == MyGuiScreenMessageBox.ResultEnum.YES)
                        DoDamage(1000, MyDamageType.Suicide, true, this.EntityId);
                }));
            }
        }

        void DieInternal()
        {
            if (!CharacterCanDie && !MyPerGameSettings.CharacterSuicideEnabled)
                return;

            if (m_InventoryScreen != null)
            {
                m_InventoryScreen.CloseScreen();                
            }

			if (StatComp != null && StatComp.Health != null)
				StatComp.Health.OnStatChanged -= StatComp.OnHealthChanged;

            if (m_breath != null)
                m_breath.CurrentState = MyCharacterBreath.State.Dead;

            if (CurrentRemoteControl != null)
            {
                //This will happen when character is killed without being destroyed
                var remoteControl = CurrentRemoteControl as MyRemoteControl;
                if (remoteControl != null)
                {
                    remoteControl.ForceReleaseControl();
                }
                else
                {
                    var turretControl = CurrentRemoteControl as MyLargeTurretBase;
                    if (turretControl != null)
                    {
                        turretControl.ForceReleaseControl();
                    }
                }
            }

            if (ControllerInfo != null && ControllerInfo.IsLocallyHumanControlled())
            {
                if (MyGuiScreenTerminal.IsOpen)
                {
                    MyGuiScreenTerminal.Hide();
                }

                if (MyGuiScreenGamePlay.ActiveGameplayScreen != null)
                {
                    MyGuiScreenGamePlay.ActiveGameplayScreen.CloseScreen();
                    MyGuiScreenGamePlay.ActiveGameplayScreen = null;
                }

                if (MyGuiScreenGamePlay.TmpGameplayScreenHolder != null)
                {
                    MyGuiScreenGamePlay.TmpGameplayScreenHolder.CloseScreen();
                    MyGuiScreenGamePlay.TmpGameplayScreenHolder = null;
                }
            }

            if (Parent is MyCockpit)
            {
                var cockpit = Parent as MyCockpit;
                if (cockpit.Pilot == this)
                    cockpit.RemovePilot(); //needed to be done localy otherwise client wont see respawn message
            }

            if (MySession.ControlledEntity is MyRemoteControl)
            {
                //This will happen when character is killed without being destroyed
                var remoteControl = MySession.ControlledEntity as MyRemoteControl;
                if (remoteControl.PreviousControlledEntity == this)
                {
                    remoteControl.ForceReleaseControl();
                }
            }

            //TODO(AF) Create a shared RemoteControl component
            if (MySession.ControlledEntity is MyLargeTurretBase)
            {
                //This will happen when character is killed without being destroyed
                var turret = MySession.ControlledEntity as MyLargeTurretBase;
                if (turret.PreviousControlledEntity == this)
                {
                    turret.ForceReleaseControl();
                }
            }

            if (m_currentMovementState == MyCharacterMovementEnum.Died)
            {
                StartRespawn(0.1f);
                return;
            }


            ulong playerId = 0;
            if (ControllerInfo.Controller != null && ControllerInfo.Controller.Player != null)
            {
                playerId = ControllerInfo.Controller.Player.Id.SteamId;
                if (!MySession.Static.Cameras.TryGetCameraSettings(ControllerInfo.Controller.Player.Id, EntityId, out m_cameraSettingsWhenAlive))
                {
                    if (ControllerInfo.IsLocallyHumanControlled())
                    {
                        m_cameraSettingsWhenAlive = new MyEntityCameraSettings()
                        {
                            Distance = MyThirdPersonSpectator.Static.GetDistance(),
                            IsFirstPerson = IsInFirstPersonView,
                            HeadAngle = new Vector2(HeadLocalXAngle, HeadLocalYAngle)
                        };
                    }
                }
            }

            MyAnalyticsHelper.ReportPlayerDeath(ControllerInfo.IsLocallyHumanControlled(), playerId);

            MySandboxGame.Log.WriteLine("Player character died. Id : " + playerId);

            EndShootAll();

            if (Sync.IsServer && m_currentWeapon != null && m_currentWeapon.PhysicalObject != null)
            {
                var inventoryItem = new MyPhysicalInventoryItem()
                {
                    Amount = 1,
                    Content = m_currentWeapon.PhysicalObject,
                };
                // Guns 
                if (inventoryItem.Content is MyObjectBuilder_PhysicalGunObject)
                {
                    (inventoryItem.Content as MyObjectBuilder_PhysicalGunObject).GunEntity.EntityId = 0;
                }
                MyFloatingObjects.Spawn(inventoryItem, ((MyEntity)m_currentWeapon).PositionComp.GetPosition(), WorldMatrix.Forward, WorldMatrix.Up, Physics);
                Inventory.RemoveItemsOfType(1, m_currentWeapon.PhysicalObject);
            }

            IsUsing = null;
            m_isFalling = false;
	        JetpackComp = null; // m_jetpackEnabled = false;
            SetCurrentMovementState(MyCharacterMovementEnum.Died, false);
            UnequipWeapon();
            //m_inventory.Clear(false);
            StopUpperAnimation(0.5f);
			SoundComp.StartSecondarySound(Definition.DeathSoundName, sync: false);

            if (m_isInFirstPerson)
                PlayCharacterAnimation("DiedFps", MyBlendOption.Immediate, MyFrameOption.PlayOnce, 0.5f);
            else
                PlayCharacterAnimation("Died", MyBlendOption.Immediate, MyFrameOption.PlayOnce, 0.5f);

            //InitBoxPhysics(MyMaterialType.METAL, ModelLod0, 900, 0, MyPhysics.DefaultCollisionFilter, RigidBodyFlag.RBF_DEFAULT);
            //InitSpherePhysics(MyMaterialType.METAL, ModelLod0, 900, 0, 0, 0, RigidBodyFlag.RBF_DEFAULT);

            InitDeadBodyPhysics();

            StartRespawn(RESPAWN_TIME);

            m_currentLootingCounter = m_characterDefinition.LootingTime;

            if (CharacterDied != null)
                CharacterDied(this);

            foreach (var component in Components)
            {
                var characterComponent = component as MyCharacterComponent;
                if (characterComponent != null)
                {
                    characterComponent.OnCharacterDead();
                }
            }

            // Syncing dead bodies only when the ragdoll is disabled
            if (!Components.Has<MyCharacterRagdollComponent>())
            {
                SyncFlag = true;
            }
        }

        private void StartRespawn(float respawnTime)
        {
            if (ControllerInfo.Controller != null && ControllerInfo.Controller.Player != null)
            {
                MySessionComponentMissionTriggers.PlayerDied(this.ControllerInfo.Controller.Player);
                if (!MySessionComponentMissionTriggers.CanRespawn(this.ControllerInfo.Controller.Player.Id))
                {
                    m_currentRespawnCounter = -1;
                    return;
                }
            }

            if (this == MySession.ControlledEntity)
            {
                MyGuiScreenTerminal.Hide();

                m_respawnNotification = new MyHudNotification(MySpaceTexts.NotificationRespawn, (int)(RESPAWN_TIME * 1000), priority: 5);
                m_respawnNotification.Level = MyNotificationLevel.Important;
                m_respawnNotification.SetTextFormatArguments((int)m_currentRespawnCounter);
                MyHud.Notifications.Add(m_respawnNotification);
            }

            m_currentRespawnCounter = respawnTime;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }

        private void InitDeadBodyPhysics()
        {
            Vector3 velocity = Vector3.Zero;

            m_radioBroadcaster.BroadcastRadius = 5;
            
            if (Physics != null)
            {
                velocity = Physics.LinearVelocity;

                Physics.Enabled = false;
                Physics.Close();
                Physics = null;
            }

            //if (Physics == null)
            {
                var massProperties = new HkMassProperties();
                massProperties.Mass = 500;

                HkShape shape;
                // CH:TODO: Need to rethink this. It does not belong here, but I don't want to add "DeadCharacterBodyCenterOfMass" to the character definition either...
                if (Definition.Name == "Medieval_barbarian" || Definition.Name == "Medival_male")
                {
                    HkBoxShape bshape = new HkBoxShape(PositionComp.LocalAABB.HalfExtents * new Vector3(1.0f, 1.0f, 0.5f));
                    massProperties = HkInertiaTensorComputer.ComputeBoxVolumeMassProperties(bshape.HalfExtents, massProperties.Mass);
                    massProperties.CenterOfMass = new Vector3(0, 0, bshape.HalfExtents.Z);
                    shape = bshape;

                    Physics = new MyPhysicsBody(this, RigidBodyFlag.RBF_DEFAULT);
                    MatrixD pos = MatrixD.CreateTranslation(PositionComp.LocalAABB.HalfExtents * new Vector3(0.0f, 0.0f, 0.5f));
                    Physics.CreateFromCollisionObject(shape, PositionComp.LocalVolume.Center + PositionComp.LocalAABB.HalfExtents * new Vector3(0.0f, 0.0f, 0.5f), pos, massProperties, MyPhysics.FloatingObjectCollisionLayer);
                    Physics.Friction = 0.5f;
                    Physics.RigidBody.MaxAngularVelocity = MathHelper.PiOver2;
                    Physics.LinearVelocity = velocity;
                    shape.RemoveReference();

                    Physics.Enabled = true;
                }
                else if (Definition.Name == "Space_spider")
                {
                    HkBoxShape bshape = new HkBoxShape(PositionComp.LocalAABB.HalfExtents * new Vector3(2.0f, 0.3f, 2.0f));
                    massProperties = HkInertiaTensorComputer.ComputeBoxVolumeMassProperties(bshape.HalfExtents, massProperties.Mass);
                    massProperties.CenterOfMass = new Vector3(0, 0, 0);
                    shape = bshape;

                    Physics = new MyPhysicsBody(this, RigidBodyFlag.RBF_DEFAULT);
                    MatrixD pos = MatrixD.CreateTranslation(PositionComp.LocalAABB.HalfExtents * new Vector3(0.0f, 0.2f, 0.0f));
                    Physics.CreateFromCollisionObject(shape, PositionComp.LocalVolume.Center + PositionComp.LocalAABB.HalfExtents * new Vector3(0.0f, 0.2f, 0.0f), pos, massProperties, MyPhysics.FloatingObjectCollisionLayer);
                    Physics.Friction = 20.0f;
                    Physics.RigidBody.MaxAngularVelocity = MathHelper.PiOver2;
                    Physics.LinearVelocity = velocity;
                    shape.RemoveReference();

                    Physics.Enabled = true;
                }
                else
                {
                    HkBoxShape bshape = new HkBoxShape(PositionComp.LocalAABB.HalfExtents);
                    massProperties = HkInertiaTensorComputer.ComputeBoxVolumeMassProperties(bshape.HalfExtents, massProperties.Mass);
                    massProperties.CenterOfMass = new Vector3(bshape.HalfExtents.X, 0, 0);
                    shape = bshape;

                    Physics = new MyPhysicsBody(this, RigidBodyFlag.RBF_DEFAULT);
                    Physics.CreateFromCollisionObject(shape, PositionComp.LocalVolume.Center, MatrixD.Identity, massProperties, MyPhysics.FloatingObjectCollisionLayer);
                    Physics.Friction = 0.5f;
                    Physics.RigidBody.MaxAngularVelocity = MathHelper.PiOver2;
                    Physics.LinearVelocity = velocity;
                    shape.RemoveReference();

                    Physics.Enabled = true;
                }
            }            

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (GetCurrentMovementState() == MyCharacterMovementEnum.Died)
            {
                Physics.ForceActivate();
            }

            base.UpdateOnceBeforeFrame();
        }

        #endregion

        #region Properties

        public Vector3 ColorMask
        {
            get { return Render.ColorMaskHsv; }
        }

        public string ModelName
        {
            get { return m_characterModel; }
        }

        public IMyGunObject<MyDeviceBase> CurrentWeapon
        {
            get { return m_currentWeapon; }
        }

        public IMyHandheldGunObject<MyDeviceBase> LeftHandItem
        {
            get { return m_leftHandItem as IMyHandheldGunObject<MyDeviceBase>; }
        }

        internal IMyControllableEntity CurrentRemoteControl { get; set; }

        public MyBattery SuitBattery
        {
            get { return m_suitBattery; }
        }

        public StringBuilder DisplayNameText
        {
            //get { return MyTexts.Get(MySpaceTexts.PlayerCharacter); }
            get { return new StringBuilder(DisplayName); }
        }

        public static bool CharactersCanDie
        {
            get { return !MySession.Static.CreativeMode || MyFakes.CHARACTER_CAN_DIE_EVEN_IN_CREATIVE_MODE; }
        }

        public bool CharacterCanDie
        {
            get { return CharactersCanDie || ((MyPerGameSettings.Game == GameEnum.ME_GAME) && ControllerInfo.Controller != null && ControllerInfo.Controller.Player.Id.SerialId != 0); }
        }

        public override Vector3D LocationForHudMarker
        {
            get
            {
                return base.LocationForHudMarker + WorldMatrix.Up * 2.1;
            }
        }

        internal MyRadioBroadcaster RadioBroadcaster
        {
            get { return m_radioBroadcaster; }
        }

        internal MyRadioReceiver RadioReceiver
        {
            get { return m_radioReceiver; }
        }

        public new MyPhysicsBody Physics { get { return base.Physics as MyPhysicsBody; } set { base.Physics = value; } }

        #endregion

        #region Scene

        public void SetLocalHeadAnimation(float? targetX, float? targetY, float length)
        {
            m_currentLocalHeadAnimation = 0;
            m_localHeadAnimationLength = length;
            if (targetX.HasValue)
                m_localHeadAnimationX = new Vector2(m_headLocalXAngle, targetX.Value);
            else
                m_localHeadAnimationX = null;

            if (targetY.HasValue)
                m_localHeadAnimationY = new Vector2(m_headLocalYAngle, targetY.Value);
            else
                m_localHeadAnimationY = null;
        }

        public bool IsLocalHeadAnimationInProgress()
        {
            return m_currentLocalHeadAnimation >= 0;
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);

            bool isInFPDisabledCockpit = (IsUsing is MyCockpit) && !(IsUsing as MyCockpit).BlockDefinition.EnableFirstPerson;
            if (m_currentMovementState == MyCharacterMovementEnum.Sitting)
            {
                EnableBag(m_enableBag);
                EnableHead(!isInFPDisabledCockpit || !Render.NearFlag);
            }

            //Causes to add weapon after the character in update list
            if (m_currentWeapon != null)
            {
                MyEntities.Remove((MyEntity)m_currentWeapon);
                MyEntities.Add((MyEntity)m_currentWeapon);
            }

            UpdateShadowIgnoredObjects();
        }

        /// <summary>
        /// This will just spawn new character, to take control, call respawn on player
        /// </summary>
        public static MyCharacter CreateCharacter(MatrixD worldMatrix, Vector3 velocity, string characterName, string model, Vector3? colorMask, bool findNearPos = true, bool AIMode = false, MyCockpit cockpit = null, bool useInventory = true, ulong playerSteamId = 0)
        {
            Vector3D? characterPos = null;
            if (findNearPos)
            {
                characterPos = MyEntities.FindFreePlace(worldMatrix.Translation, 2, 200, 5, 0.5f);

                // Extended search
                if (!characterPos.HasValue)
                {
                    characterPos = MyEntities.FindFreePlace(worldMatrix.Translation, 2, 200, 5, 5);
                }
            }

            // Use default position
            if (characterPos.HasValue)
            {
                worldMatrix.Translation = characterPos.Value;
            }

            MyCharacter character = CreateCharacterBase(worldMatrix, ref velocity, characterName, model, colorMask, AIMode, useInventory);

            if (cockpit == null)
            {
                MyMultiplayer.ReplicateImmediatelly(MyExternalReplicable.FindByObject(character));
                //MySyncCreate.SendEntityCreated(character.GetObjectBuilder());
            }
            else
            {
                IMyReplicable characterReplicable = MyExternalReplicable.FindByObject(character);
                MyMultiplayer.ReplicateImmediatelly(characterReplicable, new EndpointId(playerSteamId));
                MyMultiplayer.ReplicateImmediatelly(MyExternalReplicable.FindByObject(cockpit.CubeGrid), characterReplicable);
                MySyncCharacter.SendCharacterAttachToCockpit(character.EntityId, cockpit);
                //MySyncCharacter.SendCharacterCreated(character.GetObjectBuilder() as MyObjectBuilder_Character, cockpit);
            }

            return character;
        }

        public static MyCharacter CreateCharacterRelative(MyEntity baseEntity, Matrix relativeMatrix, Vector3 relativeVelocity, string characterName, string model, Vector3? colorMask, bool findNearPos = true, bool AIMode = false)
        {
            Matrix worldMatrix = relativeMatrix * baseEntity.WorldMatrix;

            var characterPos = MyEntities.FindFreePlace((Vector3D)worldMatrix.Translation, 2, 200, 5, 0.5f);
            if (characterPos.HasValue)
                worldMatrix.Translation = (Vector3)characterPos.Value;

            Vector3 velocity = Vector3.Transform(relativeVelocity, baseEntity.WorldMatrix.GetOrientation());

            MyCharacter character = CreateCharacterBase(worldMatrix, ref velocity, characterName, model, colorMask, AIMode);

            MySyncCreate.SendEntityCreatedRelative(character.GetObjectBuilder(), baseEntity, relativeVelocity);
            return character;
        }

        private static MyCharacter CreateCharacterBase(MatrixD worldMatrix, ref Vector3 velocity, string characterName, string model, Vector3? colorMask, bool AIMode, bool useInventory = true)
        {
            MyCharacter character = new MyCharacter();
            MyEntities.RaiseEntityCreated(character);
            MyObjectBuilder_Character objectBuilder = MyCharacter.Random();
            objectBuilder.CharacterModel = model ?? MyCharacter.DefaultModel;

            if (colorMask.HasValue)
                objectBuilder.ColorMaskHSV = colorMask.Value;

            objectBuilder.JetpackEnabled = MySession.Static.CreativeMode;
	        objectBuilder.Battery = new MyObjectBuilder_Battery {CurrentCapacity = 1};
            objectBuilder.AIMode = AIMode;
            objectBuilder.DisplayName = characterName;
            objectBuilder.LinearVelocity = velocity;
            objectBuilder.PositionAndOrientation = new MyPositionAndOrientation(worldMatrix);
            character.Init(objectBuilder);
            if (useInventory)
                MyWorldGenerator.InitInventoryWithDefaults(character.GetInventory());
            MyEntities.Add(character);
            //character.PositionComp.SetWorldMatrix(worldMatrix);
            if (velocity.Length() > 0)
            {
	            var jetpack = character.JetpackComp;

				if(jetpack != null)
					jetpack.EnableDampeners(false, false);
            }

            return character;
        }

        #endregion


        public override string ToString()
        {
            return m_characterModel;
        }

        public MyEntity Entity
        {
            get { return this; }
        }

        private MyControllerInfo m_info = new MyControllerInfo();
        private MyDefinitionId? m_endShootAutoswitch = null;
        private MyDefinitionId? m_autoswitch = null;
        public MyControllerInfo ControllerInfo { get { return m_info; } }

        public bool IsDead
        {
            get { return m_currentMovementState == MyCharacterMovementEnum.Died; }
        }

        public bool IsSitting
        {
            get { return m_currentMovementState == MyCharacterMovementEnum.Sitting; }
        }

        public void ShowOutOfAmmoNotification()
        {
            if (OutOfAmmoNotification == null)
            {
                OutOfAmmoNotification = new MyHudNotification(MySpaceTexts.OutOfAmmo, 2000, font: MyFontEnum.Red);
            }

            if (m_currentWeapon is MyEntity)
                OutOfAmmoNotification.SetTextFormatArguments((m_currentWeapon as MyEntity).DisplayName);
            MyHud.Notifications.Add(OutOfAmmoNotification);
        }

        public void UpdateHudCharacterInfo()
        {
            MyHud.CharacterInfo.BatteryEnergy = 100 * SuitBattery.ResourceSource.RemainingCapacityByType(MyResourceDistributorComponent.ElectricityId) / MyEnergyConstants.BATTERY_MAX_CAPACITY;
            MyHud.CharacterInfo.IsBatteryEnergyLow = SuitBattery.IsEnergyLow;
            MyHud.CharacterInfo.Speed = Physics.LinearVelocity.Length();
            MyHud.CharacterInfo.Mass = Inventory != null ? (int)((float)Inventory.CurrentMass + Definition.Mass) : 0;
            MyHud.CharacterInfo.LightEnabled = LightEnabled;
            MyHud.CharacterInfo.BroadcastEnabled = m_radioBroadcaster.Enabled;
			
	        var jetpack = JetpackComp;
	        bool canFly = jetpack != null && jetpack.Running;
            MyHud.CharacterInfo.DampenersEnabled = jetpack != null && jetpack.DampenersTurnedOn;
            MyHud.CharacterInfo.JetpackEnabled = jetpack != null && jetpack.TurnedOn;

			MyHud.CharacterInfo.State = MyHudCharacterStateEnum.Standing;
			var entity = MySession.ControlledEntity;
			var cockpit = entity as MyCockpit;
			if(entity != null)
			{
				if (cockpit != null)
				{
					var grid = cockpit.CubeGrid;
					if (grid.GridSizeEnum == MyCubeSize.Small)
					{
						MyHud.CharacterInfo.State = MyHudCharacterStateEnum.PilotingSmallShip;
					}
					else
					{
						if (grid.IsStatic)
							MyHud.CharacterInfo.State = MyHudCharacterStateEnum.ControllingStation;
						else
							MyHud.CharacterInfo.State = MyHudCharacterStateEnum.PilotingLargeShip;
					}
				}
				else
				{
					if (canFly)
						MyHud.CharacterInfo.State = MyHudCharacterStateEnum.Flying;
					else if (IsCrouching)
						MyHud.CharacterInfo.State = MyHudCharacterStateEnum.Crouching;
					else
						if (IsFalling)
							MyHud.CharacterInfo.State = MyHudCharacterStateEnum.Falling;
						else
							MyHud.CharacterInfo.State = MyHudCharacterStateEnum.Standing;
				}
			}

            

			float healthRatio = 1.0f;
			if(StatComp != null)
				healthRatio = StatComp.HealthRatio;

            MyHud.CharacterInfo.HealthRatio = healthRatio;
			MyHud.CharacterInfo.IsHealthLow = healthRatio < MyCharacterStatComponent.LOW_HEALTH_RATIO;
            MyHud.CharacterInfo.InventoryVolume = Inventory != null ? Inventory.CurrentVolume : 0;
            MyHud.CharacterInfo.IsInventoryFull = Inventory == null || ((float)Inventory.CurrentVolume / (float)Inventory.MaxVolume) > 0.95f;
            MyHud.CharacterInfo.BroadcastRange = RadioBroadcaster.BroadcastRadius;
            MyHud.CharacterInfo.OxygenLevel = OxygenComponent.SuitOxygenLevel;
            MyHud.CharacterInfo.HydrogenRatio = OxygenComponent.GetGasFillLevel(MyCharacterOxygenComponent.HydrogenId);
            MyHud.CharacterInfo.IsOxygenLevelLow = OxygenComponent.IsOxygenLevelLow;
            MyHud.CharacterInfo.IsHelmetOn = !Definition.NeedsOxygen;
        }


        internal void UpdateCharacterPhysics(bool isLocalPlayer)
        {
            if (Physics != null && Physics.Enabled == false)
                return;

            float offset = 2 * MyPerGameSettings.PhysicsConvexRadius + 0.03f; //compensation for convex radius

            if (isLocalPlayer || !MyPerGameSettings.EnableKinematicMPCharacter)
            {
                if (Physics == null || Physics.IsKinematic)
                {
                    if (Physics != null)
                        Physics.Close();

                    this.InitCharacterPhysics(MyMaterialType.CHARACTER, PositionComp.LocalVolume.Center, Definition.CharacterCollisionWidth * Definition.CharacterCollisionScale, Definition.CharacterCollisionHeight - Definition.CharacterCollisionWidth * Definition.CharacterCollisionScale - offset,
                    Definition.CharacterCollisionCrouchHeight - Definition.CharacterCollisionWidth,
                    Definition.CharacterCollisionWidth - offset,
                    Definition.CharacterHeadSize * Definition.CharacterCollisionScale,
                    Definition.CharacterHeadHeight,
                    0.7f, 0.7f, (ushort)MyPhysics.CharacterCollisionLayer, RigidBodyFlag.RBF_DEFAULT,
                    MyPerGameSettings.Destruction ? MyDestructionHelper.MassToHavok(Definition.Mass) : Definition.Mass,
                    Definition.VerticalPositionFlyingOnly,
                    Definition.MaxSlope,
                    Definition.ImpulseLimit, false);

                    Physics.Enabled = true;
                }
            }
            else
            {
                if (Physics == null || !Physics.IsKinematic)
                {
                    if (Physics != null)
                        Physics.Close();

                    float scale = Sync.IsServer ? MyPerGameSettings.NetworkCharacterScale : 1;
                    int layer = Sync.IsServer ? MyPhysics.CharacterNetworkCollisionLayer : MyPerGameSettings.NetworkCharacterCollisionLayer;

                    this.InitCharacterPhysics(MyMaterialType.CHARACTER, PositionComp.LocalVolume.Center, Definition.CharacterCollisionWidth * Definition.CharacterCollisionScale * scale, Definition.CharacterCollisionHeight - Definition.CharacterCollisionWidth * Definition.CharacterCollisionScale * scale - offset,
                    Definition.CharacterCollisionCrouchHeight - Definition.CharacterCollisionWidth,
                    Definition.CharacterCollisionWidth - offset,
                    Definition.CharacterHeadSize * Definition.CharacterCollisionScale * scale,
                    Definition.CharacterHeadHeight,
                    0.7f, 0.7f, (ushort)layer, MyPerGameSettings.NetworkCharacterType, 0, //Mass is not scaled on purpose (collision over networks)
                    Definition.VerticalPositionFlyingOnly,
                    Definition.MaxSlope,
                    Definition.ImpulseLimit, true);

                    if (MyPerGameSettings.NetworkCharacterType == RigidBodyFlag.RBF_DEFAULT)
                    {
                        Physics.Friction = 1; //to not move on steep surfaces
                    }

                    Physics.Enabled = true;
                }
            }            
        }

        #region Multiplayer

        internal new MySyncCharacter SyncObject
        {
            get { return (MySyncCharacter)base.SyncObject; }
        }

        protected override MySyncEntity OnCreateSync()
        {
            var result = new MySyncCharacter(this);
            result.MovementStateChanged += MovementStateChangeSuccess;
            result.CharacterModelSwitched += ChangeModelAndColorInternal;
            result.FlagsChanged += FlagsChangeSuccess;
            result.HeadOrSpineChanged += HeadOrSpineChangeSuccess;
            result.SwitchToWeaponSuccessHandler += SwitchToWeaponSuccess;
            result.SwitchAmmoMagazineSuccessHandler += SwitchAmmoMagazineSuccess;
            //result.ShootHandler += ShootSuccess;
            result.DoDamageHandler += DoDamageSuccess;

            if (MyFakes.CHARACTER_SERVER_SYNC)
                //result.UpdatesOnlyOnServer = true;
                SyncFlag = false; //synced only through MoveAndRotate

            if (MyPerGameSettings.EnablePerFrameCharacterSync)
            {
                result.DefaultUpdateCount = 0;
                result.LinearMovementUpdateCount = 0;
            }
            return result;
        }

        void MovementStateChangeSuccess(MyCharacterMovementEnum state)
        {
            if (!IsDead)
            {
                if (!MyFakes.CHARACTER_SERVER_SYNC)
                {
                    SwitchAnimation(state);
                    SetCurrentMovementState(state, false);
                }
            }
        }

        void FlagsChangeSuccess(bool enableJetpack, bool enableDampeners, bool enableLights, bool enableIronsight, bool enableBroadcast, bool targetFromCamera)
        {
	        if (IsDead)
				return;

	        var jetpack = JetpackComp;

	        if (jetpack != null)
                {
				jetpack.TurnOnJetpack(enableJetpack, false, false);
				jetpack.EnableDampeners(enableDampeners, false);
                }

                if (enableLights != LightEnabled)
                {
                    EnableLights(enableLights, false);
                }

                if (enableIronsight != (m_zoomMode == MyZoomModeEnum.IronSight))
                {
                    EnableIronsight(enableIronsight, true, false, false);
                }

                if (enableBroadcast != m_radioBroadcaster.Enabled)
                {
                    EnableBroadcasting(enableBroadcast, false);
                }

                TargetFromCamera = targetFromCamera;
            }

        void HeadOrSpineChangeSuccess(float headLocalXAngle, float headLocalYAngle, Quaternion spineRotation,
    Quaternion headRotation, Quaternion handRotation, Quaternion upperHandRotation)
        {
            if (!IsDead)
            {
                SetHeadLocalXAngle(headLocalXAngle, false);
                SetHeadLocalYAngle(headLocalYAngle, false);
                if (spineRotation != Quaternion.Zero)
                    SetSpineAdditionalRotation(spineRotation, spineRotation, false);
                SetHeadAdditionalRotation(headRotation, false);
                SetHandAdditionalRotation(handRotation, false);
                SetUpperHandAdditionalRotation(upperHandRotation, false);
            }
        }

        void SwitchToWeaponSuccess(MyDefinitionId? weapon, MyObjectBuilder_EntityBase weaponObjectBuilder, long weaponEntityId)
        {
            if (!IsDead)
            {
                SwitchToWeaponInternal(weapon, false, false, weaponObjectBuilder, weaponEntityId);
            }            

            if (OnWeaponChanged != null)
            {
                OnWeaponChanged(this, null);               
            }            
        }

        void DoDamageSuccess(float damage, MyStringHash damageType, long attackerId)
        {
            DoDamage(damage, damageType, false, attackerId);
        }
        #endregion

        static float minAmp = 1.12377834f;
        static float maxAmp = 1.21786702f;
        static float medAmp = (minAmp + maxAmp) / 2.0f;
        static float runMedAmp = (1.03966641f + 1.21786702f) / 2.0f;
        private MatrixD m_lastCorrectSpectatorCamera;
        private float m_squeezeDamageTimer;
        

        void UpdateWeaponPosition()
        {
            float IKRatio = m_currentAnimationToIKTime / m_animationToIKDelay;

	        var jetpack = JetpackComp;
	        bool canFly = jetpack != null && jetpack.Running;
            MatrixD weaponMatrixPositioned = GetHeadMatrix(false, !canFly, false, true);

            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_MISC)
            {
                VRageRender.MyRenderProxy.DebugDrawText3D(weaponMatrixPositioned.Translation, "HeadDummy", Color.White, 0.8f, false);
                VRageRender.MyRenderProxy.DebugDrawAxis(weaponMatrixPositioned, 1, false);
            }

            float cameraModeBlend = 0;

            if (MySession.ControlledEntity == this)
                cameraModeBlend = MySector.MainCamera.Zoom.GetZoomLevel();
            else
            {
                cameraModeBlend = m_zoomMode == MyZoomModeEnum.IronSight ? 0 : 1;
            }


            MatrixD ironsightMatrix = MatrixD.Identity;
            ironsightMatrix.Translation = m_weaponIronsightTranslation;
            if (m_currentWeapon is MyEngineerToolBase)
            {
                ironsightMatrix.Translation = m_toolIronsightTranslation;
            }

            MatrixD weaponLocation;
            float totalBlendTime = m_handItemDefinition.BlendTime;
            float shootBlendTime = m_handItemDefinition.ShootBlend;

            bool isWalkingState = IsWalkingState(m_currentMovementState);
            MatrixD standingMatrix = m_isInFirstPerson ? m_handItemDefinition.ItemLocation : m_handItemDefinition.ItemLocation3rd;
            MatrixD walkingMatrix = m_isInFirstPerson ? m_handItemDefinition.ItemWalkingLocation : m_handItemDefinition.ItemWalkingLocation3rd;
            MatrixD shootingMatrix = m_isInFirstPerson ? m_handItemDefinition.ItemShootLocation : m_handItemDefinition.ItemShootLocation3rd;

            if (m_currentHandItemWalkingBlend != -1 && totalBlendTime > 0)
            {
                m_currentHandItemWalkingBlend += 0.05f;
                if (m_currentHandItemWalkingBlend > totalBlendTime)
                    m_currentHandItemWalkingBlend = -1;
            }

            if (m_currentShootPositionTime > 0 && shootBlendTime > 0)
            {
                m_currentHandItemWalkingBlend = -1;

                m_currentHandItemShootBlend += 0.05f;

                if (m_currentHandItemShootBlend > shootBlendTime)
                    m_currentHandItemShootBlend = shootBlendTime;

                MatrixD source = isWalkingState ? walkingMatrix : standingMatrix;
                MatrixD target = shootingMatrix;

                weaponLocation = MatrixD.Lerp(source, target, m_currentHandItemShootBlend / shootBlendTime);
            }
            else
                if (m_currentShootPositionTime <= 0 && shootBlendTime > 0 && m_currentHandItemShootBlend > 0)
                {
                    m_currentHandItemWalkingBlend = -1;

                    m_currentHandItemShootBlend -= 0.05f;

                    if (m_currentHandItemShootBlend < 0)
                        m_currentHandItemShootBlend = 0;

                    MatrixD source = isWalkingState ? walkingMatrix : standingMatrix;
                    MatrixD target = shootingMatrix;

                    weaponLocation = MatrixD.Lerp(source, target, m_currentHandItemShootBlend / shootBlendTime);
                }
                else
                {
                    m_currentHandItemShootBlend = 0;

                    if (m_currentHandItemWalkingBlend != -1 && totalBlendTime > 0)
                    {

                        MatrixD source;
                        MatrixD target;

                        if (isWalkingState)
                        {
                            source = standingMatrix;
                            target = walkingMatrix;
                        }
                        else
                        {
                            source = walkingMatrix;
                            target = standingMatrix;
                        }

                        weaponLocation = MatrixD.Lerp(source, target, m_currentHandItemWalkingBlend / totalBlendTime);
                    }
                    else
                    {
                        weaponLocation = isWalkingState ? walkingMatrix : standingMatrix;
                    }
                }


            if (m_currentShootPositionTime > 0 && m_handItemDefinition.ScatterSpeed > 0)
            {
                if (m_currentScatterBlend == 0)
                    m_lastScatterPos = Vector3.Zero;

                if (m_currentScatterBlend == m_handItemDefinition.ScatterSpeed)
                {
                    m_lastScatterPos = m_currentScatterPos;
                    m_currentScatterBlend = 0;
                }

                if ((m_currentScatterBlend == 0) || (m_currentScatterBlend == m_handItemDefinition.ScatterSpeed))
                {
                    m_currentScatterPos = new Vector3(
                    MyUtils.GetRandomFloat(-m_handItemDefinition.ShootScatter.X / 2, m_handItemDefinition.ShootScatter.X / 2),
                    MyUtils.GetRandomFloat(-m_handItemDefinition.ShootScatter.Y / 2, m_handItemDefinition.ShootScatter.Y / 2),
                    MyUtils.GetRandomFloat(-m_handItemDefinition.ShootScatter.Z / 2, m_handItemDefinition.ShootScatter.Z / 2)
                    );
                }

                m_currentScatterBlend += 0.003f;

                if (m_currentScatterBlend > m_handItemDefinition.ScatterSpeed)
                    m_currentScatterBlend = m_handItemDefinition.ScatterSpeed;

                Vector3 scatterPos = Vector3.Lerp(m_lastScatterPos, m_currentScatterPos, m_currentScatterBlend / m_handItemDefinition.ScatterSpeed);

                weaponLocation.Translation += scatterPos;
            }
            else
            {
                m_currentScatterBlend = 0;
            }

            Matrix spineMatrix = WorldMatrix;
            if (Bones.IsValidIndex(m_spineBone))
                spineMatrix = Bones[m_spineBone].AbsoluteTransform;


            float middle = m_currentMovementState == MyCharacterMovementEnum.Sprinting ? runMedAmp : medAmp;

            float waveValue = (spineMatrix.Translation.Y - middle);

            //if (!m_isCrouching)
            //  weaponLocation = Matrix.CreateRotationY((waveValue + 0.02f)) * weaponLocation;

            float runScale = 1.0f;
            if (m_currentMovementState == MyCharacterMovementEnum.Sprinting)
                runScale = m_handItemDefinition.RunMultiplier;
            if (!m_isInFirstPerson)
                runScale *= m_handItemDefinition.AmplitudeMultiplier3rd;

            MatrixD ironsightMatrixPositioned = ironsightMatrix * weaponMatrixPositioned;
            MatrixD weaponMatrixPositionedWaved = weaponLocation * weaponMatrixPositioned;

            weaponMatrixPositionedWaved.Translation = Vector3D.Transform(weaponLocation.Translation, weaponMatrixPositioned);

            Vector3D weaponWorldTranslation = weaponMatrixPositionedWaved.Translation;
            if (isWalkingState)
            {
                weaponWorldTranslation += WorldMatrix.Right * runScale * m_handItemDefinition.XAmplitudeScale * (waveValue);
                weaponWorldTranslation += WorldMatrix.Up * runScale * m_handItemDefinition.YAmplitudeScale * (waveValue);
                weaponWorldTranslation += WorldMatrix.Forward * runScale * m_handItemDefinition.ZAmplitudeScale * (waveValue);
            }
            weaponMatrixPositionedWaved.Translation = weaponWorldTranslation;


            if (m_currentWeapon is MyEngineerToolBase)
            {
                ((MyEngineerToolBase)m_currentWeapon).SensorDisplacement = -weaponLocation.Translation;
            }

            MatrixD weaponFinalLocalIK = MatrixD.Lerp(ironsightMatrixPositioned, weaponMatrixPositionedWaved, cameraModeBlend);
            Debug.Assert(Bones.IsValidIndex(m_weaponBone), "Warning! Weapon bone " + Definition.WeaponBone + " on model " + ModelName + " is missing.");

            MatrixD weaponFinalLocalAnim;
            if (Bones.IsValidIndex(m_weaponBone))
            {
                weaponFinalLocalAnim = m_relativeWeaponMatrix * Bones[m_weaponBone].AbsoluteTransform * WorldMatrix;
            }
            else
            {
                weaponFinalLocalAnim = m_relativeWeaponMatrix * WorldMatrix;
            }

            MatrixD weaponFinalLocal = MatrixD.Lerp(weaponFinalLocalAnim, weaponFinalLocalIK, IKRatio);

            if (m_currentWeapon.BackkickForcePerSecond > 0 && m_currentShotTime > SHOT_TIME - 0.05f)
            {
                weaponFinalLocal.Translation -= weaponFinalLocal.Forward * 0.01f * (float)System.Math.Cos(MySandboxGame.TotalGamePlayTimeInMilliseconds);
            }

            //VRageRender.MyRenderProxy.DebugDrawAxis(weaponMatrixPositioned, 10, false);

            ((MyEntity)m_currentWeapon).WorldMatrix = weaponFinalLocal;

            var headMatrix = GetHeadMatrix(true);
            m_crosshairPoint = headMatrix.Translation + headMatrix.Forward * 2000;            
        }

        void UpdateLeftHandItemPosition()
        {
            MatrixD leftHandItemMatrix = Bones[m_leftHandItemBone].AbsoluteTransform * WorldMatrix;
            Vector3D up = leftHandItemMatrix.Up;
            leftHandItemMatrix.Up = leftHandItemMatrix.Forward;
            leftHandItemMatrix.Forward = up;
            m_leftHandItem.WorldMatrix = leftHandItemMatrix;
        }


        void StoreWeaponRelativeMatrix()
        {
            if (m_currentWeapon != null)
            {
                Matrix weaponWorld = ((MyEntity)m_currentWeapon).WorldMatrix;
                Matrix handWorld;
                if (Bones.IsValidIndex(m_weaponBone))
                {
                    handWorld = Bones[m_weaponBone].AbsoluteTransform * WorldMatrix;
                }
                else
                {
                    handWorld = WorldMatrix;
                }
                m_relativeWeaponMatrix = weaponWorld * Matrix.Invert(handWorld);
            }
        }


        public float CurrentJump
        {
            get { return m_currentJump; }
        }

        public MyToolbarType ToolbarType
        {
            get
            {
                return MyToolbarType.Character;
            }
        }

        public void ChangeModelAndColor(string model, Vector3 colorMaskHSV)
        {
            if (SyncObject != null)
                SyncObject.ChangeCharacterModelAndColor(model, colorMaskHSV);
        }

        internal void ChangeModelAndColorInternal(string model, Vector3 colorMaskHSV)
        {
            MyCharacterDefinition def;
            if (model != m_characterModel && MyDefinitionManager.Static.Characters.TryGetValue(model, out def) && !string.IsNullOrEmpty(def.Model))
            {
                MyObjectBuilder_Character characterOb = (MyObjectBuilder_Character)GetObjectBuilder();

                Components.Remove<MyInventoryBase>();
				Components.Remove<MyCharacterJetpackComponent>();

                var pos = PositionComp.GetPosition();
                var newModel = MyModels.GetModelOnlyData(def.Model);

                if (newModel == null)
                    return;

                Render.CleanLights();
                CloseInternal();

                MyEntities.Remove(this);
                Physics.Close();
                Physics = null;

                m_characterModel = model;
                Render.ModelStorage = newModel;

                characterOb.CharacterModel = model;
                characterOb.EntityId = 0;
                if (characterOb.HandWeapon != null)
                    characterOb.HandWeapon.EntityId = 0;

                Init(characterOb);

                SwitchAnimation(characterOb.MovementState, false);

                if (m_currentWeapon != null)
                {
                    m_currentWeapon.OnControlAcquired(this);
                }

                MyEntities.Add(this);

                MyEntityIdentifier.AllocationSuspended = false;

                ControllerInfo.Controller.Player.Identity.ChangeCharacter(this);
            }

            Render.ColorMaskHsv = colorMaskHSV;
        }

        public void SetPhysicsEnabled(bool enabled)
        {
            SyncObject.SetPhysicsEnabled(enabled);
        }

        bool IMyComponentOwner<MyDataBroadcaster>.GetComponent(out MyDataBroadcaster component)
        {
            component = m_radioBroadcaster;
            return m_radioBroadcaster != null;
        }

        bool IMyComponentOwner<MyDataReceiver>.GetComponent(out MyDataReceiver component)
        {
            component = m_radioReceiver;
            return m_radioReceiver != null;
        }

        public MyRelationsBetweenPlayerAndBlock GetRelationTo(long playerId)
        {
            var controller = ControllerInfo.Controller ?? m_oldController;
            if (controller == null)
                return MyRelationsBetweenPlayerAndBlock.Enemies;

            return controller.Player.GetRelationTo(playerId);
        }

        //IMyUseObject implementation for dead bodies
        /// <summary>
        /// Consider object as being in interactive range only if distance from character is smaller or equal to this value
        /// </summary>
        float IMyUseObject.InteractiveDistance
        {
            get
            {
                return 5;
            }
        }

        /// <summary>
        /// Matrix of object, scale represents size of object
        /// </summary>
        MatrixD IMyUseObject.ActivationMatrix
        {
            get
            {
                if (PositionComp == null)
                    return MatrixD.Zero;
                float scale = 0.75f;
                Matrix m = WorldMatrix;
                m.Forward *= scale;
                m.Up *= Definition.CharacterCollisionHeight * scale;
                m.Right *= scale;
                m.Translation = PositionComp.WorldAABB.Center;
                return m;
            }
        }

        MatrixD IMyUseObject.WorldMatrix
        {
            get { return WorldMatrix; }
        }

        /// <summary>
        /// Matrix of object, scale represents size of object
        /// </summary>
        int IMyUseObject.RenderObjectID
        {
            get
            {
                if (Render.RenderObjectIDs.Length > 0)
                    return (int)Render.RenderObjectIDs[0];
                return -1;
            }
        }

        /// <summary>
        /// Show overlay (semitransparent bounding box)
        /// </summary>
        bool IMyUseObject.ShowOverlay
        {
            get { return false; }
        }

        /// <summary>
        /// Returns supported actions
        /// </summary>
        UseActionEnum IMyUseObject.SupportedActions
        {
            get
            {
                return GetCurrentMovementState() == MyCharacterMovementEnum.Died ? UseActionEnum.OpenInventory | UseActionEnum.OpenTerminal : UseActionEnum.None;
            }
        }

        /// <summary>
        /// When true, use will be called every frame
        /// </summary>
        bool IMyUseObject.ContinuousUsage
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Uses object by specified action
        /// Caller calls this method only on supported actions
        /// </summary>
        void IMyUseObject.Use(UseActionEnum actionEnum, IMyEntity entity)
        {
            var user = entity as MyCharacter;
            if (MyPerGameSettings.TerminalEnabled)
            {
                MyGuiScreenTerminal.Show(MyTerminalPageEnum.Inventory, user, this);
            }
            if (MyPerGameSettings.GUI.InventoryScreen != null && IsDead)
            {               
                var inventory = Components.Get<MyInventoryAggregate>();
                var screen = user.ShowAggregateInventoryScreen(inventory);               
            }
        }

        /// <summary>
        /// Gets action text
        /// Caller calls this method only on supported actions
        /// </summary>
        MyActionDescription IMyUseObject.GetActionInfo(UseActionEnum actionEnum)
        {
            return new MyActionDescription()
            {
                Text = MySpaceTexts.NotificationHintPressToOpenInventory,
                FormatParams = new object[] { MyInput.Static.GetGameControl(MyControlsSpace.INVENTORY), DisplayName },
                IsTextControlHint = true,
                JoystickText = MySpaceTexts.NotificationHintJoystickPressToOpenInventory,
                JoystickFormatParams = new object[] { DisplayName },
            };
        }

        void IMyUseObject.OnSelectionLost() { }

        bool IMyUseObject.PlayIndicatorSound
        {
            get { return true; }
        }

        public void SwitchLeadingGears()
        {
        }

        public void OnInventoryBreak()
        {
            Debug.Fail("Inventory should be released");
        }


        public void OnDestroy()
        {
            Die();
        }

        public bool UseDamageSystem { get; private set; }

        public float Integrity
        {
			get
			{
				float integrity = 100.0f;
				if (StatComp != null && StatComp.Health != null)
					integrity = StatComp.Health.Value;
				return integrity;
			}
        }

        MatrixD IMyCameraController.GetViewMatrix()
        {
            return GetViewMatrix();
        }

        void IMyCameraController.Rotate(Vector2 rotationIndicator, float rollIndicator)
        {
            Rotate(rotationIndicator, rollIndicator);
        }

        void IMyCameraController.RotateStopped()
        {
            RotateStopped();
        }

        void IMyCameraController.OnAssumeControl(IMyCameraController previousCameraController)
        {
            OnAssumeControl(previousCameraController);
        }

        void IMyCameraController.OnReleaseControl(IMyCameraController newCameraController)
        {
            OnReleaseControl(newCameraController);
        }

        bool IMyCameraController.IsInFirstPersonView
        {
            get
            {
                return IsInFirstPersonView;
            }
            set
            {
                IsInFirstPersonView = value;
            }
        }

        bool IMyCameraController.ForceFirstPersonCamera
        {
            get
            {
                return ForceFirstPersonCamera;
            }
            set
            {
                ForceFirstPersonCamera = value;
            }
        }

        bool IMyCameraController.HandleUse()
        {
            return false;
        }

        bool IMyModdingControllableEntity.ForceFirstPersonCamera
        {
            get
            {
                return ForceFirstPersonCamera;
            }
            set
            {
                ForceFirstPersonCamera = value;
            }
        }

        bool IMyCameraController.AllowCubeBuilding
        {
            get
            {
                return true;
            }
        }

        MatrixD Sandbox.ModAPI.Interfaces.IMyControllableEntity.GetHeadMatrix(bool includeY, bool includeX, bool forceHeadAnim, bool forceHeadBone)
        {
            return GetHeadMatrix(includeY, includeX, forceHeadAnim);
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.MoveAndRotate(Vector3 moveIndicator, Vector2 rotationIndicator, float rollIndicator)
        {
            MoveAndRotate(moveIndicator, rotationIndicator, rollIndicator);
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.MoveAndRotateStopped()
        {
            MoveAndRotateStopped();
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.Use()
        {
            Use();
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.UseContinues()
        {
            UseContinues();
        }

        void IMyControllableEntity.UseFinished()
        {
            UseFinished();
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.Jump()
        {
            Jump();
        }

        void IMyControllableEntity.Sprint()
        {
            Sprint();
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.Up()
        {
            Up();
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.Crouch()
        {
            Crouch();
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.Down()
        {
            Down();
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.ShowInventory()
        {
            ShowInventory();
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.ShowTerminal()
        {
            ShowTerminal();
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.SwitchThrusts()
        {
		    var jetpack = JetpackComp;
		    if (jetpack != null)
			    jetpack.SwitchThrusts();
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.SwitchDamping()
        {
		    var jetpack = JetpackComp;
			if(jetpack != null)
				jetpack.SwitchDamping();
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.SwitchLights()
        {
            SwitchLights();
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.SwitchLeadingGears()
        {
            SwitchLeadingGears();
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.SwitchReactors()
        {
            SwitchReactors();
        }

        bool Sandbox.ModAPI.Interfaces.IMyControllableEntity.EnabledThrusts
        {
            get { return JetpackComp != null && JetpackComp.TurnedOn; }
        }

        bool Sandbox.ModAPI.Interfaces.IMyControllableEntity.EnabledDamping
        {
            get { return JetpackComp != null && JetpackComp.DampenersTurnedOn; }
        }

        bool Sandbox.ModAPI.Interfaces.IMyControllableEntity.EnabledLights
        {
            get { return LightEnabled; }
        }

        bool Sandbox.ModAPI.Interfaces.IMyControllableEntity.EnabledLeadingGears
        {
            get { return false; }
        }

        bool Sandbox.ModAPI.Interfaces.IMyControllableEntity.EnabledReactors
        {
            get { return false; }
        }

        bool IMyControllableEntity.EnabledBroadcasting
        {
            get { return m_radioBroadcaster.Enabled; }
        }

        bool Sandbox.ModAPI.Interfaces.IMyControllableEntity.EnabledHelmet
        {
            get { return OxygenComponent.EnabledHelmet; }
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.SwitchHelmet()
        {
            OxygenComponent.SwitchHelmet();
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.Die()
        {
            Die();
        }

        void IMyDestroyableObject.OnDestroy()
        {
            OnDestroy();
        }

        void IMyDestroyableObject.DoDamage(float damage, MyStringHash damageType, bool sync, MyHitInfo? hitInfo, long attackerId)
        {
            DoDamage(damage, damageType, sync, attackerId);
        }

        float IMyDestroyableObject.Integrity
        {
            get { return Integrity; }
        }

        public bool PrimaryLookaround
        {
            get { return false; }
        }

        class MyCharacterPosition : MyPositionComponent
        {
            const int CHECK_FREQUENCY = 20;
            int m_checkOutOfWorldCounter = 0;
            protected override void OnWorldPositionChanged(object source)
            {
                ClampToWorld();
                base.OnWorldPositionChanged(source);
            }

            private void ClampToWorld()
            {
                if (MyPerGameSettings.LimitedWorld)
                {
                    m_checkOutOfWorldCounter++;
                    if (m_checkOutOfWorldCounter > CHECK_FREQUENCY)
                    {
                        var pos = GetPosition();
                        var min = MySession.Static.WorldBoundaries.Min;
                        var max = MySession.Static.WorldBoundaries.Max;
                        var vMinTen = pos - Vector3.One * 10;
                        var vPlusTen = pos + Vector3.One * 10;
                        if (!(vMinTen.X < min.X || vMinTen.Y < min.Y || vMinTen.Z < min.Z || vPlusTen.X > max.X || vPlusTen.Y > max.Y || vPlusTen.Z > max.Z))
                        {
                            m_checkOutOfWorldCounter = 0;
                            return;
                        }
                        var velocity = Container.Entity.Physics.LinearVelocity;
                        bool clamp = false;
                        if(pos.X < min.X || pos.X > max.X)
                        {
                            clamp = true;
                            velocity.X = 0;
                        }
                        if(pos.Y < min.Y ||  pos.Y > max.Y)
                        {
                            clamp = true;
                            velocity.Y = 0;
                        }
                        if (pos.Z < min.Z || pos.Z > max.Z)
                        {
                            clamp = true;
                            velocity.Z = 0;
                        }
                        if(clamp)
                        {
                            m_checkOutOfWorldCounter = 0; //set position will send us to this function again so dont check twice
                            SetPosition(Vector3.Clamp(pos, min, max));
                            Container.Entity.Physics.LinearVelocity = velocity;
                        }
                        m_checkOutOfWorldCounter = CHECK_FREQUENCY; //recheck next frame
                    }
                }
            }
        }

        public static void Preload()
        {
            var animations = MyDefinitionManager.Static.GetAnimationDefinitions();
            foreach (var animation in animations)
            {
                string model = ((MyAnimationDefinition)animation).AnimationModel;
                if (!string.IsNullOrEmpty(model))
                {
                    MyModel animationModel = MyModels.GetModelOnlyAnimationData(model);
                }
            }

            if (VRage.Import.MyModelImporter.LINEAR_KEYFRAME_REDUCTION_STATS)
            {
                var stats = VRage.Import.MyModelImporter.ReductionStats;

                List<float> improvements = new List<float>();
                foreach (var animation in stats)
                {
                    foreach (var bone in animation.Value)
                    {
                        improvements.Add(bone.OptimizedKeys / (float)bone.OriginalKeys);
                    }
                }

                float overallReduction = improvements.Average();
            }

			MyCharacterSoundComponent.Preload();
        }

        #region Movement properties
		public MyCharacterMovementFlags MovementFlags { get { return m_movementFlags; } }
        public bool WantsJump
        {
            get { return (m_movementFlags & MyCharacterMovementFlags.Jump) == MyCharacterMovementFlags.Jump; }
            private set
            {
                if (value)
                    m_movementFlags |= MyCharacterMovementFlags.Jump;
                else
                    m_movementFlags &= ~MyCharacterMovementFlags.Jump;
            }
        }
        bool WantsSprint
        {
            get { return (m_movementFlags & MyCharacterMovementFlags.Sprint) == MyCharacterMovementFlags.Sprint; }
            set
            {
                if (value)
                    m_movementFlags |= MyCharacterMovementFlags.Sprint;
                else
                    m_movementFlags &= ~MyCharacterMovementFlags.Sprint;
            }
        }
        public bool WantsWalk
        {
            get { return (m_movementFlags & MyCharacterMovementFlags.Walk) == MyCharacterMovementFlags.Walk; }
            private set
            {
                if (value)
                    m_movementFlags |= MyCharacterMovementFlags.Walk;
                else
                    m_movementFlags &= ~MyCharacterMovementFlags.Walk;
            }
        }
        bool WantsFlyUp
        {
            get { return (m_movementFlags & MyCharacterMovementFlags.FlyUp) == MyCharacterMovementFlags.FlyUp; }
            set
            {
                if (value)
                    m_movementFlags |= MyCharacterMovementFlags.FlyUp;
                else
                    m_movementFlags &= ~MyCharacterMovementFlags.FlyUp;
            }
        }
        bool WantsFlyDown
        {
            get { return (m_movementFlags & MyCharacterMovementFlags.FlyDown) == MyCharacterMovementFlags.FlyDown; }
            set
            {
                if (value)
                    m_movementFlags |= MyCharacterMovementFlags.FlyDown;
                else
                    m_movementFlags &= ~MyCharacterMovementFlags.FlyDown;
            }
        }
        bool WantsCrouch
        {
            get { return (m_movementFlags & MyCharacterMovementFlags.Crouch) == MyCharacterMovementFlags.Crouch; }
            set
            {
                if (value)
                    m_movementFlags |= MyCharacterMovementFlags.Crouch;
                else
                    m_movementFlags &= ~MyCharacterMovementFlags.Crouch;
            }
        }

        #endregion

		private MyCharacterBreath m_breath;
		public MyCharacterBreath Breath { get { return m_breath; } }

        bool IMyUseObject.HandleInput() 
        {
            MyCharacterDetectorComponent detectorComponent = Components.Get<MyCharacterDetectorComponent>();

            if (detectorComponent != null && detectorComponent.UseObject != null)
            {
                return detectorComponent.UseObject.HandleInput();
            }

            return false;
        }

        public float CharacterAccumulatedDamage { get; set; }

        public MyEntityCameraSettings GetCameraEntitySettings()
        {
            return m_cameraSettingsWhenAlive;
        }

        public MyStringId ControlContext
        {
            get 
            {
                if (MyCubeBuilder.Static.IsBuildMode)
                    return MySpaceBindingCreator.CX_BUILD_MODE;
                else if (MySessionComponentVoxelHand.Static.BuildMode)
                    return MySpaceBindingCreator.CX_VOXEL;
                else
                    return MySpaceBindingCreator.CX_CHARACTER; 
            }
        }

        void ResetMovement()
        {

            MoveIndicator = Vector3.Zero;
            RotationIndicator = Vector2.Zero;
            Roll = 0.0f;

        }

        #region ModAPI
        float IMyCharacter.EnvironmentOxygenLevel
        {
            get { return OxygenComponent.EnvironmentOxygenLevel; }
        }

        #endregion

        //public bool IsUseObjectOfType<T>()
        //{
        //    return UseObject is T;
        //}

        public MyEntity ManipulatedEntity;
        private MyGuiScreenBase m_InventoryScreen;
    }
}
