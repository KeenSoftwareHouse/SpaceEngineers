using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.Entity;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.AI.Navigation
{
    public class MyTargetSteering : MySteeringBase
    {
        protected Vector3D? m_target;
        protected MyEntity m_entity;

        private const float m_slowdownRadius = 4.0f;
        private const float m_maxSpeed = 1.0f;

        // Shape of the capsule that tells whether the target was reached
        private float m_capsuleRadiusSq = 1.0f;
        private float m_capsuleHeight = 0.5f;
        private float m_capsuleOffset = -0.8f;

        public bool TargetSet { get { return m_target != null; } }

        public bool Flying { get; private set; }
        public Vector3D? TargetWorld
        {
            get
            {
                if (m_entity == null || m_entity.MarkedForClose)
                    return m_target;
                else if (m_target.HasValue)
                    return Vector3D.Transform(m_target.Value, m_entity.WorldMatrix);
                else
                    return null;
            }
        }

        public MyTargetSteering(MyBotNavigation navigation)
            : base(navigation, 1.0f)
        {
            m_target = null;
        }

        public override string GetName()
        {
            return "Target steering";
        }

        public void SetTarget(Vector3D target, float radius = 1.0f, MyEntity relativeEntity = null, float weight = 1.0f, bool fly = false)
        {
            if (relativeEntity == null || relativeEntity.MarkedForClose)
            {
                m_entity = null;
                m_target = target;
            }
            else
            {
                m_entity = relativeEntity;
                m_target = Vector3D.Transform(target, m_entity.PositionComp.WorldMatrixNormalizedInv);
            }

            m_capsuleRadiusSq = radius * radius;

            Weight = weight;
            Flying = fly;
        }

        public void UnsetTarget()
        {
            m_target = null;
        }

        public bool TargetReached()
        {
            if (!TargetWorld.HasValue)
            {
                return false;
            }

            Vector3D target = TargetWorld.Value;
            return TargetReached(ref target, m_capsuleRadiusSq);
        }

        protected Vector3D CapsuleCenter()
        {
            Vector3D capsuleAxis = Parent.PositionAndOrientation.Up;
            return Parent.PositionAndOrientation.Translation + capsuleAxis * (m_capsuleOffset + m_capsuleHeight) * 0.5f;
        }

        public double TargetDistanceSq(ref Vector3D target)
        {
            double d, t, distSq;

            Vector3D capsuleAxis = Parent.PositionAndOrientation.Up;
            Vector3D capsulePoint = Parent.PositionAndOrientation.Translation + capsuleAxis * m_capsuleOffset;
            Vector3D.Dot(ref capsulePoint, ref capsuleAxis, out d);
            Vector3D.Dot(ref target, ref capsuleAxis, out t);
            t -= d;

            if (t >= m_capsuleHeight) // Beyond the capsule's apex
            {
                capsulePoint += capsuleAxis;
            }
            else if (t >= 0) // Between apex and origin
            {
                capsulePoint += capsuleAxis * t;
            }

            Vector3D.DistanceSquared(ref target, ref capsulePoint, out distSq);
            return distSq;
        }

        public bool TargetReached(ref Vector3D target, float radiusSq)
        {
            return TargetDistanceSq(ref target) < radiusSq;
        }

        public override void AccumulateCorrection(ref Vector3 correctionHint, ref float weight)
        {
            if (m_entity != null && m_entity.MarkedForClose)
                m_entity = null;

            Vector3 currentMovement;
            Vector3 wantedMovement;
            GetMovements(out currentMovement, out wantedMovement);

            correctionHint += (wantedMovement - currentMovement) * Weight;
            weight += Weight;
        }

        public override void Update()
        {
            base.Update();

            if (TargetReached())
            {
                UnsetTarget();
            }
        }

        private void GetMovements(out Vector3 currentMovement, out Vector3 wantedMovement)
        {
            Vector3? target = TargetWorld;
            currentMovement = Parent.ForwardVector * Parent.Speed;

            if (target.HasValue)
            {
                wantedMovement = target.Value - Parent.PositionAndOrientation.Translation;
            }
            else
            {
                wantedMovement = Vector3.Zero;
                return;
            }
            float distance = wantedMovement.Length();

            if (distance > m_slowdownRadius)
                wantedMovement = wantedMovement * m_maxSpeed / distance;
            else
                wantedMovement = wantedMovement * m_maxSpeed / m_slowdownRadius;
        }

        public override void DebugDraw()
        {
            base.DebugDraw();

            var pos1 = Parent.PositionAndOrientation.Translation + Parent.PositionAndOrientation.Up * m_capsuleOffset;
            var pos2 = pos1 + Parent.PositionAndOrientation.Up * m_capsuleHeight;
            var center = (pos1 + pos2) * 0.5f;

            Vector3 currentMovement;
            Vector3 wantedMovement;
            GetMovements(out currentMovement, out wantedMovement);

            Vector3D? target = TargetWorld;
            if (target.HasValue)
            {
                MyRenderProxy.DebugDrawLine3D(center, target.Value, Color.White, Color.White, true);
                MyRenderProxy.DebugDrawSphere(target.Value, 0.05f, Color.White.ToVector3(), 1.0f, false);
                MyRenderProxy.DebugDrawCapsule(pos1, pos2, (float)Math.Sqrt(m_capsuleRadiusSq), Color.Yellow, false);
            }

            MyRenderProxy.DebugDrawLine3D(pos2, pos2 + wantedMovement, Color.Red, Color.Red, false);
            MyRenderProxy.DebugDrawLine3D(pos2, pos2 + currentMovement, Color.Green, Color.Green, false);
        }
    }
}
