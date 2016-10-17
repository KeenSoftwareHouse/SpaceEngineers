using System;
using System.Collections.Generic;
using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ArithmeticScriptNode : MyObjectBuilder_ScriptNode
    {
        
        public List<MyVariableIdentifier> OutputNodeIDs = new List<MyVariableIdentifier>();
       
        public string Operation;
       
        public string Type;
      
        public MyVariableIdentifier InputAID = MyVariableIdentifier.Default;
       
        public MyVariableIdentifier InputBID = MyVariableIdentifier.Default;
       
        public string ValueA = String.Empty;
       
        public string ValueB = String.Empty;
    }
}