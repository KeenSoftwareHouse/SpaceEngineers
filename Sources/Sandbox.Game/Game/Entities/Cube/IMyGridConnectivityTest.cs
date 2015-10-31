using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.Entities.Cube
{
    public struct ConnectivityResult
    {
        public Vector3I Position;
        public MyCubeBlock FatBlock;
        public MyBlockOrientation Orientation;
        public MyCubeBlockDefinition Definition;
    }

    public interface IMyGridConnectivityTest
    {
        void GetConnectedBlocks(Vector3I minI, Vector3I maxI, Dictionary<Vector3I, ConnectivityResult> outConnectedCubeBlocks);
    }
}
