using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Noise;
using VRageMath;

namespace Sandbox.Game.World.Generator
{
    interface IMyAsteroidFieldDensityFunction: IMyModule
    {
        bool ExistsInCell(ref Vector3I cellId);
        double GetValue(ref Vector3D position);
    }
}
