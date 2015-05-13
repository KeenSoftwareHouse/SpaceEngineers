using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.Entities.Cube
{
    struct OverlapResult
    {
        public Vector3I Position;
        public MyCubeBlock FatBlock;
        public MyBlockOrientation Orientation;
        public MyCubeBlockDefinition Definition;
    }

    interface IMyGridOverlapTest
    {
        void GetBlocks(Vector3I minI, Vector3I maxI, Dictionary<Vector3I, OverlapResult> outOverlappedBlocks);
    }
}
