using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Sandbox.Engine.Networking;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using SteamSDK;
using VRage;
using VRage.Compression;
using VRage.FileSystem;
using VRage.Game;
using VRage.Game.Components.Session;
using VRage.Game.ObjectBuilders.Campaign;
using VRage.Game.ObjectBuilders.VisualScripting;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace Sandbox.Game
{
    public class MyCampaignManager
    {
        #region Private Fields
        // Instance for static access
        private static MyCampaignManager m_instance;

        // State data
        private string                      m_selectedLanguage;
        private string                      m_activeCampaignName;
        private MyObjectBuilder_Campaign    m_activeCampaign;

        // All campaign storage
        private readonly Dictionary<string, List<MyObjectBuilder_Campaign>> m_campaignsByNames =
            new Dictionary<string, List<MyObjectBuilder_Campaign>>();

        // Temp storage for GUI purposes
        private readonly List<string> m_activeCampaignLevelNames =
            new List<string>();
        #endregion

        #region Constants
        // Relative content path to Campaign folder
        private const string CAMPAIGN_CONTENT_RELATIVE_PATH = "Campaigns";
        // Relative content path to Campaign debug folder
        private const string CAMPAIGN_DEBUG_RELATIVE_PATH = @"Worlds\Campaigns";
        #endregion

        #region Public Properties
        // Static accessor property
        public static MyCampaignManager Static
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = new MyCampaignManager();
                }

                return m_instance;
            }
        }

        public IEnumerable<MyObjectBuilder_Campaign> Campaigns
        {
            get
            {
                var storage = new List<MyObjectBuilder_Campaign>();

                foreach (var campaignList in m_campaignsByNames.Values)
                {
                    storage.AddRange(campaignList);
                }

                return storage;
            }
        } 

        public IEnumerable<string> CampaignNames
        {
            get { return m_campaignsByNames.Keys; }
        }

        public IEnumerable<string> ActiveCampaignLevels
        {
            get { return m_activeCampaignLevelNames; }
        }

        public string ActiveCampaignName
        {
            get { return m_activeCampaignName; }
        }

        public MyObjectBuilder_Campaign ActiveCampaign
        {
            get { return m_activeCampaign; }
        }

        public bool IsCampaignRunning
        {
            get
            {
                if (MySession.Static == null)
                    return false;

                var component = MySession.Static.GetComponent<MyCampaignSessionComponent>();
                return component.Running;
            }
        }

        public IEnumerable<string> LocalizationLanguages
        {
            get
            {
                if(m_activeCampaign == null)
                    return null;

                return m_activeCampaign.LocalizationLanguages;
            }
        }

        public string SelectedLanguage
        {
            get
            {
                if(m_activeCampaign == null) return String.Empty;
                return m_selectedLanguage;
            }

            set
            {
                var index = m_activeCampaign.LocalizationLanguages.IndexOf(value);
                if(index != -1)
                    m_selectedLanguage = m_activeCampaign.LocalizationLanguages[index];
            }
        }

        public event Action OnCampaignFinished;

        #endregion

        #region Constructor and Loading
        // Loads campaign data to storage.
        public void Init()
        {
            MySandboxGame.Log.WriteLine("MyCampaignManager.Constructor() - START");

            // Variables needed for loading
            var vanillaFiles =
                MyFileSystem.GetFiles(Path.Combine(MyFileSystem.ContentPath, CAMPAIGN_CONTENT_RELATIVE_PATH), "*.vs",
                    MySearchOption.TopDirectoryOnly);

            // Load vanilla files
            foreach (var vanillaFile in vanillaFiles)
            {
                MyObjectBuilder_VSFiles ob;
                if (MyObjectBuilderSerializer.DeserializeXML(vanillaFile, out ob))
                {
                    if (ob.Campaign != null)
                    {
                        ob.Campaign.IsVanilla = true;
                        ob.Campaign.IsLocalMod = false;
                        LoadCampaignData(ob.Campaign);
                    }
                }
            }

            // Add the Debug campaings of present
            if (!MyFinalBuildConstants.IS_OFFICIAL)
            {
                var debugCampaignFiles =
                    MyFileSystem.GetFiles(Path.Combine(MyFileSystem.ContentPath, CAMPAIGN_DEBUG_RELATIVE_PATH), "*.vs",
                        MySearchOption.TopDirectoryOnly);

                // Load vanilla files
                foreach (var debugFile in debugCampaignFiles)
                {
                    MyObjectBuilder_VSFiles ob;
                    if (MyObjectBuilderSerializer.DeserializeXML(debugFile, out ob))
                    {
                        if (ob.Campaign != null)
                        {
                            ob.Campaign.IsVanilla = true;
                            ob.Campaign.IsLocalMod = false;
                            ob.Campaign.IsDebug = true;
                            LoadCampaignData(ob.Campaign);
                        }
                    }
                }
            }

            MySandboxGame.Log.WriteLine("MyCampaignManager.Constructor() - END");
        }

        private readonly List<MySteamWorkshop.SubscribedItem> m_subscribedCampaignItems
            = new List<MySteamWorkshop.SubscribedItem>();

        /// <summary>
        /// DO NOT RUN FROM MAIN THREAD!
        /// </summary>
        public void RefreshModData()
        {
            RefreshLocalModData();
            RefreshSubscribedModData();
        }

        private void RefreshLocalModData()
        {
            var localModFolders = Directory.GetDirectories(MyFileSystem.ModsPath);
            var localModCampaignFiles = new List<string>();

            // Remove all local mods
            foreach (var campaignList in m_campaignsByNames.Values)
            {
                campaignList.RemoveAll(campaign => campaign.IsLocalMod);
            }

            // Gather local mod campaign files
            foreach (var localModFolder in localModFolders)
            {
                localModCampaignFiles.AddRange(
                    MyFileSystem.GetFiles(Path.Combine(localModFolder, CAMPAIGN_CONTENT_RELATIVE_PATH), "*.vs",
                        MySearchOption.TopDirectoryOnly));
            }

            // Add local mods data to campaign structure
            foreach (var localModCampaignFile in localModCampaignFiles)
            {
                MyObjectBuilder_VSFiles ob;
                if (MyObjectBuilderSerializer.DeserializeXML(localModCampaignFile, out ob))
                {
                    if (ob.Campaign != null)
                    {
                        ob.Campaign.IsVanilla = false;
                        ob.Campaign.IsLocalMod = true;
                        ob.Campaign.ModFolderPath = GetModFolderPath(localModCampaignFile);
                        LoadCampaignData(ob.Campaign);
                    }
                }
            }
        }

        // Removes unsubscribed items and adds newly subscribed items
        private void RefreshSubscribedModData()
        {
            if (MySteamWorkshop.GetSubscribedCampaignsBlocking(m_subscribedCampaignItems))
            {
                var toRemove = new List<MyObjectBuilder_Campaign>();
                // Remove unsubed items
                foreach (var campaignList in m_campaignsByNames.Values)
                {
                    foreach (var campaign in campaignList)
                    {
                        if(campaign.PublishedFileId == 0)
                            continue;

                        var found = false;
                        for (var index = 0; index < m_subscribedCampaignItems.Count; index++)
                        {
                            var subscribedItem = m_subscribedCampaignItems[index];
                            if (subscribedItem.PublishedFileId == campaign.PublishedFileId)
                            {
                                // Remove the result as processed (the entry should not appear elsewhere)
                                m_subscribedCampaignItems.RemoveAtFast(index);
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            toRemove.Add(campaign);
                        }
                    }

                    toRemove.ForEach(campaignToRemove => campaignList.Remove(campaignToRemove));
                    toRemove.Clear();
                }

                // Download the subscribed items
                MySteamWorkshop.DownloadModsBlocking(m_subscribedCampaignItems);

                // Load data for rest of the results
                foreach (var subscribedItem in m_subscribedCampaignItems)
                {
                    var modPath = Path.Combine(MyFileSystem.ModsPath, subscribedItem.PublishedFileId + ".sbm");
                    var visualScriptingFiles = MyFileSystem.GetFiles(modPath, "*.vs", MySearchOption.AllDirectories);

                    foreach (var modFile in visualScriptingFiles)
                    {
                        MyObjectBuilder_VSFiles ob;
                        if (MyObjectBuilderSerializer.DeserializeXML(modFile, out ob))
                        {
                            if (ob.Campaign != null)
                            {
                                ob.Campaign.IsVanilla = false;
                                ob.Campaign.IsLocalMod = false;
                                ob.Campaign.PublishedFileId = subscribedItem.PublishedFileId;
                                ob.Campaign.ModFolderPath = GetModFolderPath(modFile);
                                LoadCampaignData(ob.Campaign);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Runs publish process for active campaign.
        /// </summary>
        public void PublishActive()
        {
            var publishedSteamId = MySteamWorkshop.GetWorkshopIdFromLocalMod(m_activeCampaign.ModFolderPath);
            MySteamWorkshop.PublishModAsync(
                    m_activeCampaign.ModFolderPath,
                    m_activeCampaign.Name,
                    m_activeCampaign.Description,
                    publishedSteamId,
                    new []{MySteamWorkshop.WORKSHOP_CAMPAIGN_TAG},
                    PublishedFileVisibility.Public,
                    OnPublishFinished
                );   
        }

        // Called when campaign gets uploaded to steam workshop
        private void OnPublishFinished(bool publishSuccess, Result publishResult, ulong publishedFileId)
        {
            if (publishSuccess)
            {
                // Create metadata for further identification of the mod
                MySteamWorkshop.GenerateModInfo(m_activeCampaign.ModFolderPath, publishedFileId, Sync.MyId);
                // Success open the steam overlay with mod opened
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    styleEnum: MyMessageBoxStyleEnum.Info,
                    messageText: MyTexts.Get(MyCommonTexts.MessageBoxTextCampaignPublished),
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionCampaignPublished),
                    callback: (a) =>
                    {
                        MySteam.API.OpenOverlayUrl(string.Format("http://steamcommunity.com/sharedfiles/filedetails/?id={0}", publishedFileId));
                    }));
            }
            else
            {
                // Failed upload -- Tell whats the problem
                MyStringId error;
                switch (publishResult)
                {
                    case Result.AccessDenied:
                        error = MyCommonTexts.MessageBoxTextPublishFailed_AccessDenied;
                        break;
                    default:
                        error = MyCommonTexts.MessageBoxTextWorldPublishFailed;
                        break;
                }

                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                    messageText: MyTexts.Get(error),
                    messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionModCampaignPublishFailed)));
            }
        }

        // Takes the path the campaign file and returns path to mod folder
        private string GetModFolderPath(string campaignFilePath)
        {
            return campaignFilePath.Remove(campaignFilePath.IndexOf(CAMPAIGN_CONTENT_RELATIVE_PATH, StringComparison.InvariantCulture) - 1);
        }

        // Universal campaign loading process
        private void LoadCampaignData(MyObjectBuilder_Campaign campaignOb)
        {
            if (m_campaignsByNames.ContainsKey(campaignOb.Name))
            {
                var obs = m_campaignsByNames[campaignOb.Name];

                // check for duplicity
                foreach (var campaign in obs)
                {
                    if (campaign.IsLocalMod == campaignOb.IsLocalMod &&
                        campaign.IsMultiplayer == campaignOb.IsMultiplayer &&
                        campaign.IsVanilla == campaignOb.IsVanilla)
                    {
                        Debug.Fail("Two campaigns of same name and parameters loaded.");
                        return;
                    }
                }

                obs.Add(campaignOb);
            }
            else
            {
                m_campaignsByNames.Add(campaignOb.Name, new List<MyObjectBuilder_Campaign>());
                m_campaignsByNames[campaignOb.Name].Add(campaignOb);
            }
        }

        // Starts new session with campaign data
        public void LoadSessionFromActiveCampaign(string relativePath, Action afterLoad = null, string campaignDirectoryName = null)
        {
            var savePath = relativePath;
            string absolutePath;
            
            // >> WORLD FILE OPERATIONS
            // Find the existing file in order of modded content to vanilla
            if (m_activeCampaign.IsVanilla || m_activeCampaign.IsDebug)
            {
                absolutePath = Path.Combine(MyFileSystem.ContentPath, savePath);

                if (!MyFileSystem.FileExists(absolutePath))
                {
                    MySandboxGame.Log.WriteLine("ERROR: Missing vanilla world file in campaign: " + m_activeCampaignName);
                    Debug.Fail("ERROR: Missing vanilla world file in campaign: " + m_activeCampaignName);
                    return;
                }
            }
            else
            {
                // Modded content
                absolutePath = Path.Combine(m_activeCampaign.ModFolderPath, savePath);
                // try finding respective vanilla file if the file does not exist
                if (!MyFileSystem.FileExists(absolutePath))
                {
                    absolutePath = Path.Combine(MyFileSystem.ContentPath, savePath);
                    if (!MyFileSystem.FileExists(absolutePath))
                    {
                        MySandboxGame.Log.WriteLine("ERROR: Missing world file in campaign: " + m_activeCampaignName);
                        Debug.Fail("ERROR: Missing world file in campaign: " + m_activeCampaignName);
                        return;
                    }
                }

            }

            // Copy the save and load the session
            if(string.IsNullOrEmpty(campaignDirectoryName))
                campaignDirectoryName = ActiveCampaignName + " " + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");

            var directory = new DirectoryInfo(Path.GetDirectoryName(absolutePath));
            var targetDirectory = Path.Combine(MyFileSystem.SavesPath, campaignDirectoryName, directory.Name);
            while (MyFileSystem.DirectoryExists(targetDirectory))
            {
                // Finid new unique name for the folder
                targetDirectory = Path.Combine(MyFileSystem.SavesPath, directory.Name + " " + MyUtils.GetRandomInt(int.MaxValue).ToString("########"));
            }

            if (File.Exists(absolutePath))
            {
                // It is a local mod or vanilla file
                MyUtils.CopyDirectory(directory.FullName, targetDirectory);
            }
            else
            {
                // Its is a workshop mod
                var tmpPath = Path.Combine(Path.GetTempPath(), "TMP_CAMPAIGN_MOD_FOLDER");
                var worldFolderAbsolutePath = Path.Combine(tmpPath, Path.GetDirectoryName(relativePath));

                // Extract the mod to temp, copy the world folder to target directory, remove the temp folder
                MyZipArchive.ExtractToDirectory(m_activeCampaign.ModFolderPath, tmpPath);
                MyUtils.CopyDirectory(worldFolderAbsolutePath, targetDirectory);
                Directory.Delete(tmpPath, true);
            }

            // >> LOCALIZATION
            // Set localization for loaded level in the after load event
            if (string.IsNullOrEmpty(m_selectedLanguage))
            {
                m_selectedLanguage = m_activeCampaign.DefaultLocalizationLanguage;

                if (string.IsNullOrEmpty(m_selectedLanguage) && m_activeCampaign.LocalizationLanguages.Count > 0)
                {
                    m_selectedLanguage = m_activeCampaign.LocalizationLanguages[0];
                }
            }

            if (!string.IsNullOrEmpty(m_selectedLanguage))
            {
                // Initilize the sessionComponent in after load event
                afterLoad += () =>
                {
                    var comp = MySession.Static.GetComponent<MyLocalizationSessionComponent>();
                    comp.LoadCampaignLocalization(m_activeCampaign.LocalizationPaths, m_activeCampaign.ModFolderPath);
                    comp.SwitchLanguage(m_selectedLanguage);
                };
            }

            // ATM only single player campaigns are supported
            if (!m_activeCampaign.IsMultiplayer)
            {
                afterLoad += () => MySession.Static.Save();
                MySessionLoader.LoadSingleplayerSession(targetDirectory, afterLoad);
            }
        }

        #endregion
        // Changes the manager state to given campaign
        public void SwitchCampaign(string name, bool isVanilla = true, bool isLocalMod = false)
        {
            if (m_campaignsByNames.ContainsKey(name))
            {
                var obs = m_campaignsByNames[name];
                foreach (var campaign in obs)
                {
                    if (campaign.IsVanilla == isVanilla && campaign.IsLocalMod == isLocalMod)
                    {
                        m_activeCampaign = campaign;
                        m_activeCampaignName = name;
                        m_activeCampaignLevelNames.Clear();
                        m_selectedLanguage = m_activeCampaign.DefaultLocalizationLanguage;

                        // Fill the level names in active campaign level names
                        foreach (var campaignSmNode in m_activeCampaign.StateMachine.Nodes)
                        {
                            m_activeCampaignLevelNames.Add(campaignSmNode.Name);
                        }

                        return;
                    }
                }
            }
        }
        // starts new campaign 
        public void RunNewCampaign()
        {
            var startingState = FindStartingState();
            if (startingState != null)
            {
                LoadSessionFromActiveCampaign(startingState.SaveFilePath, () =>
                {
                    MySession.Static.GetComponent<MyCampaignSessionComponent>().InitFromActive();
                }
                );
            }
        }
        // Finds starting state of the campaign SM. For purposes of first load.
        private MyObjectBuilder_CampaignSMNode FindStartingState()
        {
            if(m_activeCampaign == null)
                return null;

            bool skip = false;
            foreach (var node in m_activeCampaign.StateMachine.Nodes)
            {
                foreach (var transition in m_activeCampaign.StateMachine.Transitions)
                {
                    if(transition.To == node.Name)
                    {
                        skip = true;
                        break;
                    }
                }
                if(skip)
                {
                    skip = false;
                    continue;
                }

                return node;
            }

            return null;
        }
        // Called from MyCampaignSessionComponent when campaign finished. Do not use anywhere else.
        public void NotifyCampaignFinished()
        {
            var handler = OnCampaignFinished;
            if (handler != null)
                handler();
        }
    }
}
