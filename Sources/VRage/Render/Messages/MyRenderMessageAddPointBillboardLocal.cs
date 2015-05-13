using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageAddPointBillboardLocal : IMyRenderMessage
    {
        public uint RenderObjectID;
        public string Material;
        public Color Color;
        public Vector3 LocalPos;
        public float Radius;
        public float Angle;
        public int Priority;
        public bool Colorize;
        public bool Near;
        public bool Lowres;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.Draw; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.AddPointBillboardLocal; } }
    }
}
