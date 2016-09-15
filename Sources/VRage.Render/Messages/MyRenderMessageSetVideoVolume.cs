
namespace VRageRender.Messages
{
    public class MyRenderMessageSetVideoVolume : MyRenderMessageBase
    {
        public uint ID;
        public float Volume;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.SetVideoVolume; } }
    }
}
