namespace VRageRender.Messages
{
    /// <summary>
    /// The difference between environment and RenderSettings is that environment are game play values,
    /// on the other hand render settings are render internal/debugging values
    /// </summary>
    public class MyRenderMessageUpdateRenderEnvironment : MyRenderMessageBase
    {
        public MyEnvironmentData Data;
        public bool ResetEyeAdaptation;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateRenderEnvironment; } }
    }
}
