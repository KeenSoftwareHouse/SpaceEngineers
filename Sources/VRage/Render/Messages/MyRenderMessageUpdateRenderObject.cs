using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageUpdateRenderObject : MyRenderMessageBase
    {
        public uint ID;
        public MatrixD WorldMatrix;
        public bool SortIntoCulling;
        public BoundingBoxD? AABB;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeEvery; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateRenderObject; } }

        public override void Close()
        {
            AABB = null;
            base.Close();
        }
    }
}
