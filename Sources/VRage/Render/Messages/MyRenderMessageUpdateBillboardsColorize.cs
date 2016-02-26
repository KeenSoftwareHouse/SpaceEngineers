using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
   public class MyRenderMessageUpdateBillboardsColorize : MyRenderMessageBase
    {
        public bool Enable;
        public Color Color;
        public float Distance;
        public Vector3 Normal;

       public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
       public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateBillboardsColorize; } }
    }
}
