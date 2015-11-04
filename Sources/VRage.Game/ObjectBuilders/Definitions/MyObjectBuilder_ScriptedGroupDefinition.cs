using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ScriptedGroupDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        public string Category;

        [ProtoMember]
        public string Script;
    }
}
