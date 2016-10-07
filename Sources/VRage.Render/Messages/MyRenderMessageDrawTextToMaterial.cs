using VRageMath;

namespace VRageRender.Messages
{
    public class MyRenderMessageDrawTextToMaterial : MyRenderMessageBase
    {
        public uint RenderObjectID;
        public string Text;
        public float TextScale;
        public string MaterialName;
        public Color FontColor;
        public Color BackgroundColor;
        public int TextureResolution;
        public int TextureAspectRatio;
        public long EntityId;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DrawTextToMaterial; } }
    }
}
