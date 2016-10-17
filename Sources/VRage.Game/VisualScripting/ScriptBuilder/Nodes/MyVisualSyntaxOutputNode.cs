using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VRage.Game.VisualScripting.Utils;

namespace VRage.Game.VisualScripting.ScriptBuilder.Nodes
{
    /// <summary>
    /// Output node that fills in the output values of a method.
    /// </summary>
    public class MyVisualSyntaxOutputNode : MyVisualSyntaxNode
    {
        private readonly List<MyVisualSyntaxNode> m_inputNodes = new List<MyVisualSyntaxNode>();

        public new MyObjectBuilder_OutputScriptNode ObjectBuilder
        {
            get { return (MyObjectBuilder_OutputScriptNode) m_objectBuilder; }
        }

        public MyVisualSyntaxOutputNode(MyObjectBuilder_ScriptNode ob) : base(ob)
        {
            Debug.Assert(ob is MyObjectBuilder_OutputScriptNode);
        }

        internal override void Reset()
        {
            base.Reset();
            m_inputNodes.Clear();
        }

        internal override void CollectInputExpressions(List<StatementSyntax> expressions)
        {
            base.CollectInputExpressions(expressions);

            // just for the sake of adding the assignments all at once after the 
            // value supplier operations.
            List<StatementSyntax> assignments = new List<StatementSyntax>(ObjectBuilder.Inputs.Count);

            for(int index = 0; index < ObjectBuilder.Inputs.Count; index++)
            {
                var valueInputNode = m_inputNodes[index];

                //valueSupplierNode.CollectInputExpressions(expressions, this);
                var inputVarName = valueInputNode.VariableSyntaxName(ObjectBuilder.Inputs[index].Input.VariableName);
                var assignment = MySyntaxFactory.SimpleAssignment(ObjectBuilder.Inputs[index].Name, SyntaxFactory.IdentifierName(inputVarName));
                assignments.Add(assignment);
            }

            // Add "return true;"
            expressions.Add(SyntaxFactory.ReturnStatement(SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)));
            expressions.AddRange(assignments);
        }

        protected internal override void Preprocess(int currentDepth)
        {
            if(!Preprocessed)
            {
                if (ObjectBuilder.SequenceInputID != -1)
                {
                    var sequenceInputNode = Navigator.GetNodeByID(ObjectBuilder.SequenceInputID);
                    Debug.Assert(sequenceInputNode != null);
                    SequenceInputs.Add(sequenceInputNode);
                }

                foreach (var inputData in ObjectBuilder.Inputs)
                {
                    if (inputData.Input.NodeID != -1)
                    {
                        var valueInputNode = Navigator.GetNodeByID(inputData.Input.NodeID);

                        Debug.Assert(valueInputNode != null);
                        m_inputNodes.Add(valueInputNode);
                        Inputs.Add(valueInputNode);
                    }
                    else
                        throw new Exception("Output node missing input for " + inputData.Name + ". NodeID: " + ObjectBuilder.ID);
                }
            }

            base.Preprocess(currentDepth);
        }
    }
}
