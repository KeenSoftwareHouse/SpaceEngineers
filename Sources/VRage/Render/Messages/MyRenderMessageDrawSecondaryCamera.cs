using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageDrawSecondaryCamera : MyRenderMessageBase
    {
        public Matrix ViewMatrix;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.Draw; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DrawSecondaryCamera; } }
    }
}
