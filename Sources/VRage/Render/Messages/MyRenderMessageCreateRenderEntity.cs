using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Import;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageCreateRenderEntity : MyRenderMessageBase
    {
        public uint ID;
        public string DebugName;
        public string Model;
        public MatrixD WorldMatrix;
        public MyMeshDrawTechnique Technique;
        public RenderFlags Flags;
        public byte DepthBias;
        public CullingOptions CullingOptions;
        public float MaxViewDistance;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.CreateRenderEntity; } }

        // Debug output
        public override string ToString()
        {
            return DebugName ?? String.Empty + ", " + Model ?? String.Empty;
        }
    }
}
