using VRageMath;

namespace VRageRender.Messages
{
    public class MyRenderMessageDebugDrawSphere : MyDebugRenderMessage
    {
        public Vector3D Position;
        public float Radius;
        public Color Color;
        public float Alpha;
        public float ? ClipDistance;
        public bool DepthRead;
        public bool Smooth;
        public bool Cull;

        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugDrawSphere; } }
    }
}
