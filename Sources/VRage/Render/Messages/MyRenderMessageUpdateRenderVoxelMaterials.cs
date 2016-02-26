using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;

namespace VRageRender
{
    public class MyRenderMessageUpdateRenderVoxelMaterials : MyRenderMessageBase
    {
        public MyRenderVoxelMaterialData[] Materials;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateRenderVoxelMaterials; } }
    }
}
