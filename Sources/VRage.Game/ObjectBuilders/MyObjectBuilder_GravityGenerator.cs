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
        public SerializableVector3 FieldSize = SerializableVector3.NotInitialized;

        [ProtoMember]
        public float GravityAcceleration = float.NaN; // NaN doesn't work in DefaultValue attribute for whatever reason, don't bother.
    }
}
