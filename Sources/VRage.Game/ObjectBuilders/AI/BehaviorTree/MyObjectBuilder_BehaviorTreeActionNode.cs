using ProtoBuf;
using System;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [Flags]
    public enum MyMemoryParameterType : byte
    {
        IN = 1 << 0,
        OUT = 1 << 1,
        IN_OUT = IN | OUT,
        PARAMETER = 1 << 2,
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BehaviorTreeActionNode : MyObjectBuilder_BehaviorTreeNode
    {
        [ProtoContract]
        public abstract class TypeValue
        {
            public abstract object GetValue();
        }

        [ProtoContract]
        public class IntType : TypeValue
        {
            [XmlAttribute]
            [ProtoMember]
            public int IntValue;
            public override object GetValue()
            {
                return IntValue;
            }
        }

        [ProtoContract]
        public class StringType : TypeValue
        {
            [XmlAttribute]
            [ProtoMember]
            public string StringValue;

            public override object GetValue()
            {
                return StringValue;
            }
        }

        [ProtoContract]
        public class FloatType : TypeValue
        {
            [XmlAttribute]
            [ProtoMember]
            public float FloatValue;

            public override object GetValue()
            {
                return FloatValue;
            }
        }

        [ProtoContract]
        public class BoolType : TypeValue
        {
            [XmlAttribute]
            [ProtoMember]
            public bool BoolValue;

            public override object GetValue()
            {
                return BoolValue;
            }
        }

        [ProtoContract]
        public class MemType : TypeValue
        {
            [XmlAttribute]
            [ProtoMember]
            public string MemName;

            public override object GetValue()
            {
                return MemName;
            }
        }

        [ProtoMember]
        public string ActionName = null;

        [ProtoMember]
        [XmlArrayItem("Parameter")]
        public TypeValue[] Parameters = null;
    }
}