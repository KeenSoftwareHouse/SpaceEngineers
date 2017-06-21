using System;

namespace VRage.Render11.GeometryStage2.Common
{
    enum MyRenderPassType
    {
        GBuffer, // instanced
        Depth, // instanced
        Highlight, // this type does not use instancing
        Glass, // instanced
    }

    enum MyInstanceLodState
    {
        Solid = 0,
        Transition = 1,
        Hologram = 2,
        Dithered = 3,

        StatesCount = 4,
    }

    [Flags]
    internal enum MyVisibilityExtFlags
    {
        None = 0,
        Gbuffer = 1 << 0,
        Depth = 1 << 1,
        Forward = 1 << 2,
        All = 7
    }
}
