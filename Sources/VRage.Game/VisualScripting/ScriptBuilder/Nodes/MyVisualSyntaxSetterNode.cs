using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VRage.Game.VisualScripting.Utils;
using VRageMath;

namespace VRage.Game.VisualScripting.ScriptBuilder.Nodes
{
    public class MyVisualSyntaxSetterNode : MyVisualSyntaxNode
    {
        private string              m_inputVariableName;
        private MyVisualSyntaxNode  m_inputNode;

        internal override bool SequenceDependent
        {
            get { return true; }
        }

        public new MyObjectBuilder_VariableSetterScriptNode ObjectBuilder
        {
            get { return (MyObjectBuilder_VariableSetterScriptNode) m_objectBuilder; }
        }

        public MyVisualSyntaxSetterNode(MyObjectBuilder_ScriptNode ob) : base(ob)
        {
            Debug.Assert(ob is MyObjectBuilder_VariableSetterScriptNode);
        }

        private StatementSyntax GetCorrectAssignmentsExpression()
        {
            // Determine Type and create correct assingment
            var boundVariableNode = Navigator.GetVariable(ObjectBuilder.VariableName);
            Debug.Assert(boundVariableNode != null, "Missing variable: " + ObjectBuilder.VariableName);

            var type = MyVisualScriptingProxy.GetType(boundVariableNode.ObjectBuilder.VariableType);

            if (type == typeof(string))
            {
                if (ObjectBuilder.ValueInputID.NodeID == -1)
                    return MySyntaxFactory.VariableAssignmentExpression(ObjectBuilder.VariableName, ObjectBuilder.VariableValue, SyntaxKind.StringLiteralExpression);
            }
            else if (type == typeof(Vector3D))
            {
                if (ObjectBuilder.ValueInputID.NodeID == -1)
                {
                    return MySyntaxFactory.SimpleAssignment(ObjectBuilder.VariableName, MySyntaxFactory.NewVector3D(ObjectBuilder.VariableValue));
                }
            }
            else if (type == typeof(bool))
            {
                if (ObjectBuilder.ValueInputID.NodeID == -1)
                {
                    var normalizedValue = MySyntaxFactory.NormalizeBool(ObjectBuilder.VariableValue);
                    var syntaxKind = normalizedValue == "true" ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression;
                    return MySyntaxFactory.VariableAssignmentExpression(
                        ObjectBuilder.VariableName, ObjectBuilder.VariableValue, syntaxKind);
                }
            }
            else
            {
                if (ObjectBuilder.ValueInputID.NodeID == -1)
                    return MySyntaxFactory.VariableAssignmentExpression(ObjectBuilder.VariableName, ObjectBuilder.VariableValue, SyntaxKind.NumericLiteralExpression);
            }

            return MySyntaxFactory.SimpleAssignment(ObjectBuilder.VariableName, SyntaxFactory.IdentifierName(m_inputVariableName));
        }

        internal override void CollectInputExpressions(List<StatementSyntax> expressions)
        {
            base.CollectInputExpressions(expressions);

            if (ObjectBuilder.ValueInputID.NodeID != -1)
            {
                m_inputVariableName = m_inputNode.VariableSyntaxName(ObjectBuilder.ValueInputID.VariableName);
                Debug.Assert(!string.IsNullOrEmpty(m_inputVariableName));
            }

            expressions.Add(GetCorrectAssignmentsExpression());
        }

        protected internal override void Preprocess(int currentDepth)
        {
            if(!Preprocessed)
            {
                if (ObjectBuilder.SequenceOutputID != -1)
                {
                    var nextSequenceNode = Navigator.GetNodeByID(ObjectBuilder.SequenceOutputID);
                    Debug.Assert(nextSequenceNode != null);
                    SequenceOutputs.Add(nextSequenceNode);
                }

                if(ObjectBuilder.SequenceInputID != -1)
                {
                    var sequenceInputNode = Navigator.GetNodeByID(ObjectBuilder.SequenceInputID);
                    Debug.Assert(sequenceInputNode != null);
                    SequenceInputs.Add(sequenceInputNode);
                }

                if (ObjectBuilder.ValueInputID.NodeID != -1)
                {
                    m_inputNode = Navigator.GetNodeByID(ObjectBuilder.ValueInputID.NodeID);
                    Debug.Assert(m_inputNode != null);
                    Inputs.Add(m_inputNode);
                }
            }

            base.Preprocess(currentDepth);
        }
    }
}
