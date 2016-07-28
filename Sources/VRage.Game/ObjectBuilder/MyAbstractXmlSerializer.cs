using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using VRage.Generics;
using VRage.ObjectBuilders;
using VRage.Plugins;
#if XB1 // XB1_ALLINONEASSEMBLY
using VRage.Utils;
#endif // XB1

namespace VRage.Game
{
    class CustomRootReader : XmlReader
    {
        private XmlReader m_source;
        private string m_customRootName;
        private int m_rootDepth;

        internal void Init(string customRootName, XmlReader source)
        {
            m_source = source;
            m_customRootName = customRootName;
            m_rootDepth = source.Depth;
        }

        internal void Release()
        {
            m_source = null;
            m_customRootName = null;
            m_rootDepth = -1;
        }

        public override int AttributeCount { get { return m_source.AttributeCount; } }
        public override string BaseURI { get { return m_source.BaseURI; } }
        public override void Close() { m_source.Close(); }
        public override int Depth { get { return m_source.Depth; } }
        public override bool EOF { get { return m_source.EOF; } }
        public override string GetAttribute(int i) { return m_source.GetAttribute(i); }
        public override string GetAttribute(string name) { return m_source.GetAttribute(name); }
        public override bool IsEmptyElement { get { return m_source.IsEmptyElement; } }
        public override string LookupNamespace(string prefix) { return m_source.LookupNamespace(prefix); }
        public override bool MoveToAttribute(string name, string ns) { return m_source.MoveToAttribute(name, ns); }
        public override bool MoveToAttribute(string name) { return m_source.MoveToAttribute(name); }
        public override bool MoveToElement() { return m_source.MoveToElement(); }
        public override bool MoveToFirstAttribute() { return m_source.MoveToFirstAttribute(); }
        public override bool MoveToNextAttribute() { return m_source.MoveToNextAttribute(); }
        public override XmlNameTable NameTable { get { return m_source.NameTable; } }
        public override XmlNodeType NodeType { get { return m_source.NodeType; } }
        public override string Prefix { get { return m_source.Prefix; } }
        public override bool Read() { return m_source.Read(); }
        public override bool ReadAttributeValue() { return m_source.ReadAttributeValue(); }
        public override ReadState ReadState { get { return m_source.ReadState; } }
        public override void ResolveEntity() { m_source.ResolveEntity(); }
        public override string Value { get { return m_source.Value; } }

        public override string LocalName
        {
            get { return (m_source.Depth == m_rootDepth) ? m_source.NameTable.Get(m_customRootName) : m_source.LocalName; }
        }

        public override string NamespaceURI
        {
            get { return (m_source.Depth == m_rootDepth) ? m_source.NameTable.Get("") : m_source.NamespaceURI; }
        }

        public override string GetAttribute(string name, string namespaceURI)
        {
            return (m_source.Depth == m_rootDepth) ? null : m_source.GetAttribute(name, namespaceURI);
        }
    }

    class CustomRootWriter : XmlWriter
    {
        private XmlWriter m_target;
        private string m_customRootType;
        private int m_currentDepth;

        internal void Init(string customRootType, XmlWriter target)
        {
            m_target = target;
            m_customRootType = customRootType;
            m_target.WriteAttributeString("xsi:type", m_customRootType);
            m_currentDepth = 0;
        }

        internal void Release()
        {
            m_target = null;
            m_customRootType = null;
            Debug.Assert(m_currentDepth == 0);
        }

        public override void Close() { m_target.Close(); }
        public override void Flush() { m_target.Flush(); }
        public override string LookupPrefix(string ns) { return m_target.LookupPrefix(ns); }
        public override void WriteBase64(byte[] buffer, int index, int count) { m_target.WriteBase64(buffer, index, count); }
        public override void WriteCData(string text) { m_target.WriteCData(text); }
        public override void WriteCharEntity(char ch) { m_target.WriteCharEntity(ch); }
        public override void WriteChars(char[] buffer, int index, int count) { m_target.WriteChars(buffer, index, count); }
        public override void WriteComment(string text) { m_target.WriteComment(text); }
        public override void WriteEndAttribute() { m_target.WriteEndAttribute(); }
        public override void WriteEndElement()
        {
            --m_currentDepth;
            if (m_currentDepth > 0)
            {
                m_target.WriteEndElement();
            }
        }
        public override void WriteEntityRef(string name) { m_target.WriteEntityRef(name); }
        public override void WriteFullEndElement() { m_target.WriteFullEndElement(); }
        public override void WriteProcessingInstruction(string name, string text) { m_target.WriteProcessingInstruction(name, text); }
        public override void WriteRaw(string data) { m_target.WriteRaw(data); }
        public override void WriteRaw(char[] buffer, int index, int count) { m_target.WriteRaw(buffer, index, count); }
        public override void WriteStartAttribute(string prefix, string localName, string ns) { m_target.WriteStartAttribute(prefix, localName, ns); }
        public override void WriteString(string text) { m_target.WriteString(text); }
        public override void WriteSurrogateCharEntity(char lowChar, char highChar) { m_target.WriteSurrogateCharEntity(lowChar, highChar); }
        public override void WriteWhitespace(string ws) { m_target.WriteWhitespace(ws); }

        public override WriteState WriteState
        {
            get { return m_target.WriteState; }
        }

        public override void WriteDocType(string name, string pubid, string sysid, string subset) { }
        public override void WriteStartDocument(bool standalone) { }
        public override void WriteStartDocument() { }
        public override void WriteEndDocument()
        {
            while (m_currentDepth > 0)
            {
                WriteEndElement(); // decrements current depth
            }
        }
        public override void WriteStartElement(string prefix, string localName, string ns)
        {
            if (m_currentDepth > 0)
            {
                m_target.WriteStartElement(prefix, localName, ns);
            }
            ++m_currentDepth;
        }

    }

    public class MyXmlSerializerManager
    {
        private static readonly Dictionary<Type, XmlSerializer> m_serializersByType = new Dictionary<Type, XmlSerializer>();
        private static readonly Dictionary<string, XmlSerializer> m_serializersBySerializedName = new Dictionary<string, XmlSerializer>();
        private static readonly Dictionary<Type, string> m_serializedNameByType = new Dictionary<Type, string>();

#if !XB1 // XB1_ALLINONEASSEMBLY
        private static HashSet<Assembly> m_registeredAssemblies = new HashSet<Assembly>();
#else // XB1
        private static bool m_registered = false;
#endif // XB1

        public static void RegisterObSerialzier(Type ob)
        {
            if (!m_serializersByType.ContainsKey(ob))
            {
                XmlSerializer serializer;
                serializer = new XmlSerializer(ob);
                string serializedName = ob.Name;

                m_serializersBySerializedName.Add(serializedName, serializer);
                m_serializersByType.Add(ob, serializer);

                m_serializedNameByType.Add(ob, serializedName);
            }
        }

        public static void RegisterFromAssembly(Assembly assembly)
        {
            if (assembly == null) return;
#if !XB1 // XB1_ALLINONEASSEMBLY
            if (m_registeredAssemblies.Contains(assembly)) return;
            m_registeredAssemblies.Add(assembly);
#endif // !XB1

#if XB1 // XB1_ALLINONEASSEMBLY
            System.Diagnostics.Debug.Assert(m_registered == false);
            if (m_registered == true)
                return;
            m_registered = true;
            foreach (Type type in MyAssembly.GetTypes())
#else // !XB1
            foreach (Type type in assembly.GetTypes())
#endif // !XB1
            {
                try
                {
                    string serializedName = null;

                    var xmlTypes = type.GetCustomAttributes(typeof(XmlTypeAttribute), false);

                    if (xmlTypes.Length > 0)
                    {
                        var descriptor = (XmlTypeAttribute)xmlTypes[0];

                        serializedName = type.Name;

                        if (!string.IsNullOrEmpty(descriptor.TypeName))
                            serializedName = descriptor.TypeName;
                    }
                    else if (typeof(MyObjectBuilder_Base).IsAssignableFrom(type))
                    {
                        serializedName = type.Name;
                    }

                    if (serializedName != null && !m_serializersBySerializedName.ContainsKey(serializedName))
                    {
                        XmlSerializer serializer;
                        serializer = new XmlSerializer(type);

                        m_serializersBySerializedName.Add(serializedName, serializer);
                        m_serializersByType.Add(type, serializer);

                        m_serializedNameByType.Add(type, serializedName);
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException("Error creating XML serializer for type " + type.Name, e);
                }
            }
        }

        public static XmlSerializer GetSerializer(Type type)
        {
            return m_serializersByType[type];
        }

        public static string GetSerializedName(Type type)
        {
            return m_serializedNameByType[type];
        }

        public static XmlSerializer GetSerializer(string serializedName)
        {
            return m_serializersBySerializedName[serializedName];
        }

        public static bool IsSerializerAvailable(string name)
        {
            return m_serializersBySerializedName.ContainsKey(name);
        }
    }

    public class MyAbstractXmlSerializer<TAbstractBase> : IXmlSerializable
    {
        [ThreadStatic]
        private static MyObjectsPool<CustomRootReader> m_readerPool;
        private static MyObjectsPool<CustomRootReader> ReaderPool
        {
            get
            {
                if (m_readerPool == null)
                    m_readerPool = new MyObjectsPool<CustomRootReader>(2);
                return m_readerPool;
            }
        }

        [ThreadStatic]
        private static MyObjectsPool<CustomRootWriter> m_writerPool;
        private static MyObjectsPool<CustomRootWriter> WriterPool
        {
            get
            {
                if (m_writerPool == null)
                    m_writerPool = new MyObjectsPool<CustomRootWriter>(2);
                return m_writerPool;
            }
        }

        // Override the Implicit Conversions Since the XmlSerializer
        // Casts to/from the required types implicitly.
        public static implicit operator TAbstractBase(MyAbstractXmlSerializer<TAbstractBase> o)
        {
            return o.Data;
        }

        public static implicit operator MyAbstractXmlSerializer<TAbstractBase>(TAbstractBase o)
        {
            return o == null ? null : new MyAbstractXmlSerializer<TAbstractBase>(o);
        }

        private TAbstractBase m_data;

        /// <summary>
        /// [Concrete] Data to be stored/is stored as XML.
        /// </summary>
        public TAbstractBase Data
        {
            get { return m_data; }
            set { m_data = value; }
        }

        /// <summary>
        /// **DO NOT USE** This is only added to enable XML Serialization.
        /// </summary>
        /// <remarks>DO NOT USE THIS CONSTRUCTOR</remarks>
        public MyAbstractXmlSerializer()
        {
            // Debug.WriteLine("AbstractXmlSerializer instantiated for: " + typeof(TAbstractBase).Name);
            // Default Ctor (Required for Xml Serialization - DO NOT USE)
        }

        /// <summary>
        /// Initialises the Serializer to work with the given data.
        /// </summary>
        /// <param name="data">Concrete Object of the AbstractType Specified.</param>
        public MyAbstractXmlSerializer(TAbstractBase data)
        {
            m_data = data;
        }

        #region IXmlSerializable Members

        public XmlSchema GetSchema()
        {
            return null; // this is fine as schema is unknown.
        }

        public void ReadXml(XmlReader reader)
        {
            // Cast the Data back from the Abstract Type.
            string typeAttrib = reader.GetAttribute("xsi:type");

//#if DEBUG
            if (typeAttrib == null || !MyXmlSerializerManager.IsSerializerAvailable(typeAttrib))
//#else
//            if (typeAttrib == null)
//#endif
            {
                typeAttrib = MyXmlSerializerManager.GetSerializedName(typeof(TAbstractBase));
            }

            // Read the Data, Deserializing based on the (now known) concrete type.
            {
                CustomRootReader customReader;
                ReaderPool.AllocateOrCreate(out customReader);
                customReader.Init(typeAttrib, reader);

                var serializer = MyXmlSerializerManager.GetSerializer(typeAttrib);
                this.Data = (TAbstractBase)serializer.Deserialize(customReader);

                customReader.Release();
                ReaderPool.Deallocate(customReader);
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            Type type = m_data.GetType();

            {
                CustomRootWriter customWriter;
                WriterPool.AllocateOrCreate(out customWriter);
                var derivedName = MyXmlSerializerManager.GetSerializedName(type);
                customWriter.Init(derivedName, writer);

                MyXmlSerializerManager.GetSerializer(type).Serialize(customWriter, m_data);

                customWriter.Release();
                WriterPool.Deallocate(customWriter);
            }
        }

        #endregion
    }
}