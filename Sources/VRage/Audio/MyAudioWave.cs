using ProtoBuf;
using System;
using System.ComponentModel;
using System.Xml.Serialization;
using VRage.Data;
using VRage.Data.Audio;

namespace VRage
{
    [ProtoContract, XmlType("DistantSound")]
    public sealed class DistantSound
    {
        [ProtoMember, XmlAttribute]
        public float distance = 50f;

        [ProtoMember, XmlAttribute]
        public float distanceCrossfade = -1f;

        [ProtoMember, XmlAttribute]
        public String sound = "";
    };

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
