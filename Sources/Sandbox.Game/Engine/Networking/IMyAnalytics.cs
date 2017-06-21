using System;
using System.Collections.Generic;
using VRage;
using Sandbox.Common.ObjectBuilders;
using VRage.Game;
using VRage.Library.Utils;

namespace Sandbox.Engine.Networking
{
    public struct MyProcessStartAnalytics
    {
        public string OsVersion;
        public string OsPlatform;
        public string CpuInfo;
        public ulong TotalPhysMemBytes;
        public byte ProcessorCount;
        public MyGraphicsInfo GpuInfo;
        public MyAudioInfo AudioInfo;
        public string GameVersion;
        public bool HasDX11;
        public string Resolution;
        public string Fullscreen;
    }

    public struct MyPlanetNamesData
    {
        public string planetName;
        public string planetType;
    }

    public enum MyGameEntryEnum
    {
        Quickstart,
        Custom,
        Scenario,
        Tutorial,
        Load,
        Join
    }

    public struct MyGameplayStartAnalytics
    {
        public MyGameEntryEnum GameEntry;
        public MyGameModeEnum GameMode;
        public MyOnlineModeEnum OnlineMode;
        public List<string> ActiveMods;
        public bool IsScenario;
        public long LoadingDuration;
        public string WorldType;
        public string hostility;
        public bool Wolfs;
        public bool spaceSpiders;
        public bool voxelSupport;
        public bool destructibleBlocks;
        public bool destructibleVoxels;
        public bool drones;
        public bool jetpack;
        public bool encounters;
        public bool oxygen;
        public bool trashAutoRemoval;
        public bool toolShake;
        public float inventorySpace;
        public float welderSpeed;
        public float grinderSpeed;
        public float assemblerSpeed;
        public float assemblerEfficiency;
        public float refinerySpeed;
        public bool hostingPlayer;
        public string worldName;
        public uint floatingObjects;
        public string multiplayerType;
    }

    public struct MyGameplayEndAnalytics
    {
        public uint TimeOnFootSeconds;
        public uint TimeOnJetpackSeconds;
        public uint TimeOnSmallShipSeconds;
        public uint TimeOnBigShipSeconds;
        public uint AverageFramesPerSecond;
        public uint AverageUpdatesPerSecond;
        public uint MinFramesPerSecond;
        public uint MaxFramesPerSecond;
        public uint AverageLatencyMilliseconds;
        public uint TotalBlocksCreated;
        public uint TotalDamageDealt;
        public uint TotalPlayTime;
        public bool Scenario;
        public Dictionary<string, MyFixedPoint> TotalAmountMined;
        public string hostility;
        public bool Wolfs;
        public bool spaceSpiders;
        public bool voxelSupport;
        public bool destructibleBlocks;
        public bool destructibleVoxels;
        public bool drones;
        public bool jetpack;
        public bool encounters;
        public bool oxygen;
        public bool trashAutoRemoval;
        public bool toolShake;
        public float inventorySpace;
        public float welderSpeed;
        public float grinderSpeed;
        public float assemblerSpeed;
        public float assemblerEfficiency;
        public float refinerySpeed;
        public string worldName;
        public uint floatingObjects;
        public string multiplayerType;
        public float averageSimSpeedPlayer;
        public float averageSimSpeedServer;
    }

    public struct MyGraphicsInfo
    {
        public string GPUModelName;
        public ulong GPUMemory;
        public string AnisotropicFiltering;
        public string AntialiasingMode;
        public string FoliageDetails;
        public string ShadowQuality;
        public string TextureQuality;
        public string VoxelQuality;
        public float GrassDensityFactor;
    }

    public struct MyAudioInfo
    {
        public float MusicVolume;
        public float SoundVolume;
        public bool HudWarnings;
        public bool MuteWhenNotInFocus;
    }

    public interface IMyAnalytics
    {
        void IdentifyPlayer(ulong playerId, string playerName, bool isOnline);

        void ReportProcessStart(MyProcessStartAnalytics attributes, bool firstRun = false, bool dedicatedServer = false);
        void ReportProcessEnd();

        void ReportGameplayStart(MyGameplayStartAnalytics attributes, bool dedicatedServer = false);
        void ReportGameplayEnd(MyGameplayEndAnalytics attributes);

        void ReportActivityStart(string activityName, string activityFocus, string activityType, string activityItemUsage, bool expectActivityEnd = true, string planetName = "", string planetType = "", float simSpeedPlayer = 1f, float simSpeedServer = 1f);
        void ReportActivityEnd(string activityName, string planetName = "", string planetType = "", float simSpeedPlayer = 1f, float simSpeedServer = 1f);

        void ReportPlayerDeath(string deathType, string deathCause, string planetName = "", string planetType = "", bool scenario = false);

        void ReportTutorialScreen(string initiatedFrom);

        void ReportTutorialStart(string tutorialName);
        void ReportTutorialStep(string stepName, int stepNumber);
        void ReportTutorialEnd();
        void ReportServerStatus(int playerCount, int maxPlayers, float simSpeedServer, int entitiesCount, int gridsCount, int blocksCount, int movingGridsCount, string hostName);
        void ReportServerStart(int maxPlayers, string hostName);

        void FlushAndDispose();
    }
}
