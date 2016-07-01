using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace VRageRender
{
    [Obsolete]
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
        public MyDecalTopoData Data;
        public MyDecalFlags Flags;
        public string SourceTarget;
        public string Material;
        public int MaterialIndex;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.CreateScreenDecal; } }
    }

    public struct MyDecalTopoData
    {
        public Vector3D Position;
        public Vector3 Normal;
        public Vector3 Scale;
        public float Rotation;
    }

    public class MyRenderMessageUpdateScreenDecal : MyRenderMessageBase
    {
        public List<MyDecalPositionUpdate> Decals = new List<MyDecalPositionUpdate>();

        public override void Init()
        {
            Decals.Clear();
        }

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeEvery; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateScreenDecal; } }
    }

    public struct MyDecalPositionUpdate
    {
        public uint ID;
        public Vector3D Position;
        public Vector3 Normal;
    }

    public class MyRenderMessageRemoveDecal : MyRenderMessageBase
    {
        public uint ID;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.RemoveDecal; } }
    }

    public class MyRenderMessageRegisterScreenDecalsMaterials : MyRenderMessageBase
    {
        public Dictionary<string, List<MyDecalMaterialDesc>> MaterialDescriptions;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.RegisterDecalsMaterials; } }
    }


    public class MyRenderMessageClearScreenDecals : MyRenderMessageBase
    {
        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.ClearDecals; } }
    }

    public class MyRenderMessageSetDecalGlobals : MyRenderMessageBase
    {
        public MyDecalGlobals Globals;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.SetDecalGlobals; } }
    }
}
