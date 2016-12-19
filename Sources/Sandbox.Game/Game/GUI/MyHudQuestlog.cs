using Sandbox.Engine.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Network;
using Sandbox.Game.Screens;
using Sandbox.Game.World;
using Sandbox.Game.WorldEnvironment.ObjectBuilders;
using VRageMath;
using Sandbox.Graphics.GUI;
using VRage.Game;
using VRage.Game.ObjectBuilders.Gui;
using VRage.Game.SessionComponents;

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

        public MultilineData[] GetQuestGetails()
        {
            if (m_questInfo.content == null)
            {
                m_questInfo.content = new Dictionary<int, MultilineData>(MAX_ROWS);
            }
            MultilineData[] ret = new MultilineData[MAX_ROWS];
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
                    ret[i] = m_questInfo.content[key];
                    i++;
                }
            }
            return ret;
        }

        public MultilineData[] GetQuestGetails(int page)
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
        public int AddDetail(String value, bool useTyping = true)
        {
            if (m_questInfo.content == null)
            {
                m_questInfo.content = new Dictionary<int, MultilineData>(MAX_ROWS);
            }
            m_latestGood += 1;
            m_questInfo.content.Add(m_latestGood, ComputeLineDataFromString(value));
            if (!useTyping)
                m_questInfo.content[m_latestGood].charactersDisplayed = m_questInfo.content[m_latestGood].data.Length;
            RaiseValueChanged();
            return m_latestGood;
        }

        public bool SetCompleted(int id, bool completed = true)
        {
            if (m_questInfo.content == null)
                return false;
            if (!m_questInfo.content.ContainsKey(id))
                return false;
            if (m_questInfo.content[id].completed == completed)
                return false;
            m_questInfo.content[id].completed = completed;
            RaiseValueChanged();
            return true;
        }

        public bool SetAllCompleted(bool completed = true)
        {
            if (m_questInfo.content == null)
                return false;
            foreach (var line in m_questInfo.content.Values)
                line.completed = completed;
            RaiseValueChanged();
            return true;
        }

        public void RemoveDetail(int id)
        {
            m_questInfo.content.Remove(id);
            RaiseValueChanged();
        }

        public void ModifyDetail(int id, string value, bool useTyping = true)
        {
            if (m_questInfo.content == null)
            {
                m_questInfo.content = new Dictionary<int, MultilineData>(MAX_ROWS);
            }
            m_questInfo.content[id] = ComputeLineDataFromString(value);
            if (!useTyping)
                m_questInfo.content[id].charactersDisplayed = m_questInfo.content[id].data.Length;
            RaiseValueChanged();
        }

        private MultilineData ComputeLineDataFromString(string value)
        {
            MultilineData ret = new MultilineData();
            ret.data = value;
            ret.completed = false;
            
            MyGuiControlMultilineText textBox = new MyGuiControlMultilineText(size: new Vector2(QuestlogSize.X * 0.92f, 1), drawScrollbar: false);
            textBox.Visible = false;
            textBox.TextScale = 0.9f;
            textBox.AppendText(value);
            
            ret.lines = textBox.NumberOfRows;
            return ret;
        }

        public void Save()
        {
            var comp = MySession.Static.GetComponent<MyVisualScriptManagerSessionComponent>();
            if (comp != null)
            {
                comp.QuestlogData = GetObjectBuilder();
            }
        }

        public MyObjectBuilder_Questlog GetObjectBuilder()
        {
            if(m_questInfo.content == null)
                return null;

            var ob = new MyObjectBuilder_Questlog
            {
                Title = QuestTitle,
                LineData = {Capacity = m_questInfo.content.Count}
            };

            foreach (var data in m_questInfo.content)
            {
                ob.LineData.Add(data.Value);
            }

            return ob;
        }

        public void Init()
        {
            var comp = MySession.Static.GetComponent<MyVisualScriptManagerSessionComponent>();
            if (comp != null)
            {
                var ob = comp.QuestlogData;
                if (ob != null)
                {
                    if (m_questInfo.content == null)
                    {
                        m_questInfo.content = new Dictionary<int, MultilineData>(MAX_ROWS);
                    }
                    else
                    {
                        m_questInfo.content.Clear();   
                    }

                    QuestTitle = ob.Title;
                    m_latestGood = 0;
                    for (int index = 0; index < ob.LineData.Count; index++)
                    {
                        m_questInfo.content.Add(index, ob.LineData[index]);
                        m_latestGood++;
                    }

                    if (ob.LineData.Count > 0)
                    {
                        Visible = true;
                    }
                }
            }
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
