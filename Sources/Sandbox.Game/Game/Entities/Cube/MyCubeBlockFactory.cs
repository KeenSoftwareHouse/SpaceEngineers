using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.Components;
using VRage.Plugins;

namespace Sandbox.Game.Entities.Cube
{
    public class MyCubeBlockTypeAttribute : MyFactoryTagAttribute
    {
        public MyCubeBlockTypeAttribute(Type objectBuilderType)
            : base(objectBuilderType)
        {
        }
    }

    internal static class MyCubeBlockFactory
    {
        // Factory uses object as base type, otherwise we can't create both MyCubeBlock and MySlimBlock.
        private static MyObjectFactory<MyCubeBlockTypeAttribute, object> m_objectFactory;

        static MyCubeBlockFactory()
        {
            m_objectFactory = new MyObjectFactory<MyCubeBlockTypeAttribute, object>();
            m_objectFactory.RegisterFromAssembly(Assembly.GetAssembly(typeof(MyCubeBlock)));

            m_objectFactory.RegisterFromAssembly(MyPlugins.GameAssembly);
            m_objectFactory.RegisterFromAssembly(MyPlugins.UserAssembly);
        }

        public static object CreateCubeBlock(MyObjectBuilder_CubeBlock builder)
        {
            var obj = m_objectFactory.CreateInstance(builder.TypeId);
            MyEntityFactory.AddScriptGameLogic(obj as MyEntity, builder.TypeId, builder.SubtypeName);
            return obj;
        }

        public static MyObjectBuilder_CubeBlock CreateObjectBuilder(MyCubeBlock cubeBlock)
        {
            MyObjectBuilder_CubeBlock objectBuilder = (MyObjectBuilder_CubeBlock)Sandbox.Common.ObjectBuilders.Serializer.MyObjectBuilderSerializer.CreateNewObject(cubeBlock.BlockDefinition.Id);

            return objectBuilder;
        }

        public static Type GetProducedType(MyObjectBuilderType objectBuilderType)
        {
            return m_objectFactory.GetProducedType(objectBuilderType);
        }
    }
}
