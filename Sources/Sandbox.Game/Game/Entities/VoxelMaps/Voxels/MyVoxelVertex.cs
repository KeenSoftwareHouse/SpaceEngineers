using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Common.Import;
using VRage.Common.Utils;
using VRageMath;
using VRageMath.PackedVector;

namespace Sandbox.Game.Entities.VoxelMaps.Voxels
{
    public struct MyVoxelVertex
    {
        /// <summary>
        /// Position in range <0; 1>.
        /// </summary>
        public Vector3 Position;
        /// <summary>
        /// Ambient coefficient in range <-1; 1>.
        /// </summary>
        public float Ambient;

        public Vector3 Normal;

        public int Material;
    }

    public struct MyPackedVoxelVertex
    {
        /// <summary>
        /// Multiplier for mapping value from float<-1, 1> to short<-32767, 32767>.
        /// </summary>
        public const int AMBIENT_PACK = short.MaxValue;
        public const float AMBIENT_UNPACK = 1f / AMBIENT_PACK;

        /// <summary>
        /// Multiplier for mapping value from float<0;1> to ushort<0; ushort.MaxValue>
        /// </summary>
        public const float POSITION_PACK = ushort.MaxValue;
        public const float POSITION_UNPACK = 1f / POSITION_PACK;

        public Vector3Ushort Position;
        public short Ambient;
        public Byte4 Normal;
        public int Material;

        public static explicit operator MyPackedVoxelVertex(MyVoxelVertex v)
        {
            Debug.Assert(v.Position.IsInsideInclusive(ref Vector3.Zero, ref Vector3.One));
            Debug.Assert(-1f <= v.Ambient && v.Ambient <= 1f);

            MyPackedVoxelVertex result;
            result.Position.X = (ushort)(v.Position.X * POSITION_PACK);
            result.Position.Y = (ushort)(v.Position.Y * POSITION_PACK);
            result.Position.Z = (ushort)(v.Position.Z * POSITION_PACK);
            result.Ambient    = (short)(v.Ambient * AMBIENT_PACK);
            result.Normal     = new Byte4(VF_Packer.PackNormal(ref v.Normal));
            result.Material   = v.Material;
            return result;
        }

        public static explicit operator MyVoxelVertex(MyPackedVoxelVertex v)
        {
            MyVoxelVertex result;
            result.Position.X = v.Position.X * POSITION_UNPACK;
            result.Position.Y = v.Position.Y * POSITION_UNPACK;
            result.Position.Z = v.Position.Z * POSITION_UNPACK;
            result.Ambient    = v.Ambient * AMBIENT_UNPACK;
            result.Normal     = VF_Packer.UnpackNormal(ref v.Normal);
            result.Material   = v.Material;
            return result;
        }
    }
}
