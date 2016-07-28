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
using Sandbox.Graphics.Render;
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

#endregion

namespace Sandbox.Game.Gui
{
    public class MyGuiScreenGamePlay : MyGuiScreenBase
    {
        private int count = 0;
        private bool audioSet = false;
        public static MyGuiScreenGamePlay Static;

        public static MyGuiScreenBase ActiveGameplayScreen = null;
        public static MyGuiScreenBase TmpGameplayScreenHolder = null;
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

                if (MySession.Static.IsAdminModeEnabled(Sync.MyId))
                {
                    return true;
                }

                if (!MySession.Static.SurvivalMode)
                    return true;

                if (MyMultiplayer.Static != null && MySession.Static.LocalHumanPlayer != null && MyMultiplayer.Static.IsAdmin(MySession.Static.LocalHumanPlayer.Id.SteamId))
                    return true;
                if (!MyFinalBuildConstants.IS_OFFICIAL || MyInput.Static.ENABLE_DEVELOPER_KEYS)
                    return true;

                return MySession.Static.Settings.EnableSpectator;
            }
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

            m_controlMenu = Activator.CreateInstance(MyPerGameSettings.ControlMenuInitializerType) as IMyControlMenuInitializer;

            MyGuiScreenCubeBuilder.ReinitializeBlockScrollbarPosition();
        }

        public static void StartLoading(Action loadingAction)
        {
            MyAnalyticsHelper.LoadingStarted();
            var newGameplayScreen = new MyGuiScreenGamePlay();
            newGameplayScreen.OnLoadingAction += loadingAction;

            var loadScreen = new MyGuiScreenLoading(newGameplayScreen, MyGuiScreenGamePlay.Static);
            loadScreen.OnScreenLoadingFinished += delegate
            {
                MyModAPIHelper.OnSessionLoaded();
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.HUDScreen));
            };
            MyGuiSandbox.AddScreen(loadScreen);
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
            MyAudio.Static.VolumeMusic = MySandboxGame.Config.MusicVolume;
            MyAudio.Static.VolumeGame = MySandboxGame.Config.GameVolume;
            MyAudio.Static.VolumeHud = MySandboxGame.Config.GameVolume;

            if (MyPerGameSettings.UseMusicController && MyFakes.ENABLE_MUSIC_CONTROLLER && MySandboxGame.Config.EnableDynamicMusic && MySandboxGame.IsDedicated == false)
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
            if (MyInput.Static.ENABLE_DEVELOPER_KEYS || (MySession.Static != null && MySession.Static.Settings.EnableSpectator) || (MyMultiplayer.Static != null && MySession.Static.LocalHumanPlayer != null && (MyMultiplayer.Static.IsAdmin(MySession.Static.LocalHumanPlayer.Id.SteamId) || MySession.Static.IsAdminModeEnabled(MySession.Static.LocalHumanPlayer.Id.SteamId))))
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
                    if (MySession.Static.ControlledEntity != null && SpectatorEnabled)
                    {
                        MySpectatorCameraController.Static.TurnLightOff();
                        MySession.Static.SetCameraController(MyCameraControllerEnum.SpectatorDelta);
                    }
                }

                //Set camera to spectator
                if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.SPECTATOR_FREE))
                {
                    if (SpectatorEnabled)
                    {
                        if (MySession.Static.GetCameraControllerEnum() != MyCameraControllerEnum.Spectator)
                        {
                            MySession.Static.SetCameraController(MyCameraControllerEnum.Spectator);
                        }
                        else if (MyInput.Static.IsAnyShiftKeyPressed())
                        {
                            MySpectatorCameraController.Static.AlignSpectatorToGravity = !MySpectatorCameraController.Static.AlignSpectatorToGravity;
                        }

                        if (MyInput.Static.IsAnyCtrlKeyPressed() && MySession.Static.ControlledEntity != null)
                        {
                            MySpectator.Static.Position = (Vector3D)MySession.Static.ControlledEntity.Entity.PositionComp.GetPosition() + MySpectator.Static.ThirdPersonCameraDelta;
                            MySpectator.Static.Target = (Vector3D)MySession.Static.ControlledEntity.Entity.PositionComp.GetPosition();
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
                            MySpectator.Static.Position = (Vector3D)MySession.Static.ControlledEntity.Entity.PositionComp.GetPosition() + MySpectator.Static.ThirdPersonCameraDelta;
                            MySpectator.Static.Target = (Vector3D)MySession.Static.ControlledEntity.Entity.PositionComp.GetPosition();
                        }
                    }
                }

                // This was added because planets, CTG testers were frustrated from testing, because they can't move in creative
                if (MySession.Static != null && (MySession.Static.CreativeMode || MySession.Static.IsAdminModeEnabled(Sync.MyId)) && MyInput.Static.IsNewKeyPressed(MyKeys.Space) && MyInput.Static.IsAnyCtrlKeyPressed())
                {
                    if (MySession.Static.CameraController == MySpectator.Static && MySession.Static.ControlledEntity != null)
                    {
                        MySession.Static.ControlledEntity.Teleport(MySpectator.Static.Position);
                    }
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

            var controlledObject = MySession.Static.ControlledEntity;
            var currentCameraController = MySession.Static.CameraController;

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

            MyStringId context = controlledObject != null ? controlledObject.ControlContext : MySpaceBindingCreator.CX_BASE;

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
                                controlledObject.BeginShoot(MyShootActionEnum.PrimaryAction);
                            }
                        }

                        if (MyControllerHelper.IsControl(context, MyControlsSpace.PRIMARY_TOOL_ACTION, MyControlStateType.NEW_RELEASED))
                        {
                            controlledObject.EndShoot(MyShootActionEnum.PrimaryAction);
                        }
                        if (MyControllerHelper.IsControl(context, MyControlsSpace.SECONDARY_TOOL_ACTION, MyControlStateType.NEW_PRESSED))
                        {
                            controlledObject.BeginShoot(MyShootActionEnum.SecondaryAction);
                        }
                        if (MyControllerHelper.IsControl(context, MyControlsSpace.SECONDARY_TOOL_ACTION, MyControlStateType.NEW_RELEASED))
                        {
                            controlledObject.EndShoot(MyShootActionEnum.SecondaryAction);
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
                        if (!(MySession.Static.CameraController is MySpectatorCameraController && MySpectatorCameraController.Static.SpectatorCameraMovement == MySpectatorCameraMovementEnum.UserControlled))
                        {
                            if (MyControllerHelper.IsControl(context, MyControlsSpace.CROUCH, MyControlStateType.NEW_PRESSED))
                            {
                                controlledObject.Crouch();
                            }
                            if (MyControllerHelper.IsControl(context, MyControlsSpace.CROUCH, MyControlStateType.PRESSED))
                            {
                                controlledObject.Down();
                            }

                            if (MyControllerHelper.IsControl(context, MyControlsSpace.SPRINT, MyControlStateType.NEW_PRESSED))
                            {
                                controlledObject.Sprint(true);
                            }
                            else if (MyControllerHelper.IsControl(context, MyControlsSpace.SPRINT, MyControlStateType.NEW_RELEASED))
                            {
                                controlledObject.Sprint(false);
                            }

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
                            if (MySession.Static.ControlledEntity != null && MySession.Static.CameraController is MySpectatorCameraController && MySpectatorCameraController.Static.SpectatorCameraMovement == MySpectatorCameraMovementEnum.UserControlled)
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
            if (!VRageRender.Profiler.MyRenderProfiler.ProfilerVisible && MyControllerHelper.IsControl(context, MyControlsSpace.CHAT_SCREEN, MyControlStateType.NEW_PRESSED))
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

            //  Launch main menu
            if (MyInput.Static.IsNewKeyPressed(MyKeys.Escape)
                || MyControllerHelper.IsControl(context, MyControlsGUI.MAIN_MENU, MyControlStateType.NEW_PRESSED))
            {
                MyGuiAudio.PlaySound(MyGuiSounds.HudMouseClick);

                //Allow changing video options from game in DX version
                MyGuiScreenMainMenu.AddMainMenu(MySandboxGame.IsPaused == false);
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
                if (MyGuiScreenCubeBuilder.Static == null && (MySession.Static.ControlledEntity is MyShipController || MySession.Static.ControlledEntity is MyCharacter))
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

            if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.PAUSE_GAME))
            {
                MySandboxGame.UserPauseToggle();
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
                    else if (MyPerGameSettings.GUI.VoxelMapEditingScreen != null && (MySession.Static.IsAdminModeEnabled(Sync.MyId) || MySession.Static.CreativeMode) && MyInput.Static.IsAnyShiftKeyPressed())
                    {
                        // Shift + F10
                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.VoxelMapEditingScreen));
                    }
                    else
                    {
                        // F10
                        if (MyFakes.ENABLE_BATTLE_SYSTEM && MySession.Static.Battle)
                        {
                            if (MyPerGameSettings.GUI.BattleBlueprintScreen != null)
                                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.BattleBlueprintScreen));
                            else
                                Debug.Fail("No battle blueprint screen");
                        }
                        else
                            MyGuiSandbox.AddScreen(new MyGuiBlueprintScreen(MyClipboardComponent.Static.Clipboard, MySession.Static.CreativeMode || MySession.Static.IsAdminModeEnabled(Sync.MyId)));
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
                if (MySession.Static.CameraController is MySpectatorCameraController && MySpectatorCameraController.Static.SpectatorCameraMovement == MySpectatorCameraMovementEnum.UserControlled)
                {
                    MySpectatorCameraController.Static.MoveAndRotate(moveIndicator, rotationIndicator, rollIndicator);
                    //MySpectatorCameraController.Static.UpdateLight();
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
                        if (MySession.Static.ControlledEntity is MyRemoteControl )
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
        }

        public static void SetCameraController()
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
                               MyGuiScreenMainMenu.UnloadAndExitToMenu();
                               MyJoinGameHelper.JoinGame(lobbyId);
                           }
                           else if (MyMultiplayer.Static is MyMultiplayerClient)
                           {
                               var server = (MyMultiplayer.Static as MyMultiplayerClient).Server;
                               MyGuiScreenMainMenu.UnloadAndExitToMenu();
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
                                           MyGuiScreenLoadSandbox.LoadSingleplayerSession(currentSession);
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
            count++;
            if (audioSet == false && count > 20 && (VRageRender.MyRenderProxy.VisibleObjectsRead.Count > 0 || count > 60 * 60))
            {
                SetAudioVolumes();
                audioSet = true;
            }
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            return true;
        }

        #endregion

        #region Draw

        public override bool Draw()
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyGuiScreenGamePlay::Draw");

            //VRageRender.MyRenderProxy.DebugDrawSphere(
            //    new Vector3D(-60.7171351205786, 34.002275028352, 78.131769977211),
            //    0.02f,
            //    Vector3.One,
            //    1, true, true);

            //VRageRender.MyRenderProxy.DebugDrawSphere(
            //    new Vector3(-13.36391f, -1.974166f, -35.97278f),
            //    0.2f,
            //    Vector3.One,
            //    1, true, true);



            //Vector3 target = new Vector3(-83.87779f, -62.17611f, -127.3294f);
            //Vector3 pos = new Vector3(-87.42791f, -57.17604f, -139.3147f);

            //VRageRender.MyRenderProxy.DebugDrawLine3D(
            //    target, pos, Color.Green, Color.Yellow, false);

            //if (MyCubeBuilder.Static.CurrentGrid != null)
            //{
            //    Matrix m = MyCubeBuilder.Static.CurrentGrid.WorldMatrix;
            //    m.Translation = MySession.Static.ControlledObject.WorldMatrix.Translation;
            //    VRageRender.MyRenderProxy.DebugDrawAxis(m, 1, false);
            //}

            if (MySector.MainCamera != null)
            {
                // set new camera values
                MySession.Static.CameraController.ControlCamera(MySector.MainCamera);
                // update camera properties accordingly to the new settings - zoom, spring, shaking...
                MySector.MainCamera.Update(MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS);
                // upload to renderer
                MySector.MainCamera.UploadViewMatrixToRender();
            }

            MyRenderProxy.UpdateGameplayFrame(MySession.Static.GameplayFrameCounter);

            VRageRender.MyRenderProxy.UpdateGodRaysSettings(
                MySector.GodRaysProperties.Enabled,
                MySector.GodRaysProperties.Density,
                MySector.GodRaysProperties.Weight,
                MySector.GodRaysProperties.Decay,
                MySector.GodRaysProperties.Exposition,
                false
            );

            VRageRender.MyRenderProxy.UpdateAntiAliasSettings(
                MyPostProcessAntiAlias.Enabled
            );

            VRageRender.MyRenderProxy.UpdateVignettingSettings(
                MyPostProcessVignetting.Enabled,
                MyPostProcessVignetting.VignettingPower
            );

            VRageRender.MyRenderProxy.UpdateColorMappingSettings(
                MyPostProcessColorMapping.Enabled
            );

            VRageRender.MyRenderProxy.UpdateChromaticAberrationSettings(
                MyPostProcessChromaticAberration.Enabled,
                MyPostProcessChromaticAberration.DistortionLens,
                MyPostProcessChromaticAberration.DistortionCubic,
                new Vector3(MyPostProcessChromaticAberration.DistortionWeightRed,
                            MyPostProcessChromaticAberration.DistortionWeightGreen,
                            MyPostProcessChromaticAberration.DistortionWeightBlue)
            );

            VRageRender.MyRenderProxy.UpdateContrastSettings(
                MyPostProcessContrast.Enabled,
                MyPostProcessContrast.Contrast,
                MyPostProcessContrast.Hue,
                MyPostProcessContrast.Saturation
            );

            VRageRender.MyRenderFogSettings fogSettings = new VRageRender.MyRenderFogSettings()
            {
                Enabled = MySector.FogProperties.EnableFog,
                FogNear = MySector.FogProperties.FogNear,
                FogFar = MySector.FogProperties.FogFar,
                FogMultiplier = MySector.FogProperties.FogMultiplier,
                FogBacklightMultiplier = MySector.FogProperties.FogBacklightMultiplier,
                FogColor = MySector.FogProperties.FogColor,
                FogDensity = MySector.FogProperties.FogDensity / 100.0f
            };
            VRageRender.MyRenderProxy.UpdateFogSettings(ref fogSettings);

            VRageRender.MyRenderProxy.UpdateHDRSettings(
                MyPostProcessHDR.DebugHDRChecked,
                MyPostProcessHDR.Exposure,
                MyPostProcessHDR.Threshold,
                MyPostProcessHDR.BloomIntensity,
                MyPostProcessHDR.BloomIntensityBackground,
                MyPostProcessHDR.VerticalBlurAmount,
                MyPostProcessHDR.HorizontalBlurAmount,
                (int)MyPostProcessHDR.NumberOfBlurPasses
            );


            VRageRender.MyRenderProxy.UpdateSSAOSettings(
                MyPostProcessVolumetricSSAO2.Enabled,
                MyPostProcessVolumetricSSAO2.ShowOnlySSAO,
                MyPostProcessVolumetricSSAO2.UseBlur,
                MyPostProcessVolumetricSSAO2.MinRadius,
                MyPostProcessVolumetricSSAO2.MaxRadius,
                MyPostProcessVolumetricSSAO2.RadiusGrowZScale,
                MyPostProcessVolumetricSSAO2.CameraZFarScale * MySector.MainCamera.FarPlaneDistance,
                MyPostProcessVolumetricSSAO2.Bias,
                MyPostProcessVolumetricSSAO2.Falloff,
                MyPostProcessVolumetricSSAO2.NormValue,
                MyPostProcessVolumetricSSAO2.Contrast
            );

            var gravityProviders = Sandbox.Game.GameSystems.MyGravityProviderSystem.NaturalGravityProviders;
            float planetFactor = 0;
            Vector3D cameraPos = MySector.MainCamera.WorldMatrix.Translation;
            foreach (var gravityProvider in gravityProviders)
            {
                var planet = gravityProvider as MyPlanet;
                if (planet != null)
                {
                    if (planet.HasAtmosphere)
                    {
                        double distanceToPlanet = (planet.WorldMatrix.Translation - cameraPos).Length();
                        float t = ((float)distanceToPlanet - planet.AverageRadius) / (planet.AtmosphereRadius - planet.AverageRadius);
                        if (t < 1.0f)
                        {
                            planetFactor = 1.0f - MathHelper.Clamp(t, 0f, 1f);

                            // Dark side intensity hack
                            //float sunDot = sunDirection.Dot(Vector3D.Normalize(planet.WorldMatrix.Translation - cameraPos));
                            //
                            //if(sunDot < 0f
                            //	&& planetFactor > 0.8f)
                            //{
                            //    float planetInfluence = 1.0f - MathHelper.Clamp((planetFactor - 0.8f) / 0.15f, 0.0f, 1.0f);
                            //    float positionInfluence = MathHelper.Clamp(1.0f + sunDot / 0.1f, 0f, 1f);
                            //    MySector.SunProperties.SunIntensity = MathHelper.Clamp(planetInfluence + positionInfluence, 0.0f, 1.0f) * MyDefinitionManager.Static.EnvironmentDefinition.SunProperties.SunIntensity;
                            //}
                            //else
                            //{
                            //    MySector.SunProperties.SunIntensity = MyDefinitionManager.Static.EnvironmentDefinition.SunProperties.SunIntensity;
                            //}

                            break;
                        }
                    }
                }
            }

            VRageRender.MyRenderProxy.UpdateRenderEnvironment(
                -MySector.SunProperties.SunDirectionNormalized,
                MySector.SunProperties.SunDiffuse,
                MySector.SunProperties.AdditionalSunDiffuse,
                MySector.SunProperties.SunSpecular,
                MySector.SunProperties.SunIntensity,
                MySector.SunProperties.AdditionalSunIntensity,
                MySector.SunProperties.AdditionalSunDirection,
                true,
                MySector.SunProperties.AmbientColor,
                MySector.SunProperties.AmbientMultiplier,
                MySector.SunProperties.EnvironmentAmbientIntensity,
                MySector.SunProperties.BackgroundColor,
                MySector.BackgroundTexture,
                MySector.BackgroundTextureNight,
                MySector.BackgroundTextureNightPrefiltered,
                MySector.BackgroundOrientation,
                MySector.SunProperties.SunSizeMultiplier,
                MySector.DistanceToSun,
                MySector.SunProperties.SunMaterial,
                MySector.DayTime,
                MySector.ResetEyeAdaptation,
                MyFakes.ENABLE_SUN_BILLBOARD,
                planetFactor
            );

            if (MyDebugDrawSettings.DEBUG_DRAW_ADDITIONAL_ENVIRONMENTAL_LIGHTS)
            {
                Color[] colors = { Color.Red, Color.Green, Color.Blue, Color.Yellow, Color.SlateGray };
                for (int lightIndex = 0; lightIndex < MySector.SunProperties.AdditionalSunDirection.Length; ++lightIndex)
                {
                    var lightDirection = MySector.SunProperties.AdditionalSunDirection[lightIndex];
                    MyRenderProxy.DebugDrawSphere(
                        MySector.MainCamera.Position + 2f * MathHelper.CalculateVectorOnSphere(MySector.SunProperties.SunDirectionNormalized, lightDirection[0], lightDirection[1]),
                        0.25f, colors[lightIndex], 1f, false);

                }
            }

            MySector.ResetEyeAdaptation = false;
            VRageRender.MyRenderProxy.UpdateEnvironmentMap();


            VRageRender.MyRenderProxy.SwitchProsprocessSettings(VRageRender.MyPostprocessSettings.LerpExposure(ref MyPostprocessSettingsWrapper.Settings, ref MyPostprocessSettingsWrapper.PlanetSettings, planetFactor));

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

            VRageRender.MyRenderProxy.GetRenderProfiler().StartNextBlock("FillDebugScreen");
            //FillDebugScreen();
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

            if (MySandboxGame.IsPaused && !MyHud.MinimalHud)
                DrawPauseIndicator();

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
