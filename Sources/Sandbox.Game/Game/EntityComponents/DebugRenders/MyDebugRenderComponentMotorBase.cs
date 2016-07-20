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
    class MyDebugRenderComponentMotorBase:MyDebugRenderComponent
    {  
        MyMotorBase m_motor = null;
        public MyDebugRenderComponentMotorBase(MyMotorBase motor):base(motor)
        {
            m_motor = motor;
        }

        public override void DebugDraw()
        {
            if (MyDebugDrawSettings.DEBUG_DRAW_ROTORS)
            {
                Vector3 halfExtents;
                Vector3D pos;
                Quaternion orientation;
                m_motor.ComputeTopQueryBox(out pos, out halfExtents, out orientation);
                VRageRender.MyRenderProxy.DebugDrawOBB(new MyOrientedBoundingBoxD(pos, halfExtents, orientation), Color.Green.ToVector3(), 1, false, false);
                if (m_motor.Rotor != null)
                {
                    var center = Vector3D.Transform(m_motor.DummyPosition, m_motor.CubeGrid.WorldMatrix) + (Vector3D.Transform((m_motor.Rotor as MyMotorRotor).DummyPosLoc, m_motor.RotorGrid.WorldMatrix) - m_motor.RotorGrid.WorldMatrix.Translation);
                    VRageRender.MyRenderProxy.DebugDrawSphere(center, 0.1f, Color.Green, 1, false);
                    var sphere = m_motor.Rotor.Model.BoundingSphere;
                    sphere.Center = Vector3D.Transform(sphere.Center, m_motor.Rotor.WorldMatrix);
                    VRageRender.MyRenderProxy.DebugDrawSphere(sphere.Center, sphere.Radius, Color.Red, 1, false);
                }
            }
        }
    }
}
