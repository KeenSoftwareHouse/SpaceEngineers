
namespace VRageRender
{
    public class MyRenderMessagePlayVideo : IMyRenderMessage
    {
        public uint ID;
        public string VideoFile;
        public float Volume;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.PlayVideo; } }
    }
}
