using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using VRage;

namespace VRage.Game
{
    /// <summary>
    /// Custom XmlSerializer for definitions that allows to override the definition type
    /// </summary>
    public class MyDefinitionXmlSerializer : MyAbstractXmlSerializer<MyObjectBuilder_DefinitionBase>
    {
        public const string DEFINITION_ELEMENT_NAME = "Definition";

        public MyDefinitionXmlSerializer() { }

        public MyDefinitionXmlSerializer(MyObjectBuilder_DefinitionBase data)
        {
            m_data = data;
        }

        protected override string GetTypeAttribute(XmlReader reader)
        {
            string typeAttrib = base.GetTypeAttribute(reader);
            if (typeAttrib == null)
                return null;

            MyXmlTextReader myreader = reader as MyXmlTextReader;
            if (myreader != null && myreader.DefinitionTypeOverrideMap != null)
            {
                string typeOverride;
                if (myreader.DefinitionTypeOverrideMap.TryGetValue(typeAttrib, out typeOverride))
                    return typeOverride;
            }

            return typeAttrib;
        }

        public static implicit operator MyDefinitionXmlSerializer(MyObjectBuilder_DefinitionBase builder)
        {
            return builder == null ? null : new MyDefinitionXmlSerializer(builder);
        }
    }
}
