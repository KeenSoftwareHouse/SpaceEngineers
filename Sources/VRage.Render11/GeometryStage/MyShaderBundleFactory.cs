using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Generics;

using VRageMath;
using VRageRender.Resources;
using VRageRender.Vertex;
using Buffer = SharpDX.Direct3D11.Buffer;
using Matrix = VRageMath.Matrix;
using Vector3 = VRageMath.Vector3;
using BoundingBox = VRageMath.BoundingBox;
using BoundingFrustum = VRageMath.BoundingFrustum;
using VRage.Collections;
using System.Collections.Specialized;
using System.Threading;
using VRage.FileSystem;
using VRage.Utils;
using System.IO;
using SharpDX.D3DCompiler;

namespace VRageRender
{
    [Flags]
    enum MyShaderUnifiedFlags
    {
        NONE = 0,
        DEPTH_ONLY = 1,

        // only one!
        ALPHAMASK = 2,
        TRANSPARENT = 4,
        DITHERED = 8,
		USE_SHADOW_CASCADES = 16,
        ALPHAMASK_ARRAY = 32,

        USE_SKINNING = 0x100,
        USE_VOXEL_MORPHING = 0x2000,

        // only one!
        USE_CUBE_INSTANCING = 0x200,
        USE_DEFORMED_CUBE_INSTANCING = 0x400,
        USE_GENERIC_INSTANCING = 0x0800,
        USE_MERGE_INSTANCING = 0x1000,

        // hacks
        FOLIAGE = 0x10000,
    }

}
