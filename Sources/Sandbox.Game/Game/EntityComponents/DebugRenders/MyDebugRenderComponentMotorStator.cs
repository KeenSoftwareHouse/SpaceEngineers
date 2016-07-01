using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.Entities.Cube;
using VRageMath;
using VRageRender;
using Sandbox.Engine.Utils;

namespace Sandbox.Game.Components
{
    class MyDebugRenderComponentMotorStator : MyDebugRenderComponent
    {
        MyMotorStator m_motor = null;
        public MyDebugRenderComponentMotorStator(MyMotorStator motor):base(motor)
        {
            m_motor = motor;
        }

        public override void DebugDraw()
        {
            if (m_motor.CanDebugDraw() == false)
            {
                return;
            }

            if (MyDebugDrawSettings.DEBUG_DRAW_ROTORS)
            {
                var statorWorld = m_motor.PositionComp.WorldMatrix;
                var rotorWorld = m_motor.Rotor.WorldMatrix;
                var pivot = Vector3.Lerp(statorWorld.Translation, rotorWorld.Translation, 0.5f);
                var axis = Vector3.Normalize(statorWorld.Up);

                VRageRender.MyRenderProxy.DebugDrawLine3D(pivot, pivot + axis, Color.Yellow, Color.Yellow, false);
                VRageRender.MyRenderProxy.DebugDrawLine3D(statorWorld.Translation, rotorWorld.Translation, Color.Red, Color.Green, false);
            }
        }
    }
}
