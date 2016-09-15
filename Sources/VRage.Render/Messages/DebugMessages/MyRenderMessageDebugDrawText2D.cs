using VRage.Utils;
using VRageMath;

namespace VRageRender.Messages
{
    public class MyRenderMessageDebugDrawText2D : MyDebugRenderMessage
    {
        public Vector2 Coord;
        public string Text;
        public Color Color;
        public float Scale;
        public MyGuiDrawAlignEnum Align;

        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DebugDrawText2D; } }
    }
}
