using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
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
    public class MyObjectBuilder_MechanicalConnectionBlock: MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public MyPositionAndOrientation? MasterToSlaveTransform;

        [XmlElement(ElementName = "weldSpeed")]
        public float WeldSpeed = 95f;

        [XmlElement(ElementName = "forceWeld")]
        public bool ForceWeld = false;

        [ProtoMember]
        public long? TopBlockId = null;

        [ProtoMember]
        public bool IsWelded = false;

        public override void Remap(IMyRemapHelper remapHelper)
        {
            base.Remap(remapHelper);
            if (TopBlockId.HasValue && TopBlockId != 0) TopBlockId = remapHelper.RemapEntityId(TopBlockId.Value);
        }
    }
}
