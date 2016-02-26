#region Using

using Sandbox.Game.Entities;
using System;
using VRage.Game.Entity;


#endregion

namespace Sandbox.Game.Gui
{
    #region Gravity Indicator
    public class MyHudGravityIndicator
    {
        internal MyEntity Entity;

        public bool Visible { get; private set; }

        public void Show(Action<MyHudGravityIndicator> propertiesInit)
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
    #endregion
}
