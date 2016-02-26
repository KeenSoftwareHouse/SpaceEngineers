using ProtoBuf;
using VRage;
using VRage.Game;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_MotorBase : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public long? RotorEntityId;

        [ProtoMember]
        public long? WeldedEntityId;

        [ProtoMember]
        public float weldSpeed = 95f;

        [ProtoMember]
        public bool forceWeld = false;

        [ProtoMember]
        public MyPositionAndOrientation? MasterToSlaveTransform;

        public override void Remap(IMyRemapHelper remapHelper)
        {
            base.Remap(remapHelper);
            if (RotorEntityId.HasValue && RotorEntityId != 0) RotorEntityId = remapHelper.RemapEntityId(RotorEntityId.Value);
            if (WeldedEntityId.HasValue && WeldedEntityId != 0) WeldedEntityId = remapHelper.RemapEntityId(WeldedEntityId.Value);
        }
    }
}
