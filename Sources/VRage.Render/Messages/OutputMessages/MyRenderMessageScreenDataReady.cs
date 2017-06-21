using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageScreenDataReady : MyRenderMessageBase
    {
        public int Id;
        public ImageFileFormat Format;
        public byte[] ScreenData;

        public override void Close()
        {
            Id = 0;
            ScreenData = null;

            base.Close();
        }

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.ScreenDataReady; } }
    }
}