using VRageMath;

namespace VRageRender.Messages
{
    public class MyRenderMessageDebugDrawOBB : MyDebugRenderMessage
    {
        public MatrixD Matrix;
        public Color Color;
        public float Alpha;
        public bool DepthRead;
        public bool Smooth;
        public bool Cull;

        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugDrawOBB; } }
    }
}
