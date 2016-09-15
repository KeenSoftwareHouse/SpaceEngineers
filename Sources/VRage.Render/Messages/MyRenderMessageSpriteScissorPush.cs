using VRageMath;

namespace VRageRender.Messages
{
    public class MyRenderMessageSpriteScissorPush : MySpriteDrawRenderMessage
    {
        public Rectangle ScreenRectangle; // Defined in screen coordinates.

        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.SpriteScissorPush; } }
    }
}
