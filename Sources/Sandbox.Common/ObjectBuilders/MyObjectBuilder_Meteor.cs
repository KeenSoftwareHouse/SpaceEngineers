using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Meteor : MyObjectBuilder_EntityBase
    {
        [ProtoMember(1)]
        public MyObjectBuilder_InventoryItem Item;

        [ProtoMember(2)]
        public Vector3 LinearVelocity;

        [ProtoMember(3)]
        public Vector3 AngularVelocity;

        [ProtoMember(4)]
        public float Integrity = 100;
    }
}
