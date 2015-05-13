using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.Weapons;
using Sandbox.Common.Components;
using VRageMath;

namespace Sandbox.Game.Components
{
    class MyDebugRenderComponentLargeTurret : MyDebugRenderComponent
    {
        MyLargeTurretBase m_turretBase = null;

        public MyDebugRenderComponentLargeTurret(MyLargeTurretBase turretBase)
            : base(turretBase)
        {
            m_turretBase = turretBase;
        }

        public override bool DebugDraw()
        {
            float radius = 0.0f;
            if (m_turretBase.Render.GetModel() != null)
                radius = m_turretBase.Render.GetModel().BoundingSphere.Radius;

            Vector3 statusColor = new Vector3();
            switch (m_turretBase.GetStatus())
            {
                case MyLargeTurretBase.MyLargeShipGunStatus.MyWeaponStatus_Deactivated:
                    {
                        statusColor = Color.Green.ToVector3();
                    }
                    break;
                case MyLargeTurretBase.MyLargeShipGunStatus.MyWeaponStatus_Searching:
                    {
                        statusColor = Color.Red.ToVector3();
                    }
                    break;
                case MyLargeTurretBase.MyLargeShipGunStatus.MyWeaponStatus_Shooting:
                    {
                        statusColor = Color.White.ToVector3();
                    }
                    break;
            }
            Color from = new Color(statusColor);
            Color to = new Color(statusColor);
            if (m_turretBase.Target != null)
            {
                VRageRender.MyRenderProxy.DebugDrawLine3D(m_turretBase.Barrel.Entity.PositionComp.GetPosition(), m_turretBase.Target.PositionComp.GetPosition(), from, to, false);
                VRageRender.MyRenderProxy.DebugDrawSphere(m_turretBase.Target.PositionComp.GetPosition(), m_turretBase.Target.PositionComp.LocalVolume.Radius, Color.White, 1, false);
            }

            //VRageRender.MyRenderProxy.DebugDrawSphere(GetPosition(), ShootingRange, Vector3.One, 1, false);

            m_turretBase.PowerReceiver.DebugDraw(m_turretBase.PositionComp.WorldMatrix);

            return base.DebugDraw();
        }
    }
}

