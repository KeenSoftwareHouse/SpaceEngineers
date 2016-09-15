using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using VRage.Generics;
using VRage.Plugins;

namespace VRage
{
    /// <summary>
    /// Xml serializer base class with custom root element reader/writer caching
    /// </summary>
    /// <typeparam name="TAbstractBase"></typeparam>
    public abstract class MyXmlSerializerBase<TAbstractBase> : IMyXmlSerializable
    {
        [ThreadStatic]
        private static MyObjectsPool<CustomRootReader> m_readerPool;
        protected static MyObjectsPool<CustomRootReader> ReaderPool
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
        protected static MyObjectsPool<CustomRootWriter> WriterPool
        {
            get
            {
                if (m_writerPool == null)
                    m_writerPool = new MyObjectsPool<CustomRootWriter>(2);

                return m_writerPool;
            }
        }

        protected TAbstractBase m_data;

        // Override the Implicit Conversions Since the XmlSerializer
        // Casts to/from the required types implicitly.
        public static implicit operator TAbstractBase(MyXmlSerializerBase<TAbstractBase> o)
        {
            return o.Data;
        }

        public TAbstractBase Data
        {
            get { return m_data; }
        }

        #region IXmlSerializable Members

        public XmlSchema GetSchema()
        {
            return null; // this is fine as schema is unknown.
        }

        public abstract void ReadXml(XmlReader reader);

        protected object Deserialize(XmlReader reader, XmlSerializer serializer, string customRootName)
        {
            CustomRootReader customReader;
            ReaderPool.AllocateOrCreate(out customReader);
            customReader.Init(customRootName, reader);

            var deserialized = serializer.Deserialize(customReader);

            customReader.Release();
            ReaderPool.Deallocate(customReader);

            return deserialized;
        }

        public void WriteXml(XmlWriter writer)
        {
            Type type = m_data.GetType();

            XmlSerializer serializer = MyXmlSerializerManager.GetOrCreateSerializer(type);
            var derivedName = MyXmlSerializerManager.GetSerializedName(type);
            CustomRootWriter customWriter;
            WriterPool.AllocateOrCreate(out customWriter);
            customWriter.Init(derivedName, writer);

            serializer.Serialize(customWriter, m_data);

            customWriter.Release();
            WriterPool.Deallocate(customWriter);
        }

        object IMyXmlSerializable.Data
        {
            get { return m_data; }
        }

        #endregion
    }

    /// <summary>
    /// Custom xml serializer that allows object instantiation on elements with xsl:type attribute
    /// </summary>
    /// <typeparam name="TAbstractBase"></typeparam>
    public class MyAbstractXmlSerializer<TAbstractBase> : MyXmlSerializerBase<TAbstractBase>
    {
        public MyAbstractXmlSerializer()
        {
            // Default Ctor (Required for Xml Serialization - DO NOT USE)
        }

        public MyAbstractXmlSerializer(TAbstractBase data)
        {
            m_data = data;
        }

        public override void ReadXml(XmlReader reader)
        {
            string customRootName;
            XmlSerializer serializer = GetSerializer(reader, out customRootName);

            // Read the Data, Deserializing based on the (now known) concrete type.
            m_data = (TAbstractBase)Deserialize(reader, serializer, customRootName);
        }

        private XmlSerializer GetSerializer(XmlReader reader, out string customRootName)
        {
            // Cast the Data back from the Abstract Type.
            string typeAttrib = GetTypeAttribute(reader);

            XmlSerializer serializer;
            if (typeAttrib == null || !MyXmlSerializerManager.TryGetSerializer(typeAttrib, out serializer))
            {
                typeAttrib = MyXmlSerializerManager.GetSerializedName(typeof(TAbstractBase));
                serializer = MyXmlSerializerManager.GetSerializer(typeAttrib);
            }

            customRootName = typeAttrib;
            return serializer;
        }

        protected virtual string GetTypeAttribute(XmlReader reader)
        {
            return reader.GetAttribute("xsi:type");
        }

        public static implicit operator MyAbstractXmlSerializer<TAbstractBase>(TAbstractBase builder)
        {
            return builder == null ? null : new MyAbstractXmlSerializer<TAbstractBase>(builder);
        }
    }

    public interface IMyXmlSerializable : IXmlSerializable
    {
        object Data { get; }
    }
}