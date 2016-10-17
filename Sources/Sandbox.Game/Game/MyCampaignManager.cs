using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using VRage.FileSystem;
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
            var localModFolders = Directory.GetDirectories(MyFileSystem.ModsPath);
            var modFiles = MyFileSystem.GetFiles(MyFileSystem.ModsPath, "*.sbm", MySearchOption.TopDirectoryOnly);
            var localModCampaignFiles = new List<string>();
            var modCampaignFiles = new List<string>();

            // Gather local mod campaign files
            foreach (var localModFolder in localModFolders)
            {
                localModCampaignFiles.AddRange(
                    MyFileSystem.GetFiles(Path.Combine(localModFolder, CAMPAIGN_CONTENT_RELATIVE_PATH), "*.vs",
                        MySearchOption.TopDirectoryOnly));
            }

            // Gather campaign files from workshop zips
            foreach (var modFile in modFiles)
            {
                try
                {
                    modCampaignFiles.AddRange(MyFileSystem.GetFiles(Path.Combine(modFile, CAMPAIGN_CONTENT_RELATIVE_PATH),
                        "*.vs", MySearchOption.TopDirectoryOnly));
                }
                catch (Exception e)
                {
                    MySandboxGame.Log.WriteLine("ERROR: Reading mod file: " + modFile + "\n" + e);
                }
            }

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

            foreach (var modCampaignFile in modCampaignFiles)
            {
                MyObjectBuilder_VSFiles ob;
                if (MyObjectBuilderSerializer.DeserializeXML(modCampaignFile, out ob))
                {
                    if (ob.Campaign != null)
                    {
                        ob.Campaign.IsVanilla = false;
                        ob.Campaign.IsLocalMod = false;
                        ob.Campaign.ModFolderPath = GetModFolderPath(modCampaignFile);
                        LoadCampaignData(ob.Campaign);
                    }
                }
            }

            MySandboxGame.Log.WriteLine("MyCampaignManager.Constructor() - END");
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
            if (m_activeCampaign.IsVanilla)
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
            MyUtils.CopyDirectory(directory.FullName, targetDirectory);

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
    }
}
