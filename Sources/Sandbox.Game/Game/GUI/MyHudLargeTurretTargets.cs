#region Using

using Sandbox.Game.Entities;
using System.Collections.Generic;
using VRage.Game.Entity;
using VRage.Game.Gui;


#endregion

namespace Sandbox.Game.Gui
{
    public class MyHudLargeTurretTargets
    {
        public bool Visible { get; set; }

        Dictionary<MyEntity, MyHudEntityParams> m_markers = new Dictionary<MyEntity, MyHudEntityParams>();

        public MyHudLargeTurretTargets()
        {
            Visible = true;
        }

        internal Dictionary<MyEntity, MyHudEntityParams> Targets
        {
            get { return m_markers; }
        }

        internal void RegisterMarker(MyEntity target, MyHudEntityParams hudParams)
        {
            m_markers[target] = hudParams;
        }

        internal void UnregisterMarker(MyEntity target)
        {
            m_markers.Remove(target);
        }
    }
}
