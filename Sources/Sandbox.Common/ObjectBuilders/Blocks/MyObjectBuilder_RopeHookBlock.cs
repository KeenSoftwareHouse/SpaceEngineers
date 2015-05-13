using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.VRageData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Medieval.ObjectBuilders.Blocks
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_RopeHookBlock : MyObjectBuilder_CubeBlock
    {
        [ProtoContract]
        public struct ReleaseData
        {
            [ProtoMember(1)]
            [XmlAttribute]
            public float Threshold;

            [ProtoMember(2)]
            public SerializableVector2 Orientation;
        }

        [ProtoMember(1)]
        public ReleaseData? Release;
        public bool ShouldSerializeRelease() { return Release.HasValue; }

    }
}
