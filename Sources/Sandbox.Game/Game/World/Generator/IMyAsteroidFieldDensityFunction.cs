using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Noise;
using VRageMath;

namespace Sandbox.Game.World.Generator
{
    public interface IMyAsteroidFieldDensityFunction: IMyModule
    {
        bool ExistsInCell(ref BoundingBoxD bbox);
    }
}
