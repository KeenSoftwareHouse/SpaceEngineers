using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    public enum MyControllerSchemaEnum : byte
    {
        Analog,
        RightAnalog,
        AxisZ,
        Button1,
        Button2,
        Button3,
        Button4,
        Button5,
        Button6,
        Button7,
        Button8,
        Button9,
        Button10,
        Button11,
        Button12,
        Dpad,
        DpadLeft,
        DpadRight,
        DpadUp,
        DpadDown,
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ControllerSchemaDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoContract]
        public class ControlDef
        {
            [XmlAttribute]
            [ProtoMember(1)]
            public string Type;

            [XmlAttribute]
            [ProtoMember(2)]
            public MyControllerSchemaEnum Control;
        }

        [ProtoContract]
        public class ControlGroup
        {
            [ProtoMember(1)]
            public string Type;

            [ProtoMember(2)]
            public string Name;

            [ProtoMember(3)]
            public List<ControlDef> ControlDefs;
        }

        [ProtoContract]
        public class CompatibleDevice
        {
            [ProtoMember(1)]
            public string DeviceId;
        }

        [ProtoContract]
        public class Schema
        {
            [ProtoMember(1)]
            public string SchemaName;

            [ProtoMember(2)]
            public List<ControlGroup> ControlGroups;
        }

        [XmlArrayItem("DeviceId")]
        [ProtoMember(1)]
        public List<string> CompatibleDeviceIds;

        [ProtoMember(2)]
        public List<Schema> Schemas;
    }
}
