using VRageMath;

namespace VRageRender.Messages
{
    public struct MySkeletonBoneDescription
    {
        public Matrix SkinTransform;
        public int Parent;
    }

    public class MyRenderMessageSetCharacterSkeleton : MyRenderMessageBase
    {
        public uint CharacterID;
        public MySkeletonBoneDescription[] SkeletonBones;
        public int[] SkeletonIndices;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.SetCharacterSkeleton; } }
    }

    public class MyRenderMessageCreateRenderCharacter : MyRenderMessageBase
    {
        public uint ID;
        public string DebugName;
        public string Model;
        public string LOD1;
        public MatrixD WorldMatrix;
        public RenderFlags Flags;
        public Color? DiffuseColor;
        public Vector3? ColorMaskHSV;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.CreateRenderCharacter; } }
    }
}
