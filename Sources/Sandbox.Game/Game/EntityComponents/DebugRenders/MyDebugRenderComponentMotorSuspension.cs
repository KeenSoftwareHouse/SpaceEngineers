using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.Entities.Cube;
using VRageMath;
using VRageRender;
using Sandbox.Engine.Utils;
using Sandbox.Game.World;

namespace Sandbox.Game.Components
{
    class MyDebugRenderComponentMotorSuspension: MyDebugRenderComponent
    {
        MyMotorSuspension m_motor = null;
        public MyDebugRenderComponentMotorSuspension(MyMotorSuspension motor):base(motor) 
        {
            m_motor = motor;
        }

        public override void DebugDraw()
        {
            if (MySession.Static.ControlledEntity != null)
            {
                var m = MySession.Static.ControlledEntity.Entity.WorldMatrix;
                VRageRender.MyRenderProxy.DebugDrawLine3D(m.Translation, m.Translation + m.Forward * 2, Color.Red, Color.Yellow, false);

            }
            Matrix WorldMatrix = m_motor.PositionComp.WorldMatrix;

            VRageRender.MyRenderProxy.DebugDrawLine3D(WorldMatrix.Translation, WorldMatrix.Translation + WorldMatrix.Right * 2, Color.Red, Color.Yellow, false);
            VRageRender.MyRenderProxy.DebugDrawLine3D(WorldMatrix.Translation, WorldMatrix.Translation + WorldMatrix.Up * 2, Color.Blue, Color.Yellow, false);
            VRageRender.MyRenderProxy.DebugDrawLine3D(WorldMatrix.Translation, WorldMatrix.Translation + WorldMatrix.Forward * 2, Color.Green, Color.Yellow, false);
        } 
    }
}
