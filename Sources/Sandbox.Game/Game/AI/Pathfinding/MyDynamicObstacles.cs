using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Game.Entity;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.AI.Pathfinding
{
    // CH: TODO: Do properly...
    public static class MyObstacleFactory
    {
        public static IMyObstacle CreateObstacleForEntity(MyEntity entity)
        {
            // CH: Uncomment to enable
            return null;
            /*var grid = entity as MyCubeGrid;
            if (grid != null) return new MyGridObstacle(grid);

            return new MyBasicObstacle(entity);*/
        }
    }

    public interface IMyObstacle
    {
        bool Contains(ref Vector3D point);
        void Update();
        void DebugDraw();
    }

    // CH: TODO: For testing only so far.
    public class MyBasicObstacle : IMyObstacle
    {
        public MatrixD m_worldInv;
        public Vector3D m_halfExtents;

        private MyEntity m_entity;

        private bool m_valid = false;
        public bool Valid { get { return m_valid; } }

        public MyBasicObstacle(MyEntity entity)
        {
            m_entity = entity;
            m_entity.OnClosing += OnEntityClosing;
            Update();
            m_valid = true;
        }

        public bool Contains(ref Vector3D point)
        {
            Vector3D local;
            Vector3D.Transform(ref point, ref m_worldInv, out local);
            return Math.Abs(local.X) < m_halfExtents.X && Math.Abs(local.Y) < m_halfExtents.Y && Math.Abs(local.Z) < m_halfExtents.Z;
        }

        public void Update()
        {
            m_worldInv = m_entity.PositionComp.WorldMatrixNormalizedInv;
            m_halfExtents = m_entity.PositionComp.LocalAABB.Extents;
        }

        public void DebugDraw()
        {
            var mat = MatrixD.Invert(m_worldInv);
            MyOrientedBoundingBoxD obb = new MyOrientedBoundingBoxD(MatrixD.CreateScale(m_halfExtents) * mat);
            MyRenderProxy.DebugDrawOBB(obb, Color.Red, 0.3f, false, false);
        }

        void OnEntityClosing(MyEntity entity)
        {
            m_valid = false;
            m_entity = null;
        }
    }

    public class MyGridObstacle : IMyObstacle
    {
        private List<BoundingBox> m_segments;
        private static MyVoxelSegmentation m_segmentation = new MyVoxelSegmentation();

        private MatrixD m_worldInv;
        private MyCubeGrid m_grid;

        public MyGridObstacle(MyCubeGrid grid)
        {
            m_grid = grid;
            Segment();
            Update();
        }

        private void Segment()
        {
            m_segmentation.ClearInput();

            foreach (var block in m_grid.CubeBlocks)
            {
                Vector3I begin = block.Min;
                Vector3I end = block.Max;
                Vector3I pos = begin;
                for (var it = new Vector3I_RangeIterator(ref begin, ref end); it.IsValid(); it.GetNext(out pos))
                    m_segmentation.AddInput(pos);
            }

            var segmentList = m_segmentation.FindSegments(MyVoxelSegmentationType.Simple2);
            m_segments = new List<BoundingBox>(segmentList.Count);
            for (int i = 0; i < segmentList.Count; ++i)
            {
                BoundingBox bb = new BoundingBox();
                bb.Min = (new Vector3(segmentList[i].Min) - Vector3.Half) * m_grid.GridSize - Vector3.Half; // The another half is here to just add some head space
                bb.Max = (new Vector3(segmentList[i].Max) + Vector3.Half) * m_grid.GridSize + Vector3.Half;
                m_segments.Add(bb);
            }

            m_segmentation.ClearInput();
        }

        public bool Contains(ref Vector3D point)
        {
            Vector3D localPoint;
            Vector3D.Transform(ref point, ref m_worldInv, out localPoint);

            // Testing a point against extrusion of the BB along gravity is the same as
            // testing extrusion of the point (i.e. a ray going in the opposite direction) against the original BB.
            Vector3D testPoint = m_grid.PositionComp.WorldAABB.Center;
            Vector3 gravity = MyGravityProviderSystem.CalculateNaturalGravityInPoint(testPoint);
            gravity = Vector3.TransformNormal(gravity, m_worldInv);
            if (!Vector3.IsZero(gravity))
            {
                gravity = Vector3.Normalize(gravity);
                Ray ray = new Ray(localPoint, -gravity * 2.0f); // 2.0f = height of the character

                foreach (var segment in m_segments)
                {
                    if (segment.Intersects(ray).HasValue) return true;
                }
            }
            else
            {
                // CH: Use some kind of a tree or something better than a list here!
                foreach (var segment in m_segments)
                {
                    if (segment.Contains(localPoint) == ContainmentType.Contains) return true;
                }
            }

            return false;
        }

        public void Update()
        {
            Segment();
            m_worldInv = m_grid.PositionComp.WorldMatrixNormalizedInv;
        }

        public void DebugDraw()
        {
            MatrixD mat = MatrixD.Invert(m_worldInv);
            Quaternion orientation = Quaternion.CreateFromRotationMatrix(mat.GetOrientation());
            foreach (var segment in m_segments)
            {
                Vector3D halfExtents = new Vector3D(segment.Size) * 0.51;
                Vector3D center = new Vector3D(segment.Min + segment.Max) * 0.5;
                center = Vector3D.Transform(center, mat);

                var obb = new MyOrientedBoundingBoxD(center, halfExtents, orientation);
                MyRenderProxy.DebugDrawOBB(obb, Color.Red, 0.5f, false, false);
            }
        }
    }

    public class MyDynamicObstacles
    {
        // CH: TODO: Use a better data structure!
        private CachingList<IMyObstacle> m_obstacles;

        public MyDynamicObstacles()
        {
            m_obstacles = new CachingList<IMyObstacle>();
        }

        public void Clear()
        {
            m_obstacles.ClearImmediate();
        }

        public void Update()
        {
            foreach (var obstacle in m_obstacles)
            {
                obstacle.Update();
            }
            m_obstacles.ApplyChanges();
        }

        public bool IsInObstacle(Vector3D point)
        {
            foreach (var obstacle in m_obstacles)
            {
                if (obstacle.Contains(ref point)) return true;
            }

            return false;
        }

        public void DebugDraw()
        {
            foreach (var obstacle in m_obstacles)
            {
                obstacle.DebugDraw();
            }
        }

        public void TryCreateObstacle(MyEntity newEntity)
        {
            if (newEntity.Physics == null) return;
            if (!(newEntity is MyCubeGrid)) return;
            if (newEntity.PositionComp == null) return;

            var obstacle = MyObstacleFactory.CreateObstacleForEntity(newEntity);

            if (obstacle != null)
                m_obstacles.Add(obstacle);
        }
    }
}
