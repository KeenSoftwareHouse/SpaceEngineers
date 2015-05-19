using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Utils;
using VRage.Utils;

namespace Sandbox.ModAPI
{
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
        bool ClientCanSave { get; }
        bool CreativeMode { get; }
        string CurrentPath { get; }
        string Description { get; set; }
        IMyCamera Camera { get; }

        /// <summary>
        /// Obtaining values from config is slow and can allocate memory!
        /// Do it only when necessary.
        /// </summary>
        IMyConfig Config { get; }
        void Draw();
        TimeSpan ElapsedPlayTime { get; }
        bool EnableCopyPaste { get; }
        Sandbox.Common.ObjectBuilders.MyEnvironmentHostilityEnum EnvironmentHostility { get; }
        DateTime GameDateTime { get; set; }
        void GameOver();
        void GameOver(MyStringId? customMessage);
        Sandbox.Common.ObjectBuilders.MyObjectBuilder_Checkpoint GetCheckpoint(string saveName);
        Sandbox.Common.ObjectBuilders.MyObjectBuilder_Sector GetSector();
        System.Collections.Generic.Dictionary<string, byte[]> GetVoxelMapsArray();
        Sandbox.Common.ObjectBuilders.MyObjectBuilder_World GetWorld();
        float GrinderSpeedMultiplier { get; }
        float HackSpeedMultiplier { get; }
        float InventoryMultiplier { get; }
        bool IsCameraAwaitingEntity { get; set; }
        bool IsPausable();
        //void LoadDataComponents(IMyLocalPlayer localPlayer = null);
        short MaxFloatingObjects { get; }
        short MaxPlayers { get; }
        bool MultiplayerAlive { get; set; }
        bool MultiplayerDirect { get; set; }
        double MultiplayerLastMsg { get; set; }
        string Name { get; set; }
        float NegativeIntegrityTotal { get; set; }
        Sandbox.Common.ObjectBuilders.MyOnlineModeEnum OnlineMode { get; }
        string Password { get; set; }
        float PositiveIntegrityTotal { get; set; }
        float RefinerySpeedMultiplier { get; }
        void RegisterComponent(Sandbox.Common.MySessionComponentBase component, Sandbox.Common.MyUpdateOrder updateOrder, int priority);
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
        void UnregisterComponent(Sandbox.Common.MySessionComponentBase component);
        void Update(MyTimeSpan time);
        void UpdateComponents();
        bool WeaponsEnabled { get; }
        float WelderSpeedMultiplier { get; }
        ulong? WorkshopId { get; }
        IMyVoxelMaps VoxelMaps { get; }
        IMyPlayer Player { get; }
        IMyControllableEntity ControlledObject { get; }
        Sandbox.Common.ObjectBuilders.MyObjectBuilder_SessionSettings SessionSettings { get;}
        IMyFactionCollection Factions { get;}
        IMyGpsCollection GPS { get; }
        event Action OnSessionReady;
    }
}
