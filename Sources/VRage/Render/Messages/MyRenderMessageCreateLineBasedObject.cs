using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageCreateLineBasedObject : IMyRenderMessage
    {
        public uint ID;
        public string DebugName;

        public MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.CreateLineBasedObject; } }
    }

    public class MyRenderMessageUpdateLineBasedObject : IMyRenderMessage
    {
        public uint ID;
        public Vector3D WorldPointA;
        public Vector3D WorldPointB;

        public MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateLineBasedObject; } }
    }
}
