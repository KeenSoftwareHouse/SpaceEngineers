using VRageMath;

namespace VRageRender.Messages
{
    public enum MyVideoRectangleFitMode
    {
        None, // Video is drawn to the rectangle with no changes.
        FitWidth, // Ensures that video fills the rectangle horizontally. Vertically it is either cut off or padded with empty borders.
        FitHeight, // Ensures that video fills the rectangle vertically. Horizontally it is either cut off or padded with empty borders.
        AutoFit, // Ensures that video always fills the rectangle. How to fit is determined using video and rectangle aspect ratios.
    }

    public class MyRenderMessageDrawVideo : MyRenderMessageBase
    {
        public uint ID;
        public Rectangle Rectangle;
        public Color Color;
        public MyVideoRectangleFitMode FitMode;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.Draw; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.DrawVideo; } }
    }
}
