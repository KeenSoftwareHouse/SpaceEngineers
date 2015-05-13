using Sandbox.Game.Voxels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Voxels
{
    public interface IMyIsoMesher
    {
        int AffectedRangeOffset { get; }
        int AffectedRangeSizeChange { get; }
        int InvalidatedRangeInflate { get; }
        int VertexPositionRangeSizeChange { get; }

        void Precalc(MyVoxelPrecalcTaskItem task);
    }
}
