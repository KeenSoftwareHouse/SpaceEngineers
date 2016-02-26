using System;

namespace VRage.Game.Common
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class MyFactoryTagAttribute : System.Attribute
    {
        public readonly Type ObjectBuilderType;
        public Type ProducedType;
        public bool IsMain;

        public MyFactoryTagAttribute(Type objectBuilderType, bool mainBuilder = true)
        {
            ObjectBuilderType = objectBuilderType;
            IsMain = mainBuilder;
        }
    }
}
