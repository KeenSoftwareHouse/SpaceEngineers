#region Usings
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
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

        public static FloatingObjectPlayerEvent PlayerPickedUp;
        public static PlayerItemEvent PlayerDropped;

        public static SingleKeyTriggerEvent AreaTrigger_Left;
        public static SingleKeyTriggerEvent AreaTrigger_Entered;

        public static SingleKeyEntityNameGridNameEvent BlockDestroyed;
        public static BlockEvent BlockBuilt;
        private static MyStringId MUSIC = MyStringId.GetOrCompute("Music");
        public static bool GameIsReady = false;

        public static MyEntity GetEntityByName(string name)
        {
            MyEntity entity;
            if(!MyEntities.TryGetEntityByName(name, out entity))
                return null;

            return entity;
        }

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

            // Register types that are used in VS but are not part of VRage
            MyVisualScriptingProxy.RegisterType(typeof(MyGuiSounds));
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
        public static void SetDroneBehaviour(string entityName, string presetName = "Default", bool activate = true, bool assignToPirates = true)
        {
            SetDroneBehaviourWithFirstWaypoint(entityName, Vector3D.Zero, presetName, activate, assignToPirates);
        }

        [VisualScriptingMiscData("AI")]
        [VisualScriptingMember(true)]
        public static void SetDroneBehaviourWithFirstWaypoint(string entityName, Vector3D firstWaypoint, string presetName = "Default", bool activate = true, bool assignToPirates = true)
        {
            var entity = GetEntityByName(entityName);
            if (entity == null)
            {
                Debug.Fail("Entity of name \"" + entityName + "\" was not found.");
                return;
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
            if (remote != null)
            {
                MySpaceStrafeData droneBehavior = MySpaceStrafeDataStatic.LoadPreset(presetName);
                var blocks = remote.CubeGrid.GetBlocks();
                List<MyUserControllableGun> guns = new List<MyUserControllableGun>();
                List<MyFunctionalBlock> tools = new List<MyFunctionalBlock>();
                foreach (var block in blocks)
                {
                    if (block.FatBlock is MyUserControllableGun)
                        guns.Add(block.FatBlock as MyUserControllableGun);
                    if (block.FatBlock is MyShipToolBase)
                        tools.Add(block.FatBlock as MyFunctionalBlock);
                    if (block.FatBlock is MyShipDrill)
                        tools.Add(block.FatBlock as MyFunctionalBlock);
                }
                if (assignToPirates)
                    remote.CubeGrid.ChangeGridOwnership(GetPirateId(), MyOwnershipShareModeEnum.Faction);
                MyDroneStrafeBehaviour behaviourComponent = new MyDroneStrafeBehaviour(remote, droneBehavior, activate, guns, tools, firstWaypoint);
                remote.SetAutomaticBehaviour(behaviourComponent);
                if (activate)
                    remote.SetAutoPilotEnabled(true);
            }
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

        #region Blocks

        [VisualScriptingMiscData("Blocks")]
        [VisualScriptingMember(true)]
        public static void EnableBlock(string blockName)
        {
            SetBlockState(blockName, true);
        }

        [VisualScriptingMiscData("Blocks")]
        [VisualScriptingMember(true)]
        public static void DisableBlock(string blockName)
        {
            SetBlockState(blockName, false);
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

        [VisualScriptingMiscData("Blocks")]
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

        [VisualScriptingMiscData("Blocks")]
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

        [VisualScriptingMiscData("Blocks")]
        [VisualScriptingMember(true)]
        public static void StartTimerBlock(string blockName)
        {
            MyEntity entity;
            if (!MyEntities.TryGetEntityByName(blockName, out entity))
                return;

            var block = entity as IMyFunctionalBlock;
            if (block != null)
            {
                block.ApplyAction("Start");
            }
        }

        [VisualScriptingMiscData("Blocks")]
        [VisualScriptingMember(true)]
        public static void SetLandingGearLock(string entityName, bool locked = true)
        {
            MyEntity entity = GetEntityByName(entityName);
            Debug.Assert(entity != null, "Designers: Entity was not found: " + entityName);

            var landingGear = entity as IMyLandingGear;
            Debug.Assert(landingGear != null, "Entity of name: " + entityName + " is not a landingGear.");
            if (landingGear != null)
            {
                landingGear.RequestLock(locked);
            }
        }

        [VisualScriptingMiscData("Blocks")]
        [VisualScriptingMember]
        public static bool IsLandingGearLocked(string entityName)
        {
            var entity = GetEntityByName(entityName);
            Debug.Assert(entity != null, "Entity of name " + entityName + " was not found.");
            if (entity == null) return false;

            var landingGear = entity as IMyLandingGear;
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

        [VisualScriptingMiscData("Blocks")]
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

        [VisualScriptingMiscData("Blocks")]
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

        [VisualScriptingMiscData("Blocks")]
        [VisualScriptingMember]
        public static bool IsBlockFunctional(string name)
        {
            MyEntity entity;

            if (MyEntities.TryGetEntityByName(name, out entity))
            {
                if (entity is MyFunctionalBlock)
                {
                    return (entity as MyFunctionalBlock).IsFunctional;
                }
            }
            return false;
        }

        [VisualScriptingMiscData("Blocks")]
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

        [VisualScriptingMiscData("Blocks")]
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

        [VisualScriptingMiscData("Blocks")]
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

        [VisualScriptingMiscData("Blocks")]
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

        [VisualScriptingMiscData("Blocks")]
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

        [VisualScriptingMiscData("Blocks")]
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

        [VisualScriptingMiscData("Blocks")]
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

        [VisualScriptingMiscData("Blocks")]
        [VisualScriptingMember(true)]
        public static void CockpitInsertPilot(string cockpitName, bool keepOriginalPlayerPosition = true, long playerId = -1)
        {
            MyEntity entity;
            if(MySession.Static.LocalCharacter == null)
                return;

            if (MyEntities.TryGetEntityByName(cockpitName, out entity))
            {
                MyCockpit cockpit = entity as MyCockpit;
                if (cockpit != null)
                {
                    cockpit.RemovePilot();
                    if (MySession.Static.ControlledEntity is MyCockpit)
                        (MySession.Static.ControlledEntity as MyCockpit).RemovePilot();
                    cockpit.AttachPilot(MySession.Static.LocalCharacter, keepOriginalPlayerPosition);
                }
            }
        }

        #endregion

        #region Cutscenes

        [VisualScriptingMiscData("Cutscenes")]
        [VisualScriptingMember(true)]
        public static void StartCutscene(string cutsceneName)
        {
            MySession.Static.GetComponent<MySessionComponentCutscenes>().PlayCutscene(cutsceneName);
        }

        [VisualScriptingMiscData("Cutscenes")]
        [VisualScriptingMember(true)]
        public static void NextCutsceneNode()
        {
            MySession.Static.GetComponent<MySessionComponentCutscenes>().CutsceneNext(true);
        }

        [VisualScriptingMiscData("Cutscenes")]
        [VisualScriptingMember(true)]
        public static void EndCutscene()
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
        public static void CreateParticleEffectAtPosition(string effectName, Vector3 position)
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

        #endregion

        #region Entity

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
        public static bool ChangeOwner(string entityName, long playerId, bool factionShare = false, bool allShare = false)
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
                    block.ChangeBlockOwnerRequest(playerId, sharingOption);
                    return true;
                }

                var grid = entity as MyCubeGrid;
                if (grid != null)
                {
                    foreach (var fatBlock in grid.GetFatBlocks())
                        fatBlock.ChangeBlockOwnerRequest(playerId, sharingOption);

                    return true;
                }
            }

            return false;
        }
        // Contains per player data in form of playerId => {enitityId, exclusiveLock} ...
        private static readonly Dictionary<long, List<MyTuple<long, int>>> m_playerIdsToHighlightData = new Dictionary<long, List<MyTuple<long, int>>>();
        private static readonly Color DEFAULT_HIGHLIGHT_COLOR = new Color(0, 96, 209, 25);

        [VisualScriptingMiscData("Entity")]
        [VisualScriptingMember(true)]
        public static void SetHighlight(string entityName, bool enabled = true, int thickness = 1, int pulseTimeInFrames = 120, Color color = default(Color), long playerId = -1)
        {
            MyEntity entity;
            if (MyEntities.TryGetEntityByName(entityName, out entity))
            {
                if (color == default(Color))
                {
                    color = DEFAULT_HIGHLIGHT_COLOR;
                }

                if(playerId == -1)
                    playerId = GetLocalPlayerId();

                // highlight for single entity
                var highlightData = new MyHighlightSystem.MyHighlightData
                {
                    EntityId  = entity.EntityId,
                    OutlineColor = color,
                    PulseTimeInFrames = (ulong)pulseTimeInFrames,
                    Thickness = enabled ? thickness : -1,
                    PlayerId = playerId,
                    IgnoreUseObjectData = true
                };

                SetHighlight(highlightData, playerId);
            }
        }


        [VisualScriptingMiscData("Entity")]
        [VisualScriptingMember(true)]
        public static void SetGPSHighlight(string entityName, string GPSName, string GPSDescription, bool enabled = true, int thickness = 1, int pulseTimeInFrames = 120, Color color = default(Color), long playerId = -1)
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
                    MySession.Static.Gpss.SendAddGps(playerId, ref newGPS, entity.EntityId);
                }
                else
                {
                    var gps = MySession.Static.Gpss.GetGpsByName(playerId, GPSName);
                    if (gps != null)
                        MySession.Static.Gpss.SendDelete(playerId, gps.Hash);
                }
                SetHighlight(entityName, enabled: enabled, thickness: thickness, pulseTimeInFrames: pulseTimeInFrames, color: color, playerId: playerId);
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
                    MyEntities.Remove(entity);
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

        #region GPS

        [VisualScriptingMiscData("GPS")]
        [VisualScriptingMember(true)]
        public static void AddGPS(string name, string description, Vector3D position, int disappearsInS = 0)
        {
            var localPlayerID = MySession.Static.LocalPlayerId;
            var newGPS = new MyGps { ShowOnHud = true, Coords = position, Name = name, Description = description, AlwaysVisible = true };
            if (disappearsInS > 0)
            {
                var timeSpan = TimeSpan.FromSeconds(MySession.Static.ElapsedPlayTime.TotalSeconds + disappearsInS);
                newGPS.DiscardAt = timeSpan;
            }
            MySession.Static.Gpss.SendAddGps(localPlayerID, ref newGPS);
        }

        [VisualScriptingMiscData("GPS")]
        [VisualScriptingMember(true)]
        public static void RemoveGPS(string name)
        {
            var localPlayerID = MySession.Static.LocalPlayerId;
            var gps = MySession.Static.Gpss.GetGpsByName(localPlayerID, name);
            if (gps != null)
                MySession.Static.Gpss.SendDelete(localPlayerID, gps.Hash);
        }

        [VisualScriptingMiscData("GPS")]
        [VisualScriptingMember(true)]
        public static void AddGPSToEntity(string entityName, string GPSName, string GPSDescription, long playerId = -1)
        {
            MyEntity entity;
            if (MyEntities.TryGetEntityByName(entityName, out entity))
            {
                if (playerId == -1)
                    playerId = GetLocalPlayerId();
                MyTuple<string, string> gpsIdentifier = new MyTuple<string, string>(entityName, GPSName);

                MyGps newGPS = new MyGps { ShowOnHud = true, Name = GPSName, Description = GPSDescription, AlwaysVisible = true };
                MySession.Static.Gpss.SendAddGps(playerId, ref newGPS, entity.EntityId);
            }
        }

        [VisualScriptingMiscData("GPS")]
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

        #endregion

        #region Grid

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
        [VisualScriptingMember(true)]
        public static void SetGridReactors(string gridName, bool turnOn = true)
        {
            MyCubeGrid grid = GetEntityByName(gridName) as MyCubeGrid;
            Debug.Assert(grid != null, "Grid was not found: " + gridName);
            if (grid != null)
            {
                if (turnOn)
                {
                    grid.SendPowerDistributorState(MyMultipleEnabledEnum.AllEnabled, MySession.Static.LocalPlayerId);
                }
                else
                {
                    grid.SendPowerDistributorState(MyMultipleEnabledEnum.AllDisabled, MySession.Static.LocalPlayerId);
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
        public static void SetGridDestructible(string entityName, bool destructible)
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
        public static void SetGridEditable(string entityName, bool editable)
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
        public static void SendChatMessage(string message, string author = "", long playerId = 0, string font = MyFontEnum.Blue)
        {
            ScriptedChatMsg msg;
            msg.Text = message;
            msg.Author = author;
            msg.Target = playerId;
            msg.Font = font;
            MyMultiplayerBase.SendScriptedChatMessage(ref msg);
        }

        [Event, Reliable, Client]
        private static void ShowNotificationSync(string message, int disappearTimeMs, string font = MyFontEnum.White)
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

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember]
        public static bool IsPlayerInCockpit(string gridName = null, string cockpitName = null)
        {
            var cockpit = MySession.Static.ControlledEntity.Entity as MyCockpit;

            if (cockpit == null) return false;

            if (gridName != null && cockpit.CubeGrid.Name != gridName)
                return false;

            if (cockpitName != null && cockpit.Name != cockpitName)
                return false;

            return true;
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember]
        public static bool IsPlayerInRemote(string gridName = null, string remoteName = null)
        {
            var remote = MySession.Static.ControlledEntity.Entity as MyRemoteControl;

            if (remote == null) return false;

            if (gridName != null && remote.CubeGrid.Name != gridName)
                return false;

            if (remoteName != null && remote.Name != remoteName)
                return false;

            return true;
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember]
        public static long GetLocalPlayerId()
        {
            return MySession.Static.LocalPlayerId;
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember]
        public static string GetPlayersFactionTag(long playerId)
        {
            var faction = MySession.Static.Factions.TryGetPlayerFaction(playerId) as MyFaction;

            Debug.Assert(faction != null, "Faction of player with id " + playerId.ToString() + " was not found.");
            if (faction == null)
                return "";

            return faction.Tag;
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember]
        public static string GetPlayersFactionName(long playerId)
        {
            var faction = MySession.Static.Factions.TryGetPlayerFaction(playerId) as MyFaction;

            Debug.Assert(faction != null, "Faction of player with id " + playerId.ToString() + " was not found.");
            if (faction == null)
                return "";

            return faction.Name;
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember]
        public static string GetPlayersName(long playerId)
        {
            var identity = MySession.Static.Players.TryGetIdentity(playerId);
            if (identity != null)
            {
                return identity.DisplayName;
            }

            return "";
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember]
        public static long GetPirateId()
        {
            return MyPirateAntennas.GetPiratesId();
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember]
        public static float GetPlayersHealth(long playerId)
        {
            var identity = MySession.Static.Players.TryGetIdentity(playerId);
            if (identity != null)
            {
                return identity.Character.StatComp.Health.Value;
            }

            return -1;
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember]
        public static bool IsPlayersJetpackEnabled(long playerId)
        {
            var identity = MySession.Static.Players.TryGetIdentity(playerId);
            if (identity != null && identity.Character != null && identity.Character.JetpackComp != null)
            {
                return identity.Character.JetpackComp.TurnedOn;
            }

            return false;
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember]
        public static float GetPlayersOxygenLevel(long playerId)
        {
            var identity = MySession.Static.Players.TryGetIdentity(playerId);
            if (identity != null)
            {
                return identity.Character.OxygenComponent.SuitOxygenLevel;
            }

            return -1;
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember]
        public static float GetPlayersHydrogenLevel(long playerId)
        {
            var identity = MySession.Static.Players.TryGetIdentity(playerId);
            if (identity != null)
            {
                return identity.Character.OxygenComponent.GetGasFillLevel(MyCharacterOxygenComponent.HydrogenId);
            }

            return -1;
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember]
        public static float GetPlayersEnergyLevel(long playerId)
        {
            var identity = MySession.Static.Players.TryGetIdentity(playerId);
            if (identity != null)
            {
                return identity.Character.SuitEnergyLevel;
            }

            return -1;
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember(true)]
        public static void SetPlayersHealth(long playerId, float value)
        {
            var identity = MySession.Static.Players.TryGetIdentity(playerId);
            if (identity != null)
            {
                identity.Character.StatComp.Health.Value = value;
            }
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember(true)]
        public static void SetPlayersOxygenLevel(long playerId, float value)
        {
            var identity = MySession.Static.Players.TryGetIdentity(playerId);
            if (identity != null)
            {
                identity.Character.OxygenComponent.SuitOxygenLevel = value;
            }
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember(true)]
        public static void SetPlayersHydrogenLevel(long playerId, float value)
        {
            var identity = MySession.Static.Players.TryGetIdentity(playerId);
            if (identity != null)
            {
                var hydrogenId = MyCharacterOxygenComponent.HydrogenId;
                identity.Character.OxygenComponent.UpdateStoredGasLevel(ref hydrogenId, value);
            }
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember(true)]
        public static void SetPlayersEnergyLevel(long playerId, float value)
        {
            var identity = MySession.Static.Players.TryGetIdentity(playerId);
            if (identity != null)
            {
                identity.Character.SuitBattery.ResourceSource
                    .SetRemainingCapacityByType(MyResourceDistributorComponent.ElectricityId, value * MyEnergyConstants.BATTERY_MAX_CAPACITY);
            }
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember(true)]
        public static void SetPlayersPosition(long playerId, Vector3D position)
        {
            var identity = MySession.Static.Players.TryGetIdentity(playerId);
            if (identity != null)
            {
                identity.Character.PositionComp.SetPosition(position);
            }
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember]
        public static Vector3D GetPlayersPosition(long playerId)
        {
            var identity = MySession.Static.Players.TryGetIdentity(playerId);
            if (identity != null)
            {
                return identity.Character.PositionComp.GetPosition();
            }

            return Vector3D.Zero;
        }

        [VisualScriptingMiscData("Players")]
        [VisualScriptingMember]
        public static int GetPlayersInventoryItemAmount(long playerId, MyDefinitionId itemId)
        {
            var identity = MySession.Static.Players.TryGetIdentity(playerId);
            if (identity != null)
            {
                if (!itemId.TypeId.IsNull)
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
        public static void AddToPlayersInventory(long playerId, MyDefinitionId itemId, int amount = 1)
        {
            var identity = MySession.Static.Players.TryGetIdentity(playerId);
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
        public static void RemoveFromPlayersInventory(long playerId, MyDefinitionId itemId, int amount = 1)
        {
            var identity = MySession.Static.Players.TryGetIdentity(playerId);
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

        #endregion

        #region Questlog

        [VisualScriptingMiscData("Questlog")]
        [VisualScriptingMember(true)]
        public static void SetQuestlog(bool visible, string questName)
        {
            MyHud.Questlog.QuestTitle = questName;
            MyHud.Questlog.CleanDetails();
            MyHud.Questlog.Visible = visible;
        }

        [VisualScriptingMiscData("Questlog")]
        [VisualScriptingMember(true)]
        public static void SetQuestlogTitle(string questName)
        {
            MyHud.Questlog.QuestTitle = questName;
        }

        [VisualScriptingMiscData("Questlog")]
        [VisualScriptingMember(true)]
        public static int AddQuestlogDetail(string questDetailRow)
        {
            return MyHud.Questlog.AddDetail(questDetailRow);
        }

        [VisualScriptingMiscData("Questlog")]
        [VisualScriptingMember(true)]
        public static void ReplaceQuestlogDetail(int id, string newDetail)
        {
            MyHud.Questlog.ModifyDetail(id, newDetail);
        }

        [VisualScriptingMiscData("Questlog")]
        [VisualScriptingMember(true)]
        public static void RemoveQuestlogDetails()
        {
            MyHud.Questlog.CleanDetails();
        }

        [VisualScriptingMiscData("Questlog")]
        [VisualScriptingMember(true)]
        public static void SetQuestlogPage(int value)
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
        public static void SetQuestlogVisible(bool value)
        {
            MyHud.Questlog.Visible = value;
        }

        [VisualScriptingMiscData("Questlog")]
        [VisualScriptingMember(false)]
        public static int GetQuestlogPageFromMessage(int id)
        {
            return MyHud.Questlog.GetPageFromMessage(id);
        }

        [VisualScriptingMiscData("Questlog")]
        [VisualScriptingMember(true)]
        public static void EnableHighlight(bool enable)
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
                    updateSync: true);
            }

            if (newGridName != null && tmpGridList.Count > 0)
            {
                tmpGridList[0].Name = newGridName;
                MyEntities.SetEntityName(tmpGridList[0]);
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
            MyEntity entity = MySessionComponentTriggerSystem.Static.GetTriggerEntity(triggerName, out trigger);
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