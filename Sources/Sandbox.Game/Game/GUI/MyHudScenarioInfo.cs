#region Using

using Sandbox.Common;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.World;
using System;
using System.Diagnostics;
using System.Text;
using VRage;
using VRage;

#endregion

namespace Sandbox.Game.Gui
{
    public class MyHudScenarioInfo
    {
        private enum LineEnum
        {
            LivesLeft,
        }

        private int m_livesLeft=-1;
        public int LivesLeft
        {
            get { return m_livesLeft; }
            set
            {
                if (m_livesLeft != value)
                {
                    m_livesLeft = value;
                    m_needsRefresh = true;
                    Visible = true;
                }
            }
        }

        private bool m_needsRefresh = true;

        public MyHudNameValueData Data
        {
            get { if (m_needsRefresh) Refresh(); return m_data; }
        }
        private MyHudNameValueData m_data;

        public MyHudScenarioInfo()
        {
            m_data = new MyHudNameValueData(typeof(LineEnum).GetEnumValues().Length);
            Reload();
        }

        public void Reload()
        {
            var items = m_data;
            items[(int)LineEnum.LivesLeft].Name.Clear().AppendStringBuilder(MyTexts.Get(MySpaceTexts.HudScenarioInfoLivesLeft));
            m_livesLeft = -1;
            m_needsRefresh = true;
        }

        public void Refresh()
        {
            m_needsRefresh = false;
            if (LivesLeft>=0)
            {
                Data[(int)LineEnum.LivesLeft].Value.Clear().AppendInt32(LivesLeft);
                Data[(int)LineEnum.LivesLeft].Visible=true;
            }
            else
                Data[(int)LineEnum.LivesLeft].Visible=false;

            if (Data.GetVisibleCount() == 0)
                Visible = false;
            else
                Visible = true;
        }


        public bool Visible { get; private set; }

        public void Show(Action<MyHudScenarioInfo> propertiesInit)
        {
            Visible = true;
            if (propertiesInit != null)
                propertiesInit(this);
        }

        public void Hide()
        {
            Visible = false;
        }

    }
}

