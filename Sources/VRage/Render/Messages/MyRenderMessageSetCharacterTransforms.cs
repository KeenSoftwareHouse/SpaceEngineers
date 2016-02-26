using System.Collections.Generic;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageSetCharacterTransforms : MyRenderMessageBase
    {
        public uint CharacterID;
        public Matrix[] RelativeBoneTransforms;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeEvery; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.SetCharacterTransforms; } }

        public override void Close()
        {
            base.Close();

            CharacterID = uint.MaxValue;
        }
    }
}
