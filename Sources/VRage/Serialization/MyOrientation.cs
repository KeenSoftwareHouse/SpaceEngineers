using VRageMath;
using System.Runtime.Serialization;
using System.Reflection;
using System;
using System.Net;
using System.Xml.Serialization;
using ProtoBuf;

namespace VRage
{
    [ProtoContract]
    public struct MyOrientation
    {
        [ProtoMember, XmlAttribute]
        public float Yaw;

        [ProtoMember, XmlAttribute]
        public float Pitch;
        
        [ProtoMember, XmlAttribute]
        public float Roll;

        public MyOrientation(float yaw, float pitch, float roll)
        {
            Yaw = yaw;
            Pitch = pitch;
            Roll = roll;
        }
    }

}
