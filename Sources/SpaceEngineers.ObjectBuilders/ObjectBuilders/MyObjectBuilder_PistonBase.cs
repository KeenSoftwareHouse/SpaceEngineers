using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using VRage;
using VRage.Game;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_PistonBase : MyObjectBuilder_FunctionalBlock
    {
        public float Velocity = -0.1f;

        public float? MaxLimit;

        public float? MinLimit;

        public bool Reverse;

        public long? TopBlockId;

        public float CurrentPosition;

        [XmlElement(ElementName = "weldSpeed")]
        public float WeldSpeed = 95f;

        [XmlElement(ElementName = "forceWeld")]
        public bool ForceWeld = false;

        public bool IsWelded = false;

        public MyPositionAndOrientation? MasterToSlaveTransform;

        public override void Remap(IMyRemapHelper remapHelper)
        {
            base.Remap(remapHelper);
            if (TopBlockId.HasValue && TopBlockId != 0) TopBlockId = remapHelper.RemapEntityId(TopBlockId.Value);
        }
    }
}
