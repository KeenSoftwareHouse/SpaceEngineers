using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;

namespace VRageRender
{
    public class MyRenderMessageUpdateRenderVoxelMaterials : IMyRenderMessage
    {
        public MyRenderVoxelMaterialData[] Materials;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.UpdateRenderVoxelMaterials; } }
    }
}
