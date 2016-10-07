using VRageMath;

namespace VRageRender.Messages
{
    public class MyRenderMessageDrawSprite : MySpriteDrawRenderMessage
    {
        public string Texture;
        public Color Color;
        public Rectangle? SourceRectangle;
        public RectangleF DestinationRectangle;
        public Vector2 Origin;
        public float Rotation;
        public Vector2 RightVector;
        public float Depth;
        public SpriteEffects Effects;
        public bool ScaleDestination;
        public bool WaitTillLoaded;

        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DrawSprite; } }
    }
}
