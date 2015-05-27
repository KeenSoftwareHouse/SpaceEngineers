using System.ComponentModel;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders.VRageData;


namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Gyro : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember, DefaultValue(1)]
        public float GyroPower = 1f;

        [ProtoMember, DefaultValue(false)]
        public bool GyroOverride = false;

        [ProtoMember]
        public SerializableVector3 TargetAngularVelocity = new SerializableVector3(0.0f, 0.0f, 0.0f);
        public bool ShouldSerializeTargetAngularVelocity() { return !TargetAngularVelocity.IsZero; }
    }
}
