using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRageRender;
using Sandbox.Game.World;
using Sandbox.Graphics.TransparentGeometry;
using Sandbox.Game.Entities;
using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders;
using VRage.Utils;
using Sandbox.Engine.Utils;
using VRage;
using VRage;
using VRage.Components;

namespace Sandbox.Game.Components
{
    class MyRenderComponentThrust : MyRenderComponentCubeBlock
    {
        MyThrust m_thrust = null;

        #region overrides
        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            m_thrust = Container.Entity as MyThrust;
        }
        public override void Draw()
        {
            base.Draw();
            ProfilerShort.Begin("MyThrust.Draw()");

            var worldToLocal = MatrixD.Invert(Container.Entity.PositionComp.WorldMatrix);

            if (m_thrust.CanDraw())
            {
                m_thrust.UpdateThrustFlame();
                m_thrust.UpdateThrustColor();

                foreach (var f in m_thrust.Flames)
                {
                    if(m_thrust.CubeGrid.Physics == null)
                    {
                        continue;
                    }
                    Vector3D forward = Vector3D.TransformNormal(f.Direction, Container.Entity.PositionComp.WorldMatrix);
                    var position = Vector3D.Transform(f.Position, Container.Entity.PositionComp.WorldMatrix);

                    float radius = m_thrust.ThrustRadiusRand * f.Radius;
                    float length = m_thrust.ThrustLengthRand * f.Radius;
                    float thickness = m_thrust.ThrustThicknessRand * f.Radius;

                    Vector3D velocityAtNewCOM = Vector3D.Cross(m_thrust.CubeGrid.Physics.AngularVelocity, position - m_thrust.CubeGrid.Physics.CenterOfMassWorld);
                    var velocity = m_thrust.CubeGrid.Physics.LinearVelocity + velocityAtNewCOM;

                    if (m_thrust.CurrentStrength > 0 && length > 0)
                    {
                        float angle = 1 - Math.Abs(Vector3.Dot(MyUtils.Normalize(MySector.MainCamera.Position - position), forward));
                        float alphaCone = (1 - (float)Math.Pow(1 - angle, 30)) * 0.5f;
                        //  We move polyline particle backward, because we are stretching ball texture and it doesn't look good if stretched. This will hide it.
                        MyTransparentGeometry.AddLineBillboard(m_thrust.FlameLengthMaterial, m_thrust.ThrustColor * alphaCone, position - forward * length * 0.25f,
                            GetRenderObjectID(), ref worldToLocal, forward, length, thickness);

                    }

                    if (radius > 0)
                        MyTransparentGeometry.AddPointBillboard(m_thrust.FlamePointMaterial, m_thrust.ThrustColor, position, GetRenderObjectID(), ref worldToLocal, radius, 0);
                }
            }

            if (m_thrust.Light != null)
            {
                m_thrust.UpdateLight();
            }

            ProfilerShort.End();
        }            
        #endregion
    }
}
