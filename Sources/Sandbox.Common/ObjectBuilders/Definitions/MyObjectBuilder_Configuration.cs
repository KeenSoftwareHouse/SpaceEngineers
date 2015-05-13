using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Configuration : MyObjectBuilder_Base
    {
        [ProtoMember(1)]
        public CubeSizeSettings CubeSizes;

        [ProtoMember(2)]
        public BaseBlockSettings BaseBlockPrefabs;

        [ProtoMember(3)]
        public BaseBlockSettings BaseBlockPrefabsSurvival;

        [ProtoContract]
        public struct CubeSizeSettings
        {
            [ProtoMember(1), XmlAttribute]
            public float Large;

            [ProtoMember(2), XmlAttribute]
            public float Small;
        }

        [ProtoContract]
        public struct BaseBlockSettings
        {
            [ProtoMember(1), XmlAttribute]
            public string SmallStatic;

            [ProtoMember(2), XmlAttribute]
            public string LargeStatic;

            [ProtoMember(3), XmlAttribute]
            public string SmallDynamic;

            [ProtoMember(4), XmlAttribute]
            public string LargeDynamic;
        }
    }
}
