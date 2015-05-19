using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using VRage.Data;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    public class MyEdgesModelSet
    {
        [ProtoMember]
        [ModdableContentFile("mwm")]
        public string Vertical;

        [ProtoMember]
        [ModdableContentFile("mwm")]
        public string VerticalDiagonal;

        [ProtoMember]
        [ModdableContentFile("mwm")]
        public string Horisontal;

        [ProtoMember]
        [ModdableContentFile("mwm")]
        public string HorisontalDiagonal;
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_EdgesDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        public MyEdgesModelSet Small;

        [ProtoMember]
        public MyEdgesModelSet Large;
    }
}
