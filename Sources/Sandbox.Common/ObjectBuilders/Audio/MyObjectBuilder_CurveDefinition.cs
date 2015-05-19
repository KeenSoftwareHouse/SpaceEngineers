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
            [ProtoMember]
            public float Time;
            [ProtoMember]
            public float Value;
        }

         [ProtoMember]
         public List<Point> Points;
    }
}
