using System.Collections.Generic;

namespace VRageRender.Messages
{
    public struct renderColoredTextureProperties
    {
        public string TextureName;
        public string PathToSave;
        public VRageMath.Vector3 ColorMaskHSV;
    }
    public class MyRenderMessageRenderColoredTexture : MyRenderMessageBase
    {
        public List<renderColoredTextureProperties> texturesToRender = null;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.RenderColoredTexture; } }
    }
}
