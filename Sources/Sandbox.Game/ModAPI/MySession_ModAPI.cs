using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI.Interfaces;
using VRage.Utils;
using VRage.Library.Utils;

namespace Sandbox.Game.World
{
    sealed partial class MySession : IMySession
    {
        IMyVoxelMaps IMySession.VoxelMaps
        {
            get { return VoxelMaps; }
        }

        ModAPI.Interfaces.IMyCameraController IMySession.CameraController
        {
            get { return CameraController; }
        }

        float IMySession.AssemblerEfficiencyMultiplier
        {
            get { return AssemblerEfficiencyMultiplier; }
        }

        float IMySession.AssemblerSpeedMultiplier
        {
            get { return AssemblerSpeedMultiplier; }
        }

        bool IMySession.AutoHealing
        {
            get { return AutoHealing; }
        }

        uint IMySession.AutoSaveInMinutes
        {
            get { return AutoSaveInMinutes; }
        }

        void IMySession.BeforeStartComponents()
        {
            BeforeStartComponents();
        }

        bool IMySession.CargoShipsEnabled
        {
            get { return CargoShipsEnabled; }
        }

        bool IMySession.ClientCanSave
        {
            get { return ClientCanSave; }
        }

        bool IMySession.CreativeMode
        {
            get { return CreativeMode; }
        }

        string IMySession.CurrentPath
        {
            get { return CurrentPath; }
        }

        string IMySession.Description
        {
            get
            {
                return Description;
            }
            set
            {
                Description = value;
            }
        }

        void IMySession.Draw()
        {
            Draw();
        }

        TimeSpan IMySession.ElapsedPlayTime
        {
            get { return ElapsedPlayTime; }
        }

        bool IMySession.EnableCopyPaste
        {
            get { return EnableCopyPaste; }
        }

        Common.ObjectBuilders.MyEnvironmentHostilityEnum IMySession.EnvironmentHostility
        {
            get { return EnvironmentHostility; }
        }

        DateTime IMySession.GameDateTime
        {
            get
            {
                return GameDateTime;
            }
            set
            {
                GameDateTime = value;
            }
        }

        void IMySession.GameOver()
        {
            GameOver();
        }

        void IMySession.GameOver(MyStringId? customMessage)
        {
            GameOver(customMessage);
        }

        Common.ObjectBuilders.MyObjectBuilder_Checkpoint IMySession.GetCheckpoint(string saveName)
        {
            return GetCheckpoint(saveName);
        }

        Common.ObjectBuilders.MyObjectBuilder_Sector IMySession.GetSector()
        {
            return GetSector();
        }

        Dictionary<string, byte[]> IMySession.GetVoxelMapsArray()
        {
            return GetVoxelMapsArray();
        }

        Common.ObjectBuilders.MyObjectBuilder_World IMySession.GetWorld()
        {
            return GetWorld();
        }

        float IMySession.GrinderSpeedMultiplier
        {
            get { return GrinderSpeedMultiplier; }
        }

        float IMySession.HackSpeedMultiplier
        {
            get { return HackSpeedMultiplier; }
        }

        float IMySession.InventoryMultiplier
        {
            get { return InventoryMultiplier; }
        }

        bool IMySession.IsCameraAwaitingEntity
        {
            get
            {
                return IsCameraAwaitingEntity;
            }
            set
            {
                IsCameraAwaitingEntity = value;
            }
        }

        bool IMySession.IsPausable()
        {
            return IsPausable();
        }

        short IMySession.MaxFloatingObjects
        {
            get { return MaxFloatingObjects; }
        }

        short IMySession.MaxPlayers
        {
            get { return MaxPlayers; }
        }

        bool IMySession.MultiplayerAlive
        {
            get
            {
                return MultiplayerAlive;
            }
            set
            {
                MultiplayerAlive = value;
            }
        }

        bool IMySession.MultiplayerDirect
        {
            get
            {
                return MultiplayerDirect;
            }
            set
            {
                MultiplayerDirect = value;
            }
        }

        double IMySession.MultiplayerLastMsg
        {
            get
            {
                return MultiplayerLastMsg;
            }
            set
            {
                MultiplayerLastMsg = value;
            }
        }

        string IMySession.Name
        {
            get
            {
                return Name;
            }
            set
            {
                Name = value;
            }
        }

        float IMySession.NegativeIntegrityTotal
        {
            get
            {
                return NegativeIntegrityTotal;
            }
            set
            {
                NegativeIntegrityTotal = value;
            }
        }

        Common.ObjectBuilders.MyOnlineModeEnum IMySession.OnlineMode
        {
            get { return OnlineMode; }
        }

        string IMySession.Password
        {
            get
            {
                return Password;
            }
            set
            {
                Password = value;
            }
        }

        float IMySession.PositiveIntegrityTotal
        {
            get
            {
                return PositiveIntegrityTotal;
            }
            set
            {
                PositiveIntegrityTotal = value;
            }
        }

        float IMySession.RefinerySpeedMultiplier
        {
            get { return RefinerySpeedMultiplier; }
        }

        void IMySession.RegisterComponent(Common.MySessionComponentBase component, Common.MyUpdateOrder updateOrder, int priority)
        {
            RegisterComponent(component, updateOrder, priority);
        }

        bool IMySession.Save(string customSaveName)
        {
            return Save(customSaveName);
        }

        void IMySession.SetAsNotReady()
        {
            SetAsNotReady();
        }

        bool IMySession.ShowPlayerNamesOnHud
        {
            get { return ShowPlayerNamesOnHud; }
        }

        bool IMySession.SurvivalMode
        {
            get { return SurvivalMode; }
        }

        bool IMySession.ThrusterDamage
        {
            get { return ThrusterDamage; }
        }

        string IMySession.ThumbPath
        {
            get { return ThumbPath; }
        }

        TimeSpan IMySession.TimeOnBigShip
        {
            get { return TimeOnBigShip; }
        }

        TimeSpan IMySession.TimeOnFoot
        {
            get { return TimeOnFoot; }
        }

        TimeSpan IMySession.TimeOnJetpack
        {
            get { return TimeOnJetpack; }
        }

        TimeSpan IMySession.TimeOnSmallShip
        {
            get { return TimeOnSmallShip; }
        }

        void IMySession.Unload()
        {
            Unload();
        }

        void IMySession.UnloadDataComponents()
        {
            UnloadDataComponents();
        }

        void IMySession.UnloadMultiplayer()
        {
            UnloadMultiplayer();
        }

        void IMySession.UnregisterComponent(Common.MySessionComponentBase component)
        {
            UnregisterComponent(component);
        }

        void IMySession.Update(MyTimeSpan time)
        {
            Update(time);
        }

        void IMySession.UpdateComponents()
        {
            UpdateComponents();
        }

        bool IMySession.WeaponsEnabled
        {
            get { return WeaponsEnabled; }
        }

        float IMySession.WelderSpeedMultiplier
        {
            get { return WelderSpeedMultiplier; }
        }

        ulong? IMySession.WorkshopId
        {
            get { return WorkshopId; }
        }

        IMyPlayer IMySession.Player
        { 
            get { return LocalHumanPlayer; } 
        }

        IMyControllableEntity IMySession.ControlledObject 
        {
            get { return ControlledEntity; } 
        }

        Common.ObjectBuilders.MyObjectBuilder_SessionSettings IMySession.SessionSettings
        {
            get { return Settings;}
        }


        IMyFactionCollection IMySession.Factions
        {
            get { return Factions;}
        }

        IMyCamera IMySession.Camera
        {
            get { return MySector.MainCamera; }
        }

        public IMyConfig Config
        {
            get { return MySandboxGame.Config; }
        }

        IMyGpsCollection IMySession.GPS
        {
            get { return MySession.Static.Gpss; }
        }

        event Action IMySession.OnSessionReady
        {
            add { MySandboxGame.OnSessionReady += value; }
            remove { MySandboxGame.OnSessionReady -= value; }
        }
    }
}
