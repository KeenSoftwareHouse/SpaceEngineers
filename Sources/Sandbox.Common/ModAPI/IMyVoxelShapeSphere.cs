using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.ModAPI
{
    public interface IMyVoxelShapeSphere:IMyVoxelShape
    {
        /// <summary>
        /// In World Space
        /// </summary>
        Vector3D Center
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
