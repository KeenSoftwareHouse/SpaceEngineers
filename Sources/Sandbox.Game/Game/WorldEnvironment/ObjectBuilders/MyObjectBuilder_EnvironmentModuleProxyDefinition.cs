using System.Xml.Serialization;
using VRage.Game;

namespace Sandbox.Game.WorldEnvironment.ObjectBuilders
{
    [XmlType("VR.EnvironmentModuleProxy")]
    public class MyObjectBuilder_EnvironmentModuleProxyDefinition: MyObjectBuilder_DefinitionBase
    {
        public string QualifiedTypeName;
    }
}
