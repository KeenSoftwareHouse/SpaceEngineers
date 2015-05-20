using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AgentDefinition : MyObjectBuilder_BotDefinition
    { // used for humanoids
        [ProtoMember]
        public string BotModel = "";

        // Obsolete!
        // [ProtoMember]
        // public string DeathSoundName = "";
    }
}
