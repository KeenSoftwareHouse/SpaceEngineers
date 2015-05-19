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
        [ProtoMember]
        public MyObjectBuilder_InventoryItem Item;

        [ProtoMember]
        public Vector3 LinearVelocity;

        [ProtoMember]
        public Vector3 AngularVelocity;

        [ProtoMember]
        public float Integrity = 100;
    }
}
