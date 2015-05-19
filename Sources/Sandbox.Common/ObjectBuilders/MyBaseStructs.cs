using VRageMath;
using System.Runtime.Serialization;
using System.Reflection;
using System;
using System.Net;
using System.Xml.Serialization;
using Sandbox.Common.ObjectBuilders.VRageData;
using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders
{
    //  Must be struct because we don't want to provocate garbage collector when received in MW game-client 
    [ProtoContract]
    public struct MyPositionAndOrientation
    {
        [ProtoMember]
        [XmlElement("Position")]
        public SerializableVector3D Position;    //  Position within sector
        
        [ProtoMember]
        [XmlElement("Forward")]
        public SerializableVector3 Forward;     //  Forward vector (for orientation)
        
        [ProtoMember]
        [XmlElement("Up")]
        public SerializableVector3 Up;          //  Up vector (for orientation)

        public static readonly MyPositionAndOrientation Default = new MyPositionAndOrientation(Vector3.Zero, Vector3.Forward, Vector3.Up);

        public MyPositionAndOrientation(Vector3D position, Vector3 forward, Vector3 up)
        {
            Position = position;
            Forward = forward;
            Up = up;
        }

        // Optimized version
        public MyPositionAndOrientation(ref MatrixD matrix)
        {
            Position = matrix.Translation;
            Forward = (Vector3)matrix.Forward;
            Up = (Vector3)matrix.Up;
        }

        public MyPositionAndOrientation(MatrixD matrix)
            : this(matrix.Translation, matrix.Forward, matrix.Up)
        {

        }

        public MatrixD GetMatrix()
        {
            return MatrixD.CreateWorld(Position, Forward, Up);
        }

        public override string ToString()
        {
            return Position.ToString() + "; " + Forward.ToString() + "; " + Up.ToString();
        }

    }

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
