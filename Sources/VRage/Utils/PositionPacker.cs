using System;
using System.Diagnostics;
using VRageMath;
using VRageMath.PackedVector;

namespace VRage.Import
{
    public static class PositionPacker
    {
        static public HalfVector4 PackPosition(ref Vector3 position)
        {
            float max_value = System.Math.Max(System.Math.Abs(position.X), System.Math.Abs(position.Y));
            max_value = System.Math.Max(max_value, System.Math.Abs(position.Z));
            float multiplier = System.Math.Min((float)System.Math.Floor(max_value), 2048.0f);
            float invMultiplier = 0;
            if (multiplier > 0)
                invMultiplier = 1.0f / multiplier;
            else
                multiplier = invMultiplier = 1.0f;

            return new HalfVector4(invMultiplier * position.X, invMultiplier * position.Y, invMultiplier * position.Z, multiplier);
        }

        static public Vector3 UnpackPosition(ref HalfVector4 position)
        {
            Vector4 unpacked = position.ToVector4();
            return unpacked.W * new Vector3(unpacked.X, unpacked.Y, unpacked.Z);
        }
    }
}
