using VRageMath;

namespace VRageRender.Messages
{
    public class MyRenderMessageDebugDrawLine3D : MyDebugRenderMessage
    {
        public Vector3D PointFrom;
        public Vector3D PointTo;
        public Color ColorFrom;
        public Color ColorTo;
        public bool DepthRead;

        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugDrawLine3D; } }
    }

    public class MyRenderMessageDebugDrawPoint : MyDebugRenderMessage
    {
        public Vector3D Position;
        public Color Color;
        public bool DepthRead;
        public float? ClipDistance;

        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugDrawPoint; } }
    }
}
