using VRageMath;

namespace VRageRender
{
    public enum ImageFileFormat
    {
        Bmp,
        Png,
        Jpg
    }
    public class MyRenderMessageRequestScreenData : MyRenderMessageBase
    {
        public int Id;
        public byte[] ScreenData;
        public ImageFileFormat Format;

        public override void Close()
        {
            Id = 0;
            ScreenData = null;

            base.Close();
        }

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.RequestScreenData; } }
    }
}