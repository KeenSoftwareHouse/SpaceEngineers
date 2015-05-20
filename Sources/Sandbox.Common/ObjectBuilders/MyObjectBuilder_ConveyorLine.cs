using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Conveyors;
using Sandbox.Common.ObjectBuilders.VRageData;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders
{    
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ConveyorLine : MyObjectBuilder_Base
    {
        public enum LineType
        {
            DEFAULT_LINE, // Backwards compatibility
            SMALL_LINE,
            LARGE_LINE,
        }

        public enum LineConductivity
        {
            FULL,
            FORWARD,
            BACKWARD,
            NONE,
        }

        [ProtoMember]
        public SerializableVector3I StartPosition;

        [ProtoMember]
        public Base6Directions.Direction StartDirection;

        [ProtoMember]
        public SerializableVector3I EndPosition;

        [ProtoMember]
        public Base6Directions.Direction EndDirection;

        [ProtoMember]
        public List<MyObjectBuilder_ConveyorPacket> PacketsForward = new List<MyObjectBuilder_ConveyorPacket>();
        public bool ShouldSerializePacketsForward() { return PacketsForward.Count != 0; }

        [ProtoMember]
        public List<MyObjectBuilder_ConveyorPacket> PacketsBackward = new List<MyObjectBuilder_ConveyorPacket>();
        public bool ShouldSerializePacketsBackward() { return PacketsBackward.Count != 0; }

        [ProtoMember, DefaultValue(null)]
        [XmlArrayItem("Section")]
        public List<SerializableLineSectionInformation> Sections = null;
        public bool ShouldSerializeSections() { return Sections != null; }

        [ProtoMember, DefaultValue(LineType.DEFAULT_LINE)]
        public LineType ConveyorLineType = LineType.DEFAULT_LINE;

        [ProtoMember, DefaultValue(LineConductivity.FULL)]
        public LineConductivity ConveyorLineConductivity = LineConductivity.FULL;
    }
}
