using System;
using System.Collections.Generic;

namespace VRageRender.Messages
{
    /* 
     * Material slot uses same semantics as texture params in MyMaterialDescriptor:
     * "ColorMetalTexture"
     * "NormalGlossTexture"
     * "AddMapsTexture"
     * "AlphamaskTexture"
     * if MaterialSlot == null: will be treated as "ColorMetalTexture" (please don't mix explicit and implicit slot naming for same object)
     * in dx9 renderer it is used only to change diffuse texture, so MaterialSlot can be null
     */

    public struct MyTextureChange
    {
        public MyTextureType TextureType;
        public string TextureName;
    }

    public class MyRenderMessageChangeMaterialTexture : MyRenderMessageBase
    {
        public uint RenderObjectID;
        public string MaterialName;
        public List<MyTextureChange> Changes;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.ChangeMaterialTexture; } }
    }

    [Flags]
    public enum MyTextureType
    {
        Unspecified = 0,
        ColorMetal = 1,
        NormalGloss = 2,
        Extensions = 4,
        Alphamask = 8,
    }

}
