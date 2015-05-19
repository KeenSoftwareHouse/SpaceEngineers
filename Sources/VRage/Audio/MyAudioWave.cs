using ProtoBuf;
using System;
using System.ComponentModel;
using System.Xml.Serialization;
using VRage.Data;
using VRage.Data.Audio;

namespace VRage
{
    [ProtoContract, XmlType("Wave")]
    public sealed class MyAudioWave
    {
        [ProtoMember, XmlAttribute]
        public MySoundDimensions Type;

        [ProtoMember, DefaultValue(""), ModdableContentFile("xwm")]
        public String Start;

        [ProtoMember, DefaultValue(""), ModdableContentFile("xwm")]
        public String Loop;

        [ProtoMember, DefaultValue(""), ModdableContentFile("xwm")]
        public String End;
    };
}
