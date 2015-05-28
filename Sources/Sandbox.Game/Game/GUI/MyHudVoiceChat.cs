using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Gui
{
    public class MyHudVoiceChat
    {
        public bool Visible
        {
            get;
            private set;
        }

        public event Action<bool> VisibilityChanged;

        public void Show()
        {
            Visible = true;
            if (VisibilityChanged != null)
                VisibilityChanged(true);
        }

        public void Hide()
        {
            Visible = false;
            if (VisibilityChanged != null)
                VisibilityChanged(false);
        }
    }
}
