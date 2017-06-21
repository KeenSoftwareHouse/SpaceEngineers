using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Sandbox.Common;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;

namespace Sandbox.Game.Gui
{
    public partial class MyHudNotification : IMyHudNotification
    {
        string IMyHudNotification.Text
        {
            get { return this.GetText(); }
            set { SetTextFormatArguments(value); }
        }

        int IMyHudNotification.AliveTime
        {
            get { return this.m_lifespanMs; }
            set
            {
                m_lifespanMs = value;
                ResetAliveTime();
            }
        }

        string IMyHudNotification.Font
        {
            get { return Font; }
            set { Font = value; }
        }

        void IMyHudNotification.Show()
        {
            MyHud.Notifications.Add(this);
        }

        void IMyHudNotification.Hide()
        {
            MyHud.Notifications.Remove(this);
        }

        void IMyHudNotification.ResetAliveTime()
        {
            this.ResetAliveTime();
        }
    }
}
