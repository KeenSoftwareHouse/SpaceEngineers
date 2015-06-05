using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRage.Voxels
{
    /// <summary>
    /// Functions for transforming to and from various coordinate systems in voxel maps and for computing bounding boxes of various types of cells.
    /// Note that local and world positions are (and should be) always in the min-corner!
    /// </summary>
    public static class MyVoxelCoordSystems
    {
        public static void WorldPositionToLocalPosition(Vector3D referenceVoxelMapPosition, ref Vector3D worldPosition, out Vector3D localPosition)
        {
            localPosition = worldPosition - referenceVoxelMapPosition;
        }

        public static void LocalPositionToWorldPosition(Vector3D referenceVoxelMapPosition, ref Vector3D localPosition, out Vector3D worldPosition)
        {
            worldPosition = localPosition + referenceVoxelMapPosition;
        }


        public static void LocalPositionToVoxelCoord(ref Vector3D localPosition, out Vector3I voxelCoord)
        {
            var tmp = localPosition / MyVoxelConstants.VOXEL_SIZE_IN_METRES;
            Vector3I.Floor(ref tmp, out voxelCoord);
        }

        public static void LocalPositionToVoxelCoord(ref Vector3D localPosition, out Vector3D voxelCoord)
        {
            voxelCoord = localPosition / MyVoxelConstants.VOXEL_SIZE_IN_METRES;
        }

        public static void LocalPositionToGeometryCellCoord(ref Vector3D localPosition, out Vector3I geometryCellCoord)
        {
            Vector3D tmp = localPosition / MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_METRES;
            Vector3I.Floor(ref tmp, out geometryCellCoord);
        }

        public static void LocalPositionToRenderCellCoord(ref Vector3D localPosition, out Vector3I renderCellCoord)
        {
            Vector3D tmp = localPosition / MyVoxelConstants.RENDER_CELL_SIZE_IN_METRES;
            Vector3I.Floor(ref tmp, out renderCellCoord);
        }


        public static void WorldPositionToVoxelCoord(Vector3D referenceVoxelMapPosition, ref Vector3D worldPosition, out Vector3I voxelCoord)
        {
            Vector3D localPosition;
            WorldPositionToLocalPosition(referenceVoxelMapPosition, ref worldPosition, out localPosition);
            LocalPositionToVoxelCoord(ref localPosition, out voxelCoord);
        }

        public static void WorldPositionToGeometryCellCoord(Vector3D referenceVoxelMapPosition, ref Vector3D worldPosition, out Vector3I geometryCellCoord)
        {
            Vector3D tmp;
            WorldPositionToLocalPosition(referenceVoxelMapPosition, ref worldPosition, out tmp);
            LocalPositionToGeometryCellCoord(ref tmp, out geometryCellCoord);
        }

        public static void WorldPositionToRenderCellCoord(Vector3D referenceVoxelMapPosition, ref Vector3D worldPosition, out Vector3I renderCellCoord)
        {
            Vector3D tmp;
            WorldPositionToLocalPosition(referenceVoxelMapPosition, ref worldPosition, out tmp);
            tmp /= MyVoxelConstants.RENDER_CELL_SIZE_IN_METRES;
            Vector3I.Floor(ref tmp, out renderCellCoord);
        }


        public static void VoxelCoordToLocalPosition(ref Vector3I voxelCoord, out Vector3D localPosition)
        {
            localPosition = voxelCoord * MyVoxelConstants.VOXEL_SIZE_IN_METRES;
        }

        public static void GeometryCellCoordToLocalPosition(ref Vector3I geometryCellCoord, out Vector3D localPosition)
        {
            localPosition = geometryCellCoord * MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_METRES;
        }

        public static void RenderCellCoordToLocalPosition(ref MyCellCoord renderCell, out Vector3D localPosition)
        {
            var scale = 1 << renderCell.Lod;
            localPosition = renderCell.CoordInLod * scale * MyVoxelConstants.RENDER_CELL_SIZE_IN_METRES;
        }


        public static void VoxelCoordToWorldPosition(Vector3D referenceVoxelMapPosition, ref Vector3I voxelCoord, out Vector3D worldPosition)
        {
            Vector3D localPosition;
            VoxelCoordToLocalPosition(ref voxelCoord, out localPosition);
            LocalPositionToWorldPosition(referenceVoxelMapPosition, ref localPosition, out worldPosition);
        }

        public static void RenderCellCoordToWorldPosition(Vector3D referenceVoxelMapPosition, ref MyCellCoord renderCell, out Vector3D worldPosition)
        {
            Vector3D localPosition;
            RenderCellCoordToLocalPosition(ref renderCell, out localPosition);
            LocalPositionToWorldPosition(referenceVoxelMapPosition, ref localPosition, out worldPosition);
        }


        public static void GeometryCellCoordToLocalAABB(ref Vector3I geometryCellCoord, out BoundingBox localAABB)
        {
            Vector3D localMinCorner;
            GeometryCellCoordToLocalPosition(ref geometryCellCoord, out localMinCorner);
            localAABB = new BoundingBox(localMinCorner, localMinCorner + MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_METRES);
        }

        public static void RenderCellCoordToLocalAABB(ref MyCellCoord renderCell, out BoundingBoxD localAABB)
        {
            Vector3D localMinCorner;
            RenderCellCoordToLocalPosition(ref renderCell, out localMinCorner);
            localAABB = new BoundingBoxD(localMinCorner, localMinCorner + (1 << renderCell.Lod) * MyVoxelConstants.RENDER_CELL_SIZE_IN_METRES);
        }


        public static void VoxelCoordToWorldAABB(Vector3D referenceVoxelMapPosition, ref Vector3I voxelCoord, out BoundingBoxD worldAABB)
        {
            Vector3D worldCenter;
            VoxelCoordToWorldPosition(referenceVoxelMapPosition, ref voxelCoord, out worldCenter);
            worldAABB = new BoundingBoxD(worldCenter, worldCenter + MyVoxelConstants.VOXEL_SIZE_IN_METRES);
        }

        public static void GeometryCellCoordToWorldAABB(Vector3D referenceVoxelMapPosition, ref Vector3I geometryCellCoord, out BoundingBoxD worldAABB)
        {
            Vector3D center;
            GeometryCellCoordToLocalPosition(ref geometryCellCoord, out center);
            LocalPositionToWorldPosition(referenceVoxelMapPosition, ref center, out center);
            worldAABB = new BoundingBoxD(center, center + MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_METRES);
        }

        public static void RenderCellCoordToWorldAABB(Vector3D referenceVoxelMapPosition, ref MyCellCoord renderCell, out BoundingBoxD worldAABB)
        {
            RenderCellCoordToLocalAABB(ref renderCell, out worldAABB);
            worldAABB = worldAABB.Translate(referenceVoxelMapPosition);
        }


        public static void VoxelCoordToGeometryCellCoord(ref Vector3I voxelCoord, out Vector3I geometryCellCoord)
        {
            geometryCellCoord = voxelCoord >> MyVoxelConstants.GEOMETRY_CELL_SIZE_IN_VOXELS_BITS;
        }

        public static void VoxelCoordToRenderCellCoord(ref Vector3I voxelCoord, out Vector3I renderCellCoord)
        {
            renderCellCoord.X = voxelCoord.X / MyVoxelConstants.RENDER_CELL_SIZE_IN_VOXELS;
            renderCellCoord.Y = voxelCoord.Y / MyVoxelConstants.RENDER_CELL_SIZE_IN_VOXELS;
            renderCellCoord.Z = voxelCoord.Z / MyVoxelConstants.RENDER_CELL_SIZE_IN_VOXELS;
        }


        public static void LocalPositionToVertexCell(int lod, ref Vector3 localPosition, out Vector3I vertexCell)
        {
            float scale = MyVoxelConstants.VOXEL_SIZE_IN_METRES * (1 << lod);
            vertexCell = Vector3I.Floor(localPosition / scale);
        }

        public static void VertexCellToLocalPosition(int lod, ref Vector3I vertexCell, out Vector3 localPosition)
        {
            float scale = MyVoxelConstants.VOXEL_SIZE_IN_METRES * (1 << lod);
            localPosition = vertexCell * scale;
        }

        public static void VertexCellToLocalAABB(int lod, ref Vector3I vertexCell, out BoundingBoxD localAABB)
        {
            float scale = MyVoxelConstants.VOXEL_SIZE_IN_METRES * (1 << lod);
            var minCorner = vertexCell * scale;
            localAABB = new BoundingBoxD(minCorner, minCorner + scale);
        }
    }
}
