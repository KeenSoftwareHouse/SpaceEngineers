using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using VRage.Collections;
using VRage.Game.ObjectBuilders;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace VRage.Game.Localization
{
    /// <summary>
    /// Class designed around an idea of localization contexts.
    /// Context can be game, gui screen, mission, campaign or a task.
    /// Consists of a multitude of files stored in content folder.
    /// Each context can be modded, same way as created.
    /// </summary>
    public class MyLocalizationContext
    {
        internal struct LocalizationFileInfo
        {
            public readonly string Language;
            public readonly string Path;
            public readonly ulong Id;
            public readonly bool IsDefault;
            public readonly MyStringId Bundle;

            public LocalizationFileInfo(string language, string path, ulong id, bool isDefault, MyStringId bundle)
            {
                Language = language;
                Path = path;
                Bundle = bundle;
                Id = id;
                IsDefault = isDefault;
            }
        }

        // Name of this context
        protected readonly MyStringId m_contextName;
        // Helper for gathering of language names
        protected readonly List<string> m_languagesHelper = new List<string>();
        // Per localization file metadata
        private readonly List<LocalizationFileInfo> m_localizationFileInfos = new List<LocalizationFileInfo>();
        // Already loaded localization files (filePath|data)
        protected readonly Dictionary<MyStringId, MyObjectBuilder_Localization> m_loadedFiles = new Dictionary<MyStringId, MyObjectBuilder_Localization>(MyStringId.Comparer);
        // Merged dictionary from all language files
        protected readonly Dictionary<MyStringId, StringBuilder> m_idsToTexts = new Dictionary<MyStringId, StringBuilder>(MyStringId.Comparer);

        // Repository for allocated string builders (may cause a memory overhead, but will save us from allocating frequently)
        private static readonly ConcurrentDictionary<MyStringId, StringBuilder> m_allocatedStringBuilders
            = new ConcurrentDictionary<MyStringId, StringBuilder>(MyStringId.Comparer);

        /// <summary>
        /// Defined languages.
        /// </summary>
        public ListReader<string> Languages
        {
            get
            {
                return m_languagesHelper;
            }
        }

        /// <summary>
        /// All accessible ids from context.
        /// </summary>
        public IEnumerable<MyStringId> Ids
        {
            get
            {
                return m_idsToTexts.Keys;
            }
        }  

        /// <summary>
        /// Name of this context.
        /// </summary>
        public MyStringId Name
        {
            get { return m_contextName; }
        }

        /// <summary>
        /// Currently selected language.
        /// </summary>
        public string CurrentLanguage
        {
            get; private set;
        }

        private MyLocalizationContext m_twinContext;
        /// <summary>
        /// Context of same name. Basicaly connection between
        /// non disposable and disposable contexts.
        /// </summary>
        internal MyLocalizationContext TwinContext
        {
            get
            {
                return m_twinContext;
            }

            set
            {
                // Twin contexts must have same name
                Debug.Assert(value.Name != Name);
                m_twinContext = value;
            }
        }

        /// <summary>
        /// Clears all data before shutting down context.
        /// </summary>
        public void Dispose()
        {
            m_languagesHelper.Clear();
            m_idsToTexts.Clear();
            m_loadedFiles.Clear();
            m_switchHelper.Clear();
            m_localizationFileInfos.Clear();
        }

        #region Internal methods

        internal MyLocalizationContext(MyStringId name)
        {
            m_contextName = name;
        }

        // Removes mod data from context and reloads its structure.
        internal void UnloadBundle(MyStringId bundleId)
        {
            for (var index = 0; index < m_localizationFileInfos.Count;)
            {
                var fileInfo = m_localizationFileInfos[index];
                // Unloading only mod content for speciefied mod folder.
                // For mod folder that is not specified remove all content from mods.
                if (bundleId == MyStringId.NullOrEmpty && fileInfo.Bundle != MyStringId.NullOrEmpty || 
                    fileInfo.Bundle == bundleId && fileInfo.Bundle != MyStringId.NullOrEmpty)
               {
                    m_loadedFiles.Remove(MyStringId.GetOrCompute(fileInfo.Path));
                    m_localizationFileInfos.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }

            // Reloads everything
            Switch(CurrentLanguage);   
        }

        // Insert file info of partialy loaded file
        internal void InsertFileInfo(LocalizationFileInfo info)
        {
            m_localizationFileInfos.Add(info);
            // Update languages helper
            if(!m_languagesHelper.Contains(info.Language))
                m_languagesHelper.Add(info.Language);
        }

        // Loads the context data from metadata
        private MyObjectBuilder_Localization Load(LocalizationFileInfo fileInfo)
        {
            MyObjectBuilder_Localization locFile;
            if (!m_loadedFiles.TryGetValue(MyStringId.GetOrCompute(fileInfo.Path), out locFile))
            {
                if (MyObjectBuilderSerializer.DeserializeXML(fileInfo.Path, out locFile))
                {
                    m_loadedFiles.Add(MyStringId.GetOrCompute(fileInfo.Path), locFile);
                }
                else
                {
                    var errorMsg = "Error occured while deserializing localization file: " + fileInfo.Path;
                    Debug.Fail(errorMsg);
                    MyLog.Default.WriteLine(errorMsg);
                }
            }

            return locFile;
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Simplified accessor.
        /// </summary>
        /// <param name="id">Tag to localize.</param>
        /// <returns>Localized String Builder.</returns>
        public StringBuilder this[MyStringId id]
        {
            get
            {
                return Localize(id);
            }
        }

        /// <summary>
        /// Simplified accessor. Preferably use the string id version.
        /// </summary>
        /// <param name="nameId">Name identifier. (will be converted to MyStringId)</param>
        /// <returns>Localized String Builder.</returns>
        public StringBuilder this[string nameId]
        {
            get
            {
                return Localize(MyStringId.GetOrCompute(nameId));
            }
        }


        private readonly HashSet<ulong> m_switchHelper = new HashSet<ulong>();  
        /// <summary>
        /// Tries to switch context to provided language.
        /// </summary>
        /// <param name="language"></param>
        public void Switch(string language)
        {
            CurrentLanguage = language;
            m_idsToTexts.Clear();
            m_switchHelper.Clear();
            // Insert vanilla content for respective language choice
            foreach (var fileInfo in m_localizationFileInfos)
            {
                if(fileInfo.Language != language || fileInfo.Bundle != MyStringId.NullOrEmpty)
                    continue;

                m_switchHelper.Add(fileInfo.Id);

                var locFile = Load(fileInfo);
                LoadLocalizationFileData(locFile);
            }

            // Override values from vanilla content with mod content
            foreach (var fileInfo in m_localizationFileInfos)
            {
                if(fileInfo.Language != language || fileInfo.Bundle == MyStringId.NullOrEmpty)
                    continue;

                m_switchHelper.Add(fileInfo.Id);

                var locFile = Load(fileInfo);
                // OVERRIDE
                LoadLocalizationFileData(locFile, true);
            }

            // Fill the missing members by default values
            foreach (var fileInfo in m_localizationFileInfos)
            {
                // skip all already loaded files
                if(m_switchHelper.Contains(fileInfo.Id))
                    continue;

                // skip all files that are not default
                if(!fileInfo.IsDefault)
                    continue;

                var locFile = Load(fileInfo);
                // we just want to fill in the not already assigned values
                LoadLocalizationFileData(locFile, suppressError: true);
            }
        }

        // Inserts new values to merged dictionary or overrides existing values
        private void LoadLocalizationFileData(MyObjectBuilder_Localization localization, bool overrideExisting = false, bool suppressError = false)
        {
            foreach (var keyValuePair in localization.Entries.Dictionary)
                {
                    // Try retriving the string builder from already allocated stuff
                    var sb = AllocateOrGet(keyValuePair.Value);
                    var tag = MyStringId.GetOrCompute(keyValuePair.Key);    

                    // Update the dictionary entries
                    if (!m_idsToTexts.ContainsKey(tag))
                    {
                        m_idsToTexts.Add(tag, sb);
                    }
                    else
                    {
                        // For vanilla content we want to warn the users of overriting existing tags
                        if(!overrideExisting)
                        {
                            if (!suppressError)
                            {
                                var errorMsg = "LocalizationContext: Context " + m_contextName.String +
                                               " already contains id " +
                                               keyValuePair.Key + " conflicting entry won't be overriten.";
                                Debug.Fail(errorMsg);
                                MyLog.Default.WriteLine(errorMsg);
                            }
                        } else
                        {
                            // For mod content we don't care
                            m_idsToTexts[tag] = sb;
                        }
                    }
                }
        }

        /// <summary>
        /// Creates or provides already existing string builder.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        protected StringBuilder AllocateOrGet(string text)
        {
            StringBuilder sb;
            m_allocatedStringBuilders.TryGetValue(MyStringId.GetOrCompute(text), out sb);

            if (sb == null)
            {
                // Allocate new
                sb = new StringBuilder(text);
                m_allocatedStringBuilders.TryAdd(MyStringId.GetOrCompute(text), sb);
            }

            return sb;
        }

        /// <summary>
        /// Retrives the localized content from entry with provided id.
        /// </summary>
        /// <param name="id">Unique identifier.</param>
        /// <returns>Localized builder.</returns>
        public StringBuilder Localize(MyStringId id)
        {
            StringBuilder sb;
            if(m_idsToTexts.TryGetValue(id, out sb))
                return sb;

            // Take a look into twin context for localization.
            if (TwinContext != null)
            {
                return TwinContext.Localize(id);
            }

            return null;
        }

        #endregion


        #region Object Overrides

        public override int GetHashCode()
        {
            return m_contextName.Id;
        }

        protected bool Equals(MyLocalizationContext other)
        {
            return m_contextName.Equals(other.m_contextName);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj is MyStringId)
            {
                return m_contextName.Equals((MyStringId) obj);
            }
            return Equals((MyLocalizationContext) obj);
        }

        #endregion

    }
}
