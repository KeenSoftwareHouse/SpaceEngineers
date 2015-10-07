using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRageRender;

namespace VRage.Voxels
{
    public struct MyClipmapCellBatch
    {
        public MyVertexFormatVoxelSingleData[] Vertices;
        public ushort[] Indices;
        public int Material0;
        public int Material1;
        public int Material2;
    }
}
