using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Common.ModAPI
{
    [ProtoContract]
    public struct MyHitInfo
    {
        [ProtoMember]
        public Vector3D Position;

        [ProtoMember]
        public Vector3D Normal;

        [ProtoMember]
        public Vector3D Velocity; //of impacting entity/bulet, normalize to get direction
    }
}
