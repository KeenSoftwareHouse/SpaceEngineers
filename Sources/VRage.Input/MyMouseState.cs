using System.Reflection;

namespace VRage.Input
{
    [Obfuscation(Feature = Obfuscator.NoRename, Exclude = true)]
    public struct MyMouseState
    {
        public int X;
        public int Y;
        public int ScrollWheelValue;

        public bool LeftButton;
        public bool RightButton;
        public bool MiddleButton;
        public bool XButton1;
        public bool XButton2;

        public MyMouseState(int x, int y, int scrollWheel, bool leftButton, bool middleButton, bool rightButton, bool xButton1, bool xButton2)
        {
            X = x;
            Y = y;
            ScrollWheelValue = scrollWheel;
            LeftButton = leftButton;
            MiddleButton = middleButton;
            RightButton = rightButton;
            XButton1 = xButton1;
            XButton2 = xButton2;
        }

        public void ClearPosition()
        {
            X = 0;
            Y = 0;
            //ScrollWheelValue = 0;
        }
    }
}
