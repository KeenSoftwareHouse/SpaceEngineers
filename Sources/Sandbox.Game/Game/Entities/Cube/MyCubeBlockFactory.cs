using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
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
            MyEntity entity = obj as MyEntity;
            var scriptManager = Sandbox.Game.World.MyScriptManager.Static;

			if (scriptManager != null && builder.SubtypeName != null && scriptManager.SubEntityScripts.ContainsKey(new Tuple<Type, string>(builder.TypeId, builder.SubtypeName)))
				entity.GameLogic = (Sandbox.Common.Components.MyGameLogicComponent)Activator.CreateInstance(scriptManager.SubEntityScripts[new Tuple<Type, string>(builder.TypeId, builder.SubtypeName)]);

            if (entity != null && scriptManager != null && scriptManager.EntityScripts.ContainsKey(builder.TypeId))
                entity.GameLogic = (Sandbox.Common.Components.MyGameLogicComponent)Activator.CreateInstance(scriptManager.EntityScripts[builder.TypeId]);

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
