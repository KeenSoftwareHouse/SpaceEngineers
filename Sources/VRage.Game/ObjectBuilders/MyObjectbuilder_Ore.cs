using VRage.ObjectBuilders;
using ProtoBuf;
using VRage.Serialization;
using VRage.Utils;
using System.Xml.Serialization;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Ore : MyObjectBuilder_PhysicalObject
    {
        [Nullable]
        [XmlIgnore]
        //string hash is for mulitplayer synchonizations
        public MyStringHash? MaterialTypeName;

        [NoSerialize]
        //string is for saving on disk
        public string MaterialNameString
        {
            get
            {
                if (MaterialTypeName.HasValue && MaterialTypeName.Value.GetHashCode() != 0)
                {
                    return MaterialTypeName.Value.String;
                }
                return m_materialName;
            }
            set
            {
                m_materialName = value;
            }
        }

        [XmlIgnore]
        [NoSerialize]
        string m_materialName;

        public string GetMaterialName()
        {
            if (false == string.IsNullOrEmpty(m_materialName))
            {
                return m_materialName;
            }
            if(MaterialTypeName.HasValue)
            {
                return MaterialTypeName.Value.String;
            }

            return string.Empty;
        }

        public bool HasMaterialName()
        {
            return (false == string.IsNullOrEmpty(m_materialName)) || (MaterialTypeName.HasValue && MaterialTypeName.Value.GetHashCode() != 0);
        }

        override public MyObjectBuilder_Base Clone()
        {
            MyObjectBuilder_Ore clone = MyObjectBuilderSerializer.Clone(this) as MyObjectBuilder_Ore;
            clone.MaterialTypeName = MaterialTypeName;
            return clone;

        }
    }
}
