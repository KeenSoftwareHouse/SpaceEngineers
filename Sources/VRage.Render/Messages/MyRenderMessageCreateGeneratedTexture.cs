using System;
using VRageMath;

namespace VRageRender.Messages
{
    public class MyRenderMessageCreateGeneratedTexture : MyRenderMessageBase
    {
        public string TextureName;
        public int Width;
        public int Height;
        public MyGeneratedTextureType Type;
        public int NumMipLevels;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.CreateGeneratedTexture; } }
    }

    public class MyRenderMessageResetGeneratedTexture : MyRenderMessageBase
    {
        public string TextureName;
        public byte[] Data;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.ResetGeneratedTexture; } }
    }

    public enum MyGeneratedTextureType
    {
        /// <summary>sRGB</summary>
        RGBA,
        /// <summary>Linear RGB</summary>
        RGBA_Linear,
        Alphamask
    }
}
