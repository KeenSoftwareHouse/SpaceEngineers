using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Algorithms;
using VRage.Game.Entity;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.AI.Pathfinding
{
    public class MySmartGoal : IMyHighLevelPrimitiveObserver
    {
        private MyNavigationPrimitive m_end = null;
        private MyHighLevelPrimitive m_hlEnd = null;
        private MyEntity m_endEntity;

        private bool m_hlEndIsApproximate;

        private IMyDestinationShape m_destination;
        private Vector3D m_destinationCenter;

        private Func<MyNavigationPrimitive, float> m_pathfindingHeuristic = null;
        private Func<MyNavigationPrimitive, float> m_terminationCriterion = null;

        private static Func<MyNavigationPrimitive, float> m_hlPathfindingHeuristic = HlHeuristic;
        private static Func<MyNavigationPrimitive, float> m_hlTerminationCriterion = HlCriterion;
        private static MySmartGoal m_pathfindingStatic = null;

        private HashSet<MyHighLevelPrimitive> m_ignoredPrimitives = null;

        public IMyDestinationShape Destination
        {
            get
            {
                return m_destination;
            }
        }

        public MyEntity EndEntity
        {
            get
            {
                return m_endEntity;
            }
        }

        public Func<MyNavigationPrimitive, float> PathfindingHeuristic
        {
            get
            {
                return m_pathfindingHeuristic;
            }
        }

        public Func<MyNavigationPrimitive, float> TerminationCriterion
        {
            get
            {
                return m_terminationCriterion;
            }
        }

        public bool IsValid { get; private set; }

        public MySmartGoal(IMyDestinationShape goal, MyEntity entity = null)
        {
            m_destination = goal;
            m_destinationCenter = goal.GetDestination();
            m_endEntity = entity;
            if (m_endEntity != null)
            {
                m_destination.SetRelativeTransform(m_endEntity.PositionComp.WorldMatrixNormalizedInv);
                m_endEntity.OnClosing += m_endEntity_OnClosing;
            }

            m_pathfindingHeuristic = this.Heuristic;
            m_terminationCriterion = this.Criterion;

            m_ignoredPrimitives = new HashSet<MyHighLevelPrimitive>();
            IsValid = true;
        }

        public void Invalidate()
        {
            Debug.Assert(IsValid);
            if (m_endEntity != null)
            {
                m_endEntity.OnClosing -= m_endEntity_OnClosing;
                m_endEntity = null;
            }
            foreach (var ignored in m_ignoredPrimitives)
            {
                ignored.Parent.StopObservingPrimitive(ignored, this);
            }
            m_ignoredPrimitives.Clear();
            IsValid = false;
        }

        public bool ShouldReinitPath()
        {
            return TargetMoved();
        }

        public void Reinit()
        {
            Debug.Assert(IsValid);
            if (m_endEntity != null)
            {
                m_destination.UpdateWorldTransform(m_endEntity.WorldMatrix);
                m_destinationCenter = m_destination.GetDestination();
            }
        }

        public MyPath<MyNavigationPrimitive> FindHighLevelPath(MyPathfinding pathfinding, MyHighLevelPrimitive startPrimitive)
        {
            m_pathfindingStatic = this;
            var path = pathfinding.FindPath(startPrimitive, m_hlPathfindingHeuristic, m_hlTerminationCriterion, null, returnClosest: false);
            pathfinding.LastHighLevelTimestamp = pathfinding.GetCurrentTimestamp();
            m_pathfindingStatic = null;

            return path;
        }

        public MyPath<MyNavigationPrimitive> FindPath(MyPathfinding pathfinding, MyNavigationPrimitive startPrimitive)
        {
            throw new NotImplementedException();
        }

        public void IgnoreHighLevel(MyHighLevelPrimitive primitive)
        {
            if (!m_ignoredPrimitives.Contains(primitive))
            {
                primitive.Parent.ObservePrimitive(primitive, this);
                bool added = m_ignoredPrimitives.Add(primitive);
                Debug.Assert(added);
            }
        }

        private bool TargetMoved()
        {
            return Vector3D.DistanceSquared(m_destinationCenter, m_destination.GetDestination()) > 4.0f;
        }

        private void m_endEntity_OnClosing(MyEntity obj)
        {
            m_endEntity = null;
            IsValid = false;
        }

        private float Heuristic(MyNavigationPrimitive primitive)
        {
            return (float)Vector3D.Distance(primitive.WorldPosition, m_destinationCenter);
        }

        private float Criterion(MyNavigationPrimitive primitive)
        {
            return m_destination.PointAdmissibility(primitive.WorldPosition, 2.0f); // Triangles of large cube blocks will fit into 2m diam. circumsphere
        }

        private static float HlHeuristic(MyNavigationPrimitive primitive)
        {
            return (float)Vector3D.RectangularDistance(primitive.WorldPosition, m_pathfindingStatic.m_destinationCenter) * 2.0f;
        }

        private static float HlCriterion(MyNavigationPrimitive primitive)
        {
            var hlPrimitive = primitive as MyHighLevelPrimitive;
            Debug.Assert(hlPrimitive != null, "Primitive in smart path termination criterion was not a high-level navigation primitive!");
            if (hlPrimitive == null || m_pathfindingStatic.m_ignoredPrimitives.Contains(hlPrimitive))
            {
                return float.PositiveInfinity;
            }

            float dist = m_pathfindingStatic.m_destination.PointAdmissibility(primitive.WorldPosition, 8.7f); // ~= sqrt(3) * voxelCellSize/2
            if (dist < float.PositiveInfinity)
            {
                return dist * 4.0f;
            }

            var component = hlPrimitive.GetComponent();
            Debug.Assert(component != null, "Component was null in termination criterion of smart path!");
            if (component == null)
            {
                return float.PositiveInfinity;
            }

            if (!component.FullyExplored)
            {
                return (float)Vector3D.RectangularDistance(primitive.WorldPosition, m_pathfindingStatic.m_destinationCenter) * 8.0f;
            }

            return float.PositiveInfinity;
        }

        public void DebugDraw()
        {
            m_destination.DebugDraw();
            foreach (var ignored in m_ignoredPrimitives)
            {
                MyRenderProxy.DebugDrawSphere(ignored.WorldPosition, 0.5f, Color.Red, 1.0f, false);
            }
        }
    }
}
