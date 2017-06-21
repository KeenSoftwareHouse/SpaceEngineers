using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using ParallelTasks;

using Sandbox.Graphics.GUI;
using Sandbox.Engine.Utils;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using SteamSDK;

using VRage.Utils;
using System.Diagnostics;
using Sandbox.Common.ObjectBuilders;
using VRage.Compression;
using Sandbox.Game;
using Sandbox.Game.Localization;
using VRage.Library.Utils;
using VRage.FileSystem;
using VRage.Game;
using VRage.ObjectBuilders;
using VRage;

#if !XB1 // XB1_NOWORKSHOP
namespace Sandbox.Engine.Networking
{
    public class MySteamWorkshop
    {
        public struct Category
        {
            public string Id;
            public MyStringId LocalizableName;
        }

        public class SubscribedItem
        {
            public ulong PublishedFileId;
            public string Title;
            public string Description;
            public ulong UGCHandle;
            public ulong SteamIDOwner;
            public uint TimeUpdated;
            public string[] Tags;
        }

        public struct MyWorkshopPathInfo
        {
            public string Path;
            public string Suffix;
            public string NamePrefix;

            public static MyWorkshopPathInfo CreateWorldInfo()
            {
                var info = new MyWorkshopPathInfo();
                info.Path = m_workshopWorldsPath;
                info.Suffix = m_workshopWorldSuffix;
                info.NamePrefix = "Workshop";
                return info;
            }

            public static MyWorkshopPathInfo CreateScenarioInfo()
            {
                var info = new MyWorkshopPathInfo();
                info.Path = m_workshopScenariosPath;
                info.Suffix = m_workshopScenariosSuffix;
                info.NamePrefix = "Scenario";
                return info;
            }
        }

        private static readonly string m_workshopWorldsDir = "WorkshopWorlds";
        private static readonly string m_workshopWorldsPath = Path.Combine(MyFileSystem.UserDataPath, m_workshopWorldsDir);
        private static readonly string m_workshopWorldSuffix = ".sbw";

        private static readonly string m_workshopBlueprintsPath = Path.Combine(MyFileSystem.UserDataPath, "Blueprints", "workshop");
        private static readonly string m_workshopBlueprintSuffix = ".sbb";

        private static readonly string m_workshopScriptPath = Path.Combine(MyFileSystem.UserDataPath, MyGuiIngameScriptsPage.SCRIPTS_DIRECTORY, "workshop");

        private static readonly string m_workshopModsPath = MyFileSystem.ModsPath;
        private static readonly string m_workshopModSuffix = ".sbm";

        private static readonly string m_workshopScenariosPath = Path.Combine(MyFileSystem.UserDataPath, "Scenarios", "workshop");
        private static readonly string m_workshopScenariosSuffix = ".sbs";

        private static readonly string[] m_previewFileNames = { "thumb.png", MyTextConstants.SESSION_THUMB_NAME_AND_EXTENSION };

        private static readonly string[] m_ignoredExecutableExtensions = {
            ".action", ".apk", ".app", ".bat", ".bin", ".cmd", ".com", ".command", ".cpl", ".csh", ".dll", ".exe", ".gadget", ".inf1", ".ins", ".inx",
            ".ipa", ".isu", ".job", ".jse", ".ksh", ".lnk", ".msc", ".msi", ".msp", ".mst", ".osx", ".out", ".pif", ".paf", ".prg", ".ps1",
            ".reg", ".rgs", ".run", ".sct", ".shb", ".shs", ".so", ".u3p", ".vb", ".vbe", ".vbs", ".vbscript", ".workflow", ".ws", ".wsf",".suo"
        };

        private static volatile bool m_stop = false;

        private static readonly int m_bufferSize = 1 * 1024 * 1024; // buffer size for copying files
        private static byte[] buffer = new byte[m_bufferSize];
        private static Category[] m_modCategories;
        private static Category[] m_worldCategories;
        private static Category[] m_blueprintCategories;
        private static Category[] m_scenarioCategories;

        public static Category[] ModCategories { get { return m_modCategories; } }
        public static Category[] WorldCategories { get { return m_worldCategories; } }
        public static Category[] BlueprintCategories { get { return m_blueprintCategories; } }
        public static Category[] ScenarioCategories { get { return m_scenarioCategories; } }

        /// <summary>
        /// Do NOT change this value, as it would break worlds published to workshop!!!
        /// Tag for workshop items which contain world data.
        /// </summary>
        public const string WORKSHOP_DEVELOPMENT_TAG = "development";
        public const string WORKSHOP_WORLD_TAG = "world";
        public const string WORKSHOP_CAMPAIGN_TAG = "campaign";
        public const string WORKSHOP_MOD_TAG = "mod";
        public const string WORKSHOP_BLUEPRINT_TAG = "blueprint";
        public const string WORKSHOP_SCENARIO_TAG = "scenario";
        private const string WORKSHOP_INGAMESCRIPT_TAG = "ingameScript";

        static FastResourceLock m_modLock = new FastResourceLock();

        public static void Init(Category[] modCategories, Category[] worldCategories, Category[] blueprintCategories, Category[] scenarioCategories)
        {
            m_modCategories = modCategories;
            m_worldCategories = worldCategories;
            m_blueprintCategories = blueprintCategories;
            m_scenarioCategories = scenarioCategories;
        }

        #region Publishing
        private static Action<bool, Result, ulong> m_onPublishingFinished;
        private static bool m_publishSuccess;
        private static ulong m_publishedFileId;
        private static Result m_publishResult;
        private static MyGuiScreenProgressAsync m_asyncPublishScreen;

        public static void PublishModAsync(string localModFolder,
            string publishedTitle,
            string publishedDescription,
            ulong publishedFileId,
            string[] tags,
            PublishedFileVisibility visibility,
            Action<bool, Result, ulong> callbackOnFinished = null)
        {
            m_onPublishingFinished = callbackOnFinished;
            m_publishSuccess = false;
            m_publishedFileId = 0;
            m_publishResult = Result.Fail;

            string[] ignoredExtensions = { ".sbmi" };

            MyGuiSandbox.AddScreen(m_asyncPublishScreen = new MyGuiScreenProgressAsync(MyCommonTexts.ProgressTextUploadingWorld,
                null,
                () => new PublishItemResult(localModFolder, publishedTitle, publishedDescription, publishedFileId, visibility, tags, ignoredExtensions),
                endActionPublish));
        }

        public static ulong GetWorkshopIdFromLocalMod(string localModFolder)
        {
            var modInfoPath = Path.Combine(MyFileSystem.ModsPath, localModFolder, "modinfo.sbmi");
            MyObjectBuilder_ModInfo modInfo;
            if (File.Exists(modInfoPath) && MyObjectBuilderSerializer.DeserializeXML(modInfoPath, out modInfo))
                return modInfo.WorkshopId;
            return 0ul;
        }

        public static ulong GetSteamIDOwnerFromLocalMod(string localModFolder)
        {
            var modInfoPath = Path.Combine(MyFileSystem.ModsPath, localModFolder, "modinfo.sbmi");
            MyObjectBuilder_ModInfo modInfo;
            if (File.Exists(modInfoPath) && MyObjectBuilderSerializer.DeserializeXML(modInfoPath, out modInfo))
                return modInfo.SteamIDOwner;
            return 0ul;
        }

        public static void PublishWorldAsync(string localWorldFolder,
            string publishedTitle,
            string publishedDescription,
            ulong? publishedFileId,
            string[] tags,
            PublishedFileVisibility visibility,
            Action<bool, Result, ulong> callbackOnFinished = null)
        {
            m_onPublishingFinished = callbackOnFinished;
            m_publishSuccess = false;
            m_publishedFileId = 0;
            m_publishResult = Result.Fail;

            string[] ignoredExtensions = { ".xmlcache", ".png" };

            MyGuiSandbox.AddScreen(m_asyncPublishScreen = new MyGuiScreenProgressAsync(MyCommonTexts.ProgressTextUploadingWorld,
                null,
                () => new PublishItemResult(localWorldFolder, publishedTitle, publishedDescription, publishedFileId, visibility, tags, ignoredExtensions),
                endActionPublish));
        }

        public static void PublishBlueprintAsync(string localWorldFolder,
            string publishedTitle,
            string publishedDescription,
            ulong? publishedFileId,
            string[] tags,
            PublishedFileVisibility visibility,
            Action<bool, Result, ulong> callbackOnFinished = null)
        {
            m_onPublishingFinished = callbackOnFinished;
            m_publishSuccess = false;
            m_publishedFileId = 0;
            m_publishResult = Result.Fail;

            string[] ignoredExtensions = { };

            MyGuiSandbox.AddScreen(m_asyncPublishScreen = new MyGuiScreenProgressAsync(MyCommonTexts.ProgressTextUploadingWorld,
                null,
                () => new PublishItemResult(localWorldFolder, publishedTitle, publishedDescription, publishedFileId, visibility, tags, ignoredExtensions),
                endActionPublish));
        }

        public static void PublishScenarioAsync(string localWorldFolder,
            string publishedTitle,
            string publishedDescription,
            ulong? publishedFileId,
            //string[] tags,
            PublishedFileVisibility visibility,
            Action<bool, Result, ulong> callbackOnFinished = null)
        {
            m_onPublishingFinished = callbackOnFinished;
            m_publishSuccess = false;
            m_publishedFileId = 0;
            m_publishResult = Result.Fail;

            string[] ignoredExtensions = { };
            string[] tags = { WORKSHOP_SCENARIO_TAG };
            MyGuiSandbox.AddScreen(m_asyncPublishScreen = new MyGuiScreenProgressAsync(MyCommonTexts.ProgressTextUploadingWorld,
                null,
                () => new PublishItemResult(localWorldFolder, publishedTitle, publishedDescription, publishedFileId, visibility, tags, ignoredExtensions),
                endActionPublish));
        }

        public static void PublishIngameScriptAsync(string localWorldFolder,
           string publishedTitle,
           string publishedDescription,
           ulong? publishedFileId,
           PublishedFileVisibility visibility,
           Action<bool, Result, ulong> callbackOnFinished = null)
        {
            m_onPublishingFinished = callbackOnFinished;
            m_publishSuccess = false;
            m_publishedFileId = 0;
            m_publishResult = Result.Fail;

            string[] tags = { WORKSHOP_INGAMESCRIPT_TAG };
            string[] ignoredExtensions = { ".sbmi",".png",".jpg"};

            MyGuiSandbox.AddScreen(m_asyncPublishScreen = new MyGuiScreenProgressAsync(MyCommonTexts.ProgressTextUploadingWorld,
                null,
                () => new PublishItemResult(localWorldFolder, publishedTitle, publishedDescription, publishedFileId, visibility, tags, ignoredExtensions),
                endActionPublish));
        }

        /// <summary>
        /// Do NOT call this method from update thread. Use PublishWorldAsync or worker thread, otherwise it will block update.
        /// </summary>
        private static ulong PublishItemBlocking(string localFolder, string publishedTitle, string publishedDescription, ulong? workshopId, PublishedFileVisibility visibility, string[] tags, string[] ignoredExtensions)
        {
            MySandboxGame.Log.WriteLine("PublishItemBlocking - START");
            MySandboxGame.Log.IncreaseIndent();

            if (tags.Length == 0)
            {
                MySandboxGame.Log.WriteLine("Error: Can not publish with no tags!");
                MySandboxGame.Log.DecreaseIndent();
                MySandboxGame.Log.WriteLine("PublishItemBlocking - END");
                return 0;
            }

            int totalBytes = 0;
            int availableBytes = 0;

            SteamAPI.Instance.RemoteStorage.GetQuota(out totalBytes, out availableBytes);
            MySandboxGame.Log.WriteLine(string.Format("Quota: total = {0}, available = {1}", totalBytes, availableBytes));

            int totalCloudFiles = SteamAPI.Instance.RemoteStorage.GetFileCount();

            MySandboxGame.Log.WriteLine(string.Format("Listing cloud {0} files", totalCloudFiles));
            MySandboxGame.Log.IncreaseIndent();
            for (int i = 0; i < totalCloudFiles; ++i)
            {
                int fileSize = 0;
                string fileName = SteamAPI.Instance.RemoteStorage.GetFileNameAndSize(i, out fileSize);
                bool persisted = SteamAPI.Instance.RemoteStorage.FilePersisted(fileName);
                bool forgot = false;

                if (persisted && fileName.StartsWith("tmp") && fileName.EndsWith(".tmp")) // dont sync useless temp files
                {
                    forgot = SteamAPI.Instance.RemoteStorage.FileForget(fileName);
                }
                MySandboxGame.Log.WriteLine(string.Format("'{0}', {1}B, {2}, {3}", fileName, fileSize, persisted, forgot));
            }
            MySandboxGame.Log.DecreaseIndent();

            SteamAPI.Instance.RemoteStorage.GetQuota(out totalBytes, out availableBytes);
            MySandboxGame.Log.WriteLine(string.Format("Quota: total = {0}, available = {1}", totalBytes, availableBytes));

            ulong publishedFileId = 0;

            var steam = MySteam.API;

            string steamItemFileName;
            string steamPreviewFileName = "";

            MySandboxGame.Log.WriteLine("Packing Item - START");
            var tempFileFullPath = Path.GetTempFileName();

            try
            {
                string[] allIgnoredExtensions = new string[m_ignoredExecutableExtensions.Length + ignoredExtensions.Length];
                ignoredExtensions.CopyTo(allIgnoredExtensions, 0);
                m_ignoredExecutableExtensions.CopyTo(allIgnoredExtensions, ignoredExtensions.Length);
                MyZipArchive.CreateFromDirectory(localFolder, tempFileFullPath, DeflateOptionEnum.Maximum, false, allIgnoredExtensions,false);
            }
            catch (Exception e)
            {
                MySandboxGame.Log.WriteLine(string.Format("Packing file failed: source = '{0}', destination = '{1}', error: {2}", localFolder, tempFileFullPath, e));
                MySandboxGame.Log.DecreaseIndent();
                return 0ul;
            }

            MySandboxGame.Log.WriteLine("Packing Item - END");

            steamItemFileName = WriteAndShareFileBlocking(tempFileFullPath);
            File.Delete(tempFileFullPath);
            if (steamItemFileName == null || steamItemFileName.Equals("FileNotFound"))
            {
                MySandboxGame.Log.DecreaseIndent();
                return 0;
            }

            foreach (var previewFileName in m_previewFileNames)
            {
                var localPreviewFileFullPath = Path.Combine(localFolder, previewFileName);
                if (File.Exists(localPreviewFileFullPath))
                {
                    steamPreviewFileName = WriteAndShareFileBlocking(localPreviewFileFullPath);
                    if (steamPreviewFileName == null)
                    {
                        MySandboxGame.Log.WriteLine(string.Format("Could not share preview file = '{0}'", localPreviewFileFullPath));
                        MySandboxGame.Log.DecreaseIndent();
                        return 0;
                    }
                    break;
                }
            }

            MySandboxGame.Log.WriteLine("Publishing - START");
            using (var mrEvent = new ManualResetEvent(false))
            {
                // Update item if it has already been published, otherwise publish it.
                bool publishedFileNotFound = true;
                if (workshopId.HasValue && workshopId != 0)
                {
                    MySandboxGame.Log.WriteLine("File appears to be published already. Attempting to update workshop file.");
                    ulong updateHandle = steam.RemoteStorage.CreatePublishedFileUpdateRequest(workshopId.Value);
                    steam.RemoteStorage.UpdatePublishedFileTags(updateHandle, tags);
                    steam.RemoteStorage.UpdatePublishedFileFile(updateHandle, steamItemFileName);
                    if (steamPreviewFileName.Equals("FileNotFound") == false)
                        steam.RemoteStorage.UpdatePublishedFilePreviewFile(updateHandle, steamPreviewFileName);
                    steam.RemoteStorage.CommitPublishedFileUpdate(updateHandle, delegate(bool ioFailure, RemoteStorageUpdatePublishedFileResult data)
                    {
                        m_publishResult = data.Result;
                        bool success = !ioFailure && data.Result == Result.OK;
                        if (success)
                            MySandboxGame.Log.WriteLine("Published file update successful");
                        else
                            MySandboxGame.Log.WriteLine(string.Format("Error during publishing: {0}", GetErrorString(ioFailure, data.Result)));
                        m_publishSuccess = success;
                        publishedFileId = data.PublishedFileId;
                        publishedFileNotFound = data.Result == Result.FileNotFound;
                        mrEvent.Set();
                    });
                    mrEvent.WaitOne();
                    mrEvent.Reset();
                }

                if (publishedFileNotFound)
                {
                    MySandboxGame.Log.WriteLine("Published file was not found. Publishing.");

                    steam.RemoteStorage.PublishWorkshopFile(steamItemFileName, steamPreviewFileName, MySteam.AppId, publishedTitle, publishedDescription, publishedDescription, visibility, tags,
                        delegate(bool ioFailure, RemoteStoragePublishFileResult data)
                        {
                            m_publishResult = data.Result;
                            m_publishSuccess = !ioFailure && data.Result == Result.OK;
                            if (m_publishSuccess)
                                MySandboxGame.Log.WriteLine("Publishing successful");
                            else
                                MySandboxGame.Log.WriteLine(string.Format("Error during publishing: {0}", GetErrorString(ioFailure, data.Result)));
                            publishedFileId = data.PublishedFileId;
                            mrEvent.Set();
                        });
                    mrEvent.WaitOne();
                    mrEvent.Reset();

                    if (m_publishSuccess)
                    {
                        MySandboxGame.Log.WriteLine("Subscribing to published file");
                        steam.RemoteStorage.SubscribePublishedFile(publishedFileId,
                            delegate(bool ioFailure, RemoteStorageSubscribePublishedFileResult data)
                            {
                                var success = !ioFailure && data.Result == Result.OK;
                                if (success)
                                    MySandboxGame.Log.WriteLine("Subscribing successful");
                                else
                                    MySandboxGame.Log.WriteLine(string.Format("Subscribing failed: id={0}, error={1}", publishedFileId, GetErrorString(ioFailure, data.Result)));
                                mrEvent.Set();
                            });
                        mrEvent.WaitOne();
                    }
                }
            }
            MySandboxGame.Log.WriteLine("Publishing - END");

            // Erasing temporary file. No need for it to take up cloud storage anymore.
            MySandboxGame.Log.WriteLine("Deleting cloud files - START");
            steam.RemoteStorage.FileDelete(steamItemFileName);
            if (steamPreviewFileName.Equals("FileNotFound") == false)
                steam.RemoteStorage.FileDelete(steamPreviewFileName);
            MySandboxGame.Log.WriteLine("Deleting cloud files - END");

            MySandboxGame.Log.DecreaseIndent();
            MySandboxGame.Log.WriteLine("PublishItemBlocking - END");

            return publishedFileId;
        }

        private static string WriteAndShareFileBlocking(string localFileFullPath)
        {
            var steam = MySteam.API;
            var steamFileName = Path.GetFileName(localFileFullPath).ToLower();
            MySandboxGame.Log.WriteLine(string.Format("Writing and sharing file '{0}' - START", steamFileName));

            if (!steam.IsOnline())
                return null;


            using (var fs = new FileStream(localFileFullPath, FileMode.Open, FileAccess.Read))
            {
                ulong handle = steam.RemoteStorage.FileWriteStreamOpen(steamFileName);
                byte[] buffer = new byte[m_bufferSize];
                int bytesRead = 0;
                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                    steam.RemoteStorage.FileWriteStreamWriteChunk(handle, buffer, bytesRead);
                steam.RemoteStorage.FileWriteStreamClose(handle);
            }


            bool fileShareSuccess = false;

            Result result = Result.Fail;
            using (var mrEvent = new ManualResetEvent(false))
            {
                steam.RemoteStorage.FileShare(steamFileName, delegate(bool ioFailure, RemoteStorageFileShareResult data)
                {
                    fileShareSuccess = !ioFailure && data.Result == Result.OK;
                    result = data.Result;
                    if (fileShareSuccess)
                        m_asyncPublishScreen.ProgressText = MyCommonTexts.ProgressTextPublishingWorld;
                    else
                        MySandboxGame.Log.WriteLine(string.Format("Error sharing the file: {0}", GetErrorString(ioFailure, data.Result)));
                    mrEvent.Set();
                });
                mrEvent.WaitOne();
                mrEvent.Reset();
            }

            MySandboxGame.Log.WriteLine(string.Format("Writing and sharing file '{0}' - END", steamFileName));

            if (!fileShareSuccess && result != Result.FileNotFound)
                return null;
            else if (result == Result.FileNotFound)
                return result.ToString();

            return steamFileName;
        }

        private static void endActionPublish(IMyAsyncResult result, MyGuiScreenProgressAsync screen)
        {
            screen.CloseScreenNow();
            if (m_onPublishingFinished != null)
                m_onPublishingFinished(m_publishSuccess, m_publishResult, m_publishedFileId);
            m_publishSuccess = false;
            m_publishResult = Result.Fail;
            m_onPublishingFinished = null;
            m_asyncPublishScreen = null;
        }

        // HACK: internal class
        internal class PublishItemResult : IMyAsyncResult
        {
            public Task Task
            {
                get;
                private set;
            }

            public PublishItemResult(string localFolder, string publishedTitle, string publishedDescription, ulong? publishedFileId, PublishedFileVisibility visibility, string[] tags, string[] ignoredExtensions)
            {
                if (MyFinalBuildConstants.IS_STABLE == false && tags.Contains(WORKSHOP_DEVELOPMENT_TAG) == false)
                {
                    Array.Resize(ref tags, tags.Length + 1);
                    tags[tags.Length - 1] = WORKSHOP_DEVELOPMENT_TAG;
                }

                Task = Parallel.Start(() =>
                {
                    m_publishedFileId = PublishItemBlocking(localFolder, publishedTitle, publishedDescription, publishedFileId, visibility, tags, ignoredExtensions);
                });
            }

            public bool IsCompleted { get { return this.Task.IsComplete; } }
        }
        #endregion

        #region Subscribed list retrieval
        /// <summary>
        /// Do NOT call this method from update thread.
        /// </summary>
        public static bool GetSubscribedWorldsBlocking(List<SubscribedItem> results)
        {
            MySandboxGame.Log.WriteLine("MySteamWorkshop.GetSubscribedWorldsBlocking - START");
            try
            {
                return GetSubscribedItemsBlocking(results, WORKSHOP_WORLD_TAG);
            }
            finally
            {
                MySandboxGame.Log.WriteLine("MySteamWorkshop.GetSubscribedWorldsBlocking - END");
            }
        }

        /// <summary>
        /// Do NOT call this method from update thread.
        /// </summary>
        public static bool GetSubscribedCampaignsBlocking(List<SubscribedItem> results)
        {
            MySandboxGame.Log.WriteLine("MySteamWorkshop.GetSubscribedWorldsBlocking - START");
            try
            {
                return GetSubscribedItemsBlocking(results, WORKSHOP_CAMPAIGN_TAG);
            }
            finally
            {
                MySandboxGame.Log.WriteLine("MySteamWorkshop.GetSubscribedWorldsBlocking - END");
            }
        }

        /// <summary>
        /// Do NOT call this method from update thread.
        /// </summary>
        public static bool GetSubscribedModsBlocking(List<SubscribedItem> results)
        {
            MySandboxGame.Log.WriteLine("MySteamWorkshop.GetSubscribedModsBlocking - START");
            try
            {
                return GetSubscribedItemsBlocking(results, WORKSHOP_MOD_TAG);
            }
            finally
            {
                MySandboxGame.Log.WriteLine("MySteamWorkshop.GetSubscribedModsBlocking - END");
            }
        }


        /// <summary>
        /// Do NOT call this method from update thread.
        /// </summary>
        public static bool GetSubscribedScenariosBlocking(List<SubscribedItem> results)
        {
            MySandboxGame.Log.WriteLine("MySteamWorkshop.GetSubscribedScenariosBlocking - START");
            try
            {
                return GetSubscribedItemsBlocking(results, WORKSHOP_SCENARIO_TAG);
            }
            finally
            {
                MySandboxGame.Log.WriteLine("MySteamWorkshop.GetSubscribedScenariosBlocking - END");
            }
        }

        /// <summary>
        /// Do NOT call this method from update thread.
        /// </summary>
        public static bool GetSubscribedBlueprintsBlocking(List<SubscribedItem> results)
        {
            MySandboxGame.Log.WriteLine("MySteamWorkshop.GetSubscribedModsBlocking - START");
            try
            {
                return GetSubscribedItemsBlocking(results, WORKSHOP_BLUEPRINT_TAG);
            }
            finally
            {
                MySandboxGame.Log.WriteLine("MySteamWorkshop.GetSubscribedModsBlocking - END");
            }
        }

        public static bool GetSubscribedIngameScriptsBlocking(List<SubscribedItem> results)
        {
            MySandboxGame.Log.WriteLine("MySteamWorkshop.GetSubscribedModsBlocking - START");
            try
            {
                return GetSubscribedItemsBlocking(results, WORKSHOP_INGAMESCRIPT_TAG);
            }
            finally
            {
                MySandboxGame.Log.WriteLine("MySteamWorkshop.GetSubscribedModsBlocking - END");
            }
        }
        public static bool GetItemsBlocking(List<SubscribedItem> results, IEnumerable<ulong> publishedFileIds)
        {
            MySandboxGame.Log.WriteLine(string.Format("MySteamWorkshop.GetItemsBlocking: getting {0} items", publishedFileIds.Count()));
            results.Clear();

            if (publishedFileIds.Count() == 0)
                return true;

            Dictionary<ulong, SubscribedItem> resultsByPublishedId = new Dictionary<ulong, SubscribedItem>(publishedFileIds.Count());
            using (ManualResetEvent mrEvent = new ManualResetEvent(false))
            {
                bool hasDuplicates = false;
                foreach (var id in publishedFileIds)
                {
                    if (!resultsByPublishedId.ContainsKey(id))
                    {
                        resultsByPublishedId.Add(id, new SubscribedItem() { PublishedFileId = id });
                    }
                    else
                    {
                        MySandboxGame.Log.WriteLine(string.Format("MySteamWorkshop.GetItemsBlocking: Duplicate entry for item with id {0}", id));
                        hasDuplicates = true;
                    }
                }

                // If we have duplicates, return false.
                // This return is delayed so that all duplicate mods get listed, this way the admin can find out which are duplicate.
                if (hasDuplicates) return false;

                // Retrieve details for each subscription.
                int callResultCounter = 0;
                int expectedResults = resultsByPublishedId.Count;
                Action<bool, RemoteStorageGetPublishedFileDetailsResult> onGetDetailsCallResult = delegate(bool ioFailure, RemoteStorageGetPublishedFileDetailsResult data)
                {
                    MySandboxGame.Log.WriteLine(string.Format("Obtained details: Id={4}; Result={0}; ugcHandle={1}; title='{2}'; tags='{3}'", data.Result, data.FileHandle, data.Title, data.Tags, data.PublishedFileId));
                    if (!ioFailure && data.Result == Result.OK && data.Tags.Length != 0)
                    {
                        resultsByPublishedId[data.PublishedFileId].Title = data.Title;
                        resultsByPublishedId[data.PublishedFileId].Description = data.Description;
                        resultsByPublishedId[data.PublishedFileId].UGCHandle = data.FileHandle;
                        resultsByPublishedId[data.PublishedFileId].SteamIDOwner = data.SteamIDOwner;
                        resultsByPublishedId[data.PublishedFileId].TimeUpdated = data.TimeUpdated;
                        resultsByPublishedId[data.PublishedFileId].Tags = data.Tags.Split(',');
                        ++callResultCounter;
                    }
                    else
                    {
                        bool removed = resultsByPublishedId.Remove(data.PublishedFileId);
                        if (!removed)
                        {
                            MySandboxGame.Log.WriteLine(string.Format("Nonexistent PublishedFileId reported in error. Id={0}, Error={1}",
                                data.PublishedFileId, GetErrorString(ioFailure, data.Result)));
                        }
                        // Workaround for when steam doesn't report correct published file id and thread hangs as a result.
                        expectedResults -= 1;
                    }

                    if (callResultCounter == expectedResults)
                    {
                        try { mrEvent.Set(); }
                        catch (System.ObjectDisposedException ex)
                        {
                            MySandboxGame.Log.WriteLine(ex);
                        }
                    }
                };

                foreach (var resultEntry in resultsByPublishedId)
                {
                    if (!MySteam.IsOnline)
                        return false;

                    MySandboxGame.Log.WriteLine(string.Format("Querying details of file " + resultEntry.Value.PublishedFileId));
                    MySteam.API.RemoteStorage.GetPublishedFileDetails(resultEntry.Value.PublishedFileId, 0, onGetDetailsCallResult);
                }

                if (!mrEvent.WaitOne(60000))
                {
                    Debug.Fail("Couldn't obtain all results before timeout.");
                }

                if (expectedResults != resultsByPublishedId.Count)
                {// Steam reported some wrong IDs so I have to search for those missing title (and other data) and remove them here
                    var listToRemove = new List<UInt64>(resultsByPublishedId.Count - expectedResults);
                    foreach (var entry in resultsByPublishedId)
                    {
                        if (entry.Value.Title == null)
                            listToRemove.Add(entry.Key);
                    }

                    StringBuilder sb = new StringBuilder();
                    foreach (var id in listToRemove)
                    {
                        sb.Append(id).Append(", ");
                        resultsByPublishedId.Remove(id);
                    }
                    MySandboxGame.Log.WriteLine(string.Format("Ids messed up by Steam: {0}", sb.ToString()));
                }

                results.InsertRange(0, resultsByPublishedId.Values);
            }

            return true;
        }

        /// <summary>
        /// Do NOT call this method from update thread.
        /// </summary>
        private static bool GetSubscribedItemsBlocking(List<SubscribedItem> results, string tag)
        {
            results.Clear();
            Dictionary<ulong, SubscribedItem> resultsByPublishedId = new Dictionary<ulong, SubscribedItem>();
            using (ManualResetEvent mrEvent = new ManualResetEvent(false))
            {
                if (MyPerGameSettings.WorkshopUseUGCEnumerate) //temporary because of UGC bug
                {
                    uint num = MySteam.API.UGC.GetNumSubscribedItems();
                    if (num == 0)
                        return true;
                    ulong[] ids = new ulong[num];
                    MySandboxGame.Log.WriteLine(string.Format("Asking steam for {0} subscribed items", num));
                    var ret = MySteam.API.UGC.GetSubscribedItems(ids, num);
                    MySandboxGame.Log.WriteLine(string.Format("Steam returned {0} subscribed items", ret));

                    if (ret == 0)
                        return true;

                    foreach (var id in ids)
                    {
                        resultsByPublishedId[id] = new SubscribedItem() { PublishedFileId = id };
                    }
                }
                else // UGC crashes for ME workshop probably because its private
                {
                    int processedCount = 0;
                    int totalItems = 0;
                    // Retrieve PublishedFileId for each subscription.
                    Action<bool, RemoteStorageEnumerateUserSubscribedFilesResult> onEnumerateCallResult = delegate(bool ioFailure, RemoteStorageEnumerateUserSubscribedFilesResult data)
                    {
                        if (!ioFailure && data.Result == Result.OK)
                        {
                            StringBuilder buffer = new StringBuilder("Obtained subscribed files: ");
                            processedCount += data.ResultsReturned;
                            totalItems = data.TotalResultCount;
                            for (int i = 0; i < data.ResultsReturned; ++i)
                            {
                                buffer.Append(data[i]).Append(',');
                                resultsByPublishedId[data[i]] = new SubscribedItem() { PublishedFileId = data[i] };
                            }
                            MySandboxGame.Log.WriteLine(string.Format("ResultsReturned = {0}, processedCount = {1}, totalItems = {2}", data.ResultsReturned, processedCount, totalItems));
                            MySandboxGame.Log.WriteLine(buffer.ToString());
                        }
                        else
                        {
                            totalItems = -1;
                            MySandboxGame.Log.WriteLine(string.Format("Error enumerating user subscribed files. {0}", GetErrorString(ioFailure, data.Result)));
                        }

                        mrEvent.Set();
                    };

                    do
                    {
                        if (!MySteam.IsOnline)
                            return false;

                        MySteam.API.RemoteStorage.EnumerateUserSubscribedFiles((uint)processedCount, onEnumerateCallResult);

                        MySandboxGame.Log.WriteLine(string.Format("Waiting for steam response. processedCount = {0}, totalItems = {1}", processedCount, totalItems));
                        if (!mrEvent.WaitOne(30 * 1000)) // timeout
                            return false;
                        MySandboxGame.Log.WriteLine(string.Format("Got response from steam.    processedCount = {0}, totalItems = {1}", processedCount, totalItems));
                        mrEvent.Reset();
                    }
                    while (processedCount < totalItems);

                    if (totalItems == -1)
                        return false;

                    if (totalItems == 0)
                        return true;
                }

                // Retrieve details for each subscription.
                int callResultCounter = 0;
                int expectedResults = resultsByPublishedId.Count;
                var listToRemove = new List<UInt64>(resultsByPublishedId.Count - expectedResults);
                Action<bool, RemoteStorageGetPublishedFileDetailsResult> onGetDetailsCallResult = delegate(bool ioFailure, RemoteStorageGetPublishedFileDetailsResult data)
                {
                    MySandboxGame.Log.WriteLine(string.Format("Obtained details: Id={4}; Result={0}; ugcHandle={1}; title='{2}'; tags='{3}'", data.Result, data.FileHandle, data.Title, data.Tags, data.PublishedFileId));
                    if (!ioFailure && data.Result == Result.OK && data.Tags.ToLowerInvariant().Split(',').Contains(tag.ToLowerInvariant()))
                    {
                        resultsByPublishedId[data.PublishedFileId].Title = data.Title;
                        resultsByPublishedId[data.PublishedFileId].Description = data.Description;
                        resultsByPublishedId[data.PublishedFileId].UGCHandle = data.FileHandle;
                        resultsByPublishedId[data.PublishedFileId].SteamIDOwner = data.SteamIDOwner;
                        resultsByPublishedId[data.PublishedFileId].TimeUpdated = data.TimeUpdated;
                        resultsByPublishedId[data.PublishedFileId].Tags = data.Tags.Split(',');
                        ++callResultCounter;
                    }
                    else
                    {
                        bool exists = resultsByPublishedId.ContainsKey(data.PublishedFileId);
                        if (exists)
                        {
                            listToRemove.Add(data.PublishedFileId);
                        }
                        else
                        {
                            MySandboxGame.Log.WriteLine(string.Format("Nonexistent PublishedFileId reported in error. Id={0}, Error={1}",
                                data.PublishedFileId, GetErrorString(ioFailure, data.Result)));
                        }
                        // Workaround for when steam doesn't report correct published file id and thread hangs as a result.
                        expectedResults = expectedResults - 1;
                    }

                    if (callResultCounter == expectedResults)
                    {
                        try { mrEvent.Set(); }
                        catch (System.ObjectDisposedException ex)
                        {
                            MySandboxGame.Log.WriteLine(ex);
                        }
                    }
                };

                foreach (var resultEntry in resultsByPublishedId)
                {
                    if (!MySteam.IsOnline)
                        return false;

                    MySandboxGame.Log.WriteLine(string.Format("Querying details of file " + resultEntry.Value.PublishedFileId));
                    MySteam.API.RemoteStorage.GetPublishedFileDetails(resultEntry.Value.PublishedFileId, 0, onGetDetailsCallResult);
                }

                if (!mrEvent.WaitOne(60000))
                {
                    Debug.Fail("Couldn't obtain all results before timeout.");
                }

                foreach (var item in listToRemove)
                    resultsByPublishedId.Remove(item);
                listToRemove.Clear();

                if (expectedResults != resultsByPublishedId.Count)
                {// Steam reported some wrong IDs so I have to search for those missing title (and other data) and remove them here
                    int newCapacity = resultsByPublishedId.Count - expectedResults;
                    if (listToRemove.Capacity < newCapacity)
                        listToRemove.Capacity = newCapacity;
                    foreach (var entry in resultsByPublishedId)
                    {
                        if (entry.Value.Title == null)
                            listToRemove.Add(entry.Key);
                    }

                    StringBuilder sb = new StringBuilder();
                    foreach (var id in listToRemove)
                    {
                        sb.Append(id).Append(", ");
                        resultsByPublishedId.Remove(id);
                    }
                    MySandboxGame.Log.WriteLine(string.Format("Ids messed up by Steam: {0}", sb.ToString()));
                }

                results.InsertRange(0, resultsByPublishedId.Values);
            }

            return true;
        }

        #endregion

        #region Download

        private static MyGuiScreenProgressAsync m_asyncDownloadScreen;

        /// <summary>
        /// Do NOT call this method from update thread.
        /// </summary>
        private static bool DownloadItemBlocking(string localFullPath, ulong UGCHandle)
        {
            if (m_stop)
                return false;

            bool downloadSuccess = false;
            int downloadSizeInBytes = 0;
            using (ManualResetEvent mrEvent = new ManualResetEvent(false))
            {
                MySteam.API.RemoteStorage.UGCDownload(UGCHandle, 0, delegate(bool ioFailure, RemoteStorageDownloadUGCResult data)
                {
                    downloadSuccess = !ioFailure && data.Result == Result.OK;
                    if (downloadSuccess)
                    {
                        MySandboxGame.Log.WriteLine(string.Format("Downloaded file: ugcHandle={0}; size={1}B", data.FileHandle, data.SizeInBytes));
                    }
                    else
                    {
                        MySandboxGame.Log.WriteLine(string.Format("Error downloading file: {0}", GetErrorString(ioFailure, data.Result)));
                    }
                    downloadSizeInBytes = data.SizeInBytes;
                    mrEvent.Set();
                });

                mrEvent.WaitOne();
            }

            if (m_stop)
                return false;

            if (!downloadSuccess)
                return false;

            // File I/O should always be exception handled
            try
            {
                using (var fs = new FileStream(localFullPath, FileMode.Create, FileAccess.Write))
                {
                    for (uint offset = 0; offset < downloadSizeInBytes; offset += (uint)m_bufferSize)
                    {
                        int bytesRead = MySteam.API.RemoteStorage.UGCRead(UGCHandle, buffer, m_bufferSize, offset);
                        fs.Write(buffer, 0, bytesRead);
                    }
                }
            }
            catch (Exception ex)
            {
                MySandboxGame.Log.WriteLine(string.Format("Error downloading file: {0}, {1}", localFullPath, ex.Message));
                return false;
            }

            return true;
        }

        public static void DownloadModsAsync(List<MyObjectBuilder_Checkpoint.ModItem> mods, Action<bool,string> onFinishedCallback, Action onCancelledCallback = null)
        {
            if (mods == null || mods.Count == 0)
            {
                onFinishedCallback(true,"");
                return;
            }

            if (!Directory.Exists(m_workshopModsPath))
                Directory.CreateDirectory(m_workshopModsPath);

            m_asyncDownloadScreen = new MyGuiScreenProgressAsync(MyCommonTexts.ProgressTextCheckingMods,
                MyCommonTexts.Cancel,
                () => new DownloadModsResult(mods, onFinishedCallback),
                endActionDownloadMods);

            m_asyncDownloadScreen.ProgressCancelled += () =>
            {
                m_stop = true;
                if (onCancelledCallback != null)
                    onCancelledCallback();
            };

            MyGuiSandbox.AddScreen(m_asyncDownloadScreen);
        }

        public struct ResultData
        {
            public bool Success;
            public string MismatchMods;
        }

        class DownloadModsResult : IMyAsyncResult
        {
            public Task Task
            {
                get;
                private set;
            }

            public ResultData Result;

            public Action<bool,string> callback;

            public DownloadModsResult(List<MyObjectBuilder_Checkpoint.ModItem> mods, Action<bool,string> onFinishedCallback)
            {
                callback = onFinishedCallback;
                Task = Parallel.Start(() =>
                {
                    Result = DownloadWorldModsBlocking(mods);
                });
            }

            public bool IsCompleted
            {
                get
                {
                    return this.Task.IsComplete;
                }
            }
        }

        static void endActionDownloadMods(IMyAsyncResult iResult, MyGuiScreenProgressAsync screen)
        {
            screen.CloseScreen();

            var result = (DownloadModsResult)iResult;

            if (!result.Result.Success)
            {
                MySandboxGame.Log.WriteLine(string.Format("Error downloading mods"));
            }
            result.callback(result.Result.Success,result.Result.MismatchMods);
        }

        /// <summary>
        /// Do NOT call this method from update thread.
        /// </summary>
        public static ResultData DownloadModsBlocking(List<SubscribedItem> mods)
        {
            int counter = 0;
            string numMods = mods.Count.ToString();
            VRage.Collections.CachingList<SubscribedItem> failedMods = new VRage.Collections.CachingList<SubscribedItem>();
            VRage.Collections.CachingList<SubscribedItem> mismatchMods = new VRage.Collections.CachingList<SubscribedItem>();
            bool downloadingFailed = false;

            long startTime = Stopwatch.GetTimestamp();
            Parallel.ForEach<SubscribedItem>(mods, delegate(SubscribedItem mod)
            {
                if (!MySteam.IsOnline)
                {
                    downloadingFailed = true;
                    return;
                }

                if (m_stop)
                {
                    downloadingFailed = true;
                    return;
                }


                bool devTagMismatch = mod.Tags != null && mod.Tags.Contains(MySteamWorkshop.WORKSHOP_DEVELOPMENT_TAG) && MyFinalBuildConstants.IS_STABLE;

                if (devTagMismatch)
                {
                    mismatchMods.Add(mod);
                }

                var localPackedModFullPath = Path.Combine(m_workshopModsPath, mod.PublishedFileId + m_workshopModSuffix);

                // If mod is up to date, no need to download it.
                if (!IsModUpToDateBlocking(localPackedModFullPath, mod, true))
                {
                    // If the mod fails to download, we need to flag it for failure, log it, then stop
                    if (!DownloadItemBlocking(localPackedModFullPath, mod.UGCHandle))
                    {
                        failedMods.Add(mod);
                        downloadingFailed = true;
                        m_stop = true;
                    }
                }
                else
                {
                    MySandboxGame.Log.WriteLineAndConsole(string.Format("Up to date mod: Id = {0}, title = '{1}'", mod.PublishedFileId, mod.Title));
                }

                if (m_asyncDownloadScreen != null)
                {
                    using (m_modLock.AcquireExclusiveUsing())
                    {
                        counter++;
                        m_asyncDownloadScreen.ProgressTextString = MyTexts.GetString(MyCommonTexts.ProgressTextDownloadingMods) + " " + counter.ToString() + " of " + numMods;
                    }
                }
                
            });

            long endTime = Stopwatch.GetTimestamp();

            if (downloadingFailed)
            {
                failedMods.ApplyChanges();
                if (failedMods.Count > 0)
                {
                    foreach (var mod in failedMods)
                    {
                        MySandboxGame.Log.WriteLineAndConsole(string.Format("Failed to download mod: Id = {0}, title = '{1}'", mod.PublishedFileId, mod.Title));
                    }
                }
                else if (!m_stop)
                {
                    MySandboxGame.Log.WriteLineAndConsole(string.Format("Failed to download mods because Steam is not in Online Mode."));
                }
                else
                {
                    MySandboxGame.Log.WriteLineAndConsole(string.Format("Failed to download mods because download was stopped."));
                }
                return new ResultData();
            }

            ResultData ret = new ResultData();
            ret.Success = true;
            ret.MismatchMods = "";
            mismatchMods.ApplyChanges();
            foreach(var mod in mismatchMods)
            {
                ret.MismatchMods += mod.Title + Environment.NewLine; 
            }

            double duration = (double)(endTime - startTime) / (double)Stopwatch.Frequency;
            MySandboxGame.Log.WriteLineAndConsole(string.Format("Mod download time: {0:0.00} seconds", duration));
            return ret;
        }

        /// <summary>
        /// Do NOT call this method from update thread.
        /// </summary>
        public static bool DownloadBlueprintsBlocking(List<SubscribedItem> mods)
        {
            foreach (var mod in mods)
            {
                if (!MySteam.IsOnline)
                    return false;

                if (m_stop)
                    return false;

                var localPackedModFullPath = Path.Combine(m_workshopBlueprintsPath, mod.PublishedFileId + m_workshopBlueprintSuffix);

                if (!IsModUpToDateBlocking(localPackedModFullPath, mod, true))
                {
                    if (!DownloadItemBlocking(localPackedModFullPath, mod.UGCHandle))
                    {
                        return false;
                    }
                }
                else
                {
                    MySandboxGame.Log.WriteLineAndConsole(string.Format("Up to date mod: Id = {0}, title = '{1}'", mod.PublishedFileId, mod.Title));
                }
            }
            return true;
        }

        public static bool DownloadScriptBlocking(SubscribedItem item)
        {
            if (!MySteam.IsOnline)
                return false;

            if (m_stop)
                return false;

            var localPackedModFullPath = Path.Combine(m_workshopScriptPath, item.PublishedFileId + MyGuiIngameScriptsPage.WORKSHOP_SCRIPT_EXTENSION);

            if (!IsModUpToDateBlocking(localPackedModFullPath, item, true))
            {
                if (!DownloadItemBlocking(localPackedModFullPath, item.UGCHandle))
                {
                    return false;
                }
            }
            else
            {
                MySandboxGame.Log.WriteLineAndConsole(string.Format("Up to date mod: Id = {0}, title = '{1}'", item.PublishedFileId, item.Title));
            }
            return true;
        }

        public static bool DownloadBlueprintBlocking(SubscribedItem item,bool check =true)
        {
            if (m_stop)
                return false;

            var localPackedModFullPath = Path.Combine(m_workshopBlueprintsPath, item.PublishedFileId + m_workshopBlueprintSuffix);

            if (check == false || !IsModUpToDateBlocking(localPackedModFullPath, item, true))
            {
                if (!DownloadItemBlocking(localPackedModFullPath, item.UGCHandle))
                {
                    return false;
                }
            }
            else
            {
                MySandboxGame.Log.WriteLineAndConsole(string.Format("Up to date mod: Id = {0}, title = '{1}'", item.PublishedFileId, item.Title));
            }
            return true;
        }

        public static bool IsBlueprintUpToDate(SubscribedItem item)
        {  
            if (!MySteam.IsOnline)
                return false;

            if (m_stop)
                return false;

            var localPackedModFullPath = Path.Combine(m_workshopBlueprintsPath, item.PublishedFileId + m_workshopBlueprintSuffix);

            return IsModUpToDateBlocking(localPackedModFullPath, item, true);
        }

        public static bool DownloadModFromURLStream(string url, ulong publishedFileId, Action<bool> callback)
        {
            uint handle = HTTP.CreateHTTPRequest(HTTPMethod.GET, url);
            if (handle == 0)
            {
                callback(false);
                return false;
            }

            if (!HTTP.SetHTTPRequestContextValue(handle, publishedFileId))
            {
                MySandboxGame.Log.WriteLine(string.Format("HTTP: could not set context value = {0}", publishedFileId));
                callback(false);
                return false;
            }

            var localPackedModFullPath = Path.Combine(MyFileSystem.ModsPath, publishedFileId.ToString() + m_workshopModSuffix);

            var fs = File.OpenWrite(localPackedModFullPath);

            DataReceived onDataRecieved = delegate(HTTPRequestDataReceived data)
            {
                if (data.ContextValue != publishedFileId)
                    return;

                var buffer = new byte[data.BytesReceived];
                HTTP.GetHTTPStreamingResponseBodyData(data.Request, data.Offset, buffer, data.BytesReceived);
                fs.Write(buffer, 0, (int)data.BytesReceived);
            };

            HTTP.DataReceived += onDataRecieved;

            if (!HTTP.SendHTTPRequestAndStreamResponse(handle, delegate(bool ioFailure, HTTPRequestCompleted data)
            {
                HTTP.DataReceived -= onDataRecieved;
                var success = !ioFailure && data.RequestSuccessful && data.StatusCode == HTTPStatusCode.OK;
                if (success)
                {
                    MySandboxGame.Log.WriteLine(string.Format("HTTP: Downloaded mod publishedFileId = {0}, size = {1}B to: '{2}' url: '{3}'", publishedFileId, fs.Length, localPackedModFullPath, url));
                }
                else
                {
                    MySandboxGame.Log.WriteLine(string.Format("HTTP: Error downloading file: Id = {0}, status = {1}, url = '{2}'", publishedFileId, data.StatusCode, url));
                }
                fs.Dispose();
                callback(success);
            }))
            {
                MySandboxGame.Log.WriteLine(string.Format("HTTP: could not send HTTP request url = '{0}'", url));
                callback(false);
                return false;
            }
            return true;
        }

        public static bool DownloadModFromURL(string url, ulong publishedFileId, Action<bool> callback)
        {
            uint handle = HTTP.CreateHTTPRequest(HTTPMethod.GET, url);
            if (handle == 0)
                return false;
            bool success = false;
            uint dataSize = 0;

            if (!HTTP.SendHTTPRequest(handle, delegate(bool ioFailure, HTTPRequestCompleted data)
            {
                if (!ioFailure && data.RequestSuccessful && data.StatusCode == HTTPStatusCode.OK)
                {
                    var localPackedModFullPath = Path.Combine(m_workshopModsPath, publishedFileId + m_workshopModSuffix);
                    if (HTTP.GetHTTPResponseBodySize(data.Request, out dataSize))
                    {
                        var bodyData = new byte[dataSize];
                        if (HTTP.GetHTTPResponseBodyData(data.Request, bodyData, dataSize))
                        {
                            try
                            {
                                File.WriteAllBytes(localPackedModFullPath, bodyData);
                                success = true;
                                MySandboxGame.Log.WriteLine(string.Format("HTTP: Downloaded mod publishedFileId = {0}, size = {1} bytes to: '{2}' from: '{3}'", publishedFileId, dataSize, localPackedModFullPath, url));
                            }
                            catch (Exception e)
                            {
                                MySandboxGame.Log.WriteLine(string.Format("HTTP: failed to write data {0} bytes to file = '{1}', error: {2}", dataSize, localPackedModFullPath, e));
                            }
                        }
                        else
                        {
                            MySandboxGame.Log.WriteLine(string.Format("HTTP: failed to read response body data, size = {0}", dataSize));
                        }
                    }
                    else
                    {
                        MySandboxGame.Log.WriteLine("HTTP: failed to read response body size");
                    }
                }
                else
                {
                    MySandboxGame.Log.WriteLine(string.Format("HTTP: error {0}", data.StatusCode));
                }
                callback(success);
            }))
                return false;
            return true;
        }


        //dont even try to understand the following function

        /// <summary>
        /// Do NOT call this method from update thread.
        /// </summary>
        public static ResultData DownloadWorldModsBlocking(List<MyObjectBuilder_Checkpoint.ModItem> mods)
        {
            ResultData ret = new ResultData();
            ret.Success = true;
            if (!MyFakes.ENABLE_WORKSHOP_MODS)
            {             
                return ret;
            }

            MySandboxGame.Log.WriteLine("Downloading world mods - START");
            MySandboxGame.Log.IncreaseIndent();

            m_stop = false;

            if (mods != null && mods.Count > 0)
            {
                var publishedFileIds = new List<ulong>();
                foreach (var mod in mods)
                {
                    if (mod.PublishedFileId != 0)
                    {
                        publishedFileIds.Add(mod.PublishedFileId);
                    }
                    else if (MySandboxGame.IsDedicated)
                    {
                        MySandboxGame.Log.WriteLineAndConsole("Local mods are not allowed in multiplayer.");
                        MySandboxGame.Log.DecreaseIndent();
                        return new ResultData();
                    }
                }

                // Check if the world doesn't contain duplicate mods, if it does, log it and remove the duplicate entry
                publishedFileIds.Sort();
                for (int i = 0; i < publishedFileIds.Count - 1;)
                {
                    ulong id1 = publishedFileIds[i];
                    ulong id2 = publishedFileIds[i + 1];
                    if (id1 == id2)
                    {
                        MySandboxGame.Log.WriteLine(string.Format("Duplicate mod entry for id: {0}", id1));
                        publishedFileIds.RemoveAt(i + 1);
                    }
                    else
                    {
                        i++;
                    }
                }

                if (MySandboxGame.IsDedicated)
                {
                    using (ManualResetEvent mrEvent = new ManualResetEvent(false))
                    {
                        string xml = "";

                        MySteamWebAPI.GetPublishedFileDetails(publishedFileIds, delegate(bool success, string data)
                        {
                            if (!success)
                            {
                                MySandboxGame.Log.WriteLine("Could not retrieve mods details.");
                            }
                            else
                            {
                                xml = data;
                            }
                            ret.Success = success;
                            mrEvent.Set();
                        });

                        while (!mrEvent.WaitOne(17))
                        {
                            mrEvent.Reset();
                            if (MySteam.Server != null)
                                MySteam.Server.RunCallbacks();
                            else
                            {
                                MySandboxGame.Log.WriteLine("Steam server API unavailable");
                                ret.Success = false;
                                break;
                            }
                        }

                        if (ret.Success)
                        {
                            try
                            {
                                System.Xml.XmlReaderSettings settings = new System.Xml.XmlReaderSettings()
                                {
                                    DtdProcessing = System.Xml.DtdProcessing.Ignore,
                                };
                                using (System.Xml.XmlReader reader = System.Xml.XmlReader.Create(new StringReader(xml), settings))
                                {
                                    reader.ReadToFollowing("result");

                                    Result xmlResult = (Result)reader.ReadElementContentAsInt();

                                    if (xmlResult != Result.OK)
                                    {
                                        MySandboxGame.Log.WriteLine(string.Format("Failed to download mods: result = {0}", xmlResult));
                                        ret.Success = false;
                                    }

                                    reader.ReadToFollowing("resultcount");
                                    int count = reader.ReadElementContentAsInt();

                                    if (count != publishedFileIds.Count)
                                    {
                                        MySandboxGame.Log.WriteLine(string.Format("Failed to download mods details: Expected {0} results, got {1}", publishedFileIds.Count, count));
                                    }

                                    var array = mods.ToArray();

                                    for (int i = 0; i < array.Length; ++i)
                                    {
                                        array[i].FriendlyName = array[i].Name;
                                    }

                                    var processed = new List<ulong>(publishedFileIds.Count);

                                    for (int i = 0; i < publishedFileIds.Count; ++i)
                                    {
                                        mrEvent.Reset();

                                        reader.ReadToFollowing("publishedfileid");
                                        ulong publishedFileId = Convert.ToUInt64(reader.ReadElementContentAsString());

                                        if (processed.Contains(publishedFileId))
                                        {
                                            MySandboxGame.Log.WriteLineAndConsole(string.Format("Duplicate mod: id = {0}", publishedFileId));
                                            continue;
                                        }
                                        processed.Add(publishedFileId);

                                        reader.ReadToFollowing("result");
                                        Result itemResult = (Result)reader.ReadElementContentAsInt();

                                        if (itemResult != Result.OK)
                                        {
                                            MySandboxGame.Log.WriteLineAndConsole(string.Format("Failed to download mod: id = {0}, result = {1}", publishedFileId, itemResult));
                                            ret.Success = false;
                                            continue;
                                        }

                                        reader.ReadToFollowing("consumer_app_id");
                                        int appid = reader.ReadElementContentAsInt();
                                        if (appid != MySteam.AppId)
                                        {
                                            MySandboxGame.Log.WriteLineAndConsole(string.Format("Failed to download mod: id = {0}, wrong appid, got {1}, expected {2}", publishedFileId, appid, MySteam.AppId));
                                            ret.Success = false;
                                            continue;
                                        }

                                        reader.ReadToFollowing("file_size");
                                        long fileSize = reader.ReadElementContentAsLong();

                                        reader.ReadToFollowing("file_url");
                                        string url = reader.ReadElementContentAsString();

                                        reader.ReadToFollowing("title");
                                        string title = reader.ReadElementContentAsString();

                                        for (int j = 0; j < array.Length; ++j)
                                        {
                                            if (array[j].PublishedFileId == publishedFileId)
                                            {
                                                array[j].FriendlyName = title;
                                                break;
                                            }
                                        }

                                        reader.ReadToFollowing("time_updated");
                                        uint timeUpdated = (uint)reader.ReadElementContentAsLong();

                                        var mod = new SubscribedItem() { Title = title, PublishedFileId = publishedFileId, TimeUpdated = timeUpdated };

                                        if (IsModUpToDateBlocking(Path.Combine(MyFileSystem.ModsPath, publishedFileId.ToString() + ".sbm"), mod, false, fileSize))
                                        {
                                            MySandboxGame.Log.WriteLineAndConsole(string.Format("Up to date mod:  id = {0}", publishedFileId));
                                            continue;
                                        }

                                        MySandboxGame.Log.WriteLineAndConsole(string.Format("Downloading mod: id = {0}, size = {1,8:0.000} MiB", publishedFileId, (double)fileSize / 1024f / 1024f));

                                        if (fileSize > 10 * 1024 * 1024) // WTF Steam
                                        {
                                            if (!DownloadModFromURLStream(url, publishedFileId, delegate(bool success)
                                            {
                                                if (!success)
                                                {
                                                    MySandboxGame.Log.WriteLineAndConsole(string.Format("Could not download mod: id = {0}, url = {1}", publishedFileId, url));
                                                }
                                                mrEvent.Set();
                                            }))
                                            {
                                                ret.Success = false;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            if (!DownloadModFromURL(url, publishedFileId, delegate(bool success)
                                            {
                                                if (!success)
                                                {
                                                    MySandboxGame.Log.WriteLineAndConsole(string.Format("Could not download mod: id = {0}, url = {1}", publishedFileId, url));
                                                }
                                                mrEvent.Set();
                                            }))
                                            {
                                                ret.Success = false;
                                                break;
                                            }
                                        }

                                        while (!mrEvent.WaitOne(17))
                                        {
                                            mrEvent.Reset();
                                            if (MySteam.Server != null)
                                                MySteam.Server.RunCallbacks();
                                            else
                                            {
                                                MySandboxGame.Log.WriteLine("Steam server API unavailable");
                                                ret.Success = false;
                                                break;
                                            }
                                        }
                                    }
                                    mods.Clear();
                                    mods.AddArray(array);
                                }
                            }
                            catch (Exception e)
                            {
                                MySandboxGame.Log.WriteLine(string.Format("Failed to download mods: {0}", e));
                                ret.Success = false;
                            }
                        }
                    }
                }
                else // client
                {
                    var toGet = new List<SubscribedItem>(publishedFileIds.Count);

                    if (!GetItemsBlocking(toGet, publishedFileIds))
                    {
                        MySandboxGame.Log.WriteLine("Could not obtain workshop item details");
                       ret.Success = false;
                    }
                    else if (publishedFileIds.Count != toGet.Count)
                    {
                        MySandboxGame.Log.WriteLine(string.Format("Could not obtain all workshop item details, expected {0}, got {1}", publishedFileIds.Count, toGet.Count));
                        ret.Success = false;
                    }
                    else
                    {
                        m_asyncDownloadScreen.ProgressTextString = MyTexts.GetString(MyCommonTexts.ProgressTextDownloadingMods) + " 0 of " + toGet.Count.ToString();
                       
                        ret = DownloadModsBlocking(toGet);
                        if (ret.Success == false)
                        {
                            MySandboxGame.Log.WriteLine("Downloading mods failed");
                        }
                        else
                        {
                            var array = mods.ToArray();

                            for (int i = 0; i < array.Length; ++i)
                            {
                                var mod = toGet.Find(x => x.PublishedFileId == array[i].PublishedFileId);
                                if (mod != null)
                                {
                                    array[i].FriendlyName = mod.Title;
                                }
                                else
                                {
                                    array[i].FriendlyName = array[i].Name;
                                }
                            }
                            mods.Clear();
                            mods.AddArray(array);
                        }
                    }
                }
            }
            MySandboxGame.Log.DecreaseIndent();
            MySandboxGame.Log.WriteLine("Downloading world mods - END");
            return ret;
        }

        /// <summary>
        /// Do NOT call this method from update thread.
        /// </summary>
        private static bool IsModUpToDateBlocking(string fullPath, SubscribedItem mod, bool checkWithWorkshop, long expectedFilesize = -1)
        {
            if (m_stop)
                return false;

            if (!File.Exists(fullPath))
                return false;

            if (checkWithWorkshop)
            {
                var success = false;

                using (ManualResetEvent mrEvent = new ManualResetEvent(false))
                {
                    MySteam.API.RemoteStorage.GetPublishedFileDetails(mod.PublishedFileId, 0, delegate(bool ioFailure, RemoteStorageGetPublishedFileDetailsResult data)
                    {
                        success = !ioFailure && data.Result == Result.OK;
                        if (success)
                        {
                            mod.TimeUpdated = data.TimeUpdated;
                            expectedFilesize = data.FileSize;
                        }
                        else
                        {
                            MySandboxGame.Log.WriteLine(string.Format("Error downloading file details: Id={0}, {1}", mod.PublishedFileId, GetErrorString(ioFailure, data.Result)));
                        }
                        mrEvent.Set();
                    });

                    success = mrEvent.WaitOne(60000);
                }

                if (!success)
                    return false;
            }

            if (expectedFilesize != -1)
            {
                using (var file = File.OpenRead(fullPath))
                {
                    if (file.Length != expectedFilesize)
                    {
                        return false;
                    }
                }
            }

            var localTimeUpdated = File.GetLastWriteTimeUtc(fullPath);
            var remoteTimeUpdated = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(mod.TimeUpdated);

            return localTimeUpdated >= remoteTimeUpdated;
        }

        public static bool GenerateModInfo(string modPath, SubscribedItem mod)
        {
            return GenerateModInfo(modPath, mod.PublishedFileId, mod.SteamIDOwner);
        }

        public static bool GenerateModInfo(string modPath, ulong publishedFileId, ulong steamIDOwner)
        {
            var modInfo = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ModInfo>();
            modInfo.WorkshopId = publishedFileId;
            modInfo.SteamIDOwner = steamIDOwner;

            if (!MyObjectBuilderSerializer.SerializeXML(Path.Combine(modPath, "modinfo.sbmi"), false, modInfo))
            {
                MySandboxGame.Log.WriteLine(string.Format("Error creating modinfo: workshopID={0}, mod='{1}'", publishedFileId, modPath));
                return false;
            }
            return true;
        }

        #endregion

        #region Subscribed world instance creation
        public static void CreateWorldInstanceAsync(SubscribedItem world, MyWorkshopPathInfo pathInfo, bool overwrite, Action<bool, string> callbackOnFinished = null)
        {
            MyGuiSandbox.AddScreen(new MyGuiScreenProgressAsync(MyCommonTexts.ProgressTextCreatingWorld,
                null,
                () => new CreateWorldResult(world, pathInfo, callbackOnFinished, overwrite),
                endActionCreateWorldInstance));
        }

        static void endActionCreateWorldInstance(IMyAsyncResult result, MyGuiScreenProgressAsync screen)
        {
            screen.CloseScreen();

            var createdWorldResult = (CreateWorldResult)result;

            var callback = createdWorldResult.Callback;

            if (callback != null)
                callback(createdWorldResult.Success, createdWorldResult.m_createdSessionPath);
        }

        /// <summary>
        /// Do NOT call this method from update thread.
        /// </summary>
        public static bool TryCreateWorldInstanceBlocking(SubscribedItem world, MyWorkshopPathInfo pathInfo, out string sessionPath, bool overwrite)
        {
            m_stop = false;
            if (!Directory.Exists(pathInfo.Path))
                Directory.CreateDirectory(pathInfo.Path);

            string safeName = MyUtils.StripInvalidChars(world.Title);
            sessionPath = null;

            var localPackedWorldFullPath = Path.Combine(pathInfo.Path, world.PublishedFileId + pathInfo.Suffix);

            if (!MySteam.IsOnline)
                return false;

            if (!IsModUpToDateBlocking(localPackedWorldFullPath, world, true))
            {
                if (!DownloadItemBlocking(localPackedWorldFullPath, world.UGCHandle))
                    return false;
            }

            // Extract packaged world.
            sessionPath = MyLocalCache.GetSessionSavesPath(safeName, false, false);

            //overwrite?
            if (overwrite && Directory.Exists(sessionPath))
                Directory.Delete(sessionPath, true);

            // Find new non existing folder. The game folder name may be different from game name, so we have to
            // make sure we don't overwrite another save
            while (Directory.Exists(sessionPath))
                sessionPath = MyLocalCache.GetSessionSavesPath(safeName + MyUtils.GetRandomInt(int.MaxValue).ToString("########"), false, false);

            MyZipArchive.ExtractToDirectory(localPackedWorldFullPath, sessionPath);

            // Update some meta-data of the new world.
            ulong checkPointSize;
            var checkpoint = MyLocalCache.LoadCheckpoint(sessionPath, out checkPointSize);
            checkpoint.SessionName = string.Format("({0}) {1}", pathInfo.NamePrefix, world.Title);
            checkpoint.LastSaveTime = DateTime.Now;
            checkpoint.WorkshopId = null;
            MyLocalCache.SaveCheckpoint(checkpoint, sessionPath);
            MyLocalCache.SaveLastLoadedTime(sessionPath, DateTime.Now);

            return true;
        }

        /// <summary>
        /// Do NOT call this method from update thread.
        /// </summary>
        public static bool TryCreateBattleWorldInstanceBlocking(SubscribedItem world, string workshopBattleWorldsPath, out string sessionPath)
        {
            if (!Directory.Exists(m_workshopWorldsPath))
                Directory.CreateDirectory(m_workshopWorldsPath);

            string safeName = MyUtils.StripInvalidChars(world.Title);
            sessionPath = null;

            var localPackedWorldFullPath = Path.Combine(m_workshopWorldsPath, world.PublishedFileId + m_workshopWorldSuffix);

            if (!MySteam.IsOnline)
                return false;

            if (!IsModUpToDateBlocking(localPackedWorldFullPath, world, true))
            {
                if (!DownloadItemBlocking(localPackedWorldFullPath, world.UGCHandle))
                    return false;
            }

            if (!Directory.Exists(workshopBattleWorldsPath))
                Directory.CreateDirectory(workshopBattleWorldsPath);

            sessionPath = Path.Combine(workshopBattleWorldsPath, safeName);

            // Find new non existing folder. The game folder name may be different from game name, so we have to
            // make sure we don't overwrite another save
            while (Directory.Exists(sessionPath))
                sessionPath = Path.Combine(workshopBattleWorldsPath, safeName + MyUtils.GetRandomInt(int.MaxValue).ToString("########"));
#if XB1
			System.Diagnostics.Debug.Assert(false);
#else
            MyZipArchive.ExtractToDirectory(localPackedWorldFullPath, sessionPath);
#endif

			// Update some meta-data of the new world.
            ulong checkPointSize;
            var checkpoint = MyLocalCache.LoadCheckpoint(sessionPath, out checkPointSize);
            checkpoint.SessionName = world.Title;
            checkpoint.LastSaveTime = DateTime.Now;
            checkpoint.WorkshopId = world.PublishedFileId;
            MyLocalCache.SaveCheckpoint(checkpoint, sessionPath);
            MyLocalCache.SaveLastLoadedTime(sessionPath, DateTime.Now);

            return true;
        }


        class CreateWorldResult : IMyAsyncResult
        {
            public Task Task
            {
                get;
                private set;
            }

            public bool Success
            {
                get;
                private set;
            }

            public string m_createdSessionPath;

            public Action<bool, string> Callback
            {
                get;
                private set;
            }

            public CreateWorldResult(SubscribedItem world, MyWorkshopPathInfo pathInfo, Action<bool, string> callback, bool overwrite)
            {
                Callback = callback;
                Task = Parallel.Start(() =>
                {
                    Success = TryCreateWorldInstanceBlocking(world, pathInfo, out m_createdSessionPath, overwrite);
                });
            }

            public bool IsCompleted { get { return this.Task.IsComplete; } }
        }

        #endregion

        private static string GetErrorString(bool ioFailure, Result result)
        {
            return ioFailure ? "IO Failure" : result.ToString();
        }

        public static bool CheckLocalModsAllowed(List<MyObjectBuilder_Checkpoint.ModItem> mods, bool allowLocalMods)
        {
            foreach (var mod in mods)
            {
                if (mod.PublishedFileId == 0 && !allowLocalMods)
                {
                    return false;
                }
            }
            return true;
        }

        public static bool CanRunOffline(List<MyObjectBuilder_Checkpoint.ModItem> mods)
        {
            foreach (var mod in mods)
            {
                if (mod.PublishedFileId != 0)
                {
                    var modFullPath = Path.Combine(MyFileSystem.ModsPath, mod.Name);
                    if (!Directory.Exists(modFullPath) && !File.Exists(modFullPath))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
#else // XB1
namespace Sandbox.Engine.Networking
{
    public class MySteamWorkshop
    {
        public static void DownloadModsAsync(List<MyObjectBuilder_Checkpoint.ModItem> mods, Action<bool,string> onFinishedCallback, Action onCancelledCallback = null)
        {
            onFinishedCallback(true,"");
            return;
        }

        public static bool DownloadWorldModsBlocking(List<MyObjectBuilder_Checkpoint.ModItem> mods)
        {
            return true;
        }

        public static bool CheckLocalModsAllowed(List<MyObjectBuilder_Checkpoint.ModItem> mods, bool allowLocalMods)
        {
            return true;
        }

        public static bool CanRunOffline(List<MyObjectBuilder_Checkpoint.ModItem> mods)
        {
            return true;
        }
    }
}
#endif // XB1
