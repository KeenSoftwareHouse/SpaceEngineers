using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Common
{
    /// <summary>
    /// structure used to set up the mesh
    /// </summary>
    public struct MyTriangleVertexIndices
    {
        public int I0, I1, I2;

        public MyTriangleVertexIndices(int i0, int i1, int i2)
        {
            this.I0 = i0;
            this.I1 = i1;
            this.I2 = i2;
        }

        public void Set(int i0, int i1, int i2)
        {
            I0 = i0; I1 = i1; I2 = i2;
        }
    }
}
