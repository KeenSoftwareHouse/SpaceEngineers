using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRageMath;
using VRageRender;

namespace VRage.Voxels
{
    public struct MyCellCoord
    {
        public const int MAX_LOD_COUNT = 1 << BITS_LOD;

        /// <summary>
        /// 0 is the most detailed.
        /// </summary>
        public int Lod;

        public Vector3I CoordInLod;

        public MyCellCoord(int lod, Vector3I coordInLod) :
            this(lod, ref coordInLod)
        {

        }

        public MyCellCoord(int lod, ref Vector3I coordInLod)
        {
            Lod = lod;
            CoordInLod = coordInLod;
        }

        public void SetUnpack(UInt32 id)
        {
            var original = id;
            CoordInLod.Z = (int)(id & MASK_Z_32); id >>= BITS_Z_32;
            CoordInLod.Y = (int)(id & MASK_Y_32); id >>= BITS_Y_32;
            CoordInLod.X = (int)(id & MASK_X_32); id >>= BITS_X_32;
            Lod = (int)id;
            Debug.Assert(PackId32() == original);
        }

        public void SetUnpack(UInt64 id)
        {
            var original = id;
            CoordInLod.Z = (int)(id & MASK_Z_64); id >>= BITS_Z_64;
            CoordInLod.Y = (int)(id & MASK_Y_64); id >>= BITS_Y_64;
            CoordInLod.X = (int)(id & MASK_X_64); id >>= BITS_X_64;
            Lod = (int)id;
            Debug.Assert(PackId64() == original);
        }

        public static int UnpackLod(UInt64 id)
        {
            return (int)(id >> SHIFT_LOD_64);
        }

        public UInt32 PackId32()
        {
            Debug.Assert(Lod <= MASK_LOD);
            Debug.Assert((CoordInLod.X & MASK_X_32) == CoordInLod.X, "Cell coord overflow!");
            Debug.Assert((CoordInLod.Y & MASK_Y_32) == CoordInLod.Y, "Cell coord overflow!");
            Debug.Assert((CoordInLod.Z & MASK_Z_32) == CoordInLod.Z, "Cell coord overflow!");

            return
                ((UInt32)Lod) << SHIFT_LOD_32 |
                ((UInt32)CoordInLod.X) << SHIFT_X_32 |
                ((UInt32)CoordInLod.Y) << SHIFT_Y_32 |
                ((UInt32)CoordInLod.Z) << SHIFT_Z_32;
        }

        public UInt64 PackId64()
        {
            Debug.Assert(Lod <= MASK_LOD);
            Debug.Assert((CoordInLod.X & MASK_X_64) == CoordInLod.X, "Cell coord overflow!");
            Debug.Assert((CoordInLod.Y & MASK_Y_64) == CoordInLod.Y, "Cell coord overflow!");
            Debug.Assert((CoordInLod.Z & MASK_Z_64) == CoordInLod.Z, "Cell coord overflow!");

            return
                ((UInt64)Lod) << SHIFT_LOD_64 |
                ((UInt64)CoordInLod.X) << SHIFT_X_64 |
                ((UInt64)CoordInLod.Y) << SHIFT_Y_64 |
                ((UInt64)CoordInLod.Z) << SHIFT_Z_64;
        }

        public static UInt64 GetClipmapCellHash(uint clipmap, ulong cellId)
        {
            ulong hash = (ulong)(cellId * 997);
            hash = (hash * 397) ^ (ulong)(clipmap * 997);
            return hash;
        }


        public override string ToString()
        {
            return string.Format("{0}, {1}", Lod, CoordInLod);
        }

        // Do not change these settings of bits! It will break backward compatibility.
        const int BITS_LOD = 4;

        const int BITS_X_32 = 10;
        const int BITS_Y_32 = 8;
        const int BITS_Z_32 = 10;

        const int BITS_X_64 = 20;
        const int BITS_Y_64 = 20;
        const int BITS_Z_64 = 20;

        const int SHIFT_LOD_32 = SHIFT_X_32 + BITS_X_32;
        const int SHIFT_X_32   = SHIFT_Y_32 + BITS_Y_32;
        const int SHIFT_Y_32   = SHIFT_Z_32 + BITS_Z_32;
        const int SHIFT_Z_32   = 0;

        const int SHIFT_LOD_64 = SHIFT_X_64 + BITS_X_64;
        const int SHIFT_X_64   = SHIFT_Y_64 + BITS_Y_64;
        const int SHIFT_Y_64   = SHIFT_Z_64 + BITS_Z_64;
        const int SHIFT_Z_64   = 0;

        const int MASK_LOD = (1 << BITS_LOD) - 1;

        const int MASK_X_32 = (1 << BITS_X_32) - 1;
        const int MASK_Y_32 = (1 << BITS_Y_32) - 1;
        const int MASK_Z_32 = (1 << BITS_Z_32) - 1;

        const int MASK_X_64 = (1 << BITS_X_64) - 1;
        const int MASK_Y_64 = (1 << BITS_Y_64) - 1;
        const int MASK_Z_64 = (1 << BITS_Z_64) - 1;

        static MyCellCoord()
        {
            Debug.Assert(BITS_LOD + BITS_X_32 + BITS_Y_32 + BITS_Z_32 <= 32);
            Debug.Assert(BITS_LOD + BITS_X_64 + BITS_Y_64 + BITS_Z_64 <= 64);
        }

    }
}
