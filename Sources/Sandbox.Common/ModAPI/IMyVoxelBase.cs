using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.ModAPI
{
    public interface IMyVoxelBase : IMyEntity
    {
        IMyStorage Storage { get; }

        VRageMath.Vector3D PositionLeftBottomCorner { get; }

        bool IsBoxIntersectingBoundingBoxOfThisVoxelMap(ref BoundingBoxD boundingBox);

        string StorageName
        {
            get;
        }
    }
}
