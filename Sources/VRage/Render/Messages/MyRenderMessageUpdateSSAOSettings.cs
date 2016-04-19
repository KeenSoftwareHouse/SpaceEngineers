namespace VRageRender
{
    public class MyRenderMessageUpdateSSAOSettings : MyRenderMessageBase
    {
        public bool Enabled;
        public bool ShowOnlySSAO;
        public bool UseBlur;

        public float MinRadius;
        public float MaxRadius;
        public float RadiusGrowZScale;
        public float CameraZFar;

        public float Bias;
        public float Falloff;
        public float NormValue;
        public float Contrast;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateSSAOSettings; } }
    }
}
