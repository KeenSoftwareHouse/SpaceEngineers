using ProtoBuf;
using System.Collections.Generic;
using VRage.ObjectBuilders;

namespace VRage.Game
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
