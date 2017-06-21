using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using System;
using System.Diagnostics;
using VRage.Game.Components;
using VRage.Data.Audio;
using VRage.Utils;
using VRageMath;
using VRage.Game;

namespace Sandbox.Game
{
    public struct MyCharacterMovementSettings
    {
        public float WalkAcceleration; //m/ss
        public float WalkDecceleration; //m/ss
        public float SprintAcceleration; //m/ss
        public float SprintDecceleration; //m/ss
    }

    public struct MyCollisionParticleSettings
    {
        public MyParticleEffectsIDEnum LargeGridClose;
        public MyParticleEffectsIDEnum LargeGridDistant;
        public MyParticleEffectsIDEnum SmallGridClose;
        public MyParticleEffectsIDEnum SmallGridDistant;
        public MyParticleEffectsIDEnum VoxelCollision;
        public float CloseDistanceSq;
        public float Scale;
    }

    public struct MyDestructionParticleSettings
    {
        public MyParticleEffectsIDEnum DestructionSmokeLarge;
        public MyParticleEffectsIDEnum DestructionHit;
        public float CloseDistanceSq;
        public float Scale;
    }

    public struct MyGUISettings
    {
        public bool EnableToolbarConfigScreen;
        public bool EnableTerminalScreen;
        public bool MultipleSpinningWheels;
        public Type HUDScreen;
        public Type ToolbarConfigScreen; // aka G-screen
        public Type ToolbarControl;
        public Type OptionsScreen;
        public Type CustomWorldScreen;
        public Type ScenarioScreen;
        public Type EditWorldSettingsScreen;
        public Type HelpScreen;
        public Type VoxelMapEditingScreen;
        public Type GameplayOptionsScreen;
        public Type ScenarioLobbyClientScreen;
        public Type InventoryScreen;
        public Type AdminMenuScreen;
        public Type FactionScreen;
        public Type CreateFactionScreen;
        public Type PlayersScreen;
        public Type MainMenu;
        public Type PerformanceWarningScreen;

        public string[] MainMenuBackgroundVideos;

        //  Loading - random screen index (this is about Background001.png...). Max is the number of highest file.
        public Vector2I LoadingScreenIndexRange;
    }

    // TODO: CH: This is temporary until we remove things from Sandbox.Game to a SE library
    public enum GameEnum
    {
        UNKNOWN_GAME,
        SE_GAME,
        ME_GAME,
        VRS_GAME
    }

    public struct MyBasicGameInfo
    {
        public int? GameVersion;
        public string GameName;
        /// <summary>
        /// Game name without any spaces and generally usable for folder names.
        /// </summary>
        public string GameNameSafe;
        public string ApplicationName;
        public string GameAcronym;
        public string MinimumRequirementsWeb;
        public string SplashScreenImage;

        public bool CheckIsSetup()
        {
            bool retval = true;

            var fields = this.GetType().GetFields();
            foreach (var field in fields)
            {
                bool fieldIsSetup = field.GetValue(this) != null;
                Debug.Assert(fieldIsSetup, "The field " + field.Name + " of MyperGameSettings.BasicGameInfo was not initialized!");

                retval = retval && fieldIsSetup;
            }

            return retval;
        }
    }

    public static class MyPerGameSettings
    {
        public static MyBasicGameInfo BasicGameInfo = new MyBasicGameInfo();

        public static GameEnum Game = GameEnum.UNKNOWN_GAME;
        public static string GameName { get { return BasicGameInfo.GameName; } }
        public static string GameNameSafe { get { return BasicGameInfo.GameNameSafe; } }
        public static string GameWebUrl = "www.SpaceEngineersGame.com";
        public static string LocalizationWebUrl = "http://www.spaceengineersgame.com/localization.html";
        public static string ChangeLogUrl = "http://mirror.keenswh.com/news/SpaceEngineersChangelog.xml";
        public static string ChangeLogUrlDevelop = "http://mirror.keenswh.com/news/SpaceEngineersChangelogDevelop.xml";
        public static string EShopUrl = "https://shop.keenswh.com/";
        public static string MinimumRequirementsPage { get { return BasicGameInfo.MinimumRequirementsWeb; } }
        public static bool RequiresDX11 = false;
        public static string GameIcon;
        public static bool EnableGlobalGravity;
        public static bool ZoomRequiresLookAroundPressed = true;

        public static bool EnablePregeneratedAsteroidHack = false;
        public static bool SendLogToKeen = true;

        public static string GA_Public_GameKey = String.Empty;
        public static string GA_Public_SecretKey = String.Empty;
        public static string GA_Dev_GameKey = String.Empty;
        public static string GA_Dev_SecretKey = String.Empty;
        public static string GA_Pirate_GameKey = String.Empty;
        public static string GA_Pirate_SecretKey = String.Empty;
        public static string GA_Other_GameKey = String.Empty;
        public static string GA_Other_SecretKey = String.Empty;

        public static string GameModAssembly;
        public static string GameModObjBuildersAssembly;
        public static string GameModBaseObjBuildersAssembly;
        public static string SandboxAssembly = "Sandbox.Common.dll";
        public static string SandboxGameAssembly = "Sandbox.Game.dll";

        public static int LoadingScreenQuoteCount = 71;
        public static bool OffsetVoxelMapByHalfVoxel = false;

        public static bool UseVolumeLimiter = false;
        public static bool UseMusicController = false;
        public static bool UseReverbEffect = false;

        public static bool UseSameSoundLimiter = false;
        public static bool UseNewDamageEffects = false;

        public static bool RestrictSpectatorFlyMode = false;

        public static float MaxFrameRate = 120;

        private static Type m_isoMesherType = typeof(MyDualContouringMesher);
        //private static Type m_isoMesherType = typeof(MyMarchingCubesMesher);
        public static Type IsoMesherType
        {
            get
            {
                Debug.Assert(typeof(IMyIsoMesher).IsAssignableFrom(m_isoMesherType));
                return m_isoMesherType;
            }
            set
            {
                Debug.Assert(typeof(IMyIsoMesher).IsAssignableFrom(m_isoMesherType));
                m_isoMesherType = value;
            }
        }

        // Minimum mass a floating object must have to be able to push large ships.
        public static double MinimumLargeShipCollidableMass = 1000;

        public static float? ConstantVoxelAmbient;

        private const float DefaultMaxWalkSpeed = 6.0f;
        private const float DefaultMaxCrouchWalkSpeed = 4.0f;
        public static MyCharacterMovementSettings CharacterMovement = new MyCharacterMovementSettings()
        {
            WalkAcceleration       = 50,  //m/ss
            WalkDecceleration      = 10,  //m/ss
            SprintAcceleration     = 100, //m/ss
            SprintDecceleration    = 20,  //m/ss
        };

        public static MyCollisionParticleSettings CollisionParticle = new MyCollisionParticleSettings()
        {
            LargeGridClose = MyParticleEffectsIDEnum.CollisionSparksLargeClose,
            LargeGridDistant = MyParticleEffectsIDEnum.CollisionSparksLargeDistant,
            SmallGridClose = MyParticleEffectsIDEnum.Collision_Sparks,
            SmallGridDistant = MyParticleEffectsIDEnum.Collision_Sparks,
            CloseDistanceSq = 20*20,
            Scale = 1
        };

        public static MyDestructionParticleSettings DestructionParticle = new MyDestructionParticleSettings()
        {
            DestructionSmokeLarge = MyParticleEffectsIDEnum.DestructionSmokeLarge,
            DestructionHit = MyParticleEffectsIDEnum.DestructionHit,
            CloseDistanceSq = 20 * 20,
            Scale = 1
        };

        public static bool UseGridSegmenter = true;
        public static bool Destruction = false;
        public static float PhysicsConvexRadius = 0.05f;
        public static bool PhysicsNoCollisionLayerWithDefault = false;
        //Be careful with setting these values to 0, object will never get to sleep state if not stopped manually
        public static float DefaultLinearDamping = 0.0f;
        public static float DefaultAngularDamping = 0.1f;
        ////////////////
        public static bool BallFriendlyPhysics = false;

        public static bool DoubleKinematicForLargeGrids = true;
        public static bool CharacterStartsOnVoxel = false;
        public static bool LimitedWorld = false;
        public static bool EnableCollisionSparksEffect = true;

        private static bool m_useAnimationInsteadOfIK = false;

        public static bool WorkshopUseUGCEnumerate = true;
        public static string SteamGameServerGameDir = "Space Engineers";
        public static string SteamGameServerProductName = "Space Engineers";
        public static string SteamGameServerDescription = "Space Engineers";

        /// <summary>
        /// Simplest way to get rid of terminal for anything but SE.
        /// Terminal has leaked into a lot of places and should get decoupled somehow.
        /// </summary>
        public static bool TerminalEnabled = true;

        public static MyGUISettings GUI = new MyGUISettings()
        {
            EnableTerminalScreen = true,
            EnableToolbarConfigScreen = true,
            MultipleSpinningWheels = true,
            LoadingScreenIndexRange = new Vector2I(1,24),
            HUDScreen = typeof(Sandbox.Game.Gui.MyGuiScreenHudSpace),
            ToolbarConfigScreen = typeof(Sandbox.Game.Gui.MyGuiScreenCubeBuilder),
            ToolbarControl = typeof(Sandbox.Game.Screens.Helpers.MyGuiControlToolbar),
            CustomWorldScreen = typeof(Sandbox.Game.Gui.MyGuiScreenWorldSettings),
            ScenarioScreen = typeof(Sandbox.Game.Gui.MyGuiScreenScenario),
            EditWorldSettingsScreen = typeof(Sandbox.Game.Gui.MyGuiScreenWorldSettings),
            HelpScreen = typeof(Sandbox.Game.Gui.MyGuiScreenHelpSpace),
            VoxelMapEditingScreen = typeof(Sandbox.Game.Gui.MyGuiScreenDebugSpawnMenu),
            ScenarioLobbyClientScreen = typeof(Sandbox.Game.Screens.MyGuiScreenScenarioMpClient),
            AdminMenuScreen = typeof(Sandbox.Game.Gui.MyGuiScreenAdminMenu),
            CreateFactionScreen = typeof(Sandbox.Game.Gui.MyGuiScreenCreateOrEditFaction),
            PlayersScreen = typeof(Sandbox.Game.Gui.MyGuiScreenPlayers),
        };

        // Artificial intelligence
        public static Type PathfindingType = null;
        public static Type BotFactoryType = null;
        public static bool EnableAi = false;
        public static bool EnablePathfinding = false;
        public static bool NavmeshPresumesDownwardGravity = false;

        public static Type ControlMenuInitializerType = null;
        public static Type CompatHelperType = typeof(Sandbox.Game.World.MySessionCompatHelper);

        public static MyCredits Credits = new MyCredits();

        public static MyMusicTrack? MainMenuTrack = null;

        public static bool EnableObjectExport = true;

        public static bool TryConvertGridToDynamicAfterSplit = false;
        public static bool AnimateOnlyVisibleCharacters = false;

        // DAMAGE SETTINGS
        public static float CharacterDamageMinVelocity = 12.0f;    // minimal speed of character to cause damage to itself 3 blocks ~ 16.7 m/s, 2 blocks 13.47 m/s 1 block 9.0 m/s
        public static float CharacterDamageDeadlyDamageVelocity = 16.0f; // speed to cause deadly damage
        public static float CharacterDamageMediumDamageVelocity = 13.0f; // speed to cause mediun damage when character falls
        public static float CharacterDamageHitObjectMinMass = 10f;    // minimal weight of the object to cause damage when squeezing the character
        public static float CharacterDamageHitObjectMinVelocity = 8.5f;   // minimal speed of object to cause damage to character 25 km/h ~ 7 m/s 
        public static float CharacterDamageHitObjectMediumEnergy = 100; // energy of the colliding object with the character to cause the medium damage
        public static float CharacterDamageHitObjectSmallEnergy = 80;
        public static float CharacterDamageHitObjectCriticalEnergy = 200;
        public static float CharacterDamageHitObjectDeadlyEnergy = 500;
        public static float CharacterSmallDamage = 10;  // amount of health points for that kind of damage
        public static float CharacterMediumDamage = 30;
        public static float CharacterCriticalDamage = 70;
        public static float CharacterDeadlyDamage = 100;
        public static float CharacterSqueezeDamageDelay = 1f; // delay before applying damage on character when squeezing
        public static float CharacterSqueezeMinMass = 200f; // minimal mass to cause squeeze on character
        public static float CharacterSqueezeMediumDamageMass = 1000;
        public static float CharacterSqueezeCriticalDamageMass = 3000;
        public static float CharacterSqueezeDeadlyDamageMass = 5000;
        
        public static bool CharacterSuicideEnabled = true;

        public static Func<bool> ConstrainInventory = () => Sandbox.Game.World.MySession.Static.SurvivalMode;
        
        public static bool SwitchToSpectatorCameraAfterDeath = false;
        public static bool SimplePlayerNames = false;
        public static Type CharacterDetectionComponent;

        public static string BugReportUrl = "http://forum.keenswh.com/forums/bug-reports.326950";

        public static bool EnableScenarios = false;

        public static bool EnableRagdollModels = true;

        public static bool ShowObfuscationStatus = true;

        public static bool EnableRagdollInJetpack = false;

        public static bool InventoryMass = false;

        public static bool EnableCharacterCollisionDamage = false;
        public static MyStringId DefaultGraphicsRenderer;

        public static bool EnableWelderAutoswitch = false;

        public static Type VoiceChatLogic = null;
        public static bool VoiceChatEnabled = false;
        public static bool EnableMutePlayer = false;    // mute checkox on players page + muting of voicechat of selected players

        public static bool EnableJumpDrive = false;
        public static bool EnableShipSoundSystem = false;

        public static Engine.Networking.IMyAnalytics AnalyticsTracker = null; // = MyInfinarioAnalytics.Instance;
        
        public static bool EnableFloatingObjectsActiveSync = false;
        public static string InfinarioOfficial;
        public static string InfinarioDebug;
        public static bool DisableAnimationsOnDS = true;
        
        public static float CharacterGravityMultiplier = 1.0f;

        public static bool BlockForVoxels = false;
        public static bool AlwaysShowAvailableBlocksOnHud = false;

        public static float MaxAntennaDrawDistance = 500000;

        public static bool EnableResearch = false;

        public static VRageRender.MyRenderDeviceSettings? DefaultRenderDeviceSettings;

        // Factions
        public static MyRelationsBetweenFactions DefaultFactionRelationship = MyRelationsBetweenFactions.Enemies;

        /// <summary>
        /// MULTIPLAYER RELATED SETTINGS
        /// </summary>
        public static bool MultiplayerEnabled = true;
        public static Type ClientStateType = typeof(MyClientState);

        public static RigidBodyFlag NetworkCharacterType = RigidBodyFlag.RBF_KINEMATIC;
        public static bool EnableKinematicMPCharacter = !MyFakes.MULTIPLAYER_CLIENT_PHYSICS;
        public static RigidBodyFlag GridRBFlagOnClients = MyFakes.MULTIPLAYER_CLIENT_PHYSICS ? RigidBodyFlag.RBF_DEFAULT : RigidBodyFlag.RBF_KINEMATIC;

        // MP: CLEANUP!!
        public static bool EnablePerFrameCharacterSync = false;
        public static float NetworkCharacterScale = 1.0f;
        public static int NetworkCharacterCollisionLayer = MyPhysics.CollisionLayers.CharacterNetworkCollisionLayer;

        /// <summary>
        /// CLIENT ANIMATING / SIMULATING
        /// </summary>
        public static RigidBodyFlag LargeGridRBFlag = MyFakes.ENABLE_DOUBLED_KINEMATIC ? RigidBodyFlag.RBF_DOUBLED_KINEMATIC : RigidBodyFlag.RBF_DEFAULT;
    }
}
