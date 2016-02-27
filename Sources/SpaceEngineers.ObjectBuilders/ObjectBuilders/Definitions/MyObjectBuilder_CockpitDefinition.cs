using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using VRage.Data;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    public enum MyCockpitType
    {
        Closed, //First Person enabled, needs Interior and Glass model
        Open, //First Person disabled
        OpenFP //First Person enabled
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_CockpitDefinition : MyObjectBuilder_ShipControllerDefinition
    {
        [XmlIgnore]
        private string m_characterAnimation;

        [ModdableContentFile("mwm")]
        public string GlassModel;

        [ModdableContentFile("mwm")]
        public string InteriorModel;

        public string CharacterAnimation
        {
            get { return m_characterAnimation; }
            set
            {
                if (value.Contains(Path.AltDirectorySeparatorChar) || value.Contains(Path.DirectorySeparatorChar))
                {
                    CharacterAnimationFile = value;
                }
                else
                {
                    m_characterAnimation = value;
                }
            }
        }

        [XmlIgnore]
        [ModdableContentFile("mwm")]
        public string CharacterAnimationFile;

        [ProtoMember]
        public float OxygenCapacity;
        [ProtoMember]
        public bool IsPressurized;
    }
}
