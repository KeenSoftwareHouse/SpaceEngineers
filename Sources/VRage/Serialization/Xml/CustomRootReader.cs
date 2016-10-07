using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace VRage
{
    /// <summary>
    /// Custom XmlReader that allows to read xml fragments
    /// </summary>
    public class CustomRootReader : XmlReader
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
}
