using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageUpdateCockpitGlass : IMyRenderMessage
    {
        public bool Visible;
        public string Model;
        public MatrixD WorldMatrix;
        public float DirtAlpha;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeEvery; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.UpdateCockpitGlass; } }
    }
}
