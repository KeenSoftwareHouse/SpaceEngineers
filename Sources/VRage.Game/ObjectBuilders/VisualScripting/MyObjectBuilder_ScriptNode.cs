using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using ProtoBuf;
using VRage.Game.VisualScripting;
using VRage.ObjectBuilders;
using VRage.Serialization;
using VRageMath;

namespace VRage.Game
{
    public class MyOutputParameterSerializationData
    {        
        public string Type;

        public string Name;
        
        public IdentifierList Outputs;

        public MyOutputParameterSerializationData()
        {
            Outputs.Ids = new List<MyVariableIdentifier>();
        }
    }

    public class MyInputParameterSerializationData
    {        
        public string Type;

        public string Name;
        
        public MyVariableIdentifier Input;

        public MyInputParameterSerializationData()
        {
            Input = MyVariableIdentifier.Default;
        }
    }

    public struct MyVariableIdentifier
    {        
        public int NodeID; 
        public string VariableName;

        public string OriginName;
        public string OriginType;

        [NoSerialize]
        public static MyVariableIdentifier Default = new MyVariableIdentifier{NodeID = -1, VariableName = ""};

        public MyVariableIdentifier(int nodeId, string variableName)
        {
            NodeID = nodeId;
            VariableName = variableName;
            OriginName = String.Empty;
            OriginType = String.Empty;
        }

        public MyVariableIdentifier(int nodeId, string variableName, ParameterInfo parameter) : this(nodeId, variableName)
        {
            OriginName = parameter.Name;
            OriginType = parameter.ParameterType.Signature();
        }

        public MyVariableIdentifier(ParameterInfo parameter) : this(-1, String.Empty, parameter)
        {
        }

        public override bool Equals(object obj)
        {
            if (!(obj is MyVariableIdentifier))
                return false;

            var identifier = (MyVariableIdentifier)obj;

            return NodeID == identifier.NodeID && VariableName == identifier.VariableName;
        }
    }

    public class MyParameterValue
    {
        public string ParameterName;
        public string Value;

        public MyParameterValue() { ParameterName = String.Empty; Value = String.Empty; }
        public MyParameterValue(string paramName) { ParameterName = paramName; }
    }

    public struct IdentifierList
    {
        public string OriginName;
        public string OriginType;
        public List<MyVariableIdentifier> Ids;
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ScriptNode : MyObjectBuilder_Base
    {
        [DefaultValue(-1)]
        public int ID;

        public Vector2 Position;
    }
}
