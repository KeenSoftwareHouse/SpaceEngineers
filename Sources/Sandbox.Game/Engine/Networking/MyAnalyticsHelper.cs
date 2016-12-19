using Sandbox.Common;
using Sandbox.Engine.Platform.VideoMode;
using Sandbox.Game;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage.Utils;
using VRageMath;
using VRage.Win32;
using VRageRender;
using Sandbox.Game.Multiplayer;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Library.Utils;
using VRage.Library;

namespace Sandbox.Engine.Networking
{
    /// <summary>
    /// Helper class to call used analytics in a game (implementation of IMyAnalytics interface)
    /// </summary>
    public static class MyAnalyticsHelper
    {
        // Change this if you want to completely disable (Infinario) analytics or enable for debug, uncommet the line below
        public const string ANALYTICS_CONDITION_STRING = MyFinalBuildConstants.IS_OFFICIAL ? "WINDOWS" : "_NOT_DEFINED_SYMBOL_";
        //public const string ANALYTICS_CONDITION_STRING = "WINDOWS";

        #region Helper methods and fields

        private static readonly List<string> m_usedMods = new List<string>();
        private static MyDamageInformation m_lastDamageInformation = new MyDamageInformation
        {
            Type = MyStringHash.NullOrEmpty
        };
        private static bool m_tutorialStarted;

        private static MyGameEntryEnum m_entry;
        private static bool m_scenarioFlag;
        private static DateTime m_loadingStartedAt;
        private static bool m_loadingStarted = false;

        //Sanity checks
        private static bool ReportChecksProcessStart = false;
        private static bool ReportChecksProcessEnd = false;
        private static int ReportChecksActivityStart = 0;
        private static int ReportChecksActivityEnd = 0;
        private static int ReportChecksLastMinute = DateTime.UtcNow.Minute;
        private static int ReportChecksGameplayStart = -1;
        private static int ReportChecksGameplayEnd = -1;

        private static float m_lastMinuteUpdate = 0f;

        private static bool SanityCheckAmountPerMinute(int reportCount, int limit)
        {
            if (DateTime.UtcNow.Minute != ReportChecksLastMinute)
            {
                ReportChecksLastMinute = DateTime.UtcNow.Minute;
                ReportChecksActivityStart = 0;
                ReportChecksActivityEnd = 0;
            }
            if (reportCount < limit)
                return false;
            else
                return true;
        }

        private static bool SanityCheckOnePerMinute(ref int lastInstance)
        {
            int now = DateTime.UtcNow.Hour * 60 + DateTime.UtcNow.Minute;
            if (now != lastInstance)
            {
                lastInstance = now;
                return false;
            }
            else
            {
                return true;
            }
        }

        private static MyProcessStartAnalytics GetProcessStartAnalyticsData()
        {
            MyProcessStartAnalytics data = new MyProcessStartAnalytics();
            try
            {
                var cpus = new ManagementObjectSearcher("root\\CIMV2", "SELECT Name FROM Win32_Processor").Get();
                // We're just reporting the first 
                var cpuName = cpus.Cast<ManagementObject>().First()["Name"].ToString();

#if XB1
                System.Diagnostics.Debug.Assert(false);
#else // !XB1
                var memoryInfo = new WinApi.MEMORYSTATUSEX();
                WinApi.GlobalMemoryStatusEx(memoryInfo);
#endif // !XB1

                MyAdapterInfo gpu = MyVideoSettingsManager.Adapters[MyVideoSettingsManager.CurrentDeviceSettings.AdapterOrdinal];
                var deviceName = gpu.Name;
                
                data.ProcessorCount = (byte)MyEnvironment.ProcessorCount;
#if XB1
                System.Diagnostics.Debug.Assert(false);
#else // !XB1
                data.OsVersion = Environment.OSVersion.VersionString;
#endif // !XB1
                data.CpuInfo = cpuName;
#if XB1
                System.Diagnostics.Debug.Assert(false);
#else // !XB1
                data.OsPlatform = Environment.Is64BitOperatingSystem ? "64bit" : "32bit";
#endif // !XB1
                data.HasDX11 = MyDirectXHelper.IsDx11Supported();
                data.GameVersion = MyFinalBuildConstants.APP_VERSION_STRING.ToString();
#if XB1
                System.Diagnostics.Debug.Assert(false);
#else // !XB1
                data.TotalPhysMemBytes = memoryInfo.ullTotalPhys;
#endif // !XB1
                data.GpuInfo = new MyGraphicsInfo();
                data.GpuInfo.AnisotropicFiltering = MyVideoSettingsManager.CurrentGraphicsSettings.Render.AnisotropicFiltering.ToString();
                data.GpuInfo.AntialiasingMode = MyVideoSettingsManager.CurrentGraphicsSettings.Render.AntialiasingMode.ToString();
                data.GpuInfo.FoliageDetails = MyVideoSettingsManager.CurrentGraphicsSettings.Render.FoliageDetails.ToString();
                data.GpuInfo.ShadowQuality = MyVideoSettingsManager.CurrentGraphicsSettings.Render.ShadowQuality.ToString();
                data.GpuInfo.TextureQuality = MyVideoSettingsManager.CurrentGraphicsSettings.Render.TextureQuality.ToString();
                data.GpuInfo.VoxelQuality = MyVideoSettingsManager.CurrentGraphicsSettings.Render.VoxelQuality.ToString();
                data.GpuInfo.GrassDensityFactor = MyVideoSettingsManager.CurrentGraphicsSettings.Render.GrassDensityFactor;
                data.GpuInfo.GPUModelName = gpu.DeviceName;
                data.GpuInfo.GPUMemory = gpu.VRAM;
                data.AudioInfo.MusicVolume = MySandboxGame.Config.MusicVolume;
                data.AudioInfo.SoundVolume = MySandboxGame.Config.GameVolume;
                data.AudioInfo.HudWarnings = MySandboxGame.Config.HudWarnings;
                data.AudioInfo.MuteWhenNotInFocus = MySandboxGame.Config.EnableMuteWhenNotInFocus;
                data.Fullscreen = MyVideoSettingsManager.CurrentDeviceSettings.WindowMode.ToString();
                data.Resolution = MySandboxGame.Config.ScreenWidth.ToString() + " x " + MySandboxGame.Config.ScreenHeight.ToString();
            }
            catch (Exception exception)
            {
                MyLog.Default.WriteLine(exception);
            }

            return data;
        }
                
        private static MyGameplayStartAnalytics GetGameplayStartAnalyticsData()
        {
            TimeSpan loadingTimeDiff = (TimeSpan)(DateTime.UtcNow - m_loadingStartedAt);
            long loadingTimeSeconds = (long)Math.Ceiling(loadingTimeDiff.TotalSeconds);
            string multiplayerType = "Off-line";
            if (MyMultiplayer.Static != null)
            {
                if (MySession.Static != null && MySession.Static.LocalCharacter != null && MyMultiplayer.Static.HostName.Equals(MySession.Static.LocalCharacter.DisplayNameText))
                {
                    multiplayerType = "Host";
                }
                else if (MyMultiplayer.Static.HostName.Equals("Dedicated server"))
                {
                    multiplayerType = "Dedicated server";
                }
                else
                {
                    multiplayerType = "Client";
                }
            }
            MyGameplayStartAnalytics data = new MyGameplayStartAnalytics
            {
                GameEntry = m_entry,
                IsScenario = m_scenarioFlag,
                ActiveMods = m_usedMods,
                LoadingDuration = loadingTimeSeconds,
                GameMode = MySession.Static.Settings.GameMode,
                OnlineMode = MySession.Static.OnlineMode,
                WorldType = MySession.Static.Scenario.DisplayNameText,
                voxelSupport = MySession.Static.Settings.EnableConvertToStation,
                destructibleBlocks = MySession.Static.Settings.DestructibleBlocks,
                destructibleVoxels = MySession.Static.Settings.EnableVoxelDestruction,
                jetpack = MySession.Static.Settings.EnableJetpack,
                hostility = MySession.Static.Settings.EnvironmentHostility.ToString(),
                drones = MySession.Static.Settings.EnableDrones,
                Wolfs = MySession.Static.Settings.EnableWolfs != null ? (bool)MySession.Static.Settings.EnableWolfs : false,
                spaceSpiders = MySession.Static.Settings.EnableSpiders != null ? (bool)MySession.Static.Settings.EnableSpiders : false,
                encounters = MySession.Static.Settings.EnableEncounters,
                oxygen = MySession.Static.Settings.EnableOxygen,
                toolShake = MySession.Static.Settings.EnableToolShake,
                inventorySpace = MySession.Static.Settings.InventorySizeMultiplier,
                welderSpeed = MySession.Static.Settings.WelderSpeedMultiplier,
                grinderSpeed = MySession.Static.Settings.GrinderSpeedMultiplier,
                refinerySpeed = MySession.Static.Settings.RefinerySpeedMultiplier,
                assemblerEfficiency = MySession.Static.Settings.AssemblerEfficiencyMultiplier,
                assemblerSpeed = MySession.Static.Settings.AssemblerSpeedMultiplier,
                hostingPlayer = MyMultiplayer.Static != null ? MyMultiplayer.Static.IsServer : true,
                floatingObjects = (uint)MySession.Static.Settings.MaxFloatingObjects,
                worldName = MySession.Static.Name,
                multiplayerType = multiplayerType
            };
            m_loadingStarted = false;
            return data;
        }

        private static MyGameplayEndAnalytics GetGameplayEndAnalyticsData()
        {
            MyFpsManager.PrepareMinMax();
            string multiplayerType = "Off-line";
            if (MyMultiplayer.Static != null)
            {
                if (MySession.Static != null && MySession.Static.LocalCharacter != null && MyMultiplayer.Static.HostName.Equals(MySession.Static.LocalCharacter.DisplayNameText))
                {
                    multiplayerType = "Host";
                }
                else if (MyMultiplayer.Static.HostName.Equals("Dedicated server"))
                {
                    multiplayerType = "Dedicated server";
                }
                else
                {
                    multiplayerType = "Client";
                }
            }
            MyGameplayEndAnalytics data = new MyGameplayEndAnalytics
            {
                AverageFramesPerSecond = (uint)(MyFpsManager.GetSessionTotalFrames() / MySession.Static.ElapsedPlayTime.TotalSeconds),
                AverageUpdatesPerSecond = (uint)(MyGameStats.Static.UpdateCount / MySession.Static.ElapsedPlayTime.TotalSeconds),

                MinFramesPerSecond = (uint)MyFpsManager.GetMinSessionFPS(),
                MaxFramesPerSecond = (uint)MyFpsManager.GetMaxSessionFPS(),
                TotalAmountMined = MySession.Static.AmountMined,
                TimeOnBigShipSeconds = (uint) MySession.Static.TimeOnBigShip.TotalSeconds,
                TimeOnSmallShipSeconds = (uint) MySession.Static.TimeOnSmallShip.TotalSeconds,
                TimeOnFootSeconds = (uint) MySession.Static.TimeOnFoot.TotalSeconds,
                TimeOnJetpackSeconds = (uint) MySession.Static.TimeOnJetpack.TotalSeconds,
                TotalDamageDealt = MySession.Static.TotalDamageDealt,
                TotalBlocksCreated = MySession.Static.TotalBlocksCreated,
                TotalPlayTime = (uint)MySession.Static.ElapsedPlayTime.TotalSeconds,
                Scenario = (bool)MySession.Static.IsScenario,
                voxelSupport = MySession.Static.Settings.EnableConvertToStation,
                destructibleBlocks = MySession.Static.Settings.DestructibleBlocks,
                destructibleVoxels = MySession.Static.Settings.EnableVoxelDestruction,
                jetpack = MySession.Static.Settings.EnableJetpack,
                hostility = MySession.Static.Settings.EnvironmentHostility.ToString(),
                drones = MySession.Static.Settings.EnableDrones,
                Wolfs = MySession.Static.Settings.EnableWolfs != null ? (bool)MySession.Static.Settings.EnableWolfs : false,
                spaceSpiders = MySession.Static.Settings.EnableSpiders != null ? (bool)MySession.Static.Settings.EnableSpiders : false,
                encounters = MySession.Static.Settings.EnableEncounters,
                oxygen = MySession.Static.Settings.EnableOxygen,
                toolShake = MySession.Static.Settings.EnableToolShake,
                inventorySpace = MySession.Static.Settings.InventorySizeMultiplier,
                welderSpeed = MySession.Static.Settings.WelderSpeedMultiplier,
                grinderSpeed = MySession.Static.Settings.GrinderSpeedMultiplier,
                refinerySpeed = MySession.Static.Settings.RefinerySpeedMultiplier,
                assemblerEfficiency = MySession.Static.Settings.AssemblerEfficiencyMultiplier,
                assemblerSpeed = MySession.Static.Settings.AssemblerSpeedMultiplier,
                floatingObjects = (uint)MySession.Static.Settings.MaxFloatingObjects,
                worldName = MySession.Static.Name,
                multiplayerType = multiplayerType
            };
            return data;
        }

        private static bool IsReportedPlayer(MyEntity entity)
        {
            if (entity == null)
                return true;

            var controllableEntity = entity as IMyControllableEntity;
            if (controllableEntity != null)
            {
                if (controllableEntity.ControllerInfo.IsLocallyControlled())
                {
                    return true;
                }
            }
            
            if (entity.Parent != null)
            {
                return IsReportedPlayer(entity.Parent);
            }

            return false;
        }

        #endregion

        #region Safe wrapped analytics method calls

        [Conditional(ANALYTICS_CONDITION_STRING)]
        public static void SetScenarioFlag(bool flag)
        {
            try
            {
                IMyAnalytics analytics = MyPerGameSettings.AnalyticsTracker;
                if (analytics == null)
                    return;
                
                m_scenarioFlag = flag;
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine(ex);
            }
        }

        [Conditional(ANALYTICS_CONDITION_STRING)]
        public static void SetLastDamageInformation(MyDamageInformation lastDamageInformation)
        {
            try
            {
                IMyAnalytics analytics = MyPerGameSettings.AnalyticsTracker;
                if (analytics == null)
                    return;
                
                // Empty message is sent from the server, we don't want it to rewrite the true damage cause.
                if (lastDamageInformation.Type == default(MyStringHash))
                    return;

                m_lastDamageInformation = lastDamageInformation;
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine(ex);
            }
        }

        [Conditional(ANALYTICS_CONDITION_STRING)]
        public static void SetUsedMods(List<MyObjectBuilder_Checkpoint.ModItem> mods)
        {
            try
            {
                IMyAnalytics analytics = MyPerGameSettings.AnalyticsTracker;
                if (analytics == null)
                    return;

                m_usedMods.Clear();
                foreach (var mod in mods)
                    m_usedMods.Add(mod.FriendlyName);
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine(ex);
            }
        }

        [Conditional(ANALYTICS_CONDITION_STRING)]
        public static void SetEntry(MyGameEntryEnum entry)
        {
            try
            {
                IMyAnalytics analytics = MyPerGameSettings.AnalyticsTracker;
                if (analytics == null)
                    return;

                // The entry point can get overwritten in some cases, we need to track scenario.
                if (entry == MyGameEntryEnum.Scenario)
                    m_scenarioFlag = true;

                m_entry = entry;
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine(ex);
            }
        }

        [Conditional(ANALYTICS_CONDITION_STRING)]
        public static void LoadingStarted()
        {
            try
            {
                IMyAnalytics analytics = MyPerGameSettings.AnalyticsTracker;
                if (analytics == null || m_loadingStarted)
                    return;

                // The entry point can get overwritten in some cases, we need to track scenario.
                m_loadingStartedAt = DateTime.UtcNow;
                m_loadingStarted = true;
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine(ex);
            }
        }

        [Conditional(ANALYTICS_CONDITION_STRING)] 
        public static void ReportProcessStart(bool firstTimeRun)
        {
            if (ReportChecksProcessStart)
                return;
            try
            {
                IMyAnalytics analytics = MyPerGameSettings.AnalyticsTracker;
                if (analytics == null)
                    return;

                // Send events to analytics.                
                analytics.ReportProcessStart(GetProcessStartAnalyticsData(), firstTimeRun, MySandboxGame.IsDedicated);
                ReportChecksProcessStart = true;

                MyLog.Default.WriteLine("Analytics helper process start reported");
            }
            catch (Exception exception)
            {
                MyLog.Default.WriteLine(exception);
            }
        }

        [Conditional(ANALYTICS_CONDITION_STRING)]
        public static void ReportServerStatus()
        {
            MyInfinarioAnalytics.updateIndex++;
            if (MyMultiplayer.Static == null || MyMultiplayer.Static.IsServer == false)
                return;
            if (MySandboxGame.IsDedicated == false && MyMultiplayer.Static.MemberCount <= 1)
                return;
            try
            {
                IMyAnalytics analytics = MyPerGameSettings.AnalyticsTracker;
                if (analytics == null)
                    return;

                // Send events to analytics.
                int gridCount = 0;
                int blockCount = 0;
                int movingGrids = 0;
                HashSet<MyEntity> entities = MyEntities.GetEntities();
                foreach (var entity in entities)
                {
                    if (entity is MyCubeGrid)
                    {
                        gridCount++;
                        blockCount += (entity as MyCubeGrid).BlocksCount;
                        if ((entity as MyCubeGrid).Physics != null && (entity as MyCubeGrid).Physics.LinearVelocity != Vector3.Zero)
                            movingGrids++;
                    }
                }
                analytics.ReportServerStatus(MyMultiplayer.Static.MemberCount,MyMultiplayer.Static.MemberLimit,Sync.ServerSimulationRatio,entities.Count,gridCount,blockCount,movingGrids,MyMultiplayer.Static.HostName);

                //MyLog.Default.WriteLine("Analytics helper server status reported");
            }
            catch (Exception exception)
            {
                MyLog.Default.WriteLine(exception);
            }
        }

        public static void InfinarioUpdate(MyTimeSpan updateTime)
        {
            if (updateTime.Seconds - 60f >= m_lastMinuteUpdate)
            {
                m_lastMinuteUpdate = (float)updateTime.Seconds;
                ReportServerStatus();
            }
        }

        [Conditional(ANALYTICS_CONDITION_STRING)] 
        public static void ReportProcessEnd()
        {
            if (MySandboxGame.IsDedicated || ReportChecksProcessEnd)
                return;
            try
            {
                IMyAnalytics analytics = MyPerGameSettings.AnalyticsTracker;
                if (analytics == null)
                    return;

                analytics.ReportProcessEnd();
                ReportChecksProcessEnd = true;
            }
            catch (Exception exception)
            {
                MyLog.Default.WriteLine(exception);
            }
        }

        [Conditional(ANALYTICS_CONDITION_STRING)] 
        public static void ReportGameplayStart()// plus server start
        {
            IMyAnalytics analytics = MyPerGameSettings.AnalyticsTracker;
            if (analytics == null)
                return;
            MyInfinarioAnalytics.updateIndex = -1;
            if (SanityCheckOnePerMinute(ref ReportChecksGameplayStart) == false)
            {
                try
                {
                    analytics.ReportGameplayStart(GetGameplayStartAnalyticsData(), MySandboxGame.IsDedicated);
                }
                catch (Exception exception)
                {
                    MyLog.Default.WriteLine(exception);
                }
            }
            if (MySandboxGame.IsDedicated || (MyMultiplayer.Static != null && MyMultiplayer.Static.IsServer && MySession.Static.OnlineMode != MyOnlineModeEnum.PRIVATE && MySession.Static.OnlineMode != MyOnlineModeEnum.OFFLINE))
            {
                analytics.ReportServerStart(MyMultiplayer.Static.MemberLimit, MyMultiplayer.Static.HostName);
            }
        }        

        [Conditional(ANALYTICS_CONDITION_STRING)] 
        public static void ReportGameplayEnd()
        {
            if (MySandboxGame.IsDedicated || SanityCheckOnePerMinute(ref ReportChecksGameplayEnd))
                return;
            try
            {
                IMyAnalytics analytics = MyPerGameSettings.AnalyticsTracker;
                if (analytics == null)
                    return;

                m_tutorialStarted = false;

                // Reset the scenario flag.
                m_scenarioFlag = false;

                analytics.ReportGameplayEnd(GetGameplayEndAnalyticsData());
                
            }
            catch (Exception exception)
            {
                MyLog.Default.WriteLine(exception);
            }
        }

        [Conditional(ANALYTICS_CONDITION_STRING)] 
        public static void ReportActivityStart(MyEntity sourceEntity, string activityName, string activityFocus, string activityType, string activityItemUsage, bool expectActivityEnd = true)
        {
            if (MySandboxGame.IsDedicated || SanityCheckAmountPerMinute(ReportChecksActivityStart, 60))
                return;
            try
            {
                IMyAnalytics analytics = MyPerGameSettings.AnalyticsTracker;
                if (analytics == null)
                    return;

                if (!IsReportedPlayer(sourceEntity)) 
                    return;

                if (MySession.Static != null && MySession.Static.LocalCharacter != null && MySession.Static.LocalCharacter.PositionComp != null)
                {
                    MyPlanetNamesData planetData = GetPlanetNames(MySession.Static.LocalCharacter.PositionComp.GetPosition());
                    analytics.ReportActivityStart(activityName, activityFocus, activityType, activityItemUsage, expectActivityEnd, planetData.planetName, planetData.planetType, Sandbox.Engine.Physics.MyPhysics.SimulationRatio, Sync.ServerSimulationRatio);
                }
                else
                {
                    analytics.ReportActivityStart(activityName, activityFocus, activityType, activityItemUsage, expectActivityEnd, simSpeedPlayer: Sandbox.Engine.Physics.MyPhysics.SimulationRatio, simSpeedServer: Sync.ServerSimulationRatio);
                }
                ReportChecksActivityStart++;
            }
            catch (Exception exception)
            {
                MyLog.Default.WriteLine(exception);
            }
        }

        [Conditional(ANALYTICS_CONDITION_STRING)]
        public static void ReportActivityStartIf(bool condition, MyEntity sourceEntity, string activityName, string activityFocus, string activityType, string activityItemUsage, bool expectActivityEnd = true)
        {
            try
            {
                IMyAnalytics analytics = MyPerGameSettings.AnalyticsTracker;
                if (analytics == null)
                    return;

                if (!condition)
                    return;
                ReportActivityStart(sourceEntity, activityName, activityFocus, activityType, activityItemUsage, expectActivityEnd);
            }
            catch (Exception exception)
            {
                MyLog.Default.WriteLine(exception);
            }
        }


        [Conditional(ANALYTICS_CONDITION_STRING)] 
        public static void ReportActivityEnd(MyEntity sourceEntity, string activityName)
        {
            if (MySandboxGame.IsDedicated || SanityCheckAmountPerMinute(ReportChecksActivityEnd, 60))
                return;
            try
            {
                IMyAnalytics analytics = MyPerGameSettings.AnalyticsTracker;
                if (analytics == null)
                    return;


                if (!IsReportedPlayer(sourceEntity)) 
                    return;


                if (MySession.Static != null && MySession.Static.LocalCharacter != null && MySession.Static.LocalCharacter.PositionComp != null)
                {
                    MyPlanetNamesData planetData = GetPlanetNames(MySession.Static.LocalCharacter.PositionComp.GetPosition());
                    analytics.ReportActivityEnd(activityName, planetData.planetName, planetData.planetType, Sandbox.Engine.Physics.MyPhysics.SimulationRatio, Sync.ServerSimulationRatio);
                }
                else
                {
                    analytics.ReportActivityEnd(activityName, simSpeedPlayer: Sandbox.Engine.Physics.MyPhysics.SimulationRatio, simSpeedServer: Sync.ServerSimulationRatio);
                }
                ReportChecksActivityEnd++;
            }
            catch (Exception exception)
            {
                MyLog.Default.WriteLine(exception);
            }
        }

        [Conditional(ANALYTICS_CONDITION_STRING)] 
        public static void ReportPlayerDeath(bool isLocallyControlled, ulong playerSteamId)
        {
            if (MySandboxGame.IsDedicated)
                return;
            try
            {
                IMyAnalytics analytics = MyPerGameSettings.AnalyticsTracker;
                if (analytics == null)
                    return;

                if (!isLocallyControlled)
                    return;

                var deathCause = m_lastDamageInformation.Type.String;

                Debug.Assert(!string.IsNullOrEmpty(deathCause), "Analytics: Unknown death type. Please report what you were doing to the devs.");

                string deathType;
                bool pvp = false;
                bool selfInflicted = false;
                if (m_lastDamageInformation.Type != MyStringHash.NullOrEmpty && m_lastDamageInformation.AttackerId != 0)
                {
                    MyEntity entity = null;
                    MyEntities.TryGetEntityById(m_lastDamageInformation.AttackerId, out entity);
                    var controllableEntity = entity as IMyControllableEntity;
                    if (controllableEntity != null)
                    {
                        // The attacker is controller by a character.
                        var controller = controllableEntity.ControllerInfo.Controller;
                        if (controller != null)
                        {
                            if (controller.Player.Id.SteamId != playerSteamId)
                            {
                                pvp = true;
                            }
                            else
                            {
                                selfInflicted = true;
                            }
                        }
                    }
                    else if (entity is IMyGunBaseUser || entity is IMyHandheldGunObject<MyToolBase> ||
                             entity is IMyHandheldGunObject<MyGunBase>)
                    {
                        pvp = true;
                    }
                }

                if (pvp)
                {
                    deathType = "pvp";
                }
                else if (selfInflicted)
                {
                    deathType = "self_inflicted";
                }
                else
                {
                    deathType = m_lastDamageInformation.Type == MyDamageType.Environment ? "environment" : "unknown";
                }

                MyPlanetNamesData planetData = GetPlanetNames(MySession.Static.LocalCharacter.PositionComp.GetPosition());
                analytics.ReportPlayerDeath(deathType, deathCause, planetData.planetName, planetData.planetType, MySession.Static.IsScenario);
            }
            catch (Exception exception)
            {
                MyLog.Default.WriteLine(exception);
            }
        }

        public static MyPlanetNamesData GetPlanetNames(Vector3D position)
        {
            MyPlanetNamesData returnData = new MyPlanetNamesData();

            var planet = MyGamePruningStructure.GetClosestPlanet(position);
            if (planet != null)
            {
                returnData.planetName = planet.StorageName;
                returnData.planetType = planet.Generator.FolderName;
            }
            else
            {
                returnData.planetName = "";
                returnData.planetType = "";
            }
            return returnData;
        }

        [Conditional(ANALYTICS_CONDITION_STRING)] 
        public static void ReportTutorialStart(string tutorialName)
        {
            if (MySandboxGame.IsDedicated)
                return;
            try
            {
                IMyAnalytics analytics = MyPerGameSettings.AnalyticsTracker;
                if (analytics == null)
                    return;

                m_tutorialStarted = true;

                analytics.ReportTutorialStart(tutorialName);
                
            }
            catch (Exception exception)
            {
                MyLog.Default.WriteLine(exception);
            }
        }

        [Conditional(ANALYTICS_CONDITION_STRING)] 
        public static void ReportTutorialStep(string stepName, int stepNumber)
        {
            if (MySandboxGame.IsDedicated)
                return;
            try
            {
                IMyAnalytics analytics = MyPerGameSettings.AnalyticsTracker;
                if (analytics == null)
                    return; analytics.ReportTutorialStep(stepName, stepNumber);

            }
            catch (Exception exception)
            {
                MyLog.Default.WriteLine(exception);
            }
        }
            
        /// <summary>
        /// This will report end of the tutorial only, if previously was called the ReportTutorialStep
        /// </summary>
        [Conditional(ANALYTICS_CONDITION_STRING)] 
        public static void ReportTutorialEnd()
        {
            if (MySandboxGame.IsDedicated)
                return;
            try
            {
                IMyAnalytics analytics = MyPerGameSettings.AnalyticsTracker;
                if (analytics == null)
                    return;

                if (!m_tutorialStarted) return;
                m_tutorialStarted = false;

                analytics.ReportTutorialEnd();
            }
            catch (Exception exception)
            {
                MyLog.Default.WriteLine(exception);
            }
        }

        [Conditional(ANALYTICS_CONDITION_STRING)] 
        public static void ReportPlayerId()
        {
            if (MySandboxGame.IsDedicated)
                return;
            try
            {
                IMyAnalytics analytics = MyPerGameSettings.AnalyticsTracker;
                if (analytics == null)
                    return;
                analytics.IdentifyPlayer(Sync.MyId, MySteam.UserName, MySteam.IsOnline);

            }
            catch (Exception exception)
            {
                MyLog.Default.WriteLine(exception);
            }
        }


        [Conditional(ANALYTICS_CONDITION_STRING)]
        public static void ReportTutorialScreen(string initiatedFrom)
        {
            if (MySandboxGame.IsDedicated)
                return;
            try
            {
                IMyAnalytics analytics = MyPerGameSettings.AnalyticsTracker;
                if (analytics == null)
                    return;
                analytics.ReportTutorialScreen(initiatedFrom);
            }
            catch (Exception exception)
            {
                MyLog.Default.WriteLine(exception);
            }
        }

        [Conditional(ANALYTICS_CONDITION_STRING)]
        public static void FlushAndDispose()
        {
            try
            {
                IMyAnalytics analytics = MyPerGameSettings.AnalyticsTracker;
                if (analytics == null)
                    return;
                analytics.FlushAndDispose();                
            }
            catch (Exception exception)
            {
                MyLog.Default.WriteLine(exception);
            }
        }

        #endregion
    }
}
