using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageCreateDecal : IMyRenderMessage
    {
        public uint ID;
        public MyDecalTriangle_Data Triangle;
        public int TrianglesToAdd;
        public MyDecalTexturesEnum Texture;
        public Vector3 Position;
        public float LightSize;
        public float Emissivity;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.CreateDecal; } }
    }

    public class MyRenderMessageCreateScreenDecal : IMyRenderMessage
    {
        public uint ID;
        public uint ParentID;
        public Matrix LocalOBB; // transforms unit box centered at 0 to volume relative to object space
        public string DecalMaterial;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.CreateScreenDecal; } }
    }

    public class MyRenderMessageRemoveDecal : IMyRenderMessage
    {
        public uint ID;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.RemoveDecal; } }
    }

    public enum MyScreenDecalType
    {
        ScreenDecalBump, // affects normalmap on whole surface
        ScreenDecalColor // affects color and metallness on alphatested surface
    }

    public struct MyDecalMaterialDesc
    {
        public MyScreenDecalType DecalType;
        public string NormalmapTexture;
        public string ColorMetalTexture;
        public string AlphamaskTexture;
    }

    public class MyRenderMessageRegisterScreenDecalsMaterials : IMyRenderMessage
    {
        public List<string> MaterialsNames;
        public List<MyDecalMaterialDesc> MaterialsDescriptions;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.RegisterDecalsMaterials; } }
    }
}
