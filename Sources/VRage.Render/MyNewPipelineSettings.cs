using System;
using System.Xml.Serialization;
using VRage;

namespace VRageRender
{
    // Do not add this to white list for modders!
    [XmlType("MyNewPipelineSettings.Struct")]
    public struct MyPassLoddingSetting
    {
        public int LodShift;
        public int MinLod;

        [StructDefault]
        public static readonly MyPassLoddingSetting Default = new MyPassLoddingSetting();
    }

    // Do not add this to white list for modders!
    [XmlType("MyGlobalLoddingSettings")]
    public struct MyGlobalLoddingSettings
    {
        public float ObjectDistanceAdd;
        public float ObjectDistanceMult;
        public bool IsUpdateEnabled;

        [StructDefault]
        public static readonly MyGlobalLoddingSettings Default = new MyGlobalLoddingSettings
        {
            ObjectDistanceAdd = 0,
            ObjectDistanceMult = 1.0f,
            IsUpdateEnabled = true,
        };
    }

    // Do not add this to white list for modders!
    public class MyNewPipelineSettings
    {
        string[] m_blackListMaterials;

        [XmlArrayItem("BlackListMaterial")]
        public string[] BlackListMaterials
        {
            get { return m_blackListMaterials; }
            set
            {
                if (m_blackListMaterials.Length != value.Length)
                    m_blackListMaterials = new string[value.Length];
                value.CopyTo(m_blackListMaterials, 0);
            }
        }

        string[] m_noShadowCasterMaterials;

        [XmlArrayItem("NoShadowCasterMaterial")]
        public string[] NoShadowCasterMaterials
        {
            get { return m_noShadowCasterMaterials; }
            set
            {
                if (m_noShadowCasterMaterials.Length != value.Length)
                    m_noShadowCasterMaterials = new string[value.Length];
                value.CopyTo(m_noShadowCasterMaterials, 0);
            }
        }

        [XmlElement(Type = typeof(MyStructXmlSerializer<MyPassLoddingSetting>))]
        public MyPassLoddingSetting GBufferLodding = MyPassLoddingSetting.Default;
        MyPassLoddingSetting[] m_cascadeDepthLoddings;
        public MyPassLoddingSetting SingleDepthLodding = MyPassLoddingSetting.Default;

        [XmlArrayItem("CascadeDepthLodding")]
        public MyPassLoddingSetting[] CascadeDepthLoddings
        {
            get { return m_cascadeDepthLoddings; }
            set
            {
                if (m_cascadeDepthLoddings.Length != value.Length)
                    m_cascadeDepthLoddings = new MyPassLoddingSetting[value.Length];
                value.CopyTo(m_cascadeDepthLoddings, 0);
            }
        }

        [XmlElement(Type = typeof(MyStructXmlSerializer<MyGlobalLoddingSettings>))]
        public MyGlobalLoddingSettings GlobalLodding = MyGlobalLoddingSettings.Default;

        public MyNewPipelineSettings()
        {
            m_cascadeDepthLoddings = new MyPassLoddingSetting[0];
            m_blackListMaterials = new string[0];
            m_noShadowCasterMaterials = new string[0];
        }

        public void CopyFrom(MyNewPipelineSettings settings)
        {
            GlobalLodding = settings.GlobalLodding;
            GBufferLodding = settings.GBufferLodding;
            CascadeDepthLoddings = settings.CascadeDepthLoddings;
            BlackListMaterials = settings.BlackListMaterials;
            NoShadowCasterMaterials = settings.NoShadowCasterMaterials;
        }
    }
}
