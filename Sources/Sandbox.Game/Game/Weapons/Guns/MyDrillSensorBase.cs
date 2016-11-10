using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Havok;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities;
using VRageMath;
using VRageRender;
using VRage.Game.Entity;


namespace Sandbox.Game.Weapons.Guns
{
    public abstract class MyDrillSensorBase
    {
        public struct DetectionInfo
        {
            public readonly MyEntity Entity;
            public readonly Vector3D DetectionPoint;
            //GK: Added this to keep track of Enviroment Sector Items (e.g. Trees)
            public readonly int ItemId;

            public DetectionInfo(MyEntity entity, Vector3D detectionPoint)
            {
                Entity = entity;
                DetectionPoint = detectionPoint;
                ItemId = 0;
            }

            public DetectionInfo(MyEntity entity, Vector3D detectionPoint, int itemid)
            {
                Entity = entity;
                DetectionPoint = detectionPoint;
                ItemId = itemid;
            }
        }

        public HashSet<MyEntity> IgnoredEntities;
        protected Dictionary<long, DetectionInfo> m_entitiesInRange;

        public Dictionary<long, DetectionInfo> EntitiesInRange
        {
            get { ReadEntitiesInRange(); return m_entitiesInRange; }
        }

        // World space center
        private Vector3D m_center;
        public Vector3D Center
        {
            get { return m_center; }
            protected set { m_center = value; }
        }

        // World space front point
        private Vector3D m_frontPoint;
        public Vector3D FrontPoint
        {
            get { return m_frontPoint; }
            protected set { m_frontPoint = value; }
        }

        public MyDrillSensorBase()
        {
            IgnoredEntities = new HashSet<MyEntity>();
            m_entitiesInRange = new Dictionary<long, DetectionInfo>();
        }

        protected abstract void ReadEntitiesInRange();

        public abstract void OnWorldPositionChanged(ref MatrixD worldMatrix);

        public abstract void DebugDraw();
    }
}
