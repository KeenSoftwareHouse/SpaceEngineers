using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VRage;
using VRage.ObjectBuilders;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Profiler;

namespace Sandbox.Game.Entities
{
    internal static class MyEntityFactory
    {
        static MyObjectFactory<MyEntityTypeAttribute, MyEntity> m_objectFactory = new MyObjectFactory<MyEntityTypeAttribute, MyEntity>();

        public static void RegisterDescriptor(MyEntityTypeAttribute descriptor, Type type)
        {
            if(type != null && descriptor != null)
                m_objectFactory.RegisterDescriptor(descriptor, type);
        }

        public static void RegisterDescriptorsFromAssembly(Assembly assembly)
        {
            if (assembly != null)
                m_objectFactory.RegisterFromAssembly(assembly);
        }

        public static MyEntity CreateEntity(MyObjectBuilder_Base builder, bool readyForReplication = true)
        {
            return CreateEntity(builder.TypeId, builder.SubtypeName, readyForReplication);
        }

        public static MyEntity CreateEntity(MyObjectBuilderType typeId, string subTypeName = null, bool readyForReplication = true)
        {
            ProfilerShort.Begin("MyEntityFactory.CreateEntity(...)");
            MyEntity entity = m_objectFactory.CreateInstance(typeId);
            AddScriptGameLogic(entity, typeId, subTypeName);
            entity.IsReadyForReplication = readyForReplication;
            ProfilerShort.End();
            MyEntities.RaiseEntityCreated(entity);            
            return entity;
        }

        public static T CreateEntity<T>(MyObjectBuilder_Base builder, bool readyForReplication = true) where T : MyEntity
        {
            ProfilerShort.Begin("MyEntityFactory.CreateEntity(...)");
            T entity = m_objectFactory.CreateInstance<T>(builder.TypeId);
            AddScriptGameLogic(entity, builder.GetType(), builder.SubtypeName);
            entity.IsReadyForReplication = readyForReplication;
            ProfilerShort.End();
            MyEntities.RaiseEntityCreated(entity);           
            return entity;
        }

        // using an empty set instead of null avoids special-casing null
        private static readonly HashSet<Type> m_emptySet = new HashSet<Type>();

        public static void AddScriptGameLogic(MyEntity entity, MyObjectBuilderType builderType, string subTypeName = null)
        {
            var scriptManager = Sandbox.Game.World.MyScriptManager.Static;
            if (scriptManager == null || entity == null)
                return;

            // both types of logic components are valid to be attached:

            // (1) those that are specific for the given subTypeName
            HashSet<Type> subEntityScripts;
            if (subTypeName != null)
            {
                var key = new Tuple<Type, string>(builderType, subTypeName);
                subEntityScripts = scriptManager.SubEntityScripts.GetValueOrDefault(key, m_emptySet);
            }
            else
            {
                subEntityScripts = m_emptySet;
            }

            // (2) and those that don't care about the subTypeName
            HashSet<Type> entityScripts = scriptManager.EntityScripts.GetValueOrDefault(builderType, m_emptySet);

            // if there are no component types to attach leave the entity as-is
            var count = subEntityScripts.Count + entityScripts.Count;
            if (count == 0)
                return;

            // just concatenate the two type-sets, they are disjunct by definition (see ScriptManager)
            var logicComponents = new List<MyGameLogicComponent>(count);
            foreach (var logicComponentType in entityScripts.Concat(subEntityScripts))
            {
                logicComponents.Add((MyGameLogicComponent)Activator.CreateInstance(logicComponentType));
            }

            // wrap the gamelogic-components to appear as a single component to the entity
            entity.GameLogic = MyCompositeGameLogicComponent.Create(logicComponents, entity);
        }

        public static MyObjectBuilder_EntityBase CreateObjectBuilder(MyEntity entity)
        {
            return m_objectFactory.CreateObjectBuilder<MyObjectBuilder_EntityBase>(entity);
        }
    }
}
