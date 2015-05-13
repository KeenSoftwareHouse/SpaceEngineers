using VRageMath;

namespace VRageRender
{
    public struct MySkeletonBoneDescription
    {
        public Matrix SkinTransform;
        public int Parent;
    }

    public class MyRenderMessageSetCharacterSkeleton : IMyRenderMessage
    {
        public uint CharacterID;
        public MySkeletonBoneDescription[] SkeletonBones;
        public int[] SkeletonIndices;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.SetCharacterSkeleton; } }
    }

    public class MyRenderMessageCreateRenderCharacter : IMyRenderMessage
    {
        public uint ID;
        public string DebugName;
        public string Model;
        public string LOD1;
        public MatrixD WorldMatrix;
        public RenderFlags Flags;
        public Color? DiffuseColor;
        public Vector3? ColorMaskHSV;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.CreateRenderCharacter; } }
    }
}
