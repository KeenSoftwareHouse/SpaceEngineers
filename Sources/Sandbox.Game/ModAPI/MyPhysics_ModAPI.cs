using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;
using VRageMath;

namespace Sandbox.ModAPI.Physics
{
    class MyPhysics : IMyPhysics
    {
        public static readonly MyPhysics Static;

        static MyPhysics()
        {
            Static = new MyPhysics();
        }

        int IMyPhysics.StepsLastSecond
        {
            get { return Engine.Physics.MyPhysics.StepsLastSecond; }
        }

        float IMyPhysics.SimulationRatio
        {
            get { return Engine.Physics.MyPhysics.SimulationRatio; }
        }

        bool IMyPhysics.CastLongRay(Vector3D from, Vector3D to, out IHitInfo hitInfo, bool any)
        {
            var hitinfo = Engine.Physics.MyPhysics.CastLongRay(from, to, any);
            if( hitinfo.HasValue )
            {
                hitInfo = hitinfo;
                return true;
            }
            hitInfo = null;
            return false;
        }

        bool IMyPhysics.CastRay(Vector3D from, Vector3D to, out IHitInfo hitInfo, int raycastFilterLayer)
        {
            var hit = Engine.Physics.MyPhysics.CastRay(from, to, raycastFilterLayer);
            if( hit.HasValue )
            {
                hitInfo = hit;
                return true;
            }
            hitInfo = null;
            return false;
        }

        List<Engine.Physics.MyPhysics.HitInfo> m_tempHitList = new List<Engine.Physics.MyPhysics.HitInfo>();

        void IMyPhysics.CastRay(Vector3D from, Vector3D to, List<IHitInfo> toList, int raycastFilterLayer)
        {
            m_tempHitList.Clear();
            toList.Clear();
            Engine.Physics.MyPhysics.CastRay(from, to, m_tempHitList, raycastFilterLayer);
            
            foreach (var hit in m_tempHitList)
                toList.Add(hit);
        }

        bool IMyPhysics.CastRay(Vector3D from, Vector3D to, out IHitInfo hitInfo, uint raycastCollisionFilter, bool ignoreConvexShape)
        {
            Sandbox.Engine.Physics.MyPhysics.HitInfo hitinfo;
            var hit = Engine.Physics.MyPhysics.CastRay(from, to, out hitinfo, raycastCollisionFilter, ignoreConvexShape);
            hitInfo = hitinfo;
            return hit;
        }

        void IMyPhysics.EnsurePhysicsSpace(BoundingBoxD aabb)
        {
            Engine.Physics.MyPhysics.EnsurePhysicsSpace(aabb);
        }

        int IMyPhysics.GetCollisionLayer(string strLayer)
        {
            return Engine.Physics.MyPhysics.GetCollisionLayer(strLayer);
        }
    }
}
