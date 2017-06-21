using System.Collections.Generic;
using ProtoBuf;
using VRage.ObjectBuilders;
using VRageMath;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_VariableScriptNode : MyObjectBuilder_ScriptNode
    {        
        public string VariableName = "Default";
        
        public string VariableType = string.Empty;
        
        public string VariableValue = string.Empty;
        
        public List<MyVariableIdentifier> OutputNodeIds = new List<MyVariableIdentifier>();
        
        public Vector3D Vector;
        
        public List<MyVariableIdentifier> OutputNodeIdsX = new List<MyVariableIdentifier>();
        
        public List<MyVariableIdentifier> OutputNodeIdsY = new List<MyVariableIdentifier>();
        
        public List<MyVariableIdentifier> OutputNodeIdsZ = new List<MyVariableIdentifier>();
    }
}