using System.Collections.Generic;
using ProtoBuf;
using VRage.ObjectBuilders;
using VRage;
using System.ComponentModel;


namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GravityGenerator : MyObjectBuilder_FunctionalBlock
    {
        //[ProtoMember, DefaultValue(true)]
        //public bool Enabled = true;

        [ProtoMember]
        public SerializableVector3 FieldSize = new SerializableVector3(150f, 150f, 150f);

        [ProtoMember, DefaultValue(9.81f)]
        public float GravityAcceleration = 9.81f;


    }
}
