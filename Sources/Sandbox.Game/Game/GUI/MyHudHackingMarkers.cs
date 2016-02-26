#region Using

using Sandbox.Common;
using Sandbox.Game.Entities;
using System.Collections.Generic;
using VRage.Game.Entity;
using VRage.Game.Gui;


#endregion

namespace Sandbox.Game.Gui
{
    public class MyHudHackingMarkers
    {
        public bool Visible { get; set; }

        Dictionary<MyEntity, MyHudEntityParams> m_markerEntities = new Dictionary<MyEntity, MyHudEntityParams>();
        Dictionary<MyEntity, float> m_blinkingTimes = new Dictionary<MyEntity, float>();
        List<MyEntity> m_removeList = new List<MyEntity>();

        public MyHudHackingMarkers()
        {
            Visible = true;
        }

        internal void UpdateMarkers()
        {
            m_removeList.Clear();
            foreach (var marker in m_markerEntities)
            {
                if (m_blinkingTimes[marker.Key] <= VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS) 
                    m_removeList.Add(marker.Key);
                else
                    m_blinkingTimes[marker.Key] -= VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            }

            foreach (var marker in m_removeList)
            {
                UnregisterMarker(marker);
            }

            m_removeList.Clear();
        }

        internal Dictionary<MyEntity, MyHudEntityParams> MarkerEntities
        {
            get { return m_markerEntities; }
        }

        internal void RegisterMarker(MyEntity entity, MyHudEntityParams hudParams)
        {
            m_markerEntities[entity] = hudParams;
            m_blinkingTimes[entity] = hudParams.BlinkingTime;
        }

        internal void UnregisterMarker(MyEntity entity)
        {
            m_markerEntities.Remove(entity);
            m_blinkingTimes.Remove(entity);
        }

        public void Clear()
        {
            m_markerEntities.Clear();
            m_blinkingTimes.Clear();
        }
    }
}
