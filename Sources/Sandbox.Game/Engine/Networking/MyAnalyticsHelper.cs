using Sandbox.Common;
using Sandbox.Engine.Platform.VideoMode;
using Sandbox.Game;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage.Utils;
using VRage.Win32;
using VRageRender;
using Sandbox.Game.World;

namespace Sandbox.Engine.Networking
{
    /// <summary>
    /// Helper class to call used analytics in a game (implementation of IMyAnalytics interface)
    /// </summary>
    public static class MyAnalyticsHelper
    {
        // Change this if you want to completely disable (Infinario) analytics
        private const string ANALYTICS_CONDITION_STRING = "WINDOWS";

        #region Helper methods and fields

        private static readonly List<string> m_usedMods = new List<string>();
        private static MyDamageInformation m_lastDamageInformation = new MyDamageInformation
        {
            Type = MyStringHash.NullOrEmpty
        };
        private static bool m_tutorialStarted;

        private static MyGameEntryEnum m_entry;        

        private static MyProcessStartAnalytics GetProcessStartAnalyticsData()
        {
            MyProcessStartAnalytics data = new MyProcessStartAnalytics();
            var cpus = new ManagementObjectSearcher("root\\CIMV2", "SELECT Name FROM Win32_Processor").Get();
            // We're just reporting the first 
            var cpuName = cpus.Cast<ManagementObject>().First()["Name"].ToString();

            var memoryInfo = new WinApi.MEMORYSTATUSEX();
            WinApi.GlobalMemoryStatusEx(memoryInfo);

            var deviceName =
                MyVideoSettingsManager.Adapters[MyVideoSettingsManager.CurrentDeviceSettings.AdapterOrdinal]
                    .Name;

            data.OsVersion = Environment.OSVersion.VersionString;
            data.CpuInfo = cpuName;
            data.GpuInfo = deviceName;
            data.GameVersion = MyFinalBuildConstants.APP_VERSION_STRING.ToString();
            data.TotalPhysMemBytes = memoryInfo.ullTotalPhys;

            return data;
        }
                
        private static MyGameplayStartAnalytics GetGameplayStartAnalyticsData()
        {
            MyGameplayStartAnalytics data = new MyGameplayStartAnalytics
            {
                GameEntry = m_entry,
                ActiveMods = m_usedMods,
                GameMode = MySession.Static.Settings.GameMode,
                OnlineMode = MySession.Static.OnlineMode
            };
            return data;
        }

        private static MyGameplayEndAnalytics GetGameplayEndAnalyticsData()
        {
            MyGameplayEndAnalytics data = new MyGameplayEndAnalytics
            {
                AverageFramesPerSecond = (int)(MyFpsManager.GetSessionTotalFrames() / MySession.Static.ElapsedPlayTime.TotalSeconds),
                MinFramesPerSecond = MyFpsManager.GetMinSessionFPS(),
                MaxFramesPerSecond = MyFpsManager.GetMaxSessionFPS(),
                TotalAmountMined = MySession.Static.AmountMined,
                TimeOnBigShipSeconds = (uint) MySession.Static.TimeOnBigShip.TotalSeconds,
                TimeOnSmallShipSeconds = (uint) MySession.Static.TimeOnSmallShip.TotalSeconds,
                TimeOnFootSeconds = (uint) MySession.Static.TimeOnFoot.TotalSeconds,
                TimeOnJetpackSeconds = (uint) MySession.Static.TimeOnJetpack.TotalSeconds
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
        public static void SetLastDamageInformation(MyDamageInformation lastDamageInformation)
        {
            try
            {
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
                m_entry = entry;
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine(ex);
            }
        }

        [Conditional(ANALYTICS_CONDITION_STRING)] 
        public static void ReportProcessStart(bool firstTimeRun)
        {
            try
            {
                // Send events to analytics.
                IMyAnalytics analytics = MyPerGameSettings.AnalyticsTracker;
                if (analytics != null)
                {
                    analytics.ReportProcessStart(GetProcessStartAnalyticsData(), firstTimeRun);
                }
            }
            catch (Exception exception)
            {
                MyLog.Default.WriteLine(exception);
            }
        }

        [Conditional(ANALYTICS_CONDITION_STRING)] 
        public static void ReportProcessEnd()
        {
            try
            {
                IMyAnalytics analytics = MyPerGameSettings.AnalyticsTracker;
                if (analytics != null)
                {
                    analytics.ReportProcessEnd();
                }
            }
            catch (Exception exception)
            {
                MyLog.Default.WriteLine(exception);
            }
        }

        [Conditional(ANALYTICS_CONDITION_STRING)] 
        public static void ReportGameplayStart()
        {
            try
            {
                IMyAnalytics analytics = MyPerGameSettings.AnalyticsTracker;
                if (analytics != null)
                {
                    analytics.ReportGameplayStart(GetGameplayStartAnalyticsData());
                }
            }
            catch (Exception exception)
            {
                MyLog.Default.WriteLine(exception);
            }
        }        

        [Conditional(ANALYTICS_CONDITION_STRING)] 
        public static void ReportGameplayEnd()
        {
            try
            {
                m_tutorialStarted = false;
                IMyAnalytics analytics = MyPerGameSettings.AnalyticsTracker;
                if (analytics != null)
                {
                    analytics.ReportGameplayEnd(GetGameplayEndAnalyticsData());
                }
            }
            catch (Exception exception)
            {
                MyLog.Default.WriteLine(exception);
            }
        }

        [Conditional(ANALYTICS_CONDITION_STRING)] 
        public static void ReportActivityStart(MyEntity sourceEntity, string activityName, string activityFocus, string activityType, string activityItemUsage, bool expectActivityEnd = true)
        {
            try
            {
                if (!IsReportedPlayer(sourceEntity)) 
                    return;

                IMyAnalytics analytics = MyPerGameSettings.AnalyticsTracker;
                if (analytics != null)
                {
                    analytics.ReportActivityStart(activityName, activityFocus, activityType, activityItemUsage, expectActivityEnd);
                }
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
            try
            {
                if (!IsReportedPlayer(sourceEntity)) 
                    return;
                IMyAnalytics analytics = MyPerGameSettings.AnalyticsTracker;
                if (analytics != null)
                {
                    analytics.ReportActivityEnd(activityName);
                }
            }
            catch (Exception exception)
            {
                MyLog.Default.WriteLine(exception);
            }
        }

        [Conditional(ANALYTICS_CONDITION_STRING)] 
        public static void ReportPlayerDeath(bool isLocallyControlled, ulong playerSteamId)
        {
            try
            {
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

                IMyAnalytics analytics = MyPerGameSettings.AnalyticsTracker;
                if (analytics != null)
                {
                    analytics.ReportPlayerDeath(deathType, deathCause);
                }
            }
            catch (Exception exception)
            {
                MyLog.Default.WriteLine(exception);
            }
        }

        [Conditional(ANALYTICS_CONDITION_STRING)] 
        public static void ReportTutorialStart(string tutorialName)
        {
            try
            {
                m_tutorialStarted = true;
                IMyAnalytics analytics = MyPerGameSettings.AnalyticsTracker;
                if (analytics != null)
                {
                    analytics.ReportTutorialStart(tutorialName);
                }
            }
            catch (Exception exception)
            {
                MyLog.Default.WriteLine(exception);
            }
        }

        [Conditional(ANALYTICS_CONDITION_STRING)] 
        public static void ReportTutorialStep(string stepName, int stepNumber)
        {
            try
            {
                IMyAnalytics analytics = MyPerGameSettings.AnalyticsTracker;
                if (analytics != null)
                {
                    analytics.ReportTutorialStep(stepName, stepNumber);
                }
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
            try
            {
                if (!m_tutorialStarted) return;
                m_tutorialStarted = false;

                IMyAnalytics analytics = MyPerGameSettings.AnalyticsTracker;
                if (analytics != null)
                {
                    analytics.ReportTutorialEnd();
                }
            }
            catch (Exception exception)
            {
                MyLog.Default.WriteLine(exception);
            }
        }

        [Conditional(ANALYTICS_CONDITION_STRING)] 
        public static void ReportPlayerId()
        {
            try
            {
                IMyAnalytics analytics = MyPerGameSettings.AnalyticsTracker;
                if (analytics != null)
                {
                    analytics.IdentifyPlayer(MySteam.UserId, MySteam.UserName, MySteam.IsOnline);
                }
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
                if (analytics != null)
                {
                    analytics.FlushAndDispose();
                }
            }
            catch (Exception exception)
            {
                MyLog.Default.WriteLine(exception);
            }
        }

        #endregion
    }
}
