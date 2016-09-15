using VRageMath;

namespace VRageRender.Messages
{
    public class MyRenderMessageDebugDrawFrustrum : MyDebugRenderMessage
    {
        public BoundingFrustum Frustrum;
        public Color Color;
        public float Alpha;
        public bool DepthRead;
        public bool Smooth;
        public bool Cull;

        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugDrawFrustrum; } }
    }
}
