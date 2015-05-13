using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;

namespace Sandbox.Common.News
{
    [XmlRoot(ElementName = "News")]
    public class MyNews
    {
        [XmlElement("Entry")]
        public List<MyNewsEntry> Entry;
    }

    //class because of Public default value
    public class MyNewsEntry
    {
        [XmlAttribute(AttributeName = "title")]
        public string Title;

        [XmlAttribute(AttributeName = "date")]
        public string Date;

        [XmlAttribute(AttributeName = "version")]
        public string Version;

        [XmlAttribute(AttributeName = "public")]
        public bool Public = true;

        [XmlText]
        public string Text;
    }
}
