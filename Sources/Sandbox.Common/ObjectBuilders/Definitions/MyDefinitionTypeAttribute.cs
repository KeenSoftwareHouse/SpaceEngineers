#region Using

using Sandbox.Common;
using System;
using VRage.ObjectBuilders;


#endregion

namespace Sandbox.Definitions
{
    public class MyDefinitionTypeAttribute : MyFactoryTagAttribute
    {
        public MyDefinitionTypeAttribute(Type objectBuilderType)
            : base(objectBuilderType)
        {
        }
    }
}
