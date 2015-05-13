using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ProtoBuf;
using VRage;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BlueprintDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember(1)]
        [XmlArrayItem("Item")]
        public BlueprintItem[] Prerequisites;

        /// <summary>
        /// THIS IS OBSOLETE
        /// </summary>
        [ProtoMember(3)]
        public BlueprintItem Result;

        [ProtoMember(4)]
        [XmlArrayItem("Item")]
        public BlueprintItem[] Results;

        /// <summary>
        /// Base production time in seconds, which is affected by speed increase of
        /// refinery or assembler.
        /// </summary>
        [ProtoMember(5)]
        public float BaseProductionTimeInSeconds = 1.0f;

    }
}
