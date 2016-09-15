using VRageMath;

namespace VRageRender.Messages
{
    public class MyRenderMessageDebugDrawCapsule : MyDebugRenderMessage
    {
        public Vector3D P0;
        public Vector3D P1;
        public float Radius;
        public Color Color;
        public bool DepthRead;
        public bool Shaded;

        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugDrawCapsule; } }
    }
}
