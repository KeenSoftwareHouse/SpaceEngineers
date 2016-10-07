using VRage.Utils;
using VRageMath;

namespace VRageRender.Messages
{
    public class MyRenderMessageDebugDrawText3D : MyDebugRenderMessage
    {
        public Vector3D Coord;
        public string Text;
        public Color Color;
        public float Scale;
        public bool DepthRead;
        public float? ClipDistance;
        public MyGuiDrawAlignEnum Align;
        public int CustomViewProjection;

        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugDrawText3D; } }
    }
}
