using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageDebugDrawAxis : MyDebugRenderMessage
    {
        public MatrixD Matrix;
        public float AxisLength;
        public bool DepthRead;
        public bool SkipScale;

        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugDrawAxis; } }
    }
}
