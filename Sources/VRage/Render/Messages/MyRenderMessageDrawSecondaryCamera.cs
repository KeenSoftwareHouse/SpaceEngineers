using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageDrawSecondaryCamera : IMyRenderMessage
    {
        public Matrix ViewMatrix;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.Draw; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.DrawSecondaryCamera; } }
    }
}
