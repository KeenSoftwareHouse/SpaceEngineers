using ProtoBuf;
using System.ComponentModel;
using System.Xml.Serialization;
using VRage.Game.ObjectBuilders.VisualScripting;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CutsceneSessionComponent : MyObjectBuilder_SessionComponent
    {
        [ProtoMember]
        [XmlArrayItem("Cutscene")]
        public Cutscene[] Cutscenes;
    }

    [ProtoContract]
    public class Cutscene
    {
        [ProtoMember]
        public string Name = "";

        [ProtoMember]
        public string StartEntity = "";

        [ProtoMember]
        public string StartLookAt = "";

        [ProtoMember]
        public string NextCutscene = "";

        [ProtoMember]
        public float StartingFOV = 70;

        [ProtoMember]
        [XmlArrayItem("Node")]
        public CutsceneSequenceNode[] SequenceNodes;
    }

    [ProtoContract]
    public class CutsceneSequenceNode
    {
        [ProtoMember]
        [XmlAttribute]
        public float Time = 0;

        [ProtoMember]
        [XmlAttribute]
        public string LookAt;

        [ProtoMember]
        [XmlAttribute]
        public string Event;

        [ProtoMember]
        [XmlAttribute]
        public float EventDelay = 0;

        [ProtoMember]
        [XmlAttribute]
        public string LockRotationTo;

        [ProtoMember]
        [XmlAttribute]
        public string AttachTo;

        [ProtoMember]
        [XmlAttribute]
        public string AttachPositionTo;

        [ProtoMember]
        [XmlAttribute]
        public string AttachRotationTo;

        [ProtoMember]
        [XmlAttribute]
        public string MoveTo;

        [ProtoMember]
        [XmlAttribute]
        public string SetPositionTo;

        [ProtoMember]
        [XmlAttribute]
        public float ChangeFOVTo = 0;

        [ProtoMember]
        [XmlAttribute]
        public string RotateTowards;

        [ProtoMember]
        [XmlAttribute]
        public string SetRorationLike;

        [ProtoMember]
        [XmlAttribute]
        public string RotateLike;

        [ProtoMember]
        [XmlArrayItem("Waypoint")]
        public CutsceneSequenceNodeWaypoint[] Waypoints;
    }

    [ProtoContract]
    public class CutsceneSequenceNodeWaypoint
    {
        [ProtoMember]
        [XmlAttribute]
        public string Name = "";

        [ProtoMember]
        [XmlAttribute]
        public float Time = 0f;
    }
}