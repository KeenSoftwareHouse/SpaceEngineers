using System;
using VRageMath;

namespace VRageRender.Messages
{
    public class MyRenderMessageRenderOffscreenTextureToMaterial : MyRenderMessageBase
    {
        public uint RenderObjectID;
        public string OffscreenTexture;
        public string MaterialName;
        public MyTextureType TextureType;
        public Color? BackgroundColor;
        public bool BlendAlphaChannel;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.RenderOffscreenTextureToMaterial; } }
    }
}
