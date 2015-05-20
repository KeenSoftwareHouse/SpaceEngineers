using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Voxels;
using Sandbox.Common.ObjectBuilders.VRageData;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [XmlRoot("ScenarioDefinitions")]
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ScenarioDefinitions : MyObjectBuilder_Base
    {
        [XmlArrayItem("ScenarioDefinition")]
        [ProtoMember]
        public MyObjectBuilder_ScenarioDefinition[] Scenarios;
    }
}
