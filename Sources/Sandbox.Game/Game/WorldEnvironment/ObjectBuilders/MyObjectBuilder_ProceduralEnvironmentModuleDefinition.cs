using System.Xml.Serialization;
using VRage.Game;

namespace Sandbox.Game.WorldEnvironment.ObjectBuilders
{
    [XmlType("VR.ProceduralEnvironmentModule")]
    public class MyObjectBuilder_ProceduralEnvironmentModuleDefinition: MyObjectBuilder_DefinitionBase
    {
        public string QualifiedTypeName;
    }
}
