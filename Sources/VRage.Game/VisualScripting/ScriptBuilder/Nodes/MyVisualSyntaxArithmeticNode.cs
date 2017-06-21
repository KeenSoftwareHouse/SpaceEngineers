using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VRage.Game.VisualScripting.Utils;

namespace VRage.Game.VisualScripting.ScriptBuilder.Nodes
{
    public class MyVisualSyntaxArithmeticNode : MyVisualSyntaxNode
    {
        private MyVisualSyntaxNode m_inputANode;
        private MyVisualSyntaxNode m_inputBNode;

        public new MyObjectBuilder_ArithmeticScriptNode ObjectBuilder
        {
            get { return (MyObjectBuilder_ArithmeticScriptNode)m_objectBuilder; }
        }

        internal override bool SequenceDependent { get { return false; } }

        public MyVisualSyntaxArithmeticNode(MyObjectBuilder_ScriptNode ob) : base(ob)
        {
            Debug.Assert(ob is MyObjectBuilder_ArithmeticScriptNode);
        }

        internal override void CollectInputExpressions(List<StatementSyntax> expressions)
        {
            // Base will traverse to input nodes
            base.CollectInputExpressions(expressions);

            string firstVariableName = null, secondVariableName = null;

            // Save names of variables
            firstVariableName = m_inputANode.VariableSyntaxName(ObjectBuilder.InputAID.VariableName);
            secondVariableName = m_inputBNode != null ? m_inputBNode.VariableSyntaxName(ObjectBuilder.InputBID.VariableName) : "null";

            if (m_inputBNode == null && ObjectBuilder.Operation != "==" && ObjectBuilder.Operation != "!=")
                throw new Exception("Null check with Operation " + ObjectBuilder.Operation + " is prohibited.");

            // Create expression for arithmetic operation
            Debug.Assert(!string.IsNullOrEmpty(firstVariableName) && !string.IsNullOrEmpty(secondVariableName));
            expressions.Add(
                MySyntaxFactory.ArithmeticStatement(
                    VariableSyntaxName(),
                    firstVariableName,
                    secondVariableName,
                    ObjectBuilder.Operation)
                );

        }

        protected internal override string VariableSyntaxName(string variableIdentifier = null)
        {
            return "arithmeticResult_" + m_objectBuilder.ID;
        }

        protected internal override void Preprocess(int currentDepth)
        {
            if(!Preprocessed)
            {
                // Read the Input A data
                if (ObjectBuilder.InputAID.NodeID == -1)
                    throw new Exception("Missing inputA in arithmetic node: " + ObjectBuilder.ID);

                m_inputANode = Navigator.GetNodeByID(ObjectBuilder.InputAID.NodeID);
                Debug.Assert(m_inputANode != null);

                // Read the Input B data
                if (ObjectBuilder.InputBID.NodeID != -1)
                {
                    m_inputBNode = Navigator.GetNodeByID(ObjectBuilder.InputBID.NodeID);
                    if (m_inputBNode == null)
                    {
                        throw new Exception("Missing inputB in arithmetic node: " + ObjectBuilder.ID);
                    }
                }

                // Fill in the Output data
                for(int index = 0; index < ObjectBuilder.OutputNodeIDs.Count; index++)
                {
                    if (ObjectBuilder.OutputNodeIDs[index].NodeID == -1)
                        throw new Exception("-1 output in arithmetic node: " + ObjectBuilder.ID);

                    var outputNode = Navigator.GetNodeByID(ObjectBuilder.OutputNodeIDs[index].NodeID);
                    Debug.Assert(outputNode != null);

                    Outputs.Add(outputNode);
                }

                // Fill in the Input nodes
                Inputs.Add(m_inputANode);
                // B is not mandatory (null)
                if(m_inputBNode != null)
                    Inputs.Add(m_inputBNode);
            }

            // Base call will take care of the rest
            base.Preprocess(currentDepth);
        }
    }
}
