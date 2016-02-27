using ProtoBuf;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_FactionDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        [XmlAttribute]
        public string Tag;

        [ProtoMember]
        [XmlAttribute]
        public string Name;

        [ProtoMember]
        [XmlAttribute]
        public string Founder;

        [ProtoMember]
        public bool AcceptHumans = false;

        [ProtoMember]
        public bool AutoAcceptMember = true;

        [ProtoMember]
        public bool EnableFriendlyFire = false;

        /// <summary>
        /// This value indicates if fraction should be created by default for every new world and its owner
        /// will be visible in Ownership dropdown.
        /// </summary>
        [ProtoMember]
        public bool IsDefault = false;

        /// <summary>
        /// Default faction relation to the other factions. 
        /// Enemies state is with highest prority and does not care if other faction want to be friend.
        /// </summary>
        [ProtoMember]
        public MyRelationsBetweenFactions DefaultRelation = MyRelationsBetweenFactions.Enemies;
    }
}
