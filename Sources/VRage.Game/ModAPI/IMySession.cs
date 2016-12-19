using System;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.ModAPI.Interfaces;
using VRage.Library.Utils;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace VRage.Game.ModAPI
{
    /// <summary>
    /// Describes what permissions a user has
    /// </summary>
    public enum MyPromoteLevel
    {
        //note: if you add items, always preserve order from lowest to highest permission
        /// <summary>
        /// Normal players
        /// </summary>
        None = 0,
        /// <summary>
        /// Can edit scripts when the scripter role is enabled
        /// </summary>
        Scripter,
        /// <summary>
        /// Can kick and ban players, has access to 'Show All Players' option in Admin Tools menu
        /// </summary>
        Moderator,
        /// <summary>
        /// Has access to Space Master tools
        /// </summary>
        SpaceMaster,
        /// <summary>
        /// Has access to Admin tools
        /// </summary>
        Admin,
        /// <summary>
        /// Admins listed in server config, cannot be demoted
        /// </summary>
        Owner,
    }

    public interface IMySession
    {
        float AssemblerEfficiencyMultiplier { get; }
        float AssemblerSpeedMultiplier { get; }
        bool AutoHealing { get; }
        uint AutoSaveInMinutes { get; }
        void BeforeStartComponents();
        //event Action<IMyCameraController, IMyCameraController> CameraAttachedToChanged;
        IMyCameraController CameraController { get; }
        bool CargoShipsEnabled { get; }
        [Obsolete("Client saving not supported anymore")]
        bool ClientCanSave { get; }
        bool CreativeMode { get; }
        string CurrentPath { get; }
        string Description { get; set; }
        IMyCamera Camera { get; }
        IMyPlayer LocalHumanPlayer { get; }

        /// <summary>
        /// Obtaining values from config is slow and can allocate memory!
        /// Do it only when necessary.
        /// </summary>
        IMyConfig Config { get; }
        void Draw();
        TimeSpan ElapsedPlayTime { get; }
        bool EnableCopyPaste { get; }
        MyEnvironmentHostilityEnum EnvironmentHostility { get; }
        DateTime GameDateTime { get; set; }
        void GameOver();
        void GameOver(MyStringId? customMessage);
        MyObjectBuilder_Checkpoint GetCheckpoint(string saveName);
        MyObjectBuilder_Sector GetSector();
        System.Collections.Generic.Dictionary<string, byte[]> GetVoxelMapsArray();
        MyObjectBuilder_World GetWorld();
        float GrinderSpeedMultiplier { get; }
        float HackSpeedMultiplier { get; }
        float InventoryMultiplier { get; }
        bool IsCameraAwaitingEntity { get; set; }
        List<MyObjectBuilder_Checkpoint.ModItem> Mods { get; set; }
        bool IsPausable();
        bool IsServer { get; }
        //void LoadDataComponents(IMyLocalPlayer localPlayer = null);
        short MaxFloatingObjects { get; }
        short MaxBackupSaves { get; }
        short MaxPlayers { get; }
        bool MultiplayerAlive { get; set; }
        bool MultiplayerDirect { get; set; }
        double MultiplayerLastMsg { get; set; }
        string Name { get; set; }
        float NegativeIntegrityTotal { get; set; }
        MyOnlineModeEnum OnlineMode { get; }
        string Password { get; set; }
        float PositiveIntegrityTotal { get; set; }
        float RefinerySpeedMultiplier { get; }
        void RegisterComponent(VRage.Game.Components.MySessionComponentBase component, VRage.Game.Components.MyUpdateOrder updateOrder, int priority);
        //void RegisterComponentsFromAssembly(System.Reflection.Assembly assembly);
        //bool Save(out MySessionSnapshot snapshot, string customSaveName = null);
        bool Save(string customSaveName = null);
        void SetAsNotReady();
        bool ShowPlayerNamesOnHud { get; }
        //void StartServer(Sandbox.Engine.Multiplayer.MyMultiplayerBase multiplayer);
        bool SurvivalMode { get; }
        //Sandbox.Game.Multiplayer.MySyncLayer SyncLayer { get; }
        bool ThrusterDamage { get; }
        string ThumbPath { get; }
        TimeSpan TimeOnBigShip { get; }
        TimeSpan TimeOnFoot { get; }
        TimeSpan TimeOnJetpack { get; }
        TimeSpan TimeOnSmallShip { get; }
        void Unload();
        void UnloadDataComponents();
        void UnloadMultiplayer();
        void UnregisterComponent(VRage.Game.Components.MySessionComponentBase component);
        void Update(MyTimeSpan time);
        void UpdateComponents();
        bool WeaponsEnabled { get; }
        float WelderSpeedMultiplier { get; }
        ulong? WorkshopId { get; }
        IMyVoxelMaps VoxelMaps { get; }
        IMyPlayer Player { get; }
        IMyControllableEntity ControlledObject { get; }
        MyObjectBuilder_SessionSettings SessionSettings { get;}
        IMyFactionCollection Factions { get;}
        IMyDamageSystem DamageSystem { get; }
        IMyGpsCollection GPS { get; }
        event Action OnSessionReady;
        event Action OnSessionLoading;
        BoundingBoxD WorldBoundaries { get; }
        
        /// <summary>
        /// Gets the local player's promote level.
        /// </summary>
        MyPromoteLevel PromoteLevel { get; }

        /// <summary>
        /// Gets a remote player's promote level.
        /// </summary>
        /// <param name="steamId"></param>
        /// <returns></returns>
        MyPromoteLevel GetUserPromoteLevel(ulong steamId);

        /// <summary>
        /// Checks if the local player is an admin or is promoted to space master (or higher).
        /// </summary>
        bool HasCreativeRights { get; }

        /// <summary>
        /// Checks if a given player is an admin (or higher).
        /// </summary>
        /// <param name="steamId"></param>
        /// <returns></returns>
        bool IsUserAdmin( ulong steamId );
        
        [Obsolete("Use GetUserPromoteLevel")]
        bool IsUserPromoted( ulong steamId );

        [Obsolete("Use HasCreativeRights")]
        bool HasAdminPrivileges { get; }

        /// <summary>
        /// Change the update order of a session component.
        /// 
        /// There is a proxy for this method in the session component itself.
        /// </summary>
        /// <param name="component">The component to set the update order for</param>
        /// <param name="order">The update order</param>
        void SetComponentUpdateOrder(MySessionComponentBase component, MyUpdateOrder order);
    }
}
