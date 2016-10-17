using System.Collections.Generic;
using ProtoBuf;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_VisualScript : MyObjectBuilder_Base
    {
        [Nullable]
        public string Interface;

        public List<string> DependencyFilePaths;

        public List<MyObjectBuilder_ScriptNode> Nodes = new List<MyObjectBuilder_ScriptNode>();
       
        public string Name;
    }
}