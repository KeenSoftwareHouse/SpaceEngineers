using System;
using System.Text;
using System.Reflection;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using VRage;
using Infinario;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using VRage.Utils;
using Sandbox.Game;
using VRage.Game;

namespace Sandbox.Engine.Networking
{
    public sealed class MyInfinarioAnalytics : IMyAnalytics
    {
        private enum MyInfinarioEventType
        {
            PlayerIdentity,
            FirstRun,
            ProcessStart,
            ProcessEnd,
            GameplayStart,
            GameplayEnd,
            ActivityStart,
            ActivityEnd,
            PlayerDeath,
            TutorialStart,
            TutorialEnd,
            TutorialStep,
            TutorialScreen,
            ServerStatus,
            ServerStart,
            ServerEnd
        }

        private struct MyEventAttributes
        {
            public MyInfinarioEventType EventType;
            public DateTime EventTimestamp;

            public ulong PlayerId;
            public string PlayerName;
            public bool PlayerOnline;
            public bool DedicatedServer;

            public MyProcessStartAnalytics ProcessStart;

            public MyGameplayStartAnalytics GameplayStart;
            public MyGameplayEndAnalytics GameplayEnd;

            public string ActivityName;
            public string ActivityFocus;
            public string ActivityType;
            public string ActivityItemUsage;
            public bool ExpectActivityEnd;

            public float SimSpeedPlayer;
            public float SimSpeedServer;

            public string DeathType;
            public string DeathCause;
            public string PlanetName;
            public string PlanetType;
            public bool Scenario;

            public string TutorialName;
            public string TutorialStepName;
            public int TutorialStepNumber;

            public string TutorialOpenedFrom;

            public int PlayerCount;
            public int PlayerCountMax;
            public int EntityCount;
            public int GridCount;
            public int BlockCount;
            public int MovingGridCount;
            public string HostName;
            public string OnlineMode;
        }

        private Dictionary<string, object> m_playerIdentityCached;
        private Dictionary<string, object> m_processStartCached;
        private Dictionary<string, object> m_gameplayStartCached;
        private Dictionary<string, object> m_worldSettingsCached;
        private Dictionary<string, object> m_tutorialStartCached;
        private Dictionary<string, Dictionary<string, object>> m_activitiesInProgress;
        private DateTime m_processStartTimestamp, m_gameplayStartTimestamp;
        private readonly Dictionary<string, DateTime> m_activityStartTimestamps;
        private readonly Infinario.Infinario m_infinario;

        private Thread m_consumer;
        SpinLock m_addGuard;
        private BlockingCollection<MyEventAttributes> m_queue;

        private static readonly object m_singletonGuard = new Object(); 
        private static volatile MyInfinarioAnalytics m_instance;

        //server variables
        public static int updateIndex = -1;//index is incremented first

        private MyInfinarioAnalytics()
        {
            var companyToken = MyFinalBuildConstants.IS_OFFICIAL ? MyPerGameSettings.InfinarioOfficial : MyPerGameSettings.InfinarioDebug;
            m_infinario = Infinario.Infinario.GetInstance();
            m_infinario.Initialize(companyToken);
            m_playerIdentityCached = new Dictionary<string, object>();
            m_processStartCached = new Dictionary<string, object>();
            m_gameplayStartCached = new Dictionary<string, object>();
            m_worldSettingsCached = new Dictionary<string, object>();
            m_tutorialStartCached = new Dictionary<string, object>();
            m_activityStartTimestamps = new Dictionary<string, DateTime>(30);
            m_activitiesInProgress = new Dictionary<string, Dictionary<string, object>>(30);
            m_addGuard = new SpinLock();
            m_queue = new BlockingCollection<MyEventAttributes>();
            m_consumer = new Thread(Consume);
            m_consumer.Name = "Infinario Analytics Queue Consumer";
            m_consumer.IsBackground = true;
            m_consumer.Start();
        }

        public static MyInfinarioAnalytics Instance
        {
            get
            {
                if (m_instance == null)
                {
                    lock (m_singletonGuard)
                    {
                        if (m_instance == null)
                            m_instance = new MyInfinarioAnalytics();
                    }
                }

                return m_instance;
            }
        }

        public void FlushAndDispose()
        {
            bool lockTaken = false;
            m_addGuard.Enter(ref lockTaken);

            m_queue.CompleteAdding();

            if (lockTaken)
                m_addGuard.Exit();
            
            m_consumer.Join();
            m_infinario.Dispose();
        }

        public static string FillIfEmpty(string str, string fallback)
        {
            return String.IsNullOrEmpty(str) ? fallback : str;
        }

        private void Consume()
        {
            while (!m_queue.IsCompleted)
            {
                try
                {
                    MyEventAttributes attributes;
                    if (!m_queue.TryTake(out attributes, Timeout.Infinite))
                        continue;

                    switch (attributes.EventType)
                    {
                        case MyInfinarioEventType.PlayerIdentity:
                            {
                                var properties = new Dictionary<string, object>();
                                properties.Add("player_id", attributes.PlayerId.ToString());
                                properties.Add("player_name", attributes.PlayerName);
                                properties.Add("online_status", attributes.PlayerOnline ? "online" : "offline");
                                m_playerIdentityCached = properties;
                                m_infinario.Identify(attributes.PlayerId.ToString(), properties);
                            }
                            break;
                        case MyInfinarioEventType.FirstRun:
                            {
                                var properties = new Dictionary<string, object>();
                                foreach (var property in m_processStartCached)
                                    properties[property.Key] = property.Value;
                                m_infinario.Track("first_run", properties);
                            }
                            break;
                        case MyInfinarioEventType.ProcessStart:
                            {
                                m_processStartTimestamp = attributes.EventTimestamp;
                                var properties = new Dictionary<string, object> 
                                {
                                    { "os_info", attributes.ProcessStart.OsVersion },
                                    { "cpu_info", attributes.ProcessStart.CpuInfo },
                                    { "number_of_cores", attributes.ProcessStart.ProcessorCount },
                                    { "os_platform", attributes.ProcessStart.OsPlatform },
                                    { "dx11_support", attributes.ProcessStart.HasDX11 },
                                    { "ram_size", (attributes.ProcessStart.TotalPhysMemBytes / 1024 / 1024) },
                                    { "gpu_name", attributes.ProcessStart.GpuInfo.GPUModelName },
                                    { "gpu_memory", (attributes.ProcessStart.GpuInfo.GPUMemory / 1024 / 1024) },
                                    { "display_resolution", attributes.ProcessStart.Resolution },
                                    { "display_window_mode", attributes.ProcessStart.Fullscreen },
                                    { "audio_sound_volume", attributes.ProcessStart.AudioInfo.SoundVolume },
                                    { "audio_music_volume", attributes.ProcessStart.AudioInfo.MusicVolume },
                                    { "audio_hud_warnings", attributes.ProcessStart.AudioInfo.HudWarnings },
                                    { "audio_mute_when_not_in_focus", attributes.ProcessStart.AudioInfo.MuteWhenNotInFocus },
                                    { "graphics_anisotropic_filtering", attributes.ProcessStart.GpuInfo.AnisotropicFiltering },
                                    { "graphics_antialiasing_mode", attributes.ProcessStart.GpuInfo.AntialiasingMode },
                                    { "graphics_foliage_details", attributes.ProcessStart.GpuInfo.FoliageDetails },
                                    { "graphics_shadow_quality", attributes.ProcessStart.GpuInfo.ShadowQuality },
                                    { "graphics_texture_quality", attributes.ProcessStart.GpuInfo.TextureQuality },
                                    { "graphics_voxel_quality", attributes.ProcessStart.GpuInfo.VoxelQuality },
                                    { "graphics_grass_density_factor", attributes.ProcessStart.GpuInfo.GrassDensityFactor },
                                    { "game_version", attributes.ProcessStart.GameVersion }
                                };
                                m_processStartCached = properties;

                                if (m_playerIdentityCached.Count > 0)
                                {
                                    var cachedProperties = new Dictionary<string, object>();
                                    foreach (var property in m_playerIdentityCached)
                                        cachedProperties[property.Key] = property.Value;
                                    foreach (var property in m_processStartCached)
                                        cachedProperties[property.Key] = property.Value;
                                    if (m_playerIdentityCached.ContainsKey("player_id"))
                                        m_infinario.Identify(m_playerIdentityCached["player_id"].ToString(), cachedProperties);
                                }

                                if (attributes.DedicatedServer == false)
                                {
                                    m_infinario.Track("process_start", properties);
                                    m_infinario.TrackSessionStart(properties);
                                }
                            }
                            break;
                        case MyInfinarioEventType.ProcessEnd:
                            {
                                var properties = new Dictionary<string, object>();
                                properties.Add("duration", (long)Math.Truncate(attributes.EventTimestamp.Subtract(m_processStartTimestamp).TotalSeconds));
                                foreach (var property in m_processStartCached)
                                    properties[property.Key] = property.Value;
                                m_infinario.Track("process_end", properties);
                                m_infinario.TrackSessionEnd();
                            }
                            break;
                        case MyInfinarioEventType.GameplayStart:
                            {
                                m_gameplayStartTimestamp = attributes.EventTimestamp;
                                var properties = new Dictionary<string, object> 
                                {
                                    { "scenario", attributes.GameplayStart.IsScenario },
                                    { "entry", attributes.GameplayStart.GameEntry },
                                    { "game_mode", attributes.GameplayStart.GameMode },
                                    { "loading_duration", attributes.GameplayStart.LoadingDuration },
                                    { "online_mode", attributes.GameplayStart.OnlineMode },
                                    { "active_mods", attributes.GameplayStart.ActiveMods },
                                    { "active_mods_count", attributes.GameplayStart.ActiveMods.Count },
                                    { "world_type" , attributes.GameplayStart.WorldType},
                                    { "is_hosting_player", attributes.GameplayStart.hostingPlayer}
                                };
                                foreach (var property in m_processStartCached)
                                    properties[property.Key] = property.Value;
                                foreach (var property in properties)
                                    m_gameplayStartCached[property.Key] = property.Value;

                                //World settings data
                                var worldSettings = new Dictionary<string, object> ();
                                worldSettings.Add("hostility", attributes.GameplayStart.hostility);
                                worldSettings.Add("cyber_hounds", attributes.GameplayStart.Wolfs);
                                worldSettings.Add("space_spiders", attributes.GameplayStart.spaceSpiders);
                                worldSettings.Add("voxel_support", attributes.GameplayStart.voxelSupport);
                                worldSettings.Add("destructible_blocks", attributes.GameplayStart.destructibleBlocks);
                                worldSettings.Add("destructible_voxels", attributes.GameplayStart.destructibleVoxels);
                                worldSettings.Add("drones", attributes.GameplayStart.drones);
                                worldSettings.Add("jetpack", attributes.GameplayStart.jetpack);
                                worldSettings.Add("encounters", attributes.GameplayStart.encounters);
                                worldSettings.Add("oxygen", attributes.GameplayStart.oxygen);
                                worldSettings.Add("trash_auto_removal", attributes.GameplayStart.trashAutoRemoval);
                                worldSettings.Add("tool_shake", attributes.GameplayStart.toolShake);
                                worldSettings.Add("inventory_size", attributes.GameplayStart.inventorySpace);
                                worldSettings.Add("welder_speed", attributes.GameplayStart.welderSpeed);
                                worldSettings.Add("grinder_speed", attributes.GameplayStart.grinderSpeed);
                                worldSettings.Add("refinery_speed", attributes.GameplayStart.refinerySpeed);
                                //worldSettings.Add("assembler_speed", attributes.GameplayStart.assemblerSpeed);//disabled for now - speed is tied to efficiency now
                                worldSettings.Add("assembler_efficiency", attributes.GameplayStart.assemblerEfficiency);
                                worldSettings.Add("floating_objects", attributes.GameplayStart.floatingObjects);
                                worldSettings.Add("world_name", attributes.GameplayStart.worldName);
                                worldSettings.Add("multiplayer_type", attributes.GameplayStart.multiplayerType);
                                foreach (var property in worldSettings)
                                {
                                    m_worldSettingsCached[property.Key] = property.Value;
                                    properties[property.Key] = property.Value;
                                }

                                if (attributes.DedicatedServer == false)
                                {
                                    m_infinario.Track("gameplay_start", properties);
                                }
                            }
                            break;
                        case MyInfinarioEventType.GameplayEnd:
                            {
                                var properties = new Dictionary<string, object>();
                                properties.Add("duration", (long)Math.Truncate(attributes.EventTimestamp.Subtract(m_gameplayStartTimestamp).TotalSeconds));
                                properties.Add("average_frames_per_second", attributes.GameplayEnd.AverageFramesPerSecond);
                                properties.Add("average_updates_per_second", attributes.GameplayEnd.AverageUpdatesPerSecond);
                                properties.Add("average_latency_milliseconds", attributes.GameplayEnd.AverageLatencyMilliseconds);
                                properties.Add("total_blocks_created", attributes.GameplayEnd.TotalBlocksCreated);
                                properties.Add("total_damage_dealt", attributes.GameplayEnd.TotalDamageDealt);
                                properties.Add("scenario", attributes.GameplayEnd.Scenario);
                                properties.Add("total_play_time", attributes.GameplayEnd.TotalPlayTime);
                                foreach (var property in m_gameplayStartCached)
                                    properties[property.Key] = property.Value;
                                foreach (var property in m_processStartCached)
                                    properties[property.Key] = property.Value;
                                foreach (var property in m_worldSettingsCached)
                                    properties[property.Key] = property.Value;

                                //gameplay end data only
                                properties.Add("average_sim_speed_player", attributes.GameplayEnd.averageSimSpeedPlayer);
                                properties.Add("average_sim_speed_server", attributes.GameplayEnd.averageSimSpeedServer);

                                m_infinario.Track("gameplay_end", properties);
                            }
                            break;
                        case MyInfinarioEventType.ActivityStart:
                            {
                                if (!String.IsNullOrEmpty(attributes.ActivityName))
                                {
                                    var properties = new Dictionary<string, object> 
                                    {
                                        { "name", attributes.ActivityName },
                                        { "focus", FillIfEmpty(attributes.ActivityFocus, "Other") },
                                        { "type", FillIfEmpty(attributes.ActivityType, "Other") },
                                        { "item_usage", FillIfEmpty(attributes.ActivityItemUsage, "None") },
                                        { "planet_name", FillIfEmpty(attributes.PlanetName, "none") },
                                        { "planet_type", FillIfEmpty(attributes.PlanetType, "none") },
                                        { "sim_speed_player", attributes.SimSpeedPlayer },
                                        { "sim_speed_server", attributes.SimSpeedServer }

                                    };

                                    if (attributes.ExpectActivityEnd)
                                    {
                                        m_activityStartTimestamps[attributes.ActivityName] = attributes.EventTimestamp;
                                        m_activitiesInProgress[attributes.ActivityName] = properties;
                                    }
                                    else
                                    {
                                        foreach (var property in m_gameplayStartCached)
                                            properties[property.Key] = property.Value;
                                        foreach (var property in m_processStartCached)
                                            properties[property.Key] = property.Value;
                                        m_infinario.Track("activity_start", properties);
                                    }
                                }
                            }
                            break;
                        case MyInfinarioEventType.ActivityEnd:
                            {
                                if (!String.IsNullOrEmpty(attributes.ActivityName))
                                {
                                    if (m_activitiesInProgress.ContainsKey(attributes.ActivityName))
                                    {
                                        var cachedProperties = m_activitiesInProgress[attributes.ActivityName];
                                        m_activitiesInProgress.Remove(attributes.ActivityName);

                                        DateTime activityStartTimestamp = m_activityStartTimestamps[attributes.ActivityName];
                                        m_activityStartTimestamps.Remove(attributes.ActivityName);

                                        TimeSpan duration = attributes.EventTimestamp.Subtract(activityStartTimestamp);
                                        bool itemUsage = !String.IsNullOrEmpty(cachedProperties["item_usage"].ToString());
                                        bool skip = itemUsage && (duration.TotalSeconds < 0.02d);

                                        if (!skip)
                                        {
                                            foreach (var property in m_gameplayStartCached)
                                                cachedProperties[property.Key] = property.Value;
                                            foreach (var property in m_processStartCached)
                                                cachedProperties[property.Key] = property.Value;
                                            TimeSpan epoch = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0));
                                            m_infinario.Track("activity_start", cachedProperties,
                                                (long)Math.Truncate(epoch.TotalSeconds - duration.TotalSeconds));

                                            var properties = new Dictionary<string, object>
                                            { 
                                                { "planet_name", FillIfEmpty(attributes.PlanetName, "none") },
                                                { "planet_type", FillIfEmpty(attributes.PlanetType, "none") },
                                                { "sim_speed_player", attributes.SimSpeedPlayer },
                                                { "sim_speed_server", attributes.SimSpeedServer }
                                            };
                                            properties.Add("duration", (long)Math.Truncate(duration.TotalSeconds));
                                            foreach (var property in cachedProperties)
                                                properties[property.Key] = property.Value;
                                            m_infinario.Track("activity_end", properties);
                                        }
                                    }
                                }
                            }
                            break;
                        case MyInfinarioEventType.PlayerDeath:
                            {
                                var properties = new Dictionary<string, object> 
                                {
                                    { "type", FillIfEmpty(attributes.DeathType, "Other") },
                                    { "cause", FillIfEmpty(attributes.DeathCause, "Other") },
                                    { "planet_name", FillIfEmpty(attributes.PlanetName, "none") },
                                    { "planet_type", FillIfEmpty(attributes.PlanetType, "none") },
                                    { "scenario", attributes.Scenario }
                                };
                                foreach (var property in m_gameplayStartCached)
                                    properties[property.Key] = property.Value;
                                foreach (var property in m_processStartCached)
                                    properties[property.Key] = property.Value;
                                m_infinario.Track("character_death", properties);
                            }
                            break;
                        case MyInfinarioEventType.TutorialStart:
                            {
                                var properties = new Dictionary<string, object>();
                                properties.Add("name", attributes.TutorialName);
                                foreach (var property in m_processStartCached)
                                    properties[property.Key] = property.Value;
                                m_tutorialStartCached = properties;
                                m_infinario.Track("tutorial_start", properties);
                            }
                            break;
                        case MyInfinarioEventType.TutorialStep:
                            {
                                var properties = new Dictionary<string, object>
                                {
                                    { "step_name", attributes.TutorialStepName },
                                    { "step_number", attributes.TutorialStepNumber }
                                };
                                foreach (var property in m_tutorialStartCached)
                                    properties[property.Key] = property.Value;
                                foreach (var property in m_processStartCached)
                                    properties[property.Key] = property.Value;
                                m_infinario.Track("tutorial_step", properties);
                            }
                            break;
                        case MyInfinarioEventType.TutorialEnd:
                            {
                                var properties = new Dictionary<string, object>();
                                foreach (var property in m_tutorialStartCached)
                                    properties[property.Key] = property.Value;
                                foreach (var property in m_processStartCached)
                                    properties[property.Key] = property.Value;
                                m_infinario.Track("tutorial_end", properties);
                            }
                            break;
                        case MyInfinarioEventType.TutorialScreen:
                            {
                                var properties = new Dictionary<string, object>()
                                {
                                    {"source", FillIfEmpty(attributes.TutorialOpenedFrom, "Other")}
                                };
                                foreach (var property in m_tutorialStartCached)
                                    properties[property.Key] = property.Value;
                                foreach (var property in m_processStartCached)
                                    properties[property.Key] = property.Value;
                                m_infinario.Track("tutorial_click", properties);
                            }
                            break;

                        case MyInfinarioEventType.ServerStatus:
                            {
                                var properties = new Dictionary<string, object>();
                                if(m_worldSettingsCached.ContainsKey("world_name") == false)
                                    break;
                                properties.Add("world_name", m_worldSettingsCached["world_name"]);
                                if (m_gameplayStartCached.ContainsKey("world_type"))
                                    properties.Add("world_type", m_gameplayStartCached["world_type"]);
                                properties.Add("host_name", attributes.HostName);
                                properties.Add("update_index", updateIndex);
                                properties.Add("player_count", attributes.PlayerCount);
                                properties.Add("player_max", attributes.PlayerCountMax);
                                properties.Add("sim_speed_server", attributes.SimSpeedServer);
                                properties.Add("is_dedicated", MySandboxGame.IsDedicated);
                                properties.Add("entity_count", attributes.EntityCount);
                                properties.Add("grid_count", attributes.GridCount);
                                properties.Add("block_count", attributes.BlockCount);
                                properties.Add("moving_grid_count", attributes.MovingGridCount);
                                /*MySandboxGame.Log.WriteLineAndConsole("server update: "+properties.Count.ToString()+" properties in total.");
                                foreach (var property in properties)
                                    MySandboxGame.Log.WriteLineAndConsole(property.Key + ": " + property.Value.ToString());*/
                                m_infinario.Track("server_status", properties);
                            }
                            break;

                        case MyInfinarioEventType.ServerStart:
                            {
                                //load basic data
                                var properties = new Dictionary<string, object>();
                                foreach (var property in m_gameplayStartCached)
                                    properties[property.Key] = property.Value;
                                foreach (var property in m_worldSettingsCached)
                                    properties[property.Key] = property.Value;

                                //server start specific
                                properties.Add("is_dedicated", MySandboxGame.IsDedicated);
                                properties.Add("max_players", attributes.PlayerCountMax);
                                properties.Add("host_name", attributes.HostName);

                                //remove unnecessary data
                                properties.Remove("audio_sound_volume");
                                properties.Remove("audio_music_volume");
                                properties.Remove("audio_hud_warnings");
                                properties.Remove("audio_mute_when_not_in_focus");
                                properties.Remove("multiplayer_type");
                                properties.Remove("display_resolution");
                                properties.Remove("display_window_mode");
                                properties.Remove("graphics_anisotropic_filtering");
                                properties.Remove("graphics_antialiasing_mode");
                                properties.Remove("graphics_foliage_details");
                                properties.Remove("graphics_shadow_quality");
                                properties.Remove("graphics_texture_quality");
                                properties.Remove("graphics_voxel_quality");
                                properties.Remove("graphics_grass_density_factor");
                                properties.Remove("gpu_name");
                                properties.Remove("gpu_memory");
                                properties.Remove("loading_duration");
                                properties.Remove("is_hosting_player");

                                m_infinario.Track("server_start", properties);
                            }
                            break;


                        case MyInfinarioEventType.ServerEnd:

                            break;
                    }
                }
                catch (Exception ex)
                {
                    //System.Diagnostics.Debug.Fail(ex.Message);
                    if (MyLog.Default != null)
                    {
                        MyLog.Default.WriteLine(ex);
                    }
                }
            }
        }

        void Add(MyEventAttributes args)
        {
            bool lockTaken = false;
            m_addGuard.Enter(ref lockTaken);

            if (!m_queue.IsAddingCompleted)
            {
                m_queue.Add(args, CancellationToken.None);
            }
            else
            {
                MyLog.Default.WriteLine("Error sending analytics: " + args.EventType.ToString());
            }

            if (lockTaken)
                m_addGuard.Exit();
        }

        public void IdentifyPlayer(ulong playerId, string playerName, bool isOnline)
        {
            Add(new MyEventAttributes
            {
                EventType = MyInfinarioEventType.PlayerIdentity,
                EventTimestamp = DateTime.UtcNow,
                PlayerId = playerId,
                PlayerName = playerName,
                PlayerOnline = isOnline
            });
        }

        public void ReportProcessStart(MyProcessStartAnalytics attributes, bool firstRun, bool dedicatedServer)
        {
            Add(new MyEventAttributes
            {
                EventType = MyInfinarioEventType.ProcessStart,
                EventTimestamp = DateTime.UtcNow,
                ProcessStart = attributes,
                DedicatedServer = dedicatedServer
            });

            if (firstRun && dedicatedServer == false)
            {
                Add(new MyEventAttributes
                {
                    EventType = MyInfinarioEventType.FirstRun,
                    EventTimestamp = DateTime.UtcNow,
                    ProcessStart = attributes
                });
            }
        }

        public void ReportProcessEnd()
        {
            Add(new MyEventAttributes
            {
                EventType = MyInfinarioEventType.ProcessEnd,
                EventTimestamp = DateTime.UtcNow,
            });
        }

        public void ReportGameplayStart(MyGameplayStartAnalytics attributes, bool dedicatedServer)
        {
            Add(new MyEventAttributes
            {
                EventType = MyInfinarioEventType.GameplayStart,
                EventTimestamp = DateTime.UtcNow,
                GameplayStart = attributes,
                DedicatedServer = dedicatedServer
            });
        }

        public void ReportGameplayEnd(MyGameplayEndAnalytics attributes)
        {
            Add(new MyEventAttributes
            {
                EventType = MyInfinarioEventType.GameplayEnd,
                EventTimestamp = DateTime.UtcNow,
                GameplayEnd = attributes
            });
        }

        public void ReportActivityStart(string activityName, string activityFocus, string activityType, string activityItemUsage, bool expectActivityEnd, string planetName = "", string planetType = "", float simSpeedPlayer = 1f, float simSpeedServer = 1f)
        {
            Add(new MyEventAttributes
            {
                EventType = MyInfinarioEventType.ActivityStart,
                EventTimestamp = DateTime.UtcNow,
                ActivityName = activityName,
                ActivityFocus = activityFocus,
                ActivityType = activityType,
                ActivityItemUsage = activityItemUsage,
                PlanetName = planetName,
                PlanetType = planetType,
                ExpectActivityEnd = expectActivityEnd,
                SimSpeedPlayer = simSpeedPlayer,
                SimSpeedServer = simSpeedServer
            });
        }

        public void ReportActivityEnd(string activityName, string planetName = "", string planetType = "", float simSpeedPlayer = 1f, float simSpeedServer = 1f)
        {
            Add(new MyEventAttributes
            {
                EventType = MyInfinarioEventType.ActivityEnd,
                EventTimestamp = DateTime.UtcNow,
                PlanetName = planetName,
                PlanetType = planetType,
                ActivityName = activityName,
                SimSpeedPlayer = simSpeedPlayer,
                SimSpeedServer = simSpeedServer
            });
        }

        public void ReportPlayerDeath(string deathType, string deathCause, string planetName = "", string planetType = "", bool scenario = false)
        {
            Add(new MyEventAttributes
            {
                EventType = MyInfinarioEventType.PlayerDeath,
                EventTimestamp = DateTime.UtcNow,
                DeathType = deathType,
                DeathCause = deathCause,
                PlanetName = planetName,
                PlanetType = planetType,
                Scenario = scenario
            });
        }

        public void ReportTutorialScreen(string initiatedFrom)
        {
            Add(new MyEventAttributes
            {
                EventType = MyInfinarioEventType.TutorialScreen,
                EventTimestamp = DateTime.UtcNow,
                TutorialOpenedFrom = initiatedFrom
            });
        }

        public void ReportTutorialStart(string tutorialName)
        {
            Add(new MyEventAttributes
            {
                EventType = MyInfinarioEventType.TutorialStart,
                EventTimestamp = DateTime.UtcNow,
                TutorialName = tutorialName
            });
        }

        public void ReportTutorialStep(string stepName, int stepNumber)
        {
            Add(new MyEventAttributes
            {
                EventType = MyInfinarioEventType.TutorialStep,
                EventTimestamp = DateTime.UtcNow,
                TutorialStepName = stepName,
                TutorialStepNumber = stepNumber
            });
        }

        public void ReportTutorialEnd()
        {
            Add(new MyEventAttributes
            {
                EventType = MyInfinarioEventType.TutorialEnd,
                EventTimestamp = DateTime.UtcNow
            });
        }

        public void ReportServerStatus(int playerCount, int maxPlayers, float simSpeedServer, int entitiesCount, int gridsCount, int blocksCount, int movingGridsCount, string hostName)
        {
            Add(new MyEventAttributes
            {
                EventType = MyInfinarioEventType.ServerStatus,
                EventTimestamp = DateTime.UtcNow,
                PlayerCount = playerCount,
                SimSpeedServer = simSpeedServer,
                PlayerCountMax = maxPlayers,
                EntityCount = entitiesCount,
                GridCount = gridsCount,
                BlockCount = blocksCount,
                MovingGridCount = movingGridsCount,
                HostName = hostName
            });
        }

        public void ReportServerStart(int maxPlayers, string hostName)
        {
            Add(new MyEventAttributes
            {
                EventType = MyInfinarioEventType.ServerStart,
                EventTimestamp = DateTime.UtcNow,
                PlayerCountMax = maxPlayers,
                HostName = hostName
            });
        }
    }
}
