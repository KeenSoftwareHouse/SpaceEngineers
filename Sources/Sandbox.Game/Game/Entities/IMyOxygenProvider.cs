using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.Entities
{
    public interface IMyOxygenProvider
    {
        float GetOxygenForPosition(Vector3D worldPoint);
        bool IsPositionInRange(Vector3D worldPoint);
    }
}
