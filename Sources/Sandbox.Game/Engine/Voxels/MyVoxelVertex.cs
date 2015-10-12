using System.Diagnostics;
using VRage.Import;
using VRageMath;
using VRageMath.PackedVector;

namespace Sandbox.Engine.Voxels
{
    public struct MyVoxelVertex
    {
        public Vector3 Position;
        public Vector3 PositionMorph;

        /// <summary>
        /// Ambient coefficient in range from -1 to 1.
        /// </summary>
        public float Ambient;
        public float AmbientMorph;

        public Vector3 Normal;
        public Vector3 NormalMorph;

        public int Material;
        public int MaterialMorph;

        public Vector3I Cell;
    }
}
