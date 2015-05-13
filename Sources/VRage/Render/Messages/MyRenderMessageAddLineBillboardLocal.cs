using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageAddLineBillboardLocal : IMyRenderMessage
    {
        public uint RenderObjectID;
        public string Material;
        public Color Color;
        public Vector3 LocalPos;
        public Vector3 LocalDir;
        public float Length;
        public float Thickness;
        public int Priority;
        public bool Near;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.Draw; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.AddLineBillboardLocal; } }
    }
}
