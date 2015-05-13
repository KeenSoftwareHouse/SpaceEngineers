using System.Collections.Generic;
using ProtoBuf;
using VRageMath;
using System.Xml.Serialization;
using System.ComponentModel;
using Sandbox.Common.ObjectBuilders.VRageData;


namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GravityGeneratorSphere : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember(2)]
        public float Radius = 150f;

        [ProtoMember(3), DefaultValue(9.81f)]
        public float GravityAcceleration = 9.81f;
    }
}
