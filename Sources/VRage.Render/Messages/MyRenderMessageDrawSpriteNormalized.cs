using VRage.Utils;
using VRageMath;

namespace VRageRender.Messages
{
    public class MyRenderMessageDrawSpriteNormalized : MySpriteDrawRenderMessage
    {
        public string Texture;
        public Vector2 NormalizedCoord;
        public Vector2 NormalizedSize;
        public Color Color;
        public MyGuiDrawAlignEnum DrawAlign;
        public float Rotation;
        public Vector2 RightVector;
        public float Scale;
        public Vector2? OriginNormalized;
        public float RotationSpeed; // Rad/s
        public bool WaitTillLoaded;

        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DrawSpriteNormalized; } }
    }
}
