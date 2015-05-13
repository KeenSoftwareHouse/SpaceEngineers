using VRage;
using VRageMath;
using VRageRender.Graphics;

namespace VRageRender
{
    public class MyRenderMessageDrawSecondaryCameraSprite : IMyRenderMessage
    {
        public Color Color;
        public Rectangle? SourceRectangle;
        public RectangleF DestinationRectangle;
        public Vector2 Origin;
        public float Rotation;
        public Vector2 RightVector;
        public float Depth;
        public SpriteEffects Effects;
        public bool ScaleDestination;
        public DrawSpriteStencilEnum Stencil;
        public int StencilLevel;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.Draw; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.DrawSecondaryCameraSprite; } }
    }
}
