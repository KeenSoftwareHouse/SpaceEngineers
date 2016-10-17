using Sandbox.Engine.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Network;
using Sandbox.Game.Screens;
using VRageMath;
using Sandbox.Graphics.GUI;

namespace Sandbox.Game.Gui
{
    [StaticEventOwner]
    public class MyHudQuestlog
    {

        private static readonly int MAX_ROWS = 5;
        private struct QuestInfo
        {
            public String title;
            public Dictionary<int, MultilineData> content;

        }

        private struct MultilineData
        {
            public int lines;
            public string data;
        }

        private QuestInfo m_questInfo;
        private bool m_isVisible = false;
        private int m_latestGood = 0;
        private int m_page = 0;

        public event Action ValueChanged;

        public readonly Vector2 QuestlogSize = new Vector2(0.4f, 0.2f);

        public String QuestTitle
        {
            get
            {
                return m_questInfo.title;
            }
            set
            {
                m_questInfo.title = value;
                RaiseValueChanged();
            }
        }

        private void RaiseValueChanged()
        {
            if (ValueChanged != null)
                ValueChanged();
        }

        public string[] GetQuestGetails()
        {
            if (m_questInfo.content == null)
            {
                m_questInfo.content = new Dictionary<int, MultilineData>(MAX_ROWS);
            }
            string[] ret = new string[MAX_ROWS];
            int pageCount = 1;
            int idx = 0;
            int i = 0;
            foreach (var key in m_questInfo.content.Keys)
            {
                idx += m_questInfo.content[key].lines;
                if (idx > MAX_ROWS)
                {
                    pageCount++;
                    idx = m_questInfo.content[key].lines;
                }
                if (pageCount - 1 == m_page)
                {
                    ret[i] = m_questInfo.content[key].data;
                    i++;
                }
            }
            return ret;
        }

        public string[] GetQuestGetails(int page)
        {
            m_page = page;
            return GetQuestGetails();
        }

        public int GetPageFromMessage(int id)
        {
            int pageCount = 1;
            int idx = 0;
            foreach (var key in m_questInfo.content.Keys)
            {
                if (key == id)
                    return pageCount;
                idx += m_questInfo.content[key].lines;
                if (idx > MAX_ROWS)
                {
                    pageCount++;
                    idx = m_questInfo.content[key].lines;
                }
            }
            return 1;
        }

        public int Page
        {
            get
            {
                return m_page + 1;
            }
            set
            {
                if (value <= MaxPages)
                    m_page = value - 1;
                if (m_page < 0)
                    m_page = 0;
                RaiseValueChanged();
            }
        }

        public int MaxPages
        {
            get
            {
                if (m_questInfo.content == null)
                {
                    m_questInfo.content = new Dictionary<int, MultilineData>(MAX_ROWS);
                }
                int pageCount = 1;
                int idx = 0;
                foreach (var key in m_questInfo.content.Keys)
                {
                    idx += m_questInfo.content[key].lines;
                    if (idx > MAX_ROWS)
                    {
                        pageCount++;
                        idx = m_questInfo.content[key].lines;
                    }
                }
                return pageCount;
            }
        }

        /// <summary>
        /// Set Visibility for Questlog
        /// </summary>
        public bool Visible
        {
            get
            {
                return m_isVisible;
            }
            set
            {
                m_isVisible = value;
            }
        }

        /// <summary>
        /// Enable to blink animation when value is changed.
        /// Default true.
        /// </summary>
        public bool HighlightChanges = true;

        /// <summary>
        /// Cleanup detail
        /// </summary>
        public void CleanDetails()
        {
            if (m_questInfo.content == null)
            {
                m_questInfo.content = new Dictionary<int, MultilineData>(MAX_ROWS);
            }
            m_questInfo.content.Clear();
            m_latestGood = -1;
            m_page = 0;
            RaiseValueChanged();
        }

        /// <summary>
        /// Add value to next row of quest details.
        /// Rotate over available rows.
        /// </summary>
        /// <param name="value"></param>
        public int AddDetail(String value)
        {
            if (m_questInfo.content == null)
            {
                m_questInfo.content = new Dictionary<int, MultilineData>(MAX_ROWS);
            }
            m_latestGood += 1;
            m_questInfo.content.Add(m_latestGood, ComputeLineDataFromString(value));
            RaiseValueChanged();
            return m_latestGood;
        }

        public void RemoveDetail(int id)
        {
            m_questInfo.content.Remove(id);
            RaiseValueChanged();
        }

        public void ModifyDetail(int id, string value)
        {
            if (m_questInfo.content == null)
            {
                m_questInfo.content = new Dictionary<int, MultilineData>(MAX_ROWS);
            }
            m_questInfo.content[id] = ComputeLineDataFromString(value);
            RaiseValueChanged();
        }

        private MultilineData ComputeLineDataFromString(string value)
        {
            MultilineData ret;
            ret.data = value;
            
            MyGuiControlMultilineText textBox = new MyGuiControlMultilineText(size: new Vector2(QuestlogSize.X * 0.92f, 1), drawScrollbar: false);
            textBox.Visible = false;
            textBox.TextScale = 0.9f;
            textBox.AppendText(value);
            
            ret.lines = textBox.NumberOfRows;
            return ret;
        }

        //public void SendQuestToClient(ulong playerSteamID)
        //{
        //    MyMultiplayer.RaiseStaticEvent(x => Template, m_questInfo, new EndpointId(playerSteamID));
        //}

        //[Event,Broadcast,Client]
        //public static void OnQuestInfoRecieved(MyHudQuestlog recieve)
        //{
        //    MyHud.Questlog.m_questInfo = recieve;
        //}

    }
}
