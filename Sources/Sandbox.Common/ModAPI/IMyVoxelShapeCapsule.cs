using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.ModAPI
{
    public interface IMyVoxelShapeCapsule:IMyVoxelShape
    {
        /// <summary>
        /// In world Coordinates
        /// </summary>
        Vector3D A
        {
            get;
            set;
        }

        /// <summary>
        /// In world Coordinates
        /// </summary>
        Vector3D B
        {
            get;
            set;
        }


        float Radius
        {
            get;
            set;
        }
    }
}
