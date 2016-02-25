using VRage.Game.ObjectBuilders.ComponentSystem;
using System;
using VRage.ObjectBuilders;
using VRage.Plugins;
using VRage.Game.Common;

namespace VRage.Game.Components
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

        public static MyComponentBase CreateInstanceByTypeId(MyObjectBuilderType type)
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

        public static MyComponentBase CreateInstanceByType(Type type)
        {
            if (type.IsAssignableFrom(typeof(MyComponentBase)))
            {
                return Activator.CreateInstance(type) as MyComponentBase;
            }
            return null;
        }

        public static Type GetCreatedInstanceType(MyObjectBuilderType type)
        {
            return m_objectFactory.GetProducedType(type);
        }

    }
}
