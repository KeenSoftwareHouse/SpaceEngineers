using Sandbox.Graphics;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using VRage.Collections;
using VRage;
using VRage.Utils;
using VRage.Utils;
using VRage.Library.Utils;
using VRage.FileSystem;

namespace Sandbox.Game.Localization
{
    static class MyLanguage
    {
        private static MyLanguagesEnum m_actualLanguage;
        private static HashSet<int> m_supportedLanguages = new HashSet<int>();

        public static void Init()
        {
            MyTexts.LoadSupportedLanguages(GetLocalizationPath(), m_supportedLanguages);
            LoadLanguage(MyLanguagesEnum.English);
        }

        public static HashSetReader<int> SupportedLanguages
        {
            get { return m_supportedLanguages; }
        }

        public static MyLanguagesEnum CurrentLanguage
        {
            set
            {
                //  Change current culture so this will switch text resources to new language
                LoadLanguage(value);

                //  Save into config
                MySandboxGame.Config.Language = m_actualLanguage;
                MySandboxGame.Config.Save();
            }

            get
            {
                return m_actualLanguage;
            }
        }

        private static void LoadLanguage(MyLanguagesEnum value)
        {
            var language = MyTexts.Languages[(int)value];
            MyTexts.Clear();
            MyTexts.LoadTexts(GetLocalizationPath(), language.CultureName, language.SubcultureName);
            MyGuiManager.LanguageTextScale = language.GuiTextScale;
            m_actualLanguage = value;
        }

        private static string GetLocalizationPath()
        {
            return Path.Combine(MyFileSystem.ContentPath, "Data", "Localization");
        }

        [Conditional("DEBUG")]
        private static void GenerateCurrentLanguageCharTable()
        {
            SortedSet<char> charSet = new SortedSet<char>();
            foreach (MyStringId value in typeof(MyStringId).GetEnumValues())
            {
                var text = MyTexts.Get(value);
                for (int i = 0; i < text.Length; ++i)
                {
                    charSet.Add(text[i]);
                }
            }

            var charList = new List<char>(charSet);
            var userFolder = MyFileSystem.UserDataPath;
            var outputTableFile = string.Format("character-table-{0}.txt", CurrentLanguage);
            var outputTablePath = Path.Combine(userFolder, outputTableFile);
            using (var outputFile = new StreamWriter(outputTablePath))
            {
                foreach (var character in charList)
                {
                    outputFile.WriteLine(string.Format("{0}\t{1:x4}", character, (int)character));
                }
            }

            var outputRangesFile = string.Format("character-ranges-{0}.txt", CurrentLanguage);
            var outputRangesPath = Path.Combine(userFolder, outputRangesFile);
            using (var outputFile = new StreamWriter(outputRangesPath))
            {
                int i = 0;
                int start, end;
                while (i < charList.Count)
                {
                    end = start = (int)charList[i];
                    ++i;
                    while (i < charList.Count &&
                           ((int)charList[i] == end + 1))
                    {
                        end = charList[i];
                        ++i;
                    }
                    outputFile.WriteLine(string.Format("-range {0:x4}-{1:x4}", start, end));
                }
            }
        }

    }
}