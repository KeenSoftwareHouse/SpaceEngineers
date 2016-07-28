using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage.Game;
using VRage.Plugins;
using VRage.ObjectBuilders;
using VRage.Game.Common;
#if XB1 // XB1_ALLINONEASSEMBLY
using VRage.Utils;
#endif // XB1

namespace Sandbox.Game.AI.BehaviorTree
{
    public class MyBehaviorTreeNodeTypeAttribute : MyFactoryTagAttribute
    {
        public readonly Type MemoryType;

        public MyBehaviorTreeNodeTypeAttribute(Type objectBuilderType)
            : base(objectBuilderType)
        {
            MemoryType = typeof(MyBehaviorTreeNodeMemory);
        }

        public MyBehaviorTreeNodeTypeAttribute(Type objectBuilderType, Type memoryType)
            : base(objectBuilderType)
        {
            MemoryType = memoryType;
        }

    }

    internal static class MyBehaviorTreeNodeFactory
    {
        private static MyObjectFactory<MyBehaviorTreeNodeTypeAttribute, MyBehaviorTreeNode> m_objectFactory;

        static MyBehaviorTreeNodeFactory()
        {
            m_objectFactory = new MyObjectFactory<MyBehaviorTreeNodeTypeAttribute, MyBehaviorTreeNode>();
#if XB1 // XB1_ALLINONEASSEMBLY
            m_objectFactory.RegisterFromAssembly(MyAssembly.AllInOneAssembly);
#else // !XB1
            m_objectFactory.RegisterFromAssembly(Assembly.GetAssembly(typeof(MyBehaviorTreeNode)));

            m_objectFactory.RegisterFromAssembly(MyPlugins.GameAssembly);
            m_objectFactory.RegisterFromAssembly(MyPlugins.SandboxAssembly); //TODO: Will be removed 
            m_objectFactory.RegisterFromAssembly(MyPlugins.UserAssembly);
#endif // !XB1
        }

        public static MyBehaviorTreeNode CreateBTNode(MyObjectBuilder_BehaviorTreeNode builder)
        {
            var obj = m_objectFactory.CreateInstance(builder.TypeId);
            return obj;
        }

        public static Type GetProducedType(MyObjectBuilderType objectBuilderType)
        {
            return m_objectFactory.GetProducedType(objectBuilderType);
        }

    }
}
