using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders.Audio
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CurveDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoContract]
        public struct Point
        {
            [ProtoMember(1)]
            public float Time;
            [ProtoMember(2)]
            public float Value;
        }

         [ProtoMember(1)]
         public List<Point> Points;
    }
}
