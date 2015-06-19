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
using Sandbox.Game.Screens;
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
using VRage;
using VRage;
using VRage.Input;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;


#endregion

namespace Sandbox.Game.Gui
{
    public class MyGuiScreenGamePlay : MyGuiScreenBase
    {
        private int count = 0;
        public static MyGuiScreenGamePlay Static;

        public static MyGuiScreenBase ActiveGameplayScreen = null;
        public static MyGuiScreenBase TmpGameplayScreenHolder = null;
        IMyControlMenuInitializer m_controlMenu = null;

        #region Properties

        public bool CanSwitchCamera
        {
            get 
            {
                if (!MyCubeBuilder.Static.Clipboard.AllowSwitchCameraMode || !MySession.Static.Settings.Enable3rdPersonView)
                    return false;
                MyCameraControllerEnum cameraControllerEnum = MySession.GetCameraControllerEnum();
                bool isValidController = (cameraControllerEnum == MyCameraControllerEnum.Entity || cameraControllerEnum == MyCameraControllerEnum.ThirdPersonSpectator);
                return (!MySession.Static.CameraController.ForceFirstPersonCamera && isValidController);
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
            if (MyCubeBuilder.Static != null)
                handled = MyCubeBuilder.Static.HandleGameInput();

            if (!handled)
                base.HandleInput(receivedFocusInThisUpdate);
        }

        public override void InputLost()
        {
            if (MyCubeBuilder.Static != null)
                MyCubeBuilder.Static.InputLost();
        }

        //  This method is called every update (but only if application has focus)
        public override void HandleUnhandledInput(bool receivedFocusInThisUpdate)
        {
            if (MyInput.Static.ENABLE_DEVELOPER_KEYS || (MySession.Static != null && MySession.Static.Settings.EnableSpectator) || (MyMultiplayer.Static != null && MySession.LocalHumanPlayer != null && MyMultiplayer.Static.IsAdmin(MySession.LocalHumanPlayer.Id.SteamId)))
            {
                //Set camera to player
                if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.SPECTATOR_NONE))
                {
                    if (MySession.ControlledEntity != null)
                    { //we already are controlling this object

                        if (MyFinalBuildConstants.IS_OFFICIAL)
                        {
                            SetCameraController();
                        }
                        else
                        {
                            var cameraController = MySession.GetCameraControllerEnum();
                            if (cameraController != MyCameraControllerEnum.Entity && cameraController != MyCameraControllerEnum.ThirdPersonSpectator)
                            {
                                SetCameraController();
                            }
                            else
                            {
                                var entities = MyEntities.GetEntities().ToList();
                                int lastKnownIndex = entities.IndexOf(MySession.ControlledEntity.Entity);

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
                                    if (character != null && !character.IsDead)
                                    {
                                        newControlledObject = character;
                                        break;
                                    }
                                }

                                if (MySession.LocalHumanPlayer != null && newControlledObject != null)
                                {
                                    MySession.LocalHumanPlayer.Controller.TakeControl(newControlledObject);
                                }
                            }

                            // We could have activated the cube builder in spectator, so deactivate it now
                            if (MyCubeBuilder.Static.IsActivated && !(MySession.ControlledEntity is MyCharacter))
                                MyCubeBuilder.Static.Deactivate();
                        }
                    }
                }

                //Set camera to following third person
                if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.SPECTATOR_DELTA))
                {
                    if (MySession.ControlledEntity != null)
                    {
                        MySession.SetCameraController(MyCameraControllerEnum.SpectatorDelta);
                    }
                }

                //Set camera to spectator
                if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.SPECTATOR_FREE))
                {
                    if (!MyFakes.ENABLE_BATTLE_SYSTEM || !MySession.Static.Battle || Sync.IsServer)
                    {
                        if (MySession.GetCameraControllerEnum() != MyCameraControllerEnum.Spectator)
                        {
                            MySession.SetCameraController(MyCameraControllerEnum.Spectator);
                        }
                        else if (MyInput.Static.IsAnyShiftKeyPressed())
                        {
                            MyFakes.ENABLE_DEVELOPER_SPECTATOR_CONTROLS = !MyFakes.ENABLE_DEVELOPER_SPECTATOR_CONTROLS;
                        }

                        if (MyInput.Static.IsAnyCtrlKeyPressed() && MySession.ControlledEntity != null)
                        {
                            MySpectator.Static.Position = (Vector3D)MySession.ControlledEntity.Entity.PositionComp.GetPosition() + MySpectator.Static.ThirdPersonCameraDelta;
                            MySpectator.Static.Target = (Vector3D)MySession.ControlledEntity.Entity.PositionComp.GetPosition();
                        }
                    }
                }

                //Set camera to static spectator, non movable
                if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.SPECTATOR_STATIC))
                {
                    if (MySession.ControlledEntity != null)
                    {
                        MySession.SetCameraController(MyCameraControllerEnum.SpectatorFixed);

                        if (MyInput.Static.IsAnyCtrlKeyPressed())
                        {
                            MySpectator.Static.Position = (Vector3D)MySession.ControlledEntity.Entity.PositionComp.GetPosition() + MySpectator.Static.ThirdPersonCameraDelta;
                            MySpectator.Static.Target = (Vector3D)MySession.ControlledEntity.Entity.PositionComp.GetPosition();
                        }
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
            if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.CAMERA_MODE) && CanSwitchCamera)
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

            var controlledObject = MySession.ControlledEntity;
            var currentCameraController = MySession.Static.CameraController;

            if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.MISSION_SETTINGS) && MyGuiScreenGamePlay.ActiveGameplayScreen == null 
                && MyPerGameSettings.Game == Sandbox.Game.GameEnum.SE_GAME
                && MyFakes.ENABLE_MISSION_TRIGGERS
                && MySession.Static.Settings.ScenarioEditMode)
                {
                MyGuiSandbox.AddScreen(new Sandbox.Game.Screens.MyGuiScreenMissionTriggers());
            }

            MyStringId context = controlledObject != null ? controlledObject.ControlContext : MySpaceBindingCreator.CX_BASE;

            bool handledByUseObject = false;
            if (MySession.ControlledEntity is VRage.Game.Entity.UseObject.IMyUseObject)
            {
                handledByUseObject = (MySession.ControlledEntity as VRage.Game.Entity.UseObject.IMyUseObject).HandleInput();
            }

            if (controlledObject != null && !handledByUseObject)
            {
                if (!MySandboxGame.IsPaused)
                {
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
                            if (MyControllerHelper.IsControl(context, MyControlsSpace.SPRINT, MyControlStateType.PRESSED))
                            {
                                controlledObject.Sprint();
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
                            MyGuiAudio.PlaySound(MyGuiSounds.HudClick);
                            controlledObject.SwitchThrusts();
                        }
                        if (MyControllerHelper.IsControl(context, MyControlsSpace.HEADLIGHTS, MyControlStateType.NEW_PRESSED))
                        {
                            MyGuiAudio.PlaySound(MyGuiSounds.HudClick);
                            controlledObject.SwitchLights();
                        }
                        if (MyControllerHelper.IsControl(context, MyControlsSpace.TOGGLE_REACTORS, MyControlStateType.NEW_PRESSED))
                        {
                            MyGuiAudio.PlaySound(MyGuiSounds.HudClick);
                            controlledObject.SwitchReactors();
                        }
                        if (MyControllerHelper.IsControl(context, MyControlsSpace.LANDING_GEAR, MyControlStateType.NEW_PRESSED))
                        {
                            MyGuiAudio.PlaySound(MyGuiSounds.HudClick);
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

                if (!MyCompilationSymbols.RenderProfiling && MyControllerHelper.IsControl(context, MyControlsSpace.CHAT_SCREEN, MyControlStateType.NEW_PRESSED))
                {
                    if (MyGuiScreenChat.Static == null)
                    {
                        Vector2 chatPos = new Vector2(0.01f, 0.84f);
                        chatPos = MyGuiScreenHudBase.ConvertHudToNormalizedGuiPosition(ref chatPos);
                        MyGuiScreenChat chatScreen = new MyGuiScreenChat(chatPos);
                        MyGuiSandbox.AddScreen(chatScreen);
                    }
                }

                if (MyPerGameSettings.VoiceChatEnabled)
                {
                    if (MyControllerHelper.IsControl(context, MyControlsSpace.VOICE_CHAT, MyControlStateType.NEW_PRESSED))
                    {
                        MyVoiceChatSessionComponent.Static.StartRecording();
                    }
                    else if (MyControllerHelper.IsControl(context, MyControlsSpace.VOICE_CHAT, MyControlStateType.NEW_RELEASED))
                    {
                        MyVoiceChatSessionComponent.Static.StopRecording();
                    }
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
                        if (MySession.Static.ClientCanSave || Sync.IsServer)
                        {
                            if (!MyAsyncSaving.InProgress)
                            {
                                var messageBox = MyGuiSandbox.CreateMessageBox(
                                    buttonType: MyMessageBoxButtonsType.YES_NO,
                                    messageText: MyTexts.Get(MySpaceTexts.MessageBoxTextAreYouSureYouWantToQuickSave),
                                    messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionPleaseConfirm),
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
                MyGuiScreenMainMenu.AddMainMenu();
            }

            if (MyInput.Static.IsNewKeyPressed(MyKeys.F3))
            {
                if (Sync.MultiplayerActive)
                {
                    MyGuiAudio.PlaySound(MyGuiSounds.HudMouseClick);
                    MyGuiSandbox.AddScreen(new MyGuiScreenPlayers());
                }
                else
                    MyHud.Notifications.Add(MyNotificationSingletons.MultiplayerDisabled);
            }

            if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.BUILD_SCREEN) && MyGuiScreenGamePlay.ActiveGameplayScreen == null)
            {
                if (MyGuiScreenCubeBuilder.Static == null && (MySession.ControlledEntity is MyShipController || MySession.ControlledEntity is MyCharacter))
                {
                    int offset = 0;
                    if (MyInput.Static.IsAnyShiftKeyPressed()) offset += 6;
                    if (MyInput.Static.IsAnyCtrlKeyPressed()) offset += 12;
                    MyGuiAudio.PlaySound(MyGuiSounds.HudMouseClick);
                    MyGuiSandbox.AddScreen(
                        MyGuiScreenGamePlay.ActiveGameplayScreen = MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.ToolbarConfigScreen,
                                                                                             offset,
                                                                                             MySession.ControlledEntity as MyShipController)
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
                    if (MyPerGameSettings.GUI.VoxelMapEditingScreen != null && MySession.Static.CreativeMode && MyInput.Static.IsAnyShiftKeyPressed())
                    { // Shift+F10
                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.VoxelMapEditingScreen));
                    }
                    else
                    { // F10
                        if (MyFakes.ENABLE_BATTLE_SYSTEM && MySession.Static.Battle)
                        {
                            if (MyPerGameSettings.GUI.BattleBlueprintScreen != null)
                                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.BattleBlueprintScreen));
                            else
                                Debug.Fail("No battle blueprint screen");
                        }
                        else
                            MyGuiSandbox.AddScreen(new MyGuiBlueprintScreen(MyCubeBuilder.Static.Clipboard));
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
            MyCameraControllerEnum cce = MySession.GetCameraControllerEnum();
            bool movementAllowedInPause = cce == MyCameraControllerEnum.Spectator;
            bool rotationAllowedInPause = movementAllowedInPause ||
                                          (cce == MyCameraControllerEnum.ThirdPersonSpectator && MyInput.Static.IsAnyAltKeyPressed());

            bool allowRoll = !MySessionComponentVoxelHand.Static.BuildMode;
            bool allowMove = !MySessionComponentVoxelHand.Static.BuildMode && !MyCubeBuilder.Static.IsBuildMode;
            float rollIndicator = allowRoll ? MyInput.Static.GetRoll() : 0;
            Vector2 rotationIndicator = MyInput.Static.GetRotation();
            VRageMath.Vector3 moveIndicator = allowMove ? MyInput.Static.GetPositionDelta() : Vector3.Zero;

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ////  Decide who is moving
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////

            //First move control objects
            if (MySession.ControlledEntity != null)// && MySession.ControlledObject != MySession.Static.CameraController)
            {
                if (MySandboxGame.IsPaused)
                {
                    if (!movementAllowedInPause && !rotationAllowedInPause)
                    {
                        return;
                    }

                    if (!rotationAllowedInPause)
                    {
                        rotationIndicator = Vector2.Zero;
                    }
                    rollIndicator = 0.0f;
                }
                if (MySession.Static.CameraController is MySpectatorCameraController && MySpectatorCameraController.Static.SpectatorCameraMovement == MySpectatorCameraMovementEnum.UserControlled)
                {
                    MySpectatorCameraController.Static.MoveAndRotate(moveIndicator, rotationIndicator, rollIndicator);
                }
                else 
                {
                    if (!MySession.Static.CameraController.IsInFirstPersonView)
                        MyThirdPersonSpectator.Static.UpdateZoom();

                    if (!MyInput.Static.IsGameControlPressed(MyControlsSpace.LOOKAROUND))
                    {
                        MySession.ControlledEntity.MoveAndRotate(moveIndicator, rotationIndicator, rollIndicator);
                    }
                    else
                    {
                        MySession.ControlledEntity.MoveAndRotate(moveIndicator, Vector2.Zero, rollIndicator);
                        if (!MySession.Static.CameraController.IsInFirstPersonView)
                            MyThirdPersonSpectator.Static.SaveSettings();
                    }
                }
            }
        }

        private static void SetCameraController()
        {
            var remote = MySession.ControlledEntity.Entity as MyRemoteControl;
            if (remote != null)
            {
                if (remote.PreviousControlledEntity is IMyCameraController)
                {
                    MySession.SetCameraController(MyCameraControllerEnum.Entity, remote.PreviousControlledEntity.Entity);
                }
            }
            else
            {
                MySession.SetCameraController(MyCameraControllerEnum.Entity, MySession.ControlledEntity.Entity);
            }
        }
        #endregion

        #region Methods

        public void SwitchCamera()
        {
            MySession.Static.CameraController.IsInFirstPersonView = !MySession.Static.CameraController.IsInFirstPersonView;

            if (MySession.GetCameraControllerEnum() == MyCameraControllerEnum.ThirdPersonSpectator)
            {
                MyEntityCameraSettings settings = null;
                if (MySession.Static.Cameras.TryGetCameraSettings(MySession.LocalHumanPlayer.Id, MySession.ControlledEntity.Entity.EntityId, out settings))
                    MyThirdPersonSpectator.Static.ResetDistance(settings.Distance);
                else
                    MyThirdPersonSpectator.Static.RecalibrateCameraPosition();
            }

            MySession.SaveControlledEntityCameraSettings(MySession.Static.CameraController.IsInFirstPersonView);
        }

        public void ShowReconnectMessageBox()
        {
            var messageBox = MyGuiSandbox.CreateMessageBox(
                   buttonType: MyMessageBoxButtonsType.YES_NO,
                   messageText: MyTexts.Get(MySpaceTexts.MessageBoxTextAreYouSureYouWantToReconnect),
                   messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionPleaseConfirm),
                   callback: delegate(MyGuiScreenMessageBox.ResultEnum callbackReturn)
                   {
                       if (callbackReturn == MyGuiScreenMessageBox.ResultEnum.YES)
                       {
                           if (MyMultiplayer.Static is MyMultiplayerLobby)
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
                                   messageText: MyTexts.Get(MySpaceTexts.MessageBoxTextAreYouSureYouWantToQuickLoad),
                                   messageCaption: MyTexts.Get(MySpaceTexts.MessageBoxCaptionPleaseConfirm),
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
            MySector.MainCamera.SetViewMatrix(MySession.Static.CameraController.GetViewMatrix());

            base.Update(hasFocus);
            count++;
            

            var s = MyScreenManager.TotalGamePlayTimeInMilliseconds;

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
            //    m.Translation = MySession.ControlledObject.WorldMatrix.Translation;
            //    VRageRender.MyRenderProxy.DebugDrawAxis(m, 1, false);
            //}

            MatrixD viewMatrix = MySession.Static.CameraController.GetViewMatrix();
            if (viewMatrix.IsValid() && viewMatrix != MatrixD.Zero)            
            {
                MySector.MainCamera.SetViewMatrix(viewMatrix);
            }
            else
            {
                Debug.Fail("Camera matrix is invalid or zero!");
            }

            

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
                FogDensity = MySector.FogProperties.FogDensity
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

            Vector3 sunDirection = -MySector.DirectionToSunNormalized;
            if (MySession.Static.Settings.EnableSunRotation)
            {
                double angle = 2.0*MathHelper.Pi * MySession.Static.ElapsedGameTime.TotalMinutes / MySession.Static.Settings.SunRotationIntervalMinutes;
                sunDirection += new Vector3(Math.Cos(angle),0, Math.Sin(angle));
                sunDirection.Normalize();
            }

            VRageRender.MyRenderProxy.UpdateRenderEnvironment(
                sunDirection,
                MySector.SunProperties.SunDiffuse,
                MySector.SunProperties.BackSunDiffuse,
                MySector.SunProperties.SunSpecular,
                MySector.SunProperties.SunIntensity,
                MySector.SunProperties.BackSunIntensity,
                true,
                MySector.SunProperties.AmbientColor,
                MySector.SunProperties.AmbientMultiplier,
                MySector.SunProperties.EnvironmentAmbientIntensity,
                MySector.SunProperties.BackgroundColor,
                MySector.BackgroundTexture,
                MySector.BackgroundOrientation,
                MySector.SunProperties.SunSizeMultiplier,
                MySector.DistanceToSun,
                MySector.SunProperties.SunMaterial,
                MySector.DayTime,
                MySector.ResetEyeAdaptation,
                MyFakes.ENABLE_SUN_BILLBOARD
            );
            MySector.ResetEyeAdaptation = false;
            VRageRender.MyRenderProxy.UpdateEnvironmentMap();

            VRageRender.MyRenderProxy.SwitchProsprocessSettings(MyPostprocessSettingsWrapper.Settings);

            VRageRender.MyRenderProxy.GetRenderProfiler().StartNextBlock("Main render");

            VRageRender.MyRenderProxy.Draw3DScene();

            using (Stats.Generic.Measure("GamePrepareDraw"))
            {
                if (MySession.Static != null)
                    MySession.Static.Draw();
            }

            VRageRender.MyRenderProxy.GetRenderProfiler().StartNextBlock("Draw HUD");

            if (MySession.ControlledEntity != null && MySession.Static.CameraController != null)
                MySession.ControlledEntity.DrawHud(MySession.Static.CameraController, MySession.LocalPlayerId);

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
            var text = MyTexts.Get(MySpaceTexts.GamePaused);

            MyGuiManager.DrawSpriteBatch(MyGuiConstants.TEXTURE_HUD_BG_MEDIUM_RED2.Texture, fullscreenRect, Color.White);
            MyGuiManager.DrawString(font, text, new Vector2(0.5f, 0.024f), 1.0f, drawAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
        }

        #endregion
    }
}
