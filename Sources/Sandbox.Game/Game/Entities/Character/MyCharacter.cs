#region Using

using Havok;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Audio;
using Sandbox.Game.Components;
using Sandbox.Game.Entities.Character.Components;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.Inventory;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Replication;
using Sandbox.Game.Screens;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Audio;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Definitions.Animation;
using VRage.Game.Entity;
using VRage.Game.Entity.UseObject;
using VRage.Game.Gui;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Interfaces;
using VRage.Game.Models;
using VRage.Game.ObjectBuilders;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.Game.Utils;
using VRage.Input;
using VRage.Library.Collections;
using VRage.Library.Utils;
using VRage.ModAPI;
using VRage.Network;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using VRageRender;
using IMyEntity = VRage.ModAPI.IMyEntity;
using IMyModdingControllableEntity = VRage.Game.ModAPI.Interfaces.IMyControllableEntity;
using VRageRender.Animations;
using VRage.Profiler;
using VRage.Sync;
using VRageRender.Import;

#endregion Using

namespace Sandbox.Game.Entities.Character
{
    internal interface IMyNetworkCommand
    {
        void Apply();

        bool ExecuteBeforeMoveAndRotate { get; }
    }

    internal class MyMoveNetCommand : IMyNetworkCommand
    {
        private MyCharacter m_character;
        private Vector3 m_move;
        private Quaternion m_rotation;

        public MyMoveNetCommand(MyCharacter character, ref Vector3 move, ref Quaternion rotation)
        {
            m_character = character;
            m_move = move;
            m_rotation = rotation;
        }

        public void Apply()
        {
            m_character.ApplyRotation(m_rotation);
            m_character.MoveAndRotate(m_move, Vector2.Zero, 0);
            m_character.MoveAndRotateInternal(m_move, Vector2.Zero, 0, Vector3.Zero);
        }

        public bool ExecuteBeforeMoveAndRotate { get { return false; } }
    }

    internal class MyDeltaNetCommand : IMyNetworkCommand
    {
        private MyCharacter m_character;
        private Vector3D m_delta;

        public MyDeltaNetCommand(MyCharacter character, ref Vector3D delta)
        {
            m_character = character;
            m_delta = delta;
        }

        public void Apply()
        {
            var old = m_character.WorldMatrix;
            old.Translation += m_delta;
            m_character.PositionComp.SetWorldMatrix(old, null, true);
        }

        public bool ExecuteBeforeMoveAndRotate { get { return true; } }
    }

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

    internal enum DamageImpactEnum
    {
        NoDamage,
        SmallDamage,
        MediumDamage,
        CriticalDamage,
        DeadlyDamage,
    }

    enum MyBootsState
    {
        Init,
        Disabled,
        Proximity,
        Enabled
    }

    #endregion Enums

    [MyEntityType(typeof(MyObjectBuilder_Character))]
    [StaticEventOwner]
    public partial class MyCharacter :
        MySkinnedEntity,
        IMyCameraController,
        IMyControllableEntity,
        IMyInventoryOwner,
        IMyUseObject,
        IMyDestroyableObject,
        IMyDecalProxy,
        IMyCharacter,
        IMyEventProxy
    {
        #region Consts

        static List<VertexArealBoneIndexWeight> m_boneIndexWeightTmp;

        [ThreadStatic]
        private static MyCharacterHitInfo m_hitInfoTmp;

        public const float CAMERA_NEAR_DISTANCE = 60.0f;

        internal const float CHARACTER_X_ROTATION_SPEED = 0.13f;
        private const float CHARACTER_Y_ROTATION_FACTOR = 0.02f;

        public const float MINIMAL_SPEED = 0.001f;

        private const float JUMP_DURATION = 0.55f; //s
        private const float JUMP_TIME = 1; //m/ss

        private const float SHOT_TIME = 0.1f;  //s

        private const float FALL_TIME = 0.3f; //s
        private const float RESPAWN_TIME = 5.0f; //s

        internal const float MIN_HEAD_LOCAL_X_ANGLE = -89.9f;
        internal const float MAX_HEAD_LOCAL_X_ANGLE = 89.0f;

        // TODO: This should probably be pulled from the HKCharacterStateType enum instead
        // But right now it is using HK_CHARACTER_USER_STATE_0
        public const int HK_CHARACTER_FLYING = 5;

        // This is the move indicator force multiplier for aerial controls, should be a low value
        private const float AERIAL_CONTROL_FORCE_MULTIPLIER = 0.062f;

        #endregion Consts

        #region Fields

        // Event called at the end of DieInternal method
        // Used for achievement
        public static event Action<MyCharacter> OnCharacterDied;

        private float m_currentShotTime = 0;
        private float m_currentShootPositionTime = 0;
        private float m_cameraDistance = 0.0f;
        private float m_currentSpeed = 0;
        private float m_currentDecceleration = 0;

        private float m_currentJumpTime = 0;
        private float m_frictionBeforeJump = 1.3f;

        private bool m_canJump = true;
        internal bool CanJump { get { return m_canJump; } set { m_canJump = value; } }

        private float m_currentWalkDelay = 0;
        internal float CurrentWalkDelay { get { return m_currentWalkDelay; } set { m_currentWalkDelay = value; } }

        float m_canPlayImpact = 0f;
        static MyStringId m_stringIdHit = MyStringId.GetOrCompute("Hit");
        static MyStringHash m_stringHashCharacter = MyStringHash.GetOrCompute("Character");

        bool m_isDeathPlayer = false;
        Vector3 m_gravity = Vector3.Zero;

        //Weapon
        public static MyHudNotification OutOfAmmoNotification;

        private int m_weaponBone = -1;
        public int WeaponBone { get { return m_weaponBone; } }
        public float CharacterGeneralDamageModifier = 1f;

        public event Action<IMyHandheldGunObject<MyDeviceBase>> WeaponEquiped;

        private IMyHandheldGunObject<MyDeviceBase> m_currentWeapon;
        private bool m_usingByPrimary = false;

        public bool DebugMode = false;

        private float m_headLocalXAngle = 0;
        private float m_headLocalYAngle = 0;
        private float m_previousHeadLocalXAngle = 0;
        private float m_previousHeadLocalYAngle = 0;
        private Vector3D m_headSafeOffset = Vector3D.Zero;
        private bool m_headRenderingEnabled = true;

        MyBootsState m_bootsState = MyBootsState.Init;

        public float RotationSpeed = CHARACTER_X_ROTATION_SPEED;

        public float HeadLocalXAngle
        {
            get { return m_headLocalXAngle.IsValid() ? m_headLocalXAngle : 0.0f; }
            set
            {
                m_previousHeadLocalXAngle = m_headLocalXAngle;
                m_headLocalXAngle = value.IsValid() ? MathHelper.Clamp(value, MIN_HEAD_LOCAL_X_ANGLE, MAX_HEAD_LOCAL_X_ANGLE) : 0.0f;
            }
        }

        public float HeadLocalYAngle
        {
            get { return m_headLocalYAngle; }
            set
            {
                m_previousHeadLocalYAngle = m_headLocalYAngle;
                m_headLocalYAngle = value;
            }
        }

        public bool HeadMoved
        {
            get { return m_previousHeadLocalXAngle != m_headLocalXAngle || m_previousHeadLocalYAngle != m_headLocalYAngle; }
        }

        private int m_headBoneIndex = -1;
        private int m_camera3rdBoneIndex = -1;
        private int m_leftHandIKStartBone = -1;
        private int m_leftHandIKEndBone = -1;
        private int m_rightHandIKStartBone = -1;
        private int m_rightHandIKEndBone = -1;
        private int m_leftUpperarmBone = -1;
        private int m_leftForearmBone = -1;
        private int m_rightUpperarmBone = -1;
        private int m_rightForearmBone = -1;
        private int m_leftHandItemBone = -1;
        private int m_rightHandItemBone = -1;
        private int m_spineBone = -1;

        protected bool m_characterBoneCapsulesReady = false;

        private bool m_animationCommandsEnabled = true;
        private float m_currentAnimationChangeDelay = 0;
        private float SAFE_DELAY_FOR_ANIMATION_BLEND = 0.1f;

        private MyCharacterMovementEnum m_currentMovementState = MyCharacterMovementEnum.Standing;
        private MyCharacterMovementEnum m_previousMovementState = MyCharacterMovementEnum.Standing;
        private MyCharacterMovementEnum m_previousNetworkMovementState = MyCharacterMovementEnum.Standing;

        public event CharacterMovementStateDelegate OnMovementStateChanged;

        private MyEntity m_leftHandItem;
        private MyHandItemDefinition m_handItemDefinition;
        public MyHandItemDefinition HandItemDefinition { get { return m_handItemDefinition; } }
        private MyZoomModeEnum m_zoomMode = MyZoomModeEnum.Classic;
        public MyZoomModeEnum ZoomMode { get { return m_zoomMode; } }

        private float m_currentHandItemWalkingBlend = 0;
        private float m_currentHandItemShootBlend = 0;
        private float m_currentScatterBlend = 0;
        private Vector3 m_currentScatterPos;
        private Vector3 m_lastScatterPos;

        /// <summary>
        /// This is now generated dynamically as some character's don't have the same skeleton as human characters.
        /// m_bodyCapsules[0] will always be head capsule
        /// If the model has ragdoll model, the capsules are generated from the ragdoll
        /// If the model is missing the ragdoll, the capsules are generated with dynamically determined parameters, which may not always be correct
        /// </summary>
        private CapsuleD[] m_bodyCapsules = new CapsuleD[1];

        private MatrixD m_headMatrix = MatrixD.CreateTranslation(0, 1.65, 0);

        private MyHudNotification m_pickupObjectNotification;
        private MyHudNotification m_broadcastingNotification;

        private HkCharacterStateType m_currentCharacterState;
        private bool m_isFalling = false;
        private bool m_isFallingAnimationPlayed = false;
        private float m_currentFallingTime = 0;
        private bool m_crouchAfterFall = false;

        private MyCharacterMovementFlags m_movementFlags;
        private MyCharacterMovementFlags m_previousMovementFlags;
        private bool m_movementsFlagsChanged;

        private string m_characterModel;

        private MyBattery m_suitBattery;
        private MyResourceDistributorComponent m_suitResourceDistributor;

        public bool JetpackRunning
        {
            get { return JetpackComp != null && JetpackComp.Running; }
        }

        internal MyResourceDistributorComponent SuitRechargeDistributor
        {
            get { return m_suitResourceDistributor; }
            set
            {
                if (Components.Contains(typeof(MyResourceDistributorComponent)))
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

        private MyEntity m_topGrid;
        private MyEntity m_usingEntity;

        private bool m_enableBag = true;

        public readonly SyncType SyncType;

        //Light
        public const float REFLECTOR_RANGE = 50;


        public const float REFLECTOR_CONE_ANGLE = 0.373f;
        public const float REFLECTOR_BILLBOARD_LENGTH = 40f;
        public const float REFLECTOR_BILLBOARD_THICKNESS = 6f;
        public const float REFLECTOR_GLOSS_FACTOR = 1.0f;
        public const float REFLECTOR_DIFFUSE_FACTOR = 3.14f;

        public static Vector4 REFLECTOR_COLOR = Vector4.One;
        public const float REFLECTOR_INTENSITY = 6;
        public static Vector4 POINT_COLOR = Vector4.One;
        public static Vector4 POINT_COLOR_SPECULAR = Vector4.One;
        public const float POINT_LIGHT_RANGE = 1.231f;
        public const float POINT_LIGHT_INTENSITY = 0.3864f;
        public const float REFLECTOR_DIRECTION = -3.5f;

        public const float LIGHT_GLARE_MAX_DISTANCE = 40;

        private float m_currentLightPower = 0; //0..1
        public float CurrentLightPower { get { return m_currentLightPower; } }
        private float m_lightPowerFromProducer = 0;
        private float m_lightTurningOnSpeed = 0.05f;
        private float m_lightTurningOffSpeed = 0.05f;
        private bool m_lightEnabled = true;

        //Needed to check relation between character and remote players when controlling a remote control
        private MyEntityController m_oldController;

        private float m_currentHeadAnimationCounter = 0;

        private float m_currentLocalHeadAnimation = -1;
        private float m_localHeadAnimationLength = -1;
        private Vector2? m_localHeadAnimationX = null;
        private Vector2? m_localHeadAnimationY = null;

        // Which bones should define the body capsules and how large the capsules should be
        private List<MyBoneCapsuleInfo> m_bodyCapsuleInfo = new List<MyBoneCapsuleInfo>();

        private float m_currentCameraShakePower = 0;

        private HashSet<uint> m_shapeContactPoints = new HashSet<uint>();

        private float m_currentRespawnCounter = 0;
        public float CurrentRespawnCounter { get { return m_currentRespawnCounter; } }
        private MyHudNotification m_respawnNotification;

        private MyHudNotification m_notEnoughStatNotification;

        private MyStringHash manipulationToolId = MyStringHash.GetOrCompute("ManipulationTool");

        private MyCameraControllerSettings m_storedCameraSettings;
        private Queue<Vector3> m_bobQueue = new Queue<Vector3>();

        private bool m_dieAfterSimulation;

        internal MyRadioReceiver RadioReceiver
        {
            get { return (MyRadioReceiver)Components.Get<MyDataReceiver>(); }
            private set { Components.Add<MyDataReceiver>(value); }
        }

        internal MyRadioBroadcaster RadioBroadcaster
        {
            get { return (MyRadioBroadcaster)Components.Get<MyDataBroadcaster>(); }
            private set { Components.Add<MyDataBroadcaster>(value); }
        }

        private float m_currentLootingCounter = 0;
        private MyEntityCameraSettings m_cameraSettingsWhenAlive;

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

        public MyAtmosphereDetectorComponent AtmosphereDetectorComp
        {
            get { return Components.Get<MyAtmosphereDetectorComponent>(); }
            set { if (Components.Has<MyAtmosphereDetectorComponent>()) Components.Remove<MyAtmosphereDetectorComponent>(); Components.Add<MyAtmosphereDetectorComponent>(value); }
        }

        public MyEntityReverbDetectorComponent ReverbDetectorComp
        {
            get { return Components.Get<MyEntityReverbDetectorComponent>(); }
            set { if (Components.Has<MyEntityReverbDetectorComponent>()) Components.Remove<MyEntityReverbDetectorComponent>(); Components.Add<MyEntityReverbDetectorComponent>(value); }
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
                if (this.GetInventory() != null)
                {
                    return BaseMass + (float)this.GetInventory().CurrentMass + carriedMass;
                }
                return BaseMass + carriedMass;
            }
        }

        private bool m_useAnimationForWeapon = true;

        private static bool? m_localCharacterWasInThirdPerson = null;

        private MyCharacterDefinition m_characterDefinition;

        public MyCharacterDefinition Definition
        {
            get { return m_characterDefinition; }
        }

        //Backwards compatibility for MyThirdPersonSpectator
        //Default needs to be true
        private bool m_isInFirstPersonView = true;

        public bool IsInFirstPersonView
        {
            //users connected from different client aren't in first person for local player
            //by Gregory: removed ForceFirstPersonCamera check it is consider a bug by the users
            get { return m_isInFirstPersonView; }
            set
            {
                m_isInFirstPersonView = value;
                ResetHeadRotation();
            }
        }

        private bool m_targetFromCamera = false;

        public bool TargetFromCamera
        {
            get
            {
                if (MySession.Static.ControlledEntity == this)
                    return MySession.Static.GetCameraControllerEnum() == MyCameraControllerEnum.ThirdPersonSpectator;

                if (MySandboxGame.IsDedicated)
                    return false;

                return m_targetFromCamera;
            }
            set
            {
                m_targetFromCamera = value;
            }
        }

        public MyToolbar Toolbar
        {
            get
            {
                return MyToolbarComponent.CharacterToolbar;
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

        public MyCharacterWeaponPositionComponent WeaponPosition
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

        public bool IsRotating
        {
            get;
            set;
        }

        public float RollIndicator
        {
            get;
            set;
        }

        public Vector3 RotationCenterIndicator
        {
            get;
            set;
        }

        private bool m_moveAndRotateStopped;
        private bool m_moveAndRotateCalled;

        private readonly Sync<int> m_currentAmmoCount;
        private readonly Sync<int> m_currentMagazineAmmoCount;

        private readonly Sync<Sandbox.Game.World.MyPlayer.PlayerId> m_controlInfo;
        private Sandbox.Game.World.MyPlayer.PlayerId? m_savedPlayer;

        private readonly Sync<Vector3> m_localHeadPosition;  // only for character rotation

        private float m_animLeaning;
        private float m_animTurningSpeed;

        private List<IMyNetworkCommand> m_cachedCommands;

        public MyPromoteLevel PromoteLevel
        {
            get
            {
                MyPlayer.PlayerId playerId = m_controlInfo.Value;
                return MySession.Static.GetUserPromoteLevel(playerId.SteamId);
            }

            set
            {
                MyPlayer.PlayerId playerId = m_controlInfo.Value;
                MySession.Static.SetUserPromoteLevel(playerId.SteamId, value);
            }
        }

        private Vector3 m_previousLinearVelocity;

        private bool[] m_isShooting;

        public bool IsShooting(MyShootActionEnum action)
        {
            return m_isShooting[(int)action];
        }

        public MyShootActionEnum? GetShootingAction()
        {
            foreach (MyShootActionEnum value in MyEnum<MyShootActionEnum>.Values)
            {
                if (m_isShooting[(int)value])
                    return value;
            }
            return null;
        }

        public Vector3 ShootDirection = Vector3.One;

        private long m_lastShootDirectionUpdate;

        private static HashSet<long> m_tempValidIds = new HashSet<long>();

        #endregion Fields

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

        private float? m_savedHealth;

        public static MyObjectBuilder_Character Random()
        {
            return new MyObjectBuilder_Character()
            {
                CharacterModel = DefaultModel,
                // We presume here that the subtype is the same as model for the default character
                SubtypeName = DefaultModel,
                ColorMaskHSV = m_defaultColors[MyUtils.GetRandomInt(0, m_defaultColors.Length)]
            };
        }

        public MyCharacter()
        {
            ControllerInfo.ControlAcquired += OnControlAcquired;
            ControllerInfo.ControlReleased += OnControlReleased;

            RadioReceiver = new MyRadioReceiver();
            Components.Add<MyDataBroadcaster>(new MyRadioBroadcaster());
            RadioBroadcaster.BroadcastRadius = 200;
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
#if !XB1 // !XB1_SYNC_NOREFLECTION
            SyncType = SyncHelpers.Compose(this);
#else // XB1
            SyncType = new SyncType(new List<SyncBase>());
            m_currentAmmoCount = SyncType.CreateAndAddProp<int>();
            m_currentMagazineAmmoCount = SyncType.CreateAndAddProp<int>();
            m_controlInfo = SyncType.CreateAndAddProp<Sandbox.Game.World.MyPlayer.PlayerId>();
            m_localHeadPosition = SyncType.CreateAndAddProp<Vector3>();
            //m_animLeaning = SyncType.CreateAndAddProp<float>();
            //m_animTurningSpeed = SyncType.CreateAndAddProp<float>();
            //m_localHeadTransform = SyncType.CreateAndAddProp<MyTransform>();
            //m_localHeadTransformTool = SyncType.CreateAndAddProp<MyTransform>();
            m_isPromoted = SyncType.CreateAndAddProp<bool>();
            //m_weaponPosition = SyncType.CreateAndAddProp<Vector3>();
#endif // XB1

            AddDebugRenderComponent(new MyDebugRenderComponentCharacter(this));

            if (MyPerGameSettings.CharacterDetectionComponent != null)
                Components.Add<MyCharacterDetectorComponent>((MyCharacterDetectorComponent)Activator.CreateInstance(MyPerGameSettings.CharacterDetectionComponent));
            else
                Components.Add<MyCharacterDetectorComponent>(new MyCharacterRaycastDetectorComponent());

            m_currentAmmoCount.ValidateNever();
            m_currentMagazineAmmoCount.ValidateNever();

            m_controlInfo.ValueChanged += (x) => ControlChanged();
            m_controlInfo.ValidateNever();

            m_isShooting = new bool[(int)MyEnum<MyShootActionEnum>.Range.Max + 1];

            //m_weaponPosition.Value = Vector3D.Zero;

            //m_localHeadTransformTool.ValueChanged += (x) => ToolHeadTransformChanged();
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
                SerializableVector3 newColorMask = MyObjectBuilder_Character.CharacterModels[asset];
                if (newColorMask.X > -1.0f || newColorMask.Y > -1.0f || newColorMask.Z > -1.0f)
                    colorMask = newColorMask;
                asset = DefaultModel;
            }
            return asset;
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            SyncFlag = true;

            /// Wee need to get the character subtype, before passing init to base classes so the components can be properly initialized..
            MyObjectBuilder_Character characterOb = (MyObjectBuilder_Character)objectBuilder;

            Render.ColorMaskHsv = characterOb.ColorMaskHSV;

            Vector3 colorMask = Render.ColorMaskHsv;

            /// This will retrieve definition and set the subtype for the character
            GetModelAndDefinition(characterOb, out m_characterModel, out m_characterDefinition, ref colorMask);

            base.UseNewAnimationSystem = m_characterDefinition.UseNewAnimationSystem;
            if (UseNewAnimationSystem)
            {
                //// Create default layer.
                //AnimationController.Controller.DeleteAllLayers();
                //var animationLayer = AnimationController.Controller.CreateLayer("Body");
                //// Build an animation node for each animation subtype.
                //// VRAGE TODO: this is just temporary for testing the new animation system
                //foreach (var animationNameSubType in m_characterDefinition.AnimationNameToSubtypeName)
                //{
                //    string animSubType = animationNameSubType.Value;
                //    MyAnimationDefinition animationDefinition = null;
                //    if (animationLayer.FindNode(animSubType) == null && TryGetAnimationDefinition(animSubType, out animationDefinition))
                //    {
                //        MyModel modelAnimation = VRage.Game.Models.MyModels.GetModelOnlyAnimationData(animationDefinition.AnimationModel);
                //        if (modelAnimation != null && animationDefinition.ClipIndex < modelAnimation.Animations.Clips.Count)
                //        {
                //            VRage.Animations.MyAnimationClip clip = modelAnimation.Animations.Clips[animationDefinition.ClipIndex];
                //            var animationState = new VRage.Animations.MyAnimationStateMachineNode(animSubType, clip);
                //            animationLayer.AddNode(animationState);
                //        }
                //    }
                //}
                AnimationController.Clear();
                MyStringHash animSubtypeNameHash = MyStringHash.GetOrCompute(m_characterDefinition.AnimationController);
                MyAnimationControllerDefinition animControllerDef =
                    MyDefinitionManager.Static.GetDefinition<MyAnimationControllerDefinition>(animSubtypeNameHash);
                if (animControllerDef != null)
                {
                    AnimationController.InitFromDefinition(animControllerDef);
                }
            }

            if (Render.ColorMaskHsv != colorMask)
                // color mask is set by definition of model
                Render.ColorMaskHsv = colorMask;

            /// Set the subtype from the definition
            characterOb.SubtypeName = m_characterDefinition.Id.SubtypeName;

            base.Init(objectBuilder);

            m_currentAnimationChangeDelay = 0;

            SoundComp = new MyCharacterSoundComponent();

            RadioBroadcaster.WantsToBeEnabled = characterOb.EnableBroadcasting && Definition.VisibleOnHud;

            Init(new StringBuilder(characterOb.DisplayName), m_characterDefinition.Model, null, null);

            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;

            PositionComp.LocalAABB = new BoundingBox(-new Vector3(0.3f, 0.0f, 0.3f), new Vector3(0.3f, 1.8f, 0.3f));

            m_currentLootingCounter = characterOb.LootingCounter;

            if (m_currentLootingCounter <= 0)
                UpdateCharacterPhysics(!characterOb.AIMode);

            m_currentMovementState = characterOb.MovementState;
            if (Physics != null && Physics.CharacterProxy != null)
            {
                switch (m_currentMovementState)
                {
                    case MyCharacterMovementEnum.Falling:
                    case MyCharacterMovementEnum.Flying:
                        Physics.CharacterProxy.SetState(HkCharacterStateType.HK_CHARACTER_IN_AIR);
                        break;

                    case MyCharacterMovementEnum.Jump:
                        Physics.CharacterProxy.SetState(HkCharacterStateType.HK_CHARACTER_JUMPING);
                        break;

                    case MyCharacterMovementEnum.Ladder:
                    case MyCharacterMovementEnum.LadderDown:
                    case MyCharacterMovementEnum.LadderUp:
                        Physics.CharacterProxy.SetState(HkCharacterStateType.HK_CHARACTER_CLIMBING);
                        break;

                    default:
                        Physics.CharacterProxy.SetState(HkCharacterStateType.HK_CHARACTER_ON_GROUND);
                        break;
                }
            }

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

            m_lightEnabled = characterOb.LightEnabled;

            Physics.LinearVelocity = characterOb.LinearVelocity;

            if (Physics.CharacterProxy != null)
            {
                Physics.CharacterProxy.ContactPointCallbackEnabled = true;
                Physics.CharacterProxy.ContactPointCallback += RigidBody_ContactPointCallback;
            }

            Render.UpdateLightProperties(m_currentLightPower);

            // Setup first person view for local player from previous state before die.
            IsInFirstPersonView = MySession.Static.Settings.Enable3rdPersonView == false
                || (m_localCharacterWasInThirdPerson != null
                ? characterOb.IsInFirstPersonView && !m_localCharacterWasInThirdPerson.Value : characterOb.IsInFirstPersonView);

            m_breath = new MyCharacterBreath(this);

            Debug.Assert(m_currentLootingCounter <= 0 || m_currentLootingCounter > 0);

            m_broadcastingNotification = new MyHudNotification();

            m_notEnoughStatNotification = new MyHudNotification(MyCommonTexts.NotificationStatNotEnough, disappearTimeMs: 1000, font: MyFontEnum.Red, level: MyNotificationLevel.Important);

            if (InventoryAggregate != null) InventoryAggregate.Init();

            UseDamageSystem = true;

            if (characterOb.EnabledComponents == null)
            {
                characterOb.EnabledComponents = new List<string>();
            }

            foreach (var componentName in m_characterDefinition.EnabledComponents)
            {
                if (characterOb.EnabledComponents.All(x => x != componentName))
                    characterOb.EnabledComponents.Add(componentName);
            }

            foreach (var componentName in characterOb.EnabledComponents)
            {
                Tuple<Type, Type> componentType;
                if (MyCharacterComponentTypes.CharacterComponents.TryGetValue(MyStringId.GetOrCompute(componentName), out componentType))
                {
                    MyEntityComponentBase component = Activator.CreateInstance(componentType.Item1) as MyEntityComponentBase;
                    Components.Add(componentType.Item2, component);
                }
            }

            if (m_characterDefinition.UsesAtmosphereDetector)
            {
                AtmosphereDetectorComp = new MyAtmosphereDetectorComponent();
                AtmosphereDetectorComp.InitComponent(true, this);
            }

            if (m_characterDefinition.UsesReverbDetector)
            {
                ReverbDetectorComp = new MyEntityReverbDetectorComponent();
                ReverbDetectorComp.InitComponent(this, true);//only local player will do updates
            }

            bool hasGases = Definition.SuitResourceStorage.Count > 0;
            var sinkData = new List<MyResourceSinkInfo>();
            var sourceData = new List<MyResourceSourceInfo>();

            if (hasGases)
            {
                OxygenComponent = new MyCharacterOxygenComponent();
                Components.Add(OxygenComponent);
                OxygenComponent.Init(characterOb);
                OxygenComponent.AppendSinkData(sinkData);
                OxygenComponent.AppendSourceData(sourceData);
            }

            m_suitBattery = new MyBattery(this);
            m_suitBattery.Init(characterOb.Battery, sinkData, sourceData);

            if (hasGases)
            {
                OxygenComponent.CharacterGasSink = m_suitBattery.ResourceSink;
                OxygenComponent.CharacterGasSource = m_suitBattery.ResourceSource;
            }

            sinkData.Clear();

            sinkData.Add(
                new MyResourceSinkInfo
                {
                    ResourceTypeId = MyResourceDistributorComponent.ElectricityId,
                    MaxRequiredInput = MyEnergyConstants.REQUIRED_INPUT_LIFE_SUPPORT + MyEnergyConstants.REQUIRED_INPUT_CHARACTER_LIGHT,
                    RequiredInputFunc = ComputeRequiredPower
                });

            if (hasGases)
            {
                sinkData.Add(new MyResourceSinkInfo
                {
                    ResourceTypeId = MyCharacterOxygenComponent.OxygenId,
                    MaxRequiredInput = (OxygenComponent.OxygenCapacity + (!OxygenComponent.NeedsOxygenFromSuit ? Definition.OxygenConsumption : 0f)) * Definition.OxygenConsumptionMultiplier * VRage.Game.MyEngineConstants.UPDATE_STEPS_PER_SECOND / 100f,
                    RequiredInputFunc = () => (OxygenComponent.HelmetEnabled ? Definition.OxygenConsumption : 0f) * Definition.OxygenConsumptionMultiplier * VRage.Game.MyEngineConstants.UPDATE_STEPS_PER_SECOND / 100f
                });
            }

            SinkComp.Init(
                MyStringHash.GetOrCompute("Utility"),
                sinkData);
            SinkComp.CurrentInputChanged += delegate
            {
                SetPowerInput(SinkComp.CurrentInputByType(MyResourceDistributorComponent.ElectricityId));
            };
            SinkComp.TemporaryConnectedEntity = this;

            SuitRechargeDistributor = new MyResourceDistributorComponent(ToString());
            SuitRechargeDistributor.AddSource(m_suitBattery.ResourceSource);
            SuitRechargeDistributor.AddSink(SinkComp);
            SinkComp.Update();

            bool isJetpackAvailable = m_characterDefinition.Jetpack != null;

            if (isJetpackAvailable)
            {
                JetpackComp = new MyCharacterJetpackComponent();
                JetpackComp.Init(characterOb);
            }

            WeaponPosition = new MyCharacterWeaponPositionComponent();
            Components.Add(WeaponPosition);
            WeaponPosition.Init(characterOb);

            InitWeapon(characterOb.HandWeapon);

            if (Definition.RagdollBonesMappings.Count > 0)
                CreateBodyCapsulesForHits(Definition.RagdollBonesMappings);
            else
                m_bodyCapsuleInfo.Clear();

            PlayCharacterAnimation(Definition.InitialAnimation, MyBlendOption.Immediate, MyFrameOption.JustFirstFrame, 0.0f);

            m_savedHealth = characterOb.Health;

            m_savedPlayer = new Sandbox.Game.World.MyPlayer.PlayerId(characterOb.PlayerSteamId, characterOb.PlayerSerialId);

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME; // TODO: Get rid of after after the character will be initialized properly from objectBuilder

            m_previousLinearVelocity = characterOb.LinearVelocity;

            if (ControllerInfo.IsLocallyControlled())
            {
                //m_localHeadTransform = new MyTransform(Vector3.Zero);
                //m_localHeadTransformTool = new MyTransform(Vector3.Zero);
            }

            CheckExistingStatComponent();
            CharacterGeneralDamageModifier = characterOb.CharacterGeneralDamageModifier;
        }

        /// <summary>
        /// Log invalid stat component, can happen for old character mod which use old stat definition (must be rewritten to component definition),
        /// character entity container has to be defined in EntityContainers.sbc and has to contain stat component (see Default_astronaut definition).  
        /// </summary>
        private void CheckExistingStatComponent()
        {
            if (StatComp == null)
            {
                bool hasStatComponentDefinition = false;
                MyContainerDefinition containerDefinition = null;
                MyComponentContainerExtension.TryGetContainerDefinition(m_characterDefinition.Id.TypeId, m_characterDefinition.Id.SubtypeId, out containerDefinition);
                if (containerDefinition != null)
                {
                    foreach (var componentId in containerDefinition.DefaultComponents)
                    {
                        if (componentId.BuilderType == typeof(MyObjectBuilder_CharacterStatComponent))
                        {
                            hasStatComponentDefinition = true;
                            break;
                        }
                    }
                }

                string msg = "Stat component has not been created for character: " + m_characterDefinition.Id + ", container defined: " + (containerDefinition != null)
                    + ", stat component defined: " + hasStatComponentDefinition;
                Debug.Fail(msg);
                MyLog.Default.WriteLine(msg);
            }
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
            bool inventoryAlreadyExists = this.GetInventory() != null;

            if (!inventoryAlreadyExists)
            {
                if (m_characterDefinition.InventoryDefinition == null)
                {
                    m_characterDefinition.InventoryDefinition = new MyObjectBuilder_InventoryDefinition();
                }

                MyInventory currentInventory = new MyInventory(m_characterDefinition.InventoryDefinition, 0);

                // CH: This is very ugly, but it is caused by the fact that we don't have proper inventory definitions
                // and I didn't want to add max item count to inventory constructors
                currentInventory.Init(null);

                if (InventoryAggregate != null)
                {
                    InventoryAggregate.AddComponent(currentInventory);
                }
                else
                {
                    Components.Add<MyInventoryBase>(currentInventory);
                }
                currentInventory.Init(characterOb.Inventory);

                Debug.Assert(currentInventory.Owner == this, "Inventory ownership was not set!");

                // Creates the aggregate inventory
                MyCubeBuilder.BuildComponent.AfterCharacterCreate(this);
                if (MyFakes.ENABLE_MEDIEVAL_INVENTORY && InventoryAggregate != null)
                {
                    var internalAggregate = InventoryAggregate.GetInventory(MyStringHash.GetOrCompute("Internal")) as MyInventoryAggregate;
                    if (internalAggregate != null)
                    {
                        internalAggregate.AddComponent(currentInventory);
                    }
                    else
                    {
                        InventoryAggregate.AddComponent(currentInventory);
                    }
                }
            }
            else if (MyPerGameSettings.ConstrainInventory())
            {
                MyInventory inventory = this.GetInventory();
                inventory.FixInventoryVolume(m_characterDefinition.InventoryDefinition.InventoryVolume);
            }

            //GR: This is a good spot for registering multiple events and calling them a lot of times... So unregister first and then register
            this.GetInventory().ContentsChanged -= inventory_OnContentsChanged;
            this.GetInventory().BeforeContentsChanged -= inventory_OnBeforeContentsChanged;
            this.GetInventory().BeforeRemovedFromContainer -= inventory_OnRemovedFromContainer;
            this.GetInventory().ContentsChanged += inventory_OnContentsChanged;
            this.GetInventory().BeforeContentsChanged += inventory_OnBeforeContentsChanged;
            this.GetInventory().BeforeRemovedFromContainer += inventory_OnRemovedFromContainer;
        }

        private void CreateBodyCapsulesForHits(Dictionary<string, MyCharacterDefinition.RagdollBoneSet> bonesMappings)
        {
            m_bodyCapsuleInfo.Clear();
            m_bodyCapsules = new CapsuleD[bonesMappings.Count];
            foreach (var boneSet in bonesMappings)
            {
                try
                {
                    String[] boneNames = boneSet.Value.Bones;
                    int ascendantBone;
                    int descendantBone;
                    Debug.Assert(boneNames.Length >= 2, "In ragdoll model definition of bonesets is only one bone, can not create body capsule properly! Model:" + ModelName + " BoneSet:" + boneSet.Key);
                    var bone1 = AnimationController.FindBone(boneNames.First(), out ascendantBone);
                    var bone2 = AnimationController.FindBone(boneNames.Last(), out descendantBone);
                    if (bone1.Depth > bone2.Depth)
                    {
                        var temp = ascendantBone;
                        ascendantBone = descendantBone;
                        descendantBone = temp;
                    }

                    m_bodyCapsuleInfo.Add(new MyBoneCapsuleInfo() { Bone1 = bone1.Index, Bone2 = bone2.Index, AscendantBone = ascendantBone, DescendantBone = descendantBone, Radius = boneSet.Value.CollisionRadius });
                }
                catch (Exception e)
                {
                    Debug.Fail(e.Message);
                }
            }
            // locating the head bone and moving as the first in the list
            for (int i = 0; i < m_bodyCapsuleInfo.Count; ++i)
            {
                var capsuleInfo = m_bodyCapsuleInfo[i];
                if (capsuleInfo.Bone1 == m_headBoneIndex)
                {
                    m_bodyCapsuleInfo.Move(i, 0);
                    break;
                }
            }
        }

        private void Toolbar_ItemChanged(MyToolbar toolbar, MyToolbar.IndexArgs index)
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
                            MyToolBarCollection.RequestChangeSlotItem(MySession.Static.LocalHumanPlayer.Id, index.ItemIndex, defId);
                        else
                            MyToolBarCollection.RequestChangeSlotItem(MySession.Static.LocalHumanPlayer.Id, index.ItemIndex, item.GetObjectBuilder());
                    }
                }
            }
            else if (MySandboxGame.IsGameReady)
            {
                MyToolBarCollection.RequestClearSlot(MySession.Static.LocalHumanPlayer.Id, index.ItemIndex);
            }
        }

        private void inventory_OnRemovedFromContainer(MyEntityComponentBase component)
        {
            Debug.Assert(this.GetInventory().Entity == this, "Inventory is not longer owned by this character !");
            this.GetInventory().BeforeRemovedFromContainer -= inventory_OnRemovedFromContainer;
            this.GetInventory().ContentsChanged -= inventory_OnContentsChanged;
            this.GetInventory().BeforeContentsChanged -= inventory_OnBeforeContentsChanged;
        }

        private void inventory_OnContentsChanged(MyInventoryBase inventory)
        {
            if (this != MySession.Static.LocalCharacter)
            {
                return;
            }
            // Switch away from the weapon if we don't have it; Cube placer is an exception
            if (m_currentWeapon != null && WeaponTakesBuilderFromInventory(m_currentWeapon.DefinitionId)
                && inventory != null && inventory is MyInventory && !(inventory as MyInventory).ContainItems(1, m_currentWeapon.PhysicalObject))
                SwitchToWeapon(null);

            // The same needs to be done with the m_leftHandItems, otherwise HandTorch
            if (LeftHandItem != null && !CanSwitchToWeapon(LeftHandItem.DefinitionId))
            {
                LeftHandItem.OnControlReleased();
                m_leftHandItem.Close();
                m_leftHandItem = null;
            }
        }

        private void inventory_OnBeforeContentsChanged(MyInventoryBase inventory)
        {
            if (this != MySession.Static.LocalCharacter)
            {
                return;
            }

            if (m_currentWeapon != null && WeaponTakesBuilderFromInventory(m_currentWeapon.DefinitionId)
                && inventory != null && inventory is MyInventory && (inventory as MyInventory).ContainItems(1, m_currentWeapon.PhysicalObject))
                SaveAmmoToWeapon();//because it may be dropped few electrons later
        }

        private void RigidBody_ContactPointCallback(ref HkContactPointEvent value)
        {
            if (IsDead || Sync.IsServer == false)
                return;

            if (Physics.CharacterProxy == null)
                return;

            if (!MySession.Static.Ready)
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
                        if (otherChar.IsDead)
                        {
                            if (otherChar.Physics.Ragdoll != null && otherChar.Physics.Ragdoll.GetRootRigidBody().HasProperty(HkCharacterRigidBody.MANIPULATED_OBJECT))
                                return;
                        }
                        else
                        {
                            if (Physics.CharacterProxy.Supported && otherChar.Physics.CharacterProxy.Supported)
                                return;
                        }
                    }

                    if (Math.Abs(value.SeparatingVelocity) < 3)
                    {
                        return;
                    }

                    Vector3 velocity1 = Physics.LinearVelocity;

                    Vector3 difference = velocity1 - m_previousLinearVelocity;

                    float lenght = difference.Length();

                    if (lenght > 10)
                    {
                        //strange angle / magnitude force mismatch
                        return;
                    }

                    Vector3 velocity2 = otherRb.GetVelocityAtPoint(value.ContactPoint.Position);

                    float velocity = velocity1.Length();
                    float speed1 = Math.Max(velocity - (MyFakes.ENABLE_CUSTOM_CHARACTER_IMPACT ? 12.6f : 17.0f), 0);//treshold for falling dmg
                    float speed2 = velocity2.Length() - 2.0f;

                    Vector3 dir1 = speed1 > 0 ? Vector3.Normalize(velocity1) : Vector3.Zero;
                    Vector3 dir2 = speed2 > 0 ? Vector3.Normalize(velocity2) : Vector3.Zero;

                    float dot1withNormal = speed1 > 0 ? Vector3.Dot(dir1, normal) : 0;
                    float dot2withNormal = speed2 > 0 ? -Vector3.Dot(dir2, normal) : 0;

                    speed1 *= dot1withNormal;
                    speed2 *= dot2withNormal;

                    float vel = Math.Min(speed1 + speed2, Math.Abs(value.SeparatingVelocity) - 17.0f);
                    if (vel >= -8f && m_canPlayImpact <= 0f)//impact sound
                    {
                        m_canPlayImpact = 0.3f;
                        HkContactPointEvent hkContactPointEvent = value;
                        Func<bool> canHear = () =>
                        {
                            if (MySession.Static.ControlledEntity != null)
                            {
                                var entity = MySession.Static.ControlledEntity.Entity.GetTopMostParent();
                                return (entity == hkContactPointEvent.GetPhysicsBody(0).Entity || entity == hkContactPointEvent.GetPhysicsBody(1).Entity);
                            }
                            return false;
                        };
                        var bodyB = value.Base.BodyB.GetBody();
                        var worldPos = Physics.ClusterToWorld(value.ContactPoint.Position);
                        var materialB = bodyB.GetMaterialAt(worldPos - value.ContactPoint.Normal * 0.1f);
                        float volume = (Math.Abs(value.SeparatingVelocity) < 15f) ? (0.5f + Math.Abs(value.SeparatingVelocity) / 30f) : 1f;
                        MyAudioComponent.PlayContactSound(Entity.EntityId, m_stringIdHit, worldPos, m_stringHashCharacter, materialB, volume, canHear);
                    }
                    if (vel < 0)
                        return;

                    float mass1 = MyDestructionHelper.MassFromHavok(Physics.Mass * this.m_massChangeForCollisions);
                    float mass2 = MyDestructionHelper.MassFromHavok(otherRb.Mass * other.m_massChangeForCollisions);

                    float impact1 = (speed1 * speed1 * mass1) * 0.5f;
                    float impact2 = (speed2 * speed2 * mass2) * 0.5f;

                    float mass;
                    if (mass1 > mass2 && !otherRb.IsFixedOrKeyframed)
                    {
                        mass = mass2;
                        //impact = impact2;
                    }
                    else
                    {
                        mass = MyDestructionHelper.MassToHavok(70);// Physics.Mass;
                        if (Physics.CharacterProxy.Supported && !otherRb.IsFixedOrKeyframed)
                            mass += Math.Abs(Vector3.Dot(Vector3.Normalize(velocity2), Physics.CharacterProxy.SupportNormal)) * mass2 / 10;
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
                    if (MyFakes.NEW_CHARACTER_DAMAGE)
                        damageImpact = impact;
                    if (damageImpact > 0)
                    {
                        if (Sync.IsServer)
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
            Vector3 direction = Physics.CharacterProxy.Position - collidingBody.Position;
            direction.Normalize();
            var gravity = m_gravity;
            gravity.Normalize();

            float resultToPlayer = Vector3.Dot(direction, gravity);

            if (resultToPlayer < 0.5f) return DamageImpactEnum.NoDamage;

            if (m_squeezeDamageTimer > 0)
            {
                m_squeezeDamageTimer -= VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
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
            if (weapon == null)
                return;
            if ((m_rightHandItemBone == -1 || weapon != null) && m_currentWeapon != null)
            {
                // First, dispose of the old weapon
                DisposeWeapon();
            }
            var physicalItemDefinition = MyDefinitionManager.Static.GetPhysicalItemForHandItem(weapon.GetId());
            Debug.Assert(this.GetInventory() != null);
            bool canEquipWeapon = physicalItemDefinition != null && (!MySession.Static.SurvivalMode || (this.GetInventory().GetItemAmount(physicalItemDefinition.Id) > 0));

            if (m_rightHandItemBone != -1 && canEquipWeapon)
            {
                m_currentWeapon = CreateGun(weapon);
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

            if (this.GetInventory() != null && !MyFakes.ENABLE_MEDIEVAL_INVENTORY)
            {
                objectBuilder.Inventory = this.GetInventory().GetObjectBuilder();
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
            objectBuilder.CharacterGeneralDamageModifier = CharacterGeneralDamageModifier;

            // ds sends IsInFirstPersonView to clients  as false
            objectBuilder.IsInFirstPersonView = !MySandboxGame.IsDedicated ? m_isInFirstPersonView : true;

            objectBuilder.EnableBroadcasting = RadioBroadcaster.WantsToBeEnabled;

            objectBuilder.MovementState = m_currentMovementState;

            if (Components != null)
            {
                if (objectBuilder.EnabledComponents == null)
                {
                    objectBuilder.EnabledComponents = new List<string>();
                }
                foreach (var component in Components)
                {
                    foreach (var definitionEnabledComponent in MyCharacterComponentTypes.CharacterComponents)
                    {
                        if (definitionEnabledComponent.Value.Item2 == component.GetType())
                        {
                            if (!objectBuilder.EnabledComponents.Contains(definitionEnabledComponent.Key.ToString()))
                            {
                                objectBuilder.EnabledComponents.Add(definitionEnabledComponent.Key.ToString());
                            }
                        }
                    }
                }
                if (JetpackComp != null)
                    JetpackComp.GetObjectBuilder(objectBuilder);

                if (OxygenComponent != null)
                    OxygenComponent.GetObjectBuilder(objectBuilder);
            }

            objectBuilder.PlayerSerialId = m_controlInfo.Value.SerialId;
            objectBuilder.PlayerSteamId = m_controlInfo.Value.SteamId;

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

            RadioBroadcaster.Enabled = false;

            if (MyToolbarComponent.CharacterToolbar != null)
                MyToolbarComponent.CharacterToolbar.ItemChanged -= Toolbar_ItemChanged;
        }

        protected override void BeforeDelete()
        {
            Render.CleanLights();
        }

        #endregion Init

        #region Simulation

        private bool m_wasInFirstPerson = false;
        private bool m_isInFirstPerson = false;

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();

            System.Diagnostics.Debug.Assert(MySession.Static != null);

            if (MySession.Static == null)
                return;

            // Persist current movement flags before UpdateAfterSimulation()
            // to safely send the client state at a later stage
            m_previousMovementFlags = m_movementFlags;

            m_previousNetworkMovementState = GetCurrentMovementState();

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update zero movement");
            UpdateZeroMovement();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            if (m_cachedCommands != null)
            {
                if (IsUsing != null)
                {
                    m_cachedCommands.Clear();
                }

                foreach (var command in m_cachedCommands)
                {
                    if (command.ExecuteBeforeMoveAndRotate)
                    {
                        command.Apply();
                    }
                }
            }

            if (ControllerInfo.IsLocallyControlled() || (IsUsing == null && (m_cachedCommands != null && m_cachedCommands.Count == 0)))
            {
                MoveAndRotateInternal(MoveIndicator, RotationIndicator, RollIndicator, RotationCenterIndicator);
            }

            if (m_cachedCommands != null)
            {
                if (IsUsing != null || IsDead)
                {
                    m_cachedCommands.Clear();
                }

                foreach (var command in m_cachedCommands)
                {
                    if (command.ExecuteBeforeMoveAndRotate == false)
                    {
                        command.Apply();
                    }
                }
                m_cachedCommands.Clear();
            }

            m_moveAndRotateCalled = false;
            m_actualUpdateFrame++;

            m_isInFirstPerson = (MySession.Static.CameraController == this) && IsInFirstPersonView;

            if (m_wasInFirstPerson != m_isInFirstPerson && m_currentMovementState != MyCharacterMovementEnum.Sitting)
            {
                MySector.MainCamera.Zoom.ApplyToFov = m_isInFirstPerson;

                if (!ForceFirstPersonCamera)
                {
                    UpdateNearFlag();
                }
            }

            m_wasInFirstPerson = m_isInFirstPerson;

            UpdateLightPower();

            SoundComp.FindAndPlayStateSound();
            SoundComp.UpdateWindSounds();

            var jetpack = JetpackComp;
            if (jetpack != null && (ControllerInfo.IsLocallyControlled() || Sync.IsServer))
                jetpack.UpdateBeforeSimulation();

            if (!IsDead && m_currentMovementState != MyCharacterMovementEnum.Sitting &&
                !MySandboxGame.IsPaused && //this update is called even in pause (jetpack, model update)
                Physics.CharacterProxy != null)
            {
                Vector3 linearVelocity = Physics.LinearVelocity;
                Vector3 angularVelocity = Physics.AngularVelocity;
                if (!JetpackRunning)
                    Physics.CharacterProxy.StepSimulation(VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS);
                else
                {
                    Physics.CharacterProxy.UpdateSupport(VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS);
                    //<ib.jetpack> SE-1247: Call  ing MyPhysicsBody.LinearVelocity += resets to all rigid bodies in ragdoll linear velocity same as character linear velocity
                    //Physics.LinearVelocity += Physics.Gravity * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS; // why to add gravity as linear velocity in jetpack mode ???
                    Physics.CharacterProxy.ApplyGravity(Physics.Gravity);

                }

                if (!Sync.IsServer && Sandbox.Engine.Utils.MyFakes.MULTIPLAYER_CLIENT_PHYSICS && !ControllerInfo.IsLocallyControlled())
                {
                    // revert velocities for not controlled characters (synced over network), step simulation is in place just to update values like ground normal
                    Physics.LinearVelocity = linearVelocity;
                    Physics.AngularVelocity = angularVelocity;
                }
            }

            m_currentAnimationChangeDelay += VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

            if (Sync.IsServer && !IsDead && !MyEntities.IsInsideWorld(PositionComp.GetPosition()))
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

            if (MyInput.Static.IsNewGameControlReleased(Sandbox.Game.MyControlsSpace.LOOKAROUND)
                && MySandboxGame.Config.ReleasingAltResetsCamera)
            {
                // set rotation to 0... animated over 0.3 sec
                SetLocalHeadAnimation(0, 0, 0.3f);
            }

            if (m_canPlayImpact > 0f)
                m_canPlayImpact -= MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

            if (ReverbDetectorComp != null && this == MySession.Static.LocalCharacter)
            {
                //ProfilerShort.BeginNextBlock("ReverbDetection");
                ReverbDetectorComp.Update();
            }
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

            if (RadioBroadcaster.WantsToBeEnabled)
            {
                RadioBroadcaster.Enabled = m_suitBattery.ResourceSource.CurrentOutput > 0;
            }
            if (!RadioBroadcaster.WantsToBeEnabled)
            {
                RadioBroadcaster.Enabled = false;
            }
        }

        public override void UpdateBeforeSimulation10()
        {
            base.UpdateBeforeSimulation10();
            ProfilerShort.Begin("SuitRecharge");
            SuitRechargeDistributor.UpdateBeforeSimulation();
            ProfilerShort.BeginNextBlock("Radio");
            RadioReceiver.UpdateBroadcastersInRange();
            if (this == MySession.Static.ControlledEntity || MySession.Static.ControlledEntity is MyCockpit)
            {
                ProfilerShort.BeginNextBlock("Hud");
                RadioReceiver.UpdateHud();
            }
            ProfilerShort.End();
        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();

            m_suitBattery.UpdateOnServer100();

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

            if (AtmosphereDetectorComp != null)
                AtmosphereDetectorComp.UpdateAtmosphereStatus();
            SoundComp.UpdateBeforeSimulation100();
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

            var oldBootsState = m_bootsState;

            if (IsMagneticBootsEnabled && !IsDead && !IsSitting)
            { //Magnetic :)
                m_bootsState = MyBootsState.Enabled;
            }
            else
                if ((JetpackRunning || IsFalling) && Physics.CharacterProxy != null && Physics.CharacterProxy.Supported && m_gravity.LengthSquared() < 0.001f)
                {
                    m_bootsState = MyBootsState.Proximity;
                }
                else
                {
                    m_bootsState = MyBootsState.Disabled;
                }

            if (oldBootsState != m_bootsState)
            {
                switch (m_bootsState)
                {
                    case MyBootsState.Enabled:
                        { //Magnetic :)
                            VRageRender.MyRenderProxy.UpdateColorEmissivity(Render.RenderObjectIDs[0], 0, "Emissive", Color.ForestGreen, 0.1f);
                        }
                        break;
                    case MyBootsState.Proximity:
                        {
                            VRageRender.MyRenderProxy.UpdateColorEmissivity(Render.RenderObjectIDs[0], 0, "Emissive", Color.Yellow, 0);
                        }
                        break;
                    case MyBootsState.Disabled:
                        {
                            VRageRender.MyRenderProxy.UpdateColorEmissivity(Render.RenderObjectIDs[0], 0, "Emissive", Color.Black, 0);
                        }
                        break;
                }
            }
        }

        private void UpdateCameraDistance()
        {
            m_cameraDistance = (float)Vector3D.Distance(MySector.MainCamera.Position, WorldMatrix.Translation);
        }

        public void DrawHud(IMyCameraController camera, long playerId)
        {
            MyHud.Crosshair.Recenter();

            if (m_currentWeapon != null)
            {
                m_currentWeapon.DrawHud(camera, playerId);
            }
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            if (!IsDead)
            {
                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update Stats");
                if (StatComp != null)
                    StatComp.Update();
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update dying");
            UpdateDying();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            if ((!MySandboxGame.IsDedicated || !MyPerGameSettings.DisableAnimationsOnDS) && !IsDead)
            {
                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update Shake");
                UpdateShake();
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update physical movement");
            UpdatePhysicalMovement();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            if (!IsDead)
            {
                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update Fall And Spine");
                UpdateFallAndSpine();
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
            }

            if (JetpackRunning)
            {
                JetpackComp.ClearMovement();
            }

            if (!MySandboxGame.IsDedicated || !MyPerGameSettings.DisableAnimationsOnDS)
            {
                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update Animation");
                UpdateAnimation(m_cameraDistance);
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update Light Position");
                Render.UpdateLightPosition();
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update BOB Queue");
                UpdateBobQueue();
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
            }
            else
            {
                //fix for grinding on DS 
                if (m_currentWeapon != null && WeaponPosition != null)
                {
                    WeaponPosition.Update();
                }
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update Character State");
            UpdateCharacterStateChange();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update Respawn and Looting");
            UpdateRespawnAndLooting();
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

            m_characterBoneCapsulesReady = false;

            if (Physics != null)
            {
                m_previousLinearVelocity = Physics.LinearVelocity;
            }
        }

        private void UpdateCharacterStateChange()
        {
            if (!ControllerInfo.IsRemotelyControlled() || Sync.IsServer || Sandbox.Engine.Utils.MyFakes.MULTIPLAYER_CLIENT_PHYSICS)
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

                m_currentRespawnCounter -= VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                if (m_respawnNotification != null)
                    m_respawnNotification.SetTextFormatArguments((int)m_currentRespawnCounter);

                if (m_currentRespawnCounter <= 0)
                {
                    if (Sync.IsServer)
                    {
                        if (ControllerInfo.Controller != null)
                            Sync.Players.KillPlayer(ControllerInfo.Controller.Player);
                    }
                }
            }

            UpdateLooting(VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS);
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();
            //GK: For now put this here in order to not to take to much bandwidth. May split implementation for client and server in the future
            UpdateHudDeathLocation();
        }

        private void UpdateHudDeathLocation()
        {
            if (m_currentLootingCounter > 0 && m_isDeathPlayer && SyncObject != null && SyncObject.Entity != null && SyncObject.Entity.PositionComp != null)
            {
                // Update corpse location
                string bodyLocationName = MyTexts.Get(MySpaceTexts.GPS_Body_Location_Name).ToString();
                MyGps deathLocation = MySession.Static.Gpss.GetGpsByName(MySession.Static.LocalPlayerId, bodyLocationName) as MyGps;

                if (deathLocation != null)
                {
                    deathLocation.Coords = SyncObject.Entity.PositionComp.GetPosition();
                    deathLocation.Coords.X = Math.Round(deathLocation.Coords.X, 2);
                    deathLocation.Coords.Y = Math.Round(deathLocation.Coords.Y, 2);
                    deathLocation.Coords.Z = Math.Round(deathLocation.Coords.Z, 2);
                    MySession.Static.Gpss.SendModifyGps(GetPlayerIdentityId(), deathLocation);
                }
            }
        }

        private bool UpdateLooting(float amount)
        {
            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_CHARACTER_MISC)
                MyRenderProxy.DebugDrawText3D(WorldMatrix.Translation, m_currentLootingCounter.ToString("n1"), Color.Green, 1.0f, false);
            if (m_currentLootingCounter > 0)
            {
                m_currentLootingCounter -= amount;

                if (m_currentLootingCounter <= 0)
                {
                    if (Sync.IsServer)
                    {
                        SyncObject.SendCloseRequest();
                        Save = false;
                        return true;
                    }
                }
            }
            return false;
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
                             m_currentMovementState == MyCharacterMovementEnum.Died ? 5 : 20;

                if (WantsCrouch)
                    bobMax = 3;

                while (m_bobQueue.Count > bobMax)
                    m_bobQueue.Dequeue();
            }
        }

        private void UpdateFallAndSpine()
        {
            var jetpack = JetpackComp;
            if (jetpack != null)
                jetpack.UpdateFall();

            if (m_isFalling)
            {
                if (!JetpackRunning)
                {
                    m_currentFallingTime += VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                    if (m_currentFallingTime > FALL_TIME && !m_isFallingAnimationPlayed)
                    {
                        SwitchAnimation(MyCharacterMovementEnum.Falling, false);
                        m_isFallingAnimationPlayed = true;
                    }
                }
            }

            if ((!JetpackRunning ||
                 (jetpack.Running && (IsLocalHeadAnimationInProgress() || Definition.VerticalPositionFlyingOnly))) &&
                !IsDead && !IsSitting)
            {
                float spineRotation = MathHelper.Clamp(-m_headLocalXAngle, -45, MAX_HEAD_LOCAL_X_ANGLE);

                float bendMultiplier = IsInFirstPersonView
                    ? m_characterDefinition.BendMultiplier1st
                    : m_characterDefinition.BendMultiplier3rd;
                var usedSpineRotation = Quaternion.CreateFromAxisAngle(Vector3.Backward,
                    MathHelper.ToRadians(bendMultiplier * spineRotation));

                if (UseNewAnimationSystem)
                {
                    float leaningValue = m_characterDefinition.BendMultiplier3rd*spineRotation;
                    if (MySession.Static.LocalCharacter == this && 
                        (!MyInput.Static.IsGameControlPressed(Sandbox.Game.MyControlsSpace.LOOKAROUND) || IsInFirstPersonView || ForceFirstPersonCamera || CurrentWeapon != null))
                    {
                        m_animLeaning = leaningValue;
                    }
                }
                else
                {
                    Quaternion clientsSpineRotation = Quaternion.CreateFromAxisAngle(Vector3.Backward,
                        MathHelper.ToRadians(m_characterDefinition.BendMultiplier3rd * spineRotation));
                    SetSpineAdditionalRotation(usedSpineRotation, clientsSpineRotation);
                }
            }
            else
            {
                if (UseNewAnimationSystem)
                {
                    AnimationController.Variables.SetValue(MyAnimationVariableStorageHints.StrIdLean, 0);
                }
                else
                {
                    SetSpineAdditionalRotation(Quaternion.CreateFromAxisAngle(Vector3.Backward, 0),
                        Quaternion.CreateFromAxisAngle(Vector3.Backward, 0));
                }
            }

            if (m_currentWeapon == null && !IsDead && !JetpackRunning && !IsSitting)
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
                //GK: When opening any other screen than main Gameplay Screen EndShoot (only if not already shooting)
                if (!(MyScreenManager.GetScreenWithFocus() is MyGuiScreenGamePlay) && MyScreenManager.IsAnyScreenOpening() && (MyInput.Static.IsGameControlPressed(MyControlsSpace.PRIMARY_TOOL_ACTION) || MyInput.Static.IsGameControlPressed(MyControlsSpace.SECONDARY_TOOL_ACTION)))
                {
                    EndShootAll();
                }
                //UpdateWeaponPosition();

                if (m_currentWeapon.IsShooting)
                {
                    m_currentShootPositionTime = SHOT_TIME;
                }

                ShootInternal();
                // CH: Warning, m_currentWeapon can be null after ShootInternal because of autoswitch!
            }
            else
            {
                if (m_usingByPrimary)
                    UseContinues();                
            }

            if (m_currentShotTime > 0)
            {
                m_currentShotTime -= VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                if (m_currentShotTime <= 0)
                {
                    m_currentShotTime = 0;
                }
            }

            if (m_currentShootPositionTime > 0)
            {
                m_currentShootPositionTime -= VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                if (m_currentShootPositionTime <= 0)
                {
                    m_currentShootPositionTime = 0;
                }
            }
        }

        private void UpdatePhysicalMovement()
        {
            if (!MySandboxGame.IsGameReady || Physics == null || !Physics.Enabled || !MySession.Static.Ready || Physics.HavokWorld == null)
                return;

            var jetpack = JetpackComp;
            bool jetpackActive = !(jetpack == null || !jetpack.UpdatePhysicalMovement());	//Solve Y orientation and gravity only in non flying mode

            m_gravity = MyGravityProviderSystem.CalculateTotalGravityInPoint(PositionComp.WorldAABB.Center) + Physics.HavokWorld.Gravity;

            if ((!jetpackActive || Definition.VerticalPositionFlyingOnly || IsMagneticBootsEnabled) && !IsDead && Physics.CharacterProxy != null)
            {
                if (!Physics.CharacterProxy.Up.IsValid())
                {
                    Debug.Fail("Character Proxy Up vector is invalid! Can not to solve gravity influence on character. Character type: " + this.GetType().ToString());
                    Physics.CharacterProxy.Up = WorldMatrix.Up;
                }

                if (!Physics.CharacterProxy.Forward.IsValid())
                {
                    Debug.Fail("Character Proxy Forward vector is invalid! Can not to solve gravity influence on character. Character type: " + this.GetType().ToString());
                    Physics.CharacterProxy.Forward = WorldMatrix.Forward;
                }

                Vector3 characterUp = Physics.CharacterProxy.Up;
                Vector3 characterForward = Physics.CharacterProxy.Forward;

                if (!jetpackActive)
                    Physics.CharacterProxy.Gravity = m_gravity * MyPerGameSettings.CharacterGravityMultiplier;
                else
                    Physics.CharacterProxy.Gravity = Vector3.Zero;

                // If there is valid non-zero gravity
                if ((m_gravity.LengthSquared() > 0.1f) && (characterUp != Vector3.Zero) && m_gravity.IsValid())
                {
                    UpdateStandup(ref m_gravity, ref characterUp, ref characterForward);
                    if (jetpack != null)
                        jetpack.CurrentAutoEnableDelay = 0;
                }
                // Zero-G
                else
                {
                    if (IsMagneticBootsEnabled)
                    {
                        Vector3 normal = -Physics.CharacterProxy.SupportNormal;
                        UpdateStandup(ref normal, ref characterUp, ref characterForward);
                    }

                    if (jetpack != null && jetpack.CurrentAutoEnableDelay != -1)
                        jetpack.CurrentAutoEnableDelay += VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                }

                Physics.CharacterProxy.Forward = characterForward;
                Physics.CharacterProxy.Up = characterUp;
            }
            else if (IsDead)
            {
                if (Physics == null) Debugger.Break();

                if (Physics.HasRigidBody && Physics.RigidBody.IsActive)
                {
                    Physics.RigidBody.Gravity = m_gravity;
                }
            }

            MatrixD worldMatrix = Physics.GetWorldMatrix();
            bool serverOverride = !MyFakes.MULTIPLAYER_SIMULATE_CHARACTER_CLIENT && Physics.ServerWorldMatrix.HasValue;
            if (serverOverride)
            {
                if (ControllerInfo.IsLocallyControlled())
                    worldMatrix.Translation = Physics.ServerWorldMatrix.Value.Translation;
                else
                    worldMatrix = Physics.ServerWorldMatrix.Value;
            }

            //Include foot error
            if (m_currentMovementState == MyCharacterMovementEnum.Standing)
            {
                m_cummulativeVerticalFootError += m_verticalFootError * 0.2f;
                m_cummulativeVerticalFootError = MathHelper.Clamp(m_cummulativeVerticalFootError, -0.75f, 0.75f);
            }
            else
                m_cummulativeVerticalFootError = 0;

            worldMatrix.Translation = worldMatrix.Translation + worldMatrix.Up * m_cummulativeVerticalFootError;

            if (Vector3D.DistanceSquared(WorldMatrix.Translation, worldMatrix.Translation) > 0.00001f ||
                Vector3D.DistanceSquared(WorldMatrix.Forward, worldMatrix.Forward) > 0.00001f ||
                Vector3D.DistanceSquared(WorldMatrix.Up, worldMatrix.Up) > 0.00001f ||
                serverOverride)
            {
                PositionComp.SetWorldMatrix(worldMatrix, serverOverride ? null : Physics);
                Physics.ServerWorldMatrix = null;
            }

            if (ControllerInfo.IsLocallyControlled())
            {
                Physics.UpdateAccelerations();
            }
            else if (!ControllerInfo.IsLocallyControlled() && Sync.IsServer)
            {
                Physics.UpdateAccelerations();
            } //otherwise OnPositionUpdate message it is updated
        }

        private void UpdateStandup(ref Vector3 gravity, ref Vector3 chUp, ref Vector3 chForward)
        {
            Vector3 minusGravity = -Vector3.Normalize(gravity);
            Vector3 testUp = minusGravity;
            if (Physics != null && Physics.CharacterProxy != null && Physics.CharacterProxy.Supported)
            {
                Vector3 supportNormal = Physics.CharacterProxy.SupportNormal;
                if (Definition.RotationToSupport == MyEnumCharacterRotationToSupport.OneAxis)
                {
                    float cosAngleMinusGravityToNormal = minusGravity.Dot(ref supportNormal);
                    if (!MyUtils.IsZero(cosAngleMinusGravityToNormal - 1) &&
                        !MyUtils.IsZero(cosAngleMinusGravityToNormal + 1))
                    {
                        Vector3 poleVec = minusGravity.Cross(supportNormal);
                        poleVec.Normalize();
                        testUp = Vector3.Lerp(supportNormal, minusGravity, Math.Abs(poleVec.Dot(WorldMatrix.Forward)));
                    }
                }
                else if (Definition.RotationToSupport == MyEnumCharacterRotationToSupport.Full)
                {
                    testUp = supportNormal;
                }
            }

            var dotProd = Vector3.Dot(chUp, testUp);
            var lenProd = chUp.Length() * testUp.Length();
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

                Vector3 normal = Vector3.Cross(chUp, testUp);

                if (normal.LengthSquared() > 0)
                {
                    normal = Vector3.Normalize(normal);
                    chUp = Vector3.TransformNormal(chUp, Matrix.CreateFromAxisAngle(normal, angle));
                    chForward = Vector3.TransformNormal(chForward, Matrix.CreateFromAxisAngle(normal, angle));
                }
            }
        }

        private void UpdateShake()
        {
            if (MySession.Static.LocalHumanPlayer == null)
                return;

            if (this == MySession.Static.LocalHumanPlayer.Identity.Character)
            {
                UpdateHudCharacterInfo();

                if (
                    (m_currentMovementState == MyCharacterMovementEnum.Standing) ||
                    (m_currentMovementState == MyCharacterMovementEnum.Crouching) ||
                    (m_currentMovementState == MyCharacterMovementEnum.Flying))
                    m_currentHeadAnimationCounter += VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                else
                    m_currentHeadAnimationCounter = 0;

                if (m_currentLocalHeadAnimation >= 0)
                {
                    m_currentLocalHeadAnimation += VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

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

        private void UpdateDying()
        {
            if (m_dieAfterSimulation)
            {
                DieInternal();
                m_dieAfterSimulation = false;
            }
        }

        internal void SetHeadLocalXAngle(float angle)
        {
            HeadLocalXAngle = angle;
        }

        private void SetHeadLocalYAngle(float angle)
        {
            HeadLocalYAngle = angle;
        }

        private bool ShouldUseAnimatedHeadRotation()
        {
            //if (m_currentHeadAnimationCounter > 0.15f)
            //  return true;

            return false;
        }

        private Vector3D m_crosshairPoint;

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

        private Vector3D m_aimedPoint;

        private Vector3D GetAimedPointFromHead()
        {
            MatrixD headMatrix = GetHeadMatrix(false);
            var endPoint = headMatrix.Translation + headMatrix.Forward * 25000;
            // Same optimization as the one in GetAimedPointFromCamera.
            return endPoint;

            if (MySession.Static.ControlledEntity == this)
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

        private Vector3D GetAimedPointFromCamera()
        {
            Vector3D endPoint = WeaponPosition != null
                ? (WeaponPosition.LogicalPositionWorld + MySector.MainCamera.ForwardVector * 25000)
                : (MySector.MainCamera.Position + MySector.MainCamera.ForwardVector * 25000);

            if (MySession.Static.ControlledEntity == this)
            {
                LineD line = new LineD(MySector.MainCamera.Position, MySector.MainCamera.Position + MySector.MainCamera.ForwardVector * 100);
                var intersection = MyEntities.GetIntersectionWithLine(ref line, this, (MyEntity)m_currentWeapon);
                if (intersection.HasValue && intersection.Value.Entity != null)
                {
                    endPoint = intersection.Value.IntersectionPointInWorldSpace;
                }
            }


            //VRageRender.MyRenderProxy.DebugDrawLine3D(WeaponPosition.LogicalPositionWorld, endPoint, Color.Red, Color.Red, false);
            //VRageRender.MyRenderProxy.DebugDrawSphere(endPoint, 0.1f, Color.Red, 1, true);

            // There doesn't seem to be any difference between doing the raycast and just
            // returning the end point. However, 25km raycast causes distant voxel maps to
            // generate geometry along the ray path, unless it is already cached (which it usually isn't),
            // and that can take very long time.
            return endPoint;
        }

        #endregion Simulation

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
                RotateHead(rotationIndicator);
                //MoveAndRotate(Vector3.Zero, rotationIndicator, roll);
            }
        }

        public void RotateStopped()
        {
        }

        public void MoveAndRotateStopped()
        {
        }

        public void MoveAndRotate(Vector3 moveIndicator, Vector2 rotationIndicator, float rollIndicator)
        {
            if (moveIndicator == Vector3.Zero && rotationIndicator == Vector2.Zero && rollIndicator == 0)
            {
                if (MoveIndicator == moveIndicator && rotationIndicator == RotationIndicator && RollIndicator == rollIndicator)
                {
                    return;
                }

                MoveIndicator = Vector3.Zero;
                RotationIndicator = Vector2.Zero;
                RollIndicator = 0;

                m_moveAndRotateStopped = true;
                return;
            }

            MoveIndicator = moveIndicator;
            RotationIndicator = rotationIndicator;
            RollIndicator = rollIndicator;

            m_moveAndRotateCalled = true;

            if (this == MySession.Static.LocalCharacter)
            {
                if (MyInput.Static.IsAnyCtrlKeyPressed() && MyInput.Static.IsAnyAltKeyPressed())
                {
                    if (MyInput.Static.PreviousMouseScrollWheelValue() < MyInput.Static.MouseScrollWheelValue())
                    {
                        RotationSpeed = Math.Min(RotationSpeed * 1.5f, CHARACTER_X_ROTATION_SPEED);
                    }
                    else if (MyInput.Static.PreviousMouseScrollWheelValue() > MyInput.Static.MouseScrollWheelValue())
                    {
                        RotationSpeed = Math.Max(RotationSpeed / 1.5f, .01f);
                    }
                }
            }
        }

        public void CacheMove(ref Vector3 moveIndicator, ref Quaternion rotate)
        {
            if (m_cachedCommands == null)
            {
                m_cachedCommands = new List<IMyNetworkCommand>();
            }

            m_cachedCommands.Add(new MyMoveNetCommand(this, ref moveIndicator, ref rotate));
        }

        public void CacheMoveDelta(ref Vector3D moveDeltaIndicator)
        {
            if (m_cachedCommands == null)
            {
                m_cachedCommands = new List<IMyNetworkCommand>();
            }

            m_cachedCommands.Add(new MyDeltaNetCommand(this, ref moveDeltaIndicator));
        }

        internal void MoveAndRotateInternal(Vector3 moveIndicator, Vector2 rotationIndicator, float roll, Vector3 rotationCenter)
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

            bool sprint = moveIndicator.Z != 0 && WantsSprint;
            bool walk = WantsWalk;
            bool jump = WantsJump;
            bool canMove = !JetpackRunning && !((m_currentCharacterState == HkCharacterStateType.HK_CHARACTER_IN_AIR || (int)m_currentCharacterState == MyCharacter.HK_CHARACTER_FLYING) && (m_currentJumpTime <= 0)) && (m_currentMovementState != MyCharacterMovementEnum.Died);
            bool canRotate = (JetpackRunning || !((m_currentCharacterState == HkCharacterStateType.HK_CHARACTER_IN_AIR || (int)m_currentCharacterState == MyCharacter.HK_CHARACTER_FLYING) && (m_currentJumpTime <= 0))) && (m_currentMovementState != MyCharacterMovementEnum.Died);

            float acceleration = 0;
            float lastSpeed = m_currentSpeed;
            if (JetpackRunning)
            {
                JetpackComp.MoveAndRotate(ref moveIndicator, ref rotationIndicator, roll, canRotate);
            }
            else if (canMove || m_movementsFlagsChanged)
            {
                if (moveIndicator.LengthSquared() > 0)
                    moveIndicator = Vector3.Normalize(moveIndicator);

                MyCharacterMovementEnum newMovementState = GetNewMovementState(ref moveIndicator, ref rotationIndicator, ref acceleration, sprint, walk, canMove, m_movementsFlagsChanged);
                SwitchAnimation(newMovementState);

                m_movementsFlagsChanged = false;

                SetCurrentMovementState(newMovementState);
                if (newMovementState == MyCharacterMovementEnum.Sprinting && StatComp != null)
                {
                    StatComp.ApplyModifier("Sprint");
                }

                if (!IsIdle)
                    m_currentWalkDelay = MathHelper.Clamp(m_currentWalkDelay - VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS, 0, m_currentWalkDelay);

                if (canMove)
                {
                    float relativeSpeed = 1.0f;
                    m_currentSpeed = LimitMaxSpeed(m_currentSpeed + (m_currentWalkDelay <= 0 ? acceleration * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS : 0), m_currentMovementState, relativeSpeed);
                }

                if (Physics.CharacterProxy != null)
                {
                    Physics.CharacterProxy.PosX = m_currentMovementState != MyCharacterMovementEnum.Sprinting ? -moveIndicator.X : 0;
                    Physics.CharacterProxy.PosY = moveIndicator.Z;
                    Physics.CharacterProxy.Elevate = 0;
                }

                if (canMove && m_currentMovementState != MyCharacterMovementEnum.Jump)
                {
                    int sign = Math.Sign(m_currentSpeed);
                    m_currentSpeed += -sign * m_currentDecceleration * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

                    if (Math.Sign(sign) != Math.Sign(m_currentSpeed))
                        m_currentSpeed = 0;
                }

                if (Physics.CharacterProxy != null)
                    Physics.CharacterProxy.Speed = m_currentMovementState != MyCharacterMovementEnum.Died ? m_currentSpeed : 0;

                if (Physics.CharacterProxy != null && Physics.CharacterProxy.GetHitRigidBody() != null)
                {

                    if ((jump && m_currentMovementState != MyCharacterMovementEnum.Jump))
                    {
                        PlayCharacterAnimation("Jump", MyBlendOption.Immediate, MyFrameOption.StayOnLastFrame, 0.0f, 1.3f);

                        if (UseNewAnimationSystem)
                            TriggerCharacterAnimationEvent("jump", true);

                        if (StatComp != null)
                        {
                            StatComp.DoAction("Jump");
                            StatComp.ApplyModifier("Jump");
                        }
                        m_currentJumpTime = JUMP_DURATION;
                        SetCurrentMovementState(MyCharacterMovementEnum.Jump);

                        m_canJump = false;

                        m_frictionBeforeJump = Physics.CharacterProxy.GetHitRigidBody().Friction;
                        // Modified jumping velocity, no need to apply force
                        //Physics.CharacterProxy.GetHitRigidBody().ApplyForce(1, WorldMatrix.Up * Definition.JumpForce * MyPerGameSettings.CharacterGravityMultiplier * Physics.Mass);
                        Physics.CharacterProxy.Jump = true;
                    }

                    //VRage.Trace.MyTrace.Send(VRage.Trace.TraceWindow.Default, "jump");

                    

                    if (m_currentJumpTime > 0)
                    {
                        m_currentJumpTime -= VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;

                        Physics.CharacterProxy.GetHitRigidBody().Friction = 0;
                        Vector3 rotatedVector = WorldMatrix.Forward * -moveIndicator.Z + WorldMatrix.Right * moveIndicator.X;
                        // Modified jumping velocity, no need to apply velocity
                        //Physics.CharacterProxy.GetHitRigidBody().ApplyForce(1, rotatedVector * AERIAL_CONTROL_FORCE_MULTIPLIER * Physics.Mass);
                    }
                    else // If still falling, check if finished.
                    {
                        MyCharacterMovementEnum afterJumpState = MyCharacterMovementEnum.Standing;

                        // Restore friction setting upon end-of-jump time.
                        Physics.CharacterProxy.GetHitRigidBody().Friction = m_frictionBeforeJump;

                        // If started falling in physics, set the char to correct state.
                        if (Physics.CharacterProxy != null && (Physics.CharacterProxy.GetState() == HkCharacterStateType.HK_CHARACTER_IN_AIR || (int)Physics.CharacterProxy.GetState() == MyCharacter.HK_CHARACTER_FLYING))
                        {
                            StartFalling();
                        }
                        // Didn't have time to start falling. Ex. landed on a mountain before started falling.
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

                            if (!m_canJump)
                                SoundComp.PlayFallSound();
                            m_canJump = true;
                            SetCurrentMovementState(afterJumpState);
                        }

                        m_currentJumpTime = 0;
                    }
                }
            }
            else if (Physics.CharacterProxy != null)
            {
                Physics.CharacterProxy.Elevate = 0;
            }

            if (!JetpackRunning)
            {
                if (rotationIndicator.Y != 0 && (canRotate || m_isFalling || m_currentJumpTime > 0) && Physics.CharacterProxy != null)
                {
                    MatrixD rotationMatrix = MatrixD.CreateRotationY((-rotationIndicator.Y * RotationSpeed * CHARACTER_Y_ROTATION_FACTOR));
                    MatrixD characterMatrix = MatrixD.CreateWorld(Physics.CharacterProxy.Position, Physics.CharacterProxy.Forward, Physics.CharacterProxy.Up);
                    Vector3D headBoneTranslation = Vector3D.Zero;

                    characterMatrix = rotationMatrix * characterMatrix;

                    Physics.CharacterProxy.Forward = characterMatrix.Forward;
                    Physics.CharacterProxy.Up = characterMatrix.Up;
                }

                if (rotationIndicator.X != 0)
                {
                    if (((m_currentMovementState == MyCharacterMovementEnum.Died) && !m_isInFirstPerson)
                        ||
                        (m_currentMovementState != MyCharacterMovementEnum.Died))
                    {
                        SetHeadLocalXAngle(m_headLocalXAngle - rotationIndicator.X * RotationSpeed);

                        int headBone = IsInFirstPersonView ? m_headBoneIndex : m_camera3rdBoneIndex;

                        if (headBone != -1)
                        {
                            m_bobQueue.Clear();
                            m_bobQueue.Enqueue(BoneAbsoluteTransforms[headBone].Translation);
                        }
                    }
                }
            }

            if (Physics.CharacterProxy != null)
            {
                if (Physics.CharacterProxy.LinearVelocity.LengthSquared() > 0.1f)
                    m_shapeContactPoints.Clear();
            }

            WantsJump = false;
            WantsFlyUp = false;
            WantsFlyDown = false;
        }

        private void RotateHead(Vector2 rotationIndicator)
        {
            const float sensitivity = 0.5f;
            if (rotationIndicator.X != 0)
                SetHeadLocalXAngle(m_headLocalXAngle - rotationIndicator.X * sensitivity);

            if (rotationIndicator.Y != 0)
            {
                float yAngleDelta = -rotationIndicator.Y * sensitivity;
                SetHeadLocalYAngle(m_headLocalYAngle + yAngleDelta);
            }
        }

        public bool IsIdle
        {
            get { return m_currentMovementState == MyCharacterMovementEnum.Standing || m_currentMovementState == MyCharacterMovementEnum.Crouching; }
        }

        private List<HkBodyCollision> m_penetrationList = new List<HkBodyCollision>();

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

            //Check for grids
            m_penetrationList.Clear();
            MyPhysics.GetPenetrationsShape(Physics.CharacterProxy != null ? Physics.CharacterProxy.GetCollisionShape() : Physics.RigidBody.GetShape(), ref translation, ref rotation, m_penetrationList, MyPhysics.CollisionLayers.CharacterCollisionLayer);
            bool somethingHit = false;
            foreach (var collision in m_penetrationList)
            {
                IMyEntity collisionEntity = collision.GetCollisionEntity();
                if (collisionEntity != null)
                {
                    if (collisionEntity.Physics == null)
                    {
                        Debug.Fail("CanPlaceCharacter found Entity with no physics: " + collisionEntity);
                        MyLog.Default.WriteLine("CanPlaceCharacter found Entity with no physics: " + collisionEntity);
                    }
                    else if (!collisionEntity.Physics.IsPhantom)
                    {
                        somethingHit = true;
                        break;
                    }
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
            { //Check for voxels
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

        public MyCharacterMovementEnum GetNetworkMovementState()
        {
            return m_previousNetworkMovementState;
        }

        public void CurrentMovementState(MyCharacterMovementEnum state)
        {
            m_currentMovementState = state;
        }

        public void SetPreviousMovementState(MyCharacterMovementEnum previousMovementState)
        {
            m_previousMovementState = previousMovementState;
        }

        internal void SetCurrentMovementState(MyCharacterMovementEnum state)
        {
            System.Diagnostics.Debug.Assert(m_currentMovementState != MyCharacterMovementEnum.Died || m_currentMovementState == state, "Trying to set a new movement state, but character is in dead state!");

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
        }

        private float GetMovementAcceleration(MyCharacterMovementEnum movement)
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

        public static bool IsWalkingState(MyCharacterMovementEnum state)
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

        public static bool IsRunningState(MyCharacterMovementEnum state)
        {
            switch (state)
            {
                case MyCharacterMovementEnum.Running:
                case MyCharacterMovementEnum.Backrunning:
                case MyCharacterMovementEnum.RunningLeftBack:
                case MyCharacterMovementEnum.RunningRightBack:
                case MyCharacterMovementEnum.RunStrafingLeft:
                case MyCharacterMovementEnum.RunStrafingRight:
                case MyCharacterMovementEnum.RunningLeftFront:
                case MyCharacterMovementEnum.RunningRightFront:
                case MyCharacterMovementEnum.Sprinting:
                    return true;
                    break;

                default:
                    return false;
            }
        }

        internal void SwitchAnimation(MyCharacterMovementEnum movementState, bool checkState = true)
        {
            if (MySandboxGame.IsDedicated && MyPerGameSettings.DisableAnimationsOnDS)
                return;

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
                    PlayCharacterAnimation("CrouchLeftTurn", AdjustSafeAnimationEnd(MyBlendOption.WaitForPreviousEnd), MyFrameOption.Loop, AdjustSafeAnimationBlend(0.2f));
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

        private MyCharacterMovementEnum GetNewMovementState(ref Vector3 moveIndicator, ref Vector2 rotationIndicator, ref float acceleration, bool sprint, bool walk, bool canMove, bool movementFlagsChanged)
        {
            // OM: Once dead, always dead, no resurrection in the game :-)
            if (m_currentMovementState == MyCharacterMovementEnum.Died)
            {
                return MyCharacterMovementEnum.Died;
            }

            MyCharacterMovementEnum newMovementState = m_currentMovementState;

            if (Definition.UseOnlyWalking)
                walk = true;

            if (m_currentJumpTime > 0f)
                return MyCharacterMovementEnum.Jump;

            if (JetpackRunning)
                return MyCharacterMovementEnum.Flying;

            bool canWalk = true;
            bool canRun = true;
            bool canSprint = true;
            bool canMoveInternal = true;
            bool continuousWalk = false;
            bool continuousRun = false;
            bool continuousSprint = false;

            var currentState = m_currentMovementState;
            switch (currentState)
            {
                case MyCharacterMovementEnum.Walking:
                case MyCharacterMovementEnum.WalkingLeftBack:
                case MyCharacterMovementEnum.WalkingLeftFront:
                case MyCharacterMovementEnum.WalkingRightBack:
                case MyCharacterMovementEnum.WalkingRightFront:
                case MyCharacterMovementEnum.WalkStrafingLeft:
                case MyCharacterMovementEnum.WalkStrafingRight:
                    continuousWalk = true;
                    break;

                case MyCharacterMovementEnum.Running:
                case MyCharacterMovementEnum.RunningLeftBack:
                case MyCharacterMovementEnum.RunningLeftFront:
                case MyCharacterMovementEnum.RunningRightBack:
                case MyCharacterMovementEnum.RunningRightFront:
                case MyCharacterMovementEnum.RunStrafingLeft:
                case MyCharacterMovementEnum.RunStrafingRight:
                    continuousRun = true;
                    break;

                case MyCharacterMovementEnum.Sprinting:
                    continuousSprint = true;
                    break;
            }

            MyTuple<ushort, MyStringHash> message;
            if (StatComp != null)
            {
                canWalk = StatComp.CanDoAction("Walk", out message, continuousWalk);
                canRun = StatComp.CanDoAction("Run", out message, continuousRun);
                canSprint = StatComp.CanDoAction("Sprint", out message, continuousSprint);

                if (MySession.Static != null && MySession.Static.LocalCharacter == this && message.Item1 == MyStatLogic.STAT_VALUE_TOO_LOW && message.Item2.String.CompareTo("Stamina") == 0)
                {
                    m_notEnoughStatNotification.SetTextFormatArguments(message.Item2);
                    MyHud.Notifications.Add(m_notEnoughStatNotification);
                }

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
                        else if (canRun)
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

        internal float LimitMaxSpeed(float currentSpeed, MyCharacterMovementEnum movementState, float serverRatio)
        {
            float limitedSpeed = currentSpeed;
            switch (movementState)
            {
                case MyCharacterMovementEnum.Running:
                case MyCharacterMovementEnum.Flying:
                    {
                        limitedSpeed = MathHelper.Clamp(currentSpeed, -Definition.MaxRunSpeed * serverRatio, Definition.MaxRunSpeed * serverRatio);
                        break;
                    }

                case MyCharacterMovementEnum.Walking:
                    {
                        limitedSpeed = MathHelper.Clamp(currentSpeed, -Definition.MaxWalkSpeed * serverRatio, Definition.MaxWalkSpeed * serverRatio);
                        break;
                    }

                case MyCharacterMovementEnum.BackWalking:
                case MyCharacterMovementEnum.WalkingLeftBack:
                case MyCharacterMovementEnum.WalkingRightBack:
                    {
                        limitedSpeed = MathHelper.Clamp(currentSpeed, -Definition.MaxBackwalkSpeed * serverRatio, Definition.MaxBackwalkSpeed * serverRatio);
                        break;
                    }

                case MyCharacterMovementEnum.WalkStrafingLeft:
                case MyCharacterMovementEnum.WalkStrafingRight:
                case MyCharacterMovementEnum.WalkingLeftFront:
                case MyCharacterMovementEnum.WalkingRightFront:
                    {
                        limitedSpeed = MathHelper.Clamp(currentSpeed, -Definition.MaxWalkStrafingSpeed * serverRatio, Definition.MaxWalkStrafingSpeed * serverRatio);
                        break;
                    }

                case MyCharacterMovementEnum.Backrunning:
                case MyCharacterMovementEnum.RunningLeftBack:
                case MyCharacterMovementEnum.RunningRightBack:
                    {
                        limitedSpeed = MathHelper.Clamp(currentSpeed, -Definition.MaxBackrunSpeed * serverRatio, Definition.MaxBackrunSpeed * serverRatio);
                        break;
                    }

                case MyCharacterMovementEnum.RunStrafingLeft:
                case MyCharacterMovementEnum.RunStrafingRight:
                case MyCharacterMovementEnum.RunningLeftFront:
                case MyCharacterMovementEnum.RunningRightFront:
                    {
                        limitedSpeed = MathHelper.Clamp(currentSpeed, -Definition.MaxRunStrafingSpeed * serverRatio, Definition.MaxRunStrafingSpeed * serverRatio);
                        break;
                    }

                case MyCharacterMovementEnum.CrouchWalking:
                    {
                        limitedSpeed = MathHelper.Clamp(currentSpeed, -Definition.MaxCrouchWalkSpeed * serverRatio, Definition.MaxCrouchWalkSpeed * serverRatio);
                        break;
                    }

                case MyCharacterMovementEnum.CrouchStrafingLeft:
                case MyCharacterMovementEnum.CrouchStrafingRight:
                case MyCharacterMovementEnum.CrouchWalkingLeftFront:
                case MyCharacterMovementEnum.CrouchWalkingRightFront:
                    {
                        limitedSpeed = MathHelper.Clamp(currentSpeed, -Definition.MaxCrouchStrafingSpeed * serverRatio, Definition.MaxCrouchStrafingSpeed * serverRatio);
                        break;
                    }

                case MyCharacterMovementEnum.CrouchBackWalking:
                case MyCharacterMovementEnum.CrouchWalkingLeftBack:
                case MyCharacterMovementEnum.CrouchWalkingRightBack:
                    {
                        limitedSpeed = MathHelper.Clamp(currentSpeed, -Definition.MaxCrouchBackwalkSpeed * serverRatio, Definition.MaxCrouchBackwalkSpeed * serverRatio);
                        break;
                    }

                case MyCharacterMovementEnum.Sprinting:
                    {
                        limitedSpeed = MathHelper.Clamp(currentSpeed, -Definition.MaxSprintSpeed * serverRatio, Definition.MaxSprintSpeed * serverRatio);
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

        private bool UpdateCapsuleBones()
        {
            if (m_characterBoneCapsulesReady)
                return true;

            if (m_bodyCapsuleInfo == null || m_bodyCapsuleInfo.Count == 0)
                return false;

            if (this.Definition.Name == "Space_spider")
                MyRenderDebugInputComponent.Clear();

            var characterBones = AnimationController.CharacterBones;

            if (Physics.Ragdoll != null && Components.Has<MyCharacterRagdollComponent>())
            {
                // TODO: OM - This needs to be changed..
                // Create capsules with help of ragdoll model
                var ragdollComponent = Components.Get<MyCharacterRagdollComponent>();
                for (int i = 0; i < m_bodyCapsuleInfo.Count; i++)
                {
                    var boneInfo = m_bodyCapsuleInfo[i];
                    if (characterBones == null || boneInfo.Bone1 >= characterBones.Length || boneInfo.Bone2 >= characterBones.Length) // prevent crashes
                        continue;

                    var rigidBody = ragdollComponent.RagdollMapper.GetBodyBindedToBone(characterBones[boneInfo.Bone1]);

                    MatrixD transformationMatrix = characterBones[boneInfo.Bone1].AbsoluteTransform * WorldMatrix;

                    var shape = rigidBody.GetShape();

                    m_bodyCapsules[i].P0 = transformationMatrix.Translation;
                    m_bodyCapsules[i].P1 = (characterBones[boneInfo.Bone2].AbsoluteTransform * WorldMatrix).Translation;
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
                                if (this.Definition.Name == "Space_spider")
                                    MyRenderDebugInputComponent.AddCapsule(m_bodyCapsules[i], Color.Green);
                                //MyRenderProxy.DebugDrawCapsule(m_bodyCapsules[i].P0, m_bodyCapsules[i].P1, m_bodyCapsules[i].Radius, Color.Green, false, false);
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
                                if (this.Definition.Name == "Space_spider")
                                    MyRenderDebugInputComponent.AddCapsule(m_bodyCapsules[i], Color.Blue);
                                //MyRenderProxy.DebugDrawCapsule(m_bodyCapsules[i].P0, m_bodyCapsules[i].P1, m_bodyCapsules[i].Radius, Color.Blue, false, false);
                            }
                        }
                    }
                    else
                    {
                        if (boneInfo.Radius != 0)
                        {
                            m_bodyCapsules[i].Radius = boneInfo.Radius;
                        }
                        else if (shape.ShapeType == HkShapeType.Capsule)
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
                            if (this.Definition.Name == "Space_spider")
                                MyRenderDebugInputComponent.AddCapsule(m_bodyCapsules[i], Color.Blue);
                            MyRenderProxy.DebugDrawCapsule(m_bodyCapsules[i].P0, m_bodyCapsules[i].P1, m_bodyCapsules[i].Radius, Color.Yellow, false, false);
                        }
                    }
                }
            }
            else
            {
                // Fallback to dynamically determined values for capsules
                for (int i = 0; i < m_bodyCapsuleInfo.Count; i++)
                {
                    var capsuleInfo = m_bodyCapsuleInfo[i];
                    if (characterBones == null || capsuleInfo.Bone1 >= characterBones.Length || capsuleInfo.Bone2 >= characterBones.Length) // prevent crashes
                        continue;

                    m_bodyCapsules[i].P0 = (characterBones[capsuleInfo.Bone1].AbsoluteTransform * WorldMatrix).Translation;
                    m_bodyCapsules[i].P1 = (characterBones[capsuleInfo.Bone2].AbsoluteTransform * WorldMatrix).Translation;
                    Vector3 difference = m_bodyCapsules[i].P0 - m_bodyCapsules[i].P1;

                    if (capsuleInfo.Radius != 0)
                    {
                        m_bodyCapsules[i].Radius = capsuleInfo.Radius;
                    }
                    else if (difference.LengthSquared() < 0.05f)
                    {
                        m_bodyCapsules[i].P1 = m_bodyCapsules[i].P0 + (characterBones[capsuleInfo.Bone1].AbsoluteTransform * WorldMatrix).Left * 0.1f;
                        m_bodyCapsules[i].Radius = 0.1f;
                    }
                    else
                    {
                        m_bodyCapsules[i].Radius = difference.Length() * 0.3f;
                    }

                    if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_SHOW_DAMAGE)
                    {
                        if (this.Definition.Name == "Space_spider")
                            MyRenderDebugInputComponent.AddCapsule(m_bodyCapsules[i], Color.Green);
                        //MyRenderProxy.DebugDrawCapsule(m_bodyCapsules[i].P0, m_bodyCapsules[i].P1, m_bodyCapsules[i].Radius, Color.Green, false, false);
                    }
                }
            }

            m_characterBoneCapsulesReady = true;
            return true;
        }

        #endregion Movement

        private MatrixD GetHeadMatrixInternal(int headBone, bool includeY, bool includeX = true, bool forceHeadAnim = false, bool forceHeadBone = false)
        {
            if (PositionComp == null)
                return MatrixD.Identity;
            //Matrix matrixRotation = Matrix.Identity;
            MatrixD matrixRotation = MatrixD.Identity;

            bool useAnimationInsteadX = ShouldUseAnimatedHeadRotation() && (!JetpackRunning || IsLocalHeadAnimationInProgress()) || forceHeadAnim;

            if (includeX && !useAnimationInsteadX)
                matrixRotation = MatrixD.CreateFromAxisAngle(Vector3D.Right, MathHelper.ToRadians(m_headLocalXAngle));

            if (includeY)
                matrixRotation = matrixRotation * Matrix.CreateFromAxisAngle(Vector3.Up, MathHelper.ToRadians(m_headLocalYAngle));

            Vector3 averageBob = Vector3.Zero;
            // MZ: hotfixed seeing face if MySandboxGame.Config.DisableHeadbob is true
            //     we have a static camera dummy now, so in fact we need to ADD bouncing
            //     headbob would be redone later
            //if (MySandboxGame.Config.DisableHeadbob && !forceHeadBone && !ForceFirstPersonCamera)
            //{
            //    foreach (var headTranslation in m_bobQueue)
            //    {
            //        averageBob += headTranslation;
            //    }
            //    if (m_bobQueue.Count > 0)
            //        averageBob /= m_bobQueue.Count;
            //}
            //else
            {
                if (headBone != -1)
                {
                    averageBob = BoneAbsoluteTransforms[headBone].Translation;
                    float applyIkOneHeadBoneWeight = 1 - (float)Math.Cos(MathHelper.ToRadians(m_headLocalXAngle)); // the headbone should be ignored by IK, but here, we need to reapply it to have correct distance from ground when looking down
                    averageBob.Y += applyIkOneHeadBoneWeight * AnimationController.InverseKinematics.RootBoneVerticalOffset;
                }
            }

            //if (this.ControllerInfo.IsLocallyControlled() && sync)
            //{
            //    m_averageBob = averageBob;
            //}
            //else if (this.ControllerInfo.IsRemotelyControlled() && Sync.IsServer)
            //{
            //    averageBob = m_averageBob;
            //}

            if (useAnimationInsteadX && headBone != -1
                && BoneAbsoluteTransforms[headBone].Right.LengthSquared() > float.Epsilon    // MZ: fixing NaN issue
                && BoneAbsoluteTransforms[headBone].Up.LengthSquared() > float.Epsilon
                && BoneAbsoluteTransforms[headBone].Forward.LengthSquared() > float.Epsilon)
            {
                //m_headMatrix = Matrix.CreateRotationX(-(float)Math.PI * 0.5f) * /* Matrix.CreateRotationY(-(float)Math.PI * 0.5f) */ Matrix.Normalize(BoneTransformsWrite[HEAD_DUMMY_BONE]);
                Matrix hm = Matrix.Identity;// Matrix.Normalize(BoneAbsoluteTransforms[headBone]);
                hm.Translation = averageBob;
                m_headMatrix = MatrixD.CreateRotationX(-Math.PI * 0.5) * hm;
            }
            else
            {
                //m_headMatrix = Matrix.CreateTranslation(BoneTransformsWrite[HEAD_DUMMY_BONE].Translation);
                m_headMatrix = MatrixD.CreateTranslation(0, averageBob.Y, averageBob.Z);
            }

            MatrixD headPosition = matrixRotation * m_headMatrix * WorldMatrix;
            //headPosition.Translation += WorldMatrix.Up * 0.04f;
            //Vector3 translation = Vector3.Transform(headPosition, WorldMatrix);

            //Orient direction to some point in front of char
            Vector3D imagPoint = PositionComp.GetPosition() + WorldMatrix.Up + WorldMatrix.Forward * 10;
            Vector3 imagDir = Vector3.Normalize(imagPoint - headPosition.Translation);

            MatrixD orientMatrix = MatrixD.CreateFromDir((Vector3D)imagDir, WorldMatrix.Up);

            MatrixD matrix = m_headMatrix * matrixRotation * orientMatrix;
            matrix.Translation = headPosition.Translation;

            return matrix;
        }

        public MatrixD GetHeadMatrix(bool includeY, bool includeX = true, bool forceHeadAnim = false, bool forceHeadBone = false, bool preferLocalOverSync = false)
        {
            //if (preferLocalOverSync || ControllerInfo.IsLocallyControlled())
            {
                int headBone = IsInFirstPersonView || forceHeadBone || ForceFirstPersonCamera ? m_headBoneIndex : m_camera3rdBoneIndex;
                MatrixD headMatrix = GetHeadMatrixInternal(headBone, includeY, includeX, forceHeadAnim, forceHeadBone);
                MatrixD headMatrixLocal = headMatrix * PositionComp.WorldMatrixInvScaled;
                //MyTransform transformToBeSent = new MyTransform(headMatrixLocal);
                //transformToBeSent.Rotation = Quaternion.Normalize(transformToBeSent.Rotation);

                //m_localHeadTransform = transformToBeSent;

                return headMatrix;
            }
            /*else
            {
                return m_localHeadTransform.TransformMatrix * PositionComp.WorldMatrix;
            }*/
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
                    target = Vector3.Transform(AnimationController.CharacterBones[m_headBoneIndex].AbsoluteTransform.Translation, WorldMatrix);
                }
                MatrixD viewMatrix = MatrixD.CreateLookAt(camPosition, target, Vector3.Up);
                return viewMatrix.IsValid() && viewMatrix != MatrixD.Zero ? viewMatrix : m_lastCorrectSpectatorCamera;
            }

            if (!m_isInFirstPersonView)
            {
                //Matrix viewMatrix = Get3rdCameraMatrix(false, true);
                bool lastForceFirstPersonCamera = ForceFirstPersonCamera;
                ForceFirstPersonCamera = !MyThirdPersonSpectator.Static.IsCameraPositionOk();
                if (!ForceFirstPersonCamera)
                {
                    if (m_switchBackToSpectatorTimer > 0)
                    {
                        m_switchBackToSpectatorTimer -= VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
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
                        m_switchBackToFirstPersonTimer -= VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                        ForceFirstPersonCamera = false;
                        return MyThirdPersonSpectator.Static.GetViewMatrix();
                    }
                    else
                    {
                        m_switchBackToSpectatorTimer = CAMERA_SWITCH_DELAY;
                    }
                }
            }

            MatrixD matrix = GetHeadMatrix(false, true, preferLocalOverSync: true);
            matrix.Translation = matrix.Translation;// +m_headSafeOffset;

            if (IsDead)
            {
                Vector3 halfExtents = new Vector3(Definition.CharacterHeadSize * 0.5f);
                Vector3D headPos = matrix.Translation;
                m_penetrationList.Clear();
                MyPhysics.GetPenetrationsBox(ref halfExtents, ref headPos, ref Quaternion.Identity, m_penetrationList, 0);

                foreach (var item in m_penetrationList)
                {
                    if (item.GetCollisionEntity() is MyVoxelBase)
                    {
                        Vector3D directionAgainstGravity = -MyGravityProviderSystem.CalculateTotalGravityInPoint(headPos);
                        directionAgainstGravity.Normalize();
                        directionAgainstGravity *= 0.3f;
                        matrix.Translation += directionAgainstGravity;
                        break;
                    }
                }
            }

            m_lastCorrectSpectatorCamera = MatrixD.Zero;

            return MatrixD.Invert(matrix);
        }

        public override bool GetIntersectionWithLine(ref LineD line, out VRage.Game.Models.MyIntersectionResultLineTriangleEx? tri, IntersectionFlags flags = IntersectionFlags.ALL_TRIANGLES)
        {
            bool ret = GetIntersectionWithLine(ref line, ref m_hitInfoTmp, flags);
            tri = m_hitInfoTmp.Triangle;
            return ret;
        }

        /// <summary>
        /// Returns closest hit from line start position.
        /// </summary>
        public bool GetIntersectionWithLine(ref LineD line, ref MyCharacterHitInfo info, IntersectionFlags flags = IntersectionFlags.ALL_TRIANGLES)
        {
            // TODO: This now uses caspule of physics rigid body on the character, it needs to be changed to ragdoll
            //       Currently this approach will be used to support Characters with different skeleton than humanoid
            if (info == null)
                info = new MyCharacterHitInfo();

            info.Reset();

            bool capsulesReady = UpdateCapsuleBones();
            if (!capsulesReady)
                return false;

            double closestDistanceToHit = double.MaxValue;

            Vector3D hitPosition = Vector3D.Zero;
            Vector3D hitPosition2 = Vector3D.Zero;
            Vector3 hitNormal = Vector3.Zero;
            Vector3 hitNormal2 = Vector3.Zero;

            int capsuleIndex = -1;
            for (int i = 0; i < m_bodyCapsules.Length; i++)
            {
                CapsuleD capsule = m_bodyCapsules[i];
                if (capsule.Intersect(line, ref hitPosition, ref hitPosition2, ref hitNormal, ref hitNormal2))
                {
                    double distanceToHit = Vector3.Distance(hitPosition, line.From);
                    if (distanceToHit >= closestDistanceToHit)
                        continue;

                    closestDistanceToHit = distanceToHit;
                    capsuleIndex = i;
                }
            }

            if (capsuleIndex != -1)
            {
                Matrix worldMatrix = PositionComp.WorldMatrix;
                int boneIndex = FindBestBone(capsuleIndex, ref hitPosition, ref worldMatrix);

                // Transform line to model static position and compute accurate collision there
                // 1. Transform line in local coordinates (used later)
                Matrix worldMatrixInv = PositionComp.WorldMatrixNormalizedInv;
                Vector3 fromTrans = Vector3.Transform(line.From, ref worldMatrixInv);
                Vector3 toTrans = Vector3.Transform(line.To, ref worldMatrixInv);
                LineD lineLocal = new LineD(fromTrans, toTrans);

                // 2. Transform line to to bone pose in binding position
                var bone = AnimationController.CharacterBones[boneIndex];
                bone.ComputeAbsoluteTransform();
                Matrix boneAbsTrans = bone.AbsoluteTransform;
                Matrix skinTransform = bone.SkinTransform;
                Matrix boneTrans = skinTransform * boneAbsTrans;
                Matrix invBoneTrans = Matrix.Invert(boneTrans);
                fromTrans = Vector3.Transform(fromTrans, ref invBoneTrans);
                toTrans = Vector3.Transform(toTrans, ref invBoneTrans);

                // 3. Move back line to world coordinates
                LineD lineTransWorld = new LineD(Vector3.Transform(fromTrans, ref worldMatrix), Vector3.Transform(toTrans, ref worldMatrix));
                MyIntersectionResultLineTriangleEx? triangle_;
                bool success = base.GetIntersectionWithLine(ref lineTransWorld, out triangle_, flags);
                if (success)
                {
                    MyIntersectionResultLineTriangleEx triangle = triangle_.Value;

                    info.CapsuleIndex = capsuleIndex;
                    info.BoneIndex = boneIndex;
                    info.Capsule = m_bodyCapsules[info.CapsuleIndex];
                    info.HitHead = info.CapsuleIndex == 0 && m_bodyCapsules.Length > 1;
                    info.HitPositionBindingPose = triangle.IntersectionPointInObjectSpace;
                    info.HitNormalBindingPose = triangle.NormalInObjectSpace;
                    info.BindingTransformation = boneTrans;

                    // 4. Move intersection from binding to dynamic pose
                    MyTriangle_Vertices vertices = new MyTriangle_Vertices();
                    vertices.Vertex0 = Vector3.Transform(triangle.Triangle.InputTriangle.Vertex0, ref boneTrans);
                    vertices.Vertex1 = Vector3.Transform(triangle.Triangle.InputTriangle.Vertex1, ref boneTrans);
                    vertices.Vertex2 = Vector3.Transform(triangle.Triangle.InputTriangle.Vertex2, ref boneTrans);
                    Vector3 triangleNormal = Vector3.TransformNormal(triangle.Triangle.InputTriangleNormal, boneTrans);
                    MyIntersectionResultLineTriangle triraw = new MyIntersectionResultLineTriangle(triangle.Triangle.TriangleIndex, ref vertices, ref triangle.Triangle.BoneWeights, ref triangleNormal, triangle.Triangle.Distance);

                    Vector3 intersectionLocal = Vector3.Transform(triangle.IntersectionPointInObjectSpace, ref boneTrans);
                    Vector3 normalLocal = Vector3.TransformNormal(triangle.NormalInObjectSpace, boneTrans);

                    // 5. Store results
                    triangle = new MyIntersectionResultLineTriangleEx();
                    triangle.Triangle = triraw;
                    triangle.IntersectionPointInObjectSpace = intersectionLocal;
                    triangle.NormalInObjectSpace = normalLocal;
                    triangle.IntersectionPointInWorldSpace = Vector3.Transform(intersectionLocal, ref worldMatrix);
                    triangle.NormalInWorldSpace = Vector3.TransformNormal(normalLocal, worldMatrix);
                    triangle.InputLineInObjectSpace = lineLocal;

                    info.Triangle = triangle;

                    if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW)
                    {
                        MyRenderProxy.DebugClearPersistentMessages();
                        MyRenderProxy.DebugDrawCapsule(info.Capsule.P0, info.Capsule.P1, info.Capsule.Radius, Color.Aqua, false, persistent: true);

                        Vector3 p0Local = Vector3.Transform(info.Capsule.P0, ref worldMatrixInv);
                        Vector3 p1Local = Vector3.Transform(info.Capsule.P1, ref worldMatrixInv);
                        Vector3 p0LocalTrans = Vector3.Transform(p0Local, ref invBoneTrans);
                        Vector3 p1LocalTrans = Vector3.Transform(p1Local, ref invBoneTrans);
                        MyRenderProxy.DebugDrawCapsule(Vector3.Transform(p0LocalTrans, ref worldMatrix), Vector3.Transform(p1LocalTrans, ref worldMatrix), info.Capsule.Radius, Color.Brown, false, persistent: true);

                        MyRenderProxy.DebugDrawLine3D(line.From, line.To, Color.Blue, Color.Red, false, true);
                        MyRenderProxy.DebugDrawLine3D(lineTransWorld.From, lineTransWorld.To, Color.Green, Color.Yellow, false, true);
                        MyRenderProxy.DebugDrawSphere(triangle.IntersectionPointInWorldSpace, 0.02f, Color.Red, 1, false, persistent: true);
                        MyRenderProxy.DebugDrawAxis((MatrixD)boneTrans * WorldMatrix, 0.1f, false, true, true);
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Projects hit position and bone transformations on capsule axis and finds best bone
        /// </summary>
        private int FindBestBone(int capsuleIndex, ref Vector3D hitPosition, ref Matrix worldMatrix)
        {
            MyBoneCapsuleInfo info = m_bodyCapsuleInfo[capsuleIndex];
            CapsuleD capsule = m_bodyCapsules[capsuleIndex];
            MyCharacterBone ascendant = AnimationController.CharacterBones[info.AscendantBone];
            MyCharacterBone descendant = AnimationController.CharacterBones[info.DescendantBone];

            Vector3D p0_p1 = Vector3.Normalize(capsule.P0 - capsule.P1);
            double p0_p1Len = p0_p1.Length();
            Vector3D hit_p1 = hitPosition - capsule.P1;
            double hitProjectionInAxis = Vector3D.Dot(hit_p1, p0_p1) / p0_p1Len;

            int prevIndex = descendant.Index;
            double currDistance = 0;
            MyCharacterBone bone = descendant.Parent;
            while (true)
            {
                if (hitProjectionInAxis < currDistance || prevIndex == ascendant.Index)
                {
                    // Second condition is we reached last bone
                    break;
                }

                Vector3 tbone = Vector3.Transform(bone.AbsoluteTransform.Translation, ref worldMatrix);
                Vector3D tbone_p1 = tbone - capsule.P1;
                currDistance = Vector3D.Dot(tbone_p1, p0_p1) / p0_p1Len;

                prevIndex = bone.Index;
                bone = bone.Parent;
                if (bone == null)
                {
                    Debug.Assert(false, "In capsule with index " + capsuleIndex + ", bone1 must be in the same branch as bone2");
                    break;
                }
            }

            return prevIndex;
        }

        #region Input handling

        public void BeginShoot(MyShootActionEnum action)
        {
            if (m_currentMovementState == MyCharacterMovementEnum.Died) 
                return;

            if (m_currentWeapon == null)
            {
                if (action == MyShootActionEnum.SecondaryAction)
                {
                    UseTerminal();
                    return;
                }

                Use();
                m_usingByPrimary = true;
                return;
            }

            //GK: Stop previous action if any
            var curentShootingAction = GetShootingAction();
            if (curentShootingAction != null && action != curentShootingAction.Value)
            {
                EndShoot(curentShootingAction.Value);
            }

            if (!m_currentWeapon.EnabledInWorldRules)
            {
                MyHud.Notifications.Add(MyNotificationSingletons.WeaponDisabledInWorldSettings);
                return;
            }

            BeginShootSync(m_currentWeapon.DirectionToTarget(m_aimedPoint), action);
        }

        public void OnBeginShoot(MyShootActionEnum action)
        {
            if (ControllerInfo == null) return;
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

        private void ShootInternal()
        {
            MyGunStatusEnum status = MyGunStatusEnum.OK;
            MyShootActionEnum? shootingAction = GetShootingAction();

            if (ControllerInfo == null )
                return;

            if (Sync.IsServer)
            {
                m_currentAmmoCount.Value = m_currentWeapon.CurrentAmmunition;
                m_currentMagazineAmmoCount.Value = m_currentWeapon.CurrentMagazineAmmunition;
            }
            else
            {
                m_currentWeapon.CurrentAmmunition = m_currentAmmoCount;
                m_currentWeapon.CurrentMagazineAmmunition = m_currentMagazineAmmoCount;
            }

            if (shootingAction.HasValue && m_currentWeapon.CanShoot(shootingAction.Value, ControllerInfo.ControllingIdentityId, out status))
            {
                //VRageRender.MyRenderProxy.DebugDrawSphere(WeaponPosition.LogicalPositionWorld, 0.1f, Color.Red, 1, false);
                //VRageRender.MyRenderProxy.DebugDrawLine3D(WeaponPosition.LogicalPositionWorld, WeaponPosition.LogicalPositionWorld + ShootDirection, Color.Red, Color.White, false);
                m_currentWeapon.Shoot(shootingAction.Value, ShootDirection, WeaponPosition.LogicalPositionWorld);
                //if(!UseAnimationForWeapon)
                // StopUpperCharacterAnimation(0);
            }

            if (MySession.Static.ControlledEntity == this && m_currentWeapon != null)
            {
                if (status != MyGunStatusEnum.OK && status != MyGunStatusEnum.Cooldown)
                {
                    ShootFailedLocal(shootingAction.Value, status);
                }
                else if (shootingAction.HasValue && m_currentWeapon.IsShooting && status == MyGunStatusEnum.OK)
                {
                    ShootSuccessfulLocal(shootingAction.Value);
                }

                UpdateShootDirection(m_currentWeapon.DirectionToTarget(m_aimedPoint), m_currentWeapon.ShootDirectionUpdateTime);
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
            m_isShooting[(int)action] = false;
            if (MySession.Static.ControlledEntity == this)
            {
                m_currentWeapon.BeginFailReactionLocal(action, status);
            }
        }

        private void ShootSuccessfulLocal(MyShootActionEnum action)
        {
            m_currentShotTime = SHOT_TIME;

            // moved to weapons/tools
            //m_currentCameraShakePower = Math.Max(m_currentCameraShakePower, MyUtils.GetRandomFloat(1.5f, m_currentWeapon.ShakeAmount));

            WeaponPosition.AddBackkick(m_currentWeapon.BackkickForcePerSecond * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS);

            var jetpack = JetpackComp;
            if (m_currentWeapon.BackkickForcePerSecond > 0 && (JetpackRunning || m_isFalling))
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
                if (IsShooting(action))
                    EndShoot(action);
            }
        }

        public void EndShoot(MyShootActionEnum action)
        {
            if (m_currentMovementState != MyCharacterMovementEnum.Died && m_currentWeapon != null)
            {
                if (MyGuiScreenGamePlay.DoubleClickDetected != null && MyGuiScreenGamePlay.DoubleClickDetected[(int)action] && (m_currentWeapon is MyAngleGrinder || m_currentWeapon is MyHandDrill || m_currentWeapon is MyWelder))
                {
                    //GK: When double click is detected keep shooting. Do not trigger Multiplayer EndShoot event for character but only for specific tools.
                }
                else
                {
                    EndShootSync(action);
                }
            }

            if (m_usingByPrimary)
            {
                m_usingByPrimary = false;
                UseFinished();
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
                        if (m_currentWeapon != null && (MySession.Static.CameraController == this || !ControllerInfo.IsLocallyControlled()))
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
                        if (MySession.Static.CameraController == this || !ControllerInfo.IsLocallyControlled())
                        {
                            //MyAudio.Static.PlayCue(MySoundCuesEnum.ArcPlayIronSightDeactivate, m_secondarySoundEmitter, Common.ObjectBuilders.Audio.MyAudioHelpers.Dimensions.D3);
                            //MyAudio.Static.PlayCue(MySoundCuesEnum.ArcPlayIronSightDeactivate);
                            SoundComp.PlaySecondarySound(CharacterSoundsEnum.IRONSIGHT_DEACT_SOUND, true);
                            EnableIronsight(false, newKeyPress, true);
                        }
                    }
                    break;
            }
        }

        public void EnableIronsight(bool enable, bool newKeyPress, bool changeCamera, bool hideCrosshairWhenAiming = true)
        {
            MyMultiplayer.RaiseEvent(this, x => x.EnableIronsightCallback, enable, newKeyPress, changeCamera, hideCrosshairWhenAiming);
            if (!Sync.IsServer)
                EnableIronsightCallback(enable, newKeyPress, changeCamera, hideCrosshairWhenAiming);
        }

        [Event, Reliable, Server, BroadcastExcept]
        public void EnableIronsightCallback(bool enable, bool newKeyPress, bool changeCamera, bool hideCrosshairWhenAiming = true)
        {
            if (enable)
            {
                if (m_currentWeapon != null && /*m_currentWeapon.Zoom(newKeyPress) &&*/ m_zoomMode != MyZoomModeEnum.IronSight)
                {
                    m_zoomMode = MyZoomModeEnum.IronSight;

                    if (changeCamera && MyEventContext.Current.IsLocallyInvoked)
                    {
                        m_storedCameraSettings.Controller = MySession.Static.GetCameraControllerEnum();
                        m_storedCameraSettings.Distance = MySession.Static.GetCameraTargetDistance();

                        float backupRotationX = m_headLocalXAngle;
                        float backupRotationY = m_headLocalYAngle;
                        MySession.Static.SetCameraController(MyCameraControllerEnum.Entity, this);
                        m_headLocalXAngle = backupRotationX;
                        m_headLocalYAngle = backupRotationY;

                        if (hideCrosshairWhenAiming)
                            MyHud.Crosshair.HideDefaultSprite();

                        MySector.MainCamera.Zoom.SetZoom(MyCameraZoomOperationType.ZoomingIn);
                    }
                }
            }
            else
            {
                m_zoomMode = MyZoomModeEnum.Classic;

                ForceFirstPersonCamera = false;

                if (changeCamera && MyEventContext.Current.IsLocallyInvoked)
                {
                    MyHud.Crosshair.ResetToDefault();
                    MySector.MainCamera.Zoom.SetZoom(MyCameraZoomOperationType.ZoomingOut);

                    float backupRotationX = m_headLocalXAngle;
                    float backupRotationY = m_headLocalYAngle;
                    MySession.Static.SetCameraController(m_storedCameraSettings.Controller, this);
                    MySession.Static.SetCameraTargetDistance(m_storedCameraSettings.Distance);
                    m_headLocalXAngle = backupRotationX;
                    m_headLocalYAngle = backupRotationY;
                }
            }
        }

        public static IMyHandheldGunObject<MyDeviceBase> CreateGun(MyObjectBuilder_EntityBase gunEntity, uint? inventoryItemId = null)
        {
            if (gunEntity != null)
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
                var gun = (IMyHandheldGunObject<MyDeviceBase>)entity;

                if (gun != null && gun.GunBase != null && !gun.GunBase.InventoryItemId.HasValue && inventoryItemId.HasValue)
                {
                    gun.GunBase.InventoryItemId = inventoryItemId.Value;
                }

                return gun;
            }

            return null;
        }

        /// <summary>
        /// This method finds the given weapon in the character's inventory. The weapon type has to be supplied
        /// either as PhysicalGunObject od weapon entity (e.g. Welder, CubePlacer, etc...).
        /// </summary>
        public MyPhysicalInventoryItem? FindWeaponItemByDefinition(MyDefinitionId weaponDefinition)
        {
            MyPhysicalInventoryItem? item = null;
            var itemId = MyDefinitionManager.Static.ItemIdFromWeaponId(weaponDefinition);
            if (itemId.HasValue && this.GetInventory() != null)
            {
                item = this.GetInventory().FindUsableItem(itemId.Value);
            }
            return item;
        }

        private void SaveAmmoToWeapon()
        {
            /*var weaponEntity = m_currentWeapon as MyEntity;
            // save weapon ammo amount in builder
            if (m_currentWeapon.PhysicalObject != null)
            {
                var inventory = this.GetInventory();
                if (inventory != null)
                {
                    var item = FindWeaponItemByDefinition(m_currentWeapon.PhysicalObject.GetId());
                    if (item.HasValue && (item.Value.Content is MyObjectBuilder_PhysicalGunObject))
                    {
                        (item.Value.Content as MyObjectBuilder_PhysicalGunObject).GunEntity = weaponEntity.GetObjectBuilder();
                    }
                }
            }*/
        }

        public bool CanSwitchToWeapon(MyDefinitionId? weaponDefinition)
        {
            if (!WeaponTakesBuilderFromInventory(weaponDefinition)) return true;
            var item = FindWeaponItemByDefinition(weaponDefinition.Value);
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

        private void SwitchAmmoMagazineInternal(bool sync)
        {
            if (sync)
            {
                MyMultiplayer.RaiseEvent(this, x => x.OnSwitchAmmoMagazineRequest);
                return;
            }

            if (!IsDead && CurrentWeapon != null)
            {
                CurrentWeapon.GunBase.SwitchAmmoMagazineToNextAvailable();
            }
        }

        private void SwitchAmmoMagazineSuccess()
        {
            SwitchAmmoMagazineInternal(false);
        }

        public void SwitchToWeapon(MyDefinitionId? weaponDefinition, bool sync = true)
        {
            if (weaponDefinition.HasValue && m_rightHandItemBone == -1)
                return;

            if (WeaponTakesBuilderFromInventory(weaponDefinition))
            {
                var item = FindWeaponItemByDefinition(weaponDefinition.Value);
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

                SwitchToWeaponInternal(weaponDefinition, sync, item.Value.ItemId, 0);
            }
            else
            {
                SwitchToWeaponInternal(weaponDefinition, sync, null, 0);
            }
        }

        public void SwitchToWeapon(MyToolbarItemWeapon weapon)
        {
            SwitchToWeapon(weapon, null);
        }

        public void SwitchToWeapon(MyToolbarItemWeapon weapon, uint? inventoryItemId)
        {
            MyDefinitionId? weaponDefinition = null;
            if (weapon != null)
                weaponDefinition = weapon.Definition.Id;
            // CH:TODO: This part of code seems to do nothing
            if (weaponDefinition.HasValue && m_rightHandItemBone == -1)
                return;

            if (WeaponTakesBuilderFromInventory(weaponDefinition))
            {
                MyPhysicalInventoryItem? item = null;
                if (inventoryItemId.HasValue)
                {
                    item = this.GetInventory().GetItemByID(inventoryItemId.Value);
                }
                else
                {
                    item = FindWeaponItemByDefinition(weaponDefinition.Value);
                }
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

                SwitchToWeaponInternal(weaponDefinition, true, item.Value.ItemId, 0);
            }
            else
            {
                SwitchToWeaponInternal(weaponDefinition, true, null, 0);
            }
        }

        private void SwitchToWeaponInternal(MyDefinitionId? weaponDefinition, bool updateSync, uint? inventoryItemId, long weaponEntityId)
        {
            if (updateSync)
            {
                Debug.Assert(weaponEntityId == 0);
                //Because while waiting for answer we dont want to shoot from old weapon
                UnequipWeapon();

                RequestSwitchToWeapon(weaponDefinition, inventoryItemId);
                return;
            }

            UnequipWeapon();

            StopCurrentWeaponShooting();

            MyObjectBuilder_EntityBase weaponEntityBuilder = GetObjectBuilderForWeapon(weaponDefinition, ref inventoryItemId, weaponEntityId);
            var gun = CreateGun(weaponEntityBuilder, inventoryItemId);

            EquipWeapon(gun);

            UpdateShadowIgnoredObjects();
        }

        private MyObjectBuilder_EntityBase GetObjectBuilderForWeapon(MyDefinitionId? weaponDefinition, ref uint? inventoryItemId, long weaponEntityId)
        {
            MyObjectBuilder_EntityBase weaponEntityBuilder = null;
            // On the server or your local client character, you have the inventory correct, so create the item from there
            if (inventoryItemId.HasValue && (Sync.IsServer || this.ControllerInfo.IsLocallyControlled()))
            {
                var inventory = this.GetInventory();
                var item = inventory.GetItemByID(inventoryItemId.Value);
                if (item.HasValue)
                {
                    var physicalGunObject = item.Value.Content as MyObjectBuilder_PhysicalGunObject;
                    if (physicalGunObject != null)
                    {
                        weaponEntityBuilder = physicalGunObject.GunEntity;
                    }

                    if (weaponEntityBuilder == null)
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
                        weaponEntityBuilder.EntityId = weaponEntityId;
                    }

                    if (physicalGunObject != null)
                    {
                        physicalGunObject.GunEntity = weaponEntityBuilder;
                    }
                }
            }
            else
            {
                // Don't check inventory only if you're a client looking at someone else or if the weapon does not require it
                bool dontCheckInventory = (!Sync.IsServer && this.ControllerInfo.IsRemotelyControlled()) || !WeaponTakesBuilderFromInventory(weaponDefinition);
                if (weaponDefinition == null)
                {
                    EquipWeapon(null);
                }
                else if (dontCheckInventory && weaponDefinition.Value.TypeId == typeof(MyObjectBuilder_PhysicalGunObject))
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
                    weaponEntityBuilder = MyObjectBuilderSerializer.CreateNewObject(weaponDefinition.Value.TypeId) as MyObjectBuilder_EntityBase;
                    if (weaponEntityBuilder != null)
                    {
                        weaponEntityBuilder.EntityId = weaponEntityId;
                        if (WeaponTakesBuilderFromInventory(weaponDefinition))
                        {
                            var item = FindWeaponItemByDefinition(weaponDefinition.Value);
                            if (item.HasValue)
                            {
                                var physicalGunBuilder = item.Value.Content as MyObjectBuilder_PhysicalGunObject;
                                if (physicalGunBuilder != null)
                                    physicalGunBuilder.GunEntity = weaponEntityBuilder;
                                inventoryItemId = item.Value.ItemId;
                            }
                        }
                    }
                    else
                    {
                        Debug.Fail("Couldn't create builder for weapon! typeID: " + weaponDefinition.Value.TypeId.ToString());
                    }
                }
            }

            if (weaponEntityBuilder != null)
            {
                var deviceBuilder = weaponEntityBuilder as IMyObjectBuilder_GunObject<MyObjectBuilder_DeviceBase>;
                if (deviceBuilder != null)
                {
                    if (deviceBuilder.DeviceBase != null)
                    {
                        deviceBuilder.DeviceBase.InventoryItemId = inventoryItemId;
                    }
                }
            }

            return weaponEntityBuilder;
        }

        private void StopCurrentWeaponShooting()
        {
            if (m_currentWeapon != null)
            {
                foreach (MyShootActionEnum action in MyEnum<MyShootActionEnum>.Values)
                {
                    if (IsShooting(action))
                    {
                        m_currentWeapon.EndShoot(action);
                    }
                }
            }
        }

        private void UpdateShadowIgnoredObjects()
        {
            Render.UpdateShadowIgnoredObjects();
            if (m_currentWeapon != null)
                UpdateShadowIgnoredObjects((MyEntity)m_currentWeapon);
            if (m_leftHandItem != null)
                UpdateShadowIgnoredObjects(m_leftHandItem);
        }

        private void UpdateShadowIgnoredObjects(IMyEntity parent)
        {
            Render.UpdateShadowIgnoredObjects(parent);
            foreach (var child in parent.Hierarchy.Children)
            {
                UpdateShadowIgnoredObjects(child.Container.Entity);
            }
        }

        public void Use()
        {
            if (!IsDead)
            {
                MyCharacterDetectorComponent detectorComponent = Components.Get<MyCharacterDetectorComponent>();

                if (detectorComponent != null && detectorComponent.UseObject != null)
                {
                    if (detectorComponent.UseObject.IsActionSupported(UseActionEnum.Manipulate))
                    {
                        if (detectorComponent.UseObject.PlayIndicatorSound)
                        {
                            MyGuiAudio.PlaySound(MyGuiSounds.HudUse);
                            SoundComp.StopStateSound(true);
                        }
                        detectorComponent.UseObject.Use(UseActionEnum.Manipulate, this);
                    }
                    else if (detectorComponent.UseObject.IsActionSupported(UseActionEnum.OpenTerminal))
                    {
                        if (detectorComponent.UseObject.PlayIndicatorSound)
                        {
                            MyGuiAudio.PlaySound(MyGuiSounds.HudUse);
                            SoundComp.StopStateSound(true);
                        }

                        //by Gregory: the parameter that should be passed should be the use key now with flags there can be confusion
                        //e.g. case for MyUseObjectInventory where it has 2 flags and from use key it should use OpenInventory not OpenTerminal TODO

                        detectorComponent.UseObject.Use(UseActionEnum.OpenTerminal, this);
                    }
                    else if (detectorComponent.UseObject.IsActionSupported(UseActionEnum.OpenInventory))
                    {
                        if (detectorComponent.UseObject.PlayIndicatorSound)
                        {
                            MyGuiAudio.PlaySound(MyGuiSounds.HudUse);
                            SoundComp.StopStateSound(true);
                        }
                        detectorComponent.UseObject.Use(UseActionEnum.OpenInventory, this);
                    }
                }
                else
                {
                    var detectedEntity = detectorComponent.DetectedEntity as MyEntity;
                    if (detectedEntity != null && (!(detectedEntity is MyCharacter) || (detectedEntity as MyCharacter).IsDead))
                    {
                        MyInventoryBase interactedInventory = null;
                        if (detectedEntity.TryGetInventory(out interactedInventory))
                        {
                            ShowAggregateInventoryScreen(interactedInventory);
                        }
                    }
                }
            }
        }

        public void UseContinues()
        {
            if (!IsDead)
            {
                MyCharacterDetectorComponent detectorComponent = Components.Get<MyCharacterDetectorComponent>();

                if (detectorComponent != null && detectorComponent.UseObject != null && detectorComponent.UseObject.IsActionSupported(UseActionEnum.Manipulate) && detectorComponent.UseObject.ContinuousUsage)
                {
                    detectorComponent.UseObject.Use(UseActionEnum.Manipulate, this);
                }
            }
        }

        public void UseTerminal()
        {
            if (!IsDead)
            {
                MyCharacterDetectorComponent detectorComponent = Components.Get<MyCharacterDetectorComponent>();

                if (detectorComponent.UseObject != null && detectorComponent.UseObject.IsActionSupported(UseActionEnum.OpenTerminal))
                {
                    detectorComponent.UseObject.Use(UseActionEnum.OpenTerminal, this);
                    detectorComponent.UseContinues();
                }
            }
        }

        public void UseFinished()
        {
            if (!IsDead)
            {
                MyCharacterDetectorComponent detectorComponent = Components.Get<MyCharacterDetectorComponent>();

                if (detectorComponent.UseObject != null && detectorComponent.UseObject.IsActionSupported(UseActionEnum.UseFinished))
                {
                    detectorComponent.UseObject.Use(UseActionEnum.UseFinished, this);
                }
            }
        }

        public void PickUp()
        {
            if (IsDead) return;
            var pickupComponent = Components.Get<MyCharacterPickupComponent>();
            if (pickupComponent == null) return;
            pickupComponent.PickUp();
        }

        public void PickUpContinues()
        {
            if (IsDead) return;
            var pickupComponent = Components.Get<MyCharacterPickupComponent>();
            if (pickupComponent == null) return;
            pickupComponent.PickUpContinues();
        }

        public void PickUpFinished()
        {
            if (IsDead) return;
            var pickupComponent = Components.Get<MyCharacterPickupComponent>();
            if (pickupComponent == null) return;
            pickupComponent.PickUpFinished();
        }

        private bool HasEnoughSpaceToStandUp()
        {
            if (IsCrouching == false)
            {
                return true;
            }

            Vector3D characterHeadPosition = WorldMatrix.Translation + Definition.CharacterCollisionCrouchHeight * WorldMatrix.Up;
            float heightDifference = Definition.CharacterCollisionHeight - Definition.CharacterCollisionCrouchHeight;

            Sandbox.Engine.Physics.MyPhysics.HitInfo? hit = MyPhysics.CastRay(characterHeadPosition, characterHeadPosition + heightDifference * WorldMatrix.Up, MyPhysics.CollisionLayers.CharacterCollisionLayer);
            if (hit.HasValue)
            {
                return false;
            }

            return true;
        }

        public void Crouch()
        {
            if (IsDead)
                return;

            if (!JetpackRunning && !m_isFalling)
            {
                if (HasEnoughSpaceToStandUp())
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

        public void Sprint(bool enabled)
        {
            if (WantsSprint != enabled)
                WantsSprint = enabled;
        }

        public void SwitchWalk()
        {
            WantsWalk = !WantsWalk;
        }

        [Event, Reliable, Server]
        public void Jump()
        {
            // Check if not dead.
            if (m_currentMovementState == MyCharacterMovementEnum.Died)
                return;

            if (HasEnoughSpaceToStandUp() == false)
            {
                return;
            }
            // Check if can jump. (ex. enough  stamina)
            MyTuple<ushort, MyStringHash> message;
            if (StatComp != null && !StatComp.CanDoAction("Jump", out message, m_currentMovementState == MyCharacterMovementEnum.Jump))
            {
                if (MySession.Static != null && MySession.Static.LocalCharacter == this && message.Item1 == MyStatLogic.STAT_VALUE_TOO_LOW && message.Item2.String.CompareTo("Stamina") == 0)
                {
                    if (m_notEnoughStatNotification != null)
                    {
                        m_notEnoughStatNotification.SetTextFormatArguments(message.Item2);
                        MyHud.Notifications.Add(m_notEnoughStatNotification);
                    }
                }
                return;
            }

            WantsJump = true;
        }

        public void ShowInventory()
        {
            if (m_currentMovementState != MyCharacterMovementEnum.Died)
            {
                if (m_currentMovementState != MyCharacterMovementEnum.Died)
                {
                    MyCharacterDetectorComponent detectorComponent = Components.Get<MyCharacterDetectorComponent>();

                    //Then for each case and game check below
                    if (detectorComponent.UseObject != null && detectorComponent.UseObject.IsActionSupported(UseActionEnum.OpenInventory))
                        detectorComponent.UseObject.Use(UseActionEnum.OpenInventory, this);
                    else if (MyPerGameSettings.TerminalEnabled)
                        MyGuiScreenTerminal.Show(MyTerminalPageEnum.Inventory, this, null);
                    else if (HasInventory)
                    {
                        var inventory = this.GetInventory();
                        if (inventory != null)
                            ShowAggregateInventoryScreen(this.GetInventory());
                    }
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

                //by Gregory: ok this button on show terminal is used by many things... First check if Voxel Hand is enabled.
                //If yes then MySessionComponentVoxelHand will handle unhadled input
                if (MyToolbarComponent.CharacterToolbar != null && MyToolbarComponent.CharacterToolbar.SelectedItem is MyToolbarItemVoxelHand)
                {
                    return;
                }

                //Then for each case and game check below
                if (detectorComponent.UseObject != null && detectorComponent.UseObject.IsActionSupported(UseActionEnum.OpenTerminal))
                    detectorComponent.UseObject.Use(UseActionEnum.OpenTerminal, this);
                else if (MyPerGameSettings.TerminalEnabled)
                    MyGuiScreenTerminal.Show(MyTerminalPageEnum.Inventory, this, null);
                else if (MyFakes.ENABLE_QUICK_WARDROBE)
                {
                    MyGuiSandbox.AddScreen(MyGuiScreenGamePlay.ActiveGameplayScreen = new MyGuiScreenWardrobe(this));
                }
                else if (MyPerGameSettings.GUI.GameplayOptionsScreen != null)
                {
                    //if (!MySession.Static.SurvivalMode || (MyMultiplayer.Static != null && MyMultiplayer.Static.IsAdmin(ControllerInfo.Controller.Player.Id.SteamId)))
                    if (!MySession.Static.SurvivalMode)
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
                EnableBroadcasting(!RadioBroadcaster.WantsToBeEnabled);

                m_broadcastingNotification.Text = (RadioBroadcaster.Enabled ? MySpaceTexts.NotificationCharacterBroadcastingOn : MySpaceTexts.NotificationCharacterBroadcastingOff);
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

        #endregion Input handling

        #region Sensor

        public void RemoveNotification(ref MyHudNotification notification)
        {
            if (notification != null)
            {
                MyHud.Notifications.Remove(notification);
                notification = null;
            }
        }

        private void RemoveNotifications()
        {
            RemoveNotification(ref m_pickupObjectNotification);
            RemoveNotification(ref m_respawnNotification);
        }

        private void OnControlAcquired(MyEntityController controller)
        {
            if (controller.Player.IsLocalPlayer)
            {
                bool isHuman = controller.Player == MySession.Static.LocalHumanPlayer;
                if (isHuman)
                {

                    MyHud.HideAll();
                    MyHud.Crosshair.ResetToDefault();
                    MyHud.Crosshair.Recenter();

                    if (MyGuiScreenGamePlay.Static != null)
                        MySession.Static.CameraAttachedToChanged += Static_CameraAttachedToChanged;

                    if (MySession.Static.CameraController is MyEntity)
                        MySession.Static.SetCameraController(IsInFirstPersonView ? MyCameraControllerEnum.Entity : MyCameraControllerEnum.ThirdPersonSpectator, this);

                    m_currentCameraShakePower = 0;

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
                if (jetpack != null)
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
            if (this == MySession.Static.ControlledEntity && MyToolbarComponent.CharacterToolbar != null)
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

            if (MySession.Static.LocalHumanPlayer == null) return m_hudParams;

            m_hudParams.Add(new MyHudEntityParams()
            {
                FlagsEnum = MyHudIndicatorFlagsEnum.SHOW_TEXT,
                Text = CustomNameWithFaction,
                ShouldDraw = MyHud.CheckShowPlayerNamesOnHud,
                MustBeDirectlyVisible = false,
                TargetMode = GetRelationTo(MySession.Static.LocalHumanPlayer.Identity.IdentityId),
                Entity = this
            });
            return m_hudParams;
        }

        private void OnControlReleased(MyEntityController controller)
        {
            Static_CameraAttachedToChanged(null, null);
            m_oldController = controller;

            if (MySession.Static.LocalHumanPlayer == controller.Player)
            {

                MyHud.SelectedObjectHighlight.RemoveHighlight();

                RemoveNotifications();

                if (MyGuiScreenGamePlay.Static != null)
                    MySession.Static.CameraAttachedToChanged -= Static_CameraAttachedToChanged;

                m_currentCameraShakePower = 0;

                MyHud.GravityIndicator.Hide();
                MyHud.CharacterInfo.Hide();
                m_suitBattery.OwnedByLocalPlayer = false;
                MyHud.LargeTurretTargets.Visible = false;
                MyHud.OreMarkers.Visible = false;
                RadioReceiver.Clear();
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

        private void Static_CameraAttachedToChanged(IMyCameraController oldController, IMyCameraController newController)
        {
            if (oldController != newController && MySession.Static.ControlledEntity == this && newController != this)
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
            //if (Parent is MyCockpit)
            //{
            //    var cockpit = Parent as MyCockpit;
            //    if (cockpit.Pilot == this)
            //    {
            //        MySession.Static.SetCameraController(MyCameraControllerEnum.Entity, cockpit);
            //    }

            //    return;
            //}
        }

        public void OnReleaseControl(IMyCameraController newCameraController)
        {
            //IsInFirstPersonView = false; //AB: Why this?
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

            if (RadioBroadcaster != null)
                RadioBroadcaster.MoveBroadcaster();

            Render.UpdateLightPosition();
        }

        private void OnCharacterStateChanged(HkCharacterStateType newState)
        {
            if (m_currentMovementState != MyCharacterMovementEnum.Died)
            {
                if (!JetpackRunning)
                {
                    if (m_currentJumpTime <= 0 && (newState == HkCharacterStateType.HK_CHARACTER_IN_AIR) || ((int)newState == MyCharacter.HK_CHARACTER_FLYING))
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

                // Reset jump time in flying mode
                if (JetpackRunning)
                {
                    m_currentJumpTime = 0.0f;
                }
            }

            m_currentCharacterState = newState;

            //MyTrace.Watch("CharacterState", newState.ToString());
        }

        internal void StartFalling()
        {
            bool canFly = JetpackRunning;
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
            if (m_isFalling && (jetpack == null || !(jetpack.TurnedOn && jetpack.IsPowered)))
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

        #endregion Sensor

        #region Inventory

        public bool CanStartConstruction(MyCubeBlockDefinition blockDefinition)
        {
            if (blockDefinition == null) return false;

            Debug.Assert(this.GetInventory() != null, "Inventory is null!");
            Debug.Assert(blockDefinition.Components.Length != 0, "Missing components!");

            var inventory = MyCubeBuilder.BuildComponent.GetBuilderInventory(this);
            if (inventory == null)
                return false;

            return (inventory.GetItemAmount(blockDefinition.Components[0].Definition.Id) >= 1);
        }

        public bool CanStartConstruction(Dictionary<MyDefinitionId, int> constructionCost)
        {
            Debug.Assert(this.GetInventory() != null, "Inventory is null!");
            var inventory = MyCubeBuilder.BuildComponent.GetBuilderInventory(this);
            foreach (var entry in constructionCost)
            {
                if (inventory.GetItemAmount(entry.Key) < entry.Value) return false;
            }
            return true;
        }

        #endregion Inventory

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

        [Event, Reliable, Server, BroadcastExcept]
        public void UnequipWeapon()
        {
            if (m_leftHandItem != null && m_leftHandItem is IMyHandheldGunObject<MyDeviceBase>)
            {
                (m_leftHandItem as IMyHandheldGunObject<MyDeviceBase>).OnControlReleased();
                m_leftHandItem.Close(); // no dual wielding now
                m_leftHandItem = null;

                TriggerCharacterAnimationEvent("unequip_left_tool", true);
            }

            if (m_currentWeapon != null)
            {
                //GK: When switching weapon EndShoot unless player is already pressing to shoot
                if (!MyInput.Static.IsGameControlPressed(MyControlsSpace.PRIMARY_TOOL_ACTION) && !MyInput.Static.IsGameControlPressed(MyControlsSpace.SECONDARY_TOOL_ACTION))
                {
                    EndShootAll();
                }
                else if (Sync.IsServer)
                {
                    // prevent player continuing to fire from the unequiped weapon, but keep the shooting flags in character
                    foreach (MyShootActionEnum action in MyEnum<MyShootActionEnum>.Values)
                        if (IsShooting(action))
                            m_currentWeapon.EndShoot(action);
                }

                var weaponEntity = m_currentWeapon as MyEntity;
                if (UseNewAnimationSystem && m_handItemDefinition != null)
                {
                    if (!string.IsNullOrEmpty(m_handItemDefinition.Id.SubtypeName))
                    {
                        AnimationController.Variables.SetValue(MyStringId.GetOrCompute(m_handItemDefinition.Id.TypeId.ToString().ToLower()), 0);
                    }
                }

                SaveAmmoToWeapon();

                m_currentWeapon.OnControlReleased();
                if (m_zoomMode == MyZoomModeEnum.IronSight && MySession.Static.CameraController == this)
                {
                    bool backupIsInFirstPerson = IsInFirstPersonView;
                    EnableIronsight(false, true, changeCamera: true);
                    IsInFirstPersonView = backupIsInFirstPerson;
                }

                var weaponSink = weaponEntity.Components.Get<MyResourceSinkComponent>();
                if (weaponSink != null)
                    SuitRechargeDistributor.RemoveSink(weaponSink);

                weaponEntity.OnClose -= gunEntity_OnClose;

                MyEntities.Remove(weaponEntity);

                weaponEntity.Close();
                //var useAnimationInsteadOfIK = MyPerGameSettings.CheckUseAnimationInsteadOfIK(m_currentWeapon);

                m_currentWeapon = null;

                if (ControllerInfo.IsLocallyHumanControlled() && MySector.MainCamera != null)
                {
                    MySector.MainCamera.Zoom.ResetZoom();
                }

                if (UseNewAnimationSystem)
                {
                    TriggerCharacterAnimationEvent("unequip_left_tool", true);
                    TriggerCharacterAnimationEvent("unequip_right_tool", true);
                }
                else
                {
                    StopUpperAnimation(0.2f);
                    SwitchAnimation(m_currentMovementState, false);
                }

                MyAnalyticsHelper.ReportActivityEnd(this, "item_equip");
            }

            if (m_currentShotTime <= 0)
            {
                //Otherwise all upper players keep updating
                StopUpperAnimation(0);
                StopFingersAnimation(0);
            }

            //MyHud.Crosshair.Hide();
            m_currentWeapon = null;
            StopFingersAnimation(0);
        }

        private void EquipWeapon(IMyHandheldGunObject<MyDeviceBase> newWeapon, bool showNotification = false)
        {
            //  Debug.Assert(newWeapon != null);
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
                if (UseNewAnimationSystem)
                {
                    TriggerCharacterAnimationEvent("equip_left_tool", true);
                    TriggerCharacterAnimationEvent("equip_right_tool", true);
                    TriggerCharacterAnimationEvent(m_handItemDefinition.Id.SubtypeName.ToLower(), true);
                    TriggerCharacterAnimationEvent(m_handItemDefinition.FingersAnimation.ToLower(), true);

                    if (!string.IsNullOrEmpty(m_handItemDefinition.Id.SubtypeName))
                    {
                        AnimationController.Variables.SetValue(MyStringId.GetOrCompute(m_handItemDefinition.Id.TypeId.ToString().ToLower()), 1);
                    }
                }

                if (!def.LeftHandItem.TypeId.IsNull)
                {
                    if (m_leftHandItem != null)
                    {
                        (m_leftHandItem as IMyHandheldGunObject<Sandbox.Game.Weapons.MyDeviceBase>).OnControlReleased();
                        m_leftHandItem.Close();
                    }

                    // CH: TODO: The entity id is not synced, but it never was in this place. It should be fixed later
                    long handItemId = MyEntityIdentifier.AllocateId();
                    uint? inventoryItemId = null;
                    var builder = GetObjectBuilderForWeapon(def.LeftHandItem, ref inventoryItemId, handItemId);
                    var leftHandItem = CreateGun(builder, inventoryItemId);

                    if (leftHandItem != null)
                    {
                        m_leftHandItem = leftHandItem as MyEntity;
                        leftHandItem.OnControlAcquired(this);
                        UpdateLeftHandItemPosition();

                        MyEntities.Add(m_leftHandItem);
                    }
                }
            }
            else if (m_handItemDefinition != null)
            {
                if (UseNewAnimationSystem)
                {
                    TriggerCharacterAnimationEvent("equip_left_tool", true);
                    TriggerCharacterAnimationEvent("equip_right_tool", true);
                    TriggerCharacterAnimationEvent(m_handItemDefinition.Id.SubtypeName.ToLower(), true);
                }
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
            if (!(IsUsing is MyCockpit))
                MyHud.Crosshair.ResetToDefault(clear: false);
        }

        private void gunEntity_OnClose(MyEntity obj)
        {
            if (m_currentWeapon == obj)
                m_currentWeapon = null;
        }

        public float InteractiveDistance
        {
            get { return MyConstants.DEFAULT_INTERACTIVE_DISTANCE; }
        }

        #endregion Interactive

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
            if (OxygenComponent != null && OxygenComponent.NeedsOxygenFromSuit)
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

        public void EnableLights(bool enable)
        {
            MyMultiplayer.RaiseEvent(this, x => x.EnableLightsCallback, enable);
            if (!Sync.IsServer)
                EnableLightsCallback(enable);
        }

        [Event, Reliable, Server, BroadcastExcept]
        private void EnableLightsCallback(bool enable)
        {
            if (m_lightEnabled != enable)
            {
                m_lightEnabled = enable;

                RecalculatePowerRequirement();
                Render.UpdateLightPosition();
            }
        }

        public void EnableBroadcasting(bool enable)
        {
            MyMultiplayer.RaiseEvent(this, x => x.EnableBroadcastingCallback, enable);
            if (!Sync.IsServer)
                EnableBroadcastingCallback(enable);
        }

        [Event, Reliable, Server, BroadcastExcept]
        public void EnableBroadcastingCallback(bool enable)
        {
            if (RadioBroadcaster.WantsToBeEnabled != enable)
            {
                RadioBroadcaster.WantsToBeEnabled = enable;
                RadioBroadcaster.Enabled = enable;
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
            get
            {
                var movementState = GetCurrentMovementState();   // MZ hotfixing falling and flying at the same time
                return m_isFalling && movementState != MyCharacterMovementEnum.Flying;
        }
        }

        public bool IsJumping
        {
            get { return m_currentMovementState == MyCharacterMovementEnum.Jump; }
        }

        public bool IsMagneticBootsEnabled
        {
            get
            {
                return !IsFalling && Physics != null && Physics.CharacterProxy != null && Physics.CharacterProxy.Gravity.LengthSquared() < 0.001f && !JetpackRunning;
            }
        }

        public void Sit(bool enableFirstPerson, bool playerIsPilot, bool enableBag, string animation)
        {
            EndShootAll();

            SwitchToWeaponInternal(weaponDefinition: null, updateSync: false, inventoryItemId: null, weaponEntityId: 0);

            Render.NearFlag = enableFirstPerson && playerIsPilot;
            m_isFalling = false;

            PlayCharacterAnimation(animation, MyBlendOption.Immediate, MyFrameOption.Loop, 0);

            StopUpperCharacterAnimation(0);
            StopFingersAnimation(0);

            SetHandAdditionalRotation(Quaternion.CreateFromAxisAngle(Vector3.Forward, MathHelper.ToRadians(0)));
            SetUpperHandAdditionalRotation(Quaternion.CreateFromAxisAngle(Vector3.Forward, MathHelper.ToRadians(0)));
            if (UseNewAnimationSystem)
            {
                AnimationController.Variables.SetValue(MyAnimationVariableStorageHints.StrIdLean, 0);
            }
            SetSpineAdditionalRotation(Quaternion.CreateFromAxisAngle(Vector3.Forward, 0), Quaternion.CreateFromAxisAngle(Vector3.Forward, 0));
            SetHeadAdditionalRotation(Quaternion.Identity, false);

            FlushAnimationQueue();

            UpdateAnimation(0);

            // SuitBattery.ResourceSource.Enabled = false;
            SinkComp.Update();
            UpdateLightPower(true);

            EnableBag(enableBag);
            //EnableHead(true);
            m_bootsState = MyBootsState.Init;

            SetCurrentMovementState(MyCharacterMovementEnum.Sitting);
            if (UseNewAnimationSystem)
            {
                TriggerCharacterAnimationEvent("sit", false);
                TriggerCharacterAnimationEvent(animation.ToLower(), false);
            }

            //Because of legs visible first frame after sitting
            if (!MySandboxGame.IsDedicated)
            {
                if (UseNewAnimationSystem)
                {
                    AnimationController.Variables.SetValue(MyAnimationVariableStorageHints.StrIdSitting, 1);
                    AnimationController.Update(); // manually update to get there result that is up to date
                    UpdateAnimation(0);
                }
                Render.Draw();
            }
        }

        private void EnableBag(bool enabled)
        {
            m_enableBag = enabled;
            if (InScene)
            {
                VRageRender.MyRenderProxy.UpdateModelProperties(
                    Render.RenderObjectIDs[0],
                    0,
                    -1,
                    "Bag",
                    enabled,
                    null,
                    null);
            }
        }

        public void EnableHead(bool enabled)
        {
            if (InScene && m_characterDefinition.MaterialsDisabledIn1st != null && m_headRenderingEnabled != enabled)
            {
                m_headRenderingEnabled = enabled;
                foreach (var material in m_characterDefinition.MaterialsDisabledIn1st)
                {
                    VRageRender.MyRenderProxy.UpdateModelProperties(
                        Render.RenderObjectIDs[0],
                        0,
                        -1,
                        material,
                        enabled,
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

            SetCurrentMovementState(MyCharacterMovementEnum.Standing);
            m_wasInFirstPerson = false;
            IsUsing = null;
            //NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public void ForceUpdateBreath()
        {
            if (m_breath != null)
                m_breath.ForceUpdate();
        }

        public long GetPlayerIdentityId()
        {
            MyPlayer localPlayer = MyPlayer.GetPlayerFromCharacter(this);
            if (localPlayer != null)
                return localPlayer.Identity.IdentityId;
            if (this == MySession.Static.LocalCharacter)
                return MySession.Static.LocalHumanPlayer.Identity.IdentityId;
            return -1;
        }

        public bool DoDamage(float damage, MyStringHash damageType, bool updateSync, long attackerId = 0)
        {
            damage *= CharacterGeneralDamageModifier;
            if (damage < 0)
                return false;

            if (damageType != MyDamageType.Suicide && ControllerInfo.IsLocallyControlled()
                && MySession.Static.CameraController == this)
            {
                const float maxShake = 5.0f;
                MySector.MainCamera.CameraShake.AddShake(maxShake * damage / (damage + maxShake));
            }

            if ((!CharacterCanDie && !(damageType == MyDamageType.Suicide && MyPerGameSettings.CharacterSuicideEnabled)) || StatComp == null)
                return false;

            if (MySession.Static.AdminSettings.HasFlag(AdminSettingsEnum.Invulnerable))
                return false;
            else if (ControllerInfo.Controller != null && ControllerInfo.Controller.Player != null)
            {
                AdminSettingsEnum set;
                if (MySession.Static.RemoteAdminSettings.TryGetValue(ControllerInfo.Controller.Player.Id.SteamId, out set))
                {
                    if (set.HasFlag(AdminSettingsEnum.Invulnerable))
                        return false;
                }
            }

            MyEntity attacker;
            if (damageType != MyDamageType.Suicide && MyEntities.TryGetEntityById(attackerId, out attacker))
            {   // Checking friendly fire using faction's friendly fire settings
                var localPlayer = MyPlayer.GetPlayerFromCharacter(this);
                MyPlayer otherPlayer = null;
                if (attacker == this)
                {
                    return false;
                }
                else if (attacker is MyCharacter)
                {
                    otherPlayer = MyPlayer.GetPlayerFromCharacter(attacker as MyCharacter);
                }
                else if (attacker is IMyGunBaseUser)
                {
                    otherPlayer = MyPlayer.GetPlayerFromWeapon(attacker as IMyGunBaseUser);
                }
                else if (attacker is MyHandDrill)
                {
                    otherPlayer = MyPlayer.GetPlayerFromCharacter((attacker as MyHandDrill).Owner);
                }

                if (localPlayer != null && otherPlayer != null)
                {
                    var localPlayerFaction = MySession.Static.Factions.TryGetPlayerFaction(localPlayer.Identity.IdentityId) as MyFaction;
                    if (localPlayerFaction != null && !localPlayerFaction.EnableFriendlyFire && localPlayerFaction.IsMember(otherPlayer.Identity.IdentityId))
                    {
                        return false; // No Friendly Fire Enabled!
                    }
                }

                if (damage >= 0f && MySession.Static != null && MyMusicController.Static != null)
                {
                    if (this == MySession.Static.LocalCharacter && attacker is MyVoxelPhysics == false && attacker is MyCubeGrid == false)
                        MyMusicController.Static.Fighting(false, (int)damage * 3);//not fall damage
                    else if (attacker == MySession.Static.LocalCharacter)
                        MyMusicController.Static.Fighting(false, (int)damage * 2);//attack other character
                    else if (attacker is IMyGunBaseUser && (attacker as IMyGunBaseUser).Owner as MyCharacter == MySession.Static.LocalCharacter)
                        MyMusicController.Static.Fighting(false, (int)damage * 2);//attack other character with gun
                    else if (MySession.Static.ControlledEntity == attacker)
                        MyMusicController.Static.Fighting(false, (int)damage);//attack other character with turret
                }
            }

            MyDamageInformation damageInfo = new MyDamageInformation(false, damage, damageType, attackerId);
            if (UseDamageSystem && !(m_dieAfterSimulation || IsDead))
                MyDamageSystem.Static.RaiseBeforeDamageApplied(this, ref damageInfo);

            if (damageInfo.Amount <= 0f)
                return false;

            StatComp.DoDamage(damage, updateSync, damageInfo);

            // Cache the last damage information for the analytics module.
            MyAnalyticsHelper.SetLastDamageInformation(damageInfo);

            if (UseDamageSystem)
                MyDamageSystem.Static.RaiseAfterDamageApplied(this, damageInfo);

            if (updateSync)
                TriggerCharacterAnimationEvent("hurt", true);
            else
                AnimationController.TriggerAction(MyAnimationVariableStorageHints.StrIdActionHurt);

            return true;
        }

        void IMyDecalProxy.AddDecals(MyHitInfo hitInfo, MyStringHash source, object customdata, IMyDecalHandler decalHandler, MyStringHash material)
        {
            MyCharacterHitInfo charHitInfo = customdata as MyCharacterHitInfo;
            if (charHitInfo == null || charHitInfo.BoneIndex == -1)
                return;

            MyDecalRenderInfo renderable = new MyDecalRenderInfo();
            renderable.Position = charHitInfo.Triangle.IntersectionPointInObjectSpace;
            renderable.Normal = charHitInfo.Triangle.NormalInObjectSpace;
            renderable.RenderObjectId = Render.GetRenderObjectID();

            if (material.GetHashCode() == 0)
                renderable.Material = MyStringHash.GetOrCompute(m_characterDefinition.PhysicalMaterial);
            else
                renderable.Material = material;


            VertexBoneIndicesWeights? boneIndicesWeights = charHitInfo.Triangle.GetAffectingBoneIndicesWeights(ref m_boneIndexWeightTmp);
            renderable.BoneIndices = boneIndicesWeights.Value.Indices;
            renderable.BoneWeights = boneIndicesWeights.Value.Weights;

            renderable.Binding = new MyDecalBindingInfo()
            {
                Position = charHitInfo.HitPositionBindingPose,
                Normal = charHitInfo.HitNormalBindingPose,
                Transformation = charHitInfo.BindingTransformation
            };

            var decalId = decalHandler.AddDecal(ref renderable);
            if (decalId == null)
                return;

            AddBoneDecal(decalId.Value, charHitInfo.BoneIndex);
        }

        void IMyCharacter.Kill(object statChangeData)
        {
            MyDamageInformation damageInfo = new MyDamageInformation();
            if (statChangeData != null)
                damageInfo = (MyDamageInformation)statChangeData;

            Kill(true, damageInfo);
        }

        void IMyCharacter.TriggerCharacterAnimationEvent(string eventName, bool sync)
        {
            this.TriggerCharacterAnimationEvent(eventName, sync);
        }

        public void Kill(bool sync, MyDamageInformation damageInfo)
        {
            if (m_dieAfterSimulation || IsDead || (MyFakes.DEVELOPMENT_PRESET && damageInfo.Type != MyDamageType.Suicide))
                return;

            if (sync)
            {
                KillCharacter(damageInfo);
                return;
            }

            if (UseDamageSystem)
                MyDamageSystem.Static.RaiseDestroyed(this, damageInfo);

            MyAnalyticsHelper.SetLastDamageInformation(damageInfo);

            m_dieAfterSimulation = true;
        }

        public void Die()
        {
            if ((CharacterCanDie || MyPerGameSettings.CharacterSuicideEnabled) && !IsDead)
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                buttonType: MyMessageBoxButtonsType.YES_NO,
                messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionPleaseConfirm),
                messageText: MyTexts.Get(MyCommonTexts.MessageBoxTextSuicide),
                focusedResult: MyGuiScreenMessageBox.ResultEnum.NO,
                callback: delegate(MyGuiScreenMessageBox.ResultEnum retval)
                {
                    if (retval == MyGuiScreenMessageBox.ResultEnum.YES)
                        DoDamage(1000, MyDamageType.Suicide, true, this.EntityId);
                }));
            }
        }

        private void DieInternal()
        {
            if (!CharacterCanDie && !MyPerGameSettings.CharacterSuicideEnabled)
                return;

            if (MySession.Static.LocalCharacter == this)
                m_localCharacterWasInThirdPerson = !IsInFirstPersonView;

            MyHud.CharacterInfo.HealthRatio = 0f;
            SoundComp.PlayDeathSound(StatComp != null ? StatComp.LastDamage.Type : MyStringHash.NullOrEmpty);
            if (UseNewAnimationSystem)
                AnimationController.Variables.SetValue(MyAnimationVariableStorageHints.StrIdDead, 1.0f);

            if (m_InventoryScreen != null)
            {
                m_InventoryScreen.CloseScreen();
            }

            if (StatComp != null && StatComp.Health != null)
                StatComp.Health.OnStatChanged -= StatComp.OnHealthChanged;

            if (m_breath != null)
                m_breath.CurrentState = MyCharacterBreath.State.NoBreath;

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

            if (MySession.Static.ControlledEntity is MyRemoteControl)
            {
                //This will happen when character is killed without being destroyed
                var remoteControl = MySession.Static.ControlledEntity as MyRemoteControl;
                if (remoteControl.PreviousControlledEntity == this)
                {
                    remoteControl.ForceReleaseControl();
                }
            }

            //TODO(AF) Create a shared RemoteControl component
            if (MySession.Static.ControlledEntity is MyLargeTurretBase && MySession.Static.LocalCharacter == this)  //GK: Character can be something else(e.g. spider and the control still will be released)
            {
                //This will happen when character is killed without being destroyed
                var turret = MySession.Static.ControlledEntity as MyLargeTurretBase;
                turret.ForceReleaseControl();
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
                            Distance = MyThirdPersonSpectator.Static.GetViewerDistance(),
                            IsFirstPerson = IsInFirstPersonView,
                            HeadAngle = new Vector2(HeadLocalXAngle, HeadLocalYAngle)
                        };
                    }
                }
            }

            MyAnalyticsHelper.ReportPlayerDeath(ControllerInfo.IsLocallyHumanControlled(), playerId);

            MySandboxGame.Log.WriteLine("Player character died. Id : " + playerId);

            EndShootAll();

            // If it is the local player who died, give this player a death location coordinate
            if (GetPlayerIdentityId() == MySession.Static.LocalPlayerId)
            {
                m_isDeathPlayer = true;
                string bodyLocationName = MyTexts.Get(MySpaceTexts.GPS_Body_Location_Name).ToString();
                MyGps deathLocation = MySession.Static.Gpss.GetGpsByName(MySession.Static.LocalPlayerId, bodyLocationName) as MyGps;

                if (deathLocation != null)
                {
                    deathLocation.Coords = new Vector3D(MySession.Static.LocalHumanPlayer.GetPosition());
                    deathLocation.Coords.X = Math.Round(deathLocation.Coords.X, 2);
                    deathLocation.Coords.Y = Math.Round(deathLocation.Coords.Y, 2);
                    deathLocation.Coords.Z = Math.Round(deathLocation.Coords.Z, 2);
                    MySession.Static.Gpss.SendModifyGps(MySession.Static.LocalPlayerId, deathLocation);
                }
                else
                {
                    deathLocation = new MyGps();
                    deathLocation.Name = bodyLocationName;
                    deathLocation.Description = MyTexts.Get(MySpaceTexts.GPS_Body_Location_Desc).ToString();
                    deathLocation.Coords = new Vector3D(MySession.Static.LocalHumanPlayer.GetPosition());
                    deathLocation.Coords.X = Math.Round(deathLocation.Coords.X, 2);
                    deathLocation.Coords.Y = Math.Round(deathLocation.Coords.Y, 2);
                    deathLocation.Coords.Z = Math.Round(deathLocation.Coords.Z, 2);
                    deathLocation.ShowOnHud = true;
                    deathLocation.DiscardAt = null;
                    MySession.Static.Gpss.SendAddGps(MySession.Static.LocalPlayerId, ref deathLocation);
                }
            }

            if (Sync.IsServer && m_currentWeapon != null && m_currentWeapon.PhysicalObject != null)
            {
                var inventoryItem = new MyPhysicalInventoryItem()
                {
                    Amount = 1,
                    Scale = 1f,
                    Content = m_currentWeapon.PhysicalObject,
                };
                // Guns 
                if (inventoryItem.Content is MyObjectBuilder_PhysicalGunObject)
                {
                    (inventoryItem.Content as MyObjectBuilder_PhysicalGunObject).GunEntity.EntityId = 0;
                }
                MyFloatingObjects.Spawn(inventoryItem, ((MyEntity)m_currentWeapon).PositionComp.GetPosition(), WorldMatrix.Forward, WorldMatrix.Up, Physics);
                this.GetInventory().RemoveItemsOfType(1, m_currentWeapon.PhysicalObject);
            }

            IsUsing = null;
            m_isFalling = false;
            SetCurrentMovementState(MyCharacterMovementEnum.Died);
            UnequipWeapon();
            //Inventory.Clear(false);
            StopUpperAnimation(0.5f);
            //SoundComp.StartSecondarySound(Definition.DeathSoundName, sync: false);

            m_animationCommandsEnabled = true;
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
            SoundComp.CharacterDied();
            JetpackComp = null; // m_jetpackEnabled = false;

            // Syncing dead bodies only when the ragdoll is disabled
            if (!Components.Has<MyCharacterRagdollComponent>())
            {
                SyncFlag = true;
            }

            var handler = OnCharacterDied;
            if (handler != null)
            {
                handler(this);
            }

        }

        private void StartRespawn(float respawnTime)
        {
            if (ControllerInfo.Controller != null && ControllerInfo.Controller.Player != null)
            {
                MySessionComponentMissionTriggers.PlayerDied(ControllerInfo.Controller.Player);
                if (MyVisualScriptLogicProvider.PlayerDied != null && !IsBot)
                    MyVisualScriptLogicProvider.PlayerDied(ControllerInfo.Controller.Player.Identity.IdentityId);
                if (MyVisualScriptLogicProvider.NPCDied != null && IsBot)
                    MyVisualScriptLogicProvider.NPCDied(DefinitionId.HasValue ? DefinitionId.Value.SubtypeName : "");
                if (!MySessionComponentMissionTriggers.CanRespawn(this.ControllerInfo.Controller.Player.Id))
                {
                    m_currentRespawnCounter = -1;
                    return;
                }
            }

            if (this == MySession.Static.ControlledEntity)
            {
                MyGuiScreenTerminal.Hide();

                m_respawnNotification = new MyHudNotification(MyCommonTexts.NotificationRespawn, (int)(RESPAWN_TIME * 1000), priority: 5);
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

            RadioBroadcaster.BroadcastRadius = 5;

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
                // CH: Need to rethink this. It does not belong here, but I don't want to add "DeadCharacterBodyCenterOfMass" to the character definition either...
                // MZ: See ticket "Correct dying for characters", https://app.asana.com/0/64822442925263/75411538582998
                //     dead body shape can now be specified in character's SBC
                if (Definition.DeadBodyShape != null)
                {
                    HkBoxShape bshape = new HkBoxShape(PositionComp.LocalAABB.HalfExtents * Definition.DeadBodyShape.BoxShapeScale);
                    massProperties = HkInertiaTensorComputer.ComputeBoxVolumeMassProperties(bshape.HalfExtents, massProperties.Mass);
                    massProperties.CenterOfMass = bshape.HalfExtents * Definition.DeadBodyShape.RelativeCenterOfMass;
                    shape = bshape;

                    Physics = new MyPhysicsBody(this, RigidBodyFlag.RBF_DEFAULT);
                    Vector3D offset = PositionComp.LocalAABB.HalfExtents * Definition.DeadBodyShape.RelativeShapeTranslation;
                    MatrixD pos = MatrixD.CreateTranslation(offset);
                    Physics.CreateFromCollisionObject(shape, PositionComp.LocalVolume.Center + offset, pos, massProperties, MyPhysics.CollisionLayers.FloatingObjectCollisionLayer);
                    Physics.Friction = Definition.DeadBodyShape.Friction;
                    Physics.RigidBody.MaxAngularVelocity = MathHelper.PiOver2;
                    Physics.LinearVelocity = velocity;
                    shape.RemoveReference();

                    Physics.Enabled = true;
                }
                else // no special definition => use AABB
                {
                    HkBoxShape bshape = new HkBoxShape(PositionComp.LocalAABB.HalfExtents);
                    massProperties = HkInertiaTensorComputer.ComputeBoxVolumeMassProperties(bshape.HalfExtents, massProperties.Mass);
                    massProperties.CenterOfMass = new Vector3(bshape.HalfExtents.X, 0, 0);
                    shape = bshape;

                    Physics = new MyPhysicsBody(this, RigidBodyFlag.RBF_DEFAULT);
                    //<ib.ragdoll> VRAG-106 Fix ragdoll bone transformations, Center must be zero for ragdoll
                    //Physics.CreateFromCollisionObject(shape, PositionComp.LocalVolume.Center, MatrixD.Identity, massProperties, MyPhysics.CollisionLayers.FloatingObjectCollisionLayer);
                    Physics.CreateFromCollisionObject(shape, Vector3.Zero, MatrixD.Identity, massProperties, MyPhysics.CollisionLayers.FloatingObjectCollisionLayer);
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
            RecalculatePowerRequirement(true);

            var health = StatComp != null ? StatComp.Health : null;
            if (health != null)
            {
                if (m_savedHealth.HasValue)
                    health.Value = m_savedHealth.Value;
                health.OnStatChanged += StatComp.OnHealthChanged;
            }

            if (m_breath != null)
                m_breath.ForceUpdate();

            if (m_currentMovementState == MyCharacterMovementEnum.Died)
            {
                Physics.ForceActivate();
            }

            base.UpdateOnceBeforeFrame();

            if (m_currentWeapon != null)
            {
                MyEntities.Remove((MyEntity)m_currentWeapon);
                EquipWeapon(m_currentWeapon);
            }

            if (m_savedPlayer.HasValue && m_savedPlayer.Value.SteamId != 0)
            {
                m_controlInfo.Value = m_savedPlayer.Value;
            }
            if (this.IsDead == false && this == MySession.Static.LocalCharacter)
            {
                //AB: We want spectator remain on saved position
                //MySpectatorCameraController.Static.Position = this.PositionComp.GetPosition();
            }
        }

        #endregion Power consumer

        #region Properties

        public Vector3 ColorMask
        {
            get { return base.Render.ColorMaskHsv; }
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

        public override String DisplayNameText
        {
            get { return DisplayName; }
        }

        public static bool CharactersCanDie
        {
            get { return !MySession.Static.CreativeMode || MyFakes.CHARACTER_CAN_DIE_EVEN_IN_CREATIVE_MODE; }
        }

        public bool CharacterCanDie
        {
            get { return CharactersCanDie || (ControllerInfo.Controller != null && ControllerInfo.Controller.Player.Id.SerialId != 0); }
        }

        public override Vector3D LocationForHudMarker
        {
            get
            {
                return base.LocationForHudMarker + WorldMatrix.Up * 2.1;
            }
        }

        public new MyPhysicsBody Physics { get { return base.Physics as MyPhysicsBody; } set { base.Physics = value; } }

        #endregion Properties

        #region Scene

        public void SetLocalHeadAnimation(float? targetX, float? targetY, float length)
        {
            if (length > 0)
            {
                // prevent rotating back many loops -> limit y rot to -180.0f,+180.0f
                if (m_headLocalYAngle < 0)
                {
                    m_headLocalYAngle = -m_headLocalYAngle;
                    m_headLocalYAngle = (m_headLocalYAngle + 180.0f) % 360.0f - 180.0f;
                    m_headLocalYAngle = -m_headLocalYAngle;
                }
                else
                {
                    m_headLocalYAngle = (m_headLocalYAngle + 180.0f) % 360.0f - 180.0f;
                }
            }
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
                //EnableHead(!isInFPDisabledCockpit || !Render.NearFlag);
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
        public static MyCharacter CreateCharacter(MatrixD worldMatrix, Vector3 velocity, string characterName, string model, Vector3? colorMask, MyBotDefinition botDefinition, bool findNearPos = true, bool AIMode = false, MyCockpit cockpit = null, bool useInventory = true, ulong playerSteamId = 0)
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

            MyCharacter character = CreateCharacterBase(worldMatrix, ref velocity, characterName, model, colorMask, AIMode, useInventory, botDefinition);

            if (cockpit == null && Sync.IsServer && MyPerGameSettings.BlockForVoxels == false)
            {
                MyMultiplayer.ReplicateImmediatelly(MyExternalReplicable.FindByObject(character), new EndpointId(playerSteamId));
            }
            return character;
        }

        private static MyCharacter CreateCharacterBase(MatrixD worldMatrix, ref Vector3 velocity, string characterName, string model, Vector3? colorMask, bool AIMode, bool useInventory = true, MyBotDefinition botDefinition = null)
        {
            MyCharacter character = new MyCharacter();
            MyObjectBuilder_Character objectBuilder = MyCharacter.Random();
            objectBuilder.CharacterModel = model ?? objectBuilder.CharacterModel;

            if (colorMask.HasValue)
                objectBuilder.ColorMaskHSV = colorMask.Value;

            objectBuilder.JetpackEnabled = MySession.Static.CreativeMode;
            objectBuilder.Battery = new MyObjectBuilder_Battery { CurrentCapacity = 1 };
            objectBuilder.AIMode = AIMode;
            objectBuilder.DisplayName = characterName;
            objectBuilder.LinearVelocity = velocity;
            objectBuilder.PositionAndOrientation = new MyPositionAndOrientation(worldMatrix);
            objectBuilder.CharacterGeneralDamageModifier = 1f;
            character.Init(objectBuilder);

            MyEntities.RaiseEntityCreated(character);
            MyEntities.Add(character);

            character.IsReadyForReplication = true;

            System.Diagnostics.Debug.Assert(character.GetInventory() as MyInventory != null, "Null or unexpected inventory type returned!");
            if (useInventory)
                MyWorldGenerator.InitInventoryWithDefaults(character.GetInventory() as MyInventory);
            else if (botDefinition != null)
            {
                // use inventory from bot definition
                botDefinition.AddItems(character);
            }
            //character.PositionComp.SetWorldMatrix(worldMatrix);
            if (velocity.Length() > 0)
            {
                var jetpack = character.JetpackComp;

                if (jetpack != null)
                    jetpack.EnableDampeners(false, false);
            }

            return character;
        }

        #endregion Scene

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
                OutOfAmmoNotification = new MyHudNotification(MyCommonTexts.OutOfAmmo, 2000, font: MyFontEnum.Red);
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
            MyHud.CharacterInfo.Mass = this.GetInventory() != null ? (int)((float)this.GetInventory().CurrentMass + Definition.Mass) : 0;
            MyHud.CharacterInfo.LightEnabled = LightEnabled;
            MyHud.CharacterInfo.BroadcastEnabled = RadioBroadcaster.Enabled;

            var jetpack = JetpackComp;
            bool canFly = JetpackRunning;
            MyHud.CharacterInfo.DampenersEnabled = jetpack != null && jetpack.DampenersTurnedOn;
            MyHud.CharacterInfo.JetpackEnabled = jetpack != null && jetpack.TurnedOn;

            MyHud.CharacterInfo.State = MyHudCharacterStateEnum.Standing;
            var entity = MySession.Static.ControlledEntity;
            var cockpit = entity as MyCockpit;
            if (entity != null)
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
                    else
                        if (IsMagneticBootsEnabled)
                        {
                            MyHud.CharacterInfo.State = MyHudCharacterStateEnum.Magnetic;
                        }
                        else
                            if (IsCrouching)
                                MyHud.CharacterInfo.State = MyHudCharacterStateEnum.Crouching;
                            else
                                if (IsFalling)
                                    MyHud.CharacterInfo.State = MyHudCharacterStateEnum.Falling;
                                else
                                    MyHud.CharacterInfo.State = MyHudCharacterStateEnum.Standing;
                }
            }

            float healthRatio = 1.0f;
            if (StatComp != null)
                healthRatio = StatComp.HealthRatio;

            MyHud.CharacterInfo.HealthRatio = healthRatio;
            MyHud.CharacterInfo.IsHealthLow = healthRatio < MyCharacterStatComponent.LOW_HEALTH_RATIO;
            MyHud.CharacterInfo.InventoryVolume = this.GetInventory() != null ? this.GetInventory().CurrentVolume : 0;
            MyHud.CharacterInfo.IsInventoryFull = this.GetInventory() == null || ((float)this.GetInventory().CurrentVolume / (float)this.GetInventory().MaxVolume) > 0.95f;
            MyHud.CharacterInfo.BroadcastRange = RadioBroadcaster.BroadcastRadius;
            if (OxygenComponent != null)
            {
                MyHud.CharacterInfo.OxygenLevel = OxygenComponent.SuitOxygenLevel;
                MyHud.CharacterInfo.HydrogenRatio = OxygenComponent.GetGasFillLevel(MyCharacterOxygenComponent.HydrogenId);
                MyHud.CharacterInfo.IsOxygenLevelLow = OxygenComponent.IsOxygenLevelLow;
                MyHud.CharacterInfo.IsHelmetOn = OxygenComponent.HelmetEnabled;
            }
        }

        internal void UpdateCharacterPhysics(bool isLocalPlayer)
        {
            if (Physics != null && Physics.Enabled == false)
                return;

            float offset = 2 * MyPerGameSettings.PhysicsConvexRadius + 0.03f; //compensation for convex radius

            float maxSpeedRelativeToShip = Math.Max(Definition.MaxSprintSpeed, Math.Max(Definition.MaxRunSpeed, Definition.MaxBackrunSpeed));

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
                    0.7f, 0.7f, (ushort)MyPhysics.CollisionLayers.CharacterCollisionLayer, RigidBodyFlag.RBF_DEFAULT,
                    MyPerGameSettings.Destruction ? MyDestructionHelper.MassToHavok(Definition.Mass) : Definition.Mass,
                    Definition.VerticalPositionFlyingOnly,
                    Definition.MaxSlope,
                    Definition.ImpulseLimit,
                    maxSpeedRelativeToShip,
                    false,
                    Definition.MaxForce);

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
                    int layer = Sync.IsServer ? MyPhysics.CollisionLayers.CharacterNetworkCollisionLayer : MyPerGameSettings.NetworkCharacterCollisionLayer;

                    this.InitCharacterPhysics(MyMaterialType.CHARACTER, PositionComp.LocalVolume.Center, Definition.CharacterCollisionWidth * Definition.CharacterCollisionScale * scale, Definition.CharacterCollisionHeight - Definition.CharacterCollisionWidth * Definition.CharacterCollisionScale * scale - offset,
                    Definition.CharacterCollisionCrouchHeight - Definition.CharacterCollisionWidth,
                    Definition.CharacterCollisionWidth - offset,
                    Definition.CharacterHeadSize * Definition.CharacterCollisionScale * scale,
                    Definition.CharacterHeadHeight,
                    0.7f, 0.7f, (ushort)layer, MyPerGameSettings.NetworkCharacterType, 0, //Mass is not scaled on purpose (collision over networks)
                    Definition.VerticalPositionFlyingOnly,
                    Definition.MaxSlope,
                    Definition.ImpulseLimit,
                    maxSpeedRelativeToShip,
                    true,
                    Definition.MaxForce);

                    if (MyPerGameSettings.NetworkCharacterType == RigidBodyFlag.RBF_DEFAULT)
                    {
                        Physics.Friction = 1; //to not move on steep surfaces
                    }

                    Physics.Enabled = true;
                }
            }
        }

        #region Multiplayer

        public void GetNetState(out MyCharacterNetState state)
        {
            state.HeadX = HeadLocalXAngle;
            state.HeadY = HeadLocalYAngle; // 2B
            state.MovementState = GetCurrentMovementState();
            state.MovementFlags = MovementFlags;
            bool hasJetpack = JetpackComp != null;
            state.Jetpack = hasJetpack && JetpackComp.TurnedOn;
            state.Dampeners = hasJetpack && JetpackComp.DampenersTurnedOn;
            state.TargetFromCamera = TargetFromCamera;
            state.MoveIndicator = MoveIndicator;
            var q = Quaternion.CreateFromRotationMatrix(Entity.WorldMatrix);
            state.Rotation = q;
            state.Valid = true;
        }

        public void SetNetState(ref MyCharacterNetState state, bool animating)
        {
            if (IsDead || IsUsing != null || Closed)
                return;

            SetHeadLocalXAngle(state.HeadX);
            SetHeadLocalYAngle(state.HeadY);

            if (!Sandbox.Engine.Utils.MyFakes.MULTIPLAYER_CLIENT_PHYSICS)
                m_isFalling = false;

            var jetpack = JetpackComp;
            if (jetpack != null)
            {
                if (state.Jetpack != JetpackComp.TurnedOn)
                {
                    jetpack.TurnOnJetpack(state.Jetpack, true);
                }
                if (state.Dampeners != JetpackComp.DampenersTurnedOn)
                {
                    jetpack.EnableDampeners(state.Dampeners, false);
                }
            }
            TargetFromCamera = state.TargetFromCamera;

            if (animating)
            {
                // do it fast, dont wait for update, no time.
                UpdateMovementAndFlags(state.MovementState, state.MovementFlags);
                if (state.MovementState == MyCharacterMovementEnum.Jump)
                {
                    // Simulate one frame for jump action to be triggered
                    Jump();
                    MoveAndRotateInternal(MoveIndicator, RotationIndicator, RollIndicator, Vector3.Zero);
                }
            }
            else
            {
                // Set client movement state directly and don't perform other operations
                // that may have side-effects to let server side Character.UpdateAfterSimulation()
                // perform exactly same operations as on client
                // Add operator becuase of jump, which can be set on server locally and thus cannot be overriden by late state from client
                MovementFlags = state.MovementFlags | (MovementFlags & MyCharacterMovementFlags.Jump);

                if (Sync.IsServer || MyFakes.MULTIPLAYER_SIMULATE_CHARACTER_CLIENT)
                {
                    CacheMove(ref state.MoveIndicator, ref state.Rotation);
                }
            }
        }

        public void UpdateMovementAndFlags(MyCharacterMovementEnum movementState, MyCharacterMovementFlags flags)
        {
            if (m_currentMovementState != movementState && Physics != null)
            {
                this.m_movementFlags = flags;

                this.SwitchAnimation(movementState);
                this.SetCurrentMovementState(movementState);
            }
        }

        private void SwitchToWeaponSuccess(MyDefinitionId? weapon, uint? inventoryItemId, long weaponEntityId)
        {
            if (!IsDead)
            {
                SwitchToWeaponInternal(weapon, false, inventoryItemId, weaponEntityId);
            }

            if (OnWeaponChanged != null)
            {
                OnWeaponChanged(this, null);
            }
        }

        #endregion Multiplayer

        private MatrixD m_lastCorrectSpectatorCamera;
        private float m_squeezeDamageTimer;

        private const float m_weaponMinAmp = 1.12377834f;
        private const float m_weaponMaxAmp = 1.21786702f;
        private const float m_weaponMedAmp = (m_weaponMinAmp + m_weaponMaxAmp) / 2.0f;
        private const float m_weaponRunMedAmp = (1.03966641f + 1.21786702f) / 2.0f;
        private Quaternion m_weaponMatrixOrientationBackup;
        //readonly Sync<Vector3> m_weaponPosition;
        //readonly Sync<float> m_ikRootBoneOffset;

        private void UpdateLeftHandItemPosition()
        {
            MatrixD leftHandItemMatrix = AnimationController.CharacterBones[m_leftHandItemBone].AbsoluteTransform * WorldMatrix;
            Vector3D up = leftHandItemMatrix.Up;
            leftHandItemMatrix.Up = leftHandItemMatrix.Forward;
            leftHandItemMatrix.Forward = up;
            leftHandItemMatrix.Right = Vector3D.Cross(leftHandItemMatrix.Forward, leftHandItemMatrix.Up);
            m_leftHandItem.WorldMatrix = leftHandItemMatrix;
        }

        public float CurrentJump
        {
            get { return m_currentJumpTime; }
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
            if (ResponsibleForUpdate(Sync.Clients.LocalClient))
            {
                MyMultiplayer.RaiseEvent(this, x => x.ChangeModel_Implementation, model, colorMaskHSV);
            }
        }

        [Event, Reliable, Server, Broadcast]
        private void ChangeModel_Implementation(string model, Vector3 colorMaskHSV)
        {
            ChangeModelAndColorInternal(model, colorMaskHSV);
        }

        public void UpdateStoredGas(MyDefinitionId gasId, float fillLevel)
        {
            MyMultiplayer.RaiseEvent(this, x => x.UpdateStoredGas_Implementation, (SerializableDefinitionId)gasId, fillLevel);
        }

        [Event, Reliable, Broadcast]
        private void UpdateStoredGas_Implementation(SerializableDefinitionId gasId, float fillLevel)
        {
            if (OxygenComponent == null)
                return;

            MyDefinitionId definition = gasId;
            OxygenComponent.UpdateStoredGasLevel(ref definition, fillLevel);
        }

        public void UpdateOxygen(float oxygenAmount)
        {
            MyMultiplayer.RaiseEvent(this, x => x.OnUpdateOxygen, oxygenAmount);
        }

        [Event, Reliable, Broadcast]
        private void OnUpdateOxygen(float oxygenAmount)
        {
            if (OxygenComponent == null)
                return;

            OxygenComponent.SuitOxygenAmount = oxygenAmount;
        }

        public void SendRefillFromBottle(MyDefinitionId gasId)
        {
            MyMultiplayer.RaiseEvent(this, x => x.OnRefillFromBottle, (SerializableDefinitionId)gasId);
        }

        [Event, Reliable, Broadcast]
        private void OnRefillFromBottle(SerializableDefinitionId gasId)
        {
            if (this == MySession.Static.LocalCharacter && OxygenComponent != null)
            {
                OxygenComponent.ShowRefillFromBottleNotification(gasId);
            }
        }

        public void PlaySecondarySound(VRage.Audio.MyCueId soundId)
        {
            MyMultiplayer.RaiseEvent(this, x => x.OnSecondarySoundPlay, soundId);
        }

        [Event, Reliable, Server, BroadcastExcept]
        private void OnSecondarySoundPlay(VRage.Audio.MyCueId soundId)
        {
            if (!MySandboxGame.IsDedicated)
            {
                SoundComp.StartSecondarySound(soundId, sync: false);
            }
        }

        internal void ChangeModelAndColorInternal(string model, Vector3 colorMaskHSV)
        {
            MyCharacterDefinition def;
            if (model != m_characterModel && MyDefinitionManager.Static.Characters.TryGetValue(model, out def) && !string.IsNullOrEmpty(def.Model))
            {
                MyObjectBuilder_Character characterOb = (MyObjectBuilder_Character)GetObjectBuilder();

                var inventory = Components.Get<MyInventoryBase>();
                Components.Remove<MyInventoryBase>();
                Components.Remove<MyCharacterJetpackComponent>();
                Components.Remove<MyCharacterRagdollComponent>();
                AnimationController.Clear();

                var newModel = VRage.Game.Models.MyModels.GetModelOnlyData(def.Model);

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

                if (m_breath != null)
                {
                    m_breath.Close();
                    m_breath = null;
                }

                float oldHealthRatio = StatComp != null ? StatComp.HealthRatio : 1f;
                float oldHeadLocalX = m_headLocalXAngle;
                float oldHeadLocalY = m_headLocalYAngle;

                Init(characterOb);
                //GR: Do this in order to reset to max volume and then be able to reinitialize inventory's max volume. Do this only when model is to be changed
                this.GetInventory().ResetVolume();
                InitInventory(characterOb);

                m_headLocalXAngle = oldHeadLocalX;
                m_headLocalYAngle = oldHeadLocalY;

                if (StatComp != null && StatComp.Health != null)
                    StatComp.Health.Value = StatComp.Health.MaxValue - StatComp.Health.MaxValue * (1f - oldHealthRatio);

                SwitchAnimation(characterOb.MovementState, false);

                if (m_currentWeapon != null)
                {
                    m_currentWeapon.OnControlAcquired(this);
                }

                MyEntities.Add(this);

                MyEntityIdentifier.AllocationSuspended = false;

                if (ControllerInfo.Controller != null && ControllerInfo.Controller.Player != null)
                {
                    ControllerInfo.Controller.Player.Identity.ChangeCharacter(this);
                }

                // Recharge you suit because is brand new after helmet takin out! (Otherwise you will fall for a moment)
                SuitRechargeDistributor.UpdateBeforeSimulation();
            }

            Render.ColorMaskHsv = colorMaskHSV;
        }

        public void SetPhysicsEnabled(bool enabled)
        {
            MyMultiplayer.RaiseEvent(this, x => x.EnablePhysics, enabled);
        }

        [Event, Reliable, Broadcast]
        private void EnablePhysics(bool enabled)
        {
            Physics.Enabled = enabled;
        }

        public VRage.Game.MyRelationsBetweenPlayerAndBlock GetRelationTo(long playerId)
        {
            var controller = ControllerInfo.Controller ?? m_oldController;
            if (controller == null)
                return VRage.Game.MyRelationsBetweenPlayerAndBlock.Enemies;

            return controller.Player.GetRelationTo(playerId);
        }

        IMyEntity IMyUseObject.Owner
        {
            get { return this; }
        }

        MyModelDummy IMyUseObject.Dummy
        {
            get { return null; }
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

                if (IsDead && Physics != null && Definition.DeadBodyShape != null)
                {
                    float scale = 0.8f;
                    Matrix m = WorldMatrix;
                    m.Forward *= scale;
                    m.Up *= Definition.CharacterCollisionHeight * scale;
                    m.Right *= scale;
                    m.Translation = PositionComp.WorldAABB.Center;
                    m.Translation += 0.5f * m.Right * Definition.DeadBodyShape.RelativeShapeTranslation.X;
                    m.Translation += 0.5f * m.Up * Definition.DeadBodyShape.RelativeShapeTranslation.Y;
                    m.Translation += 0.5f * m.Forward * Definition.DeadBodyShape.RelativeShapeTranslation.Z;
                    return m;
                }
                else
                {
                    float scale = 0.75f;
                    Matrix m = WorldMatrix;
                    m.Forward *= scale;
                    m.Up *= Definition.CharacterCollisionHeight * scale;
                    m.Right *= scale;
                    m.Translation = PositionComp.WorldAABB.Center;
                    return m;
                }
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
                // Keep using base property call, as it won't fail because of invalid cast
                return base.Render.GetRenderObjectID();
            }
        }

        void IMyUseObject.SetRenderID(uint id)
        {
        }

        int IMyUseObject.InstanceID
        {
            get { return -1; }
        }

        void IMyUseObject.SetInstanceID(int id)
        {
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
                return m_currentMovementState == MyCharacterMovementEnum.Died ? UseActionEnum.OpenInventory | UseActionEnum.OpenTerminal : UseActionEnum.None;
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
                if (inventory != null)
                {
                    var screen = user.ShowAggregateInventoryScreen(inventory);
                }
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
                JoystickText = MyCommonTexts.NotificationHintJoystickPressToOpenInventory,
                JoystickFormatParams = new object[] { DisplayName },
            };
        }

        void IMyUseObject.OnSelectionLost()
        {
        }

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

        void IMyCameraController.ControlCamera(MyCamera currentCamera)
        {
            MatrixD viewMatrix = GetViewMatrix();
            currentCamera.SetViewMatrix(viewMatrix);
            currentCamera.CameraSpring.Enabled = !(IsInFirstPersonView || ForceFirstPersonCamera);
            currentCamera.CameraSpring.SetCurrentCameraControllerVelocity(Physics != null ? Physics.LinearVelocity : Vector3.Zero);
            currentCamera.CameraShake.AddShake(m_currentCameraShakePower);
            m_currentCameraShakePower = 0;

            EnableHead(!ControllerInfo.IsLocallyControlled() || (!IsInFirstPersonView && !ForceFirstPersonCamera));
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
            if (InScene)
                EnableHead(true);
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

        bool IMyCameraController.HandlePickUp()
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

        MatrixD IMyModdingControllableEntity.GetHeadMatrix(bool includeY, bool includeX, bool forceHeadAnim, bool forceHeadBone)
        {
            return GetHeadMatrix(includeY, includeX, forceHeadAnim);
        }

        void IMyModdingControllableEntity.MoveAndRotate(Vector3 moveIndicator, Vector2 rotationIndicator, float rollIndicator)
        {
            MoveAndRotate(moveIndicator, rotationIndicator, rollIndicator);
        }

        void IMyModdingControllableEntity.MoveAndRotateStopped()
        {
            MoveAndRotateStopped();
        }

        void IMyModdingControllableEntity.Use()
        {
            Use();
        }

        void IMyModdingControllableEntity.UseContinues()
        {
            UseContinues();
        }

        void IMyControllableEntity.UseFinished()
        {
            UseFinished();
        }

        void IMyModdingControllableEntity.PickUp()
        {
            PickUp();
        }

        void IMyModdingControllableEntity.PickUpContinues()
        {
            PickUpContinues();
        }

        void IMyControllableEntity.PickUpFinished()
        {
            PickUpFinished();
        }

        void IMyModdingControllableEntity.Jump()
        {
            Jump();
            if (Sync.IsServer == false)
            {
                MyMultiplayer.RaiseEvent(this, x => x.Jump);
            }
        }

        void IMyControllableEntity.Sprint(bool enabled)
        {
            Sprint(enabled);
        }

        void IMyModdingControllableEntity.Up()
        {
            Up();
        }

        void IMyModdingControllableEntity.Crouch()
        {
            Crouch();
        }

        void IMyModdingControllableEntity.Down()
        {
            Down();
        }

        void IMyModdingControllableEntity.ShowInventory()
        {
            ShowInventory();
        }

        void IMyModdingControllableEntity.ShowTerminal()
        {
            ShowTerminal();
        }

        void IMyModdingControllableEntity.SwitchThrusts()
        {
            var jetpack = JetpackComp;
            if (jetpack != null && HasEnoughSpaceToStandUp())
            {
                jetpack.SwitchThrusts();
            }
        }

        void IMyModdingControllableEntity.SwitchDamping()
        {
            var jetpack = JetpackComp;
            if (jetpack != null)
            {
                jetpack.SwitchDamping();
            }
        }

        void IMyModdingControllableEntity.SwitchLights()
        {
            SwitchLights();
        }

        void IMyModdingControllableEntity.SwitchLeadingGears()
        {
            SwitchLeadingGears();
        }

        void IMyModdingControllableEntity.SwitchReactors()
        {
            SwitchReactors();
        }

        bool IMyModdingControllableEntity.EnabledThrusts
        {
            get { return JetpackComp != null && JetpackComp.TurnedOn; }
        }

        bool IMyModdingControllableEntity.EnabledDamping
        {
            get { return JetpackComp != null && JetpackComp.DampenersTurnedOn; }
        }

        bool IMyModdingControllableEntity.EnabledLights
        {
            get { return LightEnabled; }
        }

        bool IMyModdingControllableEntity.EnabledLeadingGears
        {
            get { return false; }
        }

        bool IMyModdingControllableEntity.EnabledReactors
        {
            get { return false; }
        }

        bool IMyControllableEntity.EnabledBroadcasting
        {
            get { return RadioBroadcaster.Enabled; }
        }

        bool IMyModdingControllableEntity.EnabledHelmet
        {
            get { return OxygenComponent.HelmetEnabled; }
        }

        void IMyModdingControllableEntity.SwitchHelmet()
        {
            MyMultiplayer.RaiseEvent(this, x => x.OnSwitchHelmet);
        }

        void IMyModdingControllableEntity.Die()
        {
            Die();
        }

        void IMyDestroyableObject.OnDestroy()
        {
            OnDestroy();
        }

        bool IMyDestroyableObject.DoDamage(float damage, MyStringHash damageType, bool sync, MyHitInfo? hitInfo, long attackerId)
        {
            return DoDamage(damage, damageType, sync, attackerId);
        }

        float IMyDestroyableObject.Integrity
        {
            get { return Integrity; }
        }

        public bool PrimaryLookaround
        {
            get { return false; }
        }

        private class MyCharacterPosition : MyPositionComponent
        {
            private const int CHECK_FREQUENCY = 20;
            private int m_checkOutOfWorldCounter = 0;

            protected override void OnWorldPositionChanged(object source, bool updateChildren)
            {
                ClampToWorld();
                base.OnWorldPositionChanged(source, updateChildren);
            }

            private void ClampToWorld()
            {
                if (MySession.Static.WorldBoundaries.HasValue)
                {
                    m_checkOutOfWorldCounter++;
                    if (m_checkOutOfWorldCounter > CHECK_FREQUENCY)
                    {
                        var pos = GetPosition();
                        var min = MySession.Static.WorldBoundaries.Value.Min;
                        var max = MySession.Static.WorldBoundaries.Value.Max;
                        var vMinTen = pos - Vector3.One * 10;
                        var vPlusTen = pos + Vector3.One * 10;
                        if (!(vMinTen.X < min.X || vMinTen.Y < min.Y || vMinTen.Z < min.Z || vPlusTen.X > max.X || vPlusTen.Y > max.Y || vPlusTen.Z > max.Z))
                        {
                            m_checkOutOfWorldCounter = 0;
                            return;
                        }
                        var velocity = Container.Entity.Physics.LinearVelocity;
                        bool clamp = false;
                        if (pos.X < min.X || pos.X > max.X)
                        {
                            clamp = true;
                            velocity.X = 0;
                        }
                        if (pos.Y < min.Y || pos.Y > max.Y)
                        {
                            clamp = true;
                            velocity.Y = 0;
                        }
                        if (pos.Z < min.Z || pos.Z > max.Z)
                        {
                            clamp = true;
                            velocity.Z = 0;
                        }
                        if (clamp)
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
            foreach (MyAnimationDefinition animation in animations)
            {
                string model = animation.AnimationModel;
                if (!string.IsNullOrEmpty(model))
                {
                    MyModels.GetModelOnlyAnimationData(model);
                }
            }

            if (MyModelImporter.LINEAR_KEYFRAME_REDUCTION_STATS)
            {
                var stats = MyModelImporter.ReductionStats;

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

            // Call moved to MyCharacterSoundComponent.Init()
            //MyCharacterSoundComponent.Preload();
        }

        #region Movement properties

        public MyCharacterMovementFlags MovementFlags
        {
            get { return m_movementFlags; }
            internal set
            {
                m_movementFlags = value;
            }
        }

        public MyCharacterMovementFlags PreviousMovementFlags
        {
            get { return m_previousMovementFlags; }
        }

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

        private bool WantsSprint
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

        private bool WantsFlyUp
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

        private bool WantsFlyDown
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

        private bool WantsCrouch
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

        #endregion Movement properties

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

        private void ResetMovement()
        {
            MoveIndicator = Vector3.Zero;
            RotationIndicator = Vector2.Zero;
            RollIndicator = 0.0f;
        }

        #region ModAPI

        public float EnvironmentOxygenLevel
        {
            get { return OxygenComponent.EnvironmentOxygenLevel; }
        }

        /// <summary>
        /// Returns the amount of energy the suit has, values will range between 0 and 1, where 0 is no charge and 1 is full charge.
        /// </summary>
        public float SuitEnergyLevel
        {
            get { return SuitBattery.ResourceSource.RemainingCapacityByType(MyResourceDistributorComponent.ElectricityId) / MyEnergyConstants.BATTERY_MAX_CAPACITY; }
        }

        /// <summary>
        /// Returns the amount of gas left in the suit, values will range between 0 and 1, where 0 is no gas and 1 is full gas.
        /// </summary>
        /// <param name="gasDefinitionId">Definition Id of the gas. Common example: new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Oxygen")</param>
        /// <returns></returns>
        public float GetSuitGasFillLevel(MyDefinitionId gasDefinitionId)
        {
            return OxygenComponent.GetGasFillLevel(gasDefinitionId);
        }

        /// <summary>
        /// Returns true is this character is a player character, otherwise false.
        /// </summary>
        public bool IsPlayer
        {
            get
            {
                if (ControllerInfo.Controller == null)
                    return false;
                return ControllerInfo.Controller.Player.IsRealPlayer;
            }
        }

        /// <summary>
        /// Returns true is this character is an AI character, otherwise false.
        /// </summary>
        public bool IsBot
        {
            get { return !IsPlayer; }
        }

        public int SpineBoneIndex
        {
            get { return m_spineBone; }
        }

        public int HeadBoneIndex
        {
            get { return m_headBoneIndex; }
        }

        #endregion ModAPI

        //public bool IsUseObjectOfType<T>()
        //{
        //    return UseObject is T;
        //}

        public MyEntity ManipulatedEntity;
        private MyGuiScreenBase m_InventoryScreen;

        private void KillCharacter(MyDamageInformation damageInfo)
        {
            Debug.Assert(Sync.IsServer, "KillCharacter called from client");
            Kill(false, damageInfo);
            MyMultiplayer.RaiseEvent(this, x => x.OnKillCharacter, damageInfo);
        }

        [Event, Reliable, Broadcast]
        private void OnKillCharacter(MyDamageInformation damageInfo)
        {
            Kill(false, damageInfo);
        }

        [Event, Reliable, Broadcast]
        public void SpawnCharacterRelative(long RelatedEntity, Vector3 DeltaPosition) // Delta position to related entity in entity local space)
        {
            // Taking control of character, set it's position and set character support
            MyEntity spawnEntity;
            if (RelatedEntity != 0 && MyEntities.TryGetEntityById(RelatedEntity, out spawnEntity))
            {
                Physics.LinearVelocity = spawnEntity.Physics.LinearVelocity;
                Physics.AngularVelocity = spawnEntity.Physics.AngularVelocity;
                MatrixD world = Matrix.CreateTranslation(DeltaPosition) * spawnEntity.WorldMatrix;
                PositionComp.SetPosition(world.Translation);
                // TODO: This should be probably moved into MyCharacterPhysicsStateGroup
                /*var physGroup = MyExternalReplicable.FindByObject(this).FindStateGroup<MyCharacterPhysicsStateGroup>();
                if (physGroup != null)
                {
                    var otherGroup = MyExternalReplicable.FindByObject(spawnEntity).FindStateGroup<MyEntityPhysicsStateGroup>();
                    physGroup.SetSupport(otherGroup ?? MySupportHelper.FindSupportForCharacter(this));
                }*/
            }
        }

        public void SetPlayer(MyPlayer player, bool update = true)
        {
            m_controlInfo.Value = player.Id;
            if (Sync.IsServer && update)
            {
                MyPlayerCollection.ChangePlayerCharacter(player, this, this);
            }
        }

        private void ControlChanged()
        {
            if (Sync.IsServer)
            {
                return;
            }

            if (m_controlInfo.Value.SteamId != 0 && (ControllerInfo.Controller == null || ControllerInfo.Controller.Player.Id != m_controlInfo.Value))
            {
                MyPlayer player = Sync.Players.GetPlayerById(m_controlInfo.Value);
                if (player != null)
                {
                    MyPlayerCollection.ChangePlayerCharacter(player, this, this);
                    if (m_usingEntity != null && player != null)
                    {
                        Sync.Players.SetControlledEntityLocally(player.Id, m_usingEntity);
                    }
                }
            }

            //set spectator to new character position
            if (this.IsDead == false && this == MySession.Static.LocalCharacter)
            {
                MySpectatorCameraController.Static.Position = this.PositionComp.GetPosition();
            }
        }

        private void PromotedChanged()
        {
            //if (Sync.IsServer)
            //    return;

            //Sandbox.Game.World.MyPlayer.PlayerId playerId = m_savedPlayer.HasValue ? m_savedPlayer.Value : m_controlInfo.Value;

            //if (m_isPromoted)
            //{
            //    MySession.Static.PromotedUsers.Add(playerId.SteamId);
            //}
            //else
            //{
            //MySession.Static.PromotedUsers.Remove(playerId.SteamId);
            //}
        }

        public bool ResponsibleForUpdate(MyNetworkClient player)
        {
            if (Sync.Players == null)
                return false;

            var controllingPlayer = Sync.Players.GetControllingPlayer(this);
            if (controllingPlayer == null)
            {
                if (CurrentRemoteControl != null)
                {
                    controllingPlayer = Sync.Players.GetControllingPlayer(CurrentRemoteControl as MyEntity);
                }
            }

            if (controllingPlayer == null)
            {
                return player.IsGameServer();
            }
            else
            {
                return controllingPlayer.Client == player;
            }
        }

        private void StartShooting(Vector3 direction, MyShootActionEnum action)
        {
            ShootDirection = direction;
            m_isShooting[(int)action] = true;
            OnBeginShoot(action);
        }

        public void BeginShootSync(Vector3 direction, MyShootActionEnum action = MyShootActionEnum.PrimaryAction)
        {
            StartShooting(direction, action);

            MyMultiplayer.RaiseEvent(this, x => x.ShootBeginCallback, direction, action);

            if (MyFakes.SIMULATE_QUICK_TRIGGER)
                EndShootInternal(action);
        }

        [Event, Reliable, Server, BroadcastExcept]
        private void ShootBeginCallback(Vector3 direction, MyShootActionEnum action)
        {
            bool wouldCallStartTwice = Sync.IsServer && MyEventContext.Current.IsLocallyInvoked;
            if (!wouldCallStartTwice)
            {
                StartShooting(direction, action);
            }
        }

        private void StopShooting(MyShootActionEnum action)
        {
            m_isShooting[(int)action] = false;
            OnEndShoot(action);
        }

        public void EndShootSync(MyShootActionEnum action = MyShootActionEnum.PrimaryAction)
        {
            if (MyFakes.SIMULATE_QUICK_TRIGGER) return;

            EndShootInternal(action);
        }

        private void EndShootInternal(MyShootActionEnum action = MyShootActionEnum.PrimaryAction)
        {
            MyMultiplayer.RaiseEvent(this, x => x.ShootEndCallback, action);
            StopShooting(action);
        }

        [Event, Reliable, Server, BroadcastExcept]
        private void ShootEndCallback(MyShootActionEnum action)
        {
            bool wouldCallStopTwice = Sync.IsServer && MyEventContext.Current.IsLocallyInvoked;
            if (!wouldCallStopTwice)
            {
                StopShooting(action);
            }
        }

        public void UpdateShootDirection(Vector3 direction, int multiplayerUpdateInterval)
        {
            if (multiplayerUpdateInterval != 0 && MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastShootDirectionUpdate > multiplayerUpdateInterval)
            {
                MyMultiplayer.RaiseEvent(this, x => x.ShootDirectionChangeCallback, direction);
                m_lastShootDirectionUpdate = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            }
            ShootDirection = direction;
        }

        [Event, Reliable, Server, BroadcastExcept]
        private void ShootDirectionChangeCallback(Vector3 direction)
        {
            ShootDirection = direction;
        }

        [Event, Reliable, Server]
        private void OnSwitchAmmoMagazineRequest()
        {
            if ((this as IMyControllableEntity).CanSwitchAmmoMagazine() == false)
            {
                return;
            }

            SwitchAmmoMagazineSuccess();
            MyMultiplayer.RaiseEvent(this, x => x.OnSwitchAmmoMagazineSuccess);
        }

        [Event, Reliable, Broadcast]
        private void OnSwitchAmmoMagazineSuccess()
        {
            SwitchAmmoMagazineSuccess();
        }

        private void RequestSwitchToWeapon(MyDefinitionId? weapon, uint? inventoryItemId)
        {
            SerializableDefinitionId? def = weapon;
            MyMultiplayer.RaiseEvent(this, x => x.SwitchToWeaponMessage, def, inventoryItemId);
        }

        [Event, Reliable, Server]
        private void SwitchToWeaponMessage(SerializableDefinitionId? weapon, uint? inventoryItemId)
        {
            if (CanSwitchToWeapon(weapon) == false)
            {
                return;
            }

            if (inventoryItemId != null)
            {
                var inventory = this.GetInventory();
                if (inventory != null)
                {
                    var item = inventory.GetItemByID(inventoryItemId.Value);
                    if (item.HasValue)
                    {
                        var itemId = MyDefinitionManager.Static.ItemIdFromWeaponId(weapon.Value);
                        if (itemId.HasValue && item.Value.Content.GetObjectId() == itemId.Value)
                        {
                            long weaponEntityId = MyEntityIdentifier.AllocateId();
                            SwitchToWeaponSuccess(weapon, inventoryItemId, weaponEntityId);
                            MyMultiplayer.RaiseEvent(this, x => x.OnSwitchToWeaponSuccess, weapon, inventoryItemId, weaponEntityId);
                        }
                    }
                }
            }
            else if (weapon != null)
            {
                long weaponEntityId = MyEntityIdentifier.AllocateId();
                SwitchToWeaponSuccess(weapon, inventoryItemId, weaponEntityId);
                MyMultiplayer.RaiseEvent(this, x => x.OnSwitchToWeaponSuccess, weapon, inventoryItemId, weaponEntityId);
            }
            else
            {
                MyMultiplayer.RaiseEvent(this, x => x.UnequipWeapon);
            }
        }

        [Event, Reliable, Broadcast]
        private void OnSwitchToWeaponSuccess(SerializableDefinitionId? weapon, uint? inventoryItemId, long weaponEntityId)
        {
            SwitchToWeaponSuccess(weapon, inventoryItemId, weaponEntityId);
        }

        public void SendNewPlayerMessage(MyPlayer.PlayerId senderId, MyPlayer.PlayerId receiverId, string text, TimeSpan timestamp)
        {
            MyMultiplayer.RaiseEvent(this, x => x.OnPlayerMessageRequest, text, senderId.SteamId, receiverId.SteamId, timestamp.Ticks);
        }

        [Event, Reliable, Server]
        private void OnPlayerMessageRequest(string text, ulong senderSteamId, ulong receiverSteamId, long timestamp)
        {
            //Ignore messages that have improper lengths
            if (text.Length == 0 || text.Length > MyChatConstants.MAX_CHAT_STRING_LENGTH)
            {
                return;
            }

            var receiverId = MySession.Static.Players.TryGetPlayerIdentity(new MyPlayer.PlayerId(receiverSteamId));
            var senderId = MySession.Static.Players.TryGetPlayerIdentity(new MyPlayer.PlayerId(senderSteamId));

            //TODO(AF) Check if message was already received

            if (receiverId != null && receiverId.Character != null && senderId != null && senderId.Character != null && MyAntennaSystem.Static.CheckConnection(senderId, receiverId))
            {
                MyMultiplayer.RaiseEvent(this, x => x.OnPlayerMessageSuccess, text, senderSteamId, receiverSteamId, timestamp, new EndpointId(senderSteamId));
                MyMultiplayer.RaiseEvent(this, x => x.OnPlayerMessageSuccess, text, senderSteamId, receiverSteamId, timestamp, new EndpointId(receiverSteamId));

                //Save chat history on server for non-server players
                if (receiverId.Character != MySession.Static.LocalCharacter)
                {
                    MyChatSystem.AddPlayerChatItem(receiverId.IdentityId, senderId.IdentityId, new MyPlayerChatItem(text, senderId.IdentityId, timestamp, true));
                }
                if (senderId.Character != MySession.Static.LocalCharacter)
                {
                    MyChatSystem.AddPlayerChatItem(senderId.IdentityId, receiverId.IdentityId, new MyPlayerChatItem(text, senderId.IdentityId, timestamp, true));
                }
            }
        }

        [Event, Reliable, Client]
        private void OnPlayerMessageSuccess(string text, ulong senderSteamId, ulong receiverSteamId, long timestamp)
        {
            var receiverId = MySession.Static.Players.TryGetPlayerIdentity(new MyPlayer.PlayerId(receiverSteamId));
            var senderId = MySession.Static.Players.TryGetPlayerIdentity(new MyPlayer.PlayerId(senderSteamId));

            if (receiverId != null && receiverId.Character != null && senderId != null && senderId.Character != null)
            {
                if (receiverId.Character == MySession.Static.LocalCharacter && receiverId.Character != senderId.Character)
                {
                    MyChatSystem.AddPlayerChatItem(receiverId.IdentityId, senderId.IdentityId, new MyPlayerChatItem(text, senderId.IdentityId, timestamp, true));
                    MySession.Static.ChatSystem.OnNewPlayerMessage(senderId.IdentityId, senderId.IdentityId);

                    MySession.Static.Gpss.ScanText(text, MyTexts.GetString(MySpaceTexts.TerminalTab_GPS_NewFromPrivateComms));
                }
                else
                {
                    MyChatSystem.SetPlayerChatItemSent(senderId.IdentityId, receiverId.IdentityId, text, new TimeSpan(timestamp), true);
                    MySession.Static.ChatSystem.OnNewPlayerMessage(receiverId.IdentityId, senderId.IdentityId);
                }
            }
        }

        public bool CheckPlayerConnection(ulong senderSteamId, ulong receiverSteamId)
        {
            var receiverId = MySession.Static.Players.TryGetPlayerIdentity(new MyPlayer.PlayerId(receiverSteamId));
            var senderId = MySession.Static.Players.TryGetPlayerIdentity(new MyPlayer.PlayerId(senderSteamId));

            return (receiverId != null && receiverId.Character != null && senderId != null && senderId.Character != null && MyAntennaSystem.Static.CheckConnection(senderId, receiverId));
        }

        public void SendNewGlobalMessage(MyPlayer.PlayerId senderId, string text)
        {
            MyMultiplayer.RaiseEvent(this, x => x.OnGlobalMessageRequest, senderId.SteamId, text);
        }

        [Event, Reliable, Server]
        private void OnGlobalMessageRequest(ulong senderSteamId, string text)
        {
            //Ignore messages that have improper lengths
            if (text.Length == 0 || text.Length > MyChatConstants.MAX_CHAT_STRING_LENGTH)
            {
                return;
            }

            var senderId = MySession.Static.Players.TryGetPlayerIdentity(new MyPlayer.PlayerId(senderSteamId));
            var allPlayers = MySession.Static.Players.GetOnlinePlayers();
            foreach (var player in allPlayers)
            {
                var receiverId = player.Identity;
                if (receiverId != null && receiverId.Character != null && senderId != null && senderId.Character != null && MyAntennaSystem.Static.CheckConnection(senderId, receiverId))
                {
                    MyMultiplayer.RaiseEvent(this, x => x.OnGlobalMessageSuccess, senderSteamId, text, new EndpointId(player.Id.SteamId));

                    //Save chat history on server for non-server players
                    if (receiverId.Character != MySession.Static.LocalCharacter)
                    {
                        MyChatSystem.AddGlobalChatItem(player.Identity.IdentityId, new MyGlobalChatItem(text, senderId.IdentityId));
                    }
                }
            }
        }

        [Event, Reliable, Client]
        private void OnGlobalMessageSuccess(ulong senderSteamId, string text)
        {
            var senderId = MySession.Static.Players.TryGetPlayerIdentity(new MyPlayer.PlayerId(senderSteamId));
            if (MySession.Static.LocalCharacter != null)
            {
                MyChatSystem.AddGlobalChatItem(MySession.Static.LocalPlayerId, new MyGlobalChatItem(text, senderId.IdentityId));
                MySession.Static.ChatSystem.OnNewGlobalMessage(senderId.IdentityId);

                if (MySession.Static.LocalPlayerId != senderId.IdentityId)
                {
                    MySession.Static.Gpss.ScanText(text, MyTexts.GetString(MySpaceTexts.TerminalTab_GPS_NewFromBroadcast));
                }
            }
        }

        public void SendNewFactionMessage(long factionId1, long factionId2, MyFactionChatItem chatItem)
        {
            MyMultiplayer.RaiseStaticEvent(x => OnFactionMessageRequest, factionId1, factionId2, chatItem.GetObjectBuilder());
        }

        static private MyFactionChatItem FindFactionChatItem(long playerId, long factionId1, long factionId2, TimeSpan timestamp, string text)
        {
            var factionChat = MyChatSystem.FindFactionChatHistory(factionId1, factionId2);
            if (factionChat != null)
            {
                foreach (var factionChatItem in factionChat.Chat)
                {
                    if (factionChatItem.Timestamp == timestamp && factionChatItem.Text == text)
                    {
                        return factionChatItem;
                    }
                }
            }

            return null;
        }

        static private void SendConfirmMessageToFaction(long factionId, Dictionary<long, bool> PlayersToSendTo, long factionId1, long factionId2, long originalSenderId, long timestampTicks, long receiverId, string text)
        {
            var receiverFaction = MySession.Static.Factions.TryGetFactionById(factionId);
            foreach (var member in receiverFaction.Members)
            {
                MyPlayer.PlayerId playerId;
                bool sendToMember = false;
                if (PlayersToSendTo.TryGetValue(member.Key, out sendToMember))
                {
                    if (MySession.Static.Players.TryGetPlayerId(member.Value.PlayerId, out playerId) && sendToMember)
                    {
                        MyMultiplayer.RaiseStaticEvent(x => OnConfirmFactionMessageSuccess, factionId1, factionId2, originalSenderId, timestampTicks, receiverId, text, new EndpointId(playerId.SteamId));

                        //Save chat history on server for non-server players
                        if (member.Value.PlayerId != MySession.Static.LocalPlayerId)
                        {
                            ConfirmMessage(factionId1, factionId2, originalSenderId, timestampTicks, receiverId, text, member.Value.PlayerId);
                        }
                    }
                }
            }
        }

        [Event, Reliable, Server]
        static private void OnFactionMessageRequest(long factionId1, long factionId2, MyObjectBuilder_FactionChatItem chatItemBuilder)
        {
            //Ignore messages that have improper lengths
            if (chatItemBuilder.Text.Length == 0 || chatItemBuilder.Text.Length > MyChatConstants.MAX_CHAT_STRING_LENGTH)
            {
                return;
            }

            long currentSenderId = MyEntityIdentifier.ConstructId(MyEntityIdentifier.ID_OBJECT_TYPE.IDENTITY, chatItemBuilder.IdentityIdUniqueNumber);
            var senderId = MySession.Static.Players.TryGetIdentity(currentSenderId);

            var chatItem = new MyFactionChatItem();
            chatItem.Init(chatItemBuilder);

            //Find all members that can receive this messages
            m_tempValidIds.Clear();
            for (int i = 0; i < chatItemBuilder.PlayersToSendToUniqueNumber.Count; i++)
            {
                if (!chatItemBuilder.IsAlreadySentTo[i])
                {
                    long receiverIdentityId = MyEntityIdentifier.ConstructId(MyEntityIdentifier.ID_OBJECT_TYPE.IDENTITY, chatItemBuilder.PlayersToSendToUniqueNumber[i]);
                    var receiverId = MySession.Static.Players.TryGetIdentity(receiverIdentityId);
                    if (Sync.Players.IdentityIsNpc(receiverIdentityId) == false && receiverId != null && receiverId.Character != null && MyAntennaSystem.Static.CheckConnection(senderId, receiverId))
                    {
                        m_tempValidIds.Add(receiverIdentityId);
                    }
                }
            }

            //Set their sent flag to true, so that everyone knows they already got it (no need for confirm message)
            foreach (var id in m_tempValidIds)
            {
                chatItem.PlayersToSendTo[id] = true;
            }

            //Save the flags back in the message
            chatItemBuilder = chatItem.GetObjectBuilder();

            //Send success message back to all recepient members
            foreach (var id in m_tempValidIds)
            {
                MyPlayer.PlayerId receiverPlayerId;
                MySession.Static.Players.TryGetPlayerId(id, out receiverPlayerId);
                ulong steamId = receiverPlayerId.SteamId;

                MyMultiplayer.RaiseStaticEvent(x => OnFactionMessageSuccess, factionId1, factionId2, chatItemBuilder, new EndpointId(steamId));
            }

            //Save chat history on server for non-server players
            if (senderId.Character != MySession.Static.LocalCharacter)
            {
                MyChatSystem.AddFactionChatItem(senderId.IdentityId, factionId1, factionId2, chatItem);
            }
        }

        [Event, Reliable, Client]
        static private void OnFactionMessageSuccess(long factionId1, long factionId2, MyObjectBuilder_FactionChatItem chatItemBuilder)
        {
            long senderIdentityId = MyEntityIdentifier.ConstructId(MyEntityIdentifier.ID_OBJECT_TYPE.IDENTITY, chatItemBuilder.IdentityIdUniqueNumber);

            var factionChatItem = new MyFactionChatItem();
            factionChatItem.Init(chatItemBuilder);
            if (!(Sync.IsServer && senderIdentityId != MySession.Static.LocalPlayerId))
            {
                MyChatSystem.AddFactionChatItem(MySession.Static.LocalPlayerId, factionId1, factionId2, factionChatItem);
            }
            if (senderIdentityId != MySession.Static.LocalPlayerId)
            {
                MySession.Static.Gpss.ScanText(factionChatItem.Text, MyTexts.GetString(MySpaceTexts.TerminalTab_GPS_NewFromFactionComms));
            }
            MySession.Static.ChatSystem.OnNewFactionMessage(factionId1, factionId2, senderIdentityId, true);
        }

        public static bool RetryFactionMessage(long factionId1, long factionId2, MyFactionChatItem chatItem, MyIdentity currentSenderIdentity)
        {
            Debug.Assert(Sync.IsServer, "Faction message retries should only be done on server");

            if (currentSenderIdentity == null || currentSenderIdentity.Character == null)
            {
                return false;
            }

            m_tempValidIds.Clear();
            foreach (var playerToSendTo in chatItem.PlayersToSendTo)
            {
                if (!playerToSendTo.Value)
                {
                    long receiverIdentityId = playerToSendTo.Key;
                    if (Sync.Players.IdentityIsNpc(receiverIdentityId))
                    {
                        continue;
                    }
                    MyIdentity receiverId = MySession.Static.Players.TryGetIdentity(receiverIdentityId);
                    if (receiverId != null && receiverId.Character != null && MyAntennaSystem.Static.CheckConnection(currentSenderIdentity, receiverId))
                    {
                        m_tempValidIds.Add(receiverIdentityId);
                    }
                }
            }

            if (m_tempValidIds.Count == 0)
            {
                return false;
            }

            foreach (var id in m_tempValidIds)
            {
                chatItem.PlayersToSendTo[id] = true;
            }

            foreach (var id in m_tempValidIds)
            {
                MyPlayer.PlayerId receiverPlayerId;
                MySession.Static.Players.TryGetPlayerId(id, out receiverPlayerId);
                ulong steamId = receiverPlayerId.SteamId;

                MyMultiplayer.RaiseStaticEvent(x => OnFactionMessageSuccess, factionId1, factionId2, chatItem.GetObjectBuilder(), new EndpointId(steamId));
            }
            foreach (var id in m_tempValidIds)
            {
                //Send confirmation to members of both factions
                SendConfirmMessageToFaction(factionId1, chatItem.PlayersToSendTo, factionId1, factionId2, chatItem.IdentityId, chatItem.Timestamp.Ticks, id, chatItem.Text);
                if (factionId1 != factionId2)
                {
                    SendConfirmMessageToFaction(factionId2, chatItem.PlayersToSendTo, factionId1, factionId2, chatItem.IdentityId, chatItem.Timestamp.Ticks, id, chatItem.Text);
                }
            }

            return true;
        }

        [Event, Reliable, Client]
        static private void OnConfirmFactionMessageSuccess(long factionId1, long factionId2, long originalSenderId, long timestampTicks, long receiverId, string text)
        {
            ConfirmMessage(factionId1, factionId2, originalSenderId, timestampTicks, receiverId, text, MySession.Static.LocalPlayerId);
        }

        static private void ConfirmMessage(long factionId1, long factionId2, long originalSenderId, long timestampTicks, long receiverId, string text, long localPlayerId)
        {
            MyChatHistory chatHistory;
            if (!MySession.Static.ChatHistory.TryGetValue(localPlayerId, out chatHistory))
            {
                chatHistory = new MyChatHistory(localPlayerId);
            }

            var timestamp = new TimeSpan(timestampTicks);
            var chatItem = FindFactionChatItem(localPlayerId, factionId1, factionId2, timestamp, text);
            if (chatItem != null)
            {
                chatItem.PlayersToSendTo[receiverId] = true;
                if (!MySandboxGame.IsDedicated)
                {
                    MySession.Static.ChatSystem.OnNewFactionMessage(factionId1, factionId2, originalSenderId, false);
                }
            }
            else
            {
                Debug.Fail("Could not find faction chat history between faction " + factionId1 + " and " + factionId2);
            }
        }

        public void SendAnimationCommand(ref MyAnimationCommand command)
        {
            MyMultiplayer.RaiseEvent(this, x => x.OnAnimationCommand, command);
        }

        [Event, Reliable, Server, Broadcast]
        private void OnAnimationCommand(MyAnimationCommand command)
        {
            AddCommand(command);
        }

        public void SendAnimationEvent(string eventName)
        {
            MyMultiplayer.RaiseEvent(this, x => x.OnAnimationEvent, eventName);
        }

        [Event, Reliable, Server, Broadcast]
        private void OnAnimationEvent(string eventName)
        {
            if (UseNewAnimationSystem)
            {
                AnimationController.TriggerAction(MyStringId.GetOrCompute(eventName));
            }
        }

        public void SendRagdollTransforms(Matrix world, Matrix[] localBodiesTransforms)
        {
            if (ResponsibleForUpdate(Sync.Clients.LocalClient))
            {
                Vector3 worldPosition = world.Translation;
                int transformsCount = localBodiesTransforms.Length;
                Quaternion worldOrientation = Quaternion.CreateFromRotationMatrix(world.GetOrientation());
                Vector3[] transformsPositions = new Vector3[transformsCount];
                Quaternion[] transformsOrientations = new Quaternion[transformsCount];
                for (int i = 0; i < localBodiesTransforms.Length; ++i)
                {
                    transformsPositions[i] = localBodiesTransforms[i].Translation;
                    transformsOrientations[i] = Quaternion.CreateFromRotationMatrix(localBodiesTransforms[i].GetOrientation());
                }
                MyMultiplayer.RaiseEvent(this, x => x.OnRagdollTransformsUpdate, transformsCount, transformsPositions, transformsOrientations, worldOrientation, worldPosition);
            }
        }

        [Event, Reliable, Broadcast]
        private void OnRagdollTransformsUpdate(int transformsCount, Vector3[] transformsPositions, Quaternion[] transformsOrientations, Quaternion worldOrientation, Vector3 worldPosition)
        {
            var ragdollComponent = Components.Get<MyCharacterRagdollComponent>();
            if (ragdollComponent == null) return;
            if (Physics == null) return;
            if (Physics.Ragdoll == null) return;
            if (ragdollComponent.RagdollMapper == null) return;
            if (!Physics.Ragdoll.InWorld) return;
            if (!ragdollComponent.RagdollMapper.IsActive) return;

            Debug.Assert(worldOrientation != null && worldOrientation != Quaternion.Zero, "Received invalid ragdoll orientation from server!");
            Debug.Assert(worldPosition != null && worldPosition != Vector3.Zero, "Received invalid ragdoll orientation from server!");
            Debug.Assert(transformsOrientations != null && transformsPositions != null, "Received empty ragdoll transformations from server!");
            Debug.Assert(transformsPositions.Length == transformsCount && transformsOrientations.Length == transformsCount, "Received ragdoll data count doesn't match!");

            Matrix worldMatrix = Matrix.CreateFromQuaternion(worldOrientation);
            worldMatrix.Translation = worldPosition;
            Matrix[] transforms = new Matrix[transformsCount];

            for (int i = 0; i < transformsCount; ++i)
            {
                transforms[i] = Matrix.CreateFromQuaternion(transformsOrientations[i]);
                transforms[i].Translation = transformsPositions[i];
            }

            ragdollComponent.RagdollMapper.UpdateRigidBodiesTransformsSynced(transformsCount, worldMatrix, transforms);
        }

        [Event, Reliable, Server, Broadcast]
        private void OnSwitchHelmet()
        {
            if (OxygenComponent != null)
            {
                OxygenComponent.SwitchHelmet();
                if (m_currentWeapon != null)
                    m_currentWeapon.UpdateSoundEmitter();
            }
        }


        public Vector3 GetLocalWeaponPosition()
        {
            return WeaponPosition.LogicalPositionLocalSpace;
        }

        private void ToolHeadTransformChanged()
        {
            MyEngineerToolBase tool =  m_currentWeapon as MyEngineerToolBase;
            if (tool != null && ControllerInfo.IsLocallyControlled() == false)
            {
                tool.UpdateSensorPosition();
            }
        }

        public void SyncHeadToolTransform(ref MatrixD headMatrix)
        {
            if (ControllerInfo.IsLocallyControlled())
            {
                MatrixD headMatrixLocal = headMatrix * PositionComp.WorldMatrixInvScaled;
                MyTransform transformToBeSent = new MyTransform(headMatrixLocal);
                transformToBeSent.Rotation = Quaternion.Normalize(transformToBeSent.Rotation);
                //m_localHeadTransformTool.Value = transformToBeSent;
            }
        }

        //public MatrixD GetSyncedToolTransform()
        //{
        //    return m_localHeadTransformTool.TransformMatrix * PositionComp.WorldMatrix;
        //}

        [Event, Reliable, Client]
        public void SwitchJetpack()
        {
            if (JetpackComp != null)
            {
                JetpackComp.SwitchThrusts();
            }
        }

        public Quaternion GetRotation()
        {
            Quaternion rot;
            if (JetpackRunning)
            {
                var mat = WorldMatrix;
                Quaternion.CreateFromRotationMatrix(ref mat, out rot);
            }
            else rot = Quaternion.CreateFromForwardUp(Physics.CharacterProxy.Forward, Physics.CharacterProxy.Up);
            return rot;
        }

        public void ApplyRotation(Quaternion rot)
        {
            var mat = MatrixD.CreateFromQuaternion(rot);
            if (JetpackRunning)
            {
                float rotationHeight = ModelCollision.BoundingBoxSizeHalf.Y;
                var physicsPosition = Physics.GetWorldMatrix().Translation;
                Vector3D translation = physicsPosition + WorldMatrix.Up * rotationHeight;
                mat.Translation = translation - mat.Up * rotationHeight;

                IsRotating = !WorldMatrix.EqualsFast(ref mat);

                WorldMatrix = mat;

                /*mat.Translation = physicsPosition;
                PositionComp.SetWorldMatrix(mat, Physics);*/
                ClearShapeContactPoints();
            }
            else
            {
                Physics.CharacterProxy.Forward = mat.Forward;
                Physics.CharacterProxy.Up = mat.Up;
            }
        }

        public override void SerializeControls(BitStream stream)
        {
            if (!IsDead)
            {
                stream.WriteBool(true);

                MyCharacterNetState charNetState;
                GetNetState(out charNetState);
                charNetState.Serialize(stream);

                if (VRage.MyCompilationSymbols.EnableNetworkPositionTracking &&
                    !MoveIndicator.Equals(Vector3.Zero, 0.001f))
                    VRage.Trace.MyTrace.Send(VRage.Trace.TraceWindow.MPositions, "xxxx " + MoveIndicator);
            }
            else stream.WriteBool(false);
        }

        private MyCharacterNetState m_lastNetState;
        public override void DeserializeControls(BitStream stream, bool outOfOrder)
        {
            var valid = stream.ReadBool();
            if (valid)
            {
                var netState = new MyCharacterNetState(stream); ;
                if (!outOfOrder)
                {
                    m_lastNetState = netState;
                    SetNetState(ref m_lastNetState, false);
                }
            }
            else m_lastNetState.Valid = false;
        }

        public override void ApplyLastControls()
        {
            if (m_lastNetState.Valid)
                CacheMove(ref m_lastNetState.MoveIndicator, ref m_lastNetState.Rotation);
        }
    }

    internal class MyBoneCapsuleInfo
    {
        public int Bone1 { get; set; }
        public int Bone2 { get; set; }

        // These are ordered with depth(Descendant) > depth(Ascendant)
        public int AscendantBone { get; set; }

        public int DescendantBone { get; set; }

        public float Radius { get; set; }
    }

    public class MyCharacterHitInfo
    {
        public MyCharacterHitInfo()
        {
            CapsuleIndex = -1;
        }

        public int CapsuleIndex { get; set; }

        public int BoneIndex { get; set; }

        public CapsuleD Capsule { get; set; }

        // Local coordinate system
        public Vector3 HitNormalBindingPose { get; set; }

        // Local coordinate system
        public Vector3 HitPositionBindingPose { get; set; }

        // Tranformation from binding to current pose
        public Matrix BindingTransformation { get; set; }

        public MyIntersectionResultLineTriangleEx Triangle { get; set; }

        public bool HitHead { get; set; }

        public void Reset()
        {
            CapsuleIndex = -1;
            BoneIndex = -1;
            Capsule = new CapsuleD();
            HitNormalBindingPose = new Vector3();
            HitPositionBindingPose = new Vector3();
            BindingTransformation = new Matrix();
            Triangle = new MyIntersectionResultLineTriangleEx();
            HitHead = false;
        }
    }
}