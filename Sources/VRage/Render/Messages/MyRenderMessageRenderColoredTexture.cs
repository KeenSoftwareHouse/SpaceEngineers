using System;
using System.Collections.Generic;

namespace VRageRender
{
    public struct renderColoredTextureProperties
    {
        public string TextureName;
        public string PathToSave;
        public VRageMath.Vector3 ColorMaskHSV;
    }
    public class MyRenderMessageRenderColoredTexture : IMyRenderMessage
    {
        public List<renderColoredTextureProperties> texturesToRender = null;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.RenderColoredTexture; } }
    }
}
