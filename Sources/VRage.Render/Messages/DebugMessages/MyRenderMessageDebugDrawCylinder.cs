using VRageMath;

namespace VRageRender.Messages
{
    public class MyRenderMessageDebugDrawCylinder : MyDebugRenderMessage
    {
        public MatrixD Matrix;
        public Color Color;
        public float Alpha;
        public bool DepthRead;
        public bool Smooth;

        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugDrawCylinder; } }
    }

    public class MyRenderMessageDebugDrawCone : MyDebugRenderMessage
    {
        public Vector3D Translation;
        public Vector3D DirectionVector;
        public Vector3D BaseVector;
        public Color Color;
        public bool DepthRead;

        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugDrawCone; } }
    }
}
