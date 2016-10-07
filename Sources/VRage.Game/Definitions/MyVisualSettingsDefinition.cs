using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRage.Game.Definitions;
using VRageMath;
using VRageRender;

namespace VRage.Game
{
    /// <summary>
    /// Stripped environment definition with only visual settings
    /// </summary>
    [MyDefinitionType(typeof(MyObjectBuilder_VisualSettingsDefinition))]
    public class MyVisualSettingsDefinition : MyDefinitionBase
    {
        public MyVisualSettingsDefinition()
        {
            ShadowSettings = new MyShadowsSettings();
        }

        public MyFogProperties FogProperties = MyFogProperties.Default;
        public MySunProperties SunProperties = MySunProperties.Default;
        public MyPostprocessSettings PostProcessSettings = MyPostprocessSettings.Default;
        [XmlIgnore]
        public MyShadowsSettings ShadowSettings { get; private set; }

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            MyObjectBuilder_VisualSettingsDefinition objBuilder = (MyObjectBuilder_VisualSettingsDefinition)builder;
            FogProperties = objBuilder.FogProperties;
            SunProperties = objBuilder.SunProperties;
            PostProcessSettings = objBuilder.PostProcessSettings;
            ShadowSettings.CopyFrom(objBuilder.ShadowSettings);
        }

        public override MyObjectBuilder_DefinitionBase GetObjectBuilder()
        {
            var result = new MyObjectBuilder_VisualSettingsDefinition();
            result.FogProperties = FogProperties;
            result.SunProperties = SunProperties;
            result.PostProcessSettings = PostProcessSettings;
            result.ShadowSettings.CopyFrom(ShadowSettings);
            return result;
        }
    }
}
