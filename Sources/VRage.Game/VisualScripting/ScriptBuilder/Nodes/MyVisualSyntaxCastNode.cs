using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VRage.Game.VisualScripting.Utils;

namespace VRage.Game.VisualScripting.ScriptBuilder.Nodes
{
    public class MyVisualSyntaxCastNode : MyVisualSyntaxNode
    {
        private MyVisualSyntaxNode  m_nextSequenceNode;
        private MyVisualSyntaxNode  m_inputNode;

        public new MyObjectBuilder_CastScriptNode ObjectBuilder
        {
            get { return (MyObjectBuilder_CastScriptNode) m_objectBuilder; }
        }

        public MyVisualSyntaxCastNode(MyObjectBuilder_ScriptNode ob) : base(ob)
        {
            Debug.Assert(ob is MyObjectBuilder_CastScriptNode);
        }

        internal override void CollectInputExpressions(List<StatementSyntax> expressions)
        {
            base.CollectInputExpressions(expressions);

            expressions.Add(
                MySyntaxFactory.CastExpression(
                    m_inputNode.VariableSyntaxName(ObjectBuilder.InputID.VariableName),
                    ObjectBuilder.Type,
                    VariableSyntaxName())
                );
        }

        protected internal override string VariableSyntaxName(string variableIdentifier = null)
        {
            return "castResult_" + ObjectBuilder.ID;
        }

        protected internal override void Preprocess(int currentDepth)
        {
            if (!Preprocessed)
            {
                if (ObjectBuilder.SequenceOuputID != -1)
                {
                    // Collect expressions from children                
                    m_nextSequenceNode = Navigator.GetNodeByID(ObjectBuilder.SequenceOuputID);
                    Debug.Assert(m_nextSequenceNode != null);
                    SequenceOutputs.Add(m_nextSequenceNode);
                }

                var sequenceInputNode = Navigator.GetNodeByID(ObjectBuilder.SequenceInputID);
                Debug.Assert(sequenceInputNode != null);
                SequenceInputs.Add(sequenceInputNode);

                if (ObjectBuilder.InputID.NodeID == -1)
                    throw new Exception("Cast node has no input. NodeId: " + ObjectBuilder.ID);

                m_inputNode = Navigator.GetNodeByID(ObjectBuilder.InputID.NodeID);
                Debug.Assert(m_inputNode != null);
                Inputs.Add(m_inputNode);
            }

            base.Preprocess(currentDepth);
        }
    }
}
