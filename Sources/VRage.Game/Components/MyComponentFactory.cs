using Sandbox.Common.ObjectBuilders.ComponentSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage.FileSystem;
using VRage.ObjectBuilders;
using VRage.Plugins;

namespace VRage.Components
{
    public class MyComponentBuilderAttribute : MyFactoryTagAttribute
    {
        public MyComponentBuilderAttribute(Type objectBuilderType, bool mainBuilder = true)
            : base(objectBuilderType, mainBuilder)
        {
        }
    }

    [PreloadRequired]
    public static class MyComponentFactory
    {
        private static MyObjectFactory<MyComponentBuilderAttribute, MyComponentBase> m_objectFactory;

        static MyComponentFactory()
        {
            m_objectFactory = new MyObjectFactory<MyComponentBuilderAttribute, MyComponentBase>();
            m_objectFactory.RegisterFromAssembly(MyPlugins.GameAssembly);
            m_objectFactory.RegisterFromAssembly(MyPlugins.SandboxGameAssembly);
            m_objectFactory.RegisterFromAssembly(MyPlugins.UserAssembly);
        }

        public static MyComponentBase CreateInstance(MyObjectBuilderType type)
        {
            return m_objectFactory.CreateInstance(type);
        }

        public static MyObjectBuilder_ComponentBase CreateObjectBuilder(MyComponentBase instance)
        {
            var objectBuilder = m_objectFactory.CreateObjectBuilder<MyObjectBuilder_ComponentBase>(instance);

            //if (objectBuilder == null)
            //{
            //    var baseType = instance.GetType().BaseType;
            //    while (baseType != null && baseType != typeof(object) && objectBuilder == null)
            //    {
            //        objectBuilder = m_objectFactory.CreateObjectBuilder<MyObjectBuilder_ComponentBase>(baseType);
            //        baseType = baseType.BaseType;
            //    }
            //}

            return objectBuilder;
        }
    }
}
