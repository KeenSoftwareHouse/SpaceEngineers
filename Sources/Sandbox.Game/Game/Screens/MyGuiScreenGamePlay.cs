#region Using

using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.GUI;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.VoiceChat;
using Sandbox.Game.World;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sandbox.Engine.Networking;
using VRage;
using VRage.Audio;
using VRage.Input;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRage.Game.Entity;
using VRage.Data.Audio;
using VRage.Game;
using VRage.Game.ModAPI.Interfaces;
using Sandbox.Game.Audio;
using Sandbox.Game.SessionComponents.Clipboard;
using VRage.Profiler;
using VRageRender.Messages;
using VRageRender.Utils;
using VRage.Library.Utils;

#endregion

namespace Sandbox.Game.Gui
{
    public class MyGuiScreenGamePlay : MyGuiScreenBase
    {
        private bool audioSet = false;
        public static MyGuiScreenGamePlay Static;

        //GK: Used for Double Click Detection when Shooting with Character. Could be added to MyCharacter only also but maybe used in future for ships too.
        private int[] m_lastBeginShootTime;
        public static bool[] DoubleClickDetected { get; private set; }

        public static MyGuiScreenBase ActiveGameplayScreen = null;
        public static MyGuiScreenBase TmpGameplayScreenHolder = null;
        public static bool DisableInput = false;
        IMyControlMenuInitializer m_controlMenu = null;

        #region Properties

        public bool CanSwitchCamera
        {
            get
            {
                if (!MyClipboardComponent.Static.Clipboard.AllowSwitchCameraMode || !MySession.Static.Settings.Enable3rdPersonView)
                    return false;
                MyCameraControllerEnum cameraControllerEnum = MySession.Static.GetCameraControllerEnum();
                bool isValidController = (cameraControllerEnum == MyCameraControllerEnum.Entity || cameraControllerEnum == MyCameraControllerEnum.ThirdPersonSpectator);
                //by Gregory: removed ForceFirstPersonCamera check it is consider a bug by the users
                //return (!MySession.Static.CameraController.ForceFirstPersonCamera && isValidController);
                return (isValidController);
            }
        }

        private static bool SpectatorEnabled
        {
            get
            {
                if (MySession.Static == null)
                    return false;

                if (MySession.Static.CreativeToolsEnabled(Sync.MyId))
                {
                    return true;
                }

                if (!MySession.Static.SurvivalMode)
                    return true;

                if (MyMultiplayer.Static != null && MySession.Static.LocalHumanPlayer != null && MySession.Static.LocalHumanPlayer.IsAdmin)
                    return true;
                if (!MyFinalBuildConstants.IS_OFFICIAL || MyInput.Static.ENABLE_DEVELOPER_KEYS)
                    return true;

                return MySession.Static.Settings.EnableSpectator;
            }
        }

        // Hack to be able to draw cursor for gameplay screen.
        // Still better than creating empty screen for that purpose.
        public bool MouseCursorVisible
        {
            get { return DrawMouseCursor; }
            set { DrawMouseCursor = value; }
        }

        #endregion

        #region Constructor

        public MyGuiScreenGamePlay()
            : base(Vector2.Zero, null, null)
        {
            Static = this;

            DrawMouseCursor = false;
            m_closeOnEsc = false;
            m_drawEvenWithoutFocus = true;
            EnabledBackgroundFade = false;
            m_canShareInput = false;
            CanBeHidden = false;
            m_isAlwaysFirst = true;
            DisableInput = false;

            m_controlMenu = Activator.CreateInstance(MyPerGameSettings.ControlMenuInitializerType) as IMyControlMenuInitializer;

            MyGuiScreenCubeBuilder.ReinitializeBlockScrollbarPosition();

            m_lastBeginShootTime = new int[(int)MyEnum<MyShootActionEnum>.Range.Max + 1];
            DoubleClickDetected = new bool[m_lastBeginShootTime.Length];
        }

        #endregion

        #region Content

        public override string GetFriendlyName()
        {
            return "MyGuiScreenGamePlay";
        }

        public override void LoadData()
        {
            MySandboxGame.Log.WriteLine("MyGuiScreenGamePlay.LoadData - START");
            MySandboxGame.Log.IncreaseIndent();

            base.LoadData();

            MyCharacter.Preload();
            MySandboxGame.Log.DecreaseIndent();
            MySandboxGame.Log.WriteLine("MyGuiScreenGamePlay.LoadData - END");
        }


        //  IMPORTANT: This method will be called in background thread so don't mess with main thread objects!!!
        public override void LoadContent()
        {
            MySandboxGame.Log.WriteLine("MyGuiScreenGamePlay.LoadContent - START");
            MySandboxGame.Log.IncreaseIndent();

            Static = this;
            base.LoadContent();

            MySandboxGame.IsUpdateReady = true;
            MySandboxGame.Log.DecreaseIndent();
            MySandboxGame.Log.WriteLine("MyGuiScreenGamePlay.LoadContent - END");
        }

        public override void UnloadData()
        {
            MySandboxGame.Log.WriteLine("MyGuiScreenGamePlay.UnloadData - START");
            MySandboxGame.Log.IncreaseIndent();

            base.UnloadData();

            MySandboxGame.Log.DecreaseIndent();
            MySandboxGame.Log.WriteLine("MyGuiScreenGamePlay.UnloadData - END");
        }

        //  IMPORTANT: This method will be called in background thread so don't mess with main thread objects!!!
        //  UPDATE: called always when GDevice is disposed
        public override void UnloadContent()
        {
            MySandboxGame.Log.WriteLine("MyGuiScreenGamePlay.UnloadContent - START");
            MySandboxGame.Log.IncreaseIndent();

            base.UnloadContent();

            MyXAudio2 audio = MyAudio.Static as MyXAudio2;
            if (audio != null)
            {
                MyEntity3DSoundEmitter.ClearEntityEmitters();
                audio.ClearSounds();
            }
            MyHud.ScreenEffects.FadeScreen(1, 0);

            //  Do GC collect as last step. Reason is that after we loaded new level, a lot of garbage is created and we want to clear it now and not wait until GC decides so.
            GC.Collect();

            Static = null;

            MySandboxGame.Log.DecreaseIndent();
            MySandboxGame.Log.WriteLine("MyGuiScreenGamePlay.UnloadContent - END");
        }

        #endregion

        #region Input

        public override void HandleInput(bool receivedFocusInThisUpdate)
        {
            bool handled = false;

            if (MyClipboardComponent.Static != null)
            {
                ProfilerShort.Begin("CubeBuilder input");
                handled = MyClipboardComponent.Static.HandleGameInput();
                ProfilerShort.End();
            }

            if (!handled && MyCubeBuilder.Static != null)
            {
                ProfilerShort.Begin("CubeBuilder input");
                handled = MyCubeBuilder.Static.HandleGameInput();
                ProfilerShort.End();
            }

            if (!handled)
            {
                ProfilerShort.Begin("BaseInput");
                base.HandleInput(receivedFocusInThisUpdate);
                ProfilerShort.End();
            }
        }

        public override void InputLost()
        {
            if (MyCubeBuilder.Static != null)
                MyCubeBuilder.Static.InputLost();
        }

        private static void SetAudioVolumes()
        {
            MyAudio.Static.StopMusic();
            MyAudio.Static.ChangeGlobalVolume(1f, 5f);

            if (MyPerGameSettings.UseMusicController && MyFakes.ENABLE_MUSIC_CONTROLLER && MySandboxGame.Config.EnableDynamicMusic && MySandboxGame.IsDedicated == false && MyMusicController.Static == null)
                MyMusicController.Static = new MyMusicController(MyAudio.Static.GetAllMusicCues());

            MyAudio.Static.MusicAllowed = (MyMusicController.Static == null);
            if (MyMusicController.Static != null)
                MyMusicController.Static.Active = true;
            else
                MyAudio.Static.PlayMusic(new MyMusicTrack() { TransitionCategory = MyStringId.GetOrCompute("Default") });
        }

        //  This method is called every update (but only if application has focus)
        public override void HandleUnhandledInput(bool receivedFocusInThisUpdate)
        {
            //  Launch main menu
            var controlledObject = MySession.Static.ControlledEntity;
            var currentCameraController = MySession.Static.CameraController;
            MyStringId context = controlledObject != null ? controlledObject.ControlContext : MySpaceBindingCreator.CX_BASE;

            if (MyInput.Static.IsNewKeyPressed(MyKeys.Escape)
                || MyControllerHelper.IsControl(context, MyControlsGUI.MAIN_MENU, MyControlStateType.NEW_PRESSED))
            {
                MyGuiAudio.PlaySound(MyGuiSounds.HudMouseClick);

                //Allow changing video options from game in DX version
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.MainMenu, MySandboxGame.IsPaused == false));
            }

            if (DisableInput)
            {
                if (MySession.Static.GetComponent<MySessionComponentCutscenes>().IsCutsceneRunning && (MyMultiplayer.Static == null || MyMultiplayer.Static.IsServer))
                {
                    if (MyInput.Static.IsNewKeyPressed(MyKeys.Enter) || MyInput.Static.IsNewKeyPressed(MyKeys.Space))
                        MySession.Static.GetComponent<MySessionComponentCutscenes>().CutsceneSkip();
                }
                MySession.Static.ControlledEntity.MoveAndRotate(Vector3.Zero, Vector2.Zero, 0f);
                return;
            }

            if (MyInput.Static.ENABLE_DEVELOPER_KEYS || (MySession.Static != null && MySession.Static.Settings.EnableSpectator) || (MyMultiplayer.Static != null && MySession.Static.LocalHumanPlayer != null && MySession.Static.CreativeToolsEnabled(MySession.Static.LocalHumanPlayer.Id.SteamId)))
            {
                //Set camera to player
                if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.SPECTATOR_NONE))
                {
                    if (MySession.Static.ControlledEntity != null)
                    { //we already are controlling this object

                        if (MyFinalBuildConstants.IS_OFFICIAL)
                        {
                            SetCameraController();
                        }
                        else
                        {
                            var cameraController = MySession.Static.GetCameraControllerEnum();
                            if (cameraController != MyCameraControllerEnum.Entity && cameraController != MyCameraControllerEnum.ThirdPersonSpectator)
                            {
                                SetCameraController();
                            }
                            else
                            {
                                var entities = MyEntities.GetEntities().ToList();
                                int lastKnownIndex = entities.IndexOf(MySession.Static.ControlledEntity.Entity);

                                var entitiesList = new List<MyEntity>();
                                if (lastKnownIndex + 1 < entities.Count)
                                    entitiesList.AddRange(entities.GetRange(lastKnownIndex + 1, entities.Count - lastKnownIndex - 1));

                                if (lastKnownIndex != -1)
                                {
                                    entitiesList.AddRange(entities.GetRange(0, lastKnownIndex + 1));
                                }

                                MyCharacter newControlledObject = null;

                                for (int i = 0; i < entitiesList.Count; i++)
                                {
                                    var character = entitiesList[i] as MyCharacter;
                                    if (character != null && !character.IsDead && character.ControllerInfo.Controller == null)
                                    {
                                        newControlledObject = character;
                                        break;
                                    }
                                }

                                if (MySession.Static.LocalHumanPlayer != null && newControlledObject != null)
                                {
                                    MySession.Static.LocalHumanPlayer.Controller.TakeControl(newControlledObject);
                                }
                            }

                            // We could have activated the cube builder in spectator, so deactivate it now
                            if (!(MySession.Static.ControlledEntity is MyCharacter))
                                MySession.Static.GameFocusManager.Clear();//MyCubeBuilder.Static.Deactivate();
                        }
                    }
                }

                //Set camera to following third person
                if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.SPECTATOR_DELTA))
                {
                    if (SpectatorEnabled)
                    {
                        MySpectatorCameraController.Static.TurnLightOff();
                        MySession.Static.SetCameraController(MyCameraControllerEnum.SpectatorDelta);
                    }

                    if (MyInput.Static.IsAnyCtrlKeyPressed())
                    {
                        if (MySession.Static.ControlledEntity != null)
                        {
                            MySpectator.Static.Position = MySession.Static.ControlledEntity.Entity.PositionComp.GetPosition() + MySpectator.Static.ThirdPersonCameraDelta;
                            MySpectator.Static.SetTarget(MySession.Static.ControlledEntity.Entity.PositionComp.GetPosition(), MySession.Static.ControlledEntity.Entity.PositionComp.WorldMatrix.Up);
                            MySpectatorCameraController.Static.TrackedEntity = MySession.Static.ControlledEntity.Entity.EntityId;
                        }
                        else
                        {
                            var target = MyCubeGrid.GetTargetEntity();
                            if (target != null)
                            {
                                MySpectator.Static.Position = target.PositionComp.GetPosition() + MySpectator.Static.ThirdPersonCameraDelta;
                                MySpectator.Static.SetTarget(target.PositionComp.GetPosition(), target.PositionComp.WorldMatrix.Up);
                                MySpectatorCameraController.Static.TrackedEntity = target.EntityId;
                            }
                        }
                    }
                }

                //Set camera to spectator
                if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.SPECTATOR_FREE))
                {
                    if (SpectatorEnabled)
                    {
                        if (MyInput.Static.IsAnyShiftKeyPressed())
                        {
                            MySession.Static.SetCameraController(MyCameraControllerEnum.SpectatorOrbit);
                            MySpectatorCameraController.Static.Reset();
                        }
                        else
                        {
                            MySession.Static.SetCameraController(MyCameraControllerEnum.Spectator);
                        }

                        if (MyInput.Static.IsAnyCtrlKeyPressed() && MySession.Static.ControlledEntity != null)
                        {
                            MySpectator.Static.Position = MySession.Static.ControlledEntity.Entity.PositionComp.GetPosition() + MySpectator.Static.ThirdPersonCameraDelta;
                            MySpectator.Static.SetTarget(MySession.Static.ControlledEntity.Entity.PositionComp.GetPosition(), MySession.Static.ControlledEntity.Entity.PositionComp.WorldMatrix.Up);
                        }
                    }
                }

                //Set camera to static spectator, non movable
                if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.SPECTATOR_STATIC))
                {
                    if (MySession.Static.ControlledEntity != null)
                    {
                        MySpectatorCameraController.Static.TurnLightOff();
                        MySession.Static.SetCameraController(MyCameraControllerEnum.SpectatorFixed);

                        if (MyInput.Static.IsAnyCtrlKeyPressed())
                        {
                            MySpectator.Static.Position = MySession.Static.ControlledEntity.Entity.PositionComp.GetPosition() + MySpectator.Static.ThirdPersonCameraDelta;
                            MySpectator.Static.SetTarget(MySession.Static.ControlledEntity.Entity.PositionComp.GetPosition(), MySession.Static.ControlledEntity.Entity.PositionComp.WorldMatrix.Up);
                        }
                    }
                }

                if (MySession.Static != null && MySession.Static.CameraController == MySpectator.Static && (MySession.Static.CreativeMode || MySession.Static.CreativeToolsEnabled(Sync.MyId)) && MyInput.Static.IsNewKeyPressed(MyKeys.Space) && MyInput.Static.IsAnyCtrlKeyPressed())
                {
                    MyMultiplayer.TeleportControlledEntity(MySpectator.Static.Position);
                }

                //Open console
                if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.CONSOLE) && MyInput.Static.IsAnyAltKeyPressed())
                {
                    MyGuiScreenConsole.Show();
                }
            }

            if (MyDefinitionErrors.ShouldShowModErrors)
            {
                MyDefinitionErrors.ShouldShowModErrors = false;
                MyGuiSandbox.ShowModErrors();
            }

            // Switch view - cockpit on/off, third person
            if ((MyInput.Static.IsNewGameControlPressed(MyControlsSpace.CAMERA_MODE)
                || MyControllerHelper.IsControl(MyControllerHelper.CX_CHARACTER, MyControlsSpace.CAMERA_MODE))
                && CanSwitchCamera)
            {
                MyGuiAudio.PlaySound(MyGuiSounds.HudClick);
                SwitchCamera();
            }

            if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.HELP_SCREEN))
            {
                if (MyInput.Static.IsAnyCtrlKeyPressed())
                {
                    switch (MySandboxGame.Config.DebugComponentsInfo)
                    {
                        case MyDebugComponent.MyDebugComponentInfoState.NoInfo:
                            MySandboxGame.Config.DebugComponentsInfo = MyDebugComponent.MyDebugComponentInfoState.EnabledInfo;
                            break;
                        case MyDebugComponent.MyDebugComponentInfoState.EnabledInfo:
                            MySandboxGame.Config.DebugComponentsInfo = MyDebugComponent.MyDebugComponentInfoState.FullInfo;
                            break;
                        case MyDebugComponent.MyDebugComponentInfoState.FullInfo:
                            MySandboxGame.Config.DebugComponentsInfo = MyDebugComponent.MyDebugComponentInfoState.NoInfo;
                            break;
                    }

                    MySandboxGame.Config.Save();
                }
                else if (MyInput.Static.IsAnyShiftKeyPressed() && MyPerGameSettings.GUI.PerformanceWarningScreen != null)
                {
                    MyGuiAudio.PlaySound(MyGuiSounds.HudMouseClick);
                    MyGuiSandbox.AddScreen(MyGuiScreenGamePlay.ActiveGameplayScreen = MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.PerformanceWarningScreen));
                }
                else
                    if (MyGuiScreenGamePlay.ActiveGameplayScreen == null)
                    {
                        MyGuiAudio.PlaySound(MyGuiSounds.HudMouseClick);
                        MyGuiSandbox.AddScreen(MyGuiScreenGamePlay.ActiveGameplayScreen = MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.HelpScreen));
                    }
            }

            if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.TOGGLE_HUD))
            {
                MyGuiAudio.PlaySound(MyGuiSounds.HudClick);
                MyHud.MinimalHud = !MyHud.MinimalHud;
            }

            if (MyPerGameSettings.SimplePlayerNames && MyInput.Static.IsNewGameControlPressed(MyControlsSpace.BROADCASTING))
            {
                MyHud.LocationMarkers.Visible = !MyHud.LocationMarkers.Visible;
            }

            if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.MISSION_SETTINGS) && MyGuiScreenGamePlay.ActiveGameplayScreen == null
                && MyPerGameSettings.Game == Sandbox.Game.GameEnum.SE_GAME
                && MyFakes.ENABLE_MISSION_TRIGGERS)
            {
                if (MySession.Static.Settings.ScenarioEditMode)
                    MyGuiSandbox.AddScreen(new Sandbox.Game.Screens.MyGuiScreenMissionTriggers());
                else
                    if (MySession.Static.IsScenario)
                        MyGuiSandbox.AddScreen(new Sandbox.Game.Screens.MyGuiScreenBriefing());
            }

            bool handledByUseObject = false;
            if (MySession.Static.ControlledEntity is VRage.Game.Entity.UseObject.IMyUseObject)
            {
                handledByUseObject = (MySession.Static.ControlledEntity as VRage.Game.Entity.UseObject.IMyUseObject).HandleInput();
            }

            if (controlledObject != null && !handledByUseObject)
            {
                if (!MySandboxGame.IsPaused)
                {
                    if (MyFakes.ENABLE_NON_PUBLIC_GUI_ELEMENTS && MyInput.Static.IsNewKeyPressed(MyKeys.F2))
                    {
                        if (MyInput.Static.IsAnyShiftKeyPressed() && !MyInput.Static.IsAnyCtrlKeyPressed() && !MyInput.Static.IsAnyAltKeyPressed())
                        {
                            if (MySession.Static.Settings.GameMode == VRage.Library.Utils.MyGameModeEnum.Creative)
                                MySession.Static.Settings.GameMode = VRage.Library.Utils.MyGameModeEnum.Survival;
                            else
                                MySession.Static.Settings.GameMode = VRage.Library.Utils.MyGameModeEnum.Creative;
                        }
                    }

                    if (context == MySpaceBindingCreator.CX_BUILD_MODE || context == MySpaceBindingCreator.CX_CHARACTER || context == MySpaceBindingCreator.CX_SPACESHIP)
                    {
                        if (MyControllerHelper.IsControl(context, MyControlsSpace.PRIMARY_TOOL_ACTION, MyControlStateType.NEW_PRESSED))
                        {
                            if (MyToolbarComponent.CurrentToolbar.ShouldActivateSlot)
                            {
                                MyToolbarComponent.CurrentToolbar.ActivateStagedSelectedItem();
                            }
                            else
                            {
                                if (context == MySpaceBindingCreator.CX_CHARACTER)  //GK: Handle Double Click for MyCharacter only (for now)
                                {
                                    if (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastBeginShootTime[(int)MyShootActionEnum.PrimaryAction] < MyGuiConstants.DOUBLE_CLICK_DELAY)
                                    {
                                        DoubleClickDetected[(int)MyShootActionEnum.PrimaryAction] = true;
                                    }
                                    else
                                    {
                                        DoubleClickDetected[(int)MyShootActionEnum.PrimaryAction] = false;
                                        m_lastBeginShootTime[(int)MyShootActionEnum.PrimaryAction] = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                                    }
                                }
                                controlledObject.BeginShoot(MyShootActionEnum.PrimaryAction);
                            }
                        }

                        if (MyControllerHelper.IsControl(context, MyControlsSpace.PRIMARY_TOOL_ACTION, MyControlStateType.NEW_RELEASED))
                        {
                            if (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastBeginShootTime[(int)MyShootActionEnum.PrimaryAction] > MyGuiConstants.DOUBLE_CLICK_DELAY)
                            {
                                DoubleClickDetected[(int)MyShootActionEnum.PrimaryAction] = false;
                            }
                            controlledObject.EndShoot(MyShootActionEnum.PrimaryAction);
                            DoubleClickDetected[(int)MyShootActionEnum.PrimaryAction] = false;
                        }
                        if (MyControllerHelper.IsControl(context, MyControlsSpace.SECONDARY_TOOL_ACTION, MyControlStateType.NEW_PRESSED))
                        {
                            if (context == MySpaceBindingCreator.CX_CHARACTER)  //GK: Handle Double Click for MyCharacter only (for now)
                            {
                                if (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastBeginShootTime[(int)MyShootActionEnum.SecondaryAction] < MyGuiConstants.DOUBLE_CLICK_DELAY)
                                {
                                    DoubleClickDetected[(int)MyShootActionEnum.SecondaryAction] = true;
                                }
                                else
                                {
                                    DoubleClickDetected[(int)MyShootActionEnum.SecondaryAction] = false;
                                    m_lastBeginShootTime[(int)MyShootActionEnum.SecondaryAction] = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                                }
                            }
                            controlledObject.BeginShoot(MyShootActionEnum.SecondaryAction);
                        }
                        if (MyControllerHelper.IsControl(context, MyControlsSpace.SECONDARY_TOOL_ACTION, MyControlStateType.NEW_RELEASED))
                        {
                            if (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastBeginShootTime[(int)MyShootActionEnum.SecondaryAction] > MyGuiConstants.DOUBLE_CLICK_DELAY)
                            {
                                DoubleClickDetected[(int)MyShootActionEnum.SecondaryAction] = false;
                            }
                            controlledObject.EndShoot(MyShootActionEnum.SecondaryAction);
                            DoubleClickDetected[(int)MyShootActionEnum.SecondaryAction] = false;
                        }
                    }

                    if (context == MySpaceBindingCreator.CX_CHARACTER || context == MySpaceBindingCreator.CX_SPACESHIP)
                    {
                        if (MyControllerHelper.IsControl(context, MyControlsSpace.USE, MyControlStateType.NEW_PRESSED))
                        {
                            // Key press
                            if (currentCameraController != null)
                            {
                                if (!currentCameraController.HandleUse())
                                {
                                    controlledObject.Use();
                                }
                            }
                            else
                            {
                                controlledObject.Use();
                            }
                        }
                        else if (MyControllerHelper.IsControl(context, MyControlsSpace.USE, MyControlStateType.PRESSED))
                        {
                            // Key not pressed this frame, holding from previous
                            controlledObject.UseContinues();
                        }
                        else if (MyControllerHelper.IsControl(context, MyControlsSpace.USE, MyControlStateType.NEW_RELEASED))
                        {
                            controlledObject.UseFinished();
                        }

                        if (MyControllerHelper.IsControl(context, MyControlsSpace.PICK_UP, MyControlStateType.NEW_PRESSED))
                        {
                            // Key press
                            if (currentCameraController != null)
                            {
                                if (!currentCameraController.HandlePickUp())
                                    controlledObject.PickUp();
                            }
                            else
                            {
                                controlledObject.PickUp();
                            }
                        }
                        else if (MyControllerHelper.IsControl(context, MyControlsSpace.PICK_UP, MyControlStateType.PRESSED))
                        {
                            controlledObject.PickUpContinues();
                        }
                        else if (MyControllerHelper.IsControl(context, MyControlsSpace.PICK_UP, MyControlStateType.NEW_RELEASED))
                        {
                            controlledObject.PickUpFinished();
                        }

                        //Temp fix until spectators are implemented as entities
                        //Prevents controlled object from getting input while spectator mode is enabled
                        if (!MySession.Static.IsCameraUserControlledSpectator())
                        {
                            if (MyControllerHelper.IsControl(context, MyControlsSpace.CROUCH, MyControlStateType.NEW_PRESSED))
                            {
                                controlledObject.Crouch();
                            }
                            if (MyControllerHelper.IsControl(context, MyControlsSpace.CROUCH, MyControlStateType.PRESSED))
                            {
                                controlledObject.Down();
                            }

                            // MZ: fixed issue that sometimes character was sprinting even without holding the control
                            controlledObject.Sprint(MyControllerHelper.IsControl(context, MyControlsSpace.SPRINT, MyControlStateType.PRESSED));
                            
                            if (MyControllerHelper.IsControl(context, MyControlsSpace.JUMP, MyControlStateType.NEW_PRESSED))
                            {
                                controlledObject.Jump();
                            }
                            if (MyControllerHelper.IsControl(context, MyControlsSpace.JUMP, MyControlStateType.PRESSED))
                            {
                                controlledObject.Up();
                            }
                            if (MyControllerHelper.IsControl(context, MyControlsSpace.SWITCH_WALK, MyControlStateType.NEW_PRESSED))
                            {
                                controlledObject.SwitchWalk();
                            }
                            if (MyControllerHelper.IsControl(context, MyControlsSpace.BROADCASTING, MyControlStateType.NEW_PRESSED))
                            {
                                controlledObject.SwitchBroadcasting();
                            }
                            if (MyControllerHelper.IsControl(context, MyControlsSpace.HELMET, MyControlStateType.NEW_PRESSED))
                            {
                                controlledObject.SwitchHelmet();
                            }
                        }

                        if (MyControllerHelper.IsControl(context, MyControlsSpace.DAMPING, MyControlStateType.NEW_PRESSED))
                        {
                            MyGuiAudio.PlaySound(MyGuiSounds.HudClick);
                            controlledObject.SwitchDamping();
                        }
                        if (MyControllerHelper.IsControl(context, MyControlsSpace.THRUSTS, MyControlStateType.NEW_PRESSED))
                        {
                            if (controlledObject is MyCharacter == false)
                                MyGuiAudio.PlaySound(MyGuiSounds.HudClick);
                            controlledObject.SwitchThrusts();
                        }
                        if (MyControllerHelper.IsControl(context, MyControlsSpace.HEADLIGHTS, MyControlStateType.NEW_PRESSED))
                        {
                            //Switch lights only on Spectator Mode
                            if (MySession.Static.IsCameraUserControlledSpectator())
                            {
                                MySpectatorCameraController.Static.SwitchLight();
                            }
                            else
                            {
                                MyGuiAudio.PlaySound(MyGuiSounds.HudClick);
                                controlledObject.SwitchLights();
                            }
                        }
                        if (MyControllerHelper.IsControl(context, MyControlsSpace.TOGGLE_REACTORS, MyControlStateType.NEW_PRESSED))
                        {
                            MyGuiAudio.PlaySound(MyGuiSounds.HudClick);
                            controlledObject.SwitchReactors();
                        }
                        if (MyControllerHelper.IsControl(context, MyControlsSpace.LANDING_GEAR, MyControlStateType.NEW_PRESSED))
                        {
                            controlledObject.SwitchLeadingGears();
                        }
                        if (MyControllerHelper.IsControl(context, MyControlsSpace.SUICIDE, MyControlStateType.NEW_PRESSED))
                        {
                            controlledObject.Die();
                        }
                        if ((controlledObject as MyCockpit) != null && MyControllerHelper.IsControl(context, MyControlsSpace.CUBE_COLOR_CHANGE, MyControlStateType.NEW_PRESSED))
                        {
                            (controlledObject as MyCockpit).SwitchWeaponMode();
                        }
                    }
                }
                else
                {
                    controlledObject.EndShoot(MyShootActionEnum.PrimaryAction);
                    controlledObject.EndShoot(MyShootActionEnum.SecondaryAction);
                }

                if (MySandboxGame.IsPaused == false)
                {
                    if (MyControllerHelper.IsControl(context, MyControlsSpace.TERMINAL, MyControlStateType.NEW_PRESSED) && MyGuiScreenGamePlay.ActiveGameplayScreen == null)
                    {
                        MyGuiAudio.PlaySound(MyGuiSounds.HudClick);
                        controlledObject.ShowTerminal();
                    }

                    if (MyControllerHelper.IsControl(context, MyControlsSpace.INVENTORY, MyControlStateType.NEW_PRESSED) && MyGuiScreenGamePlay.ActiveGameplayScreen == null)
                    {
                        MyGuiAudio.PlaySound(MyGuiSounds.HudClick);
                        controlledObject.ShowInventory();
                    }

                    if (MyControllerHelper.IsControl(context, MyControlsSpace.CONTROL_MENU, MyControlStateType.NEW_PRESSED) && MyGuiScreenGamePlay.ActiveGameplayScreen == null)
                    {
                        MyGuiAudio.PlaySound(MyGuiSounds.HudClick);
                        m_controlMenu.OpenControlMenu(controlledObject);
                    }
                }
            }
            if (!VRage.Profiler.MyRenderProfiler.ProfilerVisible && MyControllerHelper.IsControl(context, MyControlsSpace.CHAT_SCREEN, MyControlStateType.NEW_PRESSED))
            {
                if (MyGuiScreenChat.Static == null)
                {
                    Vector2 chatPos = new Vector2(0.025f, 0.84f);
                    chatPos = MyGuiScreenHudBase.ConvertHudToNormalizedGuiPosition(ref chatPos);
                    MyGuiScreenChat chatScreen = new MyGuiScreenChat(chatPos);
                    MyGuiSandbox.AddScreen(chatScreen);
                }
            }

            if (MyPerGameSettings.VoiceChatEnabled && MyVoiceChatSessionComponent.Static != null)
            {
                if (MyControllerHelper.IsControl(context, MyControlsSpace.VOICE_CHAT, MyControlStateType.NEW_PRESSED))
                {
                    MyVoiceChatSessionComponent.Static.StartRecording();
                }
                else if (MyVoiceChatSessionComponent.Static.IsRecording && !MyControllerHelper.IsControl(context, MyControlsSpace.VOICE_CHAT, MyControlStateType.PRESSED))
                {
                    MyVoiceChatSessionComponent.Static.StopRecording();
                }
            }

             
            MoveAndRotatePlayerOrCamera();

            // Quick save or quick load.
            if (MyInput.Static.IsNewKeyPressed(MyKeys.F5))
            {
                if (!MySession.Static.IsScenario)
                {
                    MyGuiAudio.PlaySound(MyGuiSounds.HudMouseClick);
                    var currentSession = MySession.Static.CurrentPath;

                    if (MyInput.Static.IsAnyShiftKeyPressed())
                    {
                        if (Sync.IsServer)
                        {
                            if (!MyAsyncSaving.InProgress)
                            {
                                var messageBox = MyGuiSandbox.CreateMessageBox(
                                    buttonType: MyMessageBoxButtonsType.YES_NO,
                                    messageText: MyTexts.Get(MyCommonTexts.MessageBoxTextAreYouSureYouWantToQuickSave),
                                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionPleaseConfirm),
                                    callback: delegate(MyGuiScreenMessageBox.ResultEnum callbackReturn)
                                    {
                                        if (callbackReturn == MyGuiScreenMessageBox.ResultEnum.YES)
                                            MyAsyncSaving.Start(() => MySector.ResetEyeAdaptation = true);//black screen after save
                                    });
                                messageBox.SkipTransition = true;
                                messageBox.CloseBeforeCallback = true;
                                MyGuiSandbox.AddScreen(messageBox);
                            }
                        }
                        else
                            MyHud.Notifications.Add(MyNotificationSingletons.ClientCannotSave);
                    }
                    else if (Sync.IsServer)
                    {
                        ShowLoadMessageBox(currentSession);
                    }
                    else
                    {
                        // Is multiplayer client, reconnect
                        ShowReconnectMessageBox();
                    }
                }
            }

            if (MyInput.Static.IsNewKeyPressed(MyKeys.F3))
            {
                if (Sync.MultiplayerActive)
                {
                    MyGuiAudio.PlaySound(MyGuiSounds.HudMouseClick);
                    MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.PlayersScreen));
                }
                else
                    MyHud.Notifications.Add(MyNotificationSingletons.MultiplayerDisabled);
            }

            if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.FACTIONS_MENU) && !MyInput.Static.IsAnyCtrlKeyPressed())
            {
                //if (MyToolbarComponent.CurrentToolbar.SelectedItem == null || (MyToolbarComponent.CurrentToolbar.SelectedItem != null && MyToolbarComponent.CurrentToolbar.SelectedItem.GetType() != typeof(MyToolbarItemVoxelHand)))
                //{
                MyGuiAudio.PlaySound(MyGuiSounds.HudMouseClick);
                var screen = MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.FactionScreen);
                MyScreenManager.AddScreenNow(screen);
                //}
            }

            // Check if any of windows keys is not pressed.
            bool windowKeyPressed = MyInput.Static.IsKeyPress(MyKeys.LeftWindows) || MyInput.Static.IsKeyPress(MyKeys.RightWindows);

            if (!windowKeyPressed && MyInput.Static.IsNewGameControlPressed(MyControlsSpace.BUILD_SCREEN) && !MyInput.Static.IsAnyCtrlKeyPressed() && MyGuiScreenGamePlay.ActiveGameplayScreen == null)
            {
                if (MyPerGameSettings.GUI.EnableToolbarConfigScreen && MyGuiScreenCubeBuilder.Static == null && (MySession.Static.ControlledEntity is MyShipController || MySession.Static.ControlledEntity is MyCharacter))
                {
                    int offset = 0;
                    if (MyInput.Static.IsAnyShiftKeyPressed()) offset += 6;
                    if (MyInput.Static.IsAnyCtrlKeyPressed()) offset += 12;
                    MyGuiAudio.PlaySound(MyGuiSounds.HudMouseClick);
                    MyGuiSandbox.AddScreen(
                        MyGuiScreenGamePlay.ActiveGameplayScreen = MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.ToolbarConfigScreen,
                                                                                             offset,
                                                                                             MySession.Static.ControlledEntity as MyShipController)
                    );
                }
            }

            if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.PAUSE_GAME) && Sync.Clients.Count < 2)
            {
                MySandboxGame.PauseToggle();
            }

            if (MySession.Static != null)
            {
                if (MyInput.Static.IsNewKeyPressed(MyKeys.F10))
                {
                    if (MyInput.Static.IsAnyAltKeyPressed())
                    {
                        // ALT + F10
                        if (MySession.Static.IsAdminMenuEnabled && MyPerGameSettings.Game != GameEnum.UNKNOWN_GAME)
                            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.AdminMenuScreen));
                        else
                            MyHud.Notifications.Add(MyNotificationSingletons.AdminMenuNotAvailable);
                    }
                    else if (MyPerGameSettings.GUI.VoxelMapEditingScreen != null && (MySession.Static.CreativeToolsEnabled(Sync.MyId) || MySession.Static.CreativeMode) && MyInput.Static.IsAnyShiftKeyPressed())
                    {
                        // Shift + F10
                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.VoxelMapEditingScreen));
                    }
                    else
                    {
                        // F10
                        MyGuiSandbox.AddScreen(new MyGuiBlueprintScreen(MyClipboardComponent.Static.Clipboard, MySession.Static.CreativeMode || MySession.Static.CreativeToolsEnabled(Sync.MyId)));
                    }
                }
            }

            // F11, mod debug
            if (MyInput.Static.IsNewKeyPressed(MyKeys.F11) && !MyInput.Static.IsAnyShiftKeyPressed() && !MyInput.Static.IsAnyCtrlKeyPressed())
            {
                MyDX9Gui.SwitchModDebugScreen();
            }
        }

        //Game and editor shares this method
        public void MoveAndRotatePlayerOrCamera()
        {
            MyCameraControllerEnum cce = MySession.Static.GetCameraControllerEnum();
            bool movementAllowedInPause = cce == MyCameraControllerEnum.Spectator;
            bool rotationAllowedInPause = movementAllowedInPause ||
                                          (cce == MyCameraControllerEnum.ThirdPersonSpectator && MyInput.Static.IsAnyAltKeyPressed());
            bool devScreenFlag = MyScreenManager.GetScreenWithFocus() is MyGuiScreenDebugBase && !MyInput.Static.IsAnyAltKeyPressed();

            bool allowRoll = !MySessionComponentVoxelHand.Static.BuildMode;
            bool allowMove = !MySessionComponentVoxelHand.Static.BuildMode && !MyCubeBuilder.Static.IsBuildMode;
            float rollIndicator = allowRoll ? MyInput.Static.GetRoll() : 0;
            Vector2 rotationIndicator = MyInput.Static.GetRotation();
            VRageMath.Vector3 moveIndicator = allowMove ? MyInput.Static.GetPositionDelta() : Vector3.Zero;

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////  Decide who is moving
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////

            //First move control objects
            if (MySession.Static.ControlledEntity != null)// && MySession.Static.ControlledObject != MySession.Static.CameraController)
            {
                if (MySandboxGame.IsPaused)
                {

                    if (!movementAllowedInPause && !rotationAllowedInPause)
                    {
                        return;
                    }

                    if (!rotationAllowedInPause || devScreenFlag)
                    {
                        rotationIndicator = Vector2.Zero;
                    }
                    rollIndicator = 0.0f;
                }
                if (MySession.Static.IsCameraUserControlledSpectator())
                {
                    MySpectatorCameraController.Static.MoveAndRotate(moveIndicator, rotationIndicator, rollIndicator);
                }
                else
                {
                    if (!MySession.Static.CameraController.IsInFirstPersonView)
                        MyThirdPersonSpectator.Static.UpdateZoom();

                    if (!MyInput.Static.IsGameControlPressed(MyControlsSpace.LOOKAROUND))
                    {
                        MySession.Static.ControlledEntity.MoveAndRotate(moveIndicator, rotationIndicator, rollIndicator);
                    }
                    else
                    {
                        // Stop the controlled entity from rolling when the character tries to in freelook mode
                        if (MySession.Static.ControlledEntity is MyRemoteControl)
                        {
                            rotationIndicator = Vector2.Zero;
                            rollIndicator = 0f;
                        }
                        else if (MySession.Static.ControlledEntity is MyCockpit || !MySession.Static.CameraController.IsInFirstPersonView)
                        {
                            rotationIndicator = Vector2.Zero;
                        }

                        MySession.Static.ControlledEntity.MoveAndRotate(moveIndicator, rotationIndicator, rollIndicator);
                        if (!MySession.Static.CameraController.IsInFirstPersonView)
                            MyThirdPersonSpectator.Static.SaveSettings();
                    }
                }
            }
            else
                MySpectatorCameraController.Static.MoveAndRotate(moveIndicator, rotationIndicator, rollIndicator);
        }

        public static void SetCameraController()
        {
            if (MySession.Static.ControlledEntity != null)
            {
                var remote = MySession.Static.ControlledEntity.Entity as MyRemoteControl;
                if (remote != null)
                {
                    if (remote.PreviousControlledEntity is IMyCameraController)
                    {
                        MySession.Static.SetCameraController(MyCameraControllerEnum.Entity, remote.PreviousControlledEntity.Entity);
                    }
                }
                else
                {
                    MySession.Static.SetCameraController(MyCameraControllerEnum.Entity, MySession.Static.ControlledEntity.Entity);
                }
            }
        }
        #endregion

        #region Methods

        public void SwitchCamera()
        {
            if (MySession.Static.CameraController == null)
                return;

            MySession.Static.CameraController.IsInFirstPersonView = !MySession.Static.CameraController.IsInFirstPersonView;

            if (MySession.Static.GetCameraControllerEnum() == MyCameraControllerEnum.ThirdPersonSpectator)
            {
                MyEntityCameraSettings settings = null;
                if (MySession.Static.LocalHumanPlayer != null && MySession.Static.ControlledEntity != null)
                {
                    if (MySession.Static.Cameras.TryGetCameraSettings(MySession.Static.LocalHumanPlayer.Id, MySession.Static.ControlledEntity.Entity.EntityId, out settings))
                        MyThirdPersonSpectator.Static.ResetViewerDistance(settings.Distance);
                    else
                        MyThirdPersonSpectator.Static.RecalibrateCameraPosition();
                }
            }

            MySession.Static.SaveControlledEntityCameraSettings(MySession.Static.CameraController.IsInFirstPersonView);
        }

        public void ShowReconnectMessageBox()
        {
            var messageBox = MyGuiSandbox.CreateMessageBox(
                   buttonType: MyMessageBoxButtonsType.YES_NO,
                   messageText: MyTexts.Get(MyCommonTexts.MessageBoxTextAreYouSureYouWantToReconnect),
                   messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionPleaseConfirm),
                   callback: delegate(MyGuiScreenMessageBox.ResultEnum callbackReturn)
                   {
                       if (callbackReturn == MyGuiScreenMessageBox.ResultEnum.YES)
                       {
                           if (MyMultiplayer.Static is MyMultiplayerLobbyClient)
                           {
                               var lobbyId = MyMultiplayer.Static.LobbyId;
                               MySessionLoader.UnloadAndExitToMenu();
                               MyJoinGameHelper.JoinGame(lobbyId);
                           }
                           else if (MyMultiplayer.Static is MyMultiplayerClient)
                           {
                               var server = (MyMultiplayer.Static as MyMultiplayerClient).Server;
                               MySessionLoader.UnloadAndExitToMenu();
                               MyJoinGameHelper.JoinGame(server);
                           }
                           else
                           {
                               Debug.Fail("Unknown multiplayer kind");
                           }
                       }
                   });
            messageBox.SkipTransition = true;
            messageBox.CloseBeforeCallback = true;
            MyGuiSandbox.AddScreen(messageBox);
        }

        public void ShowLoadMessageBox(string currentSession)
        {
            var messageBox = MyGuiSandbox.CreateMessageBox(
                                   buttonType: MyMessageBoxButtonsType.YES_NO,
                                   messageText: MyTexts.Get(MyCommonTexts.MessageBoxTextAreYouSureYouWantToQuickLoad),
                                   messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionPleaseConfirm),
                                   callback: delegate(MyGuiScreenMessageBox.ResultEnum callbackReturn)
                                   {
                                       if (callbackReturn == MyGuiScreenMessageBox.ResultEnum.YES)
                                           MySessionLoader.LoadSingleplayerSession(currentSession);
                                   });
            messageBox.SkipTransition = true;
            messageBox.CloseBeforeCallback = true;
            MyGuiSandbox.AddScreen(messageBox);
        }

        #endregion

        #region Update

        public override bool Update(bool hasFocus)
        {
            //MySandboxGame.IsGameReady = true;

            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("GuiScreenGamePlay::Update");


            //Needs to be in update, because several calculation are dependedt on camera position
            // MZ: position no longer needs to be updated
            //MySector.MainCamera.SetViewMatrix(MySector.MainCamera.ViewMatrix);

            base.Update(hasFocus);
            if (audioSet == false && MySandboxGame.IsGameReady && MyAudio.Static != null && MyRenderProxy.VisibleObjectsRead != null && MyRenderProxy.VisibleObjectsRead.Count > 0)
            {
                SetAudioVolumes();
                audioSet = true;
                MyVisualScriptLogicProvider.GameIsReady = true;
                MyHud.MinimalHud = false;
            }

            if (MySession.Static.IsCameraUserControlledSpectator())
                MySpectator.Static.Update();

            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            return true;
        }

        #endregion

        #region Draw

        public override bool Draw()
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyGuiScreenGamePlay::Draw");

            if (MyThirdPersonSpectator.Static != null)
                MyThirdPersonSpectator.Static.Update();
            if (MySector.MainCamera != null)
            {
                // set new camera values
                MySession.Static.CameraController.ControlCamera(MySector.MainCamera);
                // update camera properties accordingly to the new settings - zoom, spring, shaking...
                MySector.MainCamera.Update(MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS);
                // upload to renderer
                MySector.MainCamera.UploadViewMatrixToRender();
            }

            MySector.UpdateSunLight();

            MyRenderProxy.UpdateGameplayFrame(MySession.Static.GameplayFrameCounter);

            MyRenderFogSettings fogSettings = new MyRenderFogSettings()
            {
                FogMultiplier = MySector.FogProperties.FogMultiplier,
                FogColor = MySector.FogProperties.FogColor,
                FogDensity = MySector.FogProperties.FogDensity / 100.0f
            };
            VRageRender.MyRenderProxy.UpdateFogSettings(ref fogSettings);

            MyRenderProxy.UpdateSSAOSettings(ref MySector.SSAOSettings);
            MyRenderProxy.UpdateHBAOSettings(ref MySector.HBAOSettings);

            var envData = MySector.SunProperties.EnvironmentData;
            envData.Skybox = !string.IsNullOrEmpty(MySession.Static.CustomSkybox) ? MySession.Static.CustomSkybox : MySector.EnvironmentDefinition.EnvironmentTexture;
            envData.SkyboxOrientation = MySector.EnvironmentDefinition.EnvironmentOrientation.ToQuaternion();
            envData.EnvironmentLight.SunLightDirection = -MySector.SunProperties.SunDirectionNormalized;
            MyEnvironmentLightData.CalculateBackLightDirections(envData.EnvironmentLight.SunLightDirection, MySector.SunRotationAxis,
                out envData.EnvironmentLight.BackLightDirection1, out envData.EnvironmentLight.BackLightDirection2);

            envData.SunBillboardEnabled = MyFakes.ENABLE_SUN_BILLBOARD;

            VRageRender.MyRenderProxy.UpdateRenderEnvironment(ref envData, MySector.ResetEyeAdaptation);

            MySector.ResetEyeAdaptation = false;
            VRageRender.MyRenderProxy.UpdateEnvironmentMap();

            MyRenderProxy.SwitchPostprocessSettings(ref MyPostprocessSettingsWrapper.Settings);

            if (MyRenderProxy.SettingsDirty)
                MyRenderProxy.SwitchRenderSettings(MyRenderProxy.Settings);

            VRageRender.MyRenderProxy.GetRenderProfiler().StartNextBlock("Main render");

            VRageRender.MyRenderProxy.Draw3DScene();

            using (Stats.Generic.Measure("GamePrepareDraw"))
            {
                if (MySession.Static != null)
                    MySession.Static.Draw();
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().StartNextBlock("Draw HUD");

            if (MySession.Static.ControlledEntity != null && MySession.Static.CameraController != null)
                MySession.Static.ControlledEntity.DrawHud(MySession.Static.CameraController, MySession.Static.LocalPlayerId);

            if (MySandboxGame.IsPaused && !MyHud.MinimalHud)
                DrawPauseIndicator();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            return true;
        }

        private void DrawPauseIndicator()
        {
            var fullscreenRect = MyGuiManager.GetSafeFullscreenRectangle();
            fullscreenRect.Height /= 18;
            var font = MyFontEnum.Red;
            var text = MyTexts.Get(MyCommonTexts.GamePaused);

            MyGuiManager.DrawSpriteBatch(MyGuiConstants.TEXTURE_HUD_BG_MEDIUM_RED2.Texture, fullscreenRect, Color.White);
            MyGuiManager.DrawString(font, text, new Vector2(0.5f, 0.024f), 1.0f, drawAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
        }

        #endregion
    }
}
