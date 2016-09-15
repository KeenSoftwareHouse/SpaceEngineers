using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace VRage
{
    /// <summary>
    /// Deserializes structs using a specified default value (see StructDefaultAttribute).
    /// </summary>
    public class MyStructXmlSerializer<TStruct> : MyXmlSerializerBase<TStruct>
        where TStruct : struct
    {
        public static FieldInfo m_defaultValueField;
        private static Dictionary<string, Accessor> m_accessorMap;

        public MyStructXmlSerializer() { }

        public MyStructXmlSerializer(ref TStruct data)
        {
            m_data = data;
        }

        public override void ReadXml(XmlReader reader)
        {
            BuildAccessorsInfo();

            // Box the struct value now to make it mutable
            object boxed = (TStruct)m_defaultValueField.GetValue(null);

            reader.MoveToElement();
            if (reader.IsEmptyElement)
            {
                reader.Skip();
                return;
            }

            reader.ReadStartElement();
            reader.MoveToContent();
            while (reader.NodeType != XmlNodeType.EndElement && reader.NodeType != XmlNodeType.None)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    Accessor accessor;
                    if (m_accessorMap.TryGetValue(reader.LocalName, out accessor))
                    {
                        object value;
                        if (accessor.IsPrimitiveType)
                        {
                            string valueStr = reader.ReadElementString();
                            TypeConverter converter = TypeDescriptor.GetConverter(accessor.Type);
                            value = converter.ConvertFrom(null, CultureInfo.InvariantCulture, valueStr);
                        }
                        else if (accessor.SerializerType != null)
                        {
                            var serializer = Activator.CreateInstance(accessor.SerializerType) as IMyXmlSerializable;
                            serializer.ReadXml(reader.ReadSubtree());
                            value = serializer.Data;
                            reader.ReadEndElement();
                        }
                        else
                        {
                            XmlSerializer serializer = MyXmlSerializerManager.GetOrCreateSerializer(accessor.Type);
                            string rootName = MyXmlSerializerManager.GetSerializedName(accessor.Type);
                            value = Deserialize(reader, serializer, rootName);
                        }

                        accessor.SetValue(boxed, value);
                    }
                    else
                    {
                        reader.Skip();
                    }
                }

                reader.MoveToContent();
            }
            reader.ReadEndElement();

            // Unbox the mutated struct value
            m_data = (TStruct)boxed;
        }

        private static void BuildAccessorsInfo()
        {
            if (m_defaultValueField != null)
                return;

            lock (typeof(TStruct))
            {
                if (m_defaultValueField != null)
                    return;

                m_defaultValueField = MyStructDefault.GetDefaultFieldInfo(typeof(TStruct));
                if (m_defaultValueField == null)
                    throw new Exception("Missing default value for struct " + typeof(TStruct).FullName
                        + ". Decorate one static read-only field with StructDefault attribute");

                m_accessorMap = new Dictionary<string, Accessor>();
                foreach (var field in typeof(TStruct).GetFields(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (field.GetCustomAttribute(typeof(XmlIgnoreAttribute)) != null)
                    {
                        // Skip fields and with ignore attribute
                        continue;
                    }

                    m_accessorMap.Add(field.Name, new FieldAccessor(field));
                }

                foreach (var property in typeof(TStruct).GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (property.GetCustomAttribute(typeof(XmlIgnoreAttribute)) != null
                            || property.GetIndexParameters().Length != 0)
                    {
                        // Skip indexed properties and with ignore attribute
                        continue;
                    }

                    m_accessorMap.Add(property.Name, new PropertyAccessor(property));
                }
            }
        }

        public static implicit operator MyStructXmlSerializer<TStruct>(TStruct data)
        {
            return new MyStructXmlSerializer<TStruct>(ref data);
        }

        /// <summary>
        /// Abstract accessor for both fields and properties
        /// </summary>
        abstract class Accessor
        {
            public abstract object GetValue(object obj);
            public abstract void SetValue(object obj, object value);

            public abstract Type Type { get; }

            public Type SerializerType { get; private set; }

            public bool IsPrimitiveType
            {
                get
                {
                    Type type = Type;
                    return type.IsPrimitive || type == typeof(string);
                }
            }

            // Check for XmlElement attribute that provides IMyXmlSerializable
            protected void CheckXmlElement(MemberInfo info)
            {
                var attribute = info.GetCustomAttribute(typeof(XmlElementAttribute), false) as XmlElementAttribute;
                if (attribute != null && attribute.Type != null && typeof(IMyXmlSerializable).IsAssignableFrom(attribute.Type))
                    SerializerType = attribute.Type;
            }
        }

        class FieldAccessor : Accessor
        {
            public FieldInfo Field { get; private set; }

            public FieldAccessor(FieldInfo field)
            {
                Field = field;
                CheckXmlElement(field);
            }

            public override object GetValue(object obj)
            {
                return Field.GetValue(obj);
            }

            public override void SetValue(object obj, object value)
            {
                Field.SetValue(obj, value);
            }

            public override Type Type
            {
                get { return Field.FieldType; }
            }
        }

        class PropertyAccessor : Accessor
        {
            public PropertyInfo Property { get; private set; }

            public PropertyAccessor(PropertyInfo property)
            {
                Property = property;
                CheckXmlElement(property);
            }

            public override object GetValue(object obj)
            {
                return Property.GetValue(obj);
            }

            public override void SetValue(object obj, object value)
            {
                Property.SetValue(obj, value);
            }

            public override Type Type
            {
                get { return Property.PropertyType; }
            }
        }
    }
}
