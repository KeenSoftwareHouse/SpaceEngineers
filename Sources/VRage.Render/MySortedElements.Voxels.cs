using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    partial class MySortedElements
    {
        public class VoxelSet
        {
            public const int AproxVoxelMaterials = 40; // Combinations of voxel (multi)materials in sector

            public int RenderElementCount = 0;

            public Dictionary<int, List<MyRender.MyRenderElement>> Voxels = new Dictionary<int, List<MyRender.MyRenderElement>>(AproxVoxelMaterials);
        }
    }
}
