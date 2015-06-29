using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage.Collections;

namespace VRage.ObjectBuilders
{
    public class MyObjectFactory<TAttribute, TCreatedObjectBase>
        where TAttribute : MyFactoryTagAttribute
        where TCreatedObjectBase : class
    {
        private Dictionary<Type, TAttribute> m_attributesByProducedType = new Dictionary<Type, TAttribute>();
        private Dictionary<Type, TAttribute> m_attributesByObjectBuilder = new Dictionary<Type, TAttribute>();

        public DictionaryValuesReader<Type, TAttribute> Attributes
        {
            get { return new DictionaryValuesReader<Type, TAttribute>(m_attributesByProducedType); }
        }

        public void RegisterFromCreatedObjectAssembly()
        {
            var assembly = Assembly.GetAssembly(typeof(TCreatedObjectBase));
            RegisterFromAssembly(assembly);
        }

        public void RegisterFromAssembly(Assembly assembly)
        {
            if (assembly == null)
                return;

            foreach (Type type in assembly.GetTypes())
            {
                var descriptorArray = type.GetCustomAttributes(typeof(TAttribute), false);
                foreach (TAttribute descriptor in descriptorArray)
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
            }
        }

        public TCreatedObjectBase CreateInstance(MyObjectBuilderType objectBuilderType)
        {
            return CreateInstance<TCreatedObjectBase>(objectBuilderType);
        }

        public TBase CreateInstance<TBase>(MyObjectBuilderType objectBuilderType) where TBase : class, TCreatedObjectBase
        {
            Debug.Assert(m_attributesByObjectBuilder.ContainsKey(objectBuilderType));

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

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class MyFactoryTagAttribute : System.Attribute
    {
        public readonly Type ObjectBuilderType;
        public Type ProducedType;
        public bool IsMain;

        public MyFactoryTagAttribute(Type objectBuilderType, bool mainBuilder = true)
        {
            ObjectBuilderType = objectBuilderType;
            IsMain = mainBuilder;
        }
    }

}
