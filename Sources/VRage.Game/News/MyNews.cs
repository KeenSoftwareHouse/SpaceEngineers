using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;

namespace VRage.Game.News
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

        [XmlAttribute(AttributeName = "dev")]
        public bool Dev = false;

        [XmlText]
        public string Text;
    }
}
