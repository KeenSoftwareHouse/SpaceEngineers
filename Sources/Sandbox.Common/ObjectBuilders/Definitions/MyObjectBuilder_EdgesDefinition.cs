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
        [ProtoMember(1)]
        [ModdableContentFile("mwm")]
        public string Vertical;

        [ProtoMember(2)]
        [ModdableContentFile("mwm")]
        public string VerticalDiagonal;

        [ProtoMember(3)]
        [ModdableContentFile("mwm")]
        public string Horisontal;

        [ProtoMember(4)]
        [ModdableContentFile("mwm")]
        public string HorisontalDiagonal;
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_EdgesDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember(1)]
        public MyEdgesModelSet Small;

        [ProtoMember(2)]
        public MyEdgesModelSet Large;
    }
}
