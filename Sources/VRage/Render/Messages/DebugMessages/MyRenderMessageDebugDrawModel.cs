using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageDebugDrawModel : MyDebugRenderMessage
    {
        public string Model;
        public MatrixD WorldMatrix;
        public Color Color;
        public bool DepthRead;

        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugDrawModel; } }
    }
}
