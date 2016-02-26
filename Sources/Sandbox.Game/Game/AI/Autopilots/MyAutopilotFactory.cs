using Sandbox.Common.ObjectBuilders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Sandbox.Common;
using VRage.ObjectBuilders;
using VRage.Game.Common;

namespace Sandbox.Game.AI
{
    internal class MyAutopilotTypeAttribute : MyFactoryTagAttribute
    {
        public MyAutopilotTypeAttribute(Type objectBuilderType)
            : base(objectBuilderType)
        {
        }
    }

    internal static class MyAutopilotFactory
    {
        private static MyObjectFactory<MyAutopilotTypeAttribute, MyAutopilotBase> m_objectFactory;

        static MyAutopilotFactory()
        {
            m_objectFactory = new MyObjectFactory<MyAutopilotTypeAttribute, MyAutopilotBase>();
            m_objectFactory.RegisterFromCreatedObjectAssembly();
        }

        public static MyAutopilotBase CreateAutopilot(MyObjectBuilder_AutopilotBase builder)
        {
            return m_objectFactory.CreateInstance(builder.TypeId);
        }

        public static MyObjectBuilder_AutopilotBase CreateObjectBuilder(MyAutopilotBase autopilot)
        {
            return m_objectFactory.CreateObjectBuilder<MyObjectBuilder_AutopilotBase>(autopilot);
        }

    }
}
