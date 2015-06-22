using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRage.Voxels
{
    public static class MyVoxelConstants
    {
        public const string FILE_EXTENSION = ".vx2";

        //  This is the value that says if voxel is full or not (but only for marching cubes algorithm, not path-finding, etc)
        //  It's the middle of 0 and 255
        public const byte VOXEL_ISO_LEVEL = 127;

        //  Value of voxel's content if voxel is empty
        public const byte VOXEL_CONTENT_EMPTY = 0;

        //  Value of voxel's content if voxel is full
        public const byte VOXEL_CONTENT_FULL = 255;
        public const float VOXEL_CONTENT_FULL_FLOAT = (float)VOXEL_CONTENT_FULL;

        //  Size of a voxel in metres
        public const float VOXEL_SIZE_IN_METRES = 1f;
        public const float VOXEL_VOLUME_IN_METERS = VOXEL_SIZE_IN_METRES * VOXEL_SIZE_IN_METRES * VOXEL_SIZE_IN_METRES;
        public const float VOXEL_SIZE_IN_METRES_HALF = VOXEL_SIZE_IN_METRES / 2.0f;
        public static readonly Vector3 VOXEL_SIZE_VECTOR = new Vector3(VOXEL_SIZE_IN_METRES, VOXEL_SIZE_IN_METRES, VOXEL_SIZE_IN_METRES);
        public static readonly Vector3 VOXEL_SIZE_VECTOR_HALF = VOXEL_SIZE_VECTOR / 2.0f;
        public static readonly float VOXEL_RADIUS = VOXEL_SIZE_VECTOR_HALF.Length();

        //  Size of a voxel data cell in voxels (count of voxels in a voxel data cell) - in one direction
        //  Assume it's a power of two!
        public const int DATA_CELL_SIZE_IN_VOXELS_BITS = 3;
        public const int DATA_CELL_SIZE_IN_VOXELS = 1 << DATA_CELL_SIZE_IN_VOXELS_BITS;
        public const int DATA_CELL_SIZE_IN_VOXELS_MASK = DATA_CELL_SIZE_IN_VOXELS - 1;
        public const int DATA_CELL_SIZE_IN_VOXELS_TOTAL = DATA_CELL_SIZE_IN_VOXELS * DATA_CELL_SIZE_IN_VOXELS * DATA_CELL_SIZE_IN_VOXELS;
        public const int DATA_CELL_CONTENT_SUM_TOTAL = DATA_CELL_SIZE_IN_VOXELS_TOTAL * VOXEL_CONTENT_FULL;
        public const float DATA_CELL_SIZE_IN_METRES = DATA_CELL_SIZE_IN_VOXELS * VOXEL_SIZE_IN_METRES;

        public const int GEOMETRY_CELL_SIZE_IN_VOXELS_BITS = 3;
        public const int GEOMETRY_CELL_SIZE_IN_VOXELS = 1 << GEOMETRY_CELL_SIZE_IN_VOXELS_BITS;
        public const int GEOMETRY_CELL_SIZE_IN_VOXELS_TOTAL = GEOMETRY_CELL_SIZE_IN_VOXELS * GEOMETRY_CELL_SIZE_IN_VOXELS * GEOMETRY_CELL_SIZE_IN_VOXELS;
        public const int GEOMETRY_CELL_MAX_TRIANGLES_COUNT = GEOMETRY_CELL_SIZE_IN_VOXELS_TOTAL * 5; // 5 is max. count of triangles in polygonization cube.
        public const float GEOMETRY_CELL_SIZE_IN_METRES = GEOMETRY_CELL_SIZE_IN_VOXELS * VOXEL_SIZE_IN_METRES;
        public const float GEOMETRY_CELL_SIZE_IN_METRES_HALF = GEOMETRY_CELL_SIZE_IN_METRES / 2.0f;
        public static readonly Vector3 GEOMETRY_CELL_SIZE_VECTOR_IN_METRES = new Vector3(GEOMETRY_CELL_SIZE_IN_METRES);
        public static readonly int GEOMETRY_CELL_CACHE_SIZE = (Environment.Is64BitProcess) ? (65536 * 4) : (int)(65536 * 1.2);

        public const int RENDER_CELL_SIZE_IN_GEOMETRY_CELLS_BITS = 2;
        public const int RENDER_CELL_SIZE_IN_GEOMETRY_CELLS = 1 << RENDER_CELL_SIZE_IN_GEOMETRY_CELLS_BITS;
        public const int RENDER_CELL_SIZE_IN_GEOMETRY_CELLS_TOTAL = RENDER_CELL_SIZE_IN_GEOMETRY_CELLS * RENDER_CELL_SIZE_IN_GEOMETRY_CELLS * RENDER_CELL_SIZE_IN_GEOMETRY_CELLS;
        public const int RENDER_CELL_SIZE_IN_VOXELS = GEOMETRY_CELL_SIZE_IN_VOXELS * RENDER_CELL_SIZE_IN_GEOMETRY_CELLS;
        public const float RENDER_CELL_SIZE_IN_METRES = RENDER_CELL_SIZE_IN_VOXELS * VOXEL_SIZE_IN_METRES;
        public const float RENDER_CELL_SIZE_IN_METRES_HALF = RENDER_CELL_SIZE_IN_METRES / 2.0f;
        public static readonly Vector3 RENDER_CELL_SIZE_VECTOR_IN_METRES = new Vector3(RENDER_CELL_SIZE_IN_METRES);

        // When wrinkling voxels using voxel hand, this is default wrinkle weight amount
        public const float DEFAULT_WRINKLE_WEIGHT_ADD = 0.5f;
        public const float DEFAULT_WRINKLE_WEIGHT_REMOVE = 0.45f;

        // Increment this when adding a new generator version
        public const int VOXEL_GENERATOR_VERSION = 2;
        public const int VOXEL_GENERATOR_MIN_ICE_VERSION = 1;
    }

}
