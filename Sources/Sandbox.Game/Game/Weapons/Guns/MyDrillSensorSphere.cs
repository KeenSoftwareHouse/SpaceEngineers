using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.Entities;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Weapons.Guns
{
    class MyDrillSensorSphere : MyDrillSensorBase
    {
        float m_radius;
        float m_centerForwardOffset;

        public MyDrillSensorSphere(float radius, float centerForwardOffset)
        {
            m_radius = radius;
            m_centerForwardOffset = centerForwardOffset;
            Center = centerForwardOffset * Vector3.Forward;
            FrontPoint = Center + Vector3.Forward * m_radius;
        }

        public override void OnWorldPositionChanged(ref MatrixD worldMatrix)
        {
            Center = worldMatrix.Translation + worldMatrix.Forward * m_centerForwardOffset;
            FrontPoint = Center + worldMatrix.Forward * m_radius;
        }

        protected override void ReadEntitiesInRange()
        {
            m_entitiesInRange.Clear();

            BoundingSphereD bsphere = new BoundingSphereD(Center, m_radius);
            var res = MyEntities.GetEntitiesInSphere(ref bsphere);
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
            MyRenderProxy.DebugDrawSphere(Center, m_radius, Color.Yellow, 1.0f, true);
        }
    }
}
