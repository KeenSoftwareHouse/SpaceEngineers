using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Havok;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Weapons.Guns
{
    class MyDrillSensorBox : MyDrillSensorBase
    {
        Vector3 m_halfExtents;
        float m_centerOffset;
        Quaternion m_orientation;

        public MyDrillSensorBox(Vector3 halfExtents, float centerOffset)
        {
            m_halfExtents = halfExtents;
            m_centerOffset = centerOffset;
            Center = Vector3.Forward * centerOffset;
            FrontPoint = Center + Vector3.Forward * m_halfExtents.Z;
        }

        public override void OnWorldPositionChanged(ref MatrixD worldMatrix)
        {
            m_orientation = Quaternion.CreateFromRotationMatrix(worldMatrix.GetOrientation());
            Center = worldMatrix.Translation + worldMatrix.Forward * m_centerOffset;
            FrontPoint = Center + worldMatrix.Forward * m_halfExtents.Z;
        }

        protected override void ReadEntitiesInRange()
        {
            m_entitiesInRange.Clear();

            MyOrientedBoundingBox bbox = new MyOrientedBoundingBox(Center, m_halfExtents, m_orientation);
            var aabb = bbox.GetAABB();
            var res = MyEntities.GetEntitiesInAABB(ref aabb);
            for (int i = 0; i < res.Count; ++i)
            {
                var rootEntity = res[i].GetTopMostParent();
                if (!IgnoredEntities.Contains(rootEntity))
                    m_entitiesInRange[rootEntity.EntityId] = new DetectionInfo(rootEntity, FrontPoint);
            }
            res.Clear();
        }

        public override void DebugDraw()
        {
            MyOrientedBoundingBoxD bbox = new MyOrientedBoundingBoxD(Center, m_halfExtents, m_orientation);
            var red = new Vector3(1, 0, 0);
            MyRenderProxy.DebugDrawOBB(bbox, red, 0.6f, true, false);
        }
    }
}
