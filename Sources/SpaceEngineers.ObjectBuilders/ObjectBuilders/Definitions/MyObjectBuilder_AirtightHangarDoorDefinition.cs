using System;
using System.Collections.Generic;
using System.Linq;
using VRage.ObjectBuilders;
using System.Text;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_AirtightHangarDoorDefinition : MyObjectBuilder_AirtightDoorGenericDefinition
    {
    }
}
