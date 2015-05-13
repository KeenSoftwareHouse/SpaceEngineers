using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.ModAPI
{
    public interface IMyVoxelShapeBox:IMyVoxelShape
    {
        /// <summary>
        /// Boundaries are in local space, you need to use transform property to rotate/translate shape 
        /// </summary>
        BoundingBoxD Boundaries
        {
            get;
            set;
        }
    }
}
