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
        [ProtoMember(1), XmlAttribute]
        public MySoundDimensions Type;

        [ProtoMember(2), DefaultValue(""), ModdableContentFile("xwm")]
        public String Start;

        [ProtoMember(3), DefaultValue(""), ModdableContentFile("xwm")]
        public String Loop;

        [ProtoMember(4), DefaultValue(""), ModdableContentFile("xwm")]
        public String End;
    };
}
