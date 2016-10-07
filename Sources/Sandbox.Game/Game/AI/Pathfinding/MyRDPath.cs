using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.ModAPI;
using VRageMath;

namespace Sandbox.Game.AI.Pathfinding
{
    public class MyRDPath : IMyPath
    {
        #region Fields
        private MyRDPathfinding m_pathfinding;
        private IMyDestinationShape m_destination;
        private bool m_isValid, m_pathCompleted;
        List<Vector3D> m_pathPoints;
        int m_currentPointIndex;
        private MyPlanet m_planet;
        #endregion

        #region Constructor
        public MyRDPath(MyRDPathfinding pathfinding, Vector3D begin, IMyDestinationShape destination)
        {
            m_pathPoints = new List<Vector3D>();

            m_pathfinding = pathfinding;
            m_destination = destination;
            m_isValid = true;
            m_currentPointIndex = 0;

            m_planet = GetClosestPlanet(begin);
        }
        #endregion

        #region Interface
        public IMyDestinationShape Destination { get { return m_destination; } }

        public IMyEntity EndEntity { get { return null; } }

        public bool IsValid { get { return m_isValid; } }

        public bool PathCompleted { get { return m_pathCompleted; } }

        public void Invalidate()
        {
            m_isValid = false;
        }

        public bool GetNextTarget(Vector3D position, out Vector3D target, out float targetRadius, out IMyEntity relativeEntity)
        {
            target = Vector3D.Zero;
            relativeEntity = null;
            targetRadius = 0.8f;

            if (!m_isValid)
                return false;

            if (m_pathPoints.Count == 0 || m_pathCompleted || !m_isValid)
            {
                m_pathPoints = m_pathfinding.GetPath(m_planet, position, m_destination.GetDestination());
                if (m_pathPoints.Count < 2)
                    return false;

                // m_pathPoints[0] is the begin position
                m_currentPointIndex = 1;
            }
if(m_currentPointIndex == m_pathPoints.Count -1)
{
    //try to generate more points by RequestPath(position, destination)
}
            target = m_pathPoints[m_currentPointIndex];

            // check distance
            if (Math.Abs(Vector3.Distance(target, position)) < targetRadius)
            {
                if (m_currentPointIndex == m_pathPoints.Count - 1)
                {
                    m_pathCompleted = true;
                    return false;
                }
                else
                    m_currentPointIndex++;

                target = m_pathPoints[m_currentPointIndex];
            }

            return true;
        }

        public void Reinit(Vector3D position)
        {
            // TODO: do it to follow an entity
            //m_pathfinding.InitializeNavmesh(position);
        }

        public void DebugDraw()
        {
            if (m_pathPoints.Count > 0)
                for (int i = 0; i < m_pathPoints.Count - 1; i++)
                {
                    var worldPoint = m_pathPoints[i];
                    var nextWorldPoint = m_pathPoints[i + 1];
                    VRageRender.MyRenderProxy.DebugDrawLine3D(worldPoint, nextWorldPoint, Color.Blue, Color.Red, true);
                    VRageRender.MyRenderProxy.DebugDrawSphere(nextWorldPoint, 0.3f, Color.Yellow, 1f, true);
                }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Returns the planet closest to the given position
        /// </summary>
        /// <param name="position">3D Point from where the search is started</param>
        /// <returns>The closest planet</returns>
        private MyPlanet GetClosestPlanet(Vector3D position)
        {
            int voxelDistance = 200;
            BoundingBoxD box = new BoundingBoxD(position - voxelDistance * 0.5f, position + voxelDistance * 0.5f);
            return MyGamePruningStructure.GetClosestPlanet(ref box);
        }
        #endregion
    }
}
