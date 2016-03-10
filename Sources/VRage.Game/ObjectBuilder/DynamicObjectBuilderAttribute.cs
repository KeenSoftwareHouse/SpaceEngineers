using VRage.Serialization;

namespace VRage.ObjectBuilders
{
    public class DynamicObjectBuilderAttribute : DynamicAttribute
    {
        public DynamicObjectBuilderAttribute(bool defaultTypeCommon = false)
            : base(typeof(MyObjectBuilderDynamicSerializer), defaultTypeCommon)
        {
        }
    }

    public class DynamicObjectBuilderItemAttribute : DynamicItemAttribute
    {
        public DynamicObjectBuilderItemAttribute(bool defaultTypeCommon = false)
            : base(typeof(MyObjectBuilderDynamicSerializer), defaultTypeCommon)
        {
        }
    }

    public class DynamicNullableObjectBuilderItemAttribute : DynamicNullableItemAttribute
    {
        public DynamicNullableObjectBuilderItemAttribute(bool defaultTypeCommon = false)
            : base(typeof(MyObjectBuilderDynamicSerializer), defaultTypeCommon)
        {
        }
    }
}
