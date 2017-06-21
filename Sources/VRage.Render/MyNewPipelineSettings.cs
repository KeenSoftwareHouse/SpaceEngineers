using System;
using System.Xml.Serialization;
using VRage;

namespace VRageRender
{
    // Do not add this to white list for modders!
    public struct MyPassLoddingSetting
    {
        public int LodShift;
        public int MinLod;

        [StructDefault]
        public static readonly MyPassLoddingSetting Default = new MyPassLoddingSetting();
    }

    // Do not add this to white list for modders!
    public struct MyGlobalLoddingSettings
    {
        public float ObjectDistanceAdd;
        public float ObjectDistanceMult;
        public double MaxDistanceForSmoothCameraMovement;
        public bool IsUpdateEnabled;
        public bool EnableLodSelection;
        public int LodSelection;

        [StructDefault]
        public static readonly MyGlobalLoddingSettings Default = new MyGlobalLoddingSettings
        {
            ObjectDistanceAdd = 0,
            ObjectDistanceMult = 1.0f,
            MaxDistanceForSmoothCameraMovement = 10,
            IsUpdateEnabled = true,
            EnableLodSelection = false,
            LodSelection = 0,
        };
    }

    public class MyNewLoddingSettings
    {
        public MyPassLoddingSetting GBuffer = MyPassLoddingSetting.Default;
        MyPassLoddingSetting[] m_cascadeDepth = new MyPassLoddingSetting[0];
        public MyPassLoddingSetting SingleDepth = MyPassLoddingSetting.Default;
        public MyGlobalLoddingSettings Global = MyGlobalLoddingSettings.Default;

        [XmlArrayItem("CascadeDepth")]
        public MyPassLoddingSetting[] CascadeDepths
        {
            get { return m_cascadeDepth; }
            set
            {
                if (m_cascadeDepth.Length != value.Length)
                    m_cascadeDepth = new MyPassLoddingSetting[value.Length];
                value.CopyTo(m_cascadeDepth, 0);
            }
        }

        public void CopyFrom(MyNewLoddingSettings settings)
        {
            GBuffer = settings.GBuffer;
            CascadeDepths = settings.CascadeDepths;
            SingleDepth = settings.SingleDepth;
            Global = settings.Global;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is MyNewLoddingSettings))
                return false;

            MyNewLoddingSettings theOther = (MyNewLoddingSettings) obj;
            if (GBuffer.Equals(theOther.GBuffer))
                return false;
            if (!CascadeDepths.Equals(theOther.CascadeDepths))
                return false;
            if (SingleDepth.Equals(theOther.SingleDepth))
                return false;
            if (Global.Equals(theOther.Global))
                return false;
            return true;
        }
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

        public MyNewPipelineSettings()
        {
            m_blackListMaterials = new string[0];
            m_noShadowCasterMaterials = new string[0];
        }

        public void CopyFrom(MyNewPipelineSettings settings)
        {
            BlackListMaterials = settings.BlackListMaterials;
            NoShadowCasterMaterials = settings.NoShadowCasterMaterials;
        }
    }
}
