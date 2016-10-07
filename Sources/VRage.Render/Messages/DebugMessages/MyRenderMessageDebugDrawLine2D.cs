using VRageMath;

namespace VRageRender.Messages
{
    public class MyRenderMessageDebugDrawLine2D : MyDebugRenderMessage
    {
        public Vector2 PointFrom;
        public Vector2 PointTo;
        public Color ColorFrom;
        public Color ColorTo;
        public Matrix? Projection;

        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugDrawLine2D; } }
    }
}
