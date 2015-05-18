using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using VRageMath;

namespace Sandbox.Game.Gui
{
    public class MyHudRadarMarkers
    {
        public bool Visible { get; set; }

        List<MyEntity> m_Inss = new List<MyEntity>();

        public MyHudRadarMarkers()
        {
            Visible = true;
        }

        internal List<MyEntity> MarkerEntities
        {
            get { return m_Inss; }
        }

        internal void RegisterMarker(MyEntity ins)
        {
            if (!m_Inss.Contains(ins))
                m_Inss.Add(ins);
        }

        internal void UnregisterMarker(MyEntity ins)
        {
            m_Inss.Remove(ins);
        }

        public void Clear()
        {
            m_Inss.Clear();
        }

        public class DistanceFromCameraComparer : IComparer<MyEntity>
        {
            public int Compare(MyEntity first, MyEntity second)
            {
                return Vector3D.DistanceSquared(MySector.MainCamera.Position, second.LocationForHudMarker).CompareTo(Vector3D.DistanceSquared(MySector.MainCamera.Position, first.LocationForHudMarker));
            }
        }
        DistanceFromCameraComparer m_distFromCamComparer=new DistanceFromCameraComparer();

        internal void Sort(DistanceFromCameraComparer distComparer)
        {
            m_Inss.Sort(distComparer);
        }
        internal void Sort()
        {
            Sort(m_distFromCamComparer);
        }
    }
}
