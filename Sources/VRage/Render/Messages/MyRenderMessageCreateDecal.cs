using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageCreateDecal : IMyRenderMessage
    {
        public uint ID;
        public MyDecalTriangle_Data Triangle;
        public int TrianglesToAdd;
        public MyDecalTexturesEnum Texture;
        public Vector3 Position;
        public float LightSize;
        public float Emissivity;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.CreateDecal; } }
    }
}
