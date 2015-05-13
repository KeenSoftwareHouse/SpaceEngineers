#region Using

using Sandbox.Common;
using System;


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
