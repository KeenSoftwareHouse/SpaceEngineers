using System;
using VRage.Game.Common;

namespace VRage.Game.Entity
{
    public class MyEntityTypeAttribute : MyFactoryTagAttribute
    {
        public MyEntityTypeAttribute(Type objectBuilderType, bool mainBuilder = true)
            : base(objectBuilderType, mainBuilder)
        {
        }
    }
}
