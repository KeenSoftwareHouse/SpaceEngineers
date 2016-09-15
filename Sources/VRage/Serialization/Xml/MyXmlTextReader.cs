using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace VRage
{
    /// <summary>
    /// Custom XML Reader with user data attached
    /// </summary>
    public class MyXmlTextReader : XmlReader
    {
        private XmlReader m_reader;

        public MyXmlTextReader(Stream input, XmlReaderSettings settings)
        {
            m_reader = XmlReader.Create(input, settings);
        }

        /// <summary>
        /// Map to override definitions types
        /// </summary>
        public Dictionary<string, string> DefinitionTypeOverrideMap
        {
            get;
            set;
        }

        public override int AttributeCount
        {
            get { return m_reader.AttributeCount; }
        }

        public override string BaseURI
        {
            get { return m_reader.BaseURI; }
        }

        public override int Depth
        {
            get { return m_reader.Depth; }
        }

        public override bool EOF
        {
            get { return m_reader.EOF; }
        }

        public override string GetAttribute(int i)
        {
            return m_reader.GetAttribute(i);
        }

        public override string GetAttribute(string name, string namespaceURI)
        {
            return m_reader.GetAttribute(name, namespaceURI);
        }

        public override string GetAttribute(string name)
        {
            return m_reader.GetAttribute(name);
        }

        public override bool IsEmptyElement
        {
            get { return m_reader.IsEmptyElement; }
        }

        public override string LocalName
        {
            get { return m_reader.LocalName; }
        }

        public override string LookupNamespace(string prefix)
        {
            return m_reader.LookupNamespace(prefix);
        }

        public override bool MoveToAttribute(string name, string ns)
        {
            return m_reader.MoveToAttribute(name, ns);
        }

        public override bool MoveToAttribute(string name)
        {
            return m_reader.MoveToAttribute(name);
        }

        public override bool MoveToElement()
        {
            return m_reader.MoveToElement();
        }

        public override bool MoveToFirstAttribute()
        {
            return m_reader.MoveToFirstAttribute();
        }

        public override bool MoveToNextAttribute()
        {
            return m_reader.MoveToNextAttribute();
        }

        public override XmlNameTable NameTable
        {
            get { return m_reader.NameTable; }
        }

        public override string NamespaceURI
        {
            get { return m_reader.NamespaceURI; }
        }

        public override XmlNodeType NodeType
        {
            get { return m_reader.NodeType; }
        }

        public override string Prefix
        {
            get { return m_reader.Prefix; }
        }

        public override bool Read()
        {
            return m_reader.Read();
        }

        public override bool ReadAttributeValue()
        {
            return m_reader.ReadAttributeValue();
        }

        public override ReadState ReadState
        {
            get { return m_reader.ReadState; }
        }

        public override void ResolveEntity()
        {
            m_reader.ResolveEntity();
        }

        public override string Value
        {
            get { return m_reader.Value; }
        }
    }
}
