using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game.Audio;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using VRage;
using VRage.Audio;
using VRage.FileSystem;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.World
{
    /// <summary>
    /// This class is a Loading helper toolbox that holds syntax
    /// that was split all across the GUI screens.
    /// </summary>
    public static class MySessionLoader
    {
        public static event Action BattleWorldLoaded;
        public static event Action ScenarioWorldLoaded;        
        public static event Action<MyObjectBuilder_Checkpoint> CampaignWorldLoaded;

        /// <summary>
        /// Starts new session and unloads outdated if theres any.
        /// </summary>
        /// <param name="sessionName">Created session name.</param>
        /// <param name="settings">Session settings OB.</param>
        /// <param name="mods">Mod selection.</param>
        /// <param name="scenarioDefinition">World generator argument.</param>
        /// <param name="asteroidAmount">Hostility settings.</param>
        /// <param name="description">Session description.</param>
        /// <param name="passwd">Session password.</param>
        public static void StartNewSession( string                                      sessionName, 
                                            MyObjectBuilder_SessionSettings             settings, 
                                            List<MyObjectBuilder_Checkpoint.ModItem> mods,
                                            MyScenarioDefinition                        scenarioDefinition = null,
                                            int                                         asteroidAmount = 0, 
                                            string                                      description = "", 
                                            string                                      passwd = "")
        {
            MyLog.Default.WriteLine("StartNewSandbox - Start");

            if (!MySteamWorkshop.CheckLocalModsAllowed(mods, settings.OnlineMode == MyOnlineModeEnum.OFFLINE))
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                    messageText: MyTexts.Get(MyCommonTexts.DialogTextLocalModsDisabledInMultiplayer),
                    buttonType: MyMessageBoxButtonsType.OK));
                MyLog.Default.WriteLine("LoadSession() - End");
                return;
            }

            MySteamWorkshop.DownloadModsAsync(mods, delegate(bool success, string mismatchMods)
            {
                if (success || (settings.OnlineMode == MyOnlineModeEnum.OFFLINE) && MySteamWorkshop.CanRunOffline(mods))
                {
                    CheckMismatchmods(mismatchMods, callback: delegate(ResultEnum val)
                    {
                        MyScreenManager.RemoveAllScreensExcept(null);

                        if (asteroidAmount < 0)
                        {
                            MyWorldGenerator.SetProceduralSettings(asteroidAmount, settings);
                            asteroidAmount = 0;
                        }

                        MyAnalyticsHelper.SetEntry(MyGameEntryEnum.Custom);

                        StartLoading(delegate
                        {
                            MyAnalyticsHelper.SetEntry(MyGameEntryEnum.Custom);

                            MySession.Start(
                                sessionName,
                                description,
                                passwd,
                                settings,
                                mods,
                                new MyWorldGenerator.Args()
                                {
                                    AsteroidAmount = asteroidAmount,
                                    Scenario = scenarioDefinition
                                }
                            );
                        });
                    });
                }
                else
                {
                    if (MySteam.IsOnline)
                    {
                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                             messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                             messageText: MyTexts.Get(MyCommonTexts.DialogTextDownloadModsFailed),
                             buttonType: MyMessageBoxButtonsType.OK));
                    }
                    else
                    {
                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                                      messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                                                      messageText: MyTexts.Get(MyCommonTexts.DialogTextDownloadModsFailedSteamOffline),
                                                      buttonType: MyMessageBoxButtonsType.OK));
                    }
                }
                MyLog.Default.WriteLine("StartNewSandbox - End");
            });
        }

        public static void LoadLastSession()
        {
            var lastSessionPath = MyLocalCache.GetLastSessionPath();
            if (!MyFileSystem.DirectoryExists(lastSessionPath))
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageText: MyTexts.Get(MyCommonTexts.MessageBoxLastSessionNotFound),
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                    styleEnum: MyMessageBoxStyleEnum.Error));

                return;
            }

            LoadSingleplayerSession(lastSessionPath);
        }

        public static void LoadMultiplayerSession(MyObjectBuilder_World world, MyMultiplayerBase multiplayerSession)
        {
            MyLog.Default.WriteLine("LoadSession() - Start");

            if (!MySteamWorkshop.CheckLocalModsAllowed(world.Checkpoint.Mods, false))
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                    messageText: MyTexts.Get(MyCommonTexts.DialogTextLocalModsDisabledInMultiplayer),
                    buttonType: MyMessageBoxButtonsType.OK));
                MyLog.Default.WriteLine("LoadSession() - End");
                return;
            }

            MySteamWorkshop.DownloadModsAsync(world.Checkpoint.Mods,
                onFinishedCallback:
                delegate(bool success, string mismatchMods)
                {
                    if (success)
                    {
                        CheckMismatchmods(mismatchMods, delegate(VRage.Game.ModAPI.ResultEnum val)
                        {
                            if (val == VRage.Game.ModAPI.ResultEnum.OK)
                            {
                                //Sandbox.Audio.MyAudio.Static.Mute = true;
                                MyScreenManager.CloseAllScreensNowExcept(null);
                                MyGuiSandbox.Update(VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS);

                                // May be called from gameplay, so we must make sure we unload the current game
                                if (MySession.Static != null)
                                {
                                    MySession.Static.Unload();
                                    MySession.Static = null;
                                }

                                StartLoading(delegate { MySession.LoadMultiplayer(world, multiplayerSession); });
                            }
                            else
                            {
                                MySessionLoader.UnloadAndExitToMenu();
                            }
                        });
                    }
                    else
                    {
                        if (MyMultiplayer.Static != null)
                        {
                            MyMultiplayer.Static.Dispose();
                        }

                        if (MySteam.IsOnline)
                        {
                            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                                messageText: MyTexts.Get(MyCommonTexts.DialogTextDownloadModsFailed),
                                buttonType: MyMessageBoxButtonsType.OK));
                        }
                        else
                        {
                            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                                          messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                                                          messageText: MyTexts.Get(MyCommonTexts.DialogTextDownloadModsFailedSteamOffline),
                                                          buttonType: MyMessageBoxButtonsType.OK));
                        }
                    }
                    MyLog.Default.WriteLine("LoadSession() - End");
                },
                onCancelledCallback: delegate()
                {
                    multiplayerSession.Dispose();
                });
        }

        public static void CheckMismatchmods(string mismatchMods, Action<ResultEnum> callback)
        {
            if (String.IsNullOrEmpty(mismatchMods) == false && String.IsNullOrWhiteSpace(mismatchMods) == false && 
                //TODO: Temp check until we remove total dark mod from campaign
                MyCampaignManager.Static.ActiveCampaign == null)
            {
                MyGuiSandbox.AddScreen(new MyGuiScreenText(
                    windowSize:new Vector2(0.73f, 0.7f),
                    descSize: new Vector2(0.62f,0.44f),
                    missionTitle: MyTexts.GetString(MyCommonTexts.MessageBoxCaptionWarning),
                    currentObjectivePrefix:"",
                    currentObjective: MyTexts.GetString(MyCommonTexts.MessageBoxModsMismatch),
                    description: mismatchMods,
                    resultCallback: callback));
            }
            else if(callback != null)
            {
                callback(ResultEnum.OK);
            }
        }

        public static void LoadMultiplayerScenarioWorld(MyObjectBuilder_World world, MyMultiplayerBase multiplayerSession)
        {
            Debug.Assert(MySession.Static != null);

            MyLog.Default.WriteLine("LoadMultiplayerScenarioWorld() - Start");

            if (world.Checkpoint.BriefingVideo != null && world.Checkpoint.BriefingVideo.Length > 0)
                MyGuiSandbox.OpenUrlWithFallback(world.Checkpoint.BriefingVideo, "Scenario briefing video", true);

            if (!MySteamWorkshop.CheckLocalModsAllowed(world.Checkpoint.Mods, false))
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                    messageText: MyTexts.Get(MyCommonTexts.DialogTextLocalModsDisabledInMultiplayer),
                    buttonType: MyMessageBoxButtonsType.OK,
                    callback: delegate(MyGuiScreenMessageBox.ResultEnum result) { MySessionLoader.UnloadAndExitToMenu(); }));
                MyLog.Default.WriteLine("LoadMultiplayerScenarioWorld() - End");
                return;
            }

            MySteamWorkshop.DownloadModsAsync(world.Checkpoint.Mods,
                onFinishedCallback: delegate(bool success,string mismatchMods)
                {
                    if (success)
                    {
                        CheckMismatchmods(mismatchMods, callback: delegate(ResultEnum val)
                        {
                            MyScreenManager.CloseAllScreensNowExcept(null);
                            MyGuiSandbox.Update(MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS);

                            StartLoading(delegate
                            {
                                MySession.Static.LoadMultiplayerWorld(world, multiplayerSession);
                                if (ScenarioWorldLoaded != null)
                                    ScenarioWorldLoaded();
                            });
                        });
                    }
                    else
                    {
                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                            messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                            messageText: MyTexts.Get(MyCommonTexts.DialogTextDownloadModsFailed),
                            buttonType: MyMessageBoxButtonsType.OK,
                            callback: delegate(MyGuiScreenMessageBox.ResultEnum result) { MySessionLoader.UnloadAndExitToMenu(); }));
                    }
                    MyLog.Default.WriteLine("LoadMultiplayerScenarioWorld() - End");
                },
                onCancelledCallback: delegate()
                {
                    MySessionLoader.UnloadAndExitToMenu();
                });
        }

        //public static void LoadMultiplayerBattleWorld(MyObjectBuilder_World world, MyMultiplayerBase multiplayerSession)
        //{
        //    MyLog.Default.WriteLine("LoadMultiplayerBattleWorld() - Start");

        //    if (!MySteamWorkshop.CheckLocalModsAllowed(world.Checkpoint.Mods, false))
        //    {
        //        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
        //            messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
        //            messageText: MyTexts.Get(MyCommonTexts.DialogTextLocalModsDisabledInMultiplayer),
        //            buttonType: MyMessageBoxButtonsType.OK,
        //            callback: delegate(MyGuiScreenMessageBox.ResultEnum result) { MySessionLoader.UnloadAndExitToMenu(); }));
        //        MyLog.Default.WriteLine("LoadMultiplayerBattleWorld() - End");
        //        return;
        //    }

        //    MySteamWorkshop.DownloadModsAsync(world.Checkpoint.Mods,
        //        onFinishedCallback: delegate(bool success,string mismatchMods)
        //        {
        //            if (success)
        //            {
        //                MyScreenManager.CloseAllScreensNowExcept(null);
        //                MyGuiSandbox.Update(MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS);
        //                CheckMismatchmods(mismatchMods, callback: delegate(ResultEnum val)
        //                {
        //                    StartLoading(delegate
        //                    {
        //                        if (MySession.Static == null)
        //                        {
        //                            MySession.CreateWithEmptyWorld(multiplayerSession);
        //                            MySession.Static.Settings.Battle = true;
        //                        }

        //                        MySession.Static.LoadMultiplayerWorld(world, multiplayerSession);
        //                        Debug.Assert(MySession.Static.Battle);
        //                        if (BattleWorldLoaded != null)
        //                            BattleWorldLoaded();
        //                    });
        //                });
        //            }
        //            else
        //            {
        //                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
        //                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
        //                    messageText: MyTexts.Get(MyCommonTexts.DialogTextDownloadModsFailed),
        //                    buttonType: MyMessageBoxButtonsType.OK,
        //                    callback: delegate(MyGuiScreenMessageBox.ResultEnum result) { MySessionLoader.UnloadAndExitToMenu(); }));
        //            }
        //            MyLog.Default.WriteLine("LoadMultiplayerBattleWorld() - End");
        //        },
        //        onCancelledCallback: delegate()
        //        {
        //            MySessionLoader.UnloadAndExitToMenu();
        //        });
        //}

        private static void CheckDx11AndLoad(MyObjectBuilder_Checkpoint checkpoint, string sessionPath, ulong checkpointSizeInBytes, Action afterLoad = null)
        {
            bool needsDx11 = checkpoint.RequiresDX >= 11;
            if (!needsDx11 || MySandboxGame.IsDirectX11)
            {
                LoadSingleplayerSession(checkpoint, sessionPath, checkpointSizeInBytes, afterLoad);
            }
            else
            {
                MyJoinGameHelper.HandleDx11Needed();
            }
        }

        public static void LoadSingleplayerSession(string sessionPath, Action afterLoad = null)
        {
            MyLog.Default.WriteLine("LoadSession() - Start");
            MyLog.Default.WriteLine(sessionPath);

            ulong checkpointSizeInBytes;
            var checkpoint = MyLocalCache.LoadCheckpoint(sessionPath, out checkpointSizeInBytes);

            if (checkpoint == null)
            {
                MyLog.Default.WriteLine(MyTexts.Get(MyCommonTexts.WorldFileIsCorruptedAndCouldNotBeLoaded).ToString());
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                    messageText: MyTexts.Get(MyCommonTexts.WorldFileIsCorruptedAndCouldNotBeLoaded),
                    buttonType: MyMessageBoxButtonsType.OK));
                MyLog.Default.WriteLine("LoadSession() - End");
                return;
            }
            CheckDx11AndLoad(checkpoint, sessionPath, checkpointSizeInBytes, afterLoad);
        }

        private static string GetCustomLoadingScreenImagePath(string relativePath)
        {
            if(string.IsNullOrEmpty(relativePath)) return null;

            var customLoadingScreenPath = Path.Combine(MyFileSystem.SavesPath, relativePath);

            if (!MyFileSystem.FileExists(customLoadingScreenPath))
                customLoadingScreenPath = Path.Combine(MyFileSystem.ContentPath, relativePath);

            if (!MyFileSystem.FileExists(customLoadingScreenPath))
                customLoadingScreenPath = Path.Combine(MyFileSystem.ModsPath, relativePath);

            if (!MyFileSystem.FileExists(customLoadingScreenPath))
                customLoadingScreenPath = null;

            return customLoadingScreenPath;
        }

        public static void LoadSingleplayerSession(MyObjectBuilder_Checkpoint checkpoint, string sessionPath, ulong checkpointSizeInBytes, Action afterLoad = null)
        {
            if (!MySession.IsCompatibleVersion(checkpoint))
            {
                MyLog.Default.WriteLine(MyTexts.Get(MyCommonTexts.DialogTextIncompatibleWorldVersion).ToString());
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                    messageText: MyTexts.Get(MyCommonTexts.DialogTextIncompatibleWorldVersion),
                    buttonType: MyMessageBoxButtonsType.OK));
                MyLog.Default.WriteLine("LoadSession() - End");
                return;
            }

            if (!MySteamWorkshop.CheckLocalModsAllowed(checkpoint.Mods, checkpoint.Settings.OnlineMode == MyOnlineModeEnum.OFFLINE))
            {
                MyLog.Default.WriteLine(MyTexts.Get(MyCommonTexts.DialogTextLocalModsDisabledInMultiplayer).ToString());
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                    messageText: MyTexts.Get(MyCommonTexts.DialogTextLocalModsDisabledInMultiplayer),
                    buttonType: MyMessageBoxButtonsType.OK));
                MyLog.Default.WriteLine("LoadSession() - End");
                return;
            }

            var customLoadingScreenPath = GetCustomLoadingScreenImagePath(checkpoint.CustomLoadingScreenImage);

            MySteamWorkshop.DownloadModsAsync(checkpoint.Mods, delegate(bool success,string mismatchMods)
            {
                if (success || (checkpoint.Settings.OnlineMode == MyOnlineModeEnum.OFFLINE) && MySteamWorkshop.CanRunOffline(checkpoint.Mods))
                {
                    //Sandbox.Audio.MyAudio.Static.Mute = true;           
                    MyScreenManager.CloseAllScreensNowExcept(null);
                    MyGuiSandbox.Update(MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS);
                    CheckMismatchmods(mismatchMods, callback: delegate(ResultEnum val)
                    {
                        if (val == ResultEnum.OK)
                        {
                            // May be called from gameplay, so we must make sure we unload the current game
                            if (MySession.Static != null)
                            {
                                MySession.Static.Unload();
                                MySession.Static = null;
                            }
                            StartLoading(delegate
                            {
                                MyAnalyticsHelper.SetEntry(MyGameEntryEnum.Load);
                                MySession.Load(sessionPath, checkpoint, checkpointSizeInBytes);
                                if(afterLoad != null)
                                    afterLoad();
                            }, customLoadingScreenPath, checkpoint.CustomLoadingScreenText);
                        }
                        else
                        {
                            MySessionLoader.UnloadAndExitToMenu();
                        }
                    });                      
                }
                else
                {
                    MyLog.Default.WriteLine(MyTexts.Get(MyCommonTexts.DialogTextDownloadModsFailed).ToString());

                    if (MySteam.IsOnline)
                    {
                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                            messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                            messageText: MyTexts.Get(MyCommonTexts.DialogTextDownloadModsFailed),
                            buttonType: MyMessageBoxButtonsType.OK, callback: delegate(MyGuiScreenMessageBox.ResultEnum result)
                            {
                                if (MyFakes.QUICK_LAUNCH != null)
                                    MySessionLoader.UnloadAndExitToMenu();
                            }));
                    }
                    else
                    {
                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                            messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError),
                            messageText: MyTexts.Get(MyCommonTexts.DialogTextDownloadModsFailedSteamOffline),
                            buttonType: MyMessageBoxButtonsType.OK));
                    }
                   
                }
                MyLog.Default.WriteLine("LoadSession() - End");
            });

        }

        public static void StartLoading(Action loadingAction, string customLoadingBackground = null, string customLoadingtext = null)
        {
            MyAnalyticsHelper.LoadingStarted();
            var newGameplayScreen = new MyGuiScreenGamePlay();
            newGameplayScreen.OnLoadingAction += loadingAction;

            var loadScreen = new MyGuiScreenLoading(newGameplayScreen, MyGuiScreenGamePlay.Static, customLoadingBackground, customLoadingtext);
            loadScreen.OnScreenLoadingFinished += delegate
            {
                MyModAPIHelper.OnSessionLoaded();
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.HUDScreen));
            };
            MyGuiSandbox.AddScreen(loadScreen);
        }

        public static void UnloadAndExitToMenu()
        {
            MyScreenManager.CloseAllScreensNowExcept(null);
            MyGuiSandbox.Update(VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS);

            if (MySession.Static != null)
            {
                MySession.Static.Unload();
                MySession.Static = null;
            }

            if (MyMusicController.Static != null)
            {
                MyMusicController.Static.Unload();
                MyMusicController.Static = null;
                MyAudio.Static.MusicAllowed = true;
            }

            if(MyMultiplayer.Static != null)
            {
                MyMultiplayer.Static.Dispose();
            }

            //  This will quit actual game-play screen and move us to fly-through with main menu on top
            MyGuiSandbox.BackToMainMenu();
        }

        public static void ExitGame()
        {
            MyAnalyticsTracker.SendGameEnd("Exit to Windows", MySandboxGame.TotalTimeInMilliseconds / 1000);
            MyScreenManager.CloseAllScreensNowExcept(null);
            MySandboxGame.ExitThreadSafe();
        }
    }
}
