#region Using

using System;
using VRage.Game.Common;

#endregion

namespace VRage.Game.Definitions
{
    public class MyDefinitionTypeAttribute : MyFactoryTagAttribute
    {
        public readonly Type PostProcessor;

        public MyDefinitionTypeAttribute(Type objectBuilderType, Type postProcessor = null)
            : base(objectBuilderType)
        {
            if (postProcessor == null) postProcessor = typeof(NullDefinitionPostprocessor);
            else if (!typeof(MyDefinitionPostprocessor).IsAssignableFrom(postProcessor))
                throw new ArgumentException("postProcessor processor must be a subclass of MyDefinitionPostprocessor.", "postProcessor");

            PostProcessor = postProcessor;
        }
    }
}
