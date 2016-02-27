using VRage.ObjectBuilders;
using ProtoBuf;
using System.Xml.Serialization;
using VRage.Game;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_MedicalRoomDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember]
        public string ResourceSinkGroup;

        [ProtoMember]
        public string IdleSound;

        [ProtoMember]
        public string ProgressSound;

        [ProtoMember]
        public bool RespawnAllowed = true;

        [ProtoMember]
        public bool HealingAllowed = true;

        [ProtoMember]
        public bool RefuelAllowed = true;

        [ProtoMember]
        public bool SuitChangeAllowed = true;

        [ProtoMember]
        public bool CustomWardrobesEnabled = false;

        [ProtoMember]
        public bool ForceSuitChangeOnRespawn = false;

        [ProtoMember]
        public bool SpawnWithoutOxygenEnabled = true;

        [ProtoMember]
        public string RespawnSuitName = null;

        [ProtoMember]
        [XmlArrayItem("Name")]
        public string[] CustomWardRobeNames = null;
    }
}
