using System;
using System.Collections.Generic;
using System.IO;
using VRage.FileSystem;
using VRage.Game.Localization;
using VRage.Game.ObjectBuilders.Components;
using VRage.Utils;

namespace VRage.Game.Components.Session
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate, 666, typeof(MyObjectBuilder_LocalizationSessionComponent))]
    public class MyLocalizationSessionComponent : MySessionComponentBase
    {       
        public static readonly string MOD_BUNDLE_NAME = "MySession - Mod Bundle";
        public static readonly string CAMPAIGN_BUNDLE_NAME = "MySession - Campaing Bundle";

        // Language used for localization
        private string m_language;
        private string m_campaignModFolderName;
        private MyLocalization.MyBundle m_modBundle;
        private MyLocalization.MyBundle m_campaignBundle;
        private readonly List<MyLocalizationContext> m_influencedContexts = new List<MyLocalizationContext>();

        // Language of the current localization.
        public string Language
        {
            get { return m_language; }
        }

        public MyLocalizationSessionComponent()
        {
            m_modBundle.BundleId = MyStringId.GetOrCompute(MOD_BUNDLE_NAME);
            m_campaignBundle.BundleId = MyStringId.GetOrCompute(CAMPAIGN_BUNDLE_NAME);

            m_campaignBundle.FilePaths = new List<string>();
            m_modBundle.FilePaths = new List<string>();
        }

        // Loads campaign bundle with provided files
        // This is and should be used only
        public void LoadCampaignLocalization(IEnumerable<string> paths, string campaignModFolderPath = null)
        {
            // Remove the first part of the full path
            m_campaignModFolderName = Path.GetFileName(campaignModFolderPath);
            m_campaignBundle.FilePaths.Clear();
            // Add campaign mod folder
            if (campaignModFolderPath != null)
            {
                m_campaignBundle.FilePaths.Add(campaignModFolderPath);
            }

            // Add custom content folders
            foreach (var path in paths)
            {
                try 
                { 
                    var contentPath = Path.Combine(MyFileSystem.ContentPath, path);
                    var modPath = campaignModFolderPath != null ? Path.Combine(campaignModFolderPath, path) : string.Empty;
                    if (MyFileSystem.FileExists(contentPath))
                    {
                        m_campaignBundle.FilePaths.Add(contentPath);
                    } 
                    else if(!string.IsNullOrEmpty(campaignModFolderPath) && MyFileSystem.FileExists(modPath))
                    {
                        m_campaignBundle.FilePaths.Add(modPath);
                    }
                    else
                    { 
                        var files = MyFileSystem.GetFiles(Path.Combine(MyFileSystem.ContentPath, path), "*.sbl", MySearchOption.AllDirectories);
                        foreach (var filePath in files)
                        {
                            m_campaignBundle.FilePaths.Add(filePath);
                        }
                    }
                } 
                catch
                { }
            }


            // For nonempty bundles, clear contexts
            if(m_campaignBundle.FilePaths.Count > 0)
            {
                MyLocalization.Static.LoadBundle(m_campaignBundle, m_influencedContexts);
            }
        }

        // Switches language of influenced contexts
        public void SwitchLanguage(string language)
        {
            m_language = language;
            m_influencedContexts.ForEach(context => context.Switch(language));
        }

        #region Serialization/Deserialization

        public override void BeforeStart()
        {
            foreach (var modItem in Session.Mods)
            {
                // Local mods have a name as folder name
                // workshop mods have pulisherfileid + .sbm archive
                var modPath = Path.Combine(MyFileSystem.ModsPath,
                    modItem.ShouldSerializeName() ? modItem.Name : modItem.PublishedFileId + ".sbm");
                try
                {
                    var files = MyFileSystem.GetFiles(modPath, "*.sbl", MySearchOption.AllDirectories);

                    foreach (var file in files)
                    {
                        m_modBundle.FilePaths.Add(file);
                    }   
                }
                catch (Exception e)
                {
                    // ignored -- can be just about anything that causes trouble in file system
                    MyLog.Default.WriteLine("MyLocalizationSessionComponent: Problem deserializing " + modPath + "\n" + e);
                }
            }

            // Load the bundle
            MyLocalization.Static.LoadBundle(m_modBundle, m_influencedContexts);
            SwitchLanguage(m_language);
        }

        protected override void UnloadData()
        {
            // Unload session localization
            MyLocalization.Static.DisposeAll();
        }

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);

            var ob = sessionComponent as MyObjectBuilder_LocalizationSessionComponent;

            if (ob != null)
            {
                m_language = ob.Language;
                LoadCampaignLocalization(ob.CampaignPaths, m_campaignModFolderName);
            }
        }

        public override MyObjectBuilder_SessionComponent GetObjectBuilder()
        {
            var ob = base.GetObjectBuilder() as MyObjectBuilder_LocalizationSessionComponent;
            if (ob != null)
            {
                ob.Language = m_language;
                ob.CampaignModFolderName = m_campaignModFolderName;

                foreach (var path in m_campaignBundle.FilePaths)
                {
                    // Remove the content path from relative file path
                    ob.CampaignPaths.Add(path.Replace(MyFileSystem.ContentPath + "\\", ""));
                }
            }

            return ob;
        }

        #endregion

    }
}
