using System;
using System.ComponentModel;
using System.Xml.Serialization;
using ProtoBuf;
using VRage.Utils;
using VRage.Serialization;
using VRage.Game.Common;

namespace VRage.ObjectBuilders
{
    public class MyObjectBuilderDefinitionAttribute : MyFactoryTagAttribute
    {
        Type ObsoleteBy;
        public readonly string LegacyName;

        public MyObjectBuilderDefinitionAttribute(Type obsoleteBy = null, string LegacyName = null)
            : base(null)
        {
            ObsoleteBy = obsoleteBy;
            this.LegacyName = LegacyName;
        }
    }

    [ProtoContract]
    public abstract class MyObjectBuilder_Base
    {
        #region Fields

        [DefaultValue(0)]
        public MyStringHash SubtypeId
        {
            get
            {
                return m_subtypeId;
            }
        }
        private MyStringHash m_subtypeId;
        public bool ShouldSerializeSubtypeId() { return false; } // prevent serialization to XML

        [Serialize]
        private MyStringHash m_serializableSubtypeId
        {
            get { return m_subtypeId; }
            set { m_subtypeId = value; m_subtypeName = value.String; }
        }

        [ProtoMember, DefaultValue(null)]
        [NoSerialize]
        public string SubtypeName
        {
            get { return m_subtypeName; }
            set
            {
                m_subtypeName = value;
                m_subtypeId = MyStringHash.GetOrCompute(value);
            }
        }
        private string m_subtypeName = null;

        [XmlIgnore]
        public MyObjectBuilderType TypeId
        {
            get { return GetType(); }
        }

        #endregion

        public void Save(string filepath)
        {
            MyObjectBuilderSerializer.SerializeXML(filepath, false, this);
        }

        virtual public MyObjectBuilder_Base Clone()
        {
            return MyObjectBuilderSerializer.Clone(this);
        }
    }
}
