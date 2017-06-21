using VRageMath;

namespace VRageRender.Messages
{
    public struct MyRenderFogSettings
    {
        public float FogMultiplier;
        public Color FogColor;
        public float FogDensity;
    }

    public class MyRenderMessageUpdateFogSettings : MyRenderMessageBase
    {
        public MyRenderFogSettings Settings;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateFogSettings; } }
    }
}
