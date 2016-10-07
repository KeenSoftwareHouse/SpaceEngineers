using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using VRage.Collections;
using VRage.Game.Common;
#if XB1 // XB1_ALLINONEASSEMBLY
using VRage.Utils;
#endif // XB1

namespace VRage.ObjectBuilders
{
    public class MyObjectFactory<TAttribute, TCreatedObjectBase>
        where TAttribute : MyFactoryTagAttribute
        where TCreatedObjectBase : class
    {
#if XB1 // XB1_ALLINONEASSEMBLY
        private bool m_registered = false;
#endif // XB1

        private Dictionary<Type, TAttribute> m_attributesByProducedType = new Dictionary<Type, TAttribute>();
        private Dictionary<Type, TAttribute> m_attributesByObjectBuilder = new Dictionary<Type, TAttribute>();

        public DictionaryValuesReader<Type, TAttribute> Attributes
        {
            get { return new DictionaryValuesReader<Type, TAttribute>(m_attributesByProducedType); }
        }

        public void RegisterFromCreatedObjectAssembly()
        {
#if XB1 // XB1_ALLINONEASSEMBLY
            RegisterFromAssembly(MyAssembly.AllInOneAssembly);
#else // !XB1
            var assembly = Assembly.GetAssembly(typeof(TCreatedObjectBase));
            RegisterFromAssembly(assembly);
#endif // !XB1
        }

        public void RegisterDescriptor(TAttribute descriptor, Type type)
        {
            descriptor.ProducedType = type;

            Debug.Assert(
                typeof(TCreatedObjectBase).IsAssignableFrom(type),
                "Type " + type + " cannot have factory tag attribute " + typeof(TAttribute) + ", because it's not assignable to " + typeof(TCreatedObjectBase)
            );

            if (descriptor.IsMain)
            {
                Debug.Assert(
                    !m_attributesByProducedType.ContainsKey(descriptor.ProducedType),
                    "Duplicate factory tag attribute " + typeof(TAttribute) + " on type " + type +
                    "\nEither remove the duplicate instances or mark only one of the attributes as the main one main"
                );
                m_attributesByProducedType.Add(descriptor.ProducedType, descriptor);
            }

            if (descriptor.ObjectBuilderType != null)
            {
                System.Diagnostics.Debug.Assert(!m_attributesByObjectBuilder.ContainsKey(descriptor.ObjectBuilderType), "Object builder already assigned for another entity, fatal error!");
                m_attributesByObjectBuilder.Add(descriptor.ObjectBuilderType, descriptor);
            }
            else if (typeof(MyObjectBuilder_Base).IsAssignableFrom(descriptor.ProducedType))
            { // MyObjectBuilder_Base can use itself to convert one type to another
                m_attributesByObjectBuilder.Add(descriptor.ProducedType, descriptor);
            }
        }

        public void RegisterFromAssembly(Assembly assembly)
        {
            if (assembly == null)
                return;

#if XB1 // XB1_ALLINONEASSEMBLY
            System.Diagnostics.Debug.Assert(m_registered == false);
            if (m_registered == true)
                return;
            m_registered = true;
            var types = MyAssembly.GetTypes();
            foreach (Type type in types)
#else // !XB1
            foreach (Type type in assembly.GetTypes())
#endif // !XB1
            {
                var descriptorArray = type.GetCustomAttributes(typeof(TAttribute), false);
                foreach (TAttribute descriptor in descriptorArray)
                {
                    RegisterDescriptor(descriptor, type);
                }
            }
        }

        public TCreatedObjectBase CreateInstance(MyObjectBuilderType objectBuilderType)
        {
            return CreateInstance<TCreatedObjectBase>(objectBuilderType);
        }

        public TBase CreateInstance<TBase>(MyObjectBuilderType objectBuilderType) where TBase : class, TCreatedObjectBase
        {
            Debug.Assert(m_attributesByObjectBuilder.ContainsKey(objectBuilderType), "'" + objectBuilderType + "' cannot be resolved");

            TAttribute attribute;
            if (m_attributesByObjectBuilder.TryGetValue(objectBuilderType, out attribute))
            {
                return Activator.CreateInstance(attribute.ProducedType) as TBase;
            }

            return null;
        }

        public TBase CreateInstance<TBase>() where TBase : TCreatedObjectBase, new()
        {
            Debug.Assert(m_attributesByProducedType.ContainsKey(typeof(TBase)));
            return new TBase();
        }

        public Type GetProducedType(MyObjectBuilderType objectBuilderType)
        {
            return m_attributesByObjectBuilder[objectBuilderType].ProducedType;
        }

        public Type TryGetProducedType(MyObjectBuilderType objectBuilderType)
        {
            TAttribute attrib = null;
            if (!m_attributesByObjectBuilder.TryGetValue(objectBuilderType, out attrib)) return null;
            return attrib.ProducedType;
        }

        public TObjectBuilder CreateObjectBuilder<TObjectBuilder>(TCreatedObjectBase instance) where TObjectBuilder : MyObjectBuilder_Base
        {
            return CreateObjectBuilder<TObjectBuilder>(instance.GetType());
        }

        public TObjectBuilder CreateObjectBuilder<TObjectBuilder>(Type instanceType) where TObjectBuilder : MyObjectBuilder_Base
        {
            TAttribute attribute;
            if (!m_attributesByProducedType.TryGetValue(instanceType, out attribute))
                return null;
            Debug.Assert(typeof(TObjectBuilder).IsAssignableFrom(attribute.ObjectBuilderType));
            return MyObjectBuilderSerializer.CreateNewObject(attribute.ObjectBuilderType) as TObjectBuilder;
        }
    }
}
