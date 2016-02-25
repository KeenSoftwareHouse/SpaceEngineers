using VRage.ObjectBuilders;
using ProtoBuf;
using System.Xml.Serialization;
using System.ComponentModel;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BlueprintDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        [XmlArrayItem("Item")]
        public BlueprintItem[] Prerequisites;

        /// <summary>
        /// THIS IS OBSOLETE
        /// </summary>
        [ProtoMember]
        public BlueprintItem Result;

        [ProtoMember]
        [XmlArrayItem("Item")]
        public BlueprintItem[] Results;

        /// <summary>
        /// Base production time in seconds, which is affected by speed increase of
        /// refinery or assembler.
        /// </summary>
        [ProtoMember]
        public float BaseProductionTimeInSeconds = 1.0f;
        
        [ProtoMember, DefaultValue(null)]
        public string ProgressBarSoundCue = null;
    }
}
