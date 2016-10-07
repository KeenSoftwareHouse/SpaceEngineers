using ObjectBuilders;
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
    public class MyObjectBuilder_MotorBase : MyObjectBuilder_MechanicalConnectionBlock
    {
        //Obsolete! @MyObjectBuilder_MechanicalConnectionBlock.TopBlockId
        [ProtoMember]
        public long? RotorEntityId = null;

        //Obsolete! @MyObjectBuilder_MechanicalConnectionBlock.IsWelded
        [ProtoMember]
        public long? WeldedEntityId = null;

        public override void Remap(IMyRemapHelper remapHelper)
        {
            base.Remap(remapHelper);
            if (RotorEntityId.HasValue && RotorEntityId != 0) RotorEntityId = remapHelper.RemapEntityId(RotorEntityId.Value);
            if (WeldedEntityId.HasValue && WeldedEntityId != 0) WeldedEntityId = remapHelper.RemapEntityId(WeldedEntityId.Value);
        }
    }
}
