using ProtoBuf;
using System.Text;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders
{
    [ProtoContract]
	[MyObjectBuilderDefinition]
    public class MyObjectBuilder_AnimationSMCondition : MyObjectBuilder_Base
	{
        public enum MyOperationType
        {
            AlwaysFalse,
            AlwaysTrue,
            NotEqual,
            Less,
            LessOrEqual,
            Equal,
            GreaterOrEqual,
            Greater
        }

        // If numeric, it will be converted to constant float value.
        // Otherwise, it is a variable having this name.
        // Left side of comparison.
        [ProtoMember]
        [XmlAttribute("Lhs")]
        public string ValueLeft = null;
        
        // Condition - operation type.
        [ProtoMember]
        [XmlAttribute("Op")]
        public MyOperationType Operation = MyOperationType.AlwaysFalse;
        
        // If numeric, it will be converted to constant float value.
        // Otherwise, it is a variable having this name.
        // Right side of comparison.
        [ProtoMember]
        [XmlAttribute("Rhs")]
        public string ValueRight = null;

        // Implementation of ToString - for better debugging in VS. :)
        public override string ToString()
        {
            if (Operation == MyOperationType.AlwaysTrue)
                return "true";
            if (Operation == MyOperationType.AlwaysFalse)
                return "false";

            StringBuilder strBuilder = new StringBuilder(128);
            // fetch values
            strBuilder.Append(ValueLeft);

            strBuilder.Append(" ");
            switch (Operation)
            {
                case MyOperationType.Less:
                    strBuilder.Append("<");
                    break;
                case MyOperationType.LessOrEqual:
                    strBuilder.Append("<=");
                    break;
                case MyOperationType.Equal:
                    strBuilder.Append("==");
                    break;
                case MyOperationType.GreaterOrEqual:
                    strBuilder.Append(">=");
                    break;
                case MyOperationType.Greater:
                    strBuilder.Append(">");
                    break;
                case MyOperationType.NotEqual:
                    strBuilder.Append("!=");
                    break;
                default:
                    strBuilder.Append("???");
                    break;
            }
            strBuilder.Append(" ");
            strBuilder.Append(ValueRight);
            return strBuilder.ToString();
        }
	}

    /// <summary>
    /// Conjunction of several simple conditions. This conjunction is true if all contained conditions are true.
    /// </summary>
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AnimationSMConditionsConjunction : MyObjectBuilder_Base
    {
        [ProtoMember]
        [XmlElement("Condition")]
        public MyObjectBuilder_AnimationSMCondition[] Conditions;
    }
}
