using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace VRage
{
    /// <summary>
    /// Custom XmlWriter that allows to write xml fragments
    /// </summary>
    public class CustomRootWriter : XmlWriter
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
}
