﻿using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using VRage.ObjectBuilders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using VRage;

namespace Medieval.ObjectBuilders.Blocks
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_RopeHookBlock : MyObjectBuilder_CubeBlock
    {
        [ProtoContract]
        public struct ReleaseData
        {
            [ProtoMember]
            [XmlAttribute]
            public float Threshold;

            [ProtoMember]
            public SerializableVector2 Orientation;
        }

        [ProtoMember]
        public ReleaseData? Release;
        public bool ShouldSerializeRelease() { return Release.HasValue; }

    }
}
