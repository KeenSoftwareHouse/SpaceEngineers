using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage.Game;
using VRage.Plugins;
using VRage.ObjectBuilders;
using VRage.Game.Common;

namespace Sandbox.Game.AI.BehaviorTree
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MyBehaviorTreeNodeMemoryTypeAttribute : MyFactoryTagAttribute
    {
        public MyBehaviorTreeNodeMemoryTypeAttribute(Type objectBuilderType)
            : base(objectBuilderType)
        {
        }
    }

    internal static class MyBehaviorTreeNodeMemoryFactory
    {
        private static MyObjectFactory<MyBehaviorTreeNodeMemoryTypeAttribute, MyBehaviorTreeNodeMemory> m_objectFactory;

        static MyBehaviorTreeNodeMemoryFactory()
        {
            m_objectFactory = new MyObjectFactory<MyBehaviorTreeNodeMemoryTypeAttribute, MyBehaviorTreeNodeMemory>();
            m_objectFactory.RegisterFromAssembly(Assembly.GetAssembly(typeof(MyBehaviorTreeNodeMemory)));

            m_objectFactory.RegisterFromAssembly(MyPlugins.GameAssembly);
            m_objectFactory.RegisterFromAssembly(MyPlugins.SandboxAssembly); //TODO: Will be removed 
            m_objectFactory.RegisterFromAssembly(MyPlugins.UserAssembly);
        }

        public static MyBehaviorTreeNodeMemory CreateNodeMemory(MyObjectBuilder_BehaviorTreeNodeMemory builder)
        {
            var obj = m_objectFactory.CreateInstance(builder.TypeId);
            return obj;
        }

        public static MyObjectBuilder_BehaviorTreeNodeMemory CreateObjectBuilder(MyBehaviorTreeNodeMemory cubeBlock)
        {
            return m_objectFactory.CreateObjectBuilder<MyObjectBuilder_BehaviorTreeNodeMemory>(cubeBlock);
        }

        public static Type GetProducedType(MyObjectBuilderType objectBuilderType)
        {
            return m_objectFactory.GetProducedType(objectBuilderType);
        }

    }
}
