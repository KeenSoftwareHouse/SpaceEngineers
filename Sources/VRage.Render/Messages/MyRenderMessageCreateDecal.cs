using System.Collections.Generic;
using VRageMath;

namespace VRageRender.Messages
{
    public class MyRenderMessageCreateScreenDecal : MyRenderMessageBase
    {
        public uint ID;
        public uint ParentID;
        public MyDecalTopoData TopoData;
        public MyDecalFlags Flags;
        public string SourceTarget;
        public string Material;
        public int MaterialIndex;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.CreateScreenDecal; } }
    }

    public struct MyDecalTopoData
    {
        public Matrix MatrixBinding;
        public Vector3D WorldPosition;
        public Matrix MatrixCurrent;
        public Vector4UByte BoneIndices;
        public Vector4 BoneWeights;
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
        public Matrix Transform;
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
