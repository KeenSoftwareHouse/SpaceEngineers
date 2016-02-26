#region Using

using Sandbox.Game.Entities;
using System.Collections.Generic;
using VRage.Game.Entity;
using VRage.Game.Gui;


#endregion

namespace Sandbox.Game.Gui
{
    public class MyHudLocationMarkers
    {
        public bool Visible { get; set; }

        Dictionary<MyEntity, MyHudEntityParams> m_markerEntities = new Dictionary<MyEntity, MyHudEntityParams>();

        public MyHudLocationMarkers()
        {
            Visible = true;
        }

        public Dictionary<MyEntity, MyHudEntityParams> MarkerEntities
        {
            get { return m_markerEntities; }
        }

        public void RegisterMarker(MyEntity entity, MyHudEntityParams hudParams)
        {
            if (hudParams.Entity == null)
                hudParams.Entity = entity;
            m_markerEntities[entity] = hudParams;
        }

        public void UnregisterMarker(MyEntity entity)
        {
            m_markerEntities.Remove(entity);
        }

        public void Clear()
        {
            m_markerEntities.Clear();
        }
    }

}
