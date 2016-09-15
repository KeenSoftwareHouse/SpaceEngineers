using System.Collections.Generic;
using VRageMath;

namespace VRageRender.Messages
{
    public class MyRenderMessageSetCharacterTransforms : MyRenderMessageBase
    {
        public uint CharacterID;
        public Matrix[] BoneAbsoluteTransforms;
        public List<MyBoneDecalUpdate> BoneDecalUpdates;

        public MyRenderMessageSetCharacterTransforms()
        {
            BoneDecalUpdates = new List<MyBoneDecalUpdate>();
        }

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeEvery; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.SetCharacterTransforms; } }

        public override void Init()
        {
            BoneDecalUpdates.Clear();
        }

        public override void Close()
        {
            base.Close();

            CharacterID = uint.MaxValue;
        }
    }

    public struct MyBoneDecalUpdate
    {
        public int BoneID;
        public uint DecalID;
    }
}
