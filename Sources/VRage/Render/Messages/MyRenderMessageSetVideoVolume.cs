
namespace VRageRender
{
    public class MyRenderMessageSetVideoVolume : IMyRenderMessage
    {
        public uint ID;
        public float Volume;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.SetVideoVolume; } }
    }
}
