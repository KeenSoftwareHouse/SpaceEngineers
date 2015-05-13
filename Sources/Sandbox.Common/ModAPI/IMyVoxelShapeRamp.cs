using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.ModAPI
{
    public interface IMyVoxelShapeRamp : IMyVoxelShape
    {
        BoundingBoxD Boundaries
        {
            get;
            set;
        }

        /// <summary>
        /// normal of the sloped plane
        /// </summary>
        Vector3D RampNormal
        {
            get;
            set;
        }

        double RampNormalW
        {
            get;
            set;
        }
    }
}
