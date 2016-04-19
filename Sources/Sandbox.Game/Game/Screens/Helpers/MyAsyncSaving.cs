using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using ParallelTasks;

using Sandbox.Graphics.GUI;
using Sandbox.Engine.Utils;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using VRageMath;
using VRage.Utils;
using Sandbox.Game.Localization;
using VRage;
using VRage.Audio;

namespace Sandbox.Game.Screens.Helpers
{
    public static class MyAsyncSaving
    {
        private static Action m_callbackOnFinished;
        private static int m_inProgressCount;

        public static bool InProgress
        {
            get { return m_inProgressCount > 0; }
        }

        private static void PushInProgress()
        {
            ++m_inProgressCount;
        }

        private static void PopInProgress()
        {
            --m_inProgressCount;
        }

        public static void Start(Action callbackOnFinished = null,string customName =null)
        {
            PushInProgress();
            m_callbackOnFinished = callbackOnFinished;

            // CH: Uncomment to display rotating wheel while saving the game in the BG
            //MyHud.PushRotatingWheelVisible();

            MySessionSnapshot snapshot;
            bool success = MySession.Static.Save(out snapshot, customName);
            OnSnapshotDone(success, snapshot);
        }
        private static void OnSnapshotDone(bool snapshotSuccess, MySessionSnapshot snapshot)
        {
            if (snapshotSuccess)
            {
                if (!MySandboxGame.IsDedicated)
                {
                    var thumbPath = MySession.Static.ThumbPath;
                    try
                    {
                        if (File.Exists(thumbPath))
                            File.Delete(thumbPath);
                        MyGuiSandbox.TakeScreenshot(1200, 672, saveToPath: thumbPath, ignoreSprites: true, showNotification: false);
                    }
                    catch (Exception ex)
                    {
                        MySandboxGame.Log.WriteLine("Could not take session thumb screenshot. Exception:");
                        MySandboxGame.Log.WriteLine(ex);
                    }
                }

                snapshot.SaveParallel(completionCallback: () =>
                {
                    if (!MySandboxGame.IsDedicated)
                    {
                        // CH: Uncomment to display rotating wheel while saving the game in the BG
                        //MyHud.PopRotatingWheelVisible();

                        if (MySession.Static != null)
                        {
                            if (snapshot.SavingSuccess)
                            {
                                var notification = new MyHudNotification(MyCommonTexts.WorldSaved, 2500);
                                notification.SetTextFormatArguments(MySession.Static.Name);
                                MyHud.Notifications.Add(notification);
                            }
                            else
                            {
                                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                    messageText: new StringBuilder().AppendFormat(MyTexts.GetString(MyCommonTexts.WorldNotSaved), MySession.Static.Name),
                                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError)));
                            }
                        }
                    }

                    PopInProgress();
                });
            }
            else
            {
                if (!MySandboxGame.IsDedicated)
                {
                    MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                        messageText: new StringBuilder().AppendFormat(MyTexts.GetString(MyCommonTexts.WorldNotSaved), MySession.Static.Name),
                        messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError)));
                }

                PopInProgress();
            }

            if (m_callbackOnFinished != null)
                m_callbackOnFinished();

            m_callbackOnFinished = null;

            MyAudio.Static.Mute = false;
        }
    }
}
