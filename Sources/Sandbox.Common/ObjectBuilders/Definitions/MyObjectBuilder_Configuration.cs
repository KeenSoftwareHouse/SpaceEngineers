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
        [ProtoMember]
        public CubeSizeSettings CubeSizes;

        [ProtoMember]
        public BaseBlockSettings BaseBlockPrefabs;

        [ProtoMember]
        public BaseBlockSettings BaseBlockPrefabsSurvival;

        [ProtoContract]
        public struct CubeSizeSettings
        {
            [ProtoMember, XmlAttribute]
            public float Large;

            [ProtoMember, XmlAttribute]
            public float Small;
        }

        [ProtoContract]
        public struct BaseBlockSettings
        {
            [ProtoMember, XmlAttribute]
            public string SmallStatic;

            [ProtoMember, XmlAttribute]
            public string LargeStatic;

            [ProtoMember, XmlAttribute]
            public string SmallDynamic;

            [ProtoMember, XmlAttribute]
            public string LargeDynamic;
        }
    }
}
