using Sandbox.Game.AI.Logic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Sandbox.Game.AI
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class BehaviorActionImplAttribute : Attribute
    {
        public readonly Type LogicType;

        public BehaviorActionImplAttribute(Type logicType)
        {
            Debug.Assert(logicType.IsSubclassOf(typeof(MyBotLogic)), "Invalid logic type");
            LogicType = logicType;
        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class BehaviorLogicAttribute : Attribute
    {
        public readonly string BehaviorSubtype;

        public BehaviorLogicAttribute(string behaviorSubtype)
        {
            BehaviorSubtype = behaviorSubtype;
        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class TargetTypeAttribute : Attribute
    {
        public readonly string TargetType;

        public TargetTypeAttribute(string targetType)
        {
            TargetType = targetType;
        }
    }
}
