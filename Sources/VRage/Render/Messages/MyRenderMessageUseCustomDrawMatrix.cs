using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageUseCustomDrawMatrix : MyRenderMessageBase
    {
        public uint ID;
        public MatrixD DrawMatrix;
        public bool Enable;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UseCustomDrawMatrix; } }
    }
}
