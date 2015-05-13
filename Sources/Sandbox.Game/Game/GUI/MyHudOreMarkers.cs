#region Using

using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using System.Collections;
using System.Collections.Generic;
using VRage;

#endregion

namespace Sandbox.Game.Gui
{
    public class MyHudOreMarkers : IEnumerable<MyEntityOreDeposit>
    {
        public bool Visible { get; set; }

        private readonly HashSet<MyEntityOreDeposit> m_markers = new HashSet<MyEntityOreDeposit>(MyEntityOreDeposit.Comparer);

        public MyHudOreMarkers()
        {
            Visible = true;
        }

        internal void RegisterMarker(MyEntityOreDeposit deposit)
        {
            m_markers.Add(deposit);
        }

        internal void UnregisterMarker(MyEntityOreDeposit deposit)
        {
            m_markers.Remove(deposit);
        }

        internal void Clear()
        {
            m_markers.Clear();
        }

        public HashSet<MyEntityOreDeposit>.Enumerator GetEnumerator()
        {
            return m_markers.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<MyEntityOreDeposit> IEnumerable<MyEntityOreDeposit>.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
