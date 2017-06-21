using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRageRender;
using Sandbox.Game.World;
using Sandbox.Game.Entities;

using Sandbox.Common.ObjectBuilders;
using VRage.Utils;
using Sandbox.Engine.Utils;
using VRage;

namespace Sandbox.Game.Components
{
    class MyDebugRenderComponentThrust : MyDebugRenderComponent
    {
        MyThrust m_thrust = null;
        public MyDebugRenderComponentThrust(MyThrust thrust)
            : base(thrust)
        {
            m_thrust = thrust;
        }

        public override void DebugDraw()
        {
            //if (MyFakes.DEBUG_DRAW_THRUSTER_DAMAGE)
            DebugDrawDamageArea();
        }
        private void DebugDrawDamageArea()
        {
            if (m_thrust.CurrentStrength != 0 || MyFakes.INACTIVE_THRUSTER_DMG)
                foreach (var flameInfo in m_thrust.Flames)
                {
                    var l = m_thrust.GetDamageCapsuleLine(flameInfo);
                    MyRenderProxy.DebugDrawCapsule(l.From, l.To, flameInfo.Radius * m_thrust.FlameDamageLengthScale, Color.Red, false);
                    //MyRenderProxy.DebugDrawSphere(l.From, flameInfo.Radius * m_thrustDefinition.FlameDamageLengthScale, Color.Green.ToVector3(), 1, false);
                }
        }
    }
}
