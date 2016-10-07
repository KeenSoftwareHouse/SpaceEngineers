using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using VRageMath;
using VRageRender;

using NameToRecordDictionary = System.Collections.Generic.Dictionary<string, VRage.Render11.Tools.MyStatsDisplay.MyRecord>;
using GroupToNameDictionary = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, VRage.Render11.Tools.MyStatsDisplay.MyRecord>>;
using PageToGroupDictionary = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, VRage.Render11.Tools.MyStatsDisplay.MyRecord>>>;

namespace VRage.Render11.Tools
{
    internal class MyStatsDisplay
    {
        static readonly Color COLOR = Color.Yellow;
        const float FONT_SCALE = 0.6f;
        static readonly Vector2I SCREEN_OFFSET = new Vector2I(10, 10);
        const int MAX_LINES_PER_COLUMN = 45;
        const int MIN_LINES_PER_COLUMN = 40;
        const int COLUMN_WIDTH = 300;

        internal enum MyLifetime
        {
            PERSISTENT,
            ONE_FRAME,
        }

        internal class MyRecord
        {
            public int Value;
            public MyLifetime Lifetime;
        }

        static readonly List<string> m_orderedPages = new List<string>();
        static readonly PageToGroupDictionary m_records = new PageToGroupDictionary();

        private static IEnumerator<string> m_pageEnumerator = GetPageEnumerator();

        static readonly StringBuilder m_tmpStringBuilder = new StringBuilder(4096);
        
        static void WriteInternal(string pageName, string groupName, string name, MyLifetime lifetime, int value)
        {
            Debug.Assert(pageName != null && groupName != null && name != null, "Invalid dictionary keys.");

            GroupToNameDictionary page;
            if (!m_records.TryGetValue(pageName, out page))
            {
                page = new GroupToNameDictionary();
                m_records.Add(pageName, page);
                m_orderedPages.Add(pageName);
            }

            NameToRecordDictionary group;
            if (!page.TryGetValue(groupName, out group))
            {
                group = new NameToRecordDictionary();
                page.Add(groupName, group);
            }

            MyRecord record;
            if (!group.TryGetValue(name, out record))
            {
                record = new MyRecord();
                group.Add(name, record);
            }

            record.Lifetime = lifetime;
            record.Value = value;
        }

        static void DrawText(StringBuilder text, int nColumn)
        {
            Vector2I pos = SCREEN_OFFSET + new Vector2I(nColumn * COLUMN_WIDTH, 0);
            MySpritesRenderer.DrawText(pos, text, COLOR, FONT_SCALE);
        }

        static IEnumerator<string> GetPageEnumerator()
        {
            int i = 0;

            while (true)
            {
                if (i >= m_orderedPages.Count)
                    break;

                yield return m_orderedPages[i++];
            }
        }

        public static void Draw()
        {
            string pageName = m_pageEnumerator.Current;

            if (pageName == null)
                return;

            var page = m_records[pageName];

            int nColumn = 0;
            int nLine = 0;

            m_tmpStringBuilder.Clear();
            m_tmpStringBuilder.Append(pageName);
            m_tmpStringBuilder.AppendLine(":");
            nLine++;
            nLine++;

            foreach (var group in page)
            {
                m_tmpStringBuilder.Append("  ");
                m_tmpStringBuilder.AppendLine(group.Key);
                nLine++;

                foreach (var record in group.Value)
                {
                    MyRecord v = record.Value;
                    m_tmpStringBuilder.AppendFormat("    {0}: {1:#,0}", record.Key, v.Value);
                    m_tmpStringBuilder.AppendLine();
                    nLine++;

                    if (nLine + 1 == MAX_LINES_PER_COLUMN)
                    {
                        DrawText(m_tmpStringBuilder, nColumn);
                        m_tmpStringBuilder.Clear();
                        nColumn++;
                        nLine = 0;
                    }

                    if (v.Lifetime != MyLifetime.PERSISTENT)
                        v.Value = 0;
                }

                m_tmpStringBuilder.AppendLine();
                nLine++;

                if (nLine + 1 >= MIN_LINES_PER_COLUMN)
                {
                    DrawText(m_tmpStringBuilder, nColumn);
                    m_tmpStringBuilder.Clear();
                    nColumn++;
                    nLine = 0;
                }
            }
            DrawText(m_tmpStringBuilder, nColumn);
            m_tmpStringBuilder.Clear();
        }

        public static void Write(string group, string name, int value, string page = "Common")
        {
            WriteInternal(page, group, name, MyLifetime.ONE_FRAME, value);
        }

        public static void WritePersistent(string group, string name, int value, string page = "Common")
        {
            WriteInternal(page, group, name, MyLifetime.PERSISTENT, value);
        }

        public static bool MoveToNextPage()
        {
            bool res = m_pageEnumerator.MoveNext();

            if (!res)
                m_pageEnumerator = GetPageEnumerator();

            return res;
        }
    }
}
