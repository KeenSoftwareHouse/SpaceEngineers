using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
}
