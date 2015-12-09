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
using VRage.ObjectBuilders;

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
            m_objectFactory.RegisterFromAssembly(MyPlugins.SandboxAssembly); //TODO: Will be removed 
            m_objectFactory.RegisterFromAssembly(MyPlugins.UserAssembly);
        }

        public static object CreateCubeBlock(MyObjectBuilder_CubeBlock builder)
        {
            var obj = m_objectFactory.CreateInstance(builder.TypeId);
            var entity = obj as MyEntity; // Some are SlimBlocks
            if (entity != null)
            {
                MyEntityFactory.AddScriptGameLogic(entity, builder.TypeId, builder.SubtypeName);
                MyEntities.RaiseEntityCreated(entity);
            }
            return obj;
        }

        public static MyObjectBuilder_CubeBlock CreateObjectBuilder(MyCubeBlock cubeBlock)
        {
            MyObjectBuilder_CubeBlock objectBuilder = (MyObjectBuilder_CubeBlock)MyObjectBuilderSerializer.CreateNewObject(cubeBlock.BlockDefinition.Id);

            return objectBuilder;
        }

        public static Type GetProducedType(MyObjectBuilderType objectBuilderType)
        {
            return m_objectFactory.GetProducedType(objectBuilderType);
        }
    }
}
