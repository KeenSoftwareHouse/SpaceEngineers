using System.Xml.Serialization;
using VRage.Game;
using VRage.ObjectBuilders;
using VRage.Serialization;
using VRage;

namespace Sandbox.Game.WorldEnvironment.ObjectBuilders
{
    public class MyObjectBuilder_ProceduralEnvironmentSector : MyObjectBuilder_EnvironmentSector
    {
        public struct Module
        {
            public SerializableDefinitionId ModuleId;

            [Serialize(MyObjectFlags.Dynamic, typeof(MyObjectBuilderDynamicSerializer))]
            [XmlElement(typeof(MyAbstractXmlSerializer<MyObjectBuilder_EnvironmentModuleBase>))]
            public MyObjectBuilder_EnvironmentModuleBase Builder;
        }

        public Module[] SavedModules;
    }
}
