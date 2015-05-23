using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Resources;
using System.Text;
using VRage.Collections;
using VRage.FileSystem;
using VRage.Utils;

namespace VRage
{
    public enum MyLanguagesEnum : byte
    {
        // values matter, they are saved in config
        // when making localizations moddable (if ever), we should get rid of this enum

        English = 0,
        Czech = 1,
        Slovak = 2,
        German = 3,
        Russian = 4,
        Spanish_Spain = 5,
        French = 6,
        Italian = 7,
        Danish = 8,
        Dutch = 9,
        Icelandic = 10,
        Polish = 11,
        Finnish = 12,
        Hungarian = 13,
        Portuguese_Brazil = 14,
        Estonian = 15,
        Norwegian = 16,
        Spanish_HispanicAmerica = 17,
        Swedish = 18,
        Catalan = 19,
        Croatian = 20,
        Romanian = 21,
        Ukrainian = 22,
        Turkish = 23,
        Latvian = 24,
    }


    public static class MyTexts
    {
        public class LanguageDescription
        {
            public readonly MyLanguagesEnum Id;
            public readonly string Name;
            public readonly string CultureName;
            public readonly string SubcultureName;
            public readonly string FullCultureName;
            public readonly bool IsCommunityLocalized;
            public readonly float GuiTextScale;

            internal LanguageDescription(MyLanguagesEnum id, string displayName, string cultureName, string subcultureName, float guiTextScale, bool isCommunityLocalized)
            {
                Id = id;
                Name = displayName;
                CultureName = cultureName;
                SubcultureName = subcultureName;
                if (string.IsNullOrWhiteSpace(subcultureName))
                    FullCultureName = cultureName;
                else
                    FullCultureName = string.Format("{0}-{1}", cultureName, subcultureName);
                IsCommunityLocalized = isCommunityLocalized;
                GuiTextScale = guiTextScale;
            }

        }

        private static Dictionary<int, LanguageDescription> m_languageIdToLanguage = new Dictionary<int, LanguageDescription>();
        private static Dictionary<string, int> m_cultureToLanguageId = new Dictionary<string, int>();

        private static Dictionary<MyStringId, string> m_strings = new Dictionary<MyStringId, string>(MyStringId.Comparer);
        private static Dictionary<MyStringId, StringBuilder> m_stringBuilders = new Dictionary<MyStringId, StringBuilder>(MyStringId.Comparer);

        static MyTexts()
        {
            // Add new languages to the end of list (they are indexed and reordering will break existing config files
            AddLanguage(MyLanguagesEnum.English,                 "en",       displayName: "English", isCommunityLocalized: false);
            AddLanguage(MyLanguagesEnum.Czech,                   "cs", "CZ", displayName: "Česky", guiTextScale: 0.95f);
            AddLanguage(MyLanguagesEnum.Slovak,                  "sk", "SK", displayName: "Slovenčina", guiTextScale: 0.95f);
            AddLanguage(MyLanguagesEnum.German,                  "de",       displayName: "Deutsch", guiTextScale: 0.85f);
            AddLanguage(MyLanguagesEnum.Russian,                 "ru",       displayName: "Русский");
            AddLanguage(MyLanguagesEnum.Spanish_Spain,           "es",       displayName: "Español (España)");
            AddLanguage(MyLanguagesEnum.French,                  "fr",       displayName: "Français");
            AddLanguage(MyLanguagesEnum.Italian,                 "it",       displayName: "Italiano");
            AddLanguage(MyLanguagesEnum.Danish,                  "da",       displayName: "Dansk");
            AddLanguage(MyLanguagesEnum.Dutch,                   "nl",       displayName: "Nederlands");
            AddLanguage(MyLanguagesEnum.Icelandic,               "is", "IS", displayName: "Íslenska");
            AddLanguage(MyLanguagesEnum.Polish,                  "pl", "PL", displayName: "Polski");
            AddLanguage(MyLanguagesEnum.Finnish,                 "fi",       displayName: "Suomi");
            AddLanguage(MyLanguagesEnum.Hungarian,               "hu", "HU", displayName: "Magyar", guiTextScale: 0.85f);
            AddLanguage(MyLanguagesEnum.Portuguese_Brazil,       "pt", "BR", displayName: "Português (Brasileiro)");
            AddLanguage(MyLanguagesEnum.Estonian,                "et", "EE", displayName: "Eesti");
            AddLanguage(MyLanguagesEnum.Norwegian,               "no",       displayName: "Norsk");
            AddLanguage(MyLanguagesEnum.Spanish_HispanicAmerica, "es","419", displayName: "Español (Latinoamerica)");
            AddLanguage(MyLanguagesEnum.Swedish,                 "sv",       displayName: "Svenska", guiTextScale: 0.9f);
            AddLanguage(MyLanguagesEnum.Catalan,                 "ca", "AD", displayName: "Català", guiTextScale: 0.85f);
            AddLanguage(MyLanguagesEnum.Croatian,                "hr", "HR", displayName: "Hrvatski", guiTextScale: 0.9f);
            AddLanguage(MyLanguagesEnum.Romanian,                "ro",       displayName: "Română", guiTextScale: 0.85f);
            AddLanguage(MyLanguagesEnum.Ukrainian,               "uk",       displayName: "Українська");
            AddLanguage(MyLanguagesEnum.Turkish,                 "tr", "TR", displayName: "Türkçe");
            AddLanguage(MyLanguagesEnum.Latvian,                 "lv",       displayName: "Latviešu", guiTextScale: 0.87f);
        }

        private static void AddLanguage(MyLanguagesEnum id, string cultureName, string subcultureName = null, string displayName = null, float guiTextScale = 1f, bool isCommunityLocalized = true)
        {
            var language = new LanguageDescription(id, displayName, cultureName, subcultureName, guiTextScale, isCommunityLocalized);
            m_languageIdToLanguage.Add((int)id, language);
            m_cultureToLanguageId.Add(language.FullCultureName, (int)id);
        }

        public static DictionaryReader<int, LanguageDescription> Languages
        {
            get { return m_languageIdToLanguage; }
        }

        public static void LoadSupportedLanguages(string rootDirectory, HashSet<int> outSupportedLanguages)
        {
            // we always support English
            outSupportedLanguages.Add((int)MyLanguagesEnum.English);

            var files = MyFileSystem.GetFiles(rootDirectory, "*.resx", FileSystem.MySearchOption.TopDirectoryOnly);
            HashSet<string> foundCultures = new HashSet<string>();
            foreach (var file in files)
            {
                var parts = Path.GetFileNameWithoutExtension(file).Split('.');
                Debug.Assert(parts.Length == 1 || parts.Length == 2);
                if (parts.Length > 1)
                    foundCultures.Add(parts[1]);
            }

            foreach (var culture in foundCultures)
            {
                int id;
                if (m_cultureToLanguageId.TryGetValue(culture, out id))
                    outSupportedLanguages.Add(id);
            }
        }

        public static StringBuilder Get(MyStringId id)
        {
            StringBuilder result;
            if (!m_stringBuilders.TryGetValue(id, out result))
            {
                result = new StringBuilder(id.ToString());
                //Debug.Fail(string.Format("Missing text for localization. Id: {0}", id.ToString()));
            }

            return result;
        }

        public static string GetString(MyStringId id)
        {
            string result;
            if (!m_strings.TryGetValue(id, out result))
            {
                result = id.ToString();
                //Debug.Fail(string.Format("Missing text for localization. Id: {0}", id.ToString()));
            }

            return result;
        }

        public static bool Exists(MyStringId id)
        {
            return m_strings.ContainsKey(id);
        }

        public static bool TryGet(MyStringId id, out string value)
        {
            return m_strings.TryGetValue(id, out value);
        }

        public static void Clear()
        {
            m_strings.Clear();
            m_stringBuilders.Clear();

            // null or empty string lookups should return empty string
            m_strings[default(MyStringId)] = "";
            m_stringBuilders[default(MyStringId)] = new StringBuilder();
        }

        public static void LoadTexts(string rootDirectory, string cultureName = null, string subcultureName = null)
        {
            HashSet<string> baseFiles = new HashSet<string>();
            var files = MyFileSystem.GetFiles(rootDirectory, "*.resx", FileSystem.MySearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                baseFiles.Add(Path.GetFileNameWithoutExtension(file).Split('.')[0]);
            }

            foreach (var baseFile in baseFiles)
                PatchTexts(Path.Combine(rootDirectory, string.Format("{0}.resx", baseFile)));

            if (cultureName == null)
                return;

            foreach (var baseFile in baseFiles)
                PatchTexts(Path.Combine(rootDirectory, string.Format("{0}.{1}.resx", baseFile, cultureName)));

            if (subcultureName == null)
                return;

            foreach (var baseFile in baseFiles)
                PatchTexts(Path.Combine(rootDirectory, string.Format("{0}.{1}-{2}.resx", baseFile, cultureName, subcultureName)));
        }

        private static void PatchTexts(string resourceFile)
        {
            if (!File.Exists(resourceFile))
                return;

            using (var stream = MyFileSystem.OpenRead(resourceFile))
            using (var reader = new ResXResourceReader(stream))
            {
                foreach (DictionaryEntry entry in reader)
                {
                    string key = entry.Key as string;
                    string value = entry.Value as string;

                    Debug.Assert(key != null && value != null, string.Format("Text has incorrect format. [{0}:{1}]", key.GetType().Name, value.GetType().Name));
                    if (key == null || value == null)
                        continue;

                    var id = MyStringId.GetOrCompute(key);
                    m_strings[id] = value;
                    m_stringBuilders[id] = new StringBuilder(value);
                }
            }

        }

        public static StringBuilder AppendFormat(this StringBuilder stringBuilder, MyStringId textEnum, object arg0)
        {
            return stringBuilder.AppendFormat(GetString(textEnum), arg0);
        }
    }
}
