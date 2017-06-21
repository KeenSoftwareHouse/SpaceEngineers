using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;

namespace VRage.Game.AI
{
    public enum MyBehaviorTreeActionType
    {
        INIT,
        BODY,
        POST
    }

    [AttributeUsage(AttributeTargets.Parameter, Inherited = true)]
    public class BTInAttribute : BTMemParamAttribute
    {
        public override MyMemoryParameterType MemoryType
        {
            get { return MyMemoryParameterType.IN; }
        }
    }

    [AttributeUsage(AttributeTargets.Parameter, Inherited = true)]
    public class BTOutAttribute : BTMemParamAttribute
    {
        public override MyMemoryParameterType MemoryType
        {
            get { return MyMemoryParameterType.OUT; }
        }
    }

    [AttributeUsage(AttributeTargets.Parameter, Inherited = true)]
    public class BTInOutAttribute : BTMemParamAttribute
    {
        public override MyMemoryParameterType MemoryType
        {
            get { return MyMemoryParameterType.IN_OUT; }
        }
    }

    [AttributeUsage(AttributeTargets.Parameter, Inherited = true)]
    public abstract class BTMemParamAttribute : Attribute
    {
        public abstract MyMemoryParameterType MemoryType { get; }
    }

    [AttributeUsage(AttributeTargets.Parameter, Inherited = true)]
    public class BTParamAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class MyBehaviorTreeActionAttribute : Attribute
    {
        public readonly string ActionName;
        public readonly MyBehaviorTreeActionType ActionType;
        public bool ReturnsRunning;

        public MyBehaviorTreeActionAttribute(string actionName) :
            this(actionName, MyBehaviorTreeActionType.BODY)
        {
        }

        public MyBehaviorTreeActionAttribute(string actionName, MyBehaviorTreeActionType type)
        {
            this.ActionName = actionName;
            this.ActionType = type;
            ReturnsRunning = true;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class MyBehaviorDescriptorAttribute : Attribute
    {
        public readonly string DescriptorCategory;

        public MyBehaviorDescriptorAttribute(string category)
        {
            DescriptorCategory = category;
        }
    }
}
