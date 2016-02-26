using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        [ProtoMember]
        public float Velocity = -0.1f;

        [ProtoMember]
        public float? MaxLimit;

        [ProtoMember]
        public float? MinLimit;

        [ProtoMember]
        public bool Reverse;

        [ProtoMember]
        public long TopBlockId;

        [ProtoMember]
        public float CurrentPosition;

        [ProtoMember]
        public float weldSpeed = 95f;

        [ProtoMember]
        public bool forceWeld = false;

        public override void Remap(IMyRemapHelper remapHelper)
        {
            base.Remap(remapHelper);
            if (TopBlockId != 0) TopBlockId = remapHelper.RemapEntityId(TopBlockId);
        }
    }
}
