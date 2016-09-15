using VRageMath;

namespace VRageRender.Messages
{
    public class MyRenderMessageDebugDraw6FaceConvex : MyDebugRenderMessage
    {
        /* Vertices of the 6-faced convex shape. */
        /* vertext order is: tlb trb tlf trf bfb brb blf brf */
        public Vector3D[] Vertices;
        public Color Color;
        public float Alpha;
        public bool DepthRead;
        public bool Fill;

        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugDraw6FaceConvex; } }
    }
}
