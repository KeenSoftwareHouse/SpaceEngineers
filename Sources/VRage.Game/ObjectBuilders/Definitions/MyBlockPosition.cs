using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    public class MyBlockPosition
    {
        [ProtoMember]
        public string Name;

        [ProtoMember]
        public Vector2I Position;
    }
}
