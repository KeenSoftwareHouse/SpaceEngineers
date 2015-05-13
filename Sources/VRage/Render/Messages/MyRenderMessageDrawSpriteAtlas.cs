using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageDrawSpriteAtlas : IMyRenderMessage
    {
        public string Texture;
        public Vector2 Position;
        public Vector2 TextureOffset;
        public Vector2 TextureSize;
        public Vector2 RightVector;
        public Vector2 Scale;
        public Color Color;
        public Vector2 HalfSize;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.Draw; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.DrawSpriteAtlas; } }
    }
}
