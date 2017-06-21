#if !XB1
using ParallelTasks;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Graphics.GUI;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using VRage.FileSystem;
using VRage.Library.Utils;
using VRage.Utils;

namespace Sandbox.Engine.Networking
{
    class MySteamUGC
    {
        private static readonly string m_workshopModsPath = MyFileSystem.ModsPath;
        private static readonly string WORKSHOP_PREVIEW_FILENAME = "thumb.jpg";

        private static readonly string WORKSHOP_MOD_TAG = "mod";

        #region Publishing

        public static void PublishUGCAsync(string localModFolder,
            ulong? publishedFileId = null,
            PublishedFileVisibility visibility = PublishedFileVisibility.Private,
            Action<bool, ulong?> callbackOnFinished = null)
        {
            string[] tags = { WORKSHOP_MOD_TAG };

            MyGuiSandbox.AddScreen(new MyGuiScreenProgressAsync(MyCommonTexts.ProgressTextUploadingWorld,
                null,
                () => new PublishUGCResult(localModFolder, publishedFileId, visibility, tags, callbackOnFinished),
                endActionPublishUGC));
        }

        // HACK: internal class
        internal class PublishUGCResult : IMyAsyncResult
        {
            public Task Task
            {
                get;
                private set;
            }

            public ulong? PublishedFileId { get; private set; }

            public Action<bool, ulong?> CallbackOnFinished { get; private set; }

            public PublishUGCResult(string localFolder, ulong? publishedFileId, PublishedFileVisibility visibility, string[] tags, Action<bool, ulong?> callbackOnFinished)
            {
                CallbackOnFinished = callbackOnFinished;
                Task = Parallel.Start(() =>
                {
                    PublishedFileId = PublishUGCBlocking(localFolder, publishedFileId, visibility, tags);
                });
            }

            public bool IsCompleted { get { return this.Task.IsComplete; } }
        }

        private static void endActionPublishUGC(IMyAsyncResult iResult, MyGuiScreenProgressAsync screen)
        {
            screen.CloseScreenNow();
            PublishUGCResult result = (PublishUGCResult)iResult;
            result.CallbackOnFinished(result.PublishedFileId.HasValue, result.PublishedFileId);
        }

        private static ulong? PublishUpdateUGCBlocking(string localFolder, ulong? publishedFileId)
        {
            var success = false;
            ulong updateHandle;
            string path = Path.Combine(m_workshopModsPath, publishedFileId.Value.ToString());

            DirectoryCopy(localFolder, path, true);

            updateHandle = MySteam.API.UGC.StartItemUpdate(MySteam.AppId, publishedFileId.Value);
            if (!MySteam.API.UGC.IsUpdateHandleValid(updateHandle))
            {
                MySandboxGame.Log.WriteLine(string.Format("Error updating item, steam returned invalid update handle: '{0}'", localFolder));
                return null;
            }

            if (!MySteam.API.UGC.SetItemContent(updateHandle, path))
            {
                MySandboxGame.Log.WriteLine(string.Format("Error updating item, SetItemContent failed: '{0}'", localFolder));
                return null;
            }

            using (ManualResetEvent mrEvent = new ManualResetEvent(false))
            {
                MySteam.API.UGC.SubmitItemUpdate(updateHandle, "", delegate(bool ioFailure, SubmitItemUpdateResult data)
                {
                    success = !ioFailure && data.Result == Result.OK;
                    if (!success)
                        MySandboxGame.Log.WriteLine(string.Format("Error updating item: {0}", GetErrorString(ioFailure, data.Result)));
                    mrEvent.Set();
                });
                mrEvent.WaitOne();
            }
            return publishedFileId;
        }

        private static ulong? PublishNewUGCBlocking(string localFolder, PublishedFileVisibility visibility, string[] tags)
        {
            var success = false;
            ulong? publishedFileId = null;
            ulong updateHandle;

            using (ManualResetEvent mrEvent = new ManualResetEvent(false))
            {
                MySteam.API.UGC.CreateItem(MySteam.AppId, WorkshopFileType.Community, delegate(bool ioFailure, CreateItemResult data)
                {
                    success = !ioFailure && data.Result == Result.OK;
                    if (success)
                    {
                        publishedFileId = data.PublishedFileId;
                    }
                    else
                        MySandboxGame.Log.WriteLine(string.Format("Error creating new item: {0}", GetErrorString(ioFailure, data.Result)));
                    mrEvent.Set();
                });
                mrEvent.WaitOne();
            }
            if (!success || !publishedFileId.HasValue)
                return null;

            string path = Path.Combine(m_workshopModsPath, publishedFileId.Value.ToString());

            DirectoryCopy(localFolder, path, true);

            updateHandle = MySteam.API.UGC.StartItemUpdate(MySteam.AppId, publishedFileId.Value);
            if (!MySteam.API.UGC.IsUpdateHandleValid(updateHandle))
            {
                MySandboxGame.Log.WriteLine(string.Format("Error creating new item, steam returned invalid update handle: '{0}'", localFolder));
                return null;
            }

            var title = Path.GetFileName(localFolder);
            if (!MySteam.API.UGC.SetItemTitle(updateHandle, title))
            {
                MySandboxGame.Log.WriteLine(string.Format("Error creating new item, SetItemTitle failed: '{0}'", title));
                return null;
            }

            if (!MySteam.API.UGC.SetItemDescription(updateHandle, "lorem ipsum dolor sit amet"))
            {
                MySandboxGame.Log.WriteLine(string.Format("Error creating new item, SetItemTitle failed: '{0}'", title));
                return null;
            }

            if (!MySteam.API.UGC.SetItemVisibility(updateHandle, visibility))
            {
                MySandboxGame.Log.WriteLine(string.Format("Error creating new item, SetItemVisibility failed: '{0}'", visibility.ToString()));
                return null;
            }

            if (!MySteam.API.UGC.SetItemTags(updateHandle, tags))
            {
                MySandboxGame.Log.WriteLine(string.Format("Error creating new item, SetItemTags failed: '{0}'", String.Join(", ", tags)));
                return null;
            }

            if (!MySteam.API.UGC.SetItemContent(updateHandle, path))
            {
                MySandboxGame.Log.WriteLine(string.Format("Error creating new item, SetItemContent failed: '{0}'", localFolder));
                return null;
            }

            var previewFile = Path.Combine(path, WORKSHOP_PREVIEW_FILENAME);
            if (File.Exists(previewFile))
            {
                if (!MySteam.API.UGC.SetItemPreview(updateHandle, previewFile))
                {
                    MySandboxGame.Log.WriteLine(string.Format("Error creating new item, SetItemPreview failed: '{0}'", previewFile));
                    return null;
                }
            }

            using (ManualResetEvent mrEvent = new ManualResetEvent(false))
            {
                MySteam.API.UGC.SubmitItemUpdate(updateHandle, "", delegate(bool ioFailure, SubmitItemUpdateResult data)
                {
                    success = !ioFailure && data.Result == Result.OK;
                    if (!success)
                        MySandboxGame.Log.WriteLine(string.Format("Error creating new item: {0}", GetErrorString(ioFailure, data.Result)));
                    mrEvent.Set();
                });
                mrEvent.WaitOne();
            }

            return publishedFileId;
        }

        private static ulong? PublishUGCBlocking(string localFolder, ulong? publishedFileId, PublishedFileVisibility visibility, string[] tags)
        {
            if (!Directory.Exists(localFolder))
            {
                MySandboxGame.Log.WriteLine(string.Format("Error creating new item, directory does not exist: {0}", localFolder));
                return null;
            }

            if (publishedFileId.HasValue)
                return PublishUpdateUGCBlocking(localFolder, publishedFileId);
            else
                return PublishNewUGCBlocking(localFolder, visibility, tags);


            return null;
        }

        #endregion

        #region Downloading

        public static void DownloadUGCAsync(ulong? publishedFileId, Action<bool, ulong> callbackOnFinished = null)
        {
            if (!publishedFileId.HasValue)
                return;

            MyGuiSandbox.AddScreen(new MyGuiScreenProgressAsync(MyCommonTexts.ProgressTextDownloadingMods,
                null,
                () => new DownloadUGCResult(publishedFileId.Value, callbackOnFinished),
                endActionDownloadUGC));
        }

        // HACK: internal class
        internal class DownloadUGCResult : IMyAsyncResult
        {
            public Task Task
            {
                get;
                private set;
            }

            public ulong? PublishedFileId { get; private set; }

            public Action<bool, ulong> CallbackOnFinished { get; private set; }

            public DownloadUGCResult(ulong publishedFileId, Action<bool, ulong> callbackOnFinished)
            {
                CallbackOnFinished = callbackOnFinished;
                Task = Parallel.Start(() =>
                {
                    PublishedFileId = DownloadUGCBlocking(publishedFileId);
                });
            }

            public bool IsCompleted { get { return this.Task.IsComplete; } }
        }

        private static void endActionDownloadUGC(IMyAsyncResult iResult, MyGuiScreenProgressAsync screen)
        {
            screen.CloseScreenNow();
            DownloadUGCResult result = (DownloadUGCResult)iResult;
            result.CallbackOnFinished(result.PublishedFileId.HasValue, result.PublishedFileId.Value);
        }

        public static ulong? DownloadUGCBlocking(ulong publishedFileId)
        {
            var success = false;
            ulong? retVal = null;

            using (ManualResetEvent mrEvent = new ManualResetEvent(false))
            {
                MySteam.API.UGC.SubscribeItem(publishedFileId, delegate(bool ioFailure, RemoteStorageSubscribePublishedFileResult data)
                {
                    success = !ioFailure && data.Result == Result.OK;
                    if (success)
                        retVal = data.PublishedFileId;
                    else
                        MySandboxGame.Log.WriteLine(string.Format("Error downloading item: {0}", GetErrorString(ioFailure, data.Result)));
                    mrEvent.Set();
                });
                mrEvent.WaitOne();
            }

            return retVal;
        }

        #endregion

        #region util

        private static string GetErrorString(bool ioFailure, Result result)
        {
            return ioFailure ? "IO Failure" : result.ToString();
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!dir.Exists)
                return;

            // If the destination directory doesn't exist, create it. 
            if (Directory.Exists(destDirName))
            {
                Directory.Delete(destDirName, true);
            }
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location. 
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        #endregion

    }
}
#endif // !XB1
