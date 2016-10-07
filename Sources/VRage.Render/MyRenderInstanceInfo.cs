using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    [Flags]
    public enum MyInstanceFlagsEnum : byte
    {
        CastShadows = 0x1,
        ShowLod1 = 0x2,
        EnableColorMask = 0x4,
    }

    // 17 B
    public struct MyRenderInstanceInfo
    {
        public readonly uint InstanceBufferId; // 4B
        public readonly int InstanceStart; // 4B
        public readonly int InstanceCount; // 4B
        public readonly float MaxViewDistance; // 4B
        public readonly MyInstanceFlagsEnum Flags; // 1B

        public bool CastShadows { get { return (Flags & MyInstanceFlagsEnum.CastShadows) != 0; } }
        public bool ShowLod1 { get { return (Flags & MyInstanceFlagsEnum.ShowLod1) != 0; } }
        public bool EnableColorMask { get { return (Flags & MyInstanceFlagsEnum.EnableColorMask) != 0; } }

        public MyRenderInstanceInfo(uint instanceBufferId, int instanceStart, int instanceCount, float maxViewDistance, MyInstanceFlagsEnum flags)
        {
            Flags = flags;
            InstanceBufferId = instanceBufferId;
            InstanceStart = instanceStart;
            InstanceCount = instanceCount;
            MaxViewDistance = maxViewDistance;
        }

        public MyRenderInstanceInfo(uint instanceBufferId, int instanceStart, int instanceCount, bool castShadows, bool showLod1, float maxViewDistance, bool enableColorMaskHsv)
        {
            Flags = (castShadows ? MyInstanceFlagsEnum.CastShadows : 0) | (showLod1 ? MyInstanceFlagsEnum.ShowLod1 : 0) | (enableColorMaskHsv ? MyInstanceFlagsEnum.EnableColorMask : 0);
            InstanceBufferId = instanceBufferId;
            InstanceStart = instanceStart;
            InstanceCount = instanceCount;
            MaxViewDistance = maxViewDistance;
        }
    }
}
