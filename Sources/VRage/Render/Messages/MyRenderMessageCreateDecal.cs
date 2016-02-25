using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageCreateDecal : MyRenderMessageBase
    {
        public uint ID;
        public MyDecalTriangle_Data Triangle;
        public int TrianglesToAdd;
        public MyDecalTexturesEnum Texture;
        public Vector3 Position;
        public float LightSize;
        public float Emissivity;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.CreateDecal; } }
    }

    public class MyRenderMessageCreateScreenDecal : MyRenderMessageBase
    {
        public uint ID;
        public uint ParentID;
        public Matrix LocalOBB; // transforms unit box centered at 0 to volume relative to object space
        public string DecalMaterial;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.CreateScreenDecal; } }
    }

    public class MyRenderMessageRemoveDecal : MyRenderMessageBase
    {
        public uint ID;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.RemoveDecal; } }
    }

    public enum MyScreenDecalType
    {
        ScreenDecalBump, // affects normalmap on whole surface
        ScreenDecalColor // affects color and metallness on alphatested surface
    }

    [ProtoContract]
    public struct MyDecalMaterialDesc
    {
        [ProtoMember]
        public MyScreenDecalType DecalType;
        [ProtoMember]
        public string NormalmapTexture;
        [ProtoMember]
        public string ColorMetalTexture;
        [ProtoMember]
        public string AlphamaskTexture;
    }

    public class MyRenderMessageRegisterScreenDecalsMaterials : MyRenderMessageBase
    {
        public List<string> MaterialsNames;
        public List<MyDecalMaterialDesc> MaterialsDescriptions;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.RegisterDecalsMaterials; } }
    }
}
