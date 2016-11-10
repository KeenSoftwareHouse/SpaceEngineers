using System;
using System.Xml.Serialization;
using VRage;

namespace VRageRender
{
    public class MyMaterialsSettings
    {
        [XmlElement(Type = typeof(MyStructXmlSerializer<Struct>))] 
        public Struct Data = Struct.Default;

        [XmlType("MyMaterialsSettings.Struct")]
        public struct Struct
        {
            [StructDefault] public static readonly Struct Default;

            static Struct()
            {
                Default = new Struct()
                {
                };
            }
        }

        [XmlType("MyChangeableMaterial")]
        public struct MyChangeableMaterial
        {
            public string MaterialName;
        }

        MyChangeableMaterial[] m_changeableMaterials;

        [XmlArrayItem("ChangeableMaterial")]
        public MyChangeableMaterial[] ChangeableMaterials
        {
            get { return m_changeableMaterials; }
            set
            {
                if (m_changeableMaterials.Length != value.Length)
                    m_changeableMaterials = new MyChangeableMaterial[value.Length];
                value.CopyTo(m_changeableMaterials, 0);
            }
        }

        public MyMaterialsSettings()
        {
            m_changeableMaterials = new MyChangeableMaterial[0];
        }

        public void CopyFrom(MyMaterialsSettings settings)
        {
            Data = settings.Data;
            ChangeableMaterials = settings.ChangeableMaterials;
        }
    }
}
