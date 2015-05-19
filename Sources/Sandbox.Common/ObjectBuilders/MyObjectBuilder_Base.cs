using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using ProtoBuf;
using ProtoBuf.Meta;
using Sandbox.Common.ObjectBuilders.Definitions;
using KeenSoftwareHouse.Library.IO;
using VRage;
using System.Xml;
using VRage.Utils;
using VRage.Plugins;
using Sandbox.Common.ObjectBuilders.Serializer;
using VRage.Utils;
using VRage.Library.Utils;

namespace Sandbox.Common.ObjectBuilders
{
    public class MyObjectBuilderDefinitionAttribute : MyFactoryTagAttribute
    {
        Type ObsoleteBy;

        public MyObjectBuilderDefinitionAttribute(Type obsoleteBy = null)
            : base(null)
        {
            ObsoleteBy = obsoleteBy;
        }
    }

    [ProtoContract]
    public abstract class MyObjectBuilder_Base
    {
        #region Fields

        [DefaultValue(0)]
        public MyStringId SubtypeId
        {
            get
            {
                return m_subtypeId;
            }
        }
        private MyStringId m_subtypeId;
        public bool ShouldSerializeSubtypeId() { return false; } // prevent serialization to XML

        [ProtoMember, DefaultValue(null)]
        public string SubtypeName
        {
            get { return m_subtypeName; }
            set
            {
                m_subtypeName = value;
                m_subtypeId = MyStringId.GetOrCompute(value);
            }
        }
        private string m_subtypeName = null;

        [XmlIgnore]
        public MyObjectBuilderType TypeId
        {
            get { return GetType(); }
        }

        #endregion

        #region Clone

        public MyObjectBuilder_Base Clone()
        {
            return Sandbox.Common.ObjectBuilders.Serializer.MyObjectBuilderSerializer.Clone(this);
        }

        #endregion

    }
}
