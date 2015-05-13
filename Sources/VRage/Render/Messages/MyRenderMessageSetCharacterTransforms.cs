using System.Collections.Generic;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageSetCharacterTransforms : IMyRenderMessage
    {
        public uint CharacterID;
        public Matrix[] RelativeBoneTransforms;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeEvery; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.SetCharacterTransforms; } }
    }
}
