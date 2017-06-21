using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VRage.Game.VisualScripting.Utils;

namespace VRage.Game.VisualScripting.ScriptBuilder.Nodes
{
    public class MyVisualSyntaxTriggerNode : MyVisualSyntaxNode
    {
        public new MyObjectBuilder_TriggerScriptNode ObjectBuilder
        {
            get { return (MyObjectBuilder_TriggerScriptNode) m_objectBuilder; }
        }

        public MyVisualSyntaxTriggerNode(MyObjectBuilder_ScriptNode ob) : base(ob)
        {
            Debug.Assert(ob is MyObjectBuilder_TriggerScriptNode);
        }

        internal override void CollectInputExpressions(List<StatementSyntax> expressions)
        {
            List<string> uniqueVariableIdentifiers = new List<string>();

            for (var i = 0; i < ObjectBuilder.InputNames.Count; i++)
            {
                uniqueVariableIdentifiers.Add(Inputs[i].VariableSyntaxName(ObjectBuilder.InputIDs[i].VariableName));
            }

            expressions.Add(
                    SyntaxFactory.ExpressionStatement(
                        MySyntaxFactory.MethodInvocation(ObjectBuilder.TriggerName, uniqueVariableIdentifiers)
                    )
                );

            base.CollectInputExpressions(expressions);
        }

        internal override void CollectSequenceExpressions(List<StatementSyntax> expressions)
        {
            CollectInputExpressions(expressions);
        }

        protected internal override void Preprocess(int currentDepth)
        {
            if (!Preprocessed)
            {
                if (ObjectBuilder.SequenceInputID != -1)
                {
                    var sequenceInputNode = Navigator.GetNodeByID(ObjectBuilder.SequenceInputID);
                    Debug.Assert(sequenceInputNode != null);
                    SequenceInputs.Add(sequenceInputNode);
                }

                for (var index = 0; index < ObjectBuilder.InputNames.Count; index++)
                {
                    if (ObjectBuilder.InputIDs[index].NodeID == -1)
                        throw new Exception("TriggerNode is missing an input of " + ObjectBuilder.InputNames[index] +
                                            " . NodeId: " + ObjectBuilder.ID);

                    var valueSupplierNode = Navigator.GetNodeByID(ObjectBuilder.InputIDs[index].NodeID);
                    Debug.Assert(valueSupplierNode != null);
                    Inputs.Add(valueSupplierNode);
                }
            }

            base.Preprocess(currentDepth);
        }
    }
}
