using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Algorithms;
using VRage.Collections;
using VRage.Game.Entity;
using VRage.Profiler;using VRage.ModAPI;using VRageMath;
using VRageRender;

namespace Sandbox.Game.AI.Pathfinding
{
    public class MySmartPath : IMyHighLevelPrimitiveObserver, IMyPath
    {
        private MyPathfinding m_pathfinding;

        private int m_lastInitTime = 0;
        private bool m_usedWholePath = false;
        private bool m_valid = false;

        private List<MyHighLevelPrimitive> m_pathNodes;
        private List<Vector4D> m_expandedPath;

        private int m_pathNodePosition;
        private int m_expandedPathPosition;

        private MyNavigationPrimitive m_currentPrimitive = null;
        private MyHighLevelPrimitive m_hlBegin = null;

        private Vector3D m_startPoint;
        private MySmartGoal m_goal;

        private static MySmartPath m_pathfindingStatic;

        private const float TRANSITION_RADIUS = 1.0f;

        public IMyDestinationShape Destination { get { return m_goal.Destination; } }
        public IMyEntity EndEntity { get { return m_goal.EndEntity; } }

        public bool IsValid
        {
            get
            {
                if (!m_goal.IsValid)
                {
                    if (m_valid)
                    {
                        Invalidate();
                    }
                    return false;
                }
                else
                {
                    if (m_valid)
                    {
                        return true;
                    }
                    else
                    {
                        m_goal.Invalidate();
                        return false;
                    }
                }
            }
        }

        public bool PathCompleted
        {
            get
            {
                return m_usedWholePath;
            }
        }

        public MySmartPath(MyPathfinding pathfinding)
        {
            m_pathfinding = pathfinding;
            m_pathNodes = new List<MyHighLevelPrimitive>();
            m_expandedPath = new List<Vector4D>();
        }

        public void Init(Vector3D start, MySmartGoal goal)
        {
            ProfilerShort.Begin("MySmartPath.Init()");
            Debug.Assert(m_valid == false);

            m_lastInitTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            m_startPoint = start;
            m_goal = goal;

            ProfilerShort.Begin("Find start primitive");
            ProfilerShort.Begin("FindClosestPrimitive");
            m_currentPrimitive = m_pathfinding.FindClosestPrimitive(start, highLevel: false);
            if (m_currentPrimitive != null)
            {
                ProfilerShort.BeginNextBlock("GetHighLevelPrimitive");
                m_hlBegin = m_currentPrimitive.GetHighLevelPrimitive();
                Debug.Assert(m_hlBegin != null, "Start primitive did not have a high-level primitive!");

                if (m_hlBegin != null && !m_pathNodes.Contains(m_hlBegin))
                {
                    ProfilerShort.BeginNextBlock("ObservePrimitive");
                    m_hlBegin.Parent.ObservePrimitive(m_hlBegin, this);
                }
            }
            ProfilerShort.End();
            ProfilerShort.End();

            if (m_currentPrimitive == null)
            {
                // CH: TODO: Starting primitive was not found. What to do now?
                m_currentPrimitive = null;
                Invalidate();
                ProfilerShort.End();
                return;
            }

            m_pathNodePosition = 0;
            m_expandedPathPosition = 0;
            m_expandedPath.Clear();
            m_pathNodes.Clear();
            m_usedWholePath = false;

            m_valid = true;
            ProfilerShort.End();
        }

        public void Reinit(Vector3D newStart)
        {
            var previousGoal = m_goal;
            var previousEntity = previousGoal.EndEntity;

            ClearPathNodes();

            m_expandedPath.Clear();
            m_expandedPathPosition = 0;
            m_currentPrimitive = null;

            if (m_hlBegin != null)
            {
                m_hlBegin.Parent.StopObservingPrimitive(m_hlBegin, this);
            }
            m_hlBegin = null;

            m_valid = false;

            m_goal.Reinit();
            Init(newStart, previousGoal);
        }

        public bool GetNextTarget(Vector3D currentPosition, out Vector3D targetWorld, out float radius, out IMyEntity relativeEntity)
        {
            bool shouldReinit = false;

            targetWorld = default(Vector3D);
            radius = 1.0f;
            relativeEntity = null;

            if (m_pathNodePosition > 1)
            {
                // clearing of node on the begin of path
                ClearFirstPathNode();
            }

            if (m_expandedPathPosition >= m_expandedPath.Count)
            {
                if (!m_usedWholePath)
                {
                    shouldReinit = ShouldReinitPath();
                }
                if (shouldReinit)
                {
                    Reinit(currentPosition);
                }

                Debug.Assert(m_goal.IsValid);
                Debug.Assert(m_valid);
                Debug.Assert(IsValid, "Path is not valid in MySmartPath.GetNextTarget");
                if (!IsValid)
                {
                    return false;
                }

                ProfilerShort.Begin("ExpandPath");
                ExpandPath(currentPosition);
                ProfilerShort.End();

                if (m_expandedPath.Count == 0)
                {
                    return false;
                }
                Debug.Assert(m_expandedPathPosition < m_expandedPath.Count);
            }

            if (m_expandedPathPosition < m_expandedPath.Count)
            {
                Vector4D pathNode = m_expandedPath[m_expandedPathPosition];
                targetWorld = new Vector3D(pathNode);
                radius = (float)pathNode.W;

                m_expandedPathPosition++;
                if (m_expandedPathPosition == m_expandedPath.Count && m_pathNodePosition >= m_pathNodes.Count - 1)
                {
                    m_usedWholePath = true;
                }

                relativeEntity = null;
                return true;
            }

            return false;
        }

        public void Invalidate()
        {
            if (m_valid == false)
            {
                return;
            }

            ClearPathNodes();

            m_expandedPath.Clear();
            m_expandedPathPosition = 0;

            m_currentPrimitive = null;

            if (m_goal.IsValid)
            {
                m_goal.Invalidate();
            }
            if (m_hlBegin != null)
            {
                m_hlBegin.Parent.StopObservingPrimitive(m_hlBegin, this);
            }
            m_hlBegin = null;

            m_valid = false;
        }

        private void ExpandPath(Vector3D currentPosition)
        {
            if (m_pathNodePosition >= m_pathNodes.Count - 1)
            {
                ProfilerShort.Begin("GenerateHighLevelPath");
                GenerateHighLevelPath();
                ProfilerShort.End();
            }

            if (m_pathNodePosition >= m_pathNodes.Count)
            {
                return;
            }

            MyPath<MyNavigationPrimitive> path = null;
            bool isLastPath = false;

            m_expandedPath.Clear();
            if (m_pathNodePosition + 1 < m_pathNodes.Count)
            {
                if (m_pathNodes[m_pathNodePosition].IsExpanded)
                {
                    if (m_pathNodes[m_pathNodePosition + 1].IsExpanded)
                    {
                        IMyHighLevelComponent component = m_pathNodes[m_pathNodePosition].GetComponent();
                        IMyHighLevelComponent otherComponent = m_pathNodes[m_pathNodePosition + 1].GetComponent();

                        // CH: TODO: Preallocate the functions to avoid using lambdas.
                        ProfilerShort.Begin("FindPath to next compo");
                        path = m_pathfinding.FindPath(m_currentPrimitive, m_goal.PathfindingHeuristic, (prim) => otherComponent.Contains(prim) ? 0.0f : float.PositiveInfinity, (prim) => component.Contains(prim) || otherComponent.Contains(prim));
                        ProfilerShort.End();
                    }
                    else
                    {
                        Debug.Assert(!MyFakes.SHOW_PATH_EXPANSION_ASSERTS, "First hierarchy path node is expanded, but the second one is not! First two nodes should always be expanded so that pathfinding can be done.");
                    }
                }
                else
                {
                    Debug.Assert(!MyFakes.SHOW_PATH_EXPANSION_ASSERTS, "Nodes of smart path are not expanded!");
                }
            }
            else
            {
                if (m_pathNodes[m_pathNodePosition].IsExpanded)
                {
                    // Try to find a path to a goal primitive inside the last high level component.
                    // If the last primitive of the found path is not in the last high level component, add that component to the goal's ignore list
                    IMyHighLevelComponent component = m_pathNodes[m_pathNodePosition].GetComponent();

                    ProfilerShort.Begin("FindPath to goal in the last component");
                    path = m_pathfinding.FindPath(m_currentPrimitive, m_goal.PathfindingHeuristic, (prim) => component.Contains(prim) ? m_goal.TerminationCriterion(prim) : 30.0f, (prim) => component.Contains(prim));
                    ProfilerShort.End();

                    if (path != null)
                    {
                        // We reached goal
                        if (path.Count != 0 && component.Contains(path[path.Count - 1].Vertex as MyNavigationPrimitive))
                        {
                            isLastPath = true;
                        }
                        // We reached other component (goal could not be reached in this component)
                        else
                        {
                            m_goal.IgnoreHighLevel(m_pathNodes[m_pathNodePosition]);
                        }
                    }
                }
                else
                {
                    Debug.Assert(!MyFakes.SHOW_PATH_EXPANSION_ASSERTS, "Nodes of smart path are not expanded!");
                }
            }

            if (path == null || path.Count == 0)
            {
                return;
            }

            Vector3D end = default(Vector3D);
            var lastPrimitive = path[path.Count - 1].Vertex as MyNavigationPrimitive;
            if (isLastPath)
            {
                Vector3 endPoint = m_goal.Destination.GetBestPoint(lastPrimitive.WorldPosition);
                Vector3 localEnd = lastPrimitive.Group.GlobalToLocal(endPoint);
                localEnd = lastPrimitive.ProjectLocalPoint(localEnd);
                end = lastPrimitive.Group.LocalToGlobal(localEnd);
            }
            else
            {
                end = lastPrimitive.WorldPosition;
            }
            
            RefineFoundPath(ref currentPosition, ref end, path);

            // If the path is too short, don't use it to prevent jerking in stuck situations
            if (m_pathNodes.Count <= 1 && isLastPath && m_expandedPath.Count > 0 && path.Count <= 2 && !m_goal.ShouldReinitPath())
            {
                Vector4D end4D = m_expandedPath[m_expandedPath.Count - 1];
                // Here, 256 is just a small enough value to catch most false-positives
                if (Vector3D.DistanceSquared(currentPosition, end) < end4D.W * end4D.W / 256)
                {
                     m_expandedPath.Clear();
                }
            }
        }

        private bool ShouldReinitPath()
        {
            // This prevents re-initializing the path too often
            if (MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastInitTime < 1000)
            {
                return false;
            }

            // Otherwise, the path needs to be reinitialized only when the target entity moves
            return m_goal.ShouldReinitPath();
        }

        private void GenerateHighLevelPath()
        {
            ClearPathNodes();

            if (m_hlBegin == null)
            {
                return;
            }

            var path = m_goal.FindHighLevelPath(m_pathfinding, m_hlBegin);
            if (path == null)
            {
                // CH: TODO: No path found (not even to an unexplored primitive). We're trapped! What now?
                return;
            }

            foreach (var primitive in path)
            {
                Debug.Assert(primitive.Vertex is MyHighLevelPrimitive);

                var hlPrimitive = primitive.Vertex as MyHighLevelPrimitive;
                m_pathNodes.Add(hlPrimitive);

                if (hlPrimitive != m_hlBegin)
                {
                    hlPrimitive.Parent.ObservePrimitive(hlPrimitive, this);
                }
            }

            m_pathNodePosition = 0;
        }

        private void RefineFoundPath(ref Vector3D begin, ref Vector3D end, MyPath<MyNavigationPrimitive> path)
        {
            Debug.Assert(MyPerGameSettings.EnablePathfinding, "Pathfinding is not enabled!");
            if (!MyPerGameSettings.EnablePathfinding)
            {
                return;
            }

            if (path == null)
            {
                Debug.Assert(false, "Path to refine was null!");
                return;
            }

            m_currentPrimitive = path[path.Count - 1].Vertex as MyNavigationPrimitive;
            if (m_hlBegin != null && !m_pathNodes.Contains(m_hlBegin))
            {
                m_hlBegin.Parent.StopObservingPrimitive(m_hlBegin, this);
            }
            m_hlBegin = m_currentPrimitive.GetHighLevelPrimitive();
            if (m_hlBegin != null && !m_pathNodes.Contains(m_hlBegin))
            {
                m_hlBegin.Parent.ObservePrimitive(m_hlBegin, this);
            }

            ProfilerShort.Begin("Path refining and post-processing");
            IMyNavigationGroup prevGroup = null;
            int groupStart = 0;
            int groupEnd = 0;
            Vector3 prevBegin = default(Vector3);
            Vector3 prevEnd = default(Vector3);
            for (int i = 0; i < path.Count; ++i)
            {
                var primitive = path[i].Vertex as MyNavigationPrimitive;
                var group = primitive.Group;

                if (prevGroup == null)
                {
                    prevGroup = group;
                    prevBegin = prevGroup.GlobalToLocal(begin);
                }

                bool lastPrimitive = i == path.Count - 1;

                if (group != prevGroup)
                {
                    groupEnd = i - 1;
                    prevEnd = prevGroup.GlobalToLocal(primitive.WorldPosition);
                }
                else if (lastPrimitive)
                {
                    groupEnd = i;
                    prevEnd = prevGroup.GlobalToLocal(end);
                }
                else
                {
                    continue;
                }

                int refinedBegin = m_expandedPath.Count;
                prevGroup.RefinePath(path, m_expandedPath, ref prevBegin, ref prevEnd, groupStart, groupEnd);
                int refinedEnd = m_expandedPath.Count;
                for (int j = refinedBegin; j < refinedEnd; ++j)
                {
                    Vector3D position = new Vector3D(m_expandedPath[j]);
                    position = prevGroup.LocalToGlobal(position);

                    m_expandedPath[j] = new Vector4D(position, m_expandedPath[j].W);
                }

                if (lastPrimitive && group != prevGroup)
                {
                    m_expandedPath.Add(new Vector4D(primitive.WorldPosition, m_expandedPath[refinedEnd - 1].W));
                }

                prevGroup = group;
                groupStart = i;
                if (m_expandedPath.Count != 0)
                    prevBegin = group.GlobalToLocal(new Vector3D(m_expandedPath[m_expandedPath.Count - 1]));
            }

            m_pathNodePosition++;

            //m_expandedPath.RemoveAt(0);
            m_expandedPathPosition = 0;

            ProfilerShort.End();
        }

        private void ClearPathNodes()
        {
            foreach (var pathNode in m_pathNodes)
            {
                if (pathNode == m_hlBegin) continue;
                pathNode.Parent.StopObservingPrimitive(pathNode, this);
            }

            m_pathNodes.Clear();
            m_pathNodePosition = 0;
        }

        private void ClearFirstPathNode()
        {
            foreach (var pathNode in m_pathNodes)
            {
                if (pathNode == m_hlBegin) break; ;
                pathNode.Parent.StopObservingPrimitive(pathNode, this);
                break;
            }
            m_pathNodes.RemoveAt(0);
            m_pathNodePosition--;
        }

        public void DebugDraw()
        {
            var matrix = Sandbox.Game.World.MySector.MainCamera.ViewMatrix;

            Vector3D? prevPosition = null;
            foreach (var node in m_pathNodes)
            {
                Vector3D down = Sandbox.Game.GameSystems.MyGravityProviderSystem.CalculateTotalGravityInPoint(node.WorldPosition);
                if (Vector3D.IsZero(down, 0.001)) down = Vector3D.Down;
                down.Normalize();
                Vector3D position = node.WorldPosition + down * -10.0;
                MyRenderProxy.DebugDrawSphere(position, 1.0f, Color.IndianRed, 1.0f, false);
                MyRenderProxy.DebugDrawLine3D(node.WorldPosition, position, Color.IndianRed, Color.IndianRed, false);
                if (prevPosition.HasValue)
                {
                    MyRenderProxy.DebugDrawLine3D(position, prevPosition.Value, Color.IndianRed, Color.IndianRed, false);
                }

                prevPosition = position;
            }

            /*if (m_hlEnd != null)
            {
                Vector3D position = m_hlEnd.WorldPosition + new Vector3D(0.0f, 10.0f, 0.0f);
                MyRenderProxy.DebugDrawSphere(position, 1.0f, Color.Crimson, 1.0f, false);
                MyRenderProxy.DebugDrawLine3D(m_hlEnd.WorldPosition, position, Color.Crimson, Color.Crimson, false);
            }*/

            MyRenderProxy.DebugDrawSphere(m_startPoint, 0.5f, Color.HotPink, 1.0f, false);
            /*MyRenderProxy.DebugDrawSphere(m_endPoint, 0.5f, Color.HotPink, 1.0f, false);
            MyRenderProxy.DebugDrawLine3D(m_startPoint, m_endPoint, Color.HotPink, Color.HotPink, false);*/

            if (m_goal != null)
                m_goal.DebugDraw();

            if (MyFakes.DEBUG_DRAW_FOUND_PATH)
            {
                Vector3D? prevPoint = null;
                for (int i = 0; i < m_expandedPath.Count; ++i)
                {
                    Vector3D point = new Vector3D(m_expandedPath[i]);
                    float radius = (float)m_expandedPath[i].W;
                    Color col = i == m_expandedPath.Count - 1 ? Color.OrangeRed : Color.Orange;
                    //VRageRender.MyRenderProxy.DebugDrawSphere(point, radius, col, 1.0f, false);
                    VRageRender.MyRenderProxy.DebugDrawPoint(point, col, false);
                    VRageRender.MyRenderProxy.DebugDrawText3D(point + matrix.Right * 0.1f, radius.ToString(), col, 0.7f, false);
                    if (prevPoint.HasValue)
                    {
                        VRageRender.MyRenderProxy.DebugDrawLine3D(prevPoint.Value, point, Color.Pink, Color.Pink, false);
                    }
                    prevPoint = point;
                }
            }
        }
    }
}
