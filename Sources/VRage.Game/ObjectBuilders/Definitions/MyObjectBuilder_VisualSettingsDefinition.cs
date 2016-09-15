using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRage.Data;
using VRage.ObjectBuilders;
using VRageMath;
using VRageRender;

namespace VRage.Game
{
    /// <summary>
    /// Stripped environment definition with only visual settings
    /// </summary>
    [MyObjectBuilderDefinition]
    [XmlType("VisualSettingsDefinition")]
    public class MyObjectBuilder_VisualSettingsDefinition : MyObjectBuilder_DefinitionBase
    {
        [XmlElement(Type = typeof(MyStructXmlSerializer<MyFogProperties>))]
        public MyFogProperties FogProperties = MyFogProperties.Default;

        [XmlElement(Type = typeof(MyStructXmlSerializer<MySunProperties>))]
        public MySunProperties SunProperties = MySunProperties.Default;

        [XmlElement(Type = typeof(MyStructXmlSerializer<MyPostprocessSettings>))]
        public MyPostprocessSettings PostProcessSettings = MyPostprocessSettings.Default;

        public MyShadowsSettings ShadowSettings = new MyShadowsSettings();
    }
}
