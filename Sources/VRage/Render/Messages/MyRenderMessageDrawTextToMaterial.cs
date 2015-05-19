using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageDrawTextToMaterial : IMyRenderMessage
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
        public int FontIndex;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.DrawTextToMaterial; } }
    }
}
