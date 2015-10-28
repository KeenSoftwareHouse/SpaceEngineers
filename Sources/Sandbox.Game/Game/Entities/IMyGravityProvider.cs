using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.Entities
{
    public interface IMyGravityProvider
    {
        bool IsWorking
        {
            get;
        }

        Vector3 GetWorldGravity(Vector3D worldPoint);
        bool IsPositionInRange(Vector3D worldPoint);

        Vector3 GetWorldGravityGrid(Vector3D worldPoint);
        bool IsPositionInRangeGrid(Vector3D worldPoint);

		float GetGravityMultiplier(Vector3D worldPoint);
    }
}
