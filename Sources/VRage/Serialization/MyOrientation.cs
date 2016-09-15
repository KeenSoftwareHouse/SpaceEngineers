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

        public Quaternion ToQuaternion()
        {
            return Quaternion.CreateFromYawPitchRoll(Yaw, Pitch, Roll);
        }

        public override bool Equals(object obj)
        {
            MyOrientation other;
            if (obj is MyOrientation)
                other = (MyOrientation)obj;
            else
                return false;

            return this == other;
        }

        public override int GetHashCode()
        {
            int hash = (int)(Yaw * 997);
            hash = (hash * 397) ^ (int)(Pitch * 997);
            hash = (hash * 397) ^ (int)(Roll * 997);
            return hash;
        }

        public static bool operator ==(MyOrientation value1, MyOrientation value2)
        {
            if (value1.Yaw == value2.Yaw && value1.Pitch == value2.Pitch && value1.Roll == value2.Roll)
                return true;
            else
                return false;
        }

        public static bool operator !=(MyOrientation value1, MyOrientation value2)
        {
            if (value1.Yaw != value2.Yaw || value1.Pitch != value2.Pitch || value1.Roll != value2.Roll)
                return true;
            else
                return false;
        }
    }

}
