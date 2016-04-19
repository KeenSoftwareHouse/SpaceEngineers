using Sandbox.Game.Entities;
using System.Collections.Generic;
using Sandbox.Game.Screens.Helpers;
using VRageMath;
using Sandbox.Game.World;


namespace Sandbox.Game.Gui
{
    public class MyHudGpsMarkers
    {
        public bool Visible { get; set; }

        List<MyGps> m_Inss = new List<MyGps>();

        public MyHudGpsMarkers()
        {
            Visible = true;
        }

        internal List<MyGps> MarkerEntities
        {
            get { return m_Inss; }
        }

        public void RegisterMarker(MyGps ins)
        {
            if (!m_Inss.Contains(ins))
                m_Inss.Add(ins);
        }

        public void UnregisterMarker(MyGps ins)
        {
            m_Inss.Remove(ins);
        }

        public void Clear()
        {
            m_Inss.Clear();
        }

        public class DistanceFromCameraComparer : IComparer<MyGps>
        {
            public int Compare(MyGps first, MyGps second)
            {
                return Vector3D.DistanceSquared(MySector.MainCamera.Position, second.Coords).CompareTo(Vector3D.DistanceSquared(MySector.MainCamera.Position, first.Coords));
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
