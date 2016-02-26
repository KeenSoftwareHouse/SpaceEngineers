using ProtoBuf;
using System.Collections.Generic;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game
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
            [ProtoMember]
            public string Type;

            [XmlAttribute]
            [ProtoMember]
            public MyControllerSchemaEnum Control;
        }

        [ProtoContract]
        public class ControlGroup
        {
            [ProtoMember]
            public string Type;

            [ProtoMember]
            public string Name;

            [ProtoMember]
            public List<ControlDef> ControlDefs;
        }

        [ProtoContract]
        public class CompatibleDevice
        {
            [ProtoMember]
            public string DeviceId;
        }

        [ProtoContract]
        public class Schema
        {
            [ProtoMember]
            public string SchemaName;

            [ProtoMember]
            public List<ControlGroup> ControlGroups;
        }

        [XmlArrayItem("DeviceId")]
        [ProtoMember]
        public List<string> CompatibleDeviceIds;

        [ProtoMember]
        public List<Schema> Schemas;
    }
}