using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ModAPI;
using VRage.Utils;

namespace VRage.Components
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MyEntityComponentDescriptor : System.Attribute
    {
        public Type EntityBuilderType;
        public string[] EntityBuilderSubTypeNames;

        public MyEntityComponentDescriptor(Type entityBuilderType, params string[] entityBuilderSubTypeNames)
        {
            EntityBuilderType = entityBuilderType;
            EntityBuilderSubTypeNames = entityBuilderSubTypeNames;
        }
    }
   
    public abstract class MyEntityComponentBase : MyComponentBase<MyEntityComponentContainer>
    {
        public IMyEntity Entity { get { return Container != null ? Container.Entity : null; } }  
    }

}
