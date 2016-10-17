using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VRage.Game.VisualScripting.Utils;

namespace VRage.Game.VisualScripting.ScriptBuilder.Nodes
{
    public class MyVisualSyntaxBranchingNode : MyVisualSyntaxNode
    {
        private             MyVisualSyntaxNode                      m_nextTrueSequenceNode;
        private             MyVisualSyntaxNode                      m_nextFalseSequenceNode;
        private             MyVisualSyntaxNode                      m_comparerNode;

        public new MyObjectBuilder_BranchingScriptNode ObjectBuilder
        {
            get { return (MyObjectBuilder_BranchingScriptNode)m_objectBuilder; }
        }

        public MyVisualSyntaxBranchingNode(MyObjectBuilder_ScriptNode ob) : base(ob)
        {
            Debug.Assert(ob is MyObjectBuilder_BranchingScriptNode);
        }

        internal override void CollectSequenceExpressions(List<StatementSyntax> expressions)
        {
            // Collect mine input expressions and pushed expressions
            CollectInputExpressions(expressions);

            var trueStatments = new List<StatementSyntax>();
            var falseStatments = new List<StatementSyntax>();

            // Sequence outputs can be connected only to one input
            if (m_nextTrueSequenceNode != null)                
                m_nextTrueSequenceNode.CollectSequenceExpressions(trueStatments);

            if (m_nextFalseSequenceNode != null)
                m_nextFalseSequenceNode.CollectSequenceExpressions(falseStatments);

            var inputNodeVariableName = m_comparerNode.VariableSyntaxName(ObjectBuilder.InputID.VariableName);

            // write my expression before the collected inputs
            expressions.Add(MySyntaxFactory.IfExpressionSyntax(inputNodeVariableName, trueStatments, falseStatments));
        }

        protected internal override void Preprocess(int currentDepth)
        {
            if (!Preprocessed)
            {
                // Load true sequence node data
                if (ObjectBuilder.SequenceTrueOutputID != -1)
                {
                    // Sequence outputs can be connected only to one input
                    m_nextTrueSequenceNode = Navigator.GetNodeByID(ObjectBuilder.SequenceTrueOutputID);
                    Debug.Assert(m_nextTrueSequenceNode != null);
                    SequenceOutputs.Add(m_nextTrueSequenceNode);
                }

                // Load false sequence node data
                if (ObjectBuilder.SequnceFalseOutputID != -1)
                {
                    m_nextFalseSequenceNode = Navigator.GetNodeByID(ObjectBuilder.SequnceFalseOutputID);
                    Debug.Assert(m_nextFalseSequenceNode != null);
                    SequenceOutputs.Add(m_nextFalseSequenceNode);
                }

                // Load the sequence input
                if (ObjectBuilder.SequenceInputID != -1)
                {
                    var inputNode = Navigator.GetNodeByID(ObjectBuilder.SequenceInputID);
                    Debug.Assert(inputNode != null);
                    SequenceInputs.Add(inputNode);
                }

                // Load the input node data
                if (ObjectBuilder.InputID.NodeID == -1)
                    throw new Exception("Branching node has no comparer input. NodeID: " + ObjectBuilder.ID);

                m_comparerNode = Navigator.GetNodeByID(ObjectBuilder.InputID.NodeID);
                Debug.Assert(m_comparerNode != null);
                Inputs.Add(m_comparerNode);
            }

            base.Preprocess(currentDepth);
        }
    }
}
