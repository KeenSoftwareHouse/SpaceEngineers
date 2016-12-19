using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using VRage.FileSystem;
using VRage.Utils;

namespace VRage.Game.Localization
{
    public class MyLocalization
    {
        public struct MyBundle
        {
            public MyStringId BundleId;
            public List<string> FilePaths;
        }

        // Folder within content folder that should contain localization files
        public static readonly string LOCALIZATION_FOLDER = "Data\\Localization";

        private static readonly StringBuilder m_defaultLocalization = new StringBuilder("Failed localization attempt. Missing or not loaded contexts.");

        // Static accessor
        private static MyLocalization m_instance;
        public static MyLocalization Static
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = new MyLocalization();
                    m_instance.Init();
                }

                return m_instance;
            }
        }

        /// <summary>
        /// Initializes singleton.
        /// </summary>
        public static void Initialize()
        {
            var whatever = Static;
        }

        // Context repository
        private readonly Dictionary<MyStringId, MyLocalizationContext> m_contexts 
            = new Dictionary<MyStringId, MyLocalizationContext>(MyStringId.Comparer);

        // Disposable context repository
        private readonly Dictionary<MyStringId, MyLocalizationContext> m_disposableContexts
            = new Dictionary<MyStringId, MyLocalizationContext>(MyStringId.Comparer);

        // Loaded bundles repository (is there to check whenever we are trying to load
        // something multiple times)
        private readonly Dictionary<MyStringId, MyBundle> m_loadedBundles 
            = new Dictionary<MyStringId, MyBundle>(MyStringId.Comparer); 

        private MyLocalization()
        {
        }

        // Loads data from content folder
        private void Init()
        {
            var localizationFiles = MyFileSystem.GetFiles(
                Path.Combine(MyFileSystem.ContentPath, LOCALIZATION_FOLDER), "*.sbl", MySearchOption.AllDirectories);

            foreach (var localizationFilePath in localizationFiles)
            {
                LoadLocalizationFile(localizationFilePath, MyStringId.NullOrEmpty);
            }
        }

        private MyLocalizationContext LoadLocalizationFile(string filePath, MyStringId bundleId, bool disposableContext = false)
        {
            MyStringId fileContextId = MyStringId.NullOrEmpty;
            string fileLanguage = null;
            ulong fileId = UInt64.MaxValue;
            bool? fileIsDefault = null;
            bool valid = false;

            if(!MyFileSystem.FileExists(filePath))
                return null;


            try
            {
                // Just reading part of the file. This does not parse it whole.
                int missedThreshold = 3;
                int missed = 0;
                using (XmlReader reader = XmlReader.Create(filePath))
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            switch (reader.Name)
                            {
                                case "Context":
                                    reader.Read();
                                    fileContextId = MyStringId.GetOrCompute(reader.Value);
                                    break;
                                case "Language":
                                    reader.Read();
                                    fileLanguage = reader.Value;
                                    break;
                                case "Id":
                                    reader.Read();
                                    fileId = ulong.Parse(reader.Value);
                                    break;
                                case "Default":
                                    reader.Read();
                                    fileIsDefault = bool.Parse(reader.Value);
                                    break;
                                default:
                                    // Stop reading, not worth it
                                    if (missed > missedThreshold)
                                        return null;

                                    missed++;
                                    break;
                            }
                        }

                        if (fileContextId != MyStringId.NullOrEmpty &&
                            fileLanguage != null &&
                            fileId != UInt64.MaxValue &&
                            fileIsDefault.HasValue)
                        {
                            valid = true;
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                valid = false;
                Debug.Fail(e.ToString());
            }

            if (valid)
            {
                var context = CreateOrGetContext(fileContextId, disposableContext);
                
                // insert the data to context
                context.InsertFileInfo(
                    new MyLocalizationContext.LocalizationFileInfo(fileLanguage, filePath, fileId, fileIsDefault.Value, bundleId));

                return context;
            }

            return null;
        }

        private MyLocalizationContext CreateOrGetContext(MyStringId contextId, bool disposable)
        {
            MyLocalizationContext context = null;
            if (!disposable)
            {
                m_contexts.TryGetValue(contextId, out context);
                if (context == null)
                {
                    m_contexts.Add(contextId, context = new MyLocalizationContext(contextId));

                    // Look for twin context
                    MyLocalizationContext twin;
                    if (m_disposableContexts.TryGetValue(contextId, out twin))
                    {
                        context.TwinContext = twin;
                        twin.TwinContext = context;
                    }
                }
            }
            else
            {
                m_disposableContexts.TryGetValue(contextId, out context);
                if (context == null)
                {
                    m_disposableContexts.Add(contextId, context = new MyLocalizationContext(contextId));

                    // Look for twin context
                    MyLocalizationContext twin;
                    if (m_contexts.TryGetValue(contextId, out twin))
                    {
                        context.TwinContext = twin;
                        twin.TwinContext = context;
                    }
                }
            }

            return context;
        }

        /// <summary>
        /// Switches all contexts to provided language.
        /// </summary>
        /// <param name="language">Language name.</param>
        public void Switch(string language)
        {
            foreach (var context in m_contexts.Values)
            {
                context.Switch(language);
            }

            foreach (var context in m_disposableContexts.Values)
            {
                context.Switch(language);
            }
        }

        /// <summary>
        /// Tries to dispose disposable context.
        /// </summary>
        /// <param name="nameId">Name id of context.</param>
        public bool DisposeContext(MyStringId nameId)
        {
            MyLocalizationContext context;
            if (m_disposableContexts.TryGetValue(nameId, out context))
            {
                context.Dispose();
                m_disposableContexts.Remove(nameId);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tries to dispose all disposable contexts.
        /// </summary>
        public void DisposeAll()
        {
            m_disposableContexts.Values.ForEach(context => context.Dispose());
            m_disposableContexts.Clear();
        }

        /// <summary>
        /// Loads bundle of files under bundle id.
        /// </summary>
        /// <param name="bundle">Data bundle.</param>
        /// <param name="influencedContexts">Contexts that got some new data in the process.</param>
        public void LoadBundle(MyBundle bundle, List<MyLocalizationContext> influencedContexts = null)
        {
            // this better should not happen
            if (m_loadedBundles.ContainsKey(bundle.BundleId))
            {
                NotifyBundleConflict(bundle.BundleId);
                return;
            }

            foreach (var filePath in bundle.FilePaths)
            {
                var context = LoadLocalizationFile(filePath, bundle.BundleId, true);
                if(context != null && influencedContexts != null)
                {
                    influencedContexts.Add(context);
                }
            }
        }

        /// <summary>
        /// Unloads bundle of files from the system by given id.
        /// </summary>
        /// <param name="bundleId"></param>
        public void UnloadBundle(MyStringId bundleId)
        {
            foreach (var context in m_contexts.Values)
            {
                context.UnloadBundle(bundleId);
            }
        }

        /// <summary>
        /// Simplified accessor.
        /// </summary>
        /// <param name="contextName">Name id of context.</param>
        /// <param name="tag">Tag to translate.</param>
        /// <returns>Localized String builder.</returns>
        public StringBuilder this[MyStringId contextName, MyStringId tag]
        {
            get
            {
                return Get(contextName, tag);
            }
        }

        /// <summary>
        /// Simplified accessor. Preferably use the string id version.
        /// </summary>
        /// <param name="contexName">Name of the context.</param>
        /// <param name="tag">Name of the tag.</param>
        /// <returns></returns>
        public StringBuilder this[string contexName, string tag]
        {
            get
            {
                return this[MyStringId.GetOrCompute(contexName), MyStringId.GetOrCompute(tag)];
            }
       } 

        /// <summary>
        /// Simplified accessor.
        /// </summary>
        /// <param name="contextName">Name id of context.</param>
        /// <returns>Context of given name.</returns>
        public MyLocalizationContext this[MyStringId contextName]
        {
            get
            {
                MyLocalizationContext context;
                m_contexts.TryGetValue(contextName, out context);
                return context;
            }
        }

        /// <summary>
        /// Simplified accessor. Preferably use the string id version.
        /// </summary>
        /// <param name="contextName">Name id of context.</param>
        /// <returns>Context of given name.</returns>
        public MyLocalizationContext this[string contextName]
        {
            get
            {
                MyLocalizationContext context;
                m_contexts.TryGetValue(MyStringId.GetOrCompute(contextName), out context);
                return context;
            }
        }

        /// <summary>
        /// Returns localization for given context and id.
        /// </summary>
        /// <param name="contextId">Context name id.</param>
        /// <param name="id">Message identifier.</param>
        /// <returns>String builder with localization.</returns>
        public StringBuilder Get(MyStringId contextId, MyStringId id)
        {
            MyLocalizationContext context;
            StringBuilder sb = m_defaultLocalization;
            if (m_disposableContexts.TryGetValue(contextId, out context))
            {
                sb = context.Localize(id);
                if(sb != null)
                    return sb;
            }
            
            if(m_contexts.TryGetValue(contextId, out context))
                sb = context.Localize(id);

            return sb;
        }

        // Creates error message for conflicting bundles.
        private void NotifyBundleConflict(MyStringId bundleId)
        {
            var errorMsg = "MyLocalization: Bundle conflict - Bundle already loaded: " + bundleId.String;
            Debug.Fail(errorMsg);
            MyLog.Default.WriteLine(errorMsg);
        }
    }
}
