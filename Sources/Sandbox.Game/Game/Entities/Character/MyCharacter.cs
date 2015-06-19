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
using Sandbox.Game.Entities.Interfaces;
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
using VRage;
using VRage.Audio;
using VRage.Components;
using VRage.FileSystem;
using VRage.Game.Entity.UseObject;
using VRage.Game.ObjectBuilders;
using VRage.Input;
using VRage.Library.Utils;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using VRageRender;
using IMyModdingControllableEntity = Sandbox.ModAPI.Interfaces.IMyControllableEntity;

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

    enum MyZoomModeEnum
    {
        Classic,
        IronSight,
    }

    enum MyWalkingSurfaceType
    {
        None,
        Rock,
        Metal,
        Unknown
    }

    enum CharacterSoundsEnum
    {
        NONE_SOUND,
        JUMP_SOUND,

        WALK_ROCK_SOUND,
        WALK_METAL_SOUND,
        SPRINT_ROCK_SOUND,
        SPRINT_METAL_SOUND,

        FALL_ROCK_SOUND,
        FALL_METAL_SOUND,

        JETPACK_IDLE_SOUND,
        JETPACK_RUN_SOUND,

        CROUCH_DOWN_SOUND,
        CROUCH_UP_SOUND,
        CROUCH_RUN_ROCK_SOUND,
        CROUCH_RUN_METAL_SOUND,

        PAIN_SOUND,
        DEATH,

        IRONSIGHT_ACT_SOUND,
        IRONSIGHT_DEACT_SOUND,
        RUN_ROCK_SOUND,
        RUN_METAL_SOUND,
        WALK_GRASS_SOUND,
        WALK_WOOD_SOUND,
        RUN_WOOD_SOUND,
        RUN_GRASS_SOUND,
        SPRINT_GRASS_SOUND,
        SPRINT_WOOD_SOUND,
        FALL_WOOD_SOUND,
        FALL_GRASS_SOUND,
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
        IMyComponentOwner<MyComponentInventoryAggregate>, 
        IMyCameraController, 
        IMyControllableEntity, 
        IMyInventoryOwner, 
        IMyPowerConsumer, 
        IMyComponentOwner<MyDataBroadcaster>, 
        IMyComponentOwner<MyDataReceiver>, 
        IMyUseObject, 
        IMyDestroyableObject, 
        Sandbox.ModAPI.IMyCharacter
    {
        #region Fields

        float m_cameraDistance = 0.0f;

        public const float CAMERA_NEAR_DISTANCE = 60.0f;   

        const float CHARACTER_GRAVITY_MULTIPLIER = 2.0f;
        const float CHARACTER_X_ROTATION_SPEED = 0.13f;
        const float CHARACTER_Y_ROTATION_SPEED = 0.0026f;

        public static float MINIMAL_SPEED = 0.001f;

        const float JumpTime = 1; //m/ss

        const float ShotTime = 0.1f;  //s
        const float ZoomTime = 0.1f;  //s
        float m_currentShotTime = 0;
        float m_currentShootPositionTime = 0;
        float m_currentZoomTime = 0;
        const float FallTime = 0.3f; //s

        const float DefaultBlendTime = 0.5f;

        const float RespawnTime = 5.0f; //s

        public static float CharacterWidth = 1.0f;
        public static float CharacterHeight = 1.80f;
        public static float CrouchHeight = 1.25f;

        public static MyHudNotification OutOfAmmoNotification;

        /// <summary>
        /// Interaction half angle, 90 degrees means deviation 90 degrees from forward vector
        /// </summary>
        public static readonly float INTERACTION_HALF_COS_ANGLE = (float)Math.Cos(MathHelper.ToRadians(120));

        // Right, Up, Backward
        static readonly Vector3 WeaponIronsightTranslation = new Vector3(0.0f, -0.11f, -0.22f);
        static readonly Vector3 ToolIronsightTranslation = new Vector3(0.0f, -0.21f, -0.25f);
        static readonly Vector3 WeaponClassicTranslation = new Vector3(0.1f, -0.18f, -0.22f);

        List<MyPhysics.HitInfo> m_hits = new List<MyPhysics.HitInfo>();

        float m_currentSpeed = 0;
        float m_currentDecceleration = 0;
        float m_currentJump = 0;
        bool m_canJump = true;
        float m_currentWalkDelay = 0;

        float AUTO_ENABLE_JETPACK_INTERVAL = 1; //s
        float m_currentAutoenableJetpackDelay = 0;

        public bool DebugMode = false;
        public bool AIMode = false;

        const float MinHeadLocalXAngle = -80;
        const float MaxHeadLocalXAngle = 85;
        const float MinHeadLocalYAngle = 0;
        const float MaxHeadLocalYAngle = 0;
        float m_headLocalXAngle = 0;
        float m_headLocalYAngle = 0;
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
        int m_weaponBone = -1;
        int m_leftHandItemBone = -1;
        int m_rightHandItemBone = -1;
        int m_spineBone = -1;
        int m_rootBone = 0;
        int m_leftHipBone = -1;
        int m_leftKneeBone = -1;
        int m_leftAnkleBone = -1;
        int m_rightHipBone = -1;
        int m_rightKneeBone = -1;
        int m_rightAnkleBone = -1;

        public int WeaponBone { get { return m_weaponBone; } }

        float m_currentAnimationChangeDelay = 0;
        float SAFE_DELAY_FOR_ANIMATION_BLEND = 0.1f;

        MyCharacterMovementEnum m_currentMovementState = MyCharacterMovementEnum.Standing;
        MyCharacterMovementEnum m_previousMovementState = MyCharacterMovementEnum.Standing;
        bool m_wasFlying = false;//bacause m_previousMovementState changes several times before reaching sound stage
		public event CharacterMovementStateDelegate OnMovementStateChanged;

        public event Action<IMyHandheldGunObject<MyDeviceBase>> WeaponEquiped;
        IMyHandheldGunObject<MyDeviceBase> m_currentWeapon;
        MyEntity m_leftHandItem;
        MyHandItemDefinition m_handItemDefinition;
        MyZoomModeEnum m_zoomMode = MyZoomModeEnum.Classic;
        float m_currentHandItemWalkingBlend = 0;
        float m_currentHandItemShootBlend = 0;
        float m_currentScatterBlend = 0;
        Vector3 m_currentScatterPos;
        Vector3 m_lastScatterPos;


        //ulong m_actualUpdateFrame = 0;
        //ulong m_actualDrawFrame = 0;
        //ulong m_transformedBonesFrame = 0;
        //bool m_characterBonesReady = false;

        //0 head
        //1 body
        //2-3 left arm
        //4-5 right arm
        //6-7 left leg
        //8-9 right leg
        CapsuleD[] m_bodyCapsules = new CapsuleD[1];//new CapsuleD[10];
        MatrixD m_headMatrix = MatrixD.CreateTranslation(0, 1.65, 0);

        
        MyHudNotification m_pickupObjectNotification;        
        MyHudNotification m_inertiaDampenersNotification;
        MyHudNotification m_broadcastingNotification;
        MyHudNotification m_jetpackToggleNotification;
        
        HkCharacterStateType m_currentCharacterState;
        bool m_isFalling = false;
        bool m_isFallingAnimationPlayed = false;
        float m_currentFallingTime = 0;
        bool m_crouchAfterFall = false;

        MyCharacterMovementFlags m_movementFlags;
        bool m_isFlying;

        public static float InventoryVolume = 0.4f;
        public static float InventoryMass = 1000000f;
        static Vector3 InventorySize = new Vector3(1.2f, 0.7f, 0.4f);

        string m_characterModel;
        MyInventory m_inventory;
        MyBattery m_suitBattery;
        MyPowerDistributor m_suitPowerDistributor;
        List<MyPhysicalInventoryItem> m_inventoryResults = new List<MyPhysicalInventoryItem>();

        bool m_dampenersEnabled = true;
        bool m_jetpackEnabled = false;

        MyEntity m_topGrid;
        MyEntity m_usingEntity;

        bool m_enableBag = true;

        //Light
        public static float REFLECTOR_RANGE = 60;
        public static float REFLECTOR_CONE_ANGLE = 0.373f;
        public static float REFLECTOR_BILLBOARD_LENGTH = 40f;
        public static float REFLECTOR_BILLBOARD_THICKNESS = 6f;

        public static Vector4 REFLECTOR_COLOR = Vector4.One;
        public static float REFLECTOR_INTENSITY = 1;
        public static Vector4 POINT_COLOR = Vector4.One;
        public static Vector4 POINT_COLOR_SPECULAR = Vector4.One;
        public static float POINT_LIGHT_RANGE = 1.231f;
        public static float POINT_LIGHT_INTENSITY = 0.464f;
        public static float REFLECTOR_DIRECTION = -3.5f;

        public static float LIGHT_GLARE_MAX_DISTANCE = 40;

        float m_currentLightPower = 0; //0..1
        public float CurrentLightPower { get { return m_currentLightPower; } }
        float m_lightPowerFromProducer = 0;
        float m_lightTurningOnSpeed = 0.05f;
        float m_lightTurningOffSpeed = 0.05f;
        bool m_lightEnabled = true;

        float m_jetpackPowerFromProducer;

        BoundingBoxD m_actualWorldAABB;
        BoundingBoxD m_aabb;

        //Needed to check relation between character and remote players when controlling a remote control
        private MyEntityController m_oldController;

        float m_currentHeadAnimationCounter = 0;

        float m_currentLocalHeadAnimation = -1;
        float m_localHeadAnimationLength = -1;
        Vector2? m_localHeadAnimationX = null;
        Vector2? m_localHeadAnimationY = null;

        List<List<int>> m_bodyCapsuleBones = new List<List<int>>();
         
        float m_currentRotationDelay = 0;
        float m_currentRotationSkipDelay = 0;

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

        long m_moveAndRotateCounter = 0;
        long m_updateCounter = 0;

        private MyEntity3DSoundEmitter m_soundEmitter;
        private MyEntity3DSoundEmitter m_secondarySoundEmitter; //shouldnt play any loops

        private int m_lastScreamTime;
        const int SCREAM_DELAY_MS = 800;

        private MyWalkingSurfaceType m_walkingSurfaceType = MyWalkingSurfaceType.None;

        Queue<Vector3> m_bobQueue = new Queue<Vector3>();

        private bool m_dieAfterSimulation;

        MyRadioReceiver m_radioReceiver;
        MyRadioBroadcaster m_radioBroadcaster;

        //public bool EnableBroadcast = true;

        float m_currentLootingCounter = 0;
        MyEntityCameraSettings m_cameraSettingsWhenAlive;

        public StringBuilder CustomNameWithFaction { get; private set; }

        public float EnvironmentOxygenLevel;

        private float m_suitOxygenAmount;
        public float SuitOxygenAmount
        {
            get
            {
                return m_suitOxygenAmount;
            }
            set
            {
                m_suitOxygenAmount = value;
                if (m_suitOxygenAmount > Definition.OxygenCapacity)
                {
                    m_suitOxygenAmount = Definition.OxygenCapacity;
                }
            }
        }
        public float SuitOxygenAmountMissing
        {
            get
            {
                return Definition.OxygenCapacity - SuitOxygenAmount;
            }
        }
        public float SuitOxygenLevel
        {
            get
            {
                if (Definition.OxygenCapacity == 0)
                {
                    return 0;
                }
                return m_suitOxygenAmount / Definition.OxygenCapacity;
            }
            set
            {
                m_suitOxygenAmount = value * Definition.OxygenCapacity;
            }
        }
        private float m_oldSuitOxygenLevel;
        bool m_needsOxygen;

        public static readonly float LOW_OXYGEN_RATIO = 0.2f;
        MyHudNotification m_lowOxygenNotification;
        MyHudNotification m_criticalOxygenNotification;
        MyHudNotification m_oxygenBottleRefillNotification;
        MyHudNotification m_helmetToggleNotification;

		MyCharacterStatComponent m_stats;
		public MyCharacterStatComponent StatComp { get { return m_stats; } set { Components.Add<MyCharacterStatComponent>(value); m_stats = value; } }

		public static float LOW_HEALTH_RATIO { get { return MyCharacterStatComponent.LOW_HEALTH_RATIO; } }

		int m_healthPassiveEffectId;

		int m_staminaPassiveEffectId;

		int m_foodPassiveEffectId;

		public float Health
		{
			get { var health = StatComp.Health; return health != null ? health.Value : Definition.MaxHealth; }
		}

		public float MaxHealth
		{
			get { var health = StatComp.Health; return health != null ? health.MaxValue : Definition.MaxHealth; }
		}

		public float MinHealth
		{
			get { var health = StatComp.Health; return health != null ? health.MinValue : 0; }
		}

		public float HealthRatio
		{
			get { return Health / MaxHealth; }
		}

        bool m_useAnimationForWeapon = false;
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

        public bool IsRagdollActivated
        {
            get         
            {
                if (Physics == null) return false;                
                return this.Physics.IsRagdollModeActive;
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
                ResetHeadRotation();
            }
        }

        private float m_switchBackToSpectatorTimer;
        private float m_switchBackToFirstPersonTimer;
        private const float m_cameraSwitchDelay = 0.2f;

        private bool m_forceFirstPersonCamera;
        public bool ForceFirstPersonCamera
        {
            get { return m_forceFirstPersonCamera; }
            set { m_forceFirstPersonCamera = value; }
        }

        public bool CanDrawThrusts()
        {
            if (m_actualUpdateFrame < 2)
            {
                return false;
            }
            return true;
        }
        public bool UpdateCalled()
        {
            bool updateCalled = m_actualUpdateFrame != m_actualDrawFrame;
            m_actualDrawFrame = m_actualUpdateFrame;
            return updateCalled;
        }

        internal new MyRenderComponentCharacter Render
        {
            get { return (MyRenderComponentCharacter)base.Render; }
            set { base.Render = value; }
        }

        public bool IsCameraNear
        {
            get
            {
                if (MyFakes.ENABLE_PERMANENT_SIMULATIONS_COMPUTATION) return true;
                return Render.IsVisible() && m_cameraDistance <= CAMERA_NEAR_DISTANCE;
            }
        }

        public MyRagdollMapper RagdollMapper;

        public event EventHandler OnWeaponChanged;

        public event Action<MyCharacter> CharacterDied;

        private MyComponentInventoryAggregate m_inventoryAggregate;
        public MyComponentInventoryAggregate InventoryAggregate
        {
            get
            {
                return m_inventoryAggregate;
            }
            set 
            {
                if (m_inventoryAggregate == null)
                {
                    Components.Add<MyComponentInventoryAggregate>(value);
                }
                else
                {
                    Components.Remove<MyComponentInventoryAggregate>();
                }
                m_inventoryAggregate = value;
            }
        }

        #endregion

        #region Init

        private static readonly Dictionary<int, MySoundPair> CharacterSounds = new Dictionary<int, MySoundPair>()
        {
            { (int)CharacterSoundsEnum.NONE_SOUND, new MySoundPair() },
            { (int)CharacterSoundsEnum.JUMP_SOUND, new MySoundPair("PlayJump") },
            { (int)CharacterSoundsEnum.WALK_ROCK_SOUND, new MySoundPair("PlayWalkRock") },
            { (int)CharacterSoundsEnum.WALK_METAL_SOUND, new MySoundPair("PlayWalkMetal") },
            //{ (int)CharacterSoundsEnum.WALK_GRASS_SOUND, new MySoundPair("PlayWalkGrass") },
            //{ (int)CharacterSoundsEnum.WALK_WOOD_SOUND, new MySoundPair("PlayWalkWood") },
            { (int)CharacterSoundsEnum.RUN_ROCK_SOUND, new MySoundPair("PlayRunRock") },
            { (int)CharacterSoundsEnum.RUN_METAL_SOUND, new MySoundPair("PlayRunMetal") },
            //{ (int)CharacterSoundsEnum.RUN_GRASS_SOUND, new MySoundPair("PlayRunGrass") },
            //{ (int)CharacterSoundsEnum.RUN_WOOD_SOUND, new MySoundPair("PlayRunWood") },
            { (int)CharacterSoundsEnum.SPRINT_ROCK_SOUND, new MySoundPair("PlaySprintRock") },
            { (int)CharacterSoundsEnum.SPRINT_METAL_SOUND, new MySoundPair("PlaySprintMetal") },
            //{ (int)CharacterSoundsEnum.SPRINT_GRASS_SOUND, new MySoundPair("PlaySprintGrass") },
            //{ (int)CharacterSoundsEnum.SPRINT_WOOD_SOUND, new MySoundPair("PlaySprintWood") },

            { (int)CharacterSoundsEnum.FALL_ROCK_SOUND, new MySoundPair("PlayFallRock") },
            { (int)CharacterSoundsEnum.FALL_METAL_SOUND, new MySoundPair("PlayFallMetal") },
            //{ (int)CharacterSoundsEnum.FALL_GRASS_SOUND, new MySoundPair("PlayFallGrass") },
            //{ (int)CharacterSoundsEnum.FALL_WOOD_SOUND, new MySoundPair("PlayFallWood") },

            { (int)CharacterSoundsEnum.JETPACK_IDLE_SOUND, new MySoundPair("PlayJet") },
            { (int)CharacterSoundsEnum.JETPACK_RUN_SOUND, new MySoundPair("PlayJetRun") },

            { (int)CharacterSoundsEnum.CROUCH_DOWN_SOUND, new MySoundPair("PlayCrouchDwn") },
            { (int)CharacterSoundsEnum.CROUCH_UP_SOUND, new MySoundPair("PlayCrouchUp") },
            { (int)CharacterSoundsEnum.CROUCH_RUN_ROCK_SOUND, new MySoundPair("PlayCrouchRock") },
            { (int)CharacterSoundsEnum.CROUCH_RUN_METAL_SOUND, new MySoundPair("PlayCrouchMetal") },

            { (int)CharacterSoundsEnum.PAIN_SOUND, new MySoundPair("PlayVocPain") },

            { (int)CharacterSoundsEnum.IRONSIGHT_ACT_SOUND, new MySoundPair("PlayIronSightActivate") },
            { (int)CharacterSoundsEnum.IRONSIGHT_DEACT_SOUND, new MySoundPair("PlayIronSightDeactivate") },
        };

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
                ColorMaskHSV = m_defaultColors[MyUtils.GetRandomInt(0, 7)]
            };
        }

        public MyCharacter()
        {
            ControllerInfo.ControlAcquired += OnControlAcquired;
            ControllerInfo.ControlReleased += OnControlReleased;
            m_soundEmitter = new MyEntity3DSoundEmitter(this);
            m_secondarySoundEmitter = new MyEntity3DSoundEmitter(this);
            m_radioReceiver = new MyRadioReceiver(this);
            m_radioBroadcaster = new MyRadioBroadcaster(this);
            m_radioBroadcaster.BroadcastRadius = 200;
            CustomNameWithFaction = new StringBuilder();
            PositionComp = new MyCharacterPosition();
            (PositionComp as MyPositionComponent).WorldPositionChanged = WorldPositionChanged;
            this.Render = new MyRenderComponentCharacter();
			StatComp = new MyCharacterStatComponent();

            // TODO: When this Inventory system is working well, remove it and use it as default for SE too
            if (MyFakes.ENABLE_MEDIEVAL_INVENTORY)
            {
                InventoryAggregate = new MyComponentInventoryAggregate(this);
            }

            AddDebugRenderComponent(new MyDebugRenderComponentCharacter(this));

            Components.Add<MyCharacterDetectorComponent>(new MyCharacterRaycastDetectorComponent());
        }

        /// <summary>
        /// Backwards compatibility for old character model.
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        private string GetRealModel(string asset, Vector3 colorMask)
        {
            if (MyObjectBuilder_Character.CharacterModels.ContainsKey(asset))
            {
                Render.ColorMaskHsv = MyObjectBuilder_Character.CharacterModels[asset];
                asset = DefaultModel;
            }
            return asset;
        }
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            SyncFlag = true;
            base.Init(objectBuilder);

            MyObjectBuilder_Character characterOb = (MyObjectBuilder_Character)objectBuilder;

            SyncObject.Tick();
            SyncObject.UpdatePosition();

            m_suitBattery = new MyBattery(this);
            m_suitBattery.Init(characterOb.Battery);

            var receiver = new MyPowerReceiver(
                MyConsumerGroupEnum.Utility,
                false,
                MyEnergyConstants.REQUIRED_INPUT_LIFE_SUPPORT + MyEnergyConstants.REQUIRED_INPUT_CHARACTER_LIGHT + MyEnergyConstants.REQUIRED_INPUT_JETPACK,
                ComputeRequiredPower);
            receiver.CurrentInputChanged += delegate
            {
                SetPowerInput(receiver.CurrentInput);
            };

            receiver.Update();
            PowerReceiver = receiver;

            m_suitPowerDistributor = new MyPowerDistributor();
            m_suitPowerDistributor.AddProducer(m_suitBattery);

            m_suitPowerDistributor.AddConsumer(this);

            Render.ColorMaskHsv = characterOb.ColorMaskHSV;
            m_characterModel = GetRealModel(characterOb.CharacterModel, characterOb.ColorMaskHSV);

            if (!MyDefinitionManager.Static.Characters.TryGetValue(m_characterModel, out m_characterDefinition))
            {
                //System.Diagnostics.Debug.Fail("Character model " + m_characterModel + " not found!");
                m_characterDefinition = MyDefinitionManager.Static.Characters.First();
                m_characterModel = m_characterDefinition.Model;
            }

            CharacterHeight = m_characterDefinition.CharacterHeight;
            CharacterWidth = m_characterDefinition.CharacterWidth;

            m_radioBroadcaster.WantsToBeEnabled = characterOb.EnableBroadcasting && Definition.VisibleOnHud;
            if (MyFakes.ENABLE_BATTLE_SYSTEM && MySession.Static.Battle)
            {
                m_radioBroadcaster.Enabled = false;
                m_radioBroadcaster.WantsToBeEnabled = false;
            }

            Init(new StringBuilder(characterOb.DisplayName), m_characterDefinition.Model, null, null);
            Render.EnableColorMaskHsv = true;

            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
            Render.NeedsDraw = true;
            Render.CastShadows = true;
            Render.NeedsResolveCastShadow = false;
            Render.SkipIfTooSmall = false;

            PositionComp.LocalAABB = new BoundingBox(-new Vector3(0.3f, 0.0f, 0.3f), new Vector3(0.3f, 1.8f, 0.3f));

            m_currentLootingCounter = characterOb.LootingCounter;

            if (m_currentLootingCounter <= 0)
                UpdateCharacterPhysics(!characterOb.AIMode);

            m_currentMovementState = characterOb.MovementState;

            InitAnimations();
            ValidateBonesProperties();
            CalculateTransforms();

            if (m_currentLootingCounter > 0)
            {
                InitDeadBodyPhysics();
                if (m_currentMovementState != MyCharacterMovementEnum.Died) SetCurrentMovementState(MyCharacterMovementEnum.Died);                
                SwitchAnimation(MyCharacterMovementEnum.Died, false);
            }

            InventoryVolume = m_characterDefinition.InventoryVolume;
            InventoryMass = m_characterDefinition.InventoryMass;
            InventorySize = new Vector3(m_characterDefinition.InventorySizeX, m_characterDefinition.InventorySizeY, m_characterDefinition.InventorySizeZ);
            m_inventory = new MyInventory(InventoryVolume, InventoryMass, InventorySize, 0, this);
            m_inventory.Init(characterOb.Inventory);
            m_inventory.ContentsChanged += inventory_OnContentsChanged;
            m_inventory.ContentsChanged += MyToolbarComponent.CurrentToolbar.CharacterInventory_OnContentsChanged;

            Physics.Enabled = true;

            if (MyFakes.ENABLE_CHARACTER_VIRTUAL_PHYSICS)
            {
                VirtualPhysics = new MyControlledPhysicsBody(this, RigidBodyFlag.RBF_KINEMATIC);
                var massProperties = HkInertiaTensorComputer.ComputeSphereVolumeMassProperties(0.1f, Definition.Mass);
                HkShape sh = new HkSphereShape(0.1f);
                VirtualPhysics.InitialSolverDeactivation = HkSolverDeactivation.Off;
                VirtualPhysics.CreateFromCollisionObject(sh, Vector3.Zero, WorldMatrix, massProperties, Sandbox.Engine.Physics.MyPhysics.NoCollisionLayer);
                VirtualPhysics.RigidBody.EnableDeactivation = false;
                sh.RemoveReference();

                VirtualPhysics.Enabled = true;
            }

            SetHeadLocalXAngle(characterOb.HeadAngle.X);
            SetHeadLocalYAngle(characterOb.HeadAngle.Y);

            Render.InitLight(m_characterDefinition);
            Render.InitJetpackThrusts(m_characterDefinition);

            InitWeapon(characterOb.HandWeapon);

            m_lightEnabled = characterOb.LightEnabled;

			if ((MySession.Static.SurvivalMode && MyPerGameSettings.Game == GameEnum.ME_GAME) || MySession.Static.Battle)
                m_jetpackEnabled = false;
            else
                m_jetpackEnabled = m_characterDefinition.JetpackAvailable ? characterOb.JetpackEnabled : false;

            m_dampenersEnabled = characterOb.DampenersEnabled;

            RecalculatePowerRequirement(true);

            EnableJetpack(m_jetpackEnabled, true, true, true);
            if (m_currentMovementState == MyCharacterMovementEnum.Flying)
                m_wasFlying=true;

            Physics.LinearVelocity = characterOb.LinearVelocity;

            m_currentAutoenableJetpackDelay = characterOb.AutoenableJetpackDelay;

            if (Physics.CharacterProxy != null)
            {
                Physics.CharacterProxy.ContactPointCallbackEnabled = true;
                Physics.CharacterProxy.ContactPointCallback += RigidBody_ContactPointCallback;
            }

            Render.UpdateLightProperties(m_currentLightPower);

            IsInFirstPersonView = characterOb.IsInFirstPersonView || MySession.Static.Settings.Enable3rdPersonView == false;

            MyToolbarComponent.CharacterToolbar.ItemChanged += Toolbar_ItemChanged;

			m_breath = new MyCharacterBreath(this);

			if (m_stats != null)
			{
				MyStatsDefinition statsDefinition = null;
				if(MyDefinitionManager.Static.TryGetDefinition(new MyDefinitionId(typeof(MyObjectBuilder_StatsDefinition), Definition.Stats), out statsDefinition))
					m_stats.InitStats(statsDefinition);
			}

			var health = StatComp.Health;
			if (health != null && characterOb.Health.HasValue)
				health.Value = characterOb.Health.Value;
			m_breath.ForceUpdate();

            System.Diagnostics.Debug.Assert(Health > 0 && m_currentLootingCounter <= 0 || m_currentLootingCounter > 0);

            // Ragdoll
            if (Physics != null && MyPerGameSettings.EnableRagdollModels)
            {
                InitRagdoll();
            }

            if ((Definition.RagdollBonesMappings.Count > 1) && (MyPerGameSettings.EnableRagdollModels) && Physics.Ragdoll != null)
            {                
               InitRagdollMapper();               
            }

            if (IsDead && MyPerGameSettings.EnableRagdollModels  && Physics != null && Physics.Ragdoll != null && RagdollMapper != null)
            {
                InitDeadBodyPhysics();
            }

            if (MySession.Static.SurvivalMode)
            {
                m_suitOxygenAmount = characterOb.OxygenLevel * Definition.OxygenCapacity;
            }
            else
            {
                m_suitOxygenAmount = Definition.OxygenCapacity;
            }
            m_oldSuitOxygenLevel = SuitOxygenLevel;

            m_oxygenBottleRefillNotification = new MyHudNotification(text: MySpaceTexts.NotificationBottleRefill, level: MyNotificationLevel.Important);
            m_lowOxygenNotification = new MyHudNotification(text: MySpaceTexts.NotificationOxygenLow, font: MyFontEnum.Red, level: MyNotificationLevel.Important);
            m_criticalOxygenNotification = new MyHudNotification(text: MySpaceTexts.NotificationOxygenCritical, font: MyFontEnum.Red, level: MyNotificationLevel.Important);
            m_broadcastingNotification = new MyHudNotification();
            m_inertiaDampenersNotification = new MyHudNotification();
            m_jetpackToggleNotification = new MyHudNotification();
            m_helmetToggleNotification = m_helmetToggleNotification ?? new MyHudNotification(); // Init() is called when toggling helmet so this check is required

            m_needsOxygen = Definition.NeedsOxygen;

            if (Definition.RagdollBonesMappings.Count > 0) 
                CreateBodyCapsulesForHits(Definition.RagdollBonesMappings);
            else
                m_bodyCapsuleBones.Clear();
            InitSounds();

            if (InventoryAggregate != null) InventoryAggregate.Init();
        }

        private void InitSounds()
        {
            CharacterSounds[(int)CharacterSoundsEnum.DEATH] = new MySoundPair(Definition.DeathSoundName);
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
        }

        /// <summary>
        /// Loads Ragdoll data
        /// </summary>
        /// <param name="ragDollFile"></param>
        public void InitRagdoll()
        {
            //if (!Sync.IsServer) return;
            if (Physics.Ragdoll != null)
            {
                Physics.CloseRagdollMode();
                Physics.Ragdoll.ResetToRigPose();
                Physics.Ragdoll.SetToKeyframed();                
                //Physics.CloseRagdoll();
                //Physics.Ragdoll = null;
                return;
            }

            Physics.Ragdoll = new HkRagdoll();

            bool dataLoaded = false;
            if (Model.HavokData != null && Model.HavokData.Length > 0)  
            {
                try
                {
                    dataLoaded = Physics.Ragdoll.LoadRagdollFromBuffer(Model.HavokData);
                }
                catch (Exception e)
                {
                    Debug.Fail("Error loading ragdoll from buffer: " + e.Message);
                    Physics.CloseRagdoll();
                    Physics.Ragdoll = null;
                }
            }            
            else if (Definition.RagdollDataFile != null)
            {
                String ragDollFile = System.IO.Path.Combine(MyFileSystem.ContentPath, Definition.RagdollDataFile);
                if (System.IO.File.Exists(ragDollFile))
                {                  
                    dataLoaded = Physics.Ragdoll.LoadRagdollFromFile(ragDollFile);
                }
                else
                {               
                    System.Diagnostics.Debug.Fail("Cannot find ragdoll file: " + ragDollFile);               
                }
            }

            if (Definition.RagdollRootBody != String.Empty)
            {
                if (!Physics.Ragdoll.SetRootBody(Definition.RagdollRootBody))
                {
                    Debug.Fail("Can not set root body with name: " + Definition.RagdollRootBody + " on model " + ModelName + ". Please check your definitions.");
                }
            }

            if (!dataLoaded)
            {
                Physics.Ragdoll.Dispose();
                Physics.Ragdoll = null;
            }
           
            if (Physics.Ragdoll != null && MyFakes.ENABLE_RAGDOLL_DEFAULT_PROPERTIES)
            {
                Physics.SetRagdollDefaults();
            }

        }

        

        public void InitRagdollMapper()
        {
            if (Bones.Count == 0) return;
            if (Physics == null || Physics.Ragdoll == null) return;

            RagdollMapper = new MyRagdollMapper(this, Bones);

            RagdollMapper.Init(Definition.RagdollBonesMappings);
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

        void inventory_OnContentsChanged(MyInventory inventory)
        {
            // Switch away from the weapon if we don't have it; Cube placer is an exception
            if (m_currentWeapon != null && m_currentWeapon.DefinitionId.TypeId != typeof(MyObjectBuilder_CubePlacer)
                && inventory != null && !inventory.ContainItems(1, m_currentWeapon.PhysicalObject))
                SwitchToWeapon(null);
        }

        void RigidBody_ContactPointCallback(ref HkContactPointEvent value)
        {
            if (value.Base.BodyA.GetEntity() is MyCharacter && value.Base.BodyB.GetEntity() is MyCharacter)
            {

            }
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
                    MyEntity other = value.Base.BodyA.GetEntity() as MyEntity;

                    HkRigidBody otherRb = value.Base.BodyA;
                    if (other == this)
                    {
                        other = value.Base.BodyB.GetEntity() as MyEntity;
                        otherRb = value.Base.BodyB;
                        normal = -normal;
                    }

                    var otherChar = (other as MyCharacter);
                    if (otherChar != null && !(other as MyCharacter).IsDead)
                    {
                        if (Physics.CharacterProxy.Supported && otherChar.Physics.CharacterProxy.Supported)
                            return;
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

                    float mass1 = MyDestructionHelper.MassFromHavok(Physics.Mass);
                    float mass2 = MyDestructionHelper.MassFromHavok(other.Physics.Mass);

                    float impact1 = (speed1 * speed1 * mass1) * 0.5f;
                    float impact2 = (speed2 * speed2 * mass2) * 0.5f;


                    float mass;
                    if (Physics.Mass > other.Physics.Mass && !other.Physics.IsStatic)
                    {
                        mass = other.Physics.Mass;
                        //impact = impact2;
                    }
                    else
                    {
                        mass = 70 / 25;// Physics.Mass;
                        if (Physics.CharacterProxy.Supported && !other.Physics.IsStatic)
                            mass += Math.Abs(Vector3.Dot(Vector3.Normalize(velocity2), Physics.CharacterProxy.SupportNormal)) * other.Physics.Mass / 10;
                    }
                    mass = MyDestructionHelper.MassFromHavok(mass);
                    if (vel < 0)
                    {
                        return;
                    }

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
                            DoDamage(damageImpact, MyDamageType.Environment, true);
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
                if (value.Base.BodyA == Physics.CharacterProxy.GetHitRigidBody()) collidingBody = value.Base.BodyB;
                else collidingBody = value.Base.BodyA;
                MyEntity collidingEntity = collidingBody.GetEntity() as MyEntity;                
                if (collidingEntity == null || collidingEntity is MyCharacter) return;

                // Disable damage from hold objects
                if (VirtualPhysics != null && VirtualPhysics.Constraints != null)
                {
                    foreach (var constraint in VirtualPhysics.Constraints)
                    {
                        if (constraint.RigidBodyA == collidingBody || constraint.RigidBodyB == collidingBody) return;
                    }
                }

                if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_SHOW_DAMAGE)
                {
                    MatrixD worldMatrix = collidingEntity.Physics.GetWorldMatrix();
                    int index = 0;
                    MyPhysicsBody.DrawCollisionShape(collidingBody.GetShape(), worldMatrix, 1, ref index, "hit");
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
            Vector3 gravity = MyGravityProviderSystem.CalculateGravityInPoint(PositionComp.WorldAABB.Center) + Physics.HavokWorld.Gravity;
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
                MyPhysicsBody.DrawCollisionShape(collidingBody.GetShape(), worldMatrix, 1, ref index);
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

            // Get the objects energies to calculate the damage - must be higher above treshold
            float objectEnergy = Math.Abs(value.SeparatingVelocity) * (MyPerGameSettings.Destruction ? MyDestructionHelper.MassFromHavok(collidingBody.Mass) : collidingBody.Mass);

            if (objectEnergy > MyPerGameSettings.CharacterDamageHitObjectDeadlyEnergy) return DamageImpactEnum.DeadlyDamage;
            if (objectEnergy > MyPerGameSettings.CharacterDamageHitObjectCriticalEnergy) return DamageImpactEnum.CriticalDamage;
            if (objectEnergy > MyPerGameSettings.CharacterDamageHitObjectMediumEnergy) return DamageImpactEnum.MediumDamage;
            if (objectEnergy > MyPerGameSettings.CharacterDamageHitObjectSmallEnergy) return DamageImpactEnum.SmallDamage;

            return DamageImpactEnum.NoDamage;
        }

        private void ApplyDamage(DamageImpactEnum damageImpact, MyDamageType myDamageType)
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

            bool falledOnEntity = Vector3.Dot(value.ContactPoint.Normal, Physics.HavokWorld.Gravity) <= 0.0f;

            if (!falledOnEntity) return DamageImpactEnum.NoDamage; 

            if (Math.Abs(value.SeparatingVelocity) < MyPerGameSettings.CharacterDamageMinVelocity) return DamageImpactEnum.NoDamage;

            if (Math.Abs(value.SeparatingVelocity) > MyPerGameSettings.CharacterDamageDeadlyDamageVelocity) return DamageImpactEnum.DeadlyDamage;

            if (Math.Abs(value.SeparatingVelocity) >  MyPerGameSettings.CharacterDamageMediumDamageVelocity) return DamageImpactEnum.MediumDamage;

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
            objectBuilder.Inventory = m_inventory.GetObjectBuilder();

            if (m_currentWeapon != null)
                objectBuilder.HandWeapon = ((MyEntity)m_currentWeapon).GetObjectBuilder();

            objectBuilder.Battery = m_suitBattery.GetObjectBuilder();
            objectBuilder.DampenersEnabled = m_dampenersEnabled;
            objectBuilder.JetpackEnabled = m_jetpackEnabled;
            objectBuilder.LightEnabled = m_lightEnabled;
            objectBuilder.HeadAngle = new Vector2(m_headLocalXAngle, m_headLocalYAngle);

            objectBuilder.LinearVelocity = Physics != null ? Physics.LinearVelocity : Vector3.Zero;

            objectBuilder.AutoenableJetpackDelay = m_currentAutoenableJetpackDelay;

            objectBuilder.Health = StatComp.Health != null ? StatComp.Health.Value : Definition.MaxHealth;

            objectBuilder.LootingCounter = m_currentLootingCounter;
            objectBuilder.DisplayName = DisplayName;

            // ds sends IsInFirstPersonView to clients  as false
            objectBuilder.IsInFirstPersonView = !MySandboxGame.IsDedicated ? m_isInFirstPersonView : true;

            objectBuilder.EnableBroadcasting = m_radioBroadcaster.WantsToBeEnabled;

            objectBuilder.OxygenLevel = SuitOxygenLevel;
            objectBuilder.MovementState = m_currentMovementState;

            return objectBuilder;
        }

        protected override void Closing()
        {
            CloseInternal();

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
            if (m_breath != null)
                m_breath.Close();

            base.Closing();
        }

        private void CloseInternal()
        {
            RemoveNotifications();

            m_radioBroadcaster.Enabled = false;

            m_soundEmitter.StopSound(true);

            if (MyFakes.ENABLE_CHARACTER_VIRTUAL_PHYSICS && VirtualPhysics != null)
            {
                VirtualPhysics.Close();
                VirtualPhysics = null;
            }
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
            }

            m_wasInFirstPerson = m_isInFirstPerson;

            UpdateLightPower();

            PlaySound();

            if (!IsDead && m_currentMovementState != MyCharacterMovementEnum.Sitting && (!ControllerInfo.IsRemotelyControlled() || (MyFakes.CHARACTER_SERVER_SYNC)))
            {
                if (Physics.CharacterProxy != null)
                    Physics.CharacterProxy.StepSimulation(MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS);
            }

            m_currentAnimationChangeDelay += MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            m_currentRotationDelay -= MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            
            if (Sync.IsServer && !IsDead && m_currentMovementState != MyCharacterMovementEnum.Sitting && !MyEntities.IsInsideWorld((Vector3D)this.PositionComp.GetPosition()))
            {
                if (MySession.Static.SurvivalMode)
                    DoDamage(1000, MyDamageType.Suicide, true);
            }

            if (MyFakes.ENABLE_CHARACTER_VIRTUAL_PHYSICS && VirtualPhysics != null)
            {
                if (!VirtualPhysics.IsInWorld && Physics.IsInWorld)
                {
                    VirtualPhysics.Enabled = true;
                    VirtualPhysics.Activate();
                }

                if (VirtualPhysics.IsInWorld)
                {
                    MatrixD headWorldMatrix = GetHeadMatrix(false);
                    VirtualPhysics.SetRigidBodyTransform(headWorldMatrix);
                }
            }

            // TODO: This should be changed so the ragdoll gets registered in the generators, now for SE, apply gravity explictly
            // Apply Gravity on Ragdoll
            if (Physics.Ragdoll != null && Physics.Ragdoll.IsAddedToWorld && (!Physics.Ragdoll.IsKeyframed || RagdollMapper.IsPartiallySimulated))
            {
                Vector3 gravity = MyGravityProviderSystem.CalculateGravityInPoint(PositionComp.WorldAABB.Center) + Physics.HavokWorld.Gravity * CHARACTER_GRAVITY_MULTIPLIER;                
                Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, gravity * Definition.Mass, null, null);
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update Ragdoll");
            UpdateRagdoll();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            //MyRenderProxy.DebugDrawText3D(WorldMatrix.Translation + WorldMatrix.Up * 2.0f, m_currentMovementState.ToString(), Color.Red, 1.0f, false);
        }

        private MySoundPair SelectSound()
        {
            if (m_wasFlying && m_currentMovementState != MyCharacterMovementEnum.Flying)
            {
                m_wasFlying = false;
                return CharacterSounds[(int)CharacterSoundsEnum.JETPACK_IDLE_SOUND];
            }
            switch (m_currentMovementState)
            {
                case MyCharacterMovementEnum.Walking:
                case MyCharacterMovementEnum.BackWalking:
                case MyCharacterMovementEnum.WalkingLeftFront:
                case MyCharacterMovementEnum.WalkingRightFront:
                case MyCharacterMovementEnum.WalkingLeftBack:
                case MyCharacterMovementEnum.WalkingRightBack:
                case MyCharacterMovementEnum.WalkStrafingLeft:
                case MyCharacterMovementEnum.WalkStrafingRight:
                    {
                        m_breath.CurrentState = MyCharacterBreath.State.Calm;
                        RayCastGround();
                        if (m_walkingSurfaceType != MyWalkingSurfaceType.None)
                            return (m_walkingSurfaceType == MyWalkingSurfaceType.Rock) ? CharacterSounds[(int)CharacterSoundsEnum.WALK_ROCK_SOUND] : CharacterSounds[(int)CharacterSoundsEnum.WALK_METAL_SOUND];
                    }
                    break;
                case MyCharacterMovementEnum.Running:
                case MyCharacterMovementEnum.Backrunning:
                case MyCharacterMovementEnum.RunStrafingLeft:
                case MyCharacterMovementEnum.RunStrafingRight:
                case MyCharacterMovementEnum.RunningRightFront:
                case MyCharacterMovementEnum.RunningRightBack:
                case MyCharacterMovementEnum.RunningLeftFront:
                case MyCharacterMovementEnum.RunningLeftBack:
                    {
                        m_breath.CurrentState = MyCharacterBreath.State.Heated;
                        RayCastGround();
                        if (m_walkingSurfaceType != MyWalkingSurfaceType.None)
                            return (m_walkingSurfaceType == MyWalkingSurfaceType.Rock) ? CharacterSounds[(int)CharacterSoundsEnum.RUN_ROCK_SOUND] : CharacterSounds[(int)CharacterSoundsEnum.RUN_METAL_SOUND];
                    }
                    break;
                case MyCharacterMovementEnum.CrouchWalking:
                case MyCharacterMovementEnum.CrouchBackWalking:
                case MyCharacterMovementEnum.CrouchWalkingLeftFront:
                case MyCharacterMovementEnum.CrouchWalkingRightFront:
                case MyCharacterMovementEnum.CrouchWalkingLeftBack:
                case MyCharacterMovementEnum.CrouchWalkingRightBack:
                case MyCharacterMovementEnum.CrouchStrafingLeft:
                case MyCharacterMovementEnum.CrouchStrafingRight:
                    {
                        m_breath.CurrentState = MyCharacterBreath.State.Calm;
                        RayCastGround();
                        if (m_walkingSurfaceType != MyWalkingSurfaceType.None)
                            return (m_walkingSurfaceType == MyWalkingSurfaceType.Rock) ? CharacterSounds[(int)CharacterSoundsEnum.CROUCH_RUN_ROCK_SOUND] : CharacterSounds[(int)CharacterSoundsEnum.CROUCH_RUN_METAL_SOUND];
                    }
                    break;
                case MyCharacterMovementEnum.Crouching:
                case MyCharacterMovementEnum.Standing:
                    {
                        m_breath.CurrentState = MyCharacterBreath.State.Calm;
                        if (m_previousMovementState != m_currentMovementState && (m_previousMovementState == MyCharacterMovementEnum.Standing || m_previousMovementState == MyCharacterMovementEnum.Crouching))
                            return (m_currentMovementState == MyCharacterMovementEnum.Standing) ? CharacterSounds[(int)CharacterSoundsEnum.CROUCH_UP_SOUND] : CharacterSounds[(int)CharacterSoundsEnum.CROUCH_DOWN_SOUND];
                        else
                            return CharacterSounds[(int)CharacterSoundsEnum.NONE_SOUND];
                    }
                    break;
                case MyCharacterMovementEnum.Sprinting:
                    {
                        m_breath.CurrentState = MyCharacterBreath.State.Heated;
                        RayCastGround();
                        if (m_walkingSurfaceType != MyWalkingSurfaceType.None)
                            return (m_walkingSurfaceType == MyWalkingSurfaceType.Rock) ? CharacterSounds[(int)CharacterSoundsEnum.SPRINT_ROCK_SOUND] : CharacterSounds[(int)CharacterSoundsEnum.SPRINT_METAL_SOUND];
                    }
                    break;
                case MyCharacterMovementEnum.Jump:
                    {
                        if (m_previousMovementState == MyCharacterMovementEnum.Jump)
                            break;
                        m_previousMovementState = m_currentMovementState;
                        var emitter = MyAudioComponent.TryGetSoundEmitter(); //we need to use other emmiter otherwise the sound would be cut by silence next frame
                        if (emitter != null)
                        {
                            emitter.Entity = this;
                            emitter.PlaySingleSound(CharacterSounds[(int)CharacterSoundsEnum.JUMP_SOUND]);
                        }
                    }
                    break;
                case MyCharacterMovementEnum.Flying:
                    {
                        m_breath.CurrentState = MyCharacterBreath.State.Calm;
                        if (!m_wasFlying)
                        {
                            m_wasFlying = true;
                            return CharacterSounds[(int)CharacterSoundsEnum.JETPACK_IDLE_SOUND];
                        }
                        return CharacterSounds[(int)CharacterSoundsEnum.JETPACK_RUN_SOUND];
                    }
                    break;
                case MyCharacterMovementEnum.Falling:
                    {
                        m_breath.CurrentState = MyCharacterBreath.State.Calm;
                        return CharacterSounds[(int)CharacterSoundsEnum.NONE_SOUND];
                    }
                    break;
                default:
                    {
                    }
                    break;
            }

            return CharacterSounds[(int)CharacterSoundsEnum.NONE_SOUND];
        }

        public void UpdateLightPower(bool chargeImmediatelly = false)
        {
            float oldPower = m_currentLightPower;

            if (m_lightPowerFromProducer > 0 && m_lightEnabled)
            {
                if (chargeImmediatelly)
                    m_currentLightPower = 1;
                else
                    m_currentLightPower = MathHelper.Clamp(m_currentLightPower + m_lightTurningOnSpeed, 0, 1);
            }
            else
            {
                if (chargeImmediatelly)
                    m_currentLightPower = 0;
                else
                    m_currentLightPower = MathHelper.Clamp(m_currentLightPower - m_lightTurningOffSpeed, 0, 1);
            }

            Render.UpdateLight(m_currentLightPower, oldPower != m_currentLightPower);

            if (m_radioBroadcaster.WantsToBeEnabled)
            {
                m_radioBroadcaster.Enabled = m_suitBattery.CurrentPowerOutput > 0;
            }
            if (!m_radioBroadcaster.WantsToBeEnabled)
            {
                m_radioBroadcaster.Enabled = false;
            }
        }
        public bool IsJetpackPowered()
        {
            return m_jetpackPowerFromProducer > 0;
        }

        public override void UpdateBeforeSimulation10()
        {
            base.UpdateBeforeSimulation10();

            m_suitPowerDistributor.UpdateBeforeSimulation10();

            m_radioReceiver.UpdateBroadcastersInRange();

            if (this == MySession.ControlledEntity)
            {
                m_radioReceiver.UpdateHud();
            }
        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();
            m_soundEmitter.Update();

            m_suitBattery.UpdateOnServer();

            if (!m_suitBattery.HasCapacityRemaining && !Definition.NeedsOxygen)
            {
                DoDamage(5, MyDamageType.Environment, true);
            }

            UpdateChat();
            UpdateOxygen();

            if (Sync.IsServer && IsDead && MyFakes.ENABLE_RAGDOLL_CLIENT_SYNC)
            {
                RagdollMapper.SyncRigidBodiesTransforms(WorldMatrix);
            }
        }

		public override void UpdateAfterSimulation10()
		{
			base.UpdateAfterSimulation10();

            foreach (var component in Components)
            {
                if (component is MyCharacterComponent)
                    ((MyCharacterComponent)component).UpdateAfterSimulation10();
            }


            UpdateCameraDistance();
		}

        private void UpdateCameraDistance()
        {
            MatrixD viewMatrix = MySession.Static.CameraController.GetViewMatrix();

            Vector3 cameraLocation = MatrixD.Invert(viewMatrix).Translation;

            m_cameraDistance = Vector3.Distance(cameraLocation,WorldMatrix.Translation);
        }

        private void UpdateChat()
        {
            if (MySession.LocalCharacter == this)
            {
                MyChatHistory chatHistory;
                if (MySession.Static.ChatHistory.TryGetValue(MySession.LocalPlayerId, out chatHistory))
                {
                    foreach (var chatPlayerHistory in chatHistory.PlayerChatHistory)
                    {
                        foreach (var chatItem in chatPlayerHistory.Value.Chat)
                        {
                            if (!chatItem.Sent)
                            {
                                MyPlayer.PlayerId playerId;
                                if (MySession.Static.Players.TryGetPlayerId(chatPlayerHistory.Key, out playerId))
                                {
                                    SyncObject.SendNewPlayerMessage(MySession.LocalHumanPlayer.Id, playerId, chatItem.Text, chatItem.Timestamp);
                                }
                                else
                                {
                                    Debug.Fail("Message to send has invalid IdentityId!");
                                }
                            }
                        }
                    }

                }
            }
        }

        #region Oxygen
        private void UpdateOxygen()
        {
            if (!MySession.Static.Settings.EnableOxygen)
            {
                return;
            }

            // Try to find grids that might contain oxygen
            var entities = new List<MyEntity>();
            MyGamePruningStructure.GetAllTopMostEntitiesInBox<MyEntity>(ref m_actualWorldAABB, entities);
            bool lowOxygenDamage = true;
            bool noOxygenDamage = true;
            bool isInEnvironment = true;

            EnvironmentOxygenLevel = MyOxygenProviderSystem.GetOxygenInPoint(PositionComp.GetPosition());

            var cockpit = Parent as MyCockpit;
            if (cockpit != null && cockpit.BlockDefinition.IsPressurized)
            {
                if (Sync.IsServer && MySession.Static.SurvivalMode)
                {
                    if (!Definition.NeedsOxygen && m_suitOxygenAmount > Definition.OxygenConsumption)
                    {
                        m_suitOxygenAmount -= Definition.OxygenConsumption;
                        if (m_suitOxygenAmount < 0f)
                        {
                            m_suitOxygenAmount = 0f;
                        }
                    }

                    if (cockpit.OxygenLevel > 0f)
                    {
                        if (Definition.NeedsOxygen)
                        {
                            if (cockpit.OxygenAmount >= Definition.OxygenConsumption)
                            {
                                cockpit.OxygenAmount -= Definition.OxygenConsumption;

                                noOxygenDamage = false;
                                lowOxygenDamage = false;
                            }
                        }
                        else
                        {
                            float oxygenTransferred = Math.Min(SuitOxygenAmountMissing, cockpit.OxygenAmount);
                            oxygenTransferred = Math.Min(oxygenTransferred, MyOxygenConstants.OXYGEN_REGEN_PER_SECOND);

                            cockpit.OxygenAmount -= oxygenTransferred;
                            SuitOxygenAmount += oxygenTransferred;

                            noOxygenDamage = false;
                            lowOxygenDamage = false;
                        }
                    }
                }
                EnvironmentOxygenLevel = cockpit.OxygenLevel;
                isInEnvironment = false;
            }
            else
            {
                Vector3D pos = PositionComp.WorldMatrix.Translation;
                if (m_headBoneIndex != -1)
                {
                    pos = (BoneTransforms[m_headBoneIndex] * WorldMatrix).Translation;
                }
                foreach (var entity in entities)
                {
                    var grid = entity as MyCubeGrid;
                    // Oxygen can be present on small grids as well because of mods
                    if (grid != null)
                    {
                        var oxygenBlock = grid.GridSystems.OxygenSystem.GetSafeOxygenBlock(pos);
                        if (oxygenBlock.Room != null)
                        {
                            if (oxygenBlock.Room.OxygenLevel(grid.GridSize) > Definition.PressureLevelForLowDamage)
                            {
                                if (Definition.NeedsOxygen)
                                {
                                    lowOxygenDamage = false;
                                }
                            }

                            if (oxygenBlock.Room.IsPressurized)
                            {
                                EnvironmentOxygenLevel = oxygenBlock.Room.OxygenLevel(grid.GridSize);
                                if (oxygenBlock.Room.OxygenAmount > Definition.OxygenConsumption)
                                {
                                    if (Definition.NeedsOxygen)
                                    {
                                        noOxygenDamage = false;
                                        oxygenBlock.PreviousOxygenAmount = oxygenBlock.OxygenAmount() - Definition.OxygenConsumption;
                                        oxygenBlock.OxygenChangeTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                                        oxygenBlock.Room.OxygenAmount -= Definition.OxygenConsumption;
                                    }
                                    break;
                                }
                            }
                            else
                            {
                                EnvironmentOxygenLevel = oxygenBlock.Room.EnvironmentOxygen;
                                if (EnvironmentOxygenLevel > Definition.OxygenConsumption)
                                {
                                    if (Definition.NeedsOxygen)
                                    {
                                        noOxygenDamage = false;
                                    }
                                    break;
                                }
                            }

                            isInEnvironment = false;
                        }
                    }
                }
            }

            if (MySession.LocalCharacter == this)
            {
                if (m_oldSuitOxygenLevel >= 0.25f && SuitOxygenLevel < 0.25f)
                {
                    MyHud.Notifications.Add(m_lowOxygenNotification);
                }
                else if (m_oldSuitOxygenLevel >= 0.05f && SuitOxygenLevel < 0.05f)
                {
                    MyHud.Notifications.Add(m_criticalOxygenNotification);
                }
            }
            m_oldSuitOxygenLevel = SuitOxygenLevel;

            // Cannot early exit before calculations because of UI
            if (!Sync.IsServer || MySession.Static.CreativeMode)
            {
                return;
            }

            //TODO(AF) change this to a constant
            //Try to refill the suit from bottles in inventory
            if (SuitOxygenLevel < 0.3f && !Definition.NeedsOxygen)
            {
                var items = m_inventory.GetItems();
                bool bottlesUsed = false;
                foreach (var item in items)
                {
                    var oxygenContainer = item.Content as MyObjectBuilder_OxygenContainerObject;
                    if (oxygenContainer != null)
                    {
                        if (oxygenContainer.OxygenLevel == 0f)
                        {
                            continue;
                        }

                        var physicalItem = MyDefinitionManager.Static.GetPhysicalItemDefinition(oxygenContainer) as MyOxygenContainerDefinition;
                        float oxygenAmount = oxygenContainer.OxygenLevel * physicalItem.Capacity;

                        float transferredAmount = Math.Min(oxygenAmount, SuitOxygenAmountMissing);
                        oxygenContainer.OxygenLevel = (oxygenAmount - transferredAmount) / physicalItem.Capacity;

                        if (oxygenContainer.OxygenLevel < 0f)
                        {
                            oxygenContainer.OxygenLevel = 0f;
                        }


                        if (oxygenContainer.OxygenLevel > 1f)
                        {
                            Debug.Fail("Incorrect value");
                        }

                        m_inventory.UpdateOxygenAmount();
                        m_inventory.SyncOxygenContainerLevel(item.ItemId, oxygenContainer.OxygenLevel);

                        bottlesUsed = true;

                        SuitOxygenAmount += transferredAmount;
                        if (SuitOxygenLevel == 1f)
                        {
                            break;
                        }
                    }
                }
                if (bottlesUsed)
                {
                    if (MySession.LocalCharacter == this)
                    {
                        ShowRefillFromBottleNotification();
                    }
                    else
                    {
                        SyncObject.SendRefillFromBottle();
                    }
                }
            }

            // No oxygen found in room, try to get it from suit
            if (noOxygenDamage || lowOxygenDamage)
            {
                if (!Definition.NeedsOxygen && m_suitOxygenAmount > Definition.OxygenConsumption)
                {
                    m_suitOxygenAmount -= Definition.OxygenConsumption;
                    if (m_suitOxygenAmount < 0f)
                    {
                        m_suitOxygenAmount = 0f;
                    }
                    noOxygenDamage = false;
                    lowOxygenDamage = false;
                }

                if (isInEnvironment)
                {
                    if (EnvironmentOxygenLevel > Definition.PressureLevelForLowDamage)
                    {
                        lowOxygenDamage = false;
                    }
                    if (EnvironmentOxygenLevel > 0f)
                    {
                        noOxygenDamage = false;
                    }
                }
            }

            if (noOxygenDamage)
            {
                DoDamage(Definition.DamageAmountAtZeroPressure, MyDamageType.Environment, true);
            }
            else if (lowOxygenDamage)
            {
                DoDamage(1f, MyDamageType.Environment, true);
            }

            SyncObject.UpdateOxygen(SuitOxygenAmount);
        }

        public void ShowRefillFromBottleNotification()
        {
            MyHud.Notifications.Add(m_oxygenBottleRefillNotification);
        }
        #endregion

        public void DrawHud(IMyCameraController camera, long playerId)
        {
            MyHud.Crosshair.Position = MyHudCrosshair.ScreenCenter;

            if (m_currentWeapon != null)
            {
                m_currentWeapon.DrawHud(camera, playerId);
            }
        }

        static Vector3[] m_corners = new Vector3[8];

        public static bool TestInteractionDirection(Vector3 characterDirection, Vector3 toTargetDirection)
        {
            return Vector3.Dot(characterDirection, toTargetDirection) < INTERACTION_HALF_COS_ANGLE;
        }

        private Vector3? RayCastGround()
        {
            var from = PositionComp.GetPosition() + WorldMatrix.Up * 0.1; //(needs some small distance from the bottom or the following call to HavokWorld.CastRay will find no hits)
            var to = from + WorldMatrix.Down * MyConstants.DEFAULT_GROUND_SEARCH_DISTANCE;

            MyPhysics.CastRay(from, to, m_hits);

            // Skips invalid hits (null body, self character)
            int index = 0;
            while ((index < m_hits.Count) && ((m_hits[index].HkHitInfo.Body == null) || (m_hits[index].HkHitInfo.Body.GetEntity() == Entity.Components)))
            {
                index++;
            }

            //m_walkingSurfaceType = MyWalkingSurfaceType.None;
            if (index < m_hits.Count)
            {
                // We must take only closest hit (others are hidden behind)
                var h = m_hits[index];
                var entity = h.HkHitInfo.Body.GetEntity();

                var sqDist = Vector3D.DistanceSquared((Vector3D)h.Position, from);
                if (sqDist < MyConstants.DEFAULT_GROUND_SEARCH_DISTANCE * MyConstants.DEFAULT_GROUND_SEARCH_DISTANCE)
                {
                    if (entity is MyCubeGrid)
                        m_walkingSurfaceType = MyWalkingSurfaceType.Metal;
                    else if (entity is MyVoxelMap)
                        m_walkingSurfaceType = MyWalkingSurfaceType.Rock;

                    return h.Position;
                }
            }

            return null;
        }


        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

			VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update Stats");
			UpdateStats();
			VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update diyng");
            UpdateDiyng();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            m_updateCounter++;

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update zero movement");
            UpdateZeroMovement();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update Shake");
            UpdateShake();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update physical movement");
            UpdatePhysicalMovement();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update shooting");
            UpdateShooting();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update Fall And Spine");
            UpdateFallAndSpine();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update Animation");
            UpdateAnimation();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();


            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Calculate transforms");
            CalculateTransforms();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Calculate dependent matrices");
            CalculateDependentMatrices();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            if (m_characterDefinition.FeetIKEnabled && MyFakes.ENABLE_FOOT_IK && Physics.CharacterProxy != null)
            {
                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update Feet");
                if (IsCameraNear) UpdateFeet();
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
            }

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

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Simulate Ragdoll");
            SimulateRagdoll();    // probably should be in UpdateDying, but changes the animation of the bones..
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

		private void UpdateStats()
		{
			m_stats.Update();
		}

        private void CheckRagdollSwitch()
        {
            if (IsDead) return;
            if (MySession.ControlledEntity != this) return;
            if (!Physics.Enabled) DeactivateJetpackRagdoll();
            if (SwitchToJetpackRagdoll && !Physics.IsRagdollModeActive)
            {
                ActivateJetpackRagdoll();
                ResetJetpackRagdoll = false;
            }
            else if (!SwitchToJetpackRagdoll && Physics.IsRagdollModeActive)
            {               
                DeactivateJetpackRagdoll();
            }
            else if (SwitchToJetpackRagdoll && Physics.IsRagdollModeActive && ResetJetpackRagdoll)
            {
                DeactivateJetpackRagdoll();
                ActivateJetpackRagdoll();
                if (Physics.IsRagdollModeActive) ResetJetpackRagdoll = false;
            }
        }

        /// <summary>
        /// Sets the ragdoll pose to bones pose
        /// </summary> 
        private void UpdateRagdoll()
        {
            if (Physics == null || Physics.Ragdoll == null || RagdollMapper == null ) return;
            if (!MyPerGameSettings.EnableRagdollModels) return;
            //return;
            CheckRagdollSwitch();

            if (!RagdollMapper.IsActive || !Physics.IsRagdollModeActive) return;

            if (!RagdollMapper.IsKeyFramed && !RagdollMapper.IsPartiallySimulated) return;

            RagdollMapper.UpdateRagdollPosition();
            RagdollMapper.UpdateRagdollPose();
            RagdollMapper.SetVelocities();
            
            RagdollMapper.DebugDraw(WorldMatrix);            
        }


        private void ActivateJetpackRagdoll()
        {
            if (RagdollMapper == null || Physics == null || Physics.Ragdoll == null) return;
            if (!MyPerGameSettings.EnableRagdollModels) return;
            if (!MyPerGameSettings.EnableRagdollInJetpack) return;
                        
            List<string> bodies = new List<string>();
            string[] bodiesArray;                       
            
            if (CurrentWeapon == null)
            {
                if (m_characterDefinition.RagdollPartialSimulations.TryGetValue("Jetpack", out bodiesArray))
                {
                    bodies.AddArray(bodiesArray);
                }
                else
                {
                    // Fallback if missing definitions
                    bodies.Add("Ragdoll_SE_rig_LUpperarm001");
                    bodies.Add("Ragdoll_SE_rig_LForearm001");
                    bodies.Add("Ragdoll_SE_rig_LPalm001");
                    bodies.Add("Ragdoll_SE_rig_RUpperarm001");
                    bodies.Add("Ragdoll_SE_rig_RForearm001");
                    bodies.Add("Ragdoll_SE_rig_RPalm001");

                    bodies.Add("Ragdoll_SE_rig_LThigh001");
                    bodies.Add("Ragdoll_SE_rig_LCalf001");
                    bodies.Add("Ragdoll_SE_rig_LFoot001");
                    bodies.Add("Ragdoll_SE_rig_RThigh001");
                    bodies.Add("Ragdoll_SE_rig_RCalf001");
                    bodies.Add("Ragdoll_SE_rig_RFoot001");
                }
            }
            else
            {
                if (m_characterDefinition.RagdollPartialSimulations.TryGetValue("Jetpack_Weapon", out bodiesArray))
                {
                    bodies.AddArray(bodiesArray);
                }
                else
                {
                    bodies.Add("Ragdoll_SE_rig_LThigh001");
                    bodies.Add("Ragdoll_SE_rig_LCalf001");
                    bodies.Add("Ragdoll_SE_rig_LFoot001");
                    bodies.Add("Ragdoll_SE_rig_RThigh001");
                    bodies.Add("Ragdoll_SE_rig_RCalf001");
                    bodies.Add("Ragdoll_SE_rig_RFoot001");
                }
            }

            List<int> simulatedBodies = new List<int>();

            foreach(var body in bodies)
            {
                simulatedBodies.Add(RagdollMapper.BodyIndex(body));
            }

            Physics.SwitchToRagdollMode(false);

            if (Physics.IsRagdollModeActive)
            {
                RagdollMapper.ActivatePartialSimulation(simulatedBodies);        
            }

            // This is hack, ragdoll in jetpack sometimes can't settle and simulation is broken, if we find another way how to avoid that, this can be disabled
            if (!MyFakes.ENABLE_JETPACK_RAGDOLL_COLLISIONS)
            {
                foreach (var body in Physics.Ragdoll.RigidBodies)
                {
                    var info = HkGroupFilter.CalcFilterInfo(MyPhysics.RagdollCollisionLayer, 0, 0, 0);
                    Physics.HavokWorld.DisableCollisionsBetween(MyPhysics.RagdollCollisionLayer, MyPhysics.RagdollCollisionLayer);
                    body.SetCollisionFilterInfo(info);
                    body.LinearVelocity = Vector3.Zero;
                    body.AngularVelocity = Vector3.Zero;
                }                
            }

            RagdollMapper.ResetRagdoll(WorldMatrix);
        }

        private void DeactivateJetpackRagdoll()
        {
            if (RagdollMapper == null || Physics == null || Physics.Ragdoll == null) return;
            if (!MyPerGameSettings.EnableRagdollModels) return;
            if (!RagdollMapper.IsActive) return;
            if (!MyPerGameSettings.EnableRagdollInJetpack) return;

            RagdollMapper.DeactivatePartialSimulation();

            Physics.CloseRagdollMode();
            Physics.Ragdoll.ResetToRigPose();
        }

        /// <summary>
        /// Sets the bones pose to ragdoll pose
        /// </summary>
        private void SimulateRagdoll()
        {
            if (!MyPerGameSettings.EnableRagdollModels) return;   
            if (Physics == null || RagdollMapper == null) return;

            if (Physics.Ragdoll == null || !Physics.Ragdoll.IsAddedToWorld || !RagdollMapper.IsActive) return;
            
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update Bones To Ragdoll");

            RagdollMapper.UpdateRagdollAfterSimulation();

            if (!IsCameraNear && !MyFakes.ENABLE_PERMANENT_SIMULATIONS_COMPUTATION) return;
            
            RagdollMapper.UpdateCharacterPose( IsDead ? 1.0f : 0.1f, IsDead ? 1.0f : 0.0f);

            RagdollMapper.DebugDraw(WorldMatrix);            
                        
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            // save bone changes
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Save bones and pos update");

            for (int i = 0; i < Bones.Count; i++)
            {
                MyCharacterBone bone = Bones[i];
                m_boneRelativeTransforms[i] = bone.ComputeBoneTransform();                
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }


        /// <summary>
        /// Updates feet bones positions, locations and rotation using IK, based on current character state
        /// </summary>
        private void UpdateFeet()
        {
            
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("UpdateFeetPlacement standing");

            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_SETTINGS)
            {
                MyFeetIKSettings feetDebugSettings;
                m_characterDefinition.FeetIKSettings.TryGetValue(MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_IK_MOVEMENT_STATE, out feetDebugSettings);
                Matrix leftFootMatrix = Bones[m_leftAnkleBone].AbsoluteTransform;
                Matrix rightFootMatrix = Bones[m_rightAnkleBone].AbsoluteTransform;
                Vector3 upDirection = WorldMatrix.Up;
                Vector3 leftFootGroundPosition = new Vector3(leftFootMatrix.Translation.X, 0, leftFootMatrix.Translation.Z);
                Vector3 rightFootGroundPosition = new Vector3(rightFootMatrix.Translation.X, 0, rightFootMatrix.Translation.Z);
                Vector3 fromL = Vector3.Transform(leftFootGroundPosition, WorldMatrix);  // we get this position in the world
                Vector3 fromR = Vector3.Transform(rightFootGroundPosition, WorldMatrix);
                VRageRender.MyRenderProxy.DebugDrawLine3D(fromL, fromL + upDirection * feetDebugSettings.AboveReachableDistance, Color.Yellow, Color.Yellow, false);
                VRageRender.MyRenderProxy.DebugDrawLine3D(fromL, fromL - upDirection * feetDebugSettings.BelowReachableDistance, Color.Red, Color.Red, false);
                VRageRender.MyRenderProxy.DebugDrawLine3D(fromR, fromR + upDirection * feetDebugSettings.AboveReachableDistance, Color.Yellow, Color.Yellow, false);
                VRageRender.MyRenderProxy.DebugDrawLine3D(fromR, fromR - upDirection * feetDebugSettings.BelowReachableDistance, Color.Red, Color.Red, false);
                Matrix leftFoot = Matrix.CreateScale(feetDebugSettings.FootSize) * WorldMatrix;
                Matrix rightFoot = Matrix.CreateScale(feetDebugSettings.FootSize) * WorldMatrix;
                leftFoot.Translation = fromL;
                rightFoot.Translation = fromR;
                VRageRender.MyRenderProxy.DebugDrawOBB(leftFoot, Color.White, 1f, false, false);
                VRageRender.MyRenderProxy.DebugDrawOBB(rightFoot, Color.White, 1f, false, false);
            }

            MyFeetIKSettings feetSettings;

            if (m_characterDefinition.FeetIKSettings.TryGetValue(GetCurrentMovementState(), out feetSettings))
            {
                if (feetSettings.Enabled)
                {
                    UpdateFeetPlacement(WorldMatrix.Up,
                        feetSettings.BelowReachableDistance,
                        feetSettings.AboveReachableDistance,
                        feetSettings.VerticalShiftUpGain,
                        feetSettings.VerticalShiftDownGain,
                        feetSettings.FootSize);
                }
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
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
                m_bobQueue.Enqueue(BoneTransforms[headBone].Translation);

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
            if (!ControllerInfo.IsRemotelyControlled() || (MyFakes.CHARACTER_SERVER_SYNC))
            {
                if (m_currentAutoenableJetpackDelay >= AUTO_ENABLE_JETPACK_INTERVAL)
                {
                    m_dampenersEnabled = true;
                    EnableJetpack(true);
                    m_currentAutoenableJetpackDelay = -1;
                }

                if (m_isFalling)
                {
                    if (!CanFly())
                    {
                        m_currentFallingTime += MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                        if (m_currentFallingTime > FallTime && !m_isFallingAnimationPlayed)
                        {
                            SwitchAnimation(MyCharacterMovementEnum.Falling, false);
                            m_isFallingAnimationPlayed = true;
                        }
                    }
                }

                if ((!CanFly() || (CanFly() && (IsLocalHeadAnimationInProgress() || Definition.VerticalPositionFlyingOnly))) && !IsDead && !IsSitting)
                {
                    float spineRotation = MathHelper.Clamp(-m_headLocalXAngle, -45, MaxHeadLocalXAngle);

                    float bendMultiplier = IsInFirstPersonView ? m_characterDefinition.BendMultiplier1st : m_characterDefinition.BendMultiplier3rd;
                    Quaternion usedSpineRotation = Quaternion.CreateFromAxisAngle(Vector3.Backward, MathHelper.ToRadians(bendMultiplier * spineRotation));

                    Quaternion clientsSpineRotation = Quaternion.CreateFromAxisAngle(Vector3.Backward, MathHelper.ToRadians(m_characterDefinition.BendMultiplier3rd * spineRotation));

                    SetSpineAdditionalRotation(usedSpineRotation, clientsSpineRotation);
                }
                else
                    SetSpineAdditionalRotation(Quaternion.CreateFromAxisAngle(Vector3.Backward, 0), Quaternion.CreateFromAxisAngle(Vector3.Backward, 0));

                if (m_currentWeapon == null && !IsDead && !CanFly() && !IsSitting)
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
            } // if (ControllerInfo.IsLocalPlayer())
        }

        private void UpdateShooting()
        {
            if (m_currentWeapon != null)
            {
                //UpdateWeaponPosition();

                if (m_currentWeapon.IsShooting)
                {
                    m_currentShootPositionTime = ShotTime;
                }

                ShootInternal();
                // CH: Warning, m_currentWeapon can be null after ShootInternal because of autoswitch!
            }

            if (m_currentShotTime > 0)
            {
                m_currentShotTime -= MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                if ((m_currentShotTime <= 0) && (m_currentZoomTime == 0))
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
            if ((ControllerInfo.IsLocallyControlled() || MyFakes.CHARACTER_SERVER_SYNC) && Physics.CharacterProxy != null)
            {
                if (CanFly())
                {
                    //Flying mode
                    Physics.CharacterProxy.Gravity = Vector3.Zero;
                    if (!m_isFlying && m_dampenersEnabled)
                    {
                        Physics.CharacterProxy.LinearVelocity = Physics.CharacterProxy.LinearVelocity * m_characterDefinition.JetpackSlowdown;
                    }
                    if (Physics.CharacterProxy.LinearVelocity.Length() < MINIMAL_SPEED)
                    {
                        Physics.CharacterProxy.LinearVelocity = Vector3.Zero;
                    }
                }
                //Solve Y orientation and gravity only in non flying mode
                else if (!IsDead)
                {
                    Vector3 gravity = MyGravityProviderSystem.CalculateGravityInPoint(PositionComp.WorldAABB.Center) + Physics.HavokWorld.Gravity;
                    Physics.CharacterProxy.Gravity = gravity * CHARACTER_GRAVITY_MULTIPLIER;

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

                    Vector3 oldUp = Physics.CharacterProxy.Up;
                    Vector3 newUp = Physics.CharacterProxy.Up;
                    Vector3 newForward = Physics.CharacterProxy.Forward;

                    // If there is valid non-zero gravity
                    if ((gravity.LengthSquared() > 0.1f) && (oldUp != Vector3.Zero) && (gravity.IsValid()) && !Definition.VerticalPositionFlyingOnly)
                    {
                        UpdateStandup(ref gravity, ref oldUp, ref newUp, ref newForward);
                        m_currentAutoenableJetpackDelay = 0;
                    }
                    // Zero-G
                    else
                    {
                        if (m_currentAutoenableJetpackDelay != -1)
                            m_currentAutoenableJetpackDelay += MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                    }

                    Physics.CharacterProxy.Forward = newForward;
                    Physics.CharacterProxy.Up = newUp;
                }
            }

            MatrixD worldMatrix = Physics.GetWorldMatrix();

            // Vertical Flying - in this mode we only update position and orientation
            //if (CanFly() && Definition.VerticalPositionFlyingOnly)
            //{
            //    Vector3D newPos = worldMatrix.Translation;
            //    Vector3 newForward = new Vector3(worldMatrix.Forward.X, 0, worldMatrix.Forward.Z);
            //    Vector3 newUp = Vector3D.Up;
            //    newForward.Normalize();
            //    worldMatrix = MatrixD.CreateWorld(newPos, newForward, newUp);
            //}

            //if (ControllerInfo.Controller != null && ControllerInfo.IsRemotelyControlled() && Definition.VerticalPositionFlyingOnly)
            //{
            //    Vector3D newPos = worldMatrix.Translation;
            //    Vector3 newForward = new Vector3(worldMatrix.Forward.X, 0, worldMatrix.Forward.Z);
            //    Vector3 newUp = Vector3D.Up;
            //    newForward.Normalize();
            //    worldMatrix = MatrixD.CreateWorld(newPos, newForward, newUp);
            //}

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

            if (ControllerInfo.IsLocallyControlled() || AIMode)
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

        public void UpdateZeroMovement(bool force = false)
        {
            if ((ControllerInfo.IsLocallyControlled() && MySession.ControlledEntity == this) || force)
            {
                if (m_moveAndRotateCounter < m_updateCounter)
                {   //Stop character because MoveAndRotate was not called
                    MoveAndRotate(Vector3.Zero, Vector2.Zero, 0, m_movementFlags);
                    m_moveAndRotateCounter = m_updateCounter;
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

        private void SetHeadLocalXAngle(float angle, bool updateSync = true)
        {
            if (m_headLocalXAngle != angle)
            {
                m_headLocalXAngle = angle;
                if (updateSync)
                {
                    SyncObject.ChangeHeadOrSpine(m_headLocalXAngle, m_headLocalYAngle,
                        Quaternion.Zero, m_player.HandAdditionalRotation, m_player.HandAdditionalRotation, m_player.UpperHandAdditionalRotation);
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
                        Quaternion.Zero, m_player.HandAdditionalRotation, m_player.HandAdditionalRotation, m_player.UpperHandAdditionalRotation);
                }
            }
        }


        bool ShouldUseAnimatedHeadRotation()
        {
            //if (m_currentHeadAnimationCounter > 0.15f)
            //  return true;

            return false;
        }

        private void CalculateDependentMatrices()
        {
            Render.UpdateThrustMatrices(BoneTransforms);

            m_actualWorldAABB = BoundingBoxD.CreateInvalid();

            for (int i = 1; i < Model.Bones.Length; i++)
            {
                Vector3D p1 = Vector3D.Transform(Bones[i].Parent.AbsoluteTransform.Translation, m_helperMatrix * WorldMatrix);
                Vector3D p2 = Vector3D.Transform(Bones[i].AbsoluteTransform.Translation, m_helperMatrix * WorldMatrix);

                m_actualWorldAABB.Include(ref p1);
                m_actualWorldAABB.Include(ref p2);
            }

            ContainmentType containmentType;
            m_aabb.Contains(ref m_actualWorldAABB, out containmentType);
            if (containmentType != ContainmentType.Contains)
            {
                m_actualWorldAABB.Inflate(0.5f);
                MatrixD worldMatrix = WorldMatrix;
                VRageRender.MyRenderProxy.UpdateRenderObject(Render.RenderObjectIDs[0], ref worldMatrix, false, m_actualWorldAABB);
                m_aabb = m_actualWorldAABB;
            }
        }

        Vector3D m_crosshairPoint;
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

            //Vector3 endPoint = m_crosshairPoint;

            if (MySession.ControlledEntity == this)
            {
                LineD line = new LineD(MySector.MainCamera.Position, endPoint);
                //Line line = new Line(MySector.MainCamera.Position, MySector.MainCamera.Position + MySector.MainCamera.ForwardVector * 1000);
                var intersection = MyEntities.GetIntersectionWithLine(ref line, this, (MyEntity)m_currentWeapon);

                if (intersection.HasValue)
                {
                    return intersection.Value.IntersectionPointInWorldSpace;
                }
                else
                {
                    return line.To;
                }
            }
            else
            {
                return endPoint;
            }
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
                MoveAndRotate(Vector3.Zero, rotationIndicator, roll, m_movementFlags);
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
            MoveAndRotate(moveIndicator, rotationIndicator, roll, m_movementFlags);
        }

        public void MoveAndRotate(Vector3 moveIndicator, Vector2 rotationIndicator, float roll, MyCharacterMovementFlags movementFlags)
        {
            if (Physics == null)
                return;

            if (DebugMode)
                return;

            bool movementsFlagsChanged = (m_movementFlags | movementFlags) != m_movementFlags;
            m_movementFlags |= movementFlags;

            if (MyFakes.CHARACTER_SERVER_SYNC && ControllerInfo.IsLocallyControlled())
                SyncObject.MoveAndRotate(moveIndicator, new Vector3(rotationIndicator.X, rotationIndicator.Y, roll), m_movementFlags);

            if (MyFakes.CHARACTER_SERVER_SYNC && ControllerInfo.IsRemotelyControlled())
            {
                moveIndicator = SyncObject.CachedMovementState.MoveIndicator;
                rotationIndicator = new Vector2(SyncObject.CachedMovementState.RotationIndicator.X, SyncObject.CachedMovementState.RotationIndicator.Y);
                roll = SyncObject.CachedMovementState.RotationIndicator.Z;

                movementsFlagsChanged = m_movementFlags != SyncObject.CachedMovementState.MovementFlags;
                m_movementFlags = SyncObject.CachedMovementState.MovementFlags;
            }

            //Died character
            if (Physics.CharacterProxy == null)
            {
                moveIndicator = Vector3.Zero;
                rotationIndicator = Vector2.Zero;
                roll = 0;
            }

            m_moveAndRotateCounter++;

            float posx = 0, posy = 0;

            bool sprint = moveIndicator.Z != 0 && WantsSprint;
            bool walk = WantsWalk;
            bool jump = WantsJump;                                                                              //flying
            bool canMove = !CanFly() && !((m_currentCharacterState == HkCharacterStateType.HK_CHARACTER_IN_AIR || (int)m_currentCharacterState == 5) && (m_currentJump <= 0)) && (m_currentMovementState != MyCharacterMovementEnum.Died);
            bool canRotate = (CanFly() || !((m_currentCharacterState == HkCharacterStateType.HK_CHARACTER_IN_AIR || (int)m_currentCharacterState == 5) && (m_currentJump <= 0))) && (m_currentMovementState != MyCharacterMovementEnum.Died);

            float acceleration = 0;

            if (canMove || CanFly() || movementsFlagsChanged)
            {
                if (moveIndicator.LengthSquared() > 0)
                {
                    moveIndicator = Vector3.Normalize(moveIndicator); //normalize movement speed

                    //SyncObject.MoveAndRotate();
                }

                MyCharacterMovementEnum newMovementState = GetNewMovementState(ref moveIndicator, ref acceleration, sprint, walk, canMove, movementsFlagsChanged);

                SwitchAnimation(newMovementState);

                SetCurrentMovementState(newMovementState);

                if (!IsIdle)
                    m_currentWalkDelay = MathHelper.Clamp(m_currentWalkDelay - MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS, 0, m_currentWalkDelay);

                if (CanFly() && !canMove)
                {
                    acceleration = MyPerGameSettings.CharacterMovement.WalkAcceleration;
                }

                if (CanFly() && !canMove)
                {
                    Vector2 movementFwUp = new Vector2(-moveIndicator.Z, moveIndicator.Y);

                    if (WantsFlyDown || WantsFlyUp)
                    {
                        float len = movementFwUp.Length();
                        // len is guaranteed to be non-zero because of WantsFlyDown || WantsFlyUp
                        Debug.Assert(len != 0, "Movement vector is zero, but character wants to fly!?");
                        movementFwUp = movementFwUp / len;
                        Physics.CharacterProxy.ElevateVector = WorldMatrix.Up * movementFwUp.Y + WorldMatrix.Forward * movementFwUp.X;
                        Physics.CharacterProxy.ElevateUpVector = WorldMatrix.Up * movementFwUp.X - WorldMatrix.Forward * movementFwUp.Y;
                        moveIndicator.Z = -len;
                        moveIndicator.Y = 0;
                    }

                    if (Physics.CharacterProxy != null)
                    {
                        Physics.CharacterProxy.Elevate = (WantsFlyDown || WantsFlyUp) ? 1 : 0;
                    }

                    m_isFlying = true;
                }
                else
                {
                    if (Physics.CharacterProxy != null)
                    {
                        Physics.CharacterProxy.Elevate = 0;
                    }
                    m_isFlying = false;
                }

                if (CanFly() || canMove)
                {
                    if (m_currentWalkDelay <= 0)
                    {
                        m_currentSpeed += acceleration * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                    }

                    LimitMaxSpeed();
                }

                //MyTrace.Watch("m_currentSpeed", m_currentSpeed.ToString());


                posx = m_currentMovementState != MyCharacterMovementEnum.Sprinting ? -moveIndicator.X : 0;
                posy = moveIndicator.Z;

                if (Physics.CharacterProxy != null)
                {

                    Physics.CharacterProxy.PosX = posx;
                    Physics.CharacterProxy.PosY = posy;
                }

                m_isFlying &= posx != 0 || posy != 0;

                if (CanFly() || canMove)
                {
                    if (m_currentMovementState != MyCharacterMovementEnum.Jump)
                    {
                        int sign = Math.Sign(m_currentSpeed);
                        m_currentSpeed += -sign * m_currentDecceleration * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

                        if (Math.Sign(sign) != Math.Sign(m_currentSpeed))
                            m_currentSpeed = 0;
                    }
                }

                if ((jump && m_currentMovementState != MyCharacterMovementEnum.Jump) && (!CanFly()))
                {
                    PlayCharacterAnimation("Jump", false, MyPlayAnimationMode.Immediate, 0.0f, 1.3f);
                    m_currentJump = 0.55f;
                    SetCurrentMovementState(MyCharacterMovementEnum.Jump);
                    m_canJump = true;

                    //VRage.Trace.MyTrace.Send( VRage.Trace.TraceWindow.Default, "jump");
                }

                if (m_currentJump > 0)
                {
                    m_currentJump -= MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                    if (m_currentJump < 0.8f && m_canJump)
                    {
                        if (Physics.CharacterProxy != null)
                        {
                            Physics.CharacterProxy.Jump = true;
                        }
                        //Physics.CharacterProxy.Gravity = new Vector3(0, -40, 0);
                        m_canJump = false;
                    }


                    if (m_currentJump <= 0)
                    {
                        MyCharacterMovementEnum afterJumpState = MyCharacterMovementEnum.Standing;

                        if (!CanFly() && ((Physics.CharacterProxy != null && Physics.CharacterProxy.GetState() == HkCharacterStateType.HK_CHARACTER_IN_AIR) || (Physics.CharacterProxy != null && (int)Physics.CharacterProxy.GetState() == 5)))
                            StartFalling();
                        else
                            if (CanFly() && ((Physics.CharacterProxy != null && Physics.CharacterProxy.GetState() == HkCharacterStateType.HK_CHARACTER_IN_AIR) || (Physics.CharacterProxy != null && (int)Physics.CharacterProxy.GetState() == 5)))
                            {
                                afterJumpState = MyCharacterMovementEnum.Flying;
                                PlayCharacterAnimation("Jetpack", true, MyPlayAnimationMode.Immediate, 0.2f);

                                m_canJump = true;

                                SetCurrentMovementState(afterJumpState);
                            }
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
                                                PlayCharacterAnimation("Sprint", true, MyPlayAnimationMode.WaitForPreviousEnd, 0.2f);
                                            }
                                            else
                                            {
                                                afterJumpState = MyCharacterMovementEnum.Walking;
                                                PlayCharacterAnimation("Walk", true, MyPlayAnimationMode.WaitForPreviousEnd, 0.5f);
                                            }
                                        }
                                        else
                                        {
                                            afterJumpState = MyCharacterMovementEnum.BackWalking;
                                            PlayCharacterAnimation("WalkBack", true, MyPlayAnimationMode.WaitForPreviousEnd, 0.5f);
                                        }
                                    }
                                    else
                                    {
                                        if (moveIndicator.Z < 0)
                                        {
                                            afterJumpState = MyCharacterMovementEnum.CrouchWalking;
                                            PlayCharacterAnimation("CrouchWalk", true, MyPlayAnimationMode.WaitForPreviousEnd, 0.2f);
                                        }
                                        else
                                        {
                                            afterJumpState = MyCharacterMovementEnum.CrouchBackWalking;
                                            PlayCharacterAnimation("CrouchWalkBack", true, MyPlayAnimationMode.WaitForPreviousEnd, 0.2f);
                                        }
                                    }
                                }
                                else
                                {
                                    afterJumpState = MyCharacterMovementEnum.Standing;
                                    PlayCharacterAnimation("Idle", true, MyPlayAnimationMode.WaitForPreviousEnd, 0.2f);
                                }

                                PlayFallSound();
                                m_canJump = true;

                                SetCurrentMovementState(afterJumpState);
                            }

                        m_currentJump = 0;
                    }
                }
            }
            else
                if (Physics.CharacterProxy != null)
                {
                    Physics.CharacterProxy.Elevate = 0;
                }

                if (rotationIndicator.Y != 0 && (canRotate || m_isFalling || m_currentJump > 0))
                {
                    if (CanFly())
                    {
                        MatrixD rotationMatrix = WorldMatrix.GetOrientation();
                        Vector3D translationDraw = WorldMatrix.Translation;
                        Vector3D translationPhys = Physics.GetWorldMatrix().Translation;

                        rotationMatrix = rotationMatrix * MatrixD.CreateFromAxisAngle(WorldMatrix.Up, -rotationIndicator.Y * CHARACTER_Y_ROTATION_SPEED);

                        rotationMatrix.Translation = (Vector3D)translationPhys;

                        WorldMatrix = rotationMatrix;

                        rotationMatrix.Translation = translationDraw;
                        PositionComp.SetWorldMatrix(rotationMatrix, Physics);
                    }
                    else
                    {
                        var rotationMatrix = Matrix.CreateRotationY(-rotationIndicator.Y * CHARACTER_Y_ROTATION_SPEED);
                        var characterMatrix = Matrix.CreateWorld(Physics.CharacterProxy.Position, Physics.CharacterProxy.Forward, Physics.CharacterProxy.Up);

                        characterMatrix = rotationMatrix * characterMatrix;

                        Physics.CharacterProxy.Forward = characterMatrix.Forward;
                        Physics.CharacterProxy.Up = characterMatrix.Up;
                    }


                    const float ANGLE_FOR_ROTATION_ANIMATION = 20;

                    if ((Math.Abs(rotationIndicator.Y) > ANGLE_FOR_ROTATION_ANIMATION) && m_currentRotationDelay <= 0 &&
                        (m_currentMovementState == MyCharacterMovementEnum.Standing || m_currentMovementState == MyCharacterMovementEnum.Crouching)
                        )
                    {
                        if (WantsCrouch)
                        {
                            if (rotationIndicator.Y > 0)
                            {
                                SwitchAnimation(MyCharacterMovementEnum.CrouchRotatingRight);
                                SetCurrentMovementState(MyCharacterMovementEnum.CrouchRotatingRight);
                            }
                            else
                            {
                                SetCurrentMovementState(MyCharacterMovementEnum.CrouchRotatingLeft);
                                SwitchAnimation(MyCharacterMovementEnum.CrouchRotatingLeft);
                            }
                        }
                        else
                        {
                            if (rotationIndicator.Y > 0)
                            {
                                SwitchAnimation(MyCharacterMovementEnum.RotatingRight);
                                SetCurrentMovementState(MyCharacterMovementEnum.RotatingRight);
                            }
                            else
                            {
                                SwitchAnimation(MyCharacterMovementEnum.RotatingLeft);
                                SetCurrentMovementState(MyCharacterMovementEnum.RotatingLeft);
                            }
                        }

                        m_currentRotationDelay = 0.8f;
                        m_currentRotationSkipDelay = 0.1f;
                    }
                else
                {
                    m_currentRotationSkipDelay -= MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                    if (m_currentRotationSkipDelay <= 0)
                    {
                        m_currentRotationDelay = 0.0f;
                    }
                }

                if (rotationIndicator.X != 0)
                {
                    if (!CanFly())
                    {
                        if (((m_currentMovementState == MyCharacterMovementEnum.Died) && !m_isInFirstPerson)
                            ||
                            (m_currentMovementState != MyCharacterMovementEnum.Died))
                        {
                            SetHeadLocalXAngle(MathHelper.Clamp(m_headLocalXAngle - rotationIndicator.X * CHARACTER_X_ROTATION_SPEED, MinHeadLocalXAngle, MaxHeadLocalXAngle));
                            CalculateDependentMatrices();

                            int headBone = IsInFirstPersonView ? m_headBoneIndex : m_camera3rdBoneIndex;

                            if (headBone != -1)
                            {
                                m_bobQueue.Clear();
                                m_bobQueue.Enqueue(BoneTransforms[headBone].Translation);
                            }
                        }
                    }
                    else
                        if (canRotate)
                        {
                            MatrixD rotationMatrix = WorldMatrix.GetOrientation();
                            Vector3D translation = WorldMatrix.Translation + WorldMatrix.Up;

                            if (Definition.VerticalPositionFlyingOnly)
                            {
                                SetHeadLocalXAngle(MathHelper.Clamp(m_headLocalXAngle - rotationIndicator.X * CHARACTER_X_ROTATION_SPEED, MinHeadLocalXAngle, MaxHeadLocalXAngle));
                            }
                            else
                            {
                                rotationMatrix = rotationMatrix * MatrixD.CreateFromAxisAngle(WorldMatrix.Right, rotationIndicator.X * -0.002f);
                            }

                            rotationMatrix.Translation = translation - rotationMatrix.Up;

                            //Enable if we want limit character rotation in collisions
                            //if (m_shapeContactPoints.Count < 2)
                            {
                                WorldMatrix = rotationMatrix;
                                m_shapeContactPoints.Clear();
                            }
                        }
                }
            }

            if (roll != 0)
            {
                if (CanFly() && !Definition.VerticalPositionFlyingOnly)
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

            // If vertical flying, we need to change the positon and orientation using our computed matrix for new CharacterProxy
            if (CanFly() && Definition.VerticalPositionFlyingOnly && Physics.CharacterProxy != null)
            {
                var head = GetHeadMatrix(false);
                Physics.CharacterProxy.Forward = head.Forward;
                Physics.CharacterProxy.Up = head.Up;
            }


            if (Physics.CharacterProxy != null)
            {
                Physics.CharacterProxy.Speed = m_currentMovementState != MyCharacterMovementEnum.Died ? m_currentSpeed : 0;
            }

            CalculateTransforms();
            CalculateDependentMatrices();
        }

        private void RotateHead(Vector2 rotationIndicator)
        {
            float sensitivity = 0.5f;
            if (rotationIndicator.X != 0)
                SetHeadLocalXAngle(MathHelper.Clamp(m_headLocalXAngle - rotationIndicator.X * sensitivity, MinHeadLocalXAngle, MaxHeadLocalXAngle));

            if (rotationIndicator.Y != 0)
            {
                SetHeadLocalYAngle(m_headLocalYAngle - rotationIndicator.Y * sensitivity);
            }
        }

        public bool IsIdle
        {
            get { return m_currentMovementState == MyCharacterMovementEnum.Standing || m_currentMovementState == MyCharacterMovementEnum.Crouching; }
        }

        public bool CanPlaceCharacter(ref MatrixD worldMatrix, bool useCharacterCenter = false, bool checkCharacters = false)
        {
            Vector3D translation = worldMatrix.Translation;
            Quaternion rotation = Quaternion.CreateFromRotationMatrix(worldMatrix);

            if (Physics == null || Physics.CharacterProxy == null && Physics.RigidBody == null)
                return true;


            m_rigidBodyList.Clear();

            if (!useCharacterCenter)
            {
                Vector3D transformedCenter = Vector3D.TransformNormal(Physics.Center, WorldMatrix);
                translation += transformedCenter;
            }

            MyPhysics.GetPenetrationsShape(Physics.CharacterProxy != null ? Physics.CharacterProxy.GetCollisionShape() : Physics.RigidBody.GetShape(), ref translation, ref rotation, m_rigidBodyList, MyPhysics.CharacterCollisionLayer);
            bool somethingHit = false;
            foreach (var rb in m_rigidBodyList)
            {
                if (rb != null && (rb.GetBody() == null || !rb.GetBody().IsPhantom))
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

        List<HkRigidBody> m_rigidBodyList = new List<HkRigidBody>();

        public MyCharacterMovementEnum GetCurrentMovementState()
        {
            return m_currentMovementState;
        }

        internal void SetCurrentMovementState(MyCharacterMovementEnum state, bool updateSync = true)
        {
            System.Diagnostics.Debug.Assert(m_currentMovementState != MyCharacterMovementEnum.Died);
            //System.Diagnostics.Debug.Assert(!updateSync || (updateSync && ControllerInfo.IsLocalPlayer()));

            if (m_currentMovementState != state)
            {
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
				if(OnMovementStateChanged != null)
					OnMovementStateChanged(m_previousMovementState, m_currentMovementState);

                if (updateSync && SyncObject != null)
                    SyncObject.ChangeMovementState(state);
            }
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
                    PlayCharacterAnimation("Walk", true, AdjustSafeAnimationEnd(MyPlayAnimationMode.WaitForPreviousEnd), AdjustSafeAnimationBlend(0.1f));
                    break;

                case MyCharacterMovementEnum.BackWalking:
                    PlayCharacterAnimation("WalkBack", true, AdjustSafeAnimationEnd(MyPlayAnimationMode.WaitForPreviousEnd), AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.WalkingLeftBack:
                    PlayCharacterAnimation("WalkLeftBack", true, AdjustSafeAnimationEnd(MyPlayAnimationMode.WaitForPreviousEnd), AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.WalkingRightBack:
                    PlayCharacterAnimation("WalkRightBack", true, AdjustSafeAnimationEnd(MyPlayAnimationMode.WaitForPreviousEnd), AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.WalkStrafingLeft:
                    PlayCharacterAnimation("StrafeLeft", true, AdjustSafeAnimationEnd(MyPlayAnimationMode.WaitForPreviousEnd), AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.WalkStrafingRight:
                    PlayCharacterAnimation("StrafeRight", true, AdjustSafeAnimationEnd(MyPlayAnimationMode.WaitForPreviousEnd), AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.WalkingLeftFront:
                    PlayCharacterAnimation("WalkLeftFront", true, AdjustSafeAnimationEnd(MyPlayAnimationMode.WaitForPreviousEnd), AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.WalkingRightFront:
                    PlayCharacterAnimation("WalkRightFront", true, AdjustSafeAnimationEnd(MyPlayAnimationMode.WaitForPreviousEnd), AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.Running:
                    PlayCharacterAnimation("Run", true, AdjustSafeAnimationEnd(MyPlayAnimationMode.WaitForPreviousEnd), AdjustSafeAnimationBlend(0.1f));
                    break;

                case MyCharacterMovementEnum.Backrunning:
                    PlayCharacterAnimation("RunBack", true, AdjustSafeAnimationEnd(MyPlayAnimationMode.WaitForPreviousEnd), AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.RunningLeftBack:
                    PlayCharacterAnimation("RunLeftBack", true, AdjustSafeAnimationEnd(MyPlayAnimationMode.WaitForPreviousEnd), AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.RunningRightBack:
                    PlayCharacterAnimation("RunRightBack", true, AdjustSafeAnimationEnd(MyPlayAnimationMode.WaitForPreviousEnd), AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.RunStrafingLeft:
                    PlayCharacterAnimation("RunLeft", true, AdjustSafeAnimationEnd(MyPlayAnimationMode.WaitForPreviousEnd), AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.RunStrafingRight:
                    PlayCharacterAnimation("RunRight", true, AdjustSafeAnimationEnd(MyPlayAnimationMode.WaitForPreviousEnd), AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.RunningLeftFront:
                    PlayCharacterAnimation("RunLeftFront", true, AdjustSafeAnimationEnd(MyPlayAnimationMode.WaitForPreviousEnd), AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.RunningRightFront:
                    PlayCharacterAnimation("RunRightFront", true, AdjustSafeAnimationEnd(MyPlayAnimationMode.WaitForPreviousEnd), AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.CrouchWalking:
                    PlayCharacterAnimation("CrouchWalk", true, AdjustSafeAnimationEnd(MyPlayAnimationMode.WaitForPreviousEnd), AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.CrouchWalkingLeftFront:
                    PlayCharacterAnimation("CrouchWalkLeftFront", true, AdjustSafeAnimationEnd(MyPlayAnimationMode.WaitForPreviousEnd), AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.CrouchWalkingRightFront:
                    PlayCharacterAnimation("CrouchWalkRightFront", true, AdjustSafeAnimationEnd(MyPlayAnimationMode.WaitForPreviousEnd), AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.CrouchBackWalking:
                    PlayCharacterAnimation("CrouchWalkBack", true, AdjustSafeAnimationEnd(MyPlayAnimationMode.WaitForPreviousEnd), AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.CrouchWalkingLeftBack:
                    PlayCharacterAnimation("CrouchWalkLeftBack", true, AdjustSafeAnimationEnd(MyPlayAnimationMode.WaitForPreviousEnd), AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.CrouchWalkingRightBack:
                    PlayCharacterAnimation("CrouchWalkRightBack", true, AdjustSafeAnimationEnd(MyPlayAnimationMode.WaitForPreviousEnd), AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.CrouchStrafingLeft:
                    PlayCharacterAnimation("CrouchStrafeLeft", true, AdjustSafeAnimationEnd(MyPlayAnimationMode.WaitForPreviousEnd), AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.CrouchStrafingRight:
                    PlayCharacterAnimation("CrouchStrafeRight", true, AdjustSafeAnimationEnd(MyPlayAnimationMode.WaitForPreviousEnd), AdjustSafeAnimationBlend(0.2f));
                    break;

                case MyCharacterMovementEnum.Sprinting:
                    PlayCharacterAnimation("Sprint", true, AdjustSafeAnimationEnd(MyPlayAnimationMode.WaitForPreviousEnd), AdjustSafeAnimationBlend(0.1f));
                    break;

                case MyCharacterMovementEnum.Standing:
                    PlayCharacterAnimation("Idle", true, MyPlayAnimationMode.WaitForPreviousEnd, 0.2f);
                    break;

                case MyCharacterMovementEnum.Crouching:
                    PlayCharacterAnimation("CrouchIdle", true, MyPlayAnimationMode.WaitForPreviousEnd, 0.1f);
                    break;

                case MyCharacterMovementEnum.Flying:
                    PlayCharacterAnimation("Jetpack", true, MyPlayAnimationMode.Immediate, 0.0f);
                    break;

                //Multiplayer
                case MyCharacterMovementEnum.Jump:
                    PlayCharacterAnimation("Jump", false, MyPlayAnimationMode.Immediate, 0.0f, 1.3f);
                    break;

                case MyCharacterMovementEnum.Falling:
                    PlayCharacterAnimation("FreeFall", true, MyPlayAnimationMode.Immediate, 0.2f);
                    break;

                case MyCharacterMovementEnum.CrouchRotatingLeft:
                    PlayCharacterAnimation("CrouchLeftTurn", true, MyPlayAnimationMode.WaitForPreviousEnd, 0.2f);
                    break;

                case MyCharacterMovementEnum.CrouchRotatingRight:
                    PlayCharacterAnimation("CrouchRightTurn", true, MyPlayAnimationMode.WaitForPreviousEnd, 0.2f);
                    break;

                case MyCharacterMovementEnum.RotatingLeft:
                    PlayCharacterAnimation("StandLeftTurn", true, MyPlayAnimationMode.WaitForPreviousEnd, 0.2f);
                    break;

                case MyCharacterMovementEnum.RotatingRight:
                    PlayCharacterAnimation("StandRightTurn", true, MyPlayAnimationMode.WaitForPreviousEnd, 0.2f);
                    break;

                case MyCharacterMovementEnum.Died:
                    PlayCharacterAnimation("Died", false, MyPlayAnimationMode.Immediate, 0.5f);
                    break;

                case MyCharacterMovementEnum.Sitting:
                    break;


                default:
                    System.Diagnostics.Debug.Assert(false, "Unknown movement state");
                    break;
            }
        }

        MyCharacterMovementEnum GetNewMovementState(ref Vector3 moveIndicator, ref float acceleration, bool sprint, bool walk, bool canMove, bool movementFlagsChanged)
        {
            MyCharacterMovementEnum newMovementState = m_currentMovementState;

            if (Definition.UseOnlyWalking)
                walk = true;

            if (m_currentJump > 0f)
                return MyCharacterMovementEnum.Jump;

            if (CanFly())
                return MyCharacterMovementEnum.Flying;

            bool moving = ((moveIndicator.X != 0 || moveIndicator.Z != 0) && canMove);
            if (moving || movementFlagsChanged)
            {
                if (sprint)
                {
                    newMovementState = GetSprintState(ref moveIndicator);
                }
                else
                    if (moving)
                    {
                        if (walk)
                            newMovementState = GetWalkingState(ref moveIndicator);
                        else
                            newMovementState = GetRunningState(ref moveIndicator);
                    }
                    else
                    {
                        newMovementState = GetIdleState();
                    }

                acceleration = GetMovementAcceleration(newMovementState);
                m_currentDecceleration = 0;
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
                            if (WantsCrouch && !CanFly() && (m_currentRotationDelay <= 0))
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
                        if (m_currentRotationDelay <= 0)
                            newMovementState = GetIdleState();
                        break;

                    default:
                        //System.Diagnostics.Debug.Assert(false, "Unknown movement state");
                        break;
                }
            }

            return newMovementState;
        }

        private void LimitMaxSpeed()
        {
            switch (m_currentMovementState)
            {
                case MyCharacterMovementEnum.Running:
                case MyCharacterMovementEnum.Flying:
                    {
                        m_currentSpeed = MathHelper.Clamp(m_currentSpeed, -Definition.MaxRunSpeed, Definition.MaxRunSpeed);
                        break;
                    }

                case MyCharacterMovementEnum.Walking:
                    {
                        m_currentSpeed = MathHelper.Clamp(m_currentSpeed, -Definition.MaxWalkSpeed, Definition.MaxWalkSpeed);
                        break;
                    }

                case MyCharacterMovementEnum.BackWalking:
                case MyCharacterMovementEnum.WalkingLeftBack:
                case MyCharacterMovementEnum.WalkingRightBack:
                    {
                        m_currentSpeed = MathHelper.Clamp(m_currentSpeed, -Definition.MaxBackwalkSpeed, Definition.MaxBackwalkSpeed);
                        break;
                    }

                case MyCharacterMovementEnum.WalkStrafingLeft:
                case MyCharacterMovementEnum.WalkStrafingRight:
                case MyCharacterMovementEnum.WalkingLeftFront:
                case MyCharacterMovementEnum.WalkingRightFront:
                    {
                        m_currentSpeed = MathHelper.Clamp(m_currentSpeed, -Definition.MaxWalkStrafingSpeed, Definition.MaxWalkStrafingSpeed);
                        break;
                    }


                case MyCharacterMovementEnum.Backrunning:
                case MyCharacterMovementEnum.RunningLeftBack:
                case MyCharacterMovementEnum.RunningRightBack:
                    {
                        m_currentSpeed = MathHelper.Clamp(m_currentSpeed, -Definition.MaxBackrunSpeed, Definition.MaxBackrunSpeed);
                        break;
                    }

                case MyCharacterMovementEnum.RunStrafingLeft:
                case MyCharacterMovementEnum.RunStrafingRight:
                case MyCharacterMovementEnum.RunningLeftFront:
                case MyCharacterMovementEnum.RunningRightFront:
                    {
                        m_currentSpeed = MathHelper.Clamp(m_currentSpeed, -Definition.MaxRunStrafingSpeed, Definition.MaxRunStrafingSpeed);
                        break;
                    }


                case MyCharacterMovementEnum.CrouchWalking:
                    {
                        m_currentSpeed = MathHelper.Clamp(m_currentSpeed, -Definition.MaxCrouchWalkSpeed, Definition.MaxCrouchWalkSpeed);
                        break;
                    }

                case MyCharacterMovementEnum.CrouchStrafingLeft:
                case MyCharacterMovementEnum.CrouchStrafingRight:
                case MyCharacterMovementEnum.CrouchWalkingLeftFront:
                case MyCharacterMovementEnum.CrouchWalkingRightFront:
                    {
                        m_currentSpeed = MathHelper.Clamp(m_currentSpeed, -Definition.MaxCrouchStrafingSpeed, Definition.MaxCrouchStrafingSpeed);
                        break;
                    }

                case MyCharacterMovementEnum.CrouchBackWalking:
                case MyCharacterMovementEnum.CrouchWalkingLeftBack:
                case MyCharacterMovementEnum.CrouchWalkingRightBack:
                    {
                        m_currentSpeed = MathHelper.Clamp(m_currentSpeed, -Definition.MaxCrouchBackwalkSpeed, Definition.MaxCrouchBackwalkSpeed);
                        break;
                    }

                case MyCharacterMovementEnum.Sprinting:
                    {
                        m_currentSpeed = MathHelper.Clamp(m_currentSpeed, -Definition.MaxSprintSpeed, Definition.MaxSprintSpeed);
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
        }

        private float AdjustSafeAnimationBlend(float idealBlend)
        {
            float blend = 0;
            if (m_currentAnimationChangeDelay > SAFE_DELAY_FOR_ANIMATION_BLEND)
                blend = idealBlend;
            m_currentAnimationChangeDelay = 0;
            return blend;
        }

        private MyPlayAnimationMode AdjustSafeAnimationEnd(MyPlayAnimationMode idealEnd)
        {
            MyPlayAnimationMode end = MyPlayAnimationMode.Immediate;
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

            // TODO: This should be changed to ragdoll capsules in future
            int i = 0;
            foreach (var boneList in m_bodyCapsuleBones)
            {
                m_bodyCapsules[i].P0 = (Bones[boneList.First()].AbsoluteTransform * WorldMatrix).Translation;
                m_bodyCapsules[i].P1 = (Bones[boneList.Last()].AbsoluteTransform * WorldMatrix).Translation;
                Vector3 difference = m_bodyCapsules[i].P0 - m_bodyCapsules[i].P1;
                m_bodyCapsules[i].Radius = difference.Length() * 0.3f;

                if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_SHOW_DAMAGE)
                {
                    MyRenderProxy.DebugDrawCapsule(m_bodyCapsules[i].P0, m_bodyCapsules[i].P1, m_bodyCapsules[i].Radius, Color.Green, false, false);                    
                }

                i++;
            }
            m_characterBonesReady = true;
        }
        #endregion

        #region Debug draw

        private MatrixD GetHeadMatrixInternal(int headBone, bool includeY, bool includeX = true, bool forceHeadAnim = false, bool forceHeadBone = false)
        {
            //Matrix matrixRotation = Matrix.Identity;
            MatrixD matrixRotation = MatrixD.Identity;

            bool useAnimationInsteadX = ShouldUseAnimatedHeadRotation() && (!CanFly() || (CanFly() && IsLocalHeadAnimationInProgress())) || forceHeadAnim;

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
                    averageBob = BoneTransforms[headBone].Translation;
                }
            }


            if (useAnimationInsteadX && headBone != -1)
            {
                //m_headMatrix = Matrix.CreateRotationX(-(float)Math.PI * 0.5f) * /* Matrix.CreateRotationY(-(float)Math.PI * 0.5f) */ Matrix.Normalize(BoneTransformsWrite[HEAD_DUMMY_BONE]);
                Matrix hm = Matrix.Normalize(BoneTransforms[headBone]);
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
            return Matrix.Invert(GetHeadMatrixInternal(m_camera3rdBoneIndex, includeY, includeX));
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
                        m_switchBackToFirstPersonTimer = m_cameraSwitchDelay;
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
                        m_switchBackToSpectatorTimer =  m_cameraSwitchDelay;
                    }
                }
            }

            MatrixD matrix = GetHeadMatrix(false, true);

            if (MyFakes.CHARACTER_FACE_FORWARD > 0)
            {
                matrix.Translation += matrix.Forward * MyFakes.CHARACTER_FACE_FORWARD;
            }

            m_lastCorrectSpectatorCamera = MatrixD.Zero;

            return MatrixD.Invert(matrix);
        }

        int m_hitCapsule = -1;
        float m_hitTimeout = 1;
        Vector3 m_hitPosition;
        Vector3 m_hitPosition2;
        Vector3 m_hitNormal;
        Vector3 m_hitNormal2;
        internal override bool GetIntersectionWithLine(ref LineD line, out MyIntersectionResultLineTriangleEx? t, IntersectionFlags flags = IntersectionFlags.ALL_TRIANGLES)
        {
            // TODO: This now uses caspule of physics rigid body on the character, it needs to be changed to ragdoll
            //       Currently this approach will be used to support Characters with different skeleton than humanoid

            t = null;

            UpdateCapsuleBones();            

            if (m_characterBonesReady == false)
                return false;

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
                    m_hitCapsule = i;
                    m_hitTimeout = 1;

                    m_hitPosition = hitPosition;
                    m_hitPosition2 = hitPosition2;
                    m_hitNormal = hitNormal;
                    m_hitNormal2 = hitNormal2;

                    MyTriangle_Vertexes vertexes = new MyTriangle_Vertexes();
                    //TODO: Make correct alg. to make triangle from capsule intersection
                    vertexes.Vertex0 = m_hitPosition + line.Direction * 0.5f;
                    vertexes.Vertex1 = m_hitPosition + hitNormal * 0.5f;
                    vertexes.Vertex2 = m_hitPosition - hitNormal * 0.8f;

                    t = new MyIntersectionResultLineTriangleEx(
                        new MyIntersectionResultLineTriangle(
                        ref vertexes,
                        ref hitNormal,
                        Vector3.Distance(m_hitPosition, line.From)),
                        this, ref line,
                        (Vector3D)m_hitPosition,
                        m_hitNormal);

                    if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW)
                    {
                        MyRenderProxy.DebugDrawCapsule(capsule.P0, capsule.P1, capsule.Radius, Color.Red, false, false);
                        MyRenderProxy.DebugDrawSphere(hitPosition, 0.1f, Color.White, 1f, false);
                    }

                    return true;
                }
            }

            t = null;
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
                if(!UseAnimationForWeapon)
                    StopUpperCharacterAnimation(0);
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
            m_currentShotTime = ShotTime;

            if (m_cameraShake != null && m_currentWeapon.ShakeAmount != 0.0f)
                m_cameraShake.AddShake(MyUtils.GetRandomFloat(1.5f, m_currentWeapon.ShakeAmount));

            if (m_currentWeapon.BackkickForcePerSecond > 0 && (CanFly() || m_isFalling))
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

        public void Zoom(bool newKeyPress)
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
                            m_secondarySoundEmitter.PlaySound(CharacterSounds[(int)CharacterSoundsEnum.IRONSIGHT_ACT_SOUND], true);
                            EnableIronsight(true, newKeyPress, true);
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
                        m_secondarySoundEmitter.PlaySound(CharacterSounds[(int)CharacterSoundsEnum.IRONSIGHT_DEACT_SOUND], true);
                        EnableIronsight(false, newKeyPress, true);
                    }
                    break;
            }
        }

        void EnableIronsight(bool enable, bool newKeyPress, bool changeCamera, bool updateSync = true)
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

        void SendFlags()
        {
            SyncObject.ChangeFlags(JetpackEnabled, DampenersEnabled, LightEnabled, m_zoomMode == MyZoomModeEnum.IronSight, m_radioBroadcaster.WantsToBeEnabled);
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
                item = m_inventory.FindItem(physicalItemId);
            }
            else
            {
                item = m_inventory.FindItem(weaponDefinition);
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
            return !MyPerGameSettings.EnableWeaponWithoutInventory;
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
				var handToolGun = gunEntity as MyObjectBuilder_HandTool;
				if(handToolGun != null)
				{
					handToolGun.IsDeconstructor = weapon.IsDeconstructor;
					SwitchToWeaponInternal(weaponDefinition, sync, true, handToolGun, 0);
				}
				else
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
            UseAnimationForWeapon = false;

            if (weaponDefinition.HasValue)
            {
                if (checkInventory)
                {
                    var item = m_inventory.FindItem(weaponDefinition.Value);
                    if (item.HasValue)
                    {
                        var physicalGunObject = item.Value.Content as MyObjectBuilder_PhysicalGunObject;
                        physicalGunObject.GunEntity = gunBuilder;
                        var gun = CreateGun(gunBuilder);
                        weaponEntityBuilder = gun.PhysicalObject.GunEntity;
                        EquipWeapon(gun);
                    }
                    m_inventoryResults.Clear();
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
                            weaponEntityBuilder = (MyObjectBuilder_EntityBase)MyObjectBuilderSerializer.CreateNewObject(weaponDefinition.Value.TypeId);
                        weaponEntityBuilder.EntityId = weaponEntityId;
                        if (WeaponTakesBuilderFromInventory(weaponDefinition))
                        {
                            var item = m_inventory.FindItem(weaponDefinition.Value);
                            if (item.HasValue)
                                (item.Value.Content as MyObjectBuilder_PhysicalGunObject).GunEntity = gunBuilder;
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

        void UpdateShadowIgnoredObjects(MyEntity parent)
        {
            Render.UpdateShadowIgnoredObjects(parent);
            foreach (var child in parent.Hierarchy.Children)
            {
                UpdateShadowIgnoredObjects(child.Container.Entity as MyEntity);
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
                        m_soundEmitter.StopSound(true);
                    }

                    detectorComponent.UseObject.Use(UseActionEnum.Manipulate, this);
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
            if (m_currentMovementState != MyCharacterMovementEnum.Died)
            {
                if (!CanFly() && !m_isFalling)
                {
                    WantsCrouch = !WantsCrouch;
                }
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
            if (m_currentMovementState != MyCharacterMovementEnum.Died)
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

        public MyGuiScreenBase ShowAggregateInventoryScreen()
        {
            MyGuiScreenBase screen = null;
            if (MyPerGameSettings.GUI.InventoryScreen != null)
            {
                var aggregateComponent = Components.Get<MyComponentInventoryAggregate>();
                if (aggregateComponent != null)
                {
                    aggregateComponent.Init();
                    screen = MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.InventoryScreen, aggregateComponent);
                    MyGuiSandbox.AddScreen( screen );
                }
            }
            return screen;
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

        public void EnableDampeners(bool enable, bool updateSync = true)
        {
            if (m_dampenersEnabled != enable)
            {
                m_dampenersEnabled = enable;
                if (updateSync)
                    SendFlags();
            }
        }

        public void EnableJetpack(bool enable, bool fromLoad = false, bool updateSync = true, bool fromInit = false)
        {
            if (m_currentMovementState == MyCharacterMovementEnum.Sitting)
                return;

            if (!m_characterDefinition.JetpackAvailable)
                enable = false;

            if (MySession.Static.SurvivalMode && !MyFakes.ENABLE_JETPACK_IN_SURVIVAL)
                enable = false;

            SwitchToJetpackRagdoll = enable;

            bool valueChanged = m_jetpackEnabled != enable;
            m_jetpackEnabled = enable;

            if (valueChanged && updateSync)
                SendFlags();

            RecalculatePowerRequirement();

            if (!ControllerInfo.IsLocallyControlled() && !fromInit && !Sync.IsServer && !MyFakes.CHARACTER_SERVER_SYNC)
                return;

            StopFalling();

            bool noEnergy = false;
            bool canUseJetpack = enable;

            if (!IsJetpackPowered() && canUseJetpack)
            {
                canUseJetpack = false;
                noEnergy = true;
            }

            if (canUseJetpack)
                IsUsing = null;

            if (MySession.ControlledEntity == this && valueChanged)
            {
                m_jetpackToggleNotification.Text = (noEnergy) ? MySpaceTexts.NotificationJetpackOffNoEnergy
                                                     : (canUseJetpack) ? MySpaceTexts.NotificationJetpackOn
                                                                          : MySpaceTexts.NotificationJetpackOff;
                MyHud.Notifications.Add(m_jetpackToggleNotification);
            }

            if (Physics.CharacterProxy != null)
            {
                Physics.CharacterProxy.Forward = (Vector3)WorldMatrix.Forward;
                Physics.CharacterProxy.Up = (Vector3)WorldMatrix.Up;
                Physics.CharacterProxy.EnableFlyingState(CanFly());

                if (m_currentMovementState != MyCharacterMovementEnum.Died)
                {
                    //flying
                    if (!CanFly() && (Physics.CharacterProxy.GetState() == HkCharacterStateType.HK_CHARACTER_IN_AIR || (int)Physics.CharacterProxy.GetState() == 5))
                        StartFalling();
                    else
                    {
                        PlayCharacterAnimation("Idle", true, MyPlayAnimationMode.Immediate, 0.2f);
                        SetCurrentMovementState(MyCharacterMovementEnum.Standing);
                    }
                }

                if (CanFly() && m_currentMovementState != MyCharacterMovementEnum.Died)
                {
                    PlayCharacterAnimation("Jetpack", true, MyPlayAnimationMode.Immediate, 0.0f);
                    SetCurrentMovementState(MyCharacterMovementEnum.Flying);

                    SetLocalHeadAnimation(0, 0, 0.3f);
                }

                // When disabling the jetpack normally during the game in zero-G, disable jetpack autoenable
                if (!fromLoad && !enable && Physics.CharacterProxy.Gravity.LengthSquared() <= 0.1f)
                {
                    m_currentAutoenableJetpackDelay = -1;
                }
            }
            //else //Jetpack enabled on network character, update its properties if it is not kinematic
            //{
            //    if (MyPerGameSettings.NetworkCharacterType == RigidBodyFlag.RBF_DEFAULT && Physics.RigidBody != null)
            //    {
            //        if (enable)
            //        {
            //            Physics.RigidBody.UpdateMotionType(HkMotionType.Keyframed);
            //        }
            //        else
            //        {
            //            Physics.RigidBody.UpdateMotionType(HkMotionType.Dynamic);
            //        }
            //    }
            //}           
        }

        /// <summary>
        /// Switches jetpack modes for character.
        /// </summary>
        public void SwitchDamping()
        {
            if (m_currentMovementState != MyCharacterMovementEnum.Died)
            {
                EnableDampeners(!m_dampenersEnabled, true);

                m_inertiaDampenersNotification.Text = (m_dampenersEnabled ? MySpaceTexts.NotificationInertiaDampenersOn : MySpaceTexts.NotificationInertiaDampenersOff);
                MyHud.Notifications.Add(m_inertiaDampenersNotification);
            }
        }

        public void SwitchThrusts()
        {
            if (m_currentMovementState != MyCharacterMovementEnum.Died && ((!MySession.Static.SimpleSurvival && MyPerGameSettings.Game != GameEnum.ME_GAME) || !MySession.Static.SurvivalMode) && !MySession.Static.Battle)
            {
                EnableJetpack(!JetpackEnabled);
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
            if (controller.Player.IsLocalPlayer())
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
                EnableJetpack(m_jetpackEnabled);

                m_suitBattery.OwnedByLocalPlayer = true;
                DisplayName = controller.Player.Identity.DisplayName;
            }
            else
            {
                DisplayName = controller.Player.Identity.DisplayName;
                UpdateHudMarker();
            }

            if (Health <= MinHealth)
            {
                m_dieAfterSimulation = true;
                return;
            }

            if (m_currentWeapon != null)
                m_currentWeapon.OnControlAcquired(this);

            UpdateCharacterPhysics(controller.Player.IsLocalPlayer());
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
            }
            else
            {
                if (!MyFakes.ENABLE_RADIO_HUD)
                {
                    MyHud.LocationMarkers.UnregisterMarker(this);
                }
            }

            m_soundEmitter.StopSound(true);
        }

        void Static_CameraAttachedToChanged(IMyCameraController oldController, IMyCameraController newController)
        {
            if (oldController != newController && MySession.ControlledEntity == this && newController != this)
            {
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
            if (BoneTransforms != null)
                CalculateDependentMatrices();

            if (m_radioBroadcaster != null)
                m_radioBroadcaster.MoveBroadcaster();

            Render.UpdateLightPosition();
        }

        void OnCharacterStateChanged(HkCharacterStateType newState)
        {
            if (m_currentMovementState != MyCharacterMovementEnum.Died)
            {
                if (!CanFly())
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

        private void StartFalling()
        {
            if (!CanFly() && m_currentMovementState != MyCharacterMovementEnum.Died && m_currentMovementState != MyCharacterMovementEnum.Sitting)
            {
                if (m_currentCharacterState == HkCharacterStateType.HK_CHARACTER_JUMPING)
                {
                    m_currentFallingTime = -JumpTime;
                }
                else
                    m_currentFallingTime = 0;

                m_isFalling = true;
                m_crouchAfterFall = WantsCrouch;
                WantsCrouch = false;

                SetCurrentMovementState(MyCharacterMovementEnum.Falling);
            }
        }

        private void StopFalling()
        {
            if (m_currentMovementState == MyCharacterMovementEnum.Died)
                return;

            if(m_isFalling && m_previousMovementState != MyCharacterMovementEnum.Flying && (!JetpackEnabled || ! IsJetpackPowered()))
                PlayFallSound();

            if (Physics.CharacterProxy != null)
            {
                //if (m_isFallingAnimationPlayed)
                {
                    PlayCharacterAnimation("Idle", true, MyPlayAnimationMode.Immediate, 0.2f);
                    Physics.CharacterProxy.PosX = 0;
                    Physics.CharacterProxy.PosY = 0;
                    SetCurrentMovementState(MyCharacterMovementEnum.Standing);
                }
            }

            m_isFalling = false;
            m_isFallingAnimationPlayed = false;
            m_currentFallingTime = 0;
            m_canJump = true;
            WantsCrouch = m_crouchAfterFall;
            m_crouchAfterFall = false;
        }

        private void PlayFallSound()
        {
            RayCastGround();
            if (m_walkingSurfaceType != MyWalkingSurfaceType.None)
            {
                var emitter = MyAudioComponent.TryGetSoundEmitter(); //we need to use other emmiter otherwise the sound would be cut by silence next frame
                if (emitter != null)
                {
                    emitter.Entity = this;
                    var cue = (m_walkingSurfaceType == MyWalkingSurfaceType.Rock) ? CharacterSounds[(int)CharacterSoundsEnum.FALL_ROCK_SOUND] : CharacterSounds[(int)CharacterSoundsEnum.FALL_METAL_SOUND];
                    emitter.PlaySingleSound(cue);
                }
            }
        }

        bool CanFly()
        {
            if (!JetpackEnabled || !IsJetpackPowered())
                return false;

            if (IsDead)
                return false;

            return true;
        }

        public bool JetpackEnabled
        {
            get { return m_jetpackEnabled; }
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
            return m_inventory;
        }

        public MyInventoryOwnerTypeEnum InventoryOwnerType
        {
            get { return MyInventoryOwnerTypeEnum.Character; }
        }

        public bool CanStartConstruction(MyCubeBlockDefinition blockDefinition)
        {
            if (blockDefinition == null) return false;

            Debug.Assert(m_inventory != null, "Inventory is null!");
            Debug.Assert(blockDefinition.Components.Length != 0, "Missing components!");

			var inventory = MyCubeBuilder.BuildComponent.GetBuilderInventory(this);
			if(inventory == null)
				return false;

			return (inventory.GetItemAmount(blockDefinition.Components[0].Definition.Id) >= 1);
        }

        public bool CanStartConstruction(Dictionary<MyDefinitionId, int> constructionCost)
        {
            Debug.Assert(m_inventory != null, "Inventory is null!");
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

        public bool CanBeUsedBy(MyEntity user)
        {
            //return user is MyCharacter;
            return false;
        }

        public string GetUseText()
        {
            //return "Press " + MyGuiManager.GetInput().GetGameControl(MyGameControlEnums.USE).GetControlButtonName(MyGuiInputDeviceEnum.Keyboard) + " to enter cockpit";
            return "";
        }

        public void Use(MyEntity user)
        {

        }

        public bool CanShowTerminalFor(MyEntity user)
        {
            return false;
        }

        public string GetTerminalText()
        {
            return null;
        }

        public void ShowTerminal(MyEntity user)
        {
        }

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

        public bool ShowOverlay
        {
            get { return true; }
        }


        public MatrixD ActivationMatrix
        {
            get { return WorldMatrix; }
        }

        private void UnequipWeapon()
        {
            if (m_currentWeapon != null)
            {
                // save weapon ammo amount in builder
                if (m_currentWeapon.PhysicalObject != null)
                {
                    var item = m_inventory.FindItem(m_currentWeapon.PhysicalObject.GetId());
                    if (item.HasValue)
                    {
                        (item.Value.Content as MyObjectBuilder_PhysicalGunObject).GunEntity = (m_currentWeapon as MyEntity).GetObjectBuilder();
                    }
                }

                m_currentWeapon.OnControlReleased();
                var consumer = m_currentWeapon as IMyPowerConsumer;
                if (consumer != null)
                    m_suitPowerDistributor.RemoveConsumer(consumer);

                MyEntity gunEntity = (MyEntity)m_currentWeapon;
                gunEntity.OnClose -= gunEntity_OnClose;

                MyEntities.Remove(gunEntity);

                gunEntity.Close();
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

                ResetJetpackRagdoll = true;
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
            if (m_leftHandItem != null)
            {
                (m_leftHandItem as IMyHandheldGunObject<MyDeviceBase>).OnControlReleased();
                m_leftHandItem.Close(); // no dual wielding now
                m_leftHandItem = null;
            }
            MyEntity gunEntity = (MyEntity)newWeapon;
            gunEntity.Render.CastShadows = true;
            gunEntity.Render.NeedsResolveCastShadow = false;
            gunEntity.Save = false;
            gunEntity.OnClose += gunEntity_OnClose;

            MyEntities.Add(gunEntity);

            m_currentWeapon = newWeapon;
            m_currentWeapon.OnControlAcquired(this);

            if (WeaponEquiped != null)
                WeaponEquiped(m_currentWeapon);

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
            CalculateDependentMatrices();

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
                PlayCharacterAnimation(m_handItemDefinition.FingersAnimation, def.Loop, MyPlayAnimationMode.Play, 1.0f);
            }
            else
            {
                StopFingersAnimation(0);
            }

            var consumer = m_currentWeapon as IMyPowerConsumer;
            if (consumer != null)
                m_suitPowerDistributor.AddConsumer(consumer);

            if (showNotification)
            {
                var notificationUse = new MyHudNotification(MySpaceTexts.NotificationUsingWeaponType, 2000);
                notificationUse.SetTextFormatArguments(MyDeviceBase.GetGunNotificationName(newWeapon.DefinitionId));
                MyHud.Notifications.Add(notificationUse);
            }

            Static_CameraAttachedToChanged(null, null);
            MyHud.Crosshair.Show(null);

            ResetJetpackRagdoll = true;
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

        public MyPowerReceiver PowerReceiver
        {
            get;
            private set;
        }

        private void SetPowerInput(float input)
        {
            if (LightEnabled && input >= MyEnergyConstants.REQUIRED_INPUT_CHARACTER_LIGHT)
            {
                m_lightPowerFromProducer = MyEnergyConstants.REQUIRED_INPUT_CHARACTER_LIGHT;
                input -= MyEnergyConstants.REQUIRED_INPUT_CHARACTER_LIGHT;
            }
            else
                m_lightPowerFromProducer = 0;

            float lastPowerForJetpack = m_jetpackPowerFromProducer;

            if (JetpackEnabled && input >= MyEnergyConstants.REQUIRED_INPUT_JETPACK)
            {
                m_jetpackPowerFromProducer = MyEnergyConstants.REQUIRED_INPUT_JETPACK;
                input -= MyEnergyConstants.REQUIRED_INPUT_JETPACK;
            }
            else
            {
                m_jetpackPowerFromProducer = 0;
                if (JetpackEnabled && IsUsing == null)
                    EnableJetpack(false);
            }

            if (lastPowerForJetpack != m_jetpackPowerFromProducer)
            {
                if (m_jetpackPowerFromProducer > 0 && m_jetpackEnabled)
                    EnableJetpack(true);
            }
        }

        private float ComputeRequiredPower()
        {
            var result = MyEnergyConstants.REQUIRED_INPUT_LIFE_SUPPORT;
            if (Definition != null && Definition.NeedsOxygen)
            {
                result = MyEnergyConstants.REQUIRED_INPUT_LIFE_SUPPORT_WITHOUT_HELMET;
            }
            if (m_lightEnabled)
                result += MyEnergyConstants.REQUIRED_INPUT_CHARACTER_LIGHT;
            if (JetpackEnabled)
                result += MyEnergyConstants.REQUIRED_INPUT_JETPACK;
            return result;
        }

        void RecalculatePowerRequirement(bool chargeImmediatelly = false)
        {
            PowerReceiver.Update();
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

        public bool DampenersEnabled
        {
            get { return m_dampenersEnabled; }
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

            PlayCharacterAnimation(animation, true, MyPlayAnimationMode.Immediate, 0);

            StopUpperCharacterAnimation(0);
            StopFingersAnimation(0);

            SetHandAdditionalRotation(Quaternion.CreateFromAxisAngle(Vector3.Forward, MathHelper.ToRadians(0)));
            SetUpperHandAdditionalRotation(Quaternion.CreateFromAxisAngle(Vector3.Forward, MathHelper.ToRadians(0)));
            SetSpineAdditionalRotation(Quaternion.CreateFromAxisAngle(Vector3.Forward, 0), Quaternion.CreateFromAxisAngle(Vector3.Forward, 0));
            SetHeadAdditionalRotation(Quaternion.Identity, false);

            FlushAnimationQueue();

            UpdateAnimation();

            CalculateTransforms();
            CalculateDependentMatrices();

            //Unfortunatelly character has to be updated because of autoheal
            //NeedsUpdate = MyEntityUpdateEnum.NONE;

            SuitBattery.Enabled = false;
            UpdateLightPower(true);

            EnableBag(enableBag);
            //EnableHead(true);

            SetCurrentMovementState(MyCharacterMovementEnum.Sitting);

            //Because of legs visible first frame after sitting
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
            PlayCharacterAnimation("Idle", true, MyPlayAnimationMode.Immediate, 0);

            Render.NearFlag = false;

            StopUpperCharacterAnimation(0);

            SuitBattery.Enabled = true;

            EnableBag(true);
            EnableHead(true);

            SetCurrentMovementState(MyCharacterMovementEnum.Standing);
            m_wasInFirstPerson = false;
            IsUsing = null;
            //NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public void DoDamage(float damage, MyDamageType damageType, bool updateSync)
        {
            if (!CharacterCanDie && !(damageType == MyDamageType.Suicide && MyPerGameSettings.CharacterSuicideEnabled))
                return;

            CharacterAccumulatedDamage += damage;

            if (updateSync)
            {
                MySyncHelper.DoDamageSynced(this, damage, damageType);
                return;
            }

			var health = m_stats.Health;

			if (health == null)
				return;

            float oldHealth = Health;
			health.Decrease(damage);

            if (!IsDead)
            {
                PlayDamageSound(oldHealth);
				m_breath.ForceUpdate();
            }

            if (IsDead)
            {
                return;
            }

            Render.Damage();

			if (health.Value <= health.MinValue)
            {
                m_dieAfterSimulation = true;
                return;
            }

            return;
        }

        void explosionEffect_OnUpdate(object sender, EventArgs e)
        {
            MyParticleEffect effect = sender as MyParticleEffect;
            if (effect.GetElapsedTime() > 0.2f)
            {
                effect.OnUpdate -= explosionEffect_OnUpdate;
                effect.Stop();
            }
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
                        DoDamage(1000, MyDamageType.Suicide, true);
                }));
            }
        }

        void DieInternal()
        {
            if (!CharacterCanDie && !MyPerGameSettings.CharacterSuicideEnabled)
                return;

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
                m_inventory.RemoveItemsOfType(1, m_currentWeapon.PhysicalObject);
            }

            IsUsing = null;
            m_isFalling = false;
            m_jetpackEnabled = false;
            SetCurrentMovementState(MyCharacterMovementEnum.Died, false);
            UnequipWeapon();
            //m_inventory.Clear(false);
            StopUpperAnimation(0.5f);
            StartSecondarySound(Definition.DeathSoundName, sync: false);

            if (m_isInFirstPerson)
                PlayCharacterAnimation("DiedFps", false, MyPlayAnimationMode.Immediate, 0.5f);
            else
                PlayCharacterAnimation("Died", false, MyPlayAnimationMode.Immediate, 0.5f);

            //InitBoxPhysics(MyMaterialType.METAL, ModelLod0, 900, 0, MyPhysics.DefaultCollisionFilter, RigidBodyFlag.RBF_DEFAULT);
            //InitSpherePhysics(MyMaterialType.METAL, ModelLod0, 900, 0, 0, 0, RigidBodyFlag.RBF_DEFAULT);

            InitDeadBodyPhysics();

            StartRespawn(RespawnTime);

            m_currentLootingCounter = MySession.Static.CharacterLootingTime;

            if (CharacterDied != null)
                CharacterDied(this);
        }

        private void StartRespawn(float respawnTime)
        {
            if (ControllerInfo.Controller != null && ControllerInfo.Controller.Player != null)
            {
                MySessionComponentMissionTriggers.PlayerDied(this.ControllerInfo.Controller.Player.Id);
                if (!MySessionComponentMissionTriggers.CanRespawn(this.ControllerInfo.Controller.Player.Id))
                {
                    m_currentRespawnCounter = -1;
                    return;
                }
            }

            if (this == MySession.ControlledEntity)
            {
                MyGuiScreenTerminal.Hide();

                m_respawnNotification = new MyHudNotification(MySpaceTexts.NotificationRespawn, (int)(RespawnTime * 1000), priority: 5);
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

            if (Physics != null && (!MyPerGameSettings.EnableRagdollModels || Physics.Ragdoll == null || RagdollMapper == null))
            {
                velocity = Physics.LinearVelocity;

                Physics.Enabled = false;
                Physics.Close();
            }

            if (Physics == null || RagdollMapper == null || Physics.Ragdoll == null || !MyPerGameSettings.EnableRagdollModels)
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
            else 
            {
                /// Use ragdoll to die
                if (Physics.IsRagdollModeActive) Physics.CloseRagdollMode();
                if (RagdollMapper.IsActive) RagdollMapper.Deactivate();
                Physics.SwitchToRagdollMode();
                RagdollMapper.SetRagdollToDynamic();
                RagdollMapper.Activate();
                //Physics.IsPhantom = true;
                if (VirtualPhysics != null)
                {
                    VirtualPhysics.Enabled = false;
                    VirtualPhysics.Close();
                    VirtualPhysics = null;
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

        private MyControlledPhysicsBody m_virtualPhysics;
        public MyControlledPhysicsBody VirtualPhysics { get { return m_virtualPhysics; } set { m_virtualPhysics = value; } }

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
        public static MyCharacter CreateCharacter(MatrixD worldMatrix, Vector3 velocity, string characterName, string model, Vector3? colorMask, bool findNearPos = true, bool AIMode = false, MyCockpit cockpit = null, bool useInventory = true)
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
                MySyncCreate.SendEntityCreated(character.GetObjectBuilder());
            }
            else
            {
                MySyncCharacter.SendCharacterCreated(character.GetObjectBuilder() as MyObjectBuilder_Character, cockpit);
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
            MyObjectBuilder_Character objectBuilder = MyCharacter.Random();
            objectBuilder.CharacterModel = model ?? MyCharacter.DefaultModel;

            if (colorMask.HasValue)
                objectBuilder.ColorMaskHSV = colorMask.Value;

            objectBuilder.JetpackEnabled = MySession.Static.CreativeMode;
            objectBuilder.Battery = new MyObjectBuilder_Battery();
            objectBuilder.Battery.CurrentCapacity = 1;
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
                character.EnableDampeners(false, false);
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

        public void AddHealth(float health)
        {
			m_stats.AddHealth(health);
			m_breath.ForceUpdate();
        }

		public void AddStamina(float stamina)
		{
			m_stats.AddStamina(stamina);
		}

		public void AddFood(float food)
		{
			m_stats.AddFood(food);
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
            MyHud.CharacterInfo.BatteryEnergy = 100 * SuitBattery.RemainingCapacity / MyEnergyConstants.BATTERY_MAX_CAPACITY;
            MyHud.CharacterInfo.IsBatteryEnergyLow = SuitBattery.IsEnergyLow;
            MyHud.CharacterInfo.Speed = Physics.LinearVelocity.Length();
            MyHud.CharacterInfo.Mass = (int)((float)GetInventory().CurrentMass + Definition.Mass);
            MyHud.CharacterInfo.LightEnabled = LightEnabled;
            MyHud.CharacterInfo.DampenersEnabled = DampenersEnabled;
            MyHud.CharacterInfo.JetpackEnabled = JetpackEnabled;
            MyHud.CharacterInfo.BroadcastEnabled = m_radioBroadcaster.Enabled;

            if (CanFly())
                MyHud.CharacterInfo.State = MyHudCharacterStateEnum.Flying;
            else if (IsCrouching)
                MyHud.CharacterInfo.State = MyHudCharacterStateEnum.Crouching;
            else
                if (IsFalling)
                    MyHud.CharacterInfo.State = MyHudCharacterStateEnum.Falling;
                else
                    MyHud.CharacterInfo.State = MyHudCharacterStateEnum.Standing;

            MyHud.CharacterInfo.HealthRatio = HealthRatio;
            MyHud.CharacterInfo.IsHealthLow = HealthRatio < LOW_HEALTH_RATIO;
            MyHud.CharacterInfo.InventoryVolume = GetInventory().CurrentVolume;
            MyHud.CharacterInfo.IsInventoryFull = ((float)GetInventory().CurrentVolume / (float)GetInventory().MaxVolume) > 0.95f;
            MyHud.CharacterInfo.BroadcastRange = RadioBroadcaster.BroadcastRadius;
            MyHud.CharacterInfo.OxygenLevel = SuitOxygenLevel;
            MyHud.CharacterInfo.IsOxygenLevelLow = MyHud.CharacterInfo.OxygenLevel < LOW_OXYGEN_RATIO;
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

                    float widthScale = 1;

                    this.InitCharacterPhysics(MyMaterialType.CHARACTER, PositionComp.LocalVolume.Center, CharacterWidth * Definition.CharacterCollisionScale, CharacterHeight - CharacterWidth * Definition.CharacterCollisionScale  - offset,
                    CrouchHeight - CharacterWidth,
                    CharacterWidth - offset,
                    Definition.CharacterHeadSize * Definition.CharacterCollisionScale,
                    Definition.CharacterHeadHeight,
                    0.7f, 0.7f, (ushort)MyPhysics.CharacterCollisionLayer, RigidBodyFlag.RBF_DEFAULT, Definition.Mass,
                    Definition.VerticalPositionFlyingOnly,
                    Definition.MaxSlope, false);

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

                    this.InitCharacterPhysics(MyMaterialType.CHARACTER, PositionComp.LocalVolume.Center, CharacterWidth * Definition.CharacterCollisionScale * scale, CharacterHeight - CharacterWidth * Definition.CharacterCollisionScale * scale - offset,
                    CrouchHeight - CharacterWidth,
                    CharacterWidth - offset,
                    Definition.CharacterHeadSize * Definition.CharacterCollisionScale * scale,
                    Definition.CharacterHeadHeight,
                    0.7f, 0.7f, (ushort)layer, MyPerGameSettings.NetworkCharacterType, 0, //Mass is not scaled on purpose (collision over networks)
                    Definition.VerticalPositionFlyingOnly,
                    Definition.MaxSlope, true);

                    if (MyPerGameSettings.NetworkCharacterType == RigidBodyFlag.RBF_DEFAULT)
                    {
                        Physics.Friction = 1; //to not move on steep surfaces
                    }

                    Physics.Enabled = true;
                }
            }

            if (MyPerGameSettings.EnableRagdollModels)
            {                
                InitRagdoll();
                if ((Definition.RagdollBonesMappings.Count > 1) && Physics.Ragdoll != null)
                {
                    InitRagdollMapper();
                }
            }

            if (MyFakes.ENABLE_CHARACTER_VIRTUAL_PHYSICS && VirtualPhysics != null && !VirtualPhysics.Enabled)
            {
                VirtualPhysics.Enabled = true;
                VirtualPhysics.Activate();
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
                result.ConstantMovementUpdateCount = 0;
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

        void FlagsChangeSuccess(bool enableJetpack, bool enableDampeners, bool enableLights, bool enableIronsight, bool enableBroadcast)
        {
            if (!IsDead)
            {
                if (enableJetpack != JetpackEnabled)
                {
                    EnableJetpack(enableJetpack, false, false);
                }

                if (enableDampeners != DampenersEnabled)
                {
                    EnableDampeners(enableDampeners, false);
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
            }
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

        void DoDamageSuccess(float damage, MyDamageType damageType)
        {
            DoDamage(damage, damageType, false);
        }
        #endregion

        public void StartSecondarySound(string cueName, bool sync = false)
        {
            var cueId = MySoundPair.GetCueId(cueName);
            StartSecondarySound(cueId, sync);
        }

        public void StartSecondarySound(MyCueId cueId, bool sync = false)
        {
            if (cueId.IsNull) return;

            if (!m_secondarySoundEmitter.IsPlaying)
            {
                m_secondarySoundEmitter.PlaySound(cueId);
            }

            if (sync)
            {
                SyncObject.PlaySecondarySound(cueId);
            }
        }

        void PlaySound()
        {
            m_breath.Update();
            var cueEnum = SelectSound();

            if (cueEnum.Arcade == m_soundEmitter.SoundId || cueEnum.Realistic == m_soundEmitter.SoundId) //not nice
                return;
            if (cueEnum == CharacterSounds[(int)CharacterSoundsEnum.JETPACK_RUN_SOUND])
            {
                if (m_soundEmitter.Loop)
                    m_soundEmitter.StopSound(false);
                m_soundEmitter.PlaySingleSound(cueEnum);
                if (m_soundEmitter.Sound != null) m_soundEmitter.Sound.SetVolume(MathHelper.Clamp(Physics.LinearAcceleration.Length() / 3, 0.6f, 1));
            }
            else if (cueEnum == CharacterSounds[(int)CharacterSoundsEnum.JETPACK_IDLE_SOUND] && m_soundEmitter.SoundId == CharacterSounds[(int)CharacterSoundsEnum.JETPACK_RUN_SOUND].SoundId)
                m_soundEmitter.StopSound(false);
            else if (cueEnum == CharacterSounds[(int)CharacterSoundsEnum.NONE_SOUND] && m_soundEmitter.Loop)
                m_soundEmitter.StopSound(false);
            else
            {
                if (m_soundEmitter.Loop)
                    m_soundEmitter.StopSound(false);
                m_soundEmitter.PlaySound(cueEnum, true);
            }
        }

        public void PlayDamageSound(float oldHealth)
        {
            if (MyFakes.ENABLE_NEW_SOUNDS)
            {
                if (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastScreamTime > SCREAM_DELAY_MS)
                {
                    m_lastScreamTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                    m_secondarySoundEmitter.PlaySound(CharacterSounds[(int)CharacterSoundsEnum.PAIN_SOUND]);
                }
            }
        }

        static float minAmp = 1.12377834f;
        static float maxAmp = 1.21786702f;
        static float medAmp = (minAmp + maxAmp) / 2.0f;
        static float runMedAmp = (1.03966641f + 1.21786702f) / 2.0f;
        private MatrixD m_lastCorrectSpectatorCamera;
        private float m_squeezeDamageTimer;
        

        void UpdateWeaponPosition()
        {
            float IKRatio = m_currentAnimationToIKTime / m_animationToIKDelay;

            MatrixD weaponMatrixPositioned = GetHeadMatrix(false, !CanFly(), false, true);

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
            ironsightMatrix.Translation = WeaponIronsightTranslation;
            if (m_currentWeapon is MyEngineerToolBase)
            {
                ironsightMatrix.Translation = ToolIronsightTranslation;
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

            if (MyFakes.ENABLE_BONES_AND_ANIMATIONS_DEBUG)
            {
                Debug.Assert(Bones.IsValidIndex(m_weaponBone), "Warning! Weapon bone " + Definition.WeaponBone + " on model " + ModelName + " is missing.");
            }

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

            if (m_currentWeapon.BackkickForcePerSecond > 0 && m_currentShotTime > ShotTime - 0.05f)
            {
                weaponFinalLocal.Translation -= weaponFinalLocal.Forward * 0.01f * (float)System.Math.Cos(MySandboxGame.TotalGamePlayTimeInMilliseconds);
            }


            //VRageRender.MyRenderProxy.DebugDrawAxis(weaponMatrixPositioned, 10, false);

            ((MyEntity)m_currentWeapon).WorldMatrix = weaponFinalLocal;

            var headMatrix = GetHeadMatrix(true);
            m_crosshairPoint = headMatrix.Translation + headMatrix.Forward * 2000;
            m_aimedPoint = GetAimedPointFromCamera();
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
                var oldInvetory = this.m_inventory;
                MyObjectBuilder_Character characterOb = (MyObjectBuilder_Character)GetObjectBuilder();

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

                MyEntityIdentifier.AllocationSuspended = true;

                m_currentBlendTime = 0f;
                Init(characterOb);

                m_inventory = oldInvetory;

                if (m_currentWeapon != null)
                {
                    m_currentWeapon.OnControlAcquired(this);
                }

                MyEntities.Add(this);

                MyEntityIdentifier.AllocationSuspended = false;
            }

            Render.ColorMaskHsv = colorMaskHSV;
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
                float scale = 0.75f;
                Matrix m = WorldMatrix;
                m.Forward *= scale;
                m.Up *= CharacterHeight * scale;
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
                // TODO: This should just open the screen of the character and not adding the the aggregate itself..
                var otherAggregate = user.Components.Get<MyComponentInventoryAggregate>();
                var aggregate = Components.Get<MyComponentInventoryAggregate>();
                otherAggregate.AddChild(aggregate);
                var screen = user.ShowAggregateInventoryScreen();
                screen.Closed += delegate(MyGuiScreenBase source) { otherAggregate.RemoveChild(aggregate); };

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

        public float Integrity
        {
            get { return Health; }
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

        MatrixD Sandbox.ModAPI.Interfaces.IMyControllableEntity.GetHeadMatrix(bool includeY, bool includeX, bool forceHeadAnim, bool forceHeadBone = false)
        {
            return GetHeadMatrix(includeY, includeX, forceHeadAnim);
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.MoveAndRotate(Vector3 moveIndicator, Vector2 rotationIndicator, float rollIndicator)
        {
            MoveAndRotate(moveIndicator, rotationIndicator, rollIndicator, m_movementFlags);
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
             SwitchThrusts();
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.SwitchDamping()
        {
            SwitchDamping();
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
            get { return JetpackEnabled; }
        }

        bool Sandbox.ModAPI.Interfaces.IMyControllableEntity.EnabledDamping
        {
            get { return DampenersEnabled; }
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
            get { return !m_needsOxygen; }
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.SwitchHelmet()
        {
            if (IsDead)
            {
                return;
            }

            bool hasHelmetVariation = Definition.HelmetVariation != null;
            if (hasHelmetVariation)
            {
                bool variationExists = false;
                var characters = MyDefinitionManager.Static.Characters;
                foreach (var character in characters)
                {
                    if (character.Name == Definition.HelmetVariation)
                    {
                        variationExists = true;
                        break;
                    }
                }

                if (!variationExists)
                {
                    hasHelmetVariation = false;
                }
            }

            if (hasHelmetVariation)
            {
                ChangeModelAndColor(Definition.HelmetVariation, this.ColorMask);
                m_needsOxygen = !Definition.NeedsOxygen;
                m_helmetToggleNotification.Text = (Definition.NeedsOxygen ? MySpaceTexts.NotificationHelmetOn : MySpaceTexts.NotificationHelmetOff);
            }
            else
            {
                m_helmetToggleNotification.Text = MySpaceTexts.NotificationNoHelmetVariation;
            }

            MyHud.Notifications.Add(m_helmetToggleNotification);
        }

        void Sandbox.ModAPI.Interfaces.IMyControllableEntity.Die()
        {
            Die();
        }

        void IMyDestroyableObject.OnDestroy()
        {
            OnDestroy();
        }

        void IMyDestroyableObject.DoDamage(float damage, MyDamageType damageType, bool sync, MyHitInfo? hitInfo)
        {
            DoDamage(damage, damageType, sync);
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
            public override void OnWorldPositionChanged(object source)
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

            foreach (var sound in CharacterSounds.Values)
            {
                MyEntity3DSoundEmitter.PreloadSound(sound);
            }
        }

        #region Movement properties

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

        internal MyCharacterBreath m_breath { get; set; }

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

        #region ModAPI
        float IMyCharacter.EnvironmentOxygenLevel
        {
            get { return EnvironmentOxygenLevel; }
        }
        #endregion

        public bool SwitchToJetpackRagdoll { get; set; }

        public bool ResetJetpackRagdoll { get; set; }

        //public bool IsUseObjectOfType<T>()
        //{
        //    return UseObject is T;
        //}

        public MyEntity ManipulatedEntity;
        
        bool IMyComponentOwner<MyComponentInventoryAggregate>.GetComponent(out MyComponentInventoryAggregate component)
        {
            component = m_inventoryAggregate;
            return m_inventoryAggregate != null;
        }
    }
}
