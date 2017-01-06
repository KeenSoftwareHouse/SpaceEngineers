#region Usings

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Forms;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Game.AI;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Character.Components;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.Interfaces;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Gui;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.SessionComponents.Clipboard;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using VRage;
using VRage.FileSystem;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ObjectBuilders.AI.Bot;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game.SessionComponents;
using VRage.Game.VisualScripting;
using VRage.Network;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRage.Input;
using Sandbox.Game.Components;
using Sandbox.Game.Entities.Inventory;
using Sandbox.Game.Weapons;
using Sandbox.Game.Audio;
using VRage.Audio;
using VRage.Data.Audio;
using Sandbox.Game.GUI;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Screens;
using Sandbox.Graphics;

using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.AI;
using Sandbox.Game.GameSystems.Conveyors;

#endregion

namespace Sandbox.Game
{
    #region Delegates

    [VisualScriptingEvent(new[] { true })]
    public delegate void SingleKeyEntityNameEvent(string entityName);

    [VisualScriptingEvent(new[] { true })]
    public delegate void CutsceneEvent(string cutsceneName);

    [VisualScriptingEvent(new []{ true, true })]
    public delegate void SingleKeyEntityNameGridNameEvent(string entityName, string gridName, string typeId, string subtypeId);

    [VisualScriptingEvent(new[] { false })]
    public delegate void SingleKeyPlayerEvent(long playerId);

    [VisualScriptingEvent(new[] { true, false })]
    public delegate void DoubleKeyPlayerEvent(string entityName, long playerId, string gridName);

    [VisualScriptingEvent(new[] { true, true, true, false, false })]
    public delegate void FloatingObjectPlayerEvent(string itemTypeName, string itemSubTypeName, string entityName, long playerId, int amount);

    [VisualScriptingEvent(new[] { true, true, false, false })]
    public delegate void PlayerItemEvent(string itemTypeName, string itemSubTypeName, long playerId, int amount);

    [VisualScriptingEvent(new []{ true, true, true})]
    public delegate void BlockEvent(string typeId, string subtypeId, string gridName, long blockId);

    [VisualScriptingEvent(new[] { true, true, false, false, false })]
    public delegate void ItemSpawnedEvent(string itemTypeName, string itemSubTypeName, long itemId, int amount, Vector3D position);

    [VisualScriptingEvent]
    public delegate void ScreenManagerEvent(MyGuiScreenBase screen);

    #endregion

    [StaticEventOwner]
    public static class MyVisualScriptLogicProvider
    {
        #region Base

        // Flyability system
        private static readonly Dictionary<Vector3I, bool> m_thrustDirections = new Dictionary<Vector3I, bool>();

        // Notification system
        private static readonly Dictionary<int,MyHudNotification> m_addedNotificationsById = new Dictionary<int, MyHudNotification>();
        private static int m_notificationIdCounter;

        public static DoubleKeyPlayerEvent PlayerLeftCockpit;
        public static DoubleKeyPlayerEvent PlayerEnteredCockpit;

        public static CutsceneEvent CutsceneNodeEvent;
        public static CutsceneEvent CutsceneEnded;

        public static SingleKeyPlayerEvent PlayerSpawned;
        public static SingleKeyPlayerEvent PlayerDied;
        public static SingleKeyPlayerEvent PlayerConnected;
        public static SingleKeyPlayerEvent PlayerDisconnected;
        public static SingleKeyEntityNameEvent NPCDied;

        public static SingleKeyEntityNameEvent TimerBlockTriggered;
        public static SingleKeyEntityNameEvent TimerBlockTriggeredEntityName;

        public static FloatingObjectPlayerEvent PlayerPickedUp;
        public static PlayerItemEvent PlayerDropped;
        public static ItemSpawnedEvent ItemSpawned;

        public static SingleKeyTriggerEvent AreaTrigger_Left;
        public static SingleKeyTriggerEvent AreaTrigger_Entered;

        public static ScreenManagerEvent ScreenAdded;
        public static ScreenManagerEvent ScreenRemoved;

        public static SingleKeyEntityNameGridNameEvent BlockDestroyed;
        public static BlockEvent BlockBuilt;
        private static MyStringId MUSIC = MyStringId.GetOrCompute("Music");
        public static bool GameIsReady = false;

        private static bool m_registered = false;
        public static void Init()
        {
            // block built events from cube grids
            MyCubeGrids.BlockBuilt += (grid, block) =>
            {
                if (BlockBuilt != null)
                {
                    BlockBuilt(
                        block.BlockDefinition.Id.TypeId.ToString(),
                        block.BlockDefinition.Id.SubtypeName,
                        grid.Name,
                        block.FatBlock != null ? block.FatBlock.EntityId : 0);
                }
            };

            if (m_registered)
                return;

            m_registered = true;

            // Session unload clear operations
            MySession.OnLoading += () =>
            {
                m_addedNotificationsById.Clear();
                m_playerIdsToHighlightData.Clear();
            };

            // Entity Added should always call block built
            MyEntities.OnEntityAdd += entity =>
            {
                var grid = entity as MyCubeGrid;
                if(grid != null && BlockBuilt != null)
                {
                    if(grid.BlocksCount == 1)
                    {
                        var block = grid.GetCubeBlock(Vector3I.Zero);
                        if(block != null)
                            BlockBuilt(block.BlockDefinition.Id.TypeId.ToString(), 
                                block.BlockDefinition.Id.SubtypeName, 
                                grid.Name, 
                                block.FatBlock != null ? block.FatBlock.EntityId : 0);
                    }
                }
            };

            MyScreenManager.ScreenRemoved += screen =>
            {
                if(ScreenRemoved != null)
                    ScreenRemoved(screen);
            };

            MyScreenManager.ScreenAdded += screen =>
            {
                if(ScreenAdded != null)
                    ScreenAdded(screen);
            };

            // Register types that are used in VS but are not part of VRage
            MyVisualScriptingProxy.RegisterType(typeof(MyGuiSounds));
            MyVisualScriptingProxy.RegisterType(typeof(MyKeys));
            MyVisualScriptingProxy.RegisterType(typeof(Sandbox.Game.Entities.MyRemoteControl.FlightMode));
            
            MyVisualScriptingProxy.WhitelistExtensions(typeof(MyVisualScriptLogicProvider));
        }

        #endregion

        #region SupportFunctions

        private static bool TryGetGrid(MyEntity entity, out MyCubeGrid grid)
        {
            if (entity is MyCubeGrid)
            {
                grid = (MyCubeGrid)entity;
                return true;
            }
            else if (entity is MyCubeBlock)
            {
                grid = ((MyCubeBlock)entity).CubeGrid;
                return true;
            }
            else
            {
                grid = null;
                return false;
            }
        }

        private static bool TryGetGrid(string entityName, out MyCubeGrid grid)
        {
            grid = null;
            MyEntity entity = GetEntityByName(entityName);
            if (entity == null)
                return false;

            if (entity is MyCubeGrid)
            {
                grid = (MyCubeGrid)entity;
                return true;
            }
            else if (entity is MyCubeBlock)
            {
                grid = ((MyCubeBlock)entity).CubeGrid;
                return true;
            }

            return false;
        }

        #endregion

        #region AI

        [VisualScriptingMiscData("AI")]
        [VisualScriptingMember(true)]
        public static void SetDroneBehaviourBasic(string entityName, string presetName = "Default")
        {
            SetDroneBehaviourMethod(entityName, presetName, null, null, true, true, 10, TargetPrioritization.PriorityRandom, 10000, false);
        }

        [VisualScriptingMiscData("AI")]
        [VisualScriptingMember(true)]
        public static void SetDroneBehaviourAdvanced(string entityName, string presetName = "Default", bool activate = true, bool assignToPirates = true,
            List<MyEntity> waypoints = null, bool cycleWaypoints = false, List<MyEntity> targets = null)
        {
            List<DroneTarget> targetPairs = DroneProcessTargets(targets);
            SetDroneBehaviourMethod(entityName, presetName, waypoints, targetPairs, activate, assignToPirates, 10, TargetPrioritization.PriorityRandom, 10000, cycleWaypoints);
        }

        [VisualScriptingMiscData("AI")]
        [VisualScriptingMember(true)]
        public static void SetDroneBehaviourFull(string entityName, string presetName = "Default", bool activate = true, bool assignToPirates = true,
            List<MyEntity> waypoints = null, bool cycleWaypoints = false, List<MyEntity> targets = null, int playerPriority = 10, float maxPlayerDistance = 10000, 
            TargetPrioritization prioritizationStyle = TargetPrioritization.PriorityRandom)
        {
            List<DroneTarget> targetPairs = DroneProcessTargets(targets);
            SetDroneBehaviourMethod(entityName, presetName, waypoints, targetPairs, activate, assignToPirates, playerPriority, prioritizationStyle, maxPlayerDistance, cycleWaypoints);
        }

        private static List<DroneTarget> DroneProcessTargets(List<MyEntity> targets)
        {
            List<DroneTarget> targetPairs = new List<DroneTarget>();
            if (targets != null)
            {
                foreach (MyEntity t in targets)
                {
                    if (t is MyCubeGrid)
                    {
                        foreach (var block in ((MyCubeGrid)t).GetBlocks())
                        {
                            if (block.FatBlock is MyShipController)
                                targetPairs.Add(new DroneTarget((MyEntity)block.FatBlock, 8));

                            if (block.FatBlock is MyReactor)
                                targetPairs.Add(new DroneTarget((MyEntity)block.FatBlock, 6));

                            if (block.FatBlock is MyUserControllableGun)
                                targetPairs.Add(new DroneTarget((MyEntity)block.FatBlock, 10));
                        }
                    }
                    else
                    {
                        targetPairs.Add(new DroneTarget(t));
                    }
                }
            }
            return targetPairs;
        }

        private static MyRemoteControl DroneGetRemote(string entityName)
        {
            var entity = GetEntityByName(entityName);
            if (entity == null)
            {
                Debug.Fail("Entity of name \"" + entityName + "\" was not found.");
                return null;
            }

            MyRemoteControl remote = entity as MyRemoteControl;
            if (entity is MyCubeBlock && remote == null)
                entity = ((MyCubeBlock)entity).CubeGrid;

            if (entity is MyCubeGrid)
            {
                foreach (var block in ((MyCubeGrid)entity).GetBlocks())
                {
                    if (block.FatBlock is MyRemoteControl)
                    {
                        remote = block.FatBlock as MyRemoteControl;
                        break;
                    }
                }
            }
            Debug.Assert(remote != null, "Remote control was not found.");
            return remote;
        }

        private static void SetDroneBehaviourMethod(string entityName, string presetName, List<MyEntity> waypoints, List<DroneTarget> targets,
            bool activate, bool assignToPirates, int playerPriority, TargetPrioritization prioritizationStyle, float maxPlayerDistance, bool cycleWaypoints)
        {
            MyRemoteControl remote = DroneGetRemote(entityName);
            if (remote != null)
            {
                if (waypoints != null)
                {
                    // Remove all null waypoints -- bad input protection.
                    for (var index = 0; index < waypoints.Count;)
                    {
                        if (waypoints[index] == null)
                        {
                            waypoints.RemoveAtFast(index);
                        }
                        else
                        {
                            index++;
                        }
                    }
                }

                var blocks = remote.CubeGrid.GetBlocks();
                if (assignToPirates)
                    remote.CubeGrid.ChangeGridOwnership(GetPirateId(), MyOwnershipShareModeEnum.Faction);
                MyDroneStrafeBehaviour behaviourComponent = new MyDroneStrafeBehaviour(remote, presetName, activate, waypoints, targets, playerPriority, prioritizationStyle, maxPlayerDistance, cycleWaypoints);
                remote.SetAutomaticBehaviour(behaviourComponent);
                if (activate)
                    remote.SetAutoPilotEnabled(true);
            }
        }

        [VisualScriptingMiscData("AI")]
        [VisualScriptingMember]
        public static int DroneGetWaypointCount(string entityName)
        {
            MyRemoteControl remote = DroneGetRemote(entityName);
            if (remote != null && remote.AutomaticBehaviour != null)
                return remote.AutomaticBehaviour.WaypointList.Count + (remote.AutomaticBehaviour.WaypointActive ? 1 : 0);
            else
                return -1;
        }

        [VisualScriptingMiscData("AI")]
        [VisualScriptingMember]
        public static int DroneGetTargetsCount(string entityName)
        {
            MyRemoteControl remote = DroneGetRemote(entityName);
            if (remote != null && remote.AutomaticBehaviour != null)
                return remote.AutomaticBehaviour.TargetList.Count;
            else
                return -1;
        }

        [VisualScriptingMiscData("AI")]
        [VisualScriptingMember(true)]
        public static void DroneWaypointAdd(string entityName, MyEntity waypoint)
        {
            MyRemoteControl remote = DroneGetRemote(entityName);
            if (remote != null && remote.AutomaticBehaviour != null)
                remote.AutomaticBehaviour.WaypointAdd(waypoint);
        }

        [VisualScriptingMiscData("AI")]
        [VisualScriptingMember(true)]
        public static void DroneWaypointSetCycling(string entityName, bool cycleWaypoints = true)
        {
            MyRemoteControl remote = DroneGetRemote(entityName);
            if (remote != null && remote.AutomaticBehaviour != null)
                remote.AutomaticBehaviour.CycleWaypoints = cycleWaypoints;
        }

        [VisualScriptingMiscData("AI")]
        [VisualScriptingMember(true)]
        public static void DroneSetPlayerPriority(string entityName, int priority)
        {
            MyRemoteControl remote = DroneGetRemote(entityName);
            if (remote != null && remote.AutomaticBehaviour != null)
                remote.AutomaticBehaviour.PlayerPriority = priority;
        }

        [VisualScriptingMiscData("AI")]
        [VisualScriptingMember(true)]
        public static void DroneSetPrioritizationStyle(string entityName, TargetPrioritization style)
        {
            MyRemoteControl remote = DroneGetRemote(entityName);
            if (remote != null && remote.AutomaticBehaviour != null)
                remote.AutomaticBehaviour.PrioritizationStyle = style;
        }

        [VisualScriptingMiscData("AI")]
        [VisualScriptingMember(true)]
        public static void DroneWaypointClear(string entityName)
        {
            MyRemoteControl remote = DroneGetRemote(entityName);
            if (remote != null && remote.AutomaticBehaviour != null)
                remote.AutomaticBehaviour.WaypointClear();
        }

        [VisualScriptingMiscData("AI")]
        [VisualScriptingMember(true)]
        public static void DroneTargetAdd(string entityName, MyEntity target, int priority = 1)
        {
            MyRemoteControl remote = DroneGetRemote(entityName);
            if (remote != null && remote.AutomaticBehaviour != null)
            {
                if (target is MyCubeGrid)
                {
                    List<DroneTarget> targetPairs = DroneProcessTargets(new List<MyEntity>() { target });
                    foreach (var targetEntity in targetPairs)
                        remote.AutomaticBehaviour.TargetAdd(targetEntity);
                }
                else
                    remote.AutomaticBehaviour.TargetAdd(new DroneTarget(target, priority));
            }
        }

        [VisualScriptingMiscData("AI")]
        [VisualScriptingMember(true)]
        public static void DroneTargetClear(string entityName)
        {
            MyRemoteControl remote = DroneGetRemote(entityName);
            if (remote != null && remote.AutomaticBehaviour != null)
                remote.AutomaticBehaviour.TargetClear();
        }

        [VisualScriptingMiscData("AI")]
        [VisualScriptingMember(true)]
        public static void DroneTargetRemove(string entityName, MyEntity target)
        {
            MyRemoteControl remote = DroneGetRemote(entityName);
            if (remote != null && remote.AutomaticBehaviour != null)
                remote.AutomaticBehaviour.TargetRemove(target);
        }

        [VisualScriptingMiscData("AI")]
        [VisualScriptingMember(true)]
        public static void DroneTargetLoseCurrent(string entityName)
        {
            MyRemoteControl remote = DroneGetRemote(entityName);
            if (remote != null && remote.AutomaticBehaviour != null)
                remote.AutomaticBehaviour.TargetLoseCurrent();
        }

        [VisualScriptingMiscData("AI")]
        [VisualScriptingMember(true)]
        public static void DroneSetCollisionAvoidance(string entityName, bool collisionAvoidance = true)
        {
            MyRemoteControl remote = DroneGetRemote(entityName);
            if (remote != null && remote.AutomaticBehaviour != null)
            {
                remote.SetCollisionAvoidance(collisionAvoidance);
                remote.AutomaticBehaviour.CollisionAvoidance = collisionAvoidance;
            }
        }

        [VisualScriptingMiscData("AI")]
        [VisualScriptingMember(true)]
        public static void DroneSetRetreatPosition(string entityName, Vector3D position)
        {
            MyRemoteControl remote = DroneGetRemote(entityName);
            if (remote != null && remote.AutomaticBehaviour != null)
            {
                remote.AutomaticBehaviour.OriginPoint = position;
            }
        }

        [VisualScriptingMiscData("AI")]
        [VisualScriptingMember(true)]
        public static void DroneSetRotateToTarget(string entityName, bool rotateToTarget = true)
        {
            MyRemoteControl remote = DroneGetRemote(entityName);
            if (remote != null && remote.AutomaticBehaviour != null)
                remote.AutomaticBehaviour.RotateToTarget = rotateToTarget;
        }

        [VisualScriptingMiscData("AI")]
        [VisualScriptingMember(true)]
        public static void AddGridToTargetList(string gridName, string targetGridname)
        {
            MyCubeGrid originGrid;
            if(TryGetGrid(gridName, out originGrid)){
                MyCubeGrid targetGrid;
                if (TryGetGrid(targetGridname, out targetGrid))
                {
                    originGrid.TargetingAddId(targetGrid.EntityId);
                }
                else
                {
                    Debug.Fail("Grid or block with name \"" + targetGridname + "\" was not found.");
                }
            }
            else
            {
                Debug.Fail("Grid or block with name \"" + gridName + "\" was not found.");
            }
        }

        [VisualScriptingMiscData("AI")]
        [VisualScriptingMember(true)]
        public static void RemoveGridFromTargetList(string gridName, string targetGridname)
        {
            MyCubeGrid originGrid;
            if (TryGetGrid(gridName, out originGrid))
            {
                MyCubeGrid targetGrid;
                if (TryGetGrid(targetGridname, out targetGrid))
                {
                    originGrid.TargetingRemoveId(targetGrid.EntityId);
                }
                else
                {
                    Debug.Fail("Grid or block with name \"" + targetGridname + "\" was not found.");
                }
            }
            else
            {
                Debug.Fail("Grid or block with name \"" + gridName + "\" was not found.");
            }
        }

        [VisualScriptingMiscData("AI")]
        [VisualScriptingMember(true)]
        public static void TargetingSetWhitelist(string gridName, bool whitelistMode = true)
        {
            MyCubeGrid originGrid;
            if (TryGetGrid(gridName, out originGrid))
            {
                originGrid.TargetingSetWhitelist(whitelistMode);
            }
            else
            {
                Debug.Fail("Grid or block with name \"" + gridName + "\" was not found.");
            }
        }

        [VisualScriptingMiscData("AI")]
        [VisualScriptingMember(true)]
        public static void AutopilotGoToPosition(string entityName, Vector3D position, string waypointName = "Waypoint", float speedLimit = 120f, bool collisionAvoidance = true, bool precisionMode = false)
        {
            MyRemoteControl remote = DroneGetRemote(entityName);
            if (remote != null)
            {
                remote.SetCollisionAvoidance(collisionAvoidance);
                remote.SetAutoPilotSpeedLimit(speedLimit);
                remote.ChangeFlightMode(Sandbox.Game.Entities.MyRemoteControl.FlightMode.OneWay);
                remote.SetDockingMode(precisionMode);
                remote.ClearWaypoints();
                remote.AddWaypoint(position, waypointName);
                remote.SetAutoPilotEnabled(true);
            }
        }

        [VisualScriptingMiscData("AI")]
        [VisualScriptingMember(true)]
        public static void AutopilotClearWaypoints(string entityName)
        {
            MyRemoteControl remote = DroneGetRemote(entityName);
            if (remote != null)
            {
                remote.ClearWaypoints();
            }
        }

        [VisualScriptingMiscData("AI")]
        [VisualScriptingMember(true)]
        public static void AutopilotAddWaypoint(string entityName, Vector3D position, string waypointName = "Waypoint")
        {
            MyRemoteControl remote = DroneGetRemote(entityName);
            if (remote != null)
            {
                remote.AddWaypoint(position, waypointName);
            }
        }

        [VisualScriptingMiscData("AI")]
        [VisualScriptingMember(true)]
        public static void AutopilotSetWaypoints(string entityName, List<Vector3D> positions, string waypointName = "Waypoint")
        {
            MyRemoteControl remote = DroneGetRemote(entityName);
            if (remote != null)
            {
                remote.ClearWaypoints();
                if (positions != null)
                {
                    for (int i = 0; i < positions.Count; i++)
                        remote.AddWaypoint(positions[i], waypointName + " " + (i + 1).ToString());
                }
            }
        }

        [VisualScriptingMiscData("AI")]
        [VisualScriptingMember(true)]
        public static void AutopilotEnabled(string entityName, bool enabled = true)
        {
            MyRemoteControl remote = DroneGetRemote(entityName);
            if (remote != null)
            {
                remote.SetAutoPilotEnabled(enabled);
            }
        }

        [VisualScriptingMiscData("AI")]
        [VisualScriptingMember(true)]
        public static void AutopilotActivate(string entityName, Sandbox.Game.Entities.MyRemoteControl.FlightMode mode = MyRemoteControl.FlightMode.OneWay, float speedLimit = 120f, bool collisionAvoidance = true, bool precisionMode = false)
        {
            MyRemoteControl remote = DroneGetRemote(entityName);
            if (remote != null)
            {
                remote.SetCollisionAvoidance(collisionAvoidance);
                remote.SetAutoPilotSpeedLimit(speedLimit);
                remote.ChangeFlightMode(mode);
                remote.SetDockingMode(precisionMode);
                remote.SetAutoPilotEnabled(true);
            }
        }

        #endregion

        #region Audio

        [VisualScriptingMiscData("Audio")]
        [VisualScriptingMember(true)]
        public static void MusicPlayMusicCue(string cueName, bool playAtLeastOnce = true)
        {
            if (MyAudio.Static == null)
                return;
            if (MyMusicController.Static == null)
            {
                MyMusicController.Static = new MyMusicController(MyAudio.Static.GetAllMusicCues());
                MyAudio.Static.MusicAllowed = false;
                MyMusicController.Static.Active = true;
            }
            MyCueId cue = MyAudio.Static.GetCueId(cueName);
            if (!cue.IsNull)
            {
                MySoundData soundData = MyAudio.Static.GetCue(cue);
                if (soundData.Category.Equals(MUSIC))
                {
                    MyMusicController.Static.PlaySpecificMusicTrack(cue, playAtLeastOnce);
                }
                else
                {
                    Debug.Fail("Cue of name \"" + cueName + "\" is not music.");
                }
            }
            else
            {
                Debug.Fail("Cue of name \"" + cueName + "\" was not found.");
            }
        }

        [VisualScriptingMiscData("Audio")]
        [VisualScriptingMember(true)]
        public static void MusicPlayMusicCategory(string categoryName, bool playAtLeastOnce = true)
        {
            if (MyAudio.Static == null)
                return;
            if (MyMusicController.Static == null)
            {
                MyMusicController.Static = new MyMusicController(MyAudio.Static.GetAllMusicCues());
                MyAudio.Static.MusicAllowed = false;
                MyMusicController.Static.Active = true;
            }
            MyStringId category = MyStringId.GetOrCompute(categoryName);
            if (category.Id != 0)
            {
                MyMusicController.Static.PlaySpecificMusicCategory(category, playAtLeastOnce);
            }
        }

        [VisualScriptingMiscData("Audio")]
        [VisualScriptingMember(true)]
        public static void MusicSetMusicCategory(string categoryName)
        {
            if (MyAudio.Static == null)
                return;
            if (MyMusicController.Static == null)
            {
                MyMusicController.Static = new MyMusicController(MyAudio.Static.GetAllMusicCues());
                MyAudio.Static.MusicAllowed = false;
                MyMusicController.Static.Active = true;
            }
            MyStringId category = MyStringId.GetOrCompute(categoryName);
            if (category.Id != 0)
            {
                MyMusicController.Static.SetSpecificMusicCategory(category);
            }
        }

        [VisualScriptingMiscData("Audio")]
        [VisualScriptingMember(true)]
        public static void MusicSetDynamicMusic(bool enabled)
        {
            if (MyAudio.Static == null)
                return;
            if (MyMusicController.Static == null)
            {
                MyMusicController.Static = new MyMusicController(MyAudio.Static.GetAllMusicCues());
                MyAudio.Static.MusicAllowed = false;
                MyMusicController.Static.Active = true;
            }
            MyMusicController.Static.CanChangeCategoryGlobal = enabled;
        }

        [VisualScriptingMiscData("Audio")]
        [VisualScriptingMember(true)]
        public static void PlaySingleSoundAtEntity(string soundName, string entityName)
        {
            if (MyAudio.Static == null)
                return;
            if (soundName.Length > 0)
            {
                MySoundPair sound = new MySoundPair(soundName);
                if (sound != MySoundPair.Empty)
                {
                    MyEntity entity = GetEntityByName(entityName);
                    if (entity != null)
                    {
                        MyEntity3DSoundEmitter emitter = MyAudioComponent.TryGetSoundEmitter();
                        if(emitter != null)
                        {
                            emitter.Entity = entity;
                            emitter.PlaySound(sound);
                        }
                    }
                }
            }
        }

        [VisualScriptingMiscData("Audio")]
        [VisualScriptingMember(true)]
        public static void PlayHudSound(MyGuiSounds sound = MyGuiSounds.HudClick, long playerId = 0)
        {
            if (MyAudio.Static == null)
                return;
            MyGuiAudio.PlaySound(sound);
        }

        [VisualScriptingMiscData("Audio")]
        [VisualScriptingMember(true)]
        public static void PlaySingleSoundAtPosition(string soundName, Vector3 position)
        {
            if (MyAudio.Static == null)
                return;
            if (soundName.Length > 0)
            {
                MySoundPair sound = new MySoundPair(soundName);
                if (sound != MySoundPair.Empty)
                {
                    MyEntity3DSoundEmitter emitter = MyAudioComponent.TryGetSoundEmitter();
                    if (emitter != null)
                    {
                        emitter.SetPosition(position);
                        emitter.PlaySound(sound);
                    }
                }
            }
        }

        [VisualScriptingMiscData("Audio")]
        [VisualScriptingMember(true)]
        public static void CreateSoundEmitterAtEntity(string newEmitterId, string entityName)
        {
            if (MyAudio.Static == null)
                return;
            if (newEmitterId.Length > 0)
            {
                MyEntity entity = GetEntityByName(entityName);
                if (entity != null)
                {
                    MyEntity3DSoundEmitter emitter = MyAudioComponent.CreateNewLibraryEmitter(newEmitterId, entity);
                }
            }
        }

        [VisualScriptingMiscData("Audio")]
        [VisualScriptingMember(true)]
        public static void CreateSoundEmitterAtPosition(string newEmitterId, Vector3 position)
        {
            if (MyAudio.Static == null)
                return;
            if (newEmitterId.Length > 0)
            {
                MyEntity3DSoundEmitter emitter = MyAudioComponent.CreateNewLibraryEmitter(newEmitterId);
                if (emitter != null)
                    emitter.SetPosition(position);
            }
        }

        [VisualScriptingMiscData("Audio")]
        [VisualScriptingMember(true)]
        public static void PlaySound(string EmitterId, string soundName, bool playIn2D = false)
        {
            if (MyAudio.Static == null)
                return;
            if (EmitterId.Length > 0)
            {
                MySoundPair sound = new MySoundPair(soundName);
                if (sound != MySoundPair.Empty)
                {
                    MyEntity3DSoundEmitter emitter = MyAudioComponent.GetLibraryEmitter(EmitterId);
                    if (emitter != null)
                    {
                        emitter.PlaySound(sound, true, force2D: playIn2D);
                    }
                }
            }
        }

        [VisualScriptingMiscData("Audio")]
        [VisualScriptingMember(true)]
        public static void StopSound(string EmitterId, bool forced = false)
        {
            if (MyAudio.Static == null)
                return;
            if (EmitterId.Length > 0)
            {
                MyEntity3DSoundEmitter emitter = MyAudioComponent.GetLibraryEmitter(EmitterId);
                if (emitter != null)
                    emitter.StopSound(forced);
            }
        }

        [VisualScriptingMiscData("Audio")]
        [VisualScriptingMember(true)]
        public static void RemoveSoundEmitter(string EmitterId)
        {
            if (MyAudio.Static == null)
                return;
            if (EmitterId.Length > 0)
            {
                MyAudioComponent.RemoveLibraryEmitter(EmitterId);
            }
        }

        #endregion

        #region BlocksGeneric

        [VisualScriptingMiscData("BlocksGeneric")]
        [VisualScriptingMember(true)]
        public static void EnableBlock(string blockName)
        {
            SetBlockState(blockName, true);
        }

        [VisualScriptingMiscData("BlocksGeneric")]
        [VisualScriptingMember(true)]
        public static void DisableBlock(string blockName)
        {
            SetBlockState(blockName, false);
        }

        [VisualScriptingMiscData("BlocksGeneric")]
        [VisualScriptingMember]
        public static bool IsConveyorConnected(string firstBlock, string secondBlock)
        {
            MyEntity entity;
            if (firstBlock.Equals(secondBlock))
                return true;

            if (MyEntities.TryGetEntityByName(firstBlock, out entity))
            {
                IMyConveyorEndpointBlock block1 = entity as IMyConveyorEndpointBlock;
                if (block1 != null && MyEntities.TryGetEntityByName(secondBlock, out entity))
                {
                    IMyConveyorEndpointBlock block2 = entity as IMyConveyorEndpointBlock;
                    if (block2 != null)
                    {
                        return MyGridConveyorSystem.Reachable(block1.ConveyorEndpoint, block2.ConveyorEndpoint);
                    }
                }
            }
            return false;
        }

        private static void SetBlockState(string name, bool state)
        {
            MyEntity entity;

            if (MyEntities.TryGetEntityByName(name, out entity))
            {
                if (entity is MyFunctionalBlock)
                {
                    (entity as MyFunctionalBlock).Enabled = state;
                }
            }
        }

        [VisualScriptingMiscData("BlocksGeneric")]
        [VisualScriptingMember(true)]
        public static void SetBlockEnabled(string blockName, bool enabled = true)
        {
            SetBlockState(blockName, enabled);
        }

        [VisualScriptingMiscData("BlocksGeneric")]
        [VisualScriptingMember]
        public static bool IsBlockFunctional(string name)
        {
            MyEntity entity;

            if (MyEntities.TryGetEntityByName(name, out entity))
            {
                if (entity is MyCubeBlock)
                {
                    return (entity as MyCubeBlock).IsFunctional;
                }
            }
            return false;
        }

        [VisualScriptingMiscData("BlocksGeneric")]
        [VisualScriptingMember]
        public static bool IsBlockPowered(string name)
        {
            MyEntity entity;

            if (MyEntities.TryGetEntityByName(name, out entity))
            {
                if (entity is MyFunctionalBlock)
                {
                    return (entity as MyFunctionalBlock).ResourceSink != null && (entity as MyFunctionalBlock).ResourceSink.IsPowered;
                }
            }
            return false;
        }

        [VisualScriptingMiscData("BlocksGeneric")]
        [VisualScriptingMember]
        public static bool IsBlockEnabled(string name)
        {
            MyEntity entity;

            if (MyEntities.TryGetEntityByName(name, out entity))
            {
                if (entity is MyFunctionalBlock)
                {
                    return (entity as MyFunctionalBlock).Enabled;
                }
            }
            return false;
        }

        [VisualScriptingMiscData("BlocksGeneric")]
        [VisualScriptingMember]
        public static bool IsBlockWorking(string name)
        {
            MyEntity entity;

            if (MyEntities.TryGetEntityByName(name, out entity))
            {
                if (entity is MyFunctionalBlock)
                {
                    return (entity as MyFunctionalBlock).IsWorking;
                }
            }
            return false;
        }

        [VisualScriptingMiscData("BlocksGeneric")]
        [VisualScriptingMember(true)]
        public static void SetBlockGeneralDamageModifier(string blockName, float modifier = 1f)
        {
            MyEntity entity;

            if (MyEntities.TryGetEntityByName(blockName, out entity))
            {
                if (entity is MyCubeBlock)
                {
                    ((MyCubeBlock)entity).SlimBlock.BlockGeneralDamageModifier = modifier;
                }
            }
        }

        [VisualScriptingMiscData("BlocksGeneric")]
        [VisualScriptingMember]
        public static long GetGridIdOfBlock(string entityName)
        {
            MyCubeBlock block = GetEntityByName(entityName) as MyCubeBlock;
            Debug.Assert(block != null, "Block of name " + entityName + " was not found.");
            if (block == null) return 0;
            return block.CubeGrid.EntityId;
        }

        [VisualScriptingMiscData("BlocksGeneric")]
        [VisualScriptingMember]
        public static float GetBlockHealth(string entityName, bool buildIntegrity = true)
        {
            MyCubeBlock block = GetEntityByName(entityName) as MyCubeBlock;
            Debug.Assert(block != null, "Block of name " + entityName + " was not found.");
            if (block != null)
            {
                if (buildIntegrity)
                    return block.SlimBlock.BuildIntegrity;
                else
                    return block.SlimBlock.Integrity;
            }
            return 0f;
        }

        [VisualScriptingMiscData("BlocksGeneric")]
        [VisualScriptingMember(true)]
        public static void SetBlockHealth(string entityName, float integrity = 1f, bool damageChange = true, long changeOwner = 0)
        {
            MyCubeBlock block = GetEntityByName(entityName) as MyCubeBlock;
            Debug.Assert(block != null, "Block of name " + entityName + " was not found.");
            if (block != null)
            {
                if (damageChange)
                    block.SlimBlock.SetIntegrity(block.SlimBlock.BuildIntegrity, integrity, MyIntegrityChangeEnum.Damage, changeOwner);
                else
                    block.SlimBlock.SetIntegrity(integrity, integrity, MyIntegrityChangeEnum.Repair, changeOwner);
            }
        }

        [VisualScriptingMiscData("BlocksGeneric")]
        [VisualScriptingMember(true)]
        public static void DamageBlock(string entityName, float damage = 0f, long damageOwner = 0)
        {
            MyCubeBlock block = GetEntityByName(entityName) as MyCubeBlock;
            Debug.Assert(block != null, "Block of name " + entityName + " was not found.");
            if (block != null)
            {
                block.SlimBlock.DoDamage(damage, MyDamageType.Destruction, attackerId: damageOwner);
            }
        }

        #endregion

        #region BlocksSpecific

        [VisualScriptingMiscData("BlocksSpecific")]
        [VisualScriptingMember]
        public static int GetMergeBlockStatus(string mergeBlockName)
        {
            MyEntity entity;
            MyEntities.TryGetEntityByName(mergeBlockName, out entity);

            Debug.Assert(entity != null, "Entity of name " + mergeBlockName + " was not found.");
            if (entity == null) return -1;

            MyFunctionalBlock mergeBlock = entity as MyFunctionalBlock;
            if (mergeBlock != null)
                return mergeBlock.GetBlockSpecificState();
            else
                Debug.Fail("Entity is not a MergeBlock: " + mergeBlockName);

            return -1;
        }

        [VisualScriptingMiscData("BlocksSpecific")]
        [VisualScriptingMember(true)]
        public static void WeaponShootOnce(string weaponName)
        {
            MyEntity entity;

            if (MyEntities.TryGetEntityByName(weaponName, out entity))
            {
                if (entity is MyUserControllableGun)
                {
                    (entity as MyUserControllableGun).ShootFromTerminal(entity.WorldMatrix.Forward);
                }
            }
        }

        [VisualScriptingMiscData("BlocksSpecific")]
        [VisualScriptingMember(true)]
        public static void WeaponSetShooting(string weaponName, bool shooting = true)
        {
            MyEntity entity;

            if (MyEntities.TryGetEntityByName(weaponName, out entity))
            {
                if (entity is MyUserControllableGun)
                {
                    (entity as MyUserControllableGun).SetShooting(shooting);
                }
            }
        }

        [VisualScriptingMiscData("BlocksSpecific")]
        [VisualScriptingMember(true)]
        public static void StartTimerBlock(string blockName)
        {
            MyEntity entity;
            if (!MyEntities.TryGetEntityByName(blockName, out entity))
                return;

            IMyFunctionalBlock block = entity as IMyFunctionalBlock;
            if (block != null)
            {
                block.ApplyAction("Start");
            }
        }

        [VisualScriptingMiscData("BlocksSpecific")]
        [VisualScriptingMember(true)]
        public static void SetLandingGearLock(string entityName, bool locked = true)
        {
            MyEntity entity = GetEntityByName(entityName);
            Debug.Assert(entity != null, "Designers: Entity was not found: " + entityName);

            IMyLandingGear landingGear = entity as IMyLandingGear;
            Debug.Assert(landingGear != null, "Entity of name: " + entityName + " is not a landingGear.");
            if (landingGear != null)
            {
                landingGear.RequestLock(locked);
            }
        }

        [VisualScriptingMiscData("BlocksSpecific")]
        [VisualScriptingMember]
        public static bool IsLandingGearLocked(string entityName)
        {
            MyEntity entity = GetEntityByName(entityName);
            Debug.Assert(entity != null, "Entity of name " + entityName + " was not found.");
            if (entity == null) return false;

            IMyLandingGear landingGear = entity as IMyLandingGear;
            if (landingGear != null)
            {
                if (landingGear.LockMode == LandingGearMode.Locked)
                    return true;
            }
            else
            {
                Debug.Fail("Entity is not a LandingGear: " + entityName);
            }

            return false;
        }

        [VisualScriptingMiscData("BlocksSpecific")]
        [VisualScriptingMember]
        public static bool GetLandingGearInformation(string entityName, out bool locked, out bool inConstraint, out string attachedType, out string attachedName)
        {
            locked = false;
            inConstraint = false;
            attachedType = "";
            attachedName = "";

            MyEntity entity = GetEntityByName(entityName);
            Debug.Assert(entity != null, "Entity of name " + entityName + " was not found.");
            if (entity == null) return false;

            IMyLandingGear landingGear = entity as IMyLandingGear;
            if (landingGear != null)
            {
                locked = landingGear.LockMode == LandingGearMode.Locked;
                inConstraint = landingGear.LockMode == LandingGearMode.ReadyToLock;
                if (locked)
                {
                    MyEntity other = landingGear.GetAttachedEntity() as MyEntity;
                    if (other != null)
                    {
                        attachedType = other is MyCubeBlock ? "Block" : (other is MyCubeGrid ? "Grid" : (other is MyVoxelBase ? "Voxel" : "Other"));
                        attachedName = other.Name;
                    }
                }
                return true;
            }
            else
            {
                Debug.Fail("Entity is not a LandingGear: " + entityName);
            }

            return false;
        }

        [VisualScriptingMiscData("BlocksSpecific")]
        [VisualScriptingMember]
        public static bool GetLandingGearInformationFromEntity(MyEntity entity, out bool locked, out bool inConstraint, out string attachedType, out string attachedName)
        {
            locked = false;
            inConstraint = false;
            attachedType = "";
            attachedName = "";

            if (entity == null) return false;

            IMyLandingGear landingGear = entity as IMyLandingGear;
            if (landingGear != null)
            {
                locked = landingGear.LockMode == LandingGearMode.Locked;
                inConstraint = landingGear.LockMode == LandingGearMode.ReadyToLock;
                if (locked)
                {
                    MyEntity other = landingGear.GetAttachedEntity() as MyEntity;
                    if (other != null)
                    {
                        attachedType = other is MyCubeBlock ? "Block" : (other is MyCubeGrid ? "Grid" : (other is MyVoxelBase ? "Voxel" : "Other"));
                        attachedName = other.Name;
                    }
                }
                return true;
            }
            else
            {
                Debug.Fail("Entity is not a LandingGear: " + entity.ToString());
            }

            return false;
        }

        [VisualScriptingMiscData("BlocksSpecific")]
        [VisualScriptingMember]
        public static bool IsConnectorLocked(string connectorName)
        {
            MyEntity entity = GetEntityByName(connectorName);
            Debug.Assert(entity != null, "Entity of name " + connectorName + " was not found.");
            if (entity == null) return false;

            IMyShipConnector connector = entity as IMyShipConnector;
            if (connector != null)
                return connector.IsConnected;
            else
                Debug.Fail("Entity is not a LandingGear: " + connectorName);

            return false;
        }

        [VisualScriptingMiscData("BlocksSpecific")]
        [VisualScriptingMember(true)]
        public static void StopTimerBlock(string blockName)
        {
            MyEntity entity;
            if (!MyEntities.TryGetEntityByName(blockName, out entity))
                return;

            var block = entity as IMyFunctionalBlock;
            if (block != null)
            {
                block.ApplyAction("Stop");
            }
        }

        [VisualScriptingMiscData("BlocksSpecific")]
        [VisualScriptingMember(true)]
        public static void TriggerTimerBlock(string blockName)
        {
            MyEntity entity;
            if (!MyEntities.TryGetEntityByName(blockName, out entity))
                return;

            var block = entity as IMyFunctionalBlock;
            if (block != null)
            {
                block.ApplyAction("TriggerNow");
            }
        }

        [VisualScriptingMiscData("BlocksSpecific")]
        [VisualScriptingMember(true)]
        public static void ChangeDoorState(string doorBlockName, bool open = true)
        {
            MyEntity entity;
            if (MyEntities.TryGetEntityByName(doorBlockName, out entity))
            {
                if (entity is MyAdvancedDoor)
                    (entity as MyAdvancedDoor).Open = open;
                if (entity is MyAirtightDoorGeneric)
                    (entity as MyAirtightDoorGeneric).ChangeOpenClose(open);
                if (entity is MyDoor)
                    (entity as MyDoor).Open = open;
            }
        }

        [VisualScriptingMiscData("BlocksSpecific")]
        [VisualScriptingMember]
        public static bool IsDoorOpen(string doorBlockName)
        {
            MyEntity entity;
            if (MyEntities.TryGetEntityByName(doorBlockName, out entity))
            {
                if (entity is MyAdvancedDoor)
                    return (entity as MyAdvancedDoor).Open;
                if (entity is MyAirtightDoorGeneric)
                    return (entity as MyAirtightDoorGeneric).Open;
                if (entity is MyDoor)
                    return (entity as MyDoor).Open;
            }
            return false;
        }

        [VisualScriptingMiscData("BlocksSpecific")]
        [VisualScriptingMember(true)]
        public static void SetTextPanelDescription(string panelName, string description, bool publicDescription = true)
        {
            MyEntity entity;

            if (MyEntities.TryGetEntityByName(panelName, out entity))
            {
                MyTextPanel panel = entity as MyTextPanel;
                if (panel != null)
                {
                    if (publicDescription)
                        panel.PublicDescription = new StringBuilder(description);
                    else
                        panel.PrivateDescription = new StringBuilder(description);
                }
            }
        }

        [VisualScriptingMiscData("BlocksSpecific")]
        [VisualScriptingMember(true)]
        public static void SetTextPanelColors(string panelName, Color fontColor, Color backgroundColor)
        {
            MyEntity entity;

            if (MyEntities.TryGetEntityByName(panelName, out entity))
            {
                MyTextPanel panel = entity as MyTextPanel;
                if (panel != null)
                {
                    if(fontColor != Color.Transparent)
                        panel.FontColor = fontColor;
                    if (backgroundColor != Color.Transparent)
                        panel.BackgroundColor = backgroundColor;
                }
            }
        }

        [VisualScriptingMiscData("BlocksSpecific")]
        [VisualScriptingMember(true)]
        public static void SetTextPanelTitle(string panelName, string title, bool publicTitle = true)
        {
            MyEntity entity;

            if (MyEntities.TryGetEntityByName(panelName, out entity))
            {
                MyTextPanel panel = entity as MyTextPanel;
                if (panel != null)
                {
                    if (publicTitle)
                        panel.PublicDescription = new StringBuilder(title);
                    else
                        panel.PrivateDescription = new StringBuilder(title);
                }
            }
        }

        [VisualScriptingMiscData("BlocksSpecific")]
        [VisualScriptingMember(true)]
        public static void CockpitRemovePilot(string cockpitName)
        {
            MyEntity entity;

            if (MyEntities.TryGetEntityByName(cockpitName, out entity))
            {
                MyCockpit cockpit = entity as MyCockpit;
                if (cockpit != null)
                {
                    cockpit.RemovePilot();
                }
            }
        }

        [VisualScriptingMiscData("BlocksSpecific")]
        [VisualScriptingMember(true)]
        public static void CockpitInsertPilot(string cockpitName, bool keepOriginalPlayerPosition = true, long playerId = -1)
        {
            MyEntity entity;
            MyCharacter character = GetCharacterFromPlayerId(playerId);
            if (character == null)
                return;

            if (MyEntities.TryGetEntityByName(cockpitName, out entity))
            {
                MyCockpit cockpit = entity as MyCockpit;
                if (cockpit != null)
                {
                    cockpit.RemovePilot();
                    if (character.Parent is MyCockpit)
                        (character.Parent as MyCockpit).RemovePilot();
                    cockpit.AttachPilot(character, keepOriginalPlayerPosition);
                }
            }
        }

        [VisualScriptingMiscData("BlocksSpecific")]
        [VisualScriptingMember(true)]
        public static void SetLigtingBlockColor(string lightBlockName, Color color)
        {
            MyEntity entity;

            if (MyEntities.TryGetEntityByName(lightBlockName, out entity))
            {
                MyLightingBlock light = entity as MyLightingBlock;
                if (light != null)
                {
                    light.Color = color;
                }
            }
        }

        [VisualScriptingMiscData("BlocksSpecific")]
        [VisualScriptingMember(true)]
        public static void SetLigtingBlockIntensity(string lightBlockName, float intensity)
        {
            MyEntity entity;

            if (MyEntities.TryGetEntityByName(lightBlockName, out entity))
            {
                MyLightingBlock light = entity as MyLightingBlock;
                if (light != null)
                {
                    light.Intensity = intensity;
                }
            }
        }

        [VisualScriptingMiscData("BlocksSpecific")]
        [VisualScriptingMember(true)]
        public static void SetLigtingBlockRadius(string lightBlockName, float radius)
        {
            MyEntity entity;

            if (MyEntities.TryGetEntityByName(lightBlockName, out entity))
            {
                MyLightingBlock light = entity as MyLightingBlock;
                if (light != null)
                {
                    light.Radius = radius;
                }
            }
        }

        #endregion

        #region Cutscenes

        [VisualScriptingMiscData("Cutscenes")]
        [VisualScriptingMember(true)]
        public static void StartCutscene(string cutsceneName)
        {
            MyMultiplayer.RaiseStaticEvent(x => StartCutsceneSync, cutsceneName);
        }
        [Event, Reliable, Server, Broadcast]
        private static void StartCutsceneSync(string cutsceneName)
        {
            MySession.Static.GetComponent<MySessionComponentCutscenes>().PlayCutscene(cutsceneName);
        }


        [VisualScriptingMiscData("Cutscenes")]
        [VisualScriptingMember(true)]
        public static void NextCutsceneNode()
        {
            MyMultiplayer.RaiseStaticEvent(x => NextCutsceneNodeSync);
        }
        [Event, Reliable, Server, Broadcast]
        private static void NextCutsceneNodeSync()
        {
            MySession.Static.GetComponent<MySessionComponentCutscenes>().CutsceneNext(true);
        }

        [VisualScriptingMiscData("Cutscenes")]
        [VisualScriptingMember(true)]
        public static void EndCutscene()
        {
            MyMultiplayer.RaiseStaticEvent(x => EndCutsceneSync);
        }
        [Event, Reliable, Server, Broadcast]
        private static void EndCutsceneSync()
        {
            MySession.Static.GetComponent<MySessionComponentCutscenes>().CutsceneEnd();
        }

        #endregion

        #region Effects

        [VisualScriptingMiscData("Effects")]
        [VisualScriptingMember(true)]
        public static void CreateExplosion(Vector3D position, float radius, int damage = 5000)
        {
            var explosionType = MyExplosionTypeEnum.WARHEAD_EXPLOSION_50;
            if (radius < 2)
                explosionType = MyExplosionTypeEnum.WARHEAD_EXPLOSION_02;
            else if (radius < 15)
                explosionType = MyExplosionTypeEnum.WARHEAD_EXPLOSION_15;
            else if (radius < 30)
                explosionType = MyExplosionTypeEnum.WARHEAD_EXPLOSION_30;

            //  Create explosion
            MyExplosionInfo info = new MyExplosionInfo
            {
                PlayerDamage = 0,
                Damage = damage,
                ExplosionType = explosionType,
                ExplosionSphere = new BoundingSphereD(position, radius),
                LifespanMiliseconds = MyExplosionsConstants.EXPLOSION_LIFESPAN,
                CascadeLevel = 0,
                ParticleScale = 1,
                Direction = Vector3.Down,
                VoxelExplosionCenter = position,
                ExplosionFlags = MyExplosionFlags.AFFECT_VOXELS |
                                    MyExplosionFlags.APPLY_FORCE_AND_DAMAGE |
                                    MyExplosionFlags.CREATE_DEBRIS |
                                    MyExplosionFlags.CREATE_DECALS |
                                    MyExplosionFlags.CREATE_PARTICLE_EFFECT |
                                    MyExplosionFlags.CREATE_SHRAPNELS |
                                    MyExplosionFlags.APPLY_DEFORMATION,
                VoxelCutoutScale = 1.0f,
                PlaySound = true,
                ApplyForceAndDamage = true,
                ObjectsRemoveDelayInMiliseconds = 40
            };
            MyExplosions.AddExplosion(ref info);
        }

        [VisualScriptingMiscData("Effects")]
        [VisualScriptingMember(true)]
        public static void CreateParticleEffectAtPosition(string effectName, Vector3D position)
        {
            MyParticleEffect effect;
            if (MyParticlesManager.TryCreateParticleEffect(effectName, out effect))
            {
                effect.Loop = false;
                effect.WorldMatrix = MatrixD.CreateWorld((Vector3D)position);
            }
        }

        [VisualScriptingMiscData("Effects")]
        [VisualScriptingMember(true)]
        public static void CreateParticleEffectAtEntity(string effectName, string entityName)
        {
            MyParticleEffect effect;
            MyEntity entity = GetEntityByName(entityName);
            if (entity != null)
            {
                if (MyParticlesManager.TryCreateParticleEffect(effectName, out effect))
                {
                    effect.Loop = false;
                    effect.WorldMatrix = entity.WorldMatrix;
                }
            }
        }

        [VisualScriptingMiscData("Effects")]
        [VisualScriptingMember(true)]
        public static void ScreenColorFadingStart(float time = 1f, bool toOpaque = true)
        {
            MyHud.ScreenEffects.FadeScreen(toOpaque ? 0f : 1f, time);
        }

        [VisualScriptingMiscData("Effects")]
        [VisualScriptingMember(true)]
        public static void ScreenColorFadingSetColor(Color color)
        {
            MyHud.ScreenEffects.BlackScreenColor = new Color(color, 0f);
        }

        [VisualScriptingMiscData("Effects")]
        [VisualScriptingMember(true)]
        public static void ScreenColorFadingStartSwitch(float time = 1f)
        {
            MyHud.ScreenEffects.SwitchFadeScreen(time);
        }

        [VisualScriptingMiscData("Effects")]
        [VisualScriptingMember(true)]
        public static void ScreenColorFadingMinimalizeHUD(bool minimalize)
        {
            MyHud.ScreenEffects.BlackScreenMinimalizeHUD = minimalize;
        }

        [VisualScriptingMiscData("Effects")]
        [VisualScriptingMember(true)]
        public static void ShowHud(bool flag = true)
        {
            MyHud.MinimalHud = !flag;
        }

        #endregion

        #region Entity

        [VisualScriptingMiscData("Entity")]
        [VisualScriptingMember]
        public static MyEntity GetEntityByName(string name)
        {
            MyEntity entity;
            if (!MyEntities.TryGetEntityByName(name, out entity))
                return null;

            return entity;
        }

        [VisualScriptingMiscData("Entity")]
        [VisualScriptingMember]
        public static MyEntity GetEntityById(long id)
        {
            MyEntity entity;
            if (!MyEntities.TryGetEntityById(id, out entity))
                return null;

            return entity;
        }

        [VisualScriptingMiscData("Entity")]
        [VisualScriptingMember]
        public static long GetEntityIdFromName(string name)
        {
            MyEntity entity;
            if (!MyEntities.TryGetEntityByName(name, out entity))
                return 0;

            return entity.EntityId;
        }

        [VisualScriptingMiscData("Entity")]
        [VisualScriptingMember]
        public static long GetEntityIdFromEntity(MyEntity entity)
        {
            return entity != null ? entity.EntityId : 0;
        }

        [VisualScriptingMiscData("Entity")]
        [VisualScriptingMember]
        public static Vector3D GetEntityPosition(string entityName)
        {
            MyEntity entity = GetEntityByName(entityName);
            if (entity != null)
                return entity.PositionComp.GetPosition();

            return Vector3D.Zero;
        }

        [VisualScriptingMiscData("Entity")]
        [VisualScriptingMember]
        public static void GetEntityVectors(string entityName, out Vector3D position, out Vector3D forward, out Vector3D up)
        {
            position = Vector3D.Zero;
            forward = Vector3D.Forward;
            up = Vector3D.Up;
            MyEntity entity = GetEntityByName(entityName);
            if (entity == null)
                return;

            position = entity.PositionComp.WorldMatrix.Translation;
            forward = entity.PositionComp.WorldMatrix.Forward;
            up = entity.PositionComp.WorldMatrix.Up;
        }

        [VisualScriptingMiscData("Entity")]
        [VisualScriptingMember(true)]
        public static void SetEntityPosition(string entityName, Vector3D position)
        {
            MyEntity entity = GetEntityByName(entityName);
            if (entity != null)
                entity.PositionComp.SetPosition(position);
        }

        [VisualScriptingMiscData("Entity")]
        [VisualScriptingMember]
        public static Vector3D GetEntityDirection(string entityName, Base6Directions.Direction direction = Base6Directions.Direction.Forward)
        {
            MyEntity entity = GetEntityByName(entityName);
            if (entity == null)
            {
                Debug.Fail("Entity of name: " + entityName + " was not found.");
                return Vector3D.Forward;
            }
            switch (direction)
            {
                default:
                case Base6Directions.Direction.Forward:
                    return entity.WorldMatrix.Forward;
                case Base6Directions.Direction.Backward:
                    return entity.WorldMatrix.Backward;
                case Base6Directions.Direction.Up:
                    return entity.WorldMatrix.Up;
                case Base6Directions.Direction.Down:
                    return entity.WorldMatrix.Down;
                case Base6Directions.Direction.Left:
                    return entity.WorldMatrix.Left;
                case Base6Directions.Direction.Right:
                    return entity.WorldMatrix.Right;
            }
        }

        [VisualScriptingMiscData("Entity")]
        [VisualScriptingMember]
        public static bool IsPlanetNearby(Vector3D position)
        {
            Vector3 gravity = MyGravityProviderSystem.CalculateNaturalGravityInPoint(position);
            if (gravity.LengthSquared() > 0f)
                return (MyGamePruningStructure.GetClosestPlanet(position) != null);

            return false;
        }

        [VisualScriptingMiscData("Entity")]
        [VisualScriptingMember]
        public static string GetNearestPlanet(Vector3D position)
        {
            Vector3 gravity = MyGravityProviderSystem.CalculateNaturalGravityInPoint(position);
            if (gravity.LengthSquared() > 0f)
            {
                MyPlanet planet = MyGamePruningStructure.GetClosestPlanet(position);
                if (planet != null && planet.Generator != null)
                    return planet.Generator.FolderName;
            }

            return MyTexts.GetString(MyCommonTexts.Void);
        }

        [VisualScriptingMiscData("Entity")]
        [VisualScriptingMember(true)]
        public static void AddToInventory(string entityname, MyDefinitionId itemId, int amount = 1)
        {
            var entity = GetEntityByName(entityname);
            if (entity == null)
            {
                Debug.Fail("Entity of name: " + entityname + " was not found.");
                return;
            }

            var inventory = entity.GetInventoryBase();
            if (inventory == null)
            {
                Debug.Fail("Entity of name: " + entityname + " has no inventory.");
                return;
            }

            var ob = (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(itemId);
            var amountInFixedPoint = new MyFixedPoint();
            amountInFixedPoint = amount;
            if (!inventory.AddItems(amountInFixedPoint, ob))
                Debug.Fail("Item: " + itemId.ToString() + " Amount: " + amount + " Adding to inventory failed. (Probably not enough space.)");
        }

        [VisualScriptingMiscData("Entity")]
        [VisualScriptingMember]
        public static int GetEntityInventoryItemAmount(string entityName, MyDefinitionId itemId)
        {
            var entity = GetEntityByName(entityName);
            if(entity == null || !entity.HasInventory)
                return 0;

            var amount = 0;
            for (int index = 0; index < entity.InventoryCount; index++)
            {
                var inventory = entity.GetInventory(index);
                if(inventory != null)
                    amount += inventory.GetItemAmount(itemId).ToIntSafe();
            }

            return amount;
        }

        [VisualScriptingMiscData("Entity")]
        [VisualScriptingMember(true)]
        public static bool ChangeOwner(string entityName, long playerId = 0, bool factionShare = false, bool allShare = false)
        {
            var sharingOption = MyOwnershipShareModeEnum.None;
            if(factionShare)
                sharingOption = MyOwnershipShareModeEnum.Faction;
            if(allShare)
                sharingOption = MyOwnershipShareModeEnum.All;

            MyEntity entity;
            if (MyEntities.TryGetEntityByName(entityName, out entity))
            {
                var block = entity as MyCubeBlock;
                if (block != null)
                {
                    // Has to use the intermediate "No Ownership" value
                    block.ChangeBlockOwnerRequest(0, sharingOption);
                    if (playerId > 0)
                        block.ChangeBlockOwnerRequest(playerId, sharingOption);
                    return true;
                }

                var grid = entity as MyCubeGrid;
                if (grid != null)
                {
                    foreach (var fatBlock in grid.GetFatBlocks())
                    {
                        if(!(fatBlock is MyLightingBlock) && (fatBlock is MyFunctionalBlock || fatBlock is MyShipController))
                        {
                            // Has to use the intermediate "No Ownership" value
                            fatBlock.ChangeBlockOwnerRequest(0, sharingOption);
                            if (playerId > 0)
                                fatBlock.ChangeBlockOwnerRequest(playerId, sharingOption);
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        [VisualScriptingMiscData("Entity")]
        [VisualScriptingMember(true)]
        public static void RenameEntity(string oldName, string newName = null)
        {
            if (oldName == newName)
            {
                Debug.Fail("Invalid names provided.");
                return;
            }

            var entity = GetEntityByName(oldName);
            if (entity != null)
            {
                entity.Name = newName;
                MyEntities.SetEntityName(entity);
            }
            else
            {
                Debug.Fail(@"Entity of name " + oldName + " does not exist.");
            }
        }

        [VisualScriptingMiscData("Entity")]
        [VisualScriptingMember(true)]
        public static void SetName(long entityId, string name)
        {
            MyEntity entity;
            if (!MyEntities.TryGetEntityById(entityId, out entity))
            {
                Debug.Fail("Entity of give id not found.");
                return;
            }

            var existingEntity = GetEntityByName(name);
            if (existingEntity != null)
            {
                Debug.Fail("Entity of name " + name + " already exists within this world.");
                return;
            }

            entity.Name = name;
            MyEntities.SetEntityName(entity);
        }

        [VisualScriptingMiscData("Entity")]
        [VisualScriptingMember]
        public static string GetEntityName(long entityId)
        {
            MyEntity entity;
            if (!MyEntities.TryGetEntityById(entityId, out entity))
            {
                Debug.Fail("Entity of give id not found.");
                return string.Empty;
            }

            return entity.Name;
        }

        [VisualScriptingMiscData("Entity")]
        [VisualScriptingMember]
        public static Vector3D GetEntitySpeed(string entityName)
        {
            var entity = GetEntityByName(entityName);
            if (entity != null)
            {
                if (entity.Physics != null)
                {
                    return entity.Physics.LinearVelocity;
                }
                else
                {
                    Debug.Fail("Entity of name " + entityName + " does not have physics.");
                }
            }
            else
            {
                Debug.Fail("Entity of name " + entityName + " does not exist.");
            }
            return Vector3D.Zero;
        }

        [VisualScriptingMiscData("Entity")]
        [VisualScriptingMember]
        public static MyDefinitionId GetDefinitionId(string typeId, string subtypeId)
        {
            MyObjectBuilderType type;

            if (!MyObjectBuilderType.TryParse(typeId, out type))
            {
                if (MyObjectBuilderType.TryParse("MyObjectBuilder_"+typeId, out type))
                    return (new MyDefinitionId(type, subtypeId));
                Debug.Fail("Provided typeId is invalid: " + typeId);
            }
            
            return (new MyDefinitionId(type, subtypeId));
        }

        [VisualScriptingMiscData("Entity")]
        [VisualScriptingMember(true)]
        public static void RemoveEntity(string entityName)
        {
            var entity = GetEntityByName(entityName);
            if (entity != null)
            {
                MyEntities.RemoveName(entity);

                if (entity is MyCubeGrid || entity is MyFloatingObject)
                {
                    entity.Close();
                }

                if (entity is MyCubeBlock)
                {
                    var block = (MyCubeBlock)entity;
                    block.CubeGrid.RemoveBlock(block.SlimBlock, true);
                }
            }
        }

        [VisualScriptingMiscData("Entity")]
        [VisualScriptingMember(false)]
        public static bool EntityExists(string entityName)
        {
            var entity = GetEntityByName(entityName);
            if (entity is MyCubeGrid)
                return ((MyCubeGrid)entity).InScene && !((MyCubeGrid)entity).MarkedForClose;
            return entity != null;
        }

        private static MyEntityThrustComponent GetThrustComponentByEntityName(string entityName)
        {
            var entity = GetEntityByName(entityName);
            if (entity == null)
            {
                Debug.Fail("Entity of name: " + entityName + " was not found.");
                return null;
            }
            MyComponentBase comp = null;
            entity.Components.TryGet(typeof(MyEntityThrustComponent), out comp);
            var thrustComp = comp as MyEntityThrustComponent;
            Debug.Assert(thrustComp != null, "ThrustComponent was not found. For:" + entityName);
            return thrustComp;
        }

        [VisualScriptingMiscData("Entity")]
        [VisualScriptingMember(false)]
        public static bool GetDampenersEnabled(string entityName)
        {
            var comp = GetThrustComponentByEntityName(entityName);
            if (comp == null)
                return false;

            return comp.DampenersEnabled;
        }

        [VisualScriptingMiscData("Entity")]
        [VisualScriptingMember(true)]
        public static void SetDampenersEnabled(string entityName, bool state)
        {
            var comp = GetThrustComponentByEntityName(entityName);
            if (comp == null)
                return;

            comp.DampenersEnabled = state;
        }

        #endregion

        #region Environment

        //              Sun nodes
        [VisualScriptingMiscData("Environment")]
        [VisualScriptingMember(true)]
        public static void SunRotationSetTime(float time)
        {
            MyTimeOfDayHelper.UpdateTimeOfDay(time);
        }

        [VisualScriptingMiscData("Environment")]
        [VisualScriptingMember(true)]
        public static void SunRotationEnabled(bool enabled)
        {
            MySession.Static.GetComponent<MySectorWeatherComponent>().Enabled = enabled;
        }

        [VisualScriptingMiscData("Environment")]
        [VisualScriptingMember(true)]
        public static void SunRotationSetDayLength(float length)
        {
            MySession.Static.GetComponent<MySectorWeatherComponent>().RotationInterval = length;
        }

        [VisualScriptingMiscData("Environment")]
        [VisualScriptingMember]
        public static float SunRotationGetDayLength()
        {
            return MySession.Static.GetComponent<MySectorWeatherComponent>().RotationInterval;
        }

        [VisualScriptingMiscData("Environment")]
        [VisualScriptingMember]
        public static float SunRotationGetCurrentTime()
        {
            return MyTimeOfDayHelper.TimeOfDay;
        }

        //                fog nodes
        [VisualScriptingMiscData("Environment")]
        [VisualScriptingMember(true)]
        public static void FogSetAll(float density, float multiplier, Vector3 color)
        {
            MySector.FogProperties.FogMultiplier = multiplier;
            MySector.FogProperties.FogDensity = density;
            MySector.FogProperties.FogColor = color;
        }
        [VisualScriptingMiscData("Environment")]
        [VisualScriptingMember(true)]
        public static void FogSetDensity(float density)
        {
            MySector.FogProperties.FogDensity = density;
        }
        [VisualScriptingMiscData("Environment")]
        [VisualScriptingMember(true)]
        public static void FogSetMultiplier(float multiplier)
        {
            MySector.FogProperties.FogMultiplier = multiplier;
        }
        [VisualScriptingMiscData("Environment")]
        [VisualScriptingMember(true)]
        public static void FogSetColor(Vector3 color)
        {
            MySector.FogProperties.FogColor = color;
        }

        #endregion

        #region Factions

        [VisualScriptingMiscData("Factions")]
        [VisualScriptingMember]
        public static long GetLocalPlayerId()
        {
            return MySession.Static.LocalPlayerId;
        }

        [VisualScriptingMiscData("Factions")]
        [VisualScriptingMember]
        public static long GetPirateId()
        {
            return MyPirateAntennas.GetPiratesId();
        }

        [VisualScriptingMiscData("Factions")]
        [VisualScriptingMember]
        public static string GetPlayersFactionTag(long playerId)
        {
            var faction = MySession.Static.Factions.TryGetPlayerFaction(playerId) as MyFaction;

            Debug.Assert(faction != null, "Faction of player with id " + playerId.ToString() + " was not found.");
            if (faction == null)
                return "";

            return faction.Tag;
        }

        [VisualScriptingMiscData("Factions")]
        [VisualScriptingMember]
        public static string GetPlayersFactionName(long playerId)
        {
            var faction = MySession.Static.Factions.TryGetPlayerFaction(playerId) as MyFaction;

            Debug.Assert(faction != null, "Faction of player with id " + playerId.ToString() + " was not found.");
            if (faction == null)
                return "";

            return faction.Name;
        }

        [VisualScriptingMiscData("Factions")]
        [VisualScriptingMember(true)]
        public static bool SetPlayersFaction(long playerId, string factionTag)
        {
            var faction = MySession.Static.Factions.TryGetFactionByTag(factionTag);
            if(faction == null) return false;

            MySession.Static.Factions.AddPlayerToFaction(playerId, faction.FactionId);
            return true;
        }

        #endregion

        #region Gameplay

        [VisualScriptingMiscData("Gameplay")]
        [VisualScriptingMember(true)]
        public static void EnableTerminal(bool flag)
        {
            MyPerGameSettings.GUI.EnableTerminalScreen = flag;
        }

        [VisualScriptingMiscData("Gameplay")]
        [VisualScriptingMember(true)]
        public static bool SaveSession()
        {
            if (!MyAsyncSaving.InProgress)
            {
                MyAsyncSaving.Start();
                return true;
            }

            return false;
        }

        [VisualScriptingMiscData("Gameplay")]
        [VisualScriptingMember(true)]
        public static bool SaveSessionAs(string saveName)
        {
            if (!MyAsyncSaving.InProgress)
            {
                MyAsyncSaving.Start(null, saveName);
                return true;
            }

            return false;
        }

        [VisualScriptingMiscData("Gameplay")]
        [VisualScriptingMember(true)]
        public static void SessionReloadDialog(string caption, string message, string savePath = null)
        {
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                messageCaption: new StringBuilder(caption),
                messageText: new StringBuilder(message),
                buttonType: MyMessageBoxButtonsType.YES_NO,
                callback: delegate(MyGuiScreenMessageBox.ResultEnum result)
                {
                    if (result == MyGuiScreenMessageBox.ResultEnum.YES)
                        MySessionLoader.LoadSingleplayerSession(savePath ?? MySession.Static.CurrentPath);
                    else
                        MySessionLoader.UnloadAndExitToMenu();
                }));
        }

        [VisualScriptingMiscData("Gameplay")]
        [VisualScriptingMember(true)]
        public static void SessionClose(int fadeTimeMs = 10000)
        {
            if (fadeTimeMs < 0) fadeTimeMs = 10000;

            var screen = new MyGuiScreenFade(Color.Black, (uint)fadeTimeMs, 0);
            screen.Closed += source =>
            {
                if (MyCampaignManager.Static.IsCampaignRunning)
                {
                    var campaignComp = MySession.Static.GetComponent<MyCampaignSessionComponent>();
                    campaignComp.LoadNextCampaignMission();
                }
                else
                {
                    MySessionLoader.UnloadAndExitToMenu();
                }  
            };
            screen.Shown += fade => MyScreenManager.CloseScreen(typeof(MyGuiScreenFade));

            MyHud.MinimalHud = true;
            MyScreenManager.AddScreen(screen);
        }

        [VisualScriptingMiscData("Gameplay")]
        [VisualScriptingMember(true)]
        public static void SessionReloadLastCheckpoint(int fadeTimeMs = 10000, string message = null, float textScale = 1f, string font = "Blue")
        {
            if (fadeTimeMs < 0) fadeTimeMs = 10000;

            var screen = new MyGuiScreenFade(Color.Black, (uint)fadeTimeMs, 0);
            screen.Closed += source =>
            {
                MySessionLoader.LoadSingleplayerSession(MySession.Static.CurrentPath);
                MyHud.MinimalHud = false;
            };
            screen.Shown += fade => MyScreenManager.CloseScreen(typeof(MyGuiScreenFade));

            if(!string.IsNullOrEmpty(message))
            {
                screen.Controls.Add(
                    new MyGuiControlMultilineText(
                        new Vector2(0.5f),
                        new Vector2(0.6f, 0.3f),
                        contents: new StringBuilder(message),
                        textScale: textScale,
                        font: "Red"
                        )
                    );
            }

            MyHud.MinimalHud = true;
            MyScreenManager.AddScreen(screen);
        }

        private static bool m_exitGameDialogOpened = false;

        [VisualScriptingMiscData("Gameplay")]
        [VisualScriptingMember(true)]
        public static void SessionExitGameDialog(string caption, string message)
        {
            if(m_exitGameDialogOpened) return;

            m_exitGameDialogOpened = true;
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                messageCaption: new StringBuilder(caption),
                messageText: new StringBuilder(message),
                buttonType: MyMessageBoxButtonsType.OK,
                styleEnum: MyMessageBoxStyleEnum.Info,
                okButtonText: MyCampaignManager.Static.IsCampaignRunning ? MyCommonTexts.ScreenMenuButtonContinue : MyCommonTexts.ScreenMenuButtonExitToMainMenu,
                callback: delegate(MyGuiScreenMessageBox.ResultEnum result)
                {
                    if (result == MyGuiScreenMessageBox.ResultEnum.YES)
                    {
                        if (MyCampaignManager.Static.IsCampaignRunning)
                        {
                            var campaignComp = MySession.Static.GetComponent<MyCampaignSessionComponent>();
                            campaignComp.LoadNextCampaignMission();
                        }
                        else
                        {
                            MySessionLoader.UnloadAndExitToMenu();
                        }
                    }

                    m_exitGameDialogOpened = false;
                }));
        }

        [VisualScriptingMiscData("Gameplay")]
        [VisualScriptingMember]
        public static string GetCurrentSessionPath()
        {
            return MySession.Static.CurrentPath;
        }

        [VisualScriptingMiscData("Gameplay")]
        [VisualScriptingMember]
        public static bool IsGameLoaded()
        {
            return GameIsReady;
        }

        [VisualScriptingMiscData("Gameplay")]
        [VisualScriptingMember(true)]
        public static void SetCampaignLevelOutcome(string outcome)
        {
            var campaignComp = MySession.Static.GetComponent<MyCampaignSessionComponent>();
            if (campaignComp != null)
            {
                campaignComp.CampaignLevelOutcome = outcome;
            }
        }

        #endregion

        #region GPSAndHighligths
        
        // Contains per player data in form of playerId => {enitityId, exclusiveLock} ...
        private static readonly Dictionary<long, List<MyTuple<long, int>>> m_playerIdsToHighlightData = new Dictionary<long, List<MyTuple<long, int>>>();
        private static readonly Color DEFAULT_HIGHLIGHT_COLOR = new Color(0, 96, 209, 25);

        [VisualScriptingMiscData("GPSAndHighligths")]
        [VisualScriptingMember(true)]
        public static void SetHighlight(string entityName, bool enabled = true, int thickness = 10, int pulseTimeInFrames = 120, Color color = default(Color), long playerId = -1, string subPartNames = null)
        {
            MyEntity entity;
            if (MyEntities.TryGetEntityByName(entityName, out entity))
            {
                if (color == default(Color))
                {
                    color = DEFAULT_HIGHLIGHT_COLOR;
                }

                if (playerId == -1)
                    playerId = GetLocalPlayerId();

                // highlight for single entity
                var highlightData = new MyHighlightSystem.MyHighlightData
                {
                    EntityId = entity.EntityId,
                    OutlineColor = color,
                    PulseTimeInFrames = (ulong)pulseTimeInFrames,
                    Thickness = enabled ? thickness : -1,
                    PlayerId = playerId,
                    IgnoreUseObjectData = subPartNames == null,
                    SubPartNames = string.IsNullOrEmpty(subPartNames) ? "" : subPartNames
                };

                SetHighlight(highlightData, playerId);
            }
        }

        [VisualScriptingMiscData("GPSAndHighligths")]
        [VisualScriptingMember(true)]
        public static void SetHighlightForAll(string entityName, bool enabled = true, int thickness = 10, int pulseTimeInFrames = 120, Color color = default(Color), string subPartNames = null)
        {
            var players = MySession.Static.Players.GetOnlinePlayers();
            if (players == null || players.Count == 0)
                return;
            foreach (var player in players)
            {
                SetHighlight(entityName, enabled, thickness, pulseTimeInFrames, color, player.Identity.IdentityId, subPartNames);
            }
        }


        [VisualScriptingMiscData("GPSAndHighligths")]
        [VisualScriptingMember(true)]
        public static void SetGPSHighlight(string entityName, string GPSName, string GPSDescription, Color GPSColor, bool enabled = true, int thickness = 10, int pulseTimeInFrames = 120, Color color = default(Color), long playerId = -1, string subPartNames = null)
        {
            MyEntity entity;
            if (MyEntities.TryGetEntityByName(entityName, out entity))
            {
                if (playerId == -1)
                    playerId = GetLocalPlayerId();
                MyTuple<string, string> gpsIdentifier = new MyTuple<string, string>(entityName, GPSName);

                if (enabled)
                {
                    MyGps newGPS = new MyGps { ShowOnHud = true, Name = GPSName, Description = GPSDescription, AlwaysVisible = true };
                    if (GPSColor != Color.Transparent)
                        newGPS.GPSColor = GPSColor;
                    MySession.Static.Gpss.SendAddGps(playerId, ref newGPS, entity.EntityId);
                }
                else
                {
                    var gps = MySession.Static.Gpss.GetGpsByName(playerId, GPSName);
                    if (gps != null)
                        MySession.Static.Gpss.SendDelete(playerId, gps.Hash);
                }
                SetHighlight(entityName, enabled: enabled, thickness: thickness, pulseTimeInFrames: pulseTimeInFrames, color: color, playerId: playerId, subPartNames: subPartNames);
            }
        }


        [VisualScriptingMiscData("GPSAndHighligths")]
        [VisualScriptingMember(true)]
        public static void SetGPSHighlightForAll(string entityName, string GPSName, string GPSDescription, Color GPSColor, bool enabled = true, int thickness = 10, int pulseTimeInFrames = 120, Color color = default(Color), string subPartNames = null)
        {
            var players = MySession.Static.Players.GetOnlinePlayers();
            if (players == null || players.Count == 0)
                return;
            foreach (var player in players)
            {
                SetGPSHighlight(entityName, GPSName, GPSDescription, GPSColor, enabled, thickness, pulseTimeInFrames, color, player.Identity.IdentityId, subPartNames);
            }
        }

        // Common logic used for highlighting objects from logic provider
        private static void SetHighlight(MyHighlightSystem.MyHighlightData highlightData, long playerId)
        {
            var highlightComp = MySession.Static.GetComponent<MyHighlightSystem>();
            // Determine if we are getting rid of the highlight or updating/adding it
            var enabled = highlightData.Thickness > -1;
            // Try to find exclusive key in the local dictionary
            var exclusiveKey = -1;
            if (m_playerIdsToHighlightData.ContainsKey(playerId))
            {
                exclusiveKey = m_playerIdsToHighlightData[playerId].Find(tuple => tuple.Item1 == highlightData.EntityId).Item2;
                // Tuples default int value is 0 and keys are in range of 10+
                if (exclusiveKey == 0)
                    exclusiveKey = -1;
            }

            // Add requst data with empty exclusive key for non existent records
            if (exclusiveKey == -1)
            {
                // We have nothing to disable highlight for
                if (!enabled)
                    return;
                // Listen to accepted and rejected events - They are being unregistered afterwards
                highlightComp.ExclusiveHighlightAccepted += OnExclusiveHighlightAccepted;
                highlightComp.ExclusiveHighlightRejected += OnExclusiveHighlightRejected;

                if (!m_playerIdsToHighlightData.ContainsKey(playerId))
                    m_playerIdsToHighlightData.Add(playerId, new List<MyTuple<long, int>>());

                m_playerIdsToHighlightData[playerId].Add(new MyTuple<long, int>(highlightData.EntityId, -1));
            }
            else if (!enabled)
            {
                // Removing the highlight
                // Remove the data from per player repository
                m_playerIdsToHighlightData[playerId].RemoveAll(tuple => tuple.Item2 == exclusiveKey);
            }

            // Do the request
            highlightComp.RequestHighlightChangeExclusive(highlightData, exclusiveKey);
        }

        private static void OnExclusiveHighlightRejected(MyHighlightSystem.MyHighlightData data, int exclusiveKey)
        {
            m_playerIdsToHighlightData[data.PlayerId].RemoveAll(tuple => tuple.Item1 == data.EntityId);
            // unsubscribe the second event
            MySession.Static.GetComponent<MyHighlightSystem>().ExclusiveHighlightAccepted -= OnExclusiveHighlightAccepted;
        }

        private static void OnExclusiveHighlightAccepted(MyHighlightSystem.MyHighlightData data, int exclusiveKey)
        {
            // Already disposed data
            if (data.Thickness == -1f)
                return;

            var playersList = m_playerIdsToHighlightData[data.PlayerId];
            var indexOf = playersList.FindIndex(tuple => tuple.Item1 == data.EntityId);
            var originalData = playersList[indexOf];
            m_playerIdsToHighlightData[data.PlayerId][indexOf] = new MyTuple<long, int>(originalData.Item1, exclusiveKey);
            // unsubscribe the second event
            MySession.Static.GetComponent<MyHighlightSystem>().ExclusiveHighlightRejected -= OnExclusiveHighlightRejected;
        }

        [VisualScriptingMiscData("GPSAndHighligths")]
        [VisualScriptingMember(true)]
        public static void SetGPSColor(string name, Color newColor, long playerId = -1)
        {
            IMyGps gps = MySession.Static.Gpss.GetGpsByName(playerId > 0 ? playerId : MySession.Static.LocalPlayerId, name);
            if(gps != null)
                MySession.Static.Gpss.ChangeColor(playerId > 0 ? playerId : MySession.Static.LocalPlayerId, gps.Hash, newColor);
        }

        [VisualScriptingMiscData("GPSAndHighligths")]
        [VisualScriptingMember(true)]
        public static void AddGPS(string name, string description, Vector3D position, Color GPSColor, int disappearsInS = 0, long playerId = -1)
        {
            if (playerId <= 0)
                playerId = MySession.Static.LocalPlayerId;
            var newGPS = new MyGps { ShowOnHud = true, Coords = position, Name = name, Description = description, AlwaysVisible = true };
            if (disappearsInS > 0)
            {
                var timeSpan = TimeSpan.FromSeconds(MySession.Static.ElapsedPlayTime.TotalSeconds + disappearsInS);
                newGPS.DiscardAt = timeSpan;
            }
            if (GPSColor != Color.Transparent)
                newGPS.GPSColor = GPSColor;
            MySession.Static.Gpss.SendAddGps(playerId, ref newGPS);
        }

        [VisualScriptingMiscData("GPSAndHighligths")]
        [VisualScriptingMember(true)]
        public static void AddGPSForAll(string name, string description, Vector3D position, Color GPSColor, int disappearsInS = 0)
        {
            var players = MySession.Static.Players.GetOnlinePlayers();
            if (players == null || players.Count == 0)
                return;
            foreach (var player in players)
            {
                AddGPS(name, description, position, GPSColor, disappearsInS, player.Identity.IdentityId);
            }
        }

        [VisualScriptingMiscData("GPSAndHighligths")]
        [VisualScriptingMember(true)]
        public static void RemoveGPS(string name, long playerId = -1)
        {
            if (playerId <= 0)
                playerId = MySession.Static.LocalPlayerId;
            var gps = MySession.Static.Gpss.GetGpsByName(playerId, name);
            if (gps != null)
                MySession.Static.Gpss.SendDelete(playerId, gps.Hash);
        }

        [VisualScriptingMiscData("GPSAndHighligths")]
        [VisualScriptingMember(true)]
        public static void RemoveGPSForAll(string name)
        {
            var players = MySession.Static.Players.GetOnlinePlayers();
            if (players == null || players.Count == 0)
                return;
            foreach (var player in players)
            {
                RemoveGPS(name, player.Identity.IdentityId);
            }
        }

        [VisualScriptingMiscData("GPSAndHighligths")]
        [VisualScriptingMember(true)]
        public static void AddGPSToEntity(string entityName, string GPSName, string GPSDescription, Color GPSColor, long playerId = -1)
        {
            MyEntity entity;
            if (MyEntities.TryGetEntityByName(entityName, out entity))
            {
                if (playerId == -1)
                    playerId = GetLocalPlayerId();
                MyTuple<string, string> gpsIdentifier = new MyTuple<string, string>(entityName, GPSName);

                MyGps newGPS = new MyGps { ShowOnHud = true, Name = GPSName, Description = GPSDescription, AlwaysVisible = true };
                if (GPSColor != Color.Transparent)
                    newGPS.GPSColor = GPSColor;
                MySession.Static.Gpss.SendAddGps(playerId, ref newGPS, entity.EntityId);
            }
        }

        [VisualScriptingMiscData("GPSAndHighligths")]
        [VisualScriptingMember(true)]
        public static void AddGPSToEntityForAll(string entityName, string GPSName, string GPSDescription, Color GPSColor)
        {
            var players = MySession.Static.Players.GetOnlinePlayers();
            if (players == null || players.Count == 0)
                return;
            foreach (var player in players)
            {
                AddGPSToEntity(entityName, GPSName, GPSDescription, GPSColor, player.Identity.IdentityId);
            }
        }

        [VisualScriptingMiscData("GPSAndHighligths")]
        [VisualScriptingMember(true)]
        public static void RemoveGPSFromEntity(string entityName, string GPSName, string GPSDescription, long playerId = -1)
        {
            MyEntity entity;
            if (MyEntities.TryGetEntityByName(entityName, out entity))
            {
                if (playerId == -1)
                    playerId = GetLocalPlayerId();
                MyTuple<string, string> gpsIdentifier = new MyTuple<string, string>(entityName, GPSName);

                var gps = MySession.Static.Gpss.GetGpsByName(playerId, GPSName);
                if (gps != null)
                    MySession.Static.Gpss.SendDelete(playerId, gps.Hash);
            }
        }

        [VisualScriptingMiscData("GPSAndHighligths")]
        [VisualScriptingMember(true)]
        public static void RemoveGPSFromEntityForAll(string entityName, string GPSName, string GPSDescription)
        {
            var players = MySession.Static.Players.GetOnlinePlayers();
            if (players == null || players.Count == 0)
                return;
            foreach (var player in players)
            {
                RemoveGPSFromEntity(entityName, GPSName, GPSDescription, player.Identity.IdentityId);
            }
        }

        #endregion

        #region Grid

        [VisualScriptingMiscData("Grid")]
        [VisualScriptingMember]
        public static List<long> GetIdListOfSpecificGridBlocks(string gridName, MyDefinitionId blockId)
        {
            List<long> result = new List<long>();
            MyEntity entity = GetEntityByName(gridName);
            Debug.Assert(entity != null, "Entity was not found: " + gridName);
            if (entity != null)
            {
                MyCubeGrid grid = entity as MyCubeGrid;
                Debug.Assert(grid != null, "This entity is not a grid: " + gridName);
                if (grid != null)
                {
                    foreach (var block in grid.GetFatBlocks())
                    {
                        if (block != null && block.BlockDefinition != null && block.BlockDefinition.Id == blockId)
                            result.Add(block.EntityId);
                    }
                }
            }

            return result;
        }

        [VisualScriptingMiscData("Grid")]
        [VisualScriptingMember]
        public static int GetCountOfSpecificGridBlocks(string gridName, MyDefinitionId blockId)
        {
            int result = -2;
            MyEntity entity = GetEntityByName(gridName);
            Debug.Assert(entity != null, "Entity was not found: " + gridName);
            if (entity != null)
            {
                result = -1;
                MyCubeGrid grid = entity as MyCubeGrid;
                Debug.Assert(grid != null, "This entity is not a grid: " + gridName);
                if (grid != null)
                {
                    result = 0;
                    foreach (var block in grid.GetFatBlocks())
                    {
                        if (block != null && block.BlockDefinition != null && block.BlockDefinition.Id == blockId)
                            result++;
                    }
                }
            }
            return result;
        }

        [VisualScriptingMiscData("Grid")]
        [VisualScriptingMember]
        public static long GetIdOfFirstSpecificGridBlock(string gridName, MyDefinitionId blockId)
        {
            MyEntity entity = GetEntityByName(gridName);
            Debug.Assert(entity != null, "Entity was not found: " + gridName);
            if (entity != null)
            {
                MyCubeGrid grid = entity as MyCubeGrid;
                Debug.Assert(grid != null, "This entity is not a grid: " + gridName);
                if (grid != null)
                {
                    foreach (var block in grid.GetFatBlocks())
                    {
                        if (block != null && block.BlockDefinition != null && block.BlockDefinition.Id == blockId)
                            return block.EntityId;
                    }
                }
            }
            return 0;
        }

        [VisualScriptingMiscData("Grid")]
        [VisualScriptingMember(true)]
        public static void SetGridLandingGearsLock(string gridName, bool gearLock = true)
        {
            MyCubeGrid grid = GetEntityByName(gridName) as MyCubeGrid;
            Debug.Assert(grid != null, "Grid was not found: " + gridName);
            if (grid != null)
            {
                grid.GridSystems.LandingSystem.Switch(gearLock);
            }
        }

        [VisualScriptingMiscData("Grid")]
        [VisualScriptingMember]
        public static bool IsGridLockedWithLandingGear(string gridName)
        {
            MyCubeGrid grid = GetEntityByName(gridName) as MyCubeGrid;
            Debug.Assert(grid != null, "Grid was not found: " + gridName);
            if (grid != null)
            {
                return grid.GridSystems.LandingSystem.Locked == MyMultipleEnabledEnum.Mixed || grid.GridSystems.LandingSystem.Locked == MyMultipleEnabledEnum.AllEnabled;
            }
            return false;
        }

        [VisualScriptingMiscData("Grid")]
        [VisualScriptingMember(true)]
        public static void SetGridReactors(string gridName, bool turnOn = true)
        {
            MyCubeGrid grid = GetEntityByName(gridName) as MyCubeGrid;
            Debug.Assert(grid != null, "Grid was not found: " + gridName);
            if (grid != null)
            {
                if (turnOn)
                {
                    grid.SendPowerDistributorState(MyMultipleEnabledEnum.AllEnabled, -1);
                }
                else
                {
                    grid.SendPowerDistributorState(MyMultipleEnabledEnum.AllDisabled, -1);
                }
            }
        }

        [VisualScriptingMiscData("Grid")]
        [VisualScriptingMember(true)]
        public static void SetGridWeaponStatus(string gridName, bool enabled = true)
        {
            MyCubeGrid grid = GetEntityByName(gridName) as MyCubeGrid;
            Debug.Assert(grid != null, "Grid was not found: " + gridName);
            if (grid != null)
            {
                var blocks = grid.GetBlocks();
                foreach (var block in blocks)
                {
                    if (block.FatBlock is MyUserControllableGun)
                        ((MyUserControllableGun)block.FatBlock).Enabled = enabled;
                }
            }
        }

        [VisualScriptingMiscData("Grid")]
        [VisualScriptingMember]
        public static int GetNumberOfGridBlocks(string entityName, string blockTypeId, string blockSubtypeId)
        {
            var grid = GetEntityByName(entityName) as MyCubeGrid;
            if (grid != null)
            {
                var count = 0;
                bool hasTypeId = !string.IsNullOrEmpty(blockTypeId);
                bool hasSubtypeId = !string.IsNullOrEmpty(blockSubtypeId);
                foreach (var fatBlock in grid.GetFatBlocks())
                {
                    if (hasSubtypeId && hasTypeId)
                    {
                        if (fatBlock.BlockDefinition.Id.SubtypeName == blockSubtypeId &&
                            fatBlock.BlockDefinition.Id.TypeId.ToString() == blockTypeId)
                            count++;
                    }
                    else if (hasTypeId)
                    {
                        if (fatBlock.BlockDefinition.Id.TypeId.ToString() == blockTypeId)
                            count++;
                    }
                    else if (hasSubtypeId)
                    {
                        if (fatBlock.BlockDefinition.Id.SubtypeName == blockSubtypeId)
                            count++;
                    }
                }

                return count;
            }

            return 0;
        }

        private static readonly MyDefinitionId ElectricityId = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Electricity");

        [VisualScriptingMiscData("Grid")]
        [VisualScriptingMember]
        public static bool HasThrusterInAllDirections(string entityName)
        {
            var entity = GetEntityByName(entityName);
            Debug.Assert(entity != null, "Entity of name " + entityName + " was not found.");
            if (entity == null) return false;

            var grid = entity as MyCubeGrid;
            if (grid == null)
            {
                Debug.Fail("Entity is not CubeGrid: " + entityName);
                return false;
            }

            ResetThrustDirections();
            var thrusters = grid.GetFatBlocks<MyThrust>();
            foreach (var thruster in thrusters)
            {
                if (thruster.Enabled && Math.Abs(thruster.ThrustOverride) < 0.0001f)
                    m_thrustDirections[thruster.ThrustForwardVector] = true;
            }

            foreach (var value in m_thrustDirections.Values)
            {
                if (!value)
                    return false;
            }

            return true;
        }

        [VisualScriptingMiscData("Grid")]
        [VisualScriptingMember]
        public static bool HasPower(string gridName)
        {
            var entity = GetEntityByName(gridName);
            Debug.Assert(entity != null, "Entity of name " + gridName + " was not found.");
            if (entity == null) return false;

            var grid = entity as MyCubeGrid;
            if (grid != null)
            {
                var gridPowerState = grid.GridSystems.ResourceDistributor.ResourceStateByType(MyResourceDistributorComponent.ElectricityId);
                if (gridPowerState == MyResourceStateEnum.Ok || gridPowerState == MyResourceStateEnum.OverloadAdaptible)
                    return true;
            }

            return false;
        }

        [VisualScriptingMiscData("Grid")]
        [VisualScriptingMember]
        public static bool HasOperationalGyro(string gridName)
        {
            var entity = GetEntityByName(gridName);
            Debug.Assert(entity != null, "Entity of name " + gridName + " was not found.");
            if (entity == null) return false;

            var grid = entity as MyCubeGrid;
            if (grid == null)
                Debug.Fail("Entity is not CubeGrid: " + gridName);

            var gyros = grid.GetFatBlocks<MyGyro>();
            var hasEnabledGyro = false;
            foreach (var gyro in gyros)
            {
                if (gyro.Enabled && gyro.IsPowered && !gyro.GyroOverride)
                {
                    hasEnabledGyro = true;
                    break;
                }
            }

            return hasEnabledGyro;
        }

        [VisualScriptingMiscData("Grid")]
        [VisualScriptingMember]
        public static bool HasOperationalCockpit(string gridName)
        {
            var entity = GetEntityByName(gridName);
            Debug.Assert(entity != null, "Entity of name " + gridName + " was not found.");
            if (entity == null) return false;

            var grid = entity as MyCubeGrid;
            if (grid != null)
            {
                var cockpits = grid.GetFatBlocks<MyCockpit>();
                var hasEnabledCockpit = false;
                foreach (var cockpit in cockpits)
                {
                    if (cockpit.EnableShipControl)
                    {
                        hasEnabledCockpit = true;
                        break;
                    }
                }

                return hasEnabledCockpit;
            }
            else
            {
                Debug.Fail("Entity is not CubeGrid: " + gridName);
            }

            return false;
        }

        [VisualScriptingMiscData("Grid")]
        [VisualScriptingMember]
        public static bool IsFlyable(string entityName)
        {
            var grid = GetEntityByName(entityName) as MyCubeGrid;
            if (grid != null)
            {
                var gridPowerState = grid.GridSystems.ResourceDistributor.ResourceStateByType(MyResourceDistributorComponent.ElectricityId);
                if (gridPowerState == MyResourceStateEnum.OverloadBlackout || gridPowerState == MyResourceStateEnum.NoPower)
                    return false;

                var gyros = grid.GetFatBlocks<MyGyro>();
                var hasEnabledGyro = false;
                foreach (var gyro in gyros)
                {
                    if (gyro.Enabled && gyro.IsPowered && !gyro.GyroOverride)
                    {
                        hasEnabledGyro = true;
                        break;
                    }
                }

                if (!hasEnabledGyro)
                    return false;

                var cockpits = grid.GetFatBlocks<MyShipController>();
                var hasEnabledCockpit = false;
                foreach (var cockpit in cockpits)
                {
                    if (cockpit.EnableShipControl)
                    {
                        hasEnabledCockpit = true;
                        break;
                    }
                }

                if (!hasEnabledCockpit)
                    return false;

                ResetThrustDirections();
                var thrusters = grid.GetFatBlocks<MyThrust>();
                foreach (var thruster in thrusters)
                {
                    if (thruster.IsPowered && thruster.Enabled && Math.Abs(thruster.ThrustOverride) < 0.0001f)
                        m_thrustDirections[thruster.ThrustForwardVector] = true;
                }

                foreach (var value in m_thrustDirections.Values)
                {
                    if (!value)
                        return false;
                }

                return true;
            }

            return false;
        }

        private static void ResetThrustDirections()
        {
            if (m_thrustDirections.Count == 0)
            {
                m_thrustDirections.Add(Vector3I.Forward, false);
                m_thrustDirections.Add(Vector3I.Backward, false);
                m_thrustDirections.Add(Vector3I.Left, false);
                m_thrustDirections.Add(Vector3I.Right, false);
                m_thrustDirections.Add(Vector3I.Up, false);
                m_thrustDirections.Add(Vector3I.Down, false);
            }
            else
            {
                m_thrustDirections[Vector3I.Forward] = false;
                m_thrustDirections[Vector3I.Backward] = false;
                m_thrustDirections[Vector3I.Left] = false;
                m_thrustDirections[Vector3I.Right] = false;
                m_thrustDirections[Vector3I.Up] = false;
                m_thrustDirections[Vector3I.Down] = false;
            }
        }

        [VisualScriptingMiscData("Grid")]
        [VisualScriptingMember(true)]
        public static void CreateLocalBlueprint(string gridName, string blueprintName, string blueprintDisplayName = null)
        {
            var localBPPath = Path.Combine(MyFileSystem.UserDataPath, "Blueprints", "local");
            var localBPFullPath = Path.Combine(localBPPath, blueprintName, "bp.sbc");

            if (!MyFileSystem.DirectoryExists(localBPPath))
                return;

            if (blueprintDisplayName == null)
                blueprintDisplayName = blueprintName;

            var entity = GetEntityByName(gridName);
            if (entity == null)
            {
                Debug.Fail("Entity of name: " + gridName + " was not found.");
                return;
            }

            var grid = entity as MyCubeGrid;
            if (grid == null)
            {
                Debug.Fail("Entity of name: " + gridName + " is not CubeGrid.");
                return;
            }

            MyClipboardComponent.Static.Clipboard.CopyGrid(grid);

            var prefab = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ShipBlueprintDefinition>();
            prefab.Id = new MyDefinitionId(new MyObjectBuilderType(typeof(MyObjectBuilder_ShipBlueprintDefinition)), MyUtils.StripInvalidChars(blueprintName));
            prefab.CubeGrids = MyClipboardComponent.Static.Clipboard.CopiedGrids.ToArray();
            prefab.RespawnShip = false;
            prefab.DisplayName = blueprintDisplayName;
            //prefab.OwnerSteamId = MySession.Static.LocalHumanPlayer != null ? MySession.Static.LocalHumanPlayer.Id.SteamId : 0;
            prefab.CubeGrids[0].DisplayName = blueprintDisplayName;

            var definitions = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Definitions>();
            definitions.ShipBlueprints = new MyObjectBuilder_ShipBlueprintDefinition[1];
            definitions.ShipBlueprints[0] = prefab;

            MyObjectBuilderSerializer.SerializeXML(localBPFullPath, false, definitions);
            MyClipboardComponent.Static.Clipboard.Deactivate();
        }

        [VisualScriptingMiscData("Grid")]
        [VisualScriptingMember(true)]
        public static void SetHighlightForProjection(string projectorName, bool enabled = true, int thickness = 5, int pulseTimeInFrames = 120, Color color = default(Color), long playerId = -1)
        {
            var entity = GetEntityByName(projectorName);
            if (entity != null && entity is MyProjectorBase)
            {
                var projector = (MyProjectorBase)entity;
                if (color == default(Color))
                    color = Color.Blue;

                if (color == default(Color))
                    color = Color.Blue;

                if (playerId == -1)
                    playerId = MySession.Static.LocalPlayerId;

                // highlight for single entity
                var highlightData = new MyHighlightSystem.MyHighlightData
                {
                    OutlineColor = color,
                    PulseTimeInFrames = (ulong)pulseTimeInFrames,
                    Thickness = enabled ? thickness : -1,
                    PlayerId = playerId,
                    IgnoreUseObjectData = true
                };

                foreach (var cubeGrid in projector.Clipboard.PreviewGrids)
                {
                    foreach (var block in cubeGrid.GetFatBlocks())
                    {
                        highlightData.EntityId = block.EntityId;
                        SetHighlight(highlightData, highlightData.PlayerId);
                    }
                }
            }
        }

        [VisualScriptingMiscData("Grid")]
        [VisualScriptingMember]
        public static bool IsGridDestructible(string entityName)
        {
            var grid = GetEntityByName(entityName) as MyCubeGrid;
            Debug.Assert(grid != null, "Grid of name " + entityName + " was not found.");
            if (grid == null) return true;
            return grid.DestructibleBlocks;
        }

        [VisualScriptingMiscData("Grid")]
        [VisualScriptingMember(true)]
        public static void SetGridDestructible(string entityName, bool destructible = true)
        {
            var grid = GetEntityByName(entityName) as MyCubeGrid;
            Debug.Assert(grid != null, "Grid of name " + entityName + " was not found.");
            if (grid == null) return;
            grid.DestructibleBlocks = destructible;
        }

        [VisualScriptingMiscData("Grid")]
        [VisualScriptingMember]
        public static bool IsGridEditable(string entityName)
        {
            var grid = GetEntityByName(entityName) as MyCubeGrid;
            Debug.Assert(grid != null, "Grid of name " + entityName + " was not found.");
            if (grid == null) return true;
            return grid.Editable;
        }

        [VisualScriptingMiscData("Grid")]
        [VisualScriptingMember(true)]
        public static void SetGridEditable(string entityName, bool editable = true)
        {
            var grid = GetEntityByName(entityName) as MyCubeGrid;
            Debug.Assert(grid != null, "Grid of name " + entityName + " was not found.");
            if (grid == null) return;
            grid.Editable = editable;
        }

        [VisualScriptingMiscData("Grid")]
        [VisualScriptingMember(true)]
        public static void SetGridStatic(string gridName, bool isStatic = true)
        {
            var grid = GetEntityByName(gridName) as MyCubeGrid;
            Debug.Assert(grid != null, "Grid of name " + gridName + " was not found.");
            if (grid == null) return;
            if(isStatic)
                grid.RequestConversionToStation();
            else
                grid.RequestConversionToShip();
        }

        [VisualScriptingMiscData("Grid")]
        [VisualScriptingMember(true)]
        public static void SetGridGeneralDamageModifier(string gridName, float modifier = 1f)
        {
            MyEntity entity;

            if (MyEntities.TryGetEntityByName(gridName, out entity))
            {
                if (entity is MyCubeGrid)
                {
                    ((MyCubeGrid)entity).GridGeneralDamageModifier = modifier;
                }
            }
        }

        #endregion

        #region G-Screen

        [VisualScriptingMiscData("G-Screen")]
        [VisualScriptingMember(true)]
        public static void EnableToolbarConfig(bool flag)
        {
            MyPerGameSettings.GUI.EnableToolbarConfigScreen = flag;
        }

        [VisualScriptingMiscData("G-Screen")]
        [VisualScriptingMember(true)]
        public static void ToolbarConfigGroupsHideEmpty()
        {
            MyGuiScreenToolbarConfigBase.GroupMode = MyGuiScreenToolbarConfigBase.GroupModes.HideEmpty;
        }

        [VisualScriptingMiscData("G-Screen")]
        [VisualScriptingMember(true)]
        public static void ToolbarConfigGroupsHideAll()
        {
            MyGuiScreenToolbarConfigBase.GroupMode = MyGuiScreenToolbarConfigBase.GroupModes.HideAll;
        }

        [VisualScriptingMiscData("G-Screen")]
        [VisualScriptingMember(true)]
        public static void ToolbarConfigGroupsHideBlockGroups()
        {
            MyGuiScreenToolbarConfigBase.GroupMode = MyGuiScreenToolbarConfigBase.GroupModes.HideBlockGroups;
        }

        [VisualScriptingMiscData("G-Screen")]
        [VisualScriptingMember(true)]
        public static void ToolbarConfigGroupsDefualtBehavior()
        {
            MyGuiScreenToolbarConfigBase.GroupMode = MyGuiScreenToolbarConfigBase.GroupModes.Default;
        }

        [VisualScriptingMiscData("G-Screen")]
        [VisualScriptingMember(true)]
        public static void ResearchListAddItem(MyDefinitionId itemId)
        {
            if (MySessionComponentResearch.Static != null)
                MySessionComponentResearch.Static.AddRequiredResearch(itemId);
        }

        [VisualScriptingMiscData("G-Screen")]
        [VisualScriptingMember(true)]
        public static void ResearchListRemoveItem(MyDefinitionId itemId)
        {
            if (MySessionComponentResearch.Static != null)
                MySessionComponentResearch.Static.RemoveRequiredResearch(itemId);
        }

        [VisualScriptingMiscData("G-Screen")]
        [VisualScriptingMember(true)]
        public static void ResearchListClear()
        {
            if (MySessionComponentResearch.Static != null)
                MySessionComponentResearch.Static.ClearRequiredResearch();
        }

        [VisualScriptingMiscData("G-Screen")]
        [VisualScriptingMember(true)]
        public static void PlayerResearchClearAll()
        {
            if (MySessionComponentResearch.Static != null)
                MySessionComponentResearch.Static.ResetResearchForAll();
        }

        [VisualScriptingMiscData("G-Screen")]
        [VisualScriptingMember(true)]
        public static void PlayerResearchClear(long playerId = -1)
        {
            if (playerId == -1 && MySession.Static.LocalCharacter != null)
                playerId = MySession.Static.LocalCharacter.GetPlayerIdentityId();
            if (MySessionComponentResearch.Static != null)
                MySessionComponentResearch.Static.ResetResearch(playerId);
        }

        [VisualScriptingMiscData("G-Screen")]
        [VisualScriptingMember(true)]
        public static void ResearchListWhitelist(bool whitelist)
        {
            if (MySessionComponentResearch.Static != null)
                MySessionComponentResearch.Static.SwitchWhitelistMode(whitelist);
        }

        [VisualScriptingMiscData("G-Screen")]
        [VisualScriptingMember(true)]
        public static void PlayerResearchUnlock(long playerId, MyDefinitionId itemId)
        {
            if (MySessionComponentResearch.Static != null)
                MySessionComponentResearch.Static.UnlockResearchDirect(playerId, itemId);
        }

        [VisualScriptingMiscData("G-Screen")]
        [VisualScriptingMember(true)]
        public static void PlayerResearchLock(long playerId, MyDefinitionId itemId)
        {
            if (MySessionComponentResearch.Static != null)
                MySessionComponentResearch.Static.LockResearch(playerId, itemId);
        }

        #endregion

        #region GUI

        [VisualScriptingMiscData("GUI")]
        [VisualScriptingMember]
        public static void GetToolbarConfigGridItemIndexAndControl(MyDefinitionId itemDefinition, out MyGuiControlBase control, out int index)
        {
            control = null;
            index = -1;

            var toolbarConf = GetOpenedToolbarConfig();
            if (toolbarConf != null)
            {
                control = toolbarConf.GetControlByName(@"ScrollablePanel\Grid");
                var grid = control as MyGuiControlGrid;
                if (grid != null)
                {
                    for (index = 0; index < grid.GetItemsCount(); index++)
                    {
                        var item = grid.GetItemAt(index);
                        if(item == null || item.UserData == null) continue;
                        var toolbarItem = ((MyGuiScreenToolbarConfigBase.GridItemUserData)item.UserData).ItemData as MyObjectBuilder_ToolbarItemDefinition;
                        if (toolbarItem != null && toolbarItem.DefinitionId == itemDefinition)
                        {
                            break;
                        }
                    }
                }
            }
        }

        [VisualScriptingMiscData("GUI")]
        [VisualScriptingMember]
        public static void GetPlayersInventoryItemIndexAndControl(MyDefinitionId itemDefinition, out MyGuiControlBase control, out int index)
        {
            control = null;
            index = -1;

            var terminal = GetOpenedTerminal();
            if (terminal != null)
            {
                control = terminal.GetControlByName(@"TerminalTabs\PageInventory\LeftInventory\MyGuiControlInventoryOwner\InventoryGrid");
                var grid = control as MyGuiControlGrid;
                if (grid != null)
                {
                    for (index = 0; index < grid.GetItemsCount(); index++)
                    {
                        var item = grid.GetItemAt(index);
                        var physItem = (MyPhysicalInventoryItem)item.UserData;
                        if (physItem.GetDefinitionId() == itemDefinition)
                        {
                            break;
                        }
                    }
                }
            }
        }

        [VisualScriptingMiscData("GUI")]
        [VisualScriptingMember]
        public static void GetInteractedEntityInventoryItemIndexAndControl(MyDefinitionId itemDefinition, out MyGuiControlBase control, out int index)
        {
            control = null;
            index = -1;

            var terminal = GetOpenedTerminal();
            if (terminal != null)
            {
                var inventoryOwner = terminal.GetControlByName(@"TerminalTabs\PageInventory\RightInventory\MyGuiControlInventoryOwner") as MyGuiControlInventoryOwner;
                if(inventoryOwner == null) return;

                foreach (var grid in inventoryOwner.ContentGrids)
                {
                    if (grid != null)
                    {
                        control = grid;
                        for (index = 0; index < grid.GetItemsCount(); index++)
                        {
                            var item = grid.GetItemAt(index);
                            var physItem = (MyPhysicalInventoryItem)item.UserData;
                            if (physItem.GetDefinitionId() == itemDefinition)
                            {
                                return;
                            }
                        }
                    }
                }
            }
        }

        [VisualScriptingMiscData("GUI")]
        [VisualScriptingMember(true)]
        public static void OpenSteamOverlay(string url, long playerId = 0)
        {
            if (playerId == 0)
            {
                OpenSteamOverlaySync(url);
                return;
            }

            MyPlayer.PlayerId _playerId;
            // Do nothing for nonexistent player ids
            if (!MySession.Static.Players.TryGetPlayerId(playerId, out _playerId)) return;
            // Send message to respective client
            MyMultiplayer.RaiseStaticEvent(s => OpenSteamOverlaySync, url, new EndpointId(_playerId.SteamId));
        }

        [Event, Reliable, Client]
        private static void OpenSteamOverlaySync(string url)
        {
            if(MyGuiSandbox.IsUrlWhitelisted(url))
            {
                MySteam.API.OpenOverlayUrl(url);
            }
        }

        [VisualScriptingMiscData("GUI")]
        [VisualScriptingMember(true)]
        public static void HighlightGuiControl(string controlName, string activeScreenName)
        {
            foreach (var screen in MyScreenManager.Screens)
            {
                if (screen.Name == activeScreenName)
                {
                    foreach (var control in screen.Controls)
                    {
                        if (control.Name == controlName)
                        {
                            MyGuiScreenHighlight.HighlightControl(new MyGuiScreenHighlight.MyHighlightControl { Control = control });
                        }
                    }
                }
            }
        }

        [VisualScriptingMiscData("GUI")]
        [VisualScriptingMember(true)]
        public static void HighlightGuiControl(MyGuiControlBase control, List<int> indicies = null, string customToolTipMessage = null)
        {
            if(control == null) return;

            var highlightControl = new MyGuiScreenHighlight.MyHighlightControl { Control = control };
            if(indicies != null)
            {
                highlightControl.Indices = indicies.ToArray();
            }

            if(!string.IsNullOrEmpty(customToolTipMessage))
            {
                highlightControl.CustomToolTips = new MyToolTips(customToolTipMessage);
            }

            MyGuiScreenHighlight.HighlightControl(highlightControl);
        }

        [VisualScriptingMiscData("GUI")]
        [VisualScriptingMember]
        public static MyGuiControlBase GetControlByName(this MyGuiScreenBase screen, string controlName)
        {
            if(string.IsNullOrEmpty(controlName) || screen == null) return null;

            var splits = controlName.Split('\\');

            MyGuiControlBase currentControl = screen.Controls.GetControlByName(splits[0]);
            for (int index = 1; index < splits.Length; index++)
            {
                var controlParent = currentControl as MyGuiControlParent;
                if (controlParent != null)
                {
                    currentControl = controlParent.Controls.GetControlByName(splits[index]);
                    continue;
                }
                var scrollPanel = currentControl as MyGuiControlScrollablePanel;
                if (scrollPanel != null)
                {
                    currentControl = scrollPanel.Controls.GetControlByName(splits[index]);
                    continue;
                }
                if(currentControl != null)
                { 
                    currentControl = currentControl.Elements.GetControlByName(splits[index]);
                }

                break;
            }

            return currentControl;
        }

        [VisualScriptingMiscData("GUI")]
        [VisualScriptingMember]
        public static MyGuiControlBase GetControlByName(this MyGuiControlParent control, string controlName)
        {
            if (string.IsNullOrEmpty(controlName) || control == null) return null;

            var splits = controlName.Split('\\');

            MyGuiControlBase currentControl = control.Controls.GetControlByName(splits[0]);
            for (int index = 1; index < splits.Length; index++)
            {
                var controlParent = currentControl as MyGuiControlParent;
                if (controlParent != null)
                {
                    currentControl = controlParent.Controls.GetControlByName(splits[index]);
                    continue;
                }
                var scrollPanel = currentControl as MyGuiControlScrollablePanel;
                if (scrollPanel != null)
                {
                    currentControl = scrollPanel.Controls.GetControlByName(splits[index]);
                    continue;
                }
                if (currentControl != null)
                {
                    currentControl = currentControl.Elements.GetControlByName(splits[index]);
                }

                break;
            }

            return currentControl;
        }

        [VisualScriptingMiscData("GUI")]
        [VisualScriptingMember(true)]
        public static void SetTooltip(this MyGuiControlBase control, string text)
        {
            if(control == null) return;
            control.SetToolTip(text);
        }

        [VisualScriptingMiscData("GUI")]
        [VisualScriptingMember]
        public static MyGuiScreenTerminal GetOpenedTerminal()
        {
            var terminal = MyScreenManager.GetScreenWithFocus() as MyGuiScreenTerminal;
            return terminal;
        }

        [VisualScriptingMiscData("GUI")]
        [VisualScriptingMember]
        public static MyGuiControlTabPage GetTab(this MyGuiControlTabControl tabs, int key)
        {
            if(tabs == null) return null;
            return tabs.GetTabSubControl(key);
        }

        [VisualScriptingMiscData("GUI")]
        [VisualScriptingMember]
        public static MyGuiControlTabControl GetTabs(this MyGuiScreenTerminal terminal)
        {
            if (terminal == null) return null;
            return terminal.Controls.GetControlByName("TerminalTabs") as MyGuiControlTabControl;
        }

        [VisualScriptingMiscData("GUI")]
        [VisualScriptingMember]
        public static MyGuiScreenToolbarConfigBase GetOpenedToolbarConfig()
        {
            var tollbarConfig = MyScreenManager.GetScreenWithFocus() as MyGuiScreenToolbarConfigBase;
            return tollbarConfig;
        }

        [VisualScriptingMiscData("GUI")]
        [VisualScriptingMember]
        public static bool IsNewKeyPressed(MyKeys key)
        {
            return MyInput.Static.IsNewKeyPressed(key);
        }

        [VisualScriptingMiscData("GUI")]
        [VisualScriptingMember]
        public static string GetFriendlyName(this MyGuiScreenBase screen)
        {
            return screen.GetFriendlyName();
        }

        [VisualScriptingMiscData("GUI")]
        [VisualScriptingMember(true)]
        public static void SetPage(this MyGuiControlTabControl pageControl, int pageNumber)
        {
            pageControl.SelectedPage = pageNumber;
        }
        #endregion

        #region Misc

        [VisualScriptingMiscData("Misc")]
        [VisualScriptingMember(true)]
        public static void TakeScreenshot(string destination, string name)
        {
            var path = Path.Combine(destination, name, ".png");
            MyRenderProxy.TakeScreenshot(new Vector2(0.5f, 0.5f), path, false, true, false);
            MyRenderProxy.UnloadTexture(path);
        }

        [VisualScriptingMiscData("Misc")]
        [VisualScriptingMember]
        public static string GetContentPath()
        {
            return MyFileSystem.ContentPath;
        }

        [VisualScriptingMiscData("Misc")]
        [VisualScriptingMember]
        public static string GetSavesPath()
        {
            return MyFileSystem.SavesPath;
        }

        [VisualScriptingMiscData("Misc")]
        [VisualScriptingMember]
        public static string GetModsPath()
        {
            return MyFileSystem.ModsPath;
        }

        [VisualScriptingMiscData("Misc")]
        [VisualScriptingMember(true)]
        public static void SetCustomLoadingScreenImage(string imagePath)
        {
            MySession.Static.CustomLoadingScreenImage = imagePath;
        }

        [VisualScriptingMiscData("Misc")]
        [VisualScriptingMember(true)]
        public static void SetCustomLoadingScreenText(string text)
        {
            MySession.Static.CustomLoadingScreenText = text;
        }

        [VisualScriptingMiscData("Misc")]
        [VisualScriptingMember(true)]
        public static void SetCustomSkybox(string skyboxPath)
        {
            MySession.Static.CustomSkybox = skyboxPath;
        }

        [VisualScriptingMiscData("Misc")]
        [VisualScriptingMember]
        public static string GetUserControlKey(string keyName)
        {
            MyStringId keyId = MyStringId.GetOrCompute(keyName);
            MyControl control = MyInput.Static.GetGameControl(keyId);
            if (control != null)
                return control.ToString();
            else
            {
                Debug.Fail("Control with this Id does not exist.");
                return "";
            }
        }

        [VisualScriptingMiscData("Misc")]
        [VisualScriptingMember]
        public static Color GetColor(float r = 0, float g = 0, float b = 0)
        {
            r = MathHelper.Clamp(r, 0f, 1f);
            g = MathHelper.Clamp(g, 0f, 1f);
            b = MathHelper.Clamp(b, 0f, 1f);
            return new Color(r, g, b);
        }

        #endregion

        #region Notifications

        [VisualScriptingMiscData("Notifications")]
        [VisualScriptingMember(true)]
        public static void ShowNotification(string message, int disappearTimeMs, string font = MyFontEnum.White, long playerId = 0)
        {
            if(playerId == 0)
            {
                // For default player id do the action localy.
                if (MyAPIGateway.Utilities != null)
                    MyAPIGateway.Utilities.ShowNotification(message, disappearTimeMs, font);
            }
            else
            {
                MyPlayer.PlayerId _playerId;
                // Do nothing for nonexistent player ids
                if(!MySession.Static.Players.TryGetPlayerId(playerId, out _playerId)) return;
                // Send message to respective client
                MyMultiplayer.RaiseStaticEvent(s => ShowNotificationSync, message, disappearTimeMs, font, new EndpointId(_playerId.SteamId));
            }
        }

        [VisualScriptingMiscData("Notifications")]
        [VisualScriptingMember(true)]
        public static void ShowNotificationToAll(string message, int disappearTimeMs, string font = MyFontEnum.White)
        {
            if(MyMultiplayer.Static == null)
            {
                // For default player id do the action localy.
                if (MyAPIGateway.Utilities != null)
                    MyAPIGateway.Utilities.ShowNotification(message, disappearTimeMs, font);
            }
            else
            {
                // Send message to all clients
                MyMultiplayer.RaiseStaticEvent(s => ShowNotificationToAllSync, message, disappearTimeMs, font);
            }
        }

        [VisualScriptingMiscData("Notifications")]
        [VisualScriptingMember(true)]
        public static void SendChatMessage(string message, string author = "", long playerId = 0, string font = MyFontEnum.Blue)
        {
            if (MyMultiplayer.Static != null)
            {
                ScriptedChatMsg msg;
                msg.Text = message;
                msg.Author = author;
                msg.Target = playerId;
                msg.Font = font;
                MyMultiplayerBase.SendScriptedChatMessage(ref msg);
            }
            else
                MyHud.Chat.multiplayer_ScriptedChatMessageReceived(message, author, font);
        }

        [VisualScriptingMiscData("Notifications")]
        [VisualScriptingMember(true)]
        public static void SetChatMessageDuration(int durationS = 15)
        {
            MyHudChat.MaxMessageTime = durationS * 1000;
        }

        [VisualScriptingMiscData("Notifications")]
        [VisualScriptingMember(true)]
        public static void SetChatMaxMessageCount(int count = 10)
        {
            MyHudChat.MaxMessageCount = count;
        }

        [Event, Reliable, Client]
        private static void ShowNotificationSync(string message, int disappearTimeMs, string font = MyFontEnum.White)
        {
            if (MyAPIGateway.Utilities != null)
                MyAPIGateway.Utilities.ShowNotification(message, disappearTimeMs, font);
        }

        [Event, Reliable, Broadcast, Server]
        private static void ShowNotificationToAllSync(string message, int disappearTimeMs, string font = MyFontEnum.White)
        {
            if (MyAPIGateway.Utilities != null)
                MyAPIGateway.Utilities.ShowNotification(message, disappearTimeMs, font);
        }

        [VisualScriptingMiscData("Notifications")]
        [VisualScriptingMember(true)]
        public static int AddNotification(string message, string font = MyFontEnum.White, long playerId = 0)
        {
            var messageId = MyStringId.GetOrCompute(message);
            foreach (var pair in m_addedNotificationsById)
                if (pair.Value.Text == messageId)
                    return pair.Key;

            var id = m_notificationIdCounter++;

            if(playerId == 0)
                playerId = GetLocalPlayerId();

            MyPlayer.PlayerId _playerId;
            // Do nothing for nonexistent player ids
            if (!MySession.Static.Players.TryGetPlayerId(playerId, out _playerId)) return -1;
            // Send message to respective client
            MyMultiplayer.RaiseStaticEvent(s => AddNotificationSync, messageId, font, id, new EndpointId(_playerId.SteamId));
            return id;
        }

        [Event, Reliable, Client]
        private static void AddNotificationSync(MyStringId message, string font, int notificationId)
        {
            var notification = new MyHudNotification(message, 0, font);
            MyHud.Notifications.Add(notification);
            m_addedNotificationsById.Add(notificationId, notification);
        }

        [VisualScriptingMiscData("Notifications")]
        [VisualScriptingMember(true)]
        public static void RemoveNotification(int messageId, long playerId = 0)
        {
            // For local purposes just call the method directly.
            if(playerId == 0)
            {
                RemoveNotificationSync(messageId);
                return;
            }

            MyPlayer.PlayerId _playerId;
            // Do nothing for nonexistent player ids
            if (!MySession.Static.Players.TryGetPlayerId(playerId, out _playerId)) return;
            // Send message to respective client
            MyMultiplayer.RaiseStaticEvent(s => RemoveNotificationSync, messageId, new EndpointId(_playerId.SteamId));
        }

        [Event, Reliable, Client]
        private static void RemoveNotificationSync(int messageId)
        {
            MyHudNotification notification;
            if (m_addedNotificationsById.TryGetValue(messageId, out notification))
            {
                MyHud.Notifications.Remove(notification);
                m_addedNotificationsById.Remove(messageId);
            }
        }

        [VisualScriptingMiscData("Notifications")]
        [VisualScriptingMember(true)]
        public static void ClearNotifications(long playerId = 0)
        {
            // Direct call for local purposes
            if (playerId == 0)
            {
                ClearNotificationSync();
                return;
            }

            MyPlayer.PlayerId _playerId;
            // Do nothing for nonexistent player ids
            if (!MySession.Static.Players.TryGetPlayerId(playerId, out _playerId)) return;
            // Send message to respective client
            MyMultiplayer.RaiseStaticEvent(s => ClearNotificationSync, new EndpointId(_playerId.SteamId));
        }

        [Event, Reliable, Client]
        private static void ClearNotificationSync()
        {
            MyHud.Notifications.Clear();
            m_notificationIdCounter = 0;
            m_addedNotificationsById.Clear();
        }

        #endregion

        #region Players

        private static MyCharacter GetCharacterFromPlayerId(long playerId)
        {
            if (playerId > 0)
            {
                var identity = MySession.Static.Players.TryGetIdentity(playerId);
                if (identity != null)
                    return identity.Character;
            }
            else
            {
                return MySession.Static.LocalCharacter;
            }
            return null;
        }

        private static MyIdentity GetIdentityFromPlayerId(long playerId)
        {
            if (playerId > 0)
            {
                return MySession.Static.Players.TryGetIdentity(playerId);
            }
            else
            {
                return MySession.Static.LocalHumanPlayer.Identity;
            }
            return null;
        }

        private static MyPlayer GetPlayerFromPlayerId(long playerId)
        {
            if (playerId > 0)
            {
                MyPlayer player = null;
                MyPlayer.PlayerId playerIdInternal;
                if (MySession.Static.Players.TryGetPlayerId(playerId, out playerIdInternal))
                {
                    MySession.Static.Players.TryGetPlayerById(playerIdInternal, out player);
                }
                return null;
            }
            else
            {
                return MySession.Static.LocalHumanPlayer;
            }
            return null;
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember]
        public static List<long> GetOnlinePlayers()
        {
            var players = MySession.Static.Players.GetOnlinePlayers();
            List<long> result = new List<long>();
            if (players != null && players.Count > 0)
            {
                foreach (var player in players)
                    result.Add(player.Identity.IdentityId);
            }
            return result;
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember]
        public static float GetOxygenLevelAtPlayersPosition(long playerId = -1)
        {
            if (MySession.Static.Settings.EnableOxygenPressurization && MySession.Static.Settings.EnableOxygen)
            {
                MyCharacter character = GetCharacterFromPlayerId(playerId);
                if (character != null && character.OxygenComponent != null)
                    return character.OxygenComponent.OxygenLevelAtCharacterLocation;
            }

            return 1f;
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember]
        public static bool GetPlayersHelmetStatus(long playerId = -1)
        {
            if (MySession.Static.Settings.EnableOxygenPressurization && MySession.Static.Settings.EnableOxygen)
            {
                MyCharacter character = GetCharacterFromPlayerId(playerId);
                if (character != null && character.OxygenComponent != null)
                    return character.OxygenComponent.HelmetEnabled;
            }

            return false;
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember]
        public static Vector3D GetPlayersSpeed(long playerId = -1)
        {
            MyCharacter character = GetCharacterFromPlayerId(playerId);
            if (character != null)
            {
                return character.Physics.LinearVelocity;
            }

            return Vector3D.Zero;
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember(true)]
        public static void SetPlayersSpeed(Vector3D speed = default(Vector3D), long playerId = -1)
        {
            MyCharacter character = GetCharacterFromPlayerId(playerId);
            if (character != null)
            {
                if (speed != Vector3D.Zero)
                {
                    float maxCharacterSpeedRelativeToShip = Math.Max(character.Definition.MaxSprintSpeed, Math.Max(character.Definition.MaxRunSpeed, character.Definition.MaxBackrunSpeed));
                    float maxSpeed = MyGridPhysics.ShipMaxLinearVelocity() + maxCharacterSpeedRelativeToShip;
                    if (speed.LengthSquared() > maxSpeed * maxSpeed)
                    {
                        speed.Normalize();
                        speed *= maxSpeed;
                    }
                }
                character.Physics.LinearVelocity = speed;
            }
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember]
        public static bool IsPlayerInCockpit(long playerId = -1, string gridName = null, string cockpitName = null)
        {
            MyPlayer player = GetPlayerFromPlayerId(playerId);
            MyCockpit cockpit = null;
            if (player != null && player.Controller != null && player.Controller.ControlledEntity != null)
                cockpit = player.Controller.ControlledEntity.Entity as MyCockpit;

            if (cockpit == null) return false;

            if (string.IsNullOrEmpty(gridName) == false && cockpit.CubeGrid.Name != gridName)
                return false;

            if (string.IsNullOrEmpty(cockpitName) == false && cockpit.Name != cockpitName)
                return false;

            return true;
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember]
        public static bool IsPlayerInRemote(long playerId = -1, string gridName = null, string remoteName = null)
        {
            MyPlayer player = GetPlayerFromPlayerId(playerId);
            MyRemoteControl remote = null;
            if (player != null && player.Controller != null && player.Controller.ControlledEntity != null)
                remote = player.Controller.ControlledEntity.Entity as MyRemoteControl;

            if (remote == null) return false;

            if (string.IsNullOrEmpty(gridName) == false && remote.CubeGrid.Name != gridName)
                return false;

            if (string.IsNullOrEmpty(remoteName) == false && remote.Name != remoteName)
                return false;

            return true;
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember]
        public static bool IsPlayerInWeapon(long playerId = -1, string gridName = null, string weaponName = null)
        {
            MyPlayer player = GetPlayerFromPlayerId(playerId);
            MyUserControllableGun weapon = null;
            if (player != null && player.Controller != null && player.Controller.ControlledEntity != null)
                weapon = player.Controller.ControlledEntity.Entity as MyUserControllableGun;

            if (weapon == null) return false;

            if (string.IsNullOrEmpty(gridName) == false && weapon.CubeGrid.Name != gridName)
                return false;

            if (string.IsNullOrEmpty(weaponName) == false && weapon.Name != weaponName)
                return false;

            return true;
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember]
        public static bool IsPlayerDead(long playerId = -1)
        {
            MyCharacter character = GetCharacterFromPlayerId(playerId);
            if (character != null)
                return character.IsDead;
            return false;
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember]
        public static string GetPlayersName(long playerId = -1)
        {
            MyIdentity identity = GetIdentityFromPlayerId(playerId);
            if (identity != null)
            {
                return identity.DisplayName;
            }

            return "";
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember]
        public static float GetPlayersHealth(long playerId = -1)
        {
            MyIdentity identity = GetIdentityFromPlayerId(playerId);
            if (identity != null)
            {
                return identity.Character.StatComp.Health.Value;
            }

            return -1;
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember]
        public static bool IsPlayersJetpackEnabled(long playerId = -1)
        {
            MyIdentity identity = GetIdentityFromPlayerId(playerId);
            if (identity != null && identity.Character != null && identity.Character.JetpackComp != null)
            {
                return identity.Character.JetpackComp.TurnedOn;
            }

            return false;
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember]
        public static float GetPlayersOxygenLevel(long playerId = -1)
        {
            MyIdentity identity = GetIdentityFromPlayerId(playerId);
            if (identity != null)
            {
                return identity.Character.OxygenComponent.SuitOxygenLevel;
            }

            return -1;
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember]
        public static float GetPlayersHydrogenLevel(long playerId = -1)
        {
            MyIdentity identity = GetIdentityFromPlayerId(playerId);
            if (identity != null)
            {
                return identity.Character.OxygenComponent.GetGasFillLevel(MyCharacterOxygenComponent.HydrogenId);
            }

            return -1;
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember]
        public static float GetPlayersEnergyLevel(long playerId = -1)
        {
            MyIdentity identity = GetIdentityFromPlayerId(playerId);
            if (identity != null)
            {
                return identity.Character.SuitEnergyLevel;
            }

            return -1;
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember(true)]
        public static void SetPlayersHealth(long playerId = -1, float value = 100f)
        {
            MyIdentity identity = GetIdentityFromPlayerId(playerId);
            if (identity != null)
            {
                identity.Character.StatComp.Health.Value = value;
            }
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember(true)]
        public static void SetPlayersOxygenLevel(long playerId = -1, float value = 1f)
        {
            MyIdentity identity = GetIdentityFromPlayerId(playerId);
            if (identity != null)
            {
                identity.Character.OxygenComponent.SuitOxygenLevel = value;
            }
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember(true)]
        public static void SetPlayersHydrogenLevel(long playerId = -1, float value = 1f)
        {
            MyIdentity identity = GetIdentityFromPlayerId(playerId);
            if (identity != null)
            {
                var hydrogenId = MyCharacterOxygenComponent.HydrogenId;
                identity.Character.OxygenComponent.UpdateStoredGasLevel(ref hydrogenId, value);
            }
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember(true)]
        public static void SetPlayersEnergyLevel(long playerId = -1, float value = 1f)
        {
            MyIdentity identity = GetIdentityFromPlayerId(playerId);
            if (identity != null)
            {
                identity.Character.SuitBattery.ResourceSource
                    .SetRemainingCapacityByType(MyResourceDistributorComponent.ElectricityId, value * MyEnergyConstants.BATTERY_MAX_CAPACITY);
            }
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember(true)]
        public static void SetPlayersPosition(long playerId = -1, Vector3D position = default(Vector3D))
        {
            MyIdentity identity = GetIdentityFromPlayerId(playerId);
            if (identity != null)
            {
                identity.Character.PositionComp.SetPosition(position);
            }
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember]
        public static Vector3D GetPlayersPosition(long playerId = -1)
        {
            MyIdentity identity = GetIdentityFromPlayerId(playerId);
            if (identity != null)
            {
                return identity.Character.PositionComp.GetPosition();
            }

            return Vector3D.Zero;
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember]
        public static int GetPlayersInventoryItemAmount(long playerId = -1, MyDefinitionId itemId = default(MyDefinitionId))
        {
            MyIdentity identity = GetIdentityFromPlayerId(playerId);
            if (identity != null)
            {
                if (!itemId.TypeId.IsNull && identity.Character != null)
                {
                    var inventory = identity.Character.GetInventory();
                    if(inventory != null)
                    {
                        return inventory.GetItemAmount(itemId).ToIntSafe();
                    }
                }
            }

            return -1;
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember(true)]
        public static void AddToPlayersInventory(long playerId = -1, MyDefinitionId itemId = default(MyDefinitionId), int amount = 1)
        {
            MyIdentity identity = GetIdentityFromPlayerId(playerId);
            if (identity != null)
            {
                var inventory = identity.Character.GetInventory();
                if (inventory != null)
                {
                    var ob = (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(itemId);
                    var amountInFixedPoint = new MyFixedPoint();
                    amountInFixedPoint = amount;
                    if (!inventory.AddItems(amountInFixedPoint, ob))
                        Debug.Fail("Item: " + itemId.ToString() + " Amount: " + amount + " Adding to inventory failed. (Probably not enough space.)");
                }
            }
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember(true)]
        public static void RemoveFromPlayersInventory(long playerId = -1, MyDefinitionId itemId = default(MyDefinitionId), int amount = 1)
        {
            MyIdentity identity = GetIdentityFromPlayerId(playerId);
            if (identity != null)
            {
                var inventory = identity.Character.GetInventory();
                if (inventory != null)
                {
                    var ob = (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(itemId);
                    MyFixedPoint amountInFixedPoint = new MyFixedPoint();
                    amountInFixedPoint = amount;
                    MyFixedPoint currentAmount = inventory.GetItemAmount(itemId);
                    inventory.RemoveItemsOfType(amountInFixedPoint < currentAmount ? amountInFixedPoint : currentAmount, ob);
                }
            }
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember(true)]
        public static void SetPlayerGeneralDamageModifier(long playerId = -1, float modifier = 1f)
        {
            MyCharacter character = null;
            if (playerId > 0)
            {
                var identity = MySession.Static.Players.TryGetIdentity(playerId);
                if (identity != null)
                    character = identity.Character;
            }
            else
            {
                character = MySession.Static.LocalCharacter;
            }
            if (character != null)
            {
                character.CharacterGeneralDamageModifier = modifier;
            }
        }

        #endregion

        #region Questlog

        [VisualScriptingMiscData("Questlog")]
        [VisualScriptingMember(true)]
        public static void SetQuestlog(bool visible = true, string questName = "")
        {
            MyHud.Questlog.QuestTitle = questName;
            MyHud.Questlog.CleanDetails();
            MyHud.Questlog.Visible = visible;
        }

        [VisualScriptingMiscData("Questlog")]
        [VisualScriptingMember(true)]
        public static void SetQuestlogTitle(string questName = "")
        {
            MyHud.Questlog.QuestTitle = questName;
        }

        [VisualScriptingMiscData("Questlog")]
        [VisualScriptingMember(true)]
        public static int AddQuestlogDetail(string questDetailRow = "", bool completePrevious = true, bool useTyping = true)
        {
            int id = MyHud.Questlog.AddDetail(questDetailRow, useTyping);
            if (completePrevious)
                MyHud.Questlog.SetCompleted(id - 1, true);
            return id;
        }

        [VisualScriptingMiscData("Questlog")]
        [VisualScriptingMember(true)]
        public static void SetQuestlogDetailCompleted(int lineId = 0, bool completed = true)
        {
            MyHud.Questlog.SetCompleted(lineId, completed);
        }

        [VisualScriptingMiscData("Questlog")]
        [VisualScriptingMember(true)]
        public static void SetAllQuestlogDetailsCompleted(bool completed = true)
        {
            MyHud.Questlog.SetAllCompleted(completed);
        }

        [VisualScriptingMiscData("Questlog")]
        [VisualScriptingMember(true)]
        public static void ReplaceQuestlogDetail(int id = 0, string newDetail = "", bool useTyping = true)
        {
            MyHud.Questlog.ModifyDetail(id, newDetail, useTyping);
        }

        [VisualScriptingMiscData("Questlog")]
        [VisualScriptingMember(true)]
        public static void RemoveQuestlogDetails()
        {
            MyHud.Questlog.CleanDetails();
        }

        [VisualScriptingMiscData("Questlog")]
        [VisualScriptingMember(true)]
        public static void SetQuestlogPage(int value = 0)
        {
            MyHud.Questlog.Page = value;
        }

        [VisualScriptingMiscData("Questlog")]
        [VisualScriptingMember(false)]
        public static int GetQuestlogPage()
        {
            return MyHud.Questlog.Page;
        }

        [VisualScriptingMiscData("Questlog")]
        [VisualScriptingMember(false)]
        public static int GetQuestlogMaxPages()
        {
            return MyHud.Questlog.MaxPages;
        }

        [VisualScriptingMiscData("Questlog")]
        [VisualScriptingMember(true)]
        public static void SetQuestlogVisible(bool value = true)
        {
            MyHud.Questlog.Visible = value;
        }

        [VisualScriptingMiscData("Questlog")]
        [VisualScriptingMember(false)]
        public static int GetQuestlogPageFromMessage(int id = 0)
        {
            return MyHud.Questlog.GetPageFromMessage(id);
        }

        [VisualScriptingMiscData("Questlog")]
        [VisualScriptingMember(true)]
        public static void EnableHighlight(bool enable = true)
        {
            MyHud.Questlog.HighlightChanges = enable;
        }

        #endregion

        #region Spawn

        [VisualScriptingMiscData("Spawn")]
        [VisualScriptingMember(true)]
        public static void SpawnGroup(string subtypeId, Vector3D position, Vector3D direction, Vector3D up, long ownerId = 0, string newGridName = null)
        {
            var definitions = MyDefinitionManager.Static.GetSpawnGroupDefinitions();
            MySpawnGroupDefinition definition = null;
            foreach (var spawnGroupDefinition in definitions)
            {
                if (spawnGroupDefinition.Id.SubtypeName == subtypeId)
                {
                    definition = spawnGroupDefinition;
                    break;
                }
            }

            if(definition == null)
                return;

            List<MyCubeGrid> tmpGridList = new List<MyCubeGrid>();
            direction.Normalize();
            up.Normalize();
            MatrixD originMatrix = MatrixD.CreateWorld(position, direction, up);

            var namingCallback = new Action(() =>
            {
                if (newGridName != null && tmpGridList.Count > 0)
                {
                    tmpGridList[0].Name = newGridName;
                    MyEntities.SetEntityName(tmpGridList[0]);
                }
            });

            var actionStack = new Stack<Action>();
            actionStack.Push(namingCallback);

            foreach (var prefab in definition.Prefabs)
            {
                Vector3D shipPosition = Vector3D.Transform((Vector3D)prefab.Position, originMatrix);

                MyPrefabManager.Static.SpawnPrefab(
                    resultList: tmpGridList,
                    prefabName: prefab.SubtypeId,
                    position: shipPosition,
                    forward: direction,
                    up: up,
                    initialLinearVelocity: prefab.Speed * direction,
                    beaconName: prefab.BeaconText,
                    spawningOptions: VRage.Game.ModAPI.SpawningOptions.RotateFirstCockpitTowardsDirection,
                    ownerId: ownerId,
                    updateSync: true,
                    callbacks: actionStack);
            }
        }

        [VisualScriptingMiscData("Spawn")]
        [VisualScriptingMember(true)]
        public static void SpawnLocalBlueprint(string name, Vector3D position, Vector3D direction = default(Vector3D), string newGridName = null, long ownerId = 0)
        {
            SpawnAlignedToGravityWithOffset(name, position, direction, newGridName, ownerId, 0);
        }

        [VisualScriptingMiscData("Spawn")]
        [VisualScriptingMember(true)]
        public static void SpawnLocalBlueprintInGravity(string name, Vector3D position, float rotationAngle = 0, float gravityOffset = 0, string newGridName = null, long ownerId = 0)
        {
            SpawnAlignedToGravityWithOffset(name, position, default(Vector3D), newGridName, ownerId, gravityOffset, rotationAngle);
        }

        private static void SpawnAlignedToGravityWithOffset(string name, Vector3D position, Vector3D direction, string newGridName, long ownerId = 0, float gravityOffset = 0, float gravityRotation = 0)
        {
            var localBPPath = Path.Combine(MyFileSystem.UserDataPath, "Blueprints", "local");
            var localBPFullPath = Path.Combine(localBPPath, name, "bp.sbc");
            MyObjectBuilder_ShipBlueprintDefinition[] blueprints = null;

            if (MyFileSystem.FileExists(localBPFullPath))
            {
                MyObjectBuilder_Definitions definitions;
                if(!MyObjectBuilderSerializer.DeserializeXML(localBPFullPath, out definitions))
                {
                    Debug.Fail("Blueprint of name: " + name + " was not found.");
                    return;
                }

                blueprints = definitions.ShipBlueprints;
            }

            if (blueprints == null)
                return;

            // Calculate transformations
            Vector3 gravity = MyGravityProviderSystem.CalculateNaturalGravityInPoint(position);

            // Get artificial gravity
            if(gravity == Vector3.Zero)
                gravity = MyGravityProviderSystem.CalculateArtificialGravityInPoint(position);

            Vector3D up;

            if (gravity != Vector3.Zero)
            {
                gravity.Normalize();
                up = -gravity;
                position = position + gravity * gravityOffset;
                if(direction == Vector3D.Zero)
                {
                    direction = Vector3D.CalculatePerpendicularVector(gravity);
                    if(gravityRotation != 0)
                    {
                        var rotationAlongAxis = MatrixD.CreateFromAxisAngle(up, gravityRotation);
                        direction = Vector3D.Transform(direction, rotationAlongAxis);
                    }
                }
            }
            else
            {
                if(direction == Vector3D.Zero)
                {
                    direction = Vector3D.Right;
                    up = Vector3D.Up;
                } else
                {
                    up = Vector3D.CalculatePerpendicularVector(-direction);
                }
            }

            List<MyObjectBuilder_CubeGrid> cubeGrids = new List<MyObjectBuilder_CubeGrid>();
            foreach (var blueprintDefinition in blueprints)
            {
                foreach (var cubeGrid in blueprintDefinition.CubeGrids)
                {
                    cubeGrid.CreatePhysics = true;
                    cubeGrid.EnableSmallToLargeConnections = true;
                    cubeGrid.PositionAndOrientation = new MyPositionAndOrientation(position, direction, up);
                    cubeGrid.PositionAndOrientation.Value.Orientation.Normalize();

                    if (!string.IsNullOrEmpty(newGridName))
                        cubeGrid.Name = newGridName;

                    cubeGrids.Add(cubeGrid);
                }
            }
            if (!MySandboxGame.IsDedicated)
            {
                MyHud.PushRotatingWheelVisible();
            }
            MyMultiplayer.RaiseStaticEvent(s => MyCubeGrid.TryPasteGrid_Implementation, cubeGrids, false, ownerId, Vector3.Zero, false, true);
        }

        [VisualScriptingMiscData("Spawn")]
        [VisualScriptingMember(true)]
        public static void SpawnBot(string subtypeName, Vector3D position)
        {
            var botDefinitionId = new MyDefinitionId(typeof(MyObjectBuilder_AnimalBot), subtypeName);
            MyBotDefinition botDefinition;

            if (!MyDefinitionManager.Static.TryGetBotDefinition(botDefinitionId, out botDefinition))
            {
                Debug.Fail(subtypeName + " is not valid identifier of AgentDefinition");
                return;
            }

            if (botDefinition != null)
                MyAIComponent.Static.SpawnNewBot(botDefinition as MyAgentDefinition, position, false);
        }

        #endregion

        #region StateMachines

        [VisualScriptingMiscData("StateMachines")]
        [VisualScriptingMember(true)]
        public static void StartStateMachine(string stateMachineName, long ownerId = 0)
        {
            var vSManager = MySession.Static.GetComponent<MyVisualScriptManagerSessionComponent>();
            if (vSManager != null && !vSManager.SMManager.Run(stateMachineName, ownerId))
                Debug.Fail("Mission name: '" + stateMachineName + "' is not defined in MissionManager.");
        }

        #endregion

        #region Toolbar

        [VisualScriptingMiscData("Toolbar")]
        [VisualScriptingMember(true)]
        public static void SetToolbarSlotToItem(int slot, MyDefinitionId itemId, long playerId = -1)
        {
            if (itemId.TypeId.IsNull)
                return;

            MyMultiplayer.RaiseStaticEvent(s => SetToolbarSlotToItemSync, slot, itemId, playerId);
        }
        [Event, Reliable, Server, Broadcast]
        private static void SetToolbarSlotToItemSync(int slot, MyDefinitionId itemId, long playerId = -1)
        {
            if (playerId != -1 && (MySession.Static.LocalCharacter == null || MySession.Static.LocalCharacter.GetPlayerIdentityId() != playerId))
                return;

            MyDefinitionBase definition;
            if (!MyDefinitionManager.Static.TryGetDefinition(itemId, out definition))
                return;

            var ob = MyToolbarItemFactory.ObjectBuilderFromDefinition(definition);
            var toolbarItem = MyToolbarItemFactory.CreateToolbarItem(ob);
            if (MyToolbarComponent.CurrentToolbar.SelectedSlot.HasValue && MyToolbarComponent.CurrentToolbar.SelectedSlot == slot)
                MyToolbarComponent.CurrentToolbar.Unselect(false);
            MyToolbarComponent.CurrentToolbar.SetItemAtSlot(slot, toolbarItem);
        }

        [VisualScriptingMiscData("Toolbar")]
        [VisualScriptingMember(true)]
        public static void SwitchToolbarToSlot(int slot, long playerId = -1)
        {
            MyMultiplayer.RaiseStaticEvent(s => SwitchToolbarToSlotSync, slot, playerId);
        }
        [Event, Reliable, Server, Broadcast]
        private static void SwitchToolbarToSlotSync(int slot, long playerId = -1)
        {
            if (playerId != -1 && (MySession.Static.LocalCharacter == null || MySession.Static.LocalCharacter.GetPlayerIdentityId() != playerId))
                return;
            if (slot < 0 || slot >= MyToolbarComponent.CurrentToolbar.SlotCount)
                return;

            if (MyToolbarComponent.CurrentToolbar.SelectedSlot.HasValue && MyToolbarComponent.CurrentToolbar.SelectedSlot.Value == slot)
                MyToolbarComponent.CurrentToolbar.Unselect(false);

            MyToolbarComponent.CurrentToolbar.ActivateItemAtSlot(slot);
        }

        [VisualScriptingMiscData("Toolbar")]
        [VisualScriptingMember(true)]
        public static void ClearToolbarSlot(int slot, long playerId = -1)
        {
            MyMultiplayer.RaiseStaticEvent(s => ClearToolbarSlotSync, slot, playerId);
        }
        [Event, Reliable, Server, Broadcast]
        private static void ClearToolbarSlotSync(int slot, long playerId = -1)
        {
            if (slot < 0 || slot >= MyToolbarComponent.CurrentToolbar.SlotCount)
                return;
            if (playerId != -1 && (MySession.Static.LocalCharacter == null || MySession.Static.LocalCharacter.GetPlayerIdentityId() != playerId))
                return;

            MyToolbarComponent.CurrentToolbar.SetItemAtSlot(slot, null);
        }

        [VisualScriptingMiscData("Toolbar")]
        [VisualScriptingMember(true)]
        public static void ClearAllToolbarSlots(long playerId = -1)
        {
            MyMultiplayer.RaiseStaticEvent(s => ClearAllToolbarSlotsSync, playerId);
        }
        [Event, Reliable, Server, Broadcast]
        private static void ClearAllToolbarSlotsSync(long playerId = -1)
        {
            if (playerId != -1 && (MySession.Static.LocalCharacter == null || MySession.Static.LocalCharacter.GetPlayerIdentityId() != playerId))
                return;

            int origPage = MyToolbarComponent.CurrentToolbar.CurrentPage;
            for (int i = 0; i < MyToolbarComponent.CurrentToolbar.PageCount; i++)
            {
                MyToolbarComponent.CurrentToolbar.SwitchToPage(i);
                for (int j = 0; j < MyToolbarComponent.CurrentToolbar.SlotCount; j++)
                {
                    MyToolbarComponent.CurrentToolbar.SetItemAtSlot(j, null);
                }
            }
            MyToolbarComponent.CurrentToolbar.SwitchToPage(origPage);
        }

        [VisualScriptingMiscData("Toolbar")]
        [VisualScriptingMember(true)]
        public static void SetToolbarPage(int page, long playerId = -1)
        {
            MyMultiplayer.RaiseStaticEvent(s => SetToolbarPageSync, page, playerId);
        }
        [Event, Reliable, Server, Broadcast]
        private static void SetToolbarPageSync(int page, long playerId = -1)
        {
            if (playerId != -1 && (MySession.Static.LocalCharacter == null || MySession.Static.LocalCharacter.GetPlayerIdentityId() != playerId))
                return;
            if (page < 0 || page >= MyToolbarComponent.CurrentToolbar.PageCount)
                return;

            MyToolbarComponent.CurrentToolbar.SwitchToPage(page);
        }

        [VisualScriptingMiscData("Toolbar")]
        [VisualScriptingMember(true)]
        public static void ReloadToolbarDefaults(long playerId = -1)
        {
            MyMultiplayer.RaiseStaticEvent(s => ReloadToolbarDefaultsSync, playerId);
        }
        [Event, Reliable, Server, Broadcast]
        private static void ReloadToolbarDefaultsSync(long playerId = -1)
        {
            if (playerId != -1 && (MySession.Static.LocalCharacter == null || MySession.Static.LocalCharacter.GetPlayerIdentityId() != playerId))
                return;
            MyToolbarComponent.CurrentToolbar.SetDefaults();
        }

        #endregion

        #region Triggers

        [VisualScriptingMiscData("Triggers")]
        [VisualScriptingMember(true)]
        public static void CreateAreaTriggerOnEntity(string entityName, float radius, string name)
        {
            var trigger = new MyAreaTriggerComponent(name);
            trigger.Radius = radius;

            MyEntity entity;
            if (MyEntities.TryGetEntityByName(entityName, out entity))
            {
                trigger.Center = entity.PositionComp.GetPosition();
                trigger.DefaultTranslation = Vector3D.Zero;
                if(!entity.Components.Contains(typeof(MyTriggerAggregate)))
                    entity.Components.Add(typeof(MyTriggerAggregate), new MyTriggerAggregate());
                entity.Components.Get<MyTriggerAggregate>().AddComponent(trigger);
            }
        }

        [VisualScriptingMiscData("Triggers")]
        [VisualScriptingMember(true)]
        public static void CreateAreaTriggerRelativeToEntity(Vector3D position, string entityName, float radius, string name)
        {
            var trigger = new MyAreaTriggerComponent(name);
            trigger.Radius = radius;

            MyEntity entity;
            if (MyEntities.TryGetEntityByName(entityName, out entity))
            {
                trigger.Center = position;
                trigger.DefaultTranslation = position - entity.PositionComp.GetPosition();
                if (!entity.Components.Contains(typeof(MyTriggerAggregate)))
                    entity.Components.Add(typeof(MyTriggerAggregate), new MyTriggerAggregate());
                entity.Components.Get<MyTriggerAggregate>().AddComponent(trigger);
            }
        }

        [VisualScriptingMiscData("Triggers")]
        [VisualScriptingMember(true)]
        public static long CreateAreaTriggerOnPosition(Vector3D position, float radius, string name)
        {
            var trigger = new MyAreaTriggerComponent(name);
            var entity = new MyEntity();
            trigger.Radius = radius;
            entity.PositionComp.SetPosition(position);
            entity.EntityId = MyEntityIdentifier.AllocateId();
            trigger.DefaultTranslation = Vector3D.Zero;

            MyEntities.Add(entity);
            if (!entity.Components.Contains(typeof(MyTriggerAggregate)))
                entity.Components.Add(typeof(MyTriggerAggregate), new MyTriggerAggregate());
            entity.Components.Get<MyTriggerAggregate>().AddComponent(trigger);

            return entity.EntityId;
        }

        [VisualScriptingMiscData("Triggers")]
        [VisualScriptingMember(true)]
        public static void RemoveAllTriggersFromEntity(string entityName)
        {
            MyEntity entity;
            if (MyEntities.TryGetEntityByName(entityName, out entity))
            {
                entity.Components.Remove(typeof(MyTriggerAggregate));
            }
        }

        [VisualScriptingMiscData("Triggers")]
        [VisualScriptingMember(true)]
        public static void RemoveTrigger(string triggerName)
        {
            MyTriggerComponent trigger;
            MyEntity entity = MySessionComponentTriggerSystem.Static.GetTriggersEntity(triggerName, out trigger);
            if (entity != null && trigger != null)
            {
                MyTriggerAggregate aggregate;
                if (entity.Components.TryGet<MyTriggerAggregate>(out aggregate))
                {
                    aggregate.RemoveComponent(trigger);
                }
                else
                {
                    entity.Components.Remove(typeof(MyAreaTriggerComponent), trigger as MyAreaTriggerComponent);
                }
            }
        }

        #endregion
    }
}