using System;

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

        USE_SKINNING = 0x100,
        USE_VOXEL_MORPHING = 0x2000,

        // only one!
        USE_CUBE_INSTANCING = 0x200,
        USE_DEFORMED_CUBE_INSTANCING = 0x400,
        USE_GENERIC_INSTANCING = 0x0800,
        USE_MERGE_INSTANCING = 0x1000,

        // hacks
        FOLIAGE = 0x10000
    }

}
