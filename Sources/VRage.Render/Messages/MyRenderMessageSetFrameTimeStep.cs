namespace VRageRender.Messages
{
    public class MyRenderMessageSetFrameTimeStep : MyRenderMessageBase
    {
        /// <summary>In milliseconds</summary>
        public uint TimeStep;

        public override MyRenderMessageType MessageClass
        {
            get { return MyRenderMessageType.StateChangeOnce; }
        }

        public override MyRenderMessageEnum MessageType
        {
            get { return MyRenderMessageEnum.SetFrameTimeStep; }
        }
    }
}
