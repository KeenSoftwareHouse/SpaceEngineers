using VRageMath;

namespace VRageRender.Messages
{
    public class MyRenderMessageDebugDrawTriangle : MyDebugRenderMessage
    {
        public Vector3D Vertex0;
        public Vector3D Vertex1;
        public Vector3D Vertex2;
        public Color Color;
        public bool DepthRead;
        public bool Smooth;

        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugDrawTriangle; } }
    }
}
