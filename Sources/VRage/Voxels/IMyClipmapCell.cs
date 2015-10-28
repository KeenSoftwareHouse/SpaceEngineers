using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace VRage.Voxels
{
    public interface IMyClipmapCell
    {
        void UpdateMesh(MyRenderMessageUpdateClipmapCell msg);

        void UpdateWorldMatrix(ref MatrixD worldMatrix, bool sortIntoCullObjects);
    }  
}
