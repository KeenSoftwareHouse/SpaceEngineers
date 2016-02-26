using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageUpdateCockpitGlass : MyRenderMessageBase
    {
        public bool Visible;
        public string Model;
        public MatrixD WorldMatrix;
        public float DirtAlpha;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeEvery; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateCockpitGlass; } }
    }
}
