using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.Components;
using VRage;
using VRage;

namespace Sandbox.Game.Entities
{
    internal static class MyEntityFactory
    {
        static MyObjectFactory<MyEntityTypeAttribute, MyEntity> m_objectFactory = new MyObjectFactory<MyEntityTypeAttribute, MyEntity>();

        public static void RegisterDescriptorsFromAssembly(Assembly assembly)
        {
            if (assembly != null)
                m_objectFactory.RegisterFromAssembly(assembly);
        }

        public static MyEntity CreateEntity(MyObjectBuilder_Base builder)
        {
            MyEntity entity = CreateEntity(builder.TypeId);
            return entity;
        }

        public static MyEntity CreateEntity(MyObjectBuilderType typeId)
        {
            ProfilerShort.Begin("MyEntityFactory.CreateEntity(...)");
            MyEntity entity = m_objectFactory.CreateInstance(typeId);
            var scriptManager = Sandbox.Game.World.MyScriptManager.Static;
            if (scriptManager != null && scriptManager.EntityScripts.ContainsKey(typeId))
                entity.GameLogic = (MyGameLogicComponent)Activator.CreateInstance(scriptManager.EntityScripts[typeId]);
            ProfilerShort.End();

            return entity;
        }

        public static T CreateEntity<T>(MyObjectBuilder_Base builder) where T : MyEntity
        {
            ProfilerShort.Begin("MyEntityFactory.CreateEntity(...)");
            T entity = m_objectFactory.CreateInstance<T>(builder.TypeId);
            var scriptManager = Sandbox.Game.World.MyScriptManager.Static;
            var builderType = builder.GetType();
            if (scriptManager != null && scriptManager.EntityScripts.ContainsKey(builderType))
                entity.GameLogic = (MyGameLogicComponent)Activator.CreateInstance(scriptManager.EntityScripts[builderType]);
            ProfilerShort.End();
            return entity;
        }

        public static MyObjectBuilder_EntityBase CreateObjectBuilder(MyEntity entity)
        {
            return m_objectFactory.CreateObjectBuilder<MyObjectBuilder_EntityBase>(entity);
        }
    }

    public class MyEntityTypeAttribute : MyFactoryTagAttribute
    {
        public MyEntityTypeAttribute(Type objectBuilderType, bool mainBuilder = true)
            : base(objectBuilderType, mainBuilder)
        {
        }
    }

}
