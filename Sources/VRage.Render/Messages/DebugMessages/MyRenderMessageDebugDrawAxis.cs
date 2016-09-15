using VRageMath;

namespace VRageRender.Messages
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
