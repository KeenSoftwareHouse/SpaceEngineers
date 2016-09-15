using VRageMath;

namespace VRageRender.Messages
{
    public class MyRenderMessageDebugDrawAABB : MyDebugRenderMessage
    {
        public BoundingBoxD AABB;
        public Color Color;
        public float Alpha;
        public float Scale;
        public bool DepthRead;
        public bool Shaded;

        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugDrawAABB; } }
    }
}
