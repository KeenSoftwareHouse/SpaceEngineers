using Havok;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Voxels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Entities.VoxelMaps
{
    class MyVoxelPhysicsBody : MyPhysicsBody
    {
        public enum ShapeEnum
        {
            List,
            StaticCompound,
            UniformGrid,
        }

        public static ShapeEnum CollShape = ShapeEnum.UniformGrid;

        private MyVoxelMap m_voxelMap;
        private HashSet<Vector3I> m_physicsChangedCells = new HashSet<Vector3I>(Vector3I.Comparer);
        private bool m_physicsDirty;

        internal bool IsDirty
        {
            get { return m_physicsDirty || m_physicsChangedCells.Count > 0; }
        }

        public bool IsEmpty
        {
            get;
            private set;
        }

        internal MyVoxelPhysicsBody(MyVoxelMap voxelMap): base(voxelMap, RigidBodyFlag.RBF_STATIC)
        {
            m_voxelMap = voxelMap;

            var shape = GetHavokShape();
            if (!shape.IsZero)
            {
                CreateFromCollisionObject(shape, -m_voxelMap.SizeInMetresHalf, m_voxelMap.WorldMatrix);
            }
            else
            {
                IsEmpty = true;
            }
        }

        internal void MarkDirty()
        {
            m_physicsDirty = true;
        }

        internal void UpdateShape()
        {
            if (!IsDirty)
                return;

            m_physicsDirty = false;

            switch (CollShape)
            {
                case ShapeEnum.List:            ReplaceShape(GetListShape()); break;
                case ShapeEnum.StaticCompound:  ReplaceShape(GetCompoundShape()); break;
                case ShapeEnum.UniformGrid:     RefreshUniformGridShape(); break;
                default:
                    throw new InvalidBranchException();
            }
        }

        private void ReplaceShape(HkShape shape)
        {
            if (shape.IsZero)
            {
                IsEmpty = true;
            }
            else
            {
                RigidBody.SetShape(shape);
                shape.RemoveReference();
                m_voxelMap.RaisePhysicsChanged();
            }
        }

        private HkShape GetListShape()
        {
            Vector3I cellCoord;
            Profiler.Begin("GetListShape - loop");
            List<HkShape> meshes = new List<HkShape>();
            var geometry = m_voxelMap.Geometry;
            var cellsCount = geometry.CellsCount;
            for (cellCoord.X = 0; cellCoord.X < cellsCount.X; cellCoord.X++)
            {
                for (cellCoord.Y = 0; cellCoord.Y < cellsCount.Y; cellCoord.Y++)
                {
                    for (cellCoord.Z = 0; cellCoord.Z < cellsCount.Z; cellCoord.Z++)
                    {
                        var cell = geometry.GetCell(MyLodTypeEnum.LOD0, ref cellCoord);
                        if (cell == null || cell.VoxelTrianglesCount == 0)
                            continue;

                        Profiler.Begin("MyVoxelMap::Get mesh shape");
                        meshes.Add(cell.GetMeshShape());
                        Profiler.End();
                    }
                }
            }
            Profiler.End();

            if (meshes.Count > 0)
                return new HkListShape(meshes.GetInternalArray(), meshes.Count, HkReferencePolicy.None);
            else
                return HkShape.Empty;
        }

        private HkShape GetCompoundShape()
        {
            HkStaticCompoundShape compound = new HkStaticCompoundShape(HkReferencePolicy.None);
            Vector3I cellCoord;
            Profiler.Begin("GetCompoundShape - loop");
            var geometry = m_voxelMap.Geometry;
            var cellsCount = geometry.CellsCount;
            for (cellCoord.X = 0; cellCoord.X < cellsCount.X; cellCoord.X++)
            {
                for (cellCoord.Y = 0; cellCoord.Y < cellsCount.Y; cellCoord.Y++)
                {
                    for (cellCoord.Z = 0; cellCoord.Z < cellsCount.Z; cellCoord.Z++)
                    {
                        var cell = geometry.GetCell(MyLodTypeEnum.LOD0, ref cellCoord);
                        if (cell == null || cell.VoxelTrianglesCount == 0)
                            continue;

                        Profiler.Begin("Get mesh shape");
                        compound.AddInstance(cell.GetMeshShape(), Matrix.Identity);
                        Profiler.End();
                    }
                }
            }
            Profiler.End();

            if (compound.InstanceCount > 0)
            {
                Profiler.Begin("MyVoxelMap::GetHavokShape() - bake");
                compound.Bake();
                Profiler.End();
                return compound;
            }
            else
            {
                compound.Base.RemoveReference();
                return HkShape.Empty;
            }
        }

        private HkShape GetUniformGridShape()
        {
            var geometry = m_voxelMap.Geometry;
            var cellsCount = geometry.CellsCount;
            HkUniformGridShape shape = new HkUniformGridShape(
                cellsCount.X, cellsCount.Y, cellsCount.Z,
                MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_METRES, HkReferencePolicy.None);
            RefreshUniformGridShapeRange(shape, Vector3I.Zero, cellsCount);
            if (shape.ShapeCount > 0)
            {
                return shape;
            }
            else
            {
                return HkShape.Empty;
            }
        }

        private void RefreshUniformGridShape()
        {
            Profiler.Begin("MyVoxelMap.RefreshUniformGridShape()");
            var shape = (HkUniformGridShape)RigidBody.GetShape();
            foreach (var cell in m_physicsChangedCells)
            {
                var localCell = cell;
                RefreshUniformGridShapeCell(ref shape, ref localCell);
            }

            m_physicsChangedCells.Clear();
            Profiler.End();

            m_voxelMap.RaisePhysicsChanged();
        }

        /// <param name="min">Inclusive min</param>
        /// <param name="max">Exclusive max</param>
        private void RefreshUniformGridShapeRange(HkUniformGridShape shape, Vector3I min, Vector3I max)
        {
            Vector3I cellCoord;
            Profiler.Begin("MyVoxelMap.RefreshUniformGridShapeRange() - loop");
            for (cellCoord.X = min.X; cellCoord.X < max.X; cellCoord.X++)
            for (cellCoord.Y = min.Y; cellCoord.Y < max.Y; cellCoord.Y++)
            for (cellCoord.Z = min.Z; cellCoord.Z < max.Z; cellCoord.Z++)
            {
                RefreshUniformGridShapeCell(ref shape, ref cellCoord);
            }
            Profiler.End();
        }

        private void RefreshUniformGridShapeCell(ref HkUniformGridShape shape, ref Vector3I cellCoord)
        {
            var cell = m_voxelMap.Geometry.GetCell(MyLodTypeEnum.LOD0, ref cellCoord);
            if (cell == null || cell.VoxelTrianglesCount == 0)
                shape.RemoveChild(cellCoord.X, cellCoord.Y, cellCoord.Z);
            else
                cell.SetShapeToCell(shape, ref cellCoord);
        }

        private HkShape GetHavokShape()
        {
            try
            {
                Profiler.Begin("MyVoxelMap.GetHavokShape()");
                switch (CollShape)
                {
                    case ShapeEnum.List: return GetListShape();
                    case ShapeEnum.StaticCompound: return GetCompoundShape();
                    case ShapeEnum.UniformGrid: return GetUniformGridShape();
                    default:
                        throw new InvalidBranchException();
                }
            }
            finally
            {
                Profiler.End();
            }
        }

        /// <param name="minVoxelChanged">Inclusive min.</param>
        /// <param name="maxVoxelChanged">Inclusive max.</param>
        internal void InvalidateRange(Vector3I minVoxelChanged, Vector3I maxVoxelChanged)
        {
            Vector3I minCellChanged, maxCellChanged;
            MyVoxelGeometry.ComputeCellCoord(ref minVoxelChanged, out minCellChanged);
            MyVoxelGeometry.ComputeCellCoord(ref maxVoxelChanged, out maxCellChanged);
            Vector3I cell;
            for (cell.X = minCellChanged.X; cell.X <= maxCellChanged.X; cell.X++)
            for (cell.Y = minCellChanged.Y; cell.Y <= maxCellChanged.Y; cell.Y++)
            for (cell.Z = minCellChanged.Z; cell.Z <= maxCellChanged.Z; cell.Z++)
            {
                m_physicsChangedCells.Add(cell);
            }
            m_physicsDirty = true;
        }
    }
}
