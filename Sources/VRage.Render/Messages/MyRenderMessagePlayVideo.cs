
namespace VRageRender.Messages
{
    public class MyRenderMessagePlayVideo : MyRenderMessageBase
    {
        public uint ID;
        public string VideoFile;
        public float Volume;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.PlayVideo; } }
    }
}
