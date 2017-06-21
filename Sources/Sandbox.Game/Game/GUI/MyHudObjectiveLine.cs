#region Using

using Sandbox.Common;

using Sandbox.Game.World;
using System;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using VRage;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

#endregion

namespace Sandbox.Game.Gui
{
    public class MyHudObjectiveLine : IMyHudObjectiveLine
    {
        private string m_missionTitle = "";
        private int m_currentObjective = 0;
        private List<string> m_objectives = new List<string>();

        public bool Visible { get; private set; }

        public string Title 
        {
            get { return m_missionTitle; }
            set { m_missionTitle = value; }
        }

        public string CurrentObjective
        {
            get { return m_objectives[m_currentObjective]; }
        }

        public MyHudObjectiveLine()
        {
            Visible = false;
        }

        public void Show()
        {
            Visible = true;
        }

        public void Hide()
        {
            Visible = false;
        }

        public void AdvanceObjective()
        {
            if (m_currentObjective < m_objectives.Count - 1)
            {
                m_currentObjective++;
            }
        }

        /// <summary>
        /// Sets first objective as active
        /// </summary>
        public void ResetObjectives()
        {
            m_currentObjective = 0;
        }

        public List<string> Objectives 
        {
            get
            {
                return m_objectives;
            }

            set
            {
                m_objectives = value;
            }
        }

        public void Clear()
        {
            m_missionTitle = "";
            m_currentObjective = 0;
            m_objectives.Clear();
            Visible = false;
        }
    }
}
