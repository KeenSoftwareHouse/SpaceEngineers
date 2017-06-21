using VRageMath;

namespace VRageRender.Messages
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
