using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageMath
{
    /// <summary>
    /// This can be used only to compare normal vectors of Vector3I, where X, Y and Z has values -1, 0 or 1
    /// </summary>
    public class Vector3INormalEqualityComparer : IEqualityComparer<Vector3I>
    {
        public bool Equals(Vector3I x, Vector3I y)
        {
            return (x.X + 1) + (x.Y + 1) * 3 + (x.Z + 1) * 9 == (y.X + 1) + (y.Y + 1) * 3 + (y.Z + 1) * 9;
        }

        public int GetHashCode(Vector3I x)
        {
            // 0,1,2 + 0,3,6 + 0,9,18
            return (x.X + 1) + (x.Y + 1) * 3 + (x.Z + 1) * 9;
        }
    }
}
