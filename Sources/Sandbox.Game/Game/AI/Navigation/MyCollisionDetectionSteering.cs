using Havok;
using Sandbox.Engine.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.AI.Navigation
{
    public class MyCollisionDetectionSteering : MySteeringBase
    {
        private bool m_hitLeft = false;
        private bool m_hitRight = false;
        private float m_hitLeftFraction = 0.0f;
        private float m_hitRightFraction = 0.0f;

        public MyCollisionDetectionSteering(MyBotNavigation parent)
            : base(parent, 1.0f)
        { }

        public override string GetName()
        {
            return "Collision detection steering";
        }

        public override void Update()
        {
            base.Update();
        }

        public override void AccumulateCorrection(ref Vector3 correction, ref float weight)
        {
            m_hitLeft = false;
            m_hitRight = false;

            var pos = Parent.PositionAndOrientation;
            var fwd = Parent.ForwardVector;
            var left = Vector3.Cross(pos.Up, fwd);

            List<MyPhysics.HitInfo> list = new List<MyPhysics.HitInfo>();

            MyPhysics.CastRay(pos.Translation + pos.Up, pos.Translation + pos.Up + fwd * 0.1f + left * 1.3f, list);
            if (list.Count > 0)
            {
                m_hitLeft = true;
                m_hitLeftFraction = list[0].HkHitInfo.HitFraction;
            }
            list.Clear();

            MyPhysics.CastRay(pos.Translation + pos.Up, pos.Translation + pos.Up + fwd * 0.1f - left * 1.3f, list);
            if (list.Count > 0)
            {
                m_hitRight = true;
                m_hitRightFraction = list[0].HkHitInfo.HitFraction;
            }
            list.Clear();

            float wtLeft = Weight * 0.01f * (1.0f - m_hitLeftFraction);
            float wtRight = Weight * 0.01f * (1.0f - m_hitRightFraction);

            if (m_hitLeft)
            {
                correction -= left * wtLeft;
                weight += wtLeft;
            }
            if (m_hitRight)
            {
                correction += left * wtRight;
                weight += wtRight;
            }
            if (m_hitLeft && m_hitRight)
            {
                correction -= left;
                weight += wtLeft;
            }
        }

        public override void DebugDraw()
        {
            base.DebugDraw();

            var pos = Parent.PositionAndOrientation;
            var fwd = Parent.ForwardVector;
            var left = Vector3.Cross(pos.Up, fwd);

            var color = m_hitLeft ? Color.Orange : Color.Green;
            MyRenderProxy.DebugDrawLine3D(pos.Translation + pos.Up, pos.Translation + pos.Up + fwd * 0.1f + left * 1.3f, color, color, true);
            MyRenderProxy.DebugDrawText3D(pos.Translation + pos.Up * 3.0f, "Hit LT: " + m_hitLeftFraction.ToString(), color, 0.7f, false);
            color = m_hitRight ? Color.Orange : Color.Green;
            MyRenderProxy.DebugDrawLine3D(pos.Translation + pos.Up, pos.Translation + pos.Up + fwd * 0.1f - left * 1.3f, color, color, true);
            MyRenderProxy.DebugDrawText3D(pos.Translation + pos.Up * 3.2f, "Hit RT: " + m_hitRightFraction.ToString(), color, 0.7f, false);
        }
    }
}
