using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.ObjectBuilders;

namespace VRage.Game.Components
{
    public static class MySessionComponentMapping
    {
        private static Dictionary<Type, MyObjectBuilderType> m_objectBuilderTypeByType = new Dictionary<Type,MyObjectBuilderType>();
        private static Dictionary<MyObjectBuilderType, Type> m_typeByObjectBuilderType = new Dictionary<MyObjectBuilderType,Type>();
        private static Dictionary<Type, MyObjectBuilder_SessionComponent> m_sessionObjectBuilderByType = new Dictionary<Type,MyObjectBuilder_SessionComponent>();

        public static bool Map(Type type, MyObjectBuilderType objectBuilderType)
        {
            Debug.Assert(type.IsSubclassOf(typeof(MySessionComponentBase)), "Type is not derived from session component");
            Debug.Assert(!m_objectBuilderTypeByType.ContainsKey(type), "Session component type is already mapped");
            Debug.Assert(!m_typeByObjectBuilderType.ContainsKey(objectBuilderType), "Session object builder is already mapped");

            if (!type.IsSubclassOf(typeof(MySessionComponentBase)))
                return false;

            if (!m_objectBuilderTypeByType.ContainsKey(type))
                m_objectBuilderTypeByType.Add(type, objectBuilderType);
            else
                return false;

            if (!m_typeByObjectBuilderType.ContainsKey(objectBuilderType))
                m_typeByObjectBuilderType.Add(objectBuilderType, type);
            else
                return false;

            return true;
        }

        public static Type TryGetMappedSessionComponentType(MyObjectBuilderType objectBuilderType)
        {
            Type output = null;
            m_typeByObjectBuilderType.TryGetValue(objectBuilderType, out output);
            return output;
        }

        public static MyObjectBuilderType TryGetMappedObjectBuilderType(Type type)
        {
            MyObjectBuilderType output = null;
            m_objectBuilderTypeByType.TryGetValue(type, out output);
            return output; 
        }

        public static void Clear()
        {
            m_objectBuilderTypeByType.Clear();
            m_typeByObjectBuilderType.Clear();
            m_sessionObjectBuilderByType.Clear();
        }

        public static Dictionary<Type, MyObjectBuilder_SessionComponent> GetMappedSessionObjectBuilders(List<MyObjectBuilder_SessionComponent> objectBuilders)
        {
            m_sessionObjectBuilderByType.Clear();

            foreach (var objectBuilder in objectBuilders)
            {
                Debug.Assert(m_typeByObjectBuilderType.ContainsKey(objectBuilder.GetType()), "No session component typed for the given object builder");
                if (!m_typeByObjectBuilderType.ContainsKey(objectBuilder.GetType()))
                    continue;

                var type = m_typeByObjectBuilderType[objectBuilder.GetType()];
                Debug.Assert(!m_sessionObjectBuilderByType.ContainsKey(type), "Duplication of object builders. Overwriting.");
                m_sessionObjectBuilderByType[type] = objectBuilder;
            }

            return m_sessionObjectBuilderByType;
        }


    }
}
