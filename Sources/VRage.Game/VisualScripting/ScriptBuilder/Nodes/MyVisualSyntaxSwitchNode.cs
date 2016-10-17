using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VRage.Game.ObjectBuilders.VisualScripting;
using VRage.Game.VisualScripting.Utils;

namespace VRage.Game.VisualScripting.ScriptBuilder.Nodes
{
    public class MyVisualSyntaxSwitchNode : MyVisualSyntaxNode
    {
        private readonly List<MyVisualSyntaxNode> m_sequenceOutputs = new List<MyVisualSyntaxNode>();  
        private MyVisualSyntaxNode m_valueInput;

        public new MyObjectBuilder_SwitchScriptNode ObjectBuilder
        {
            get { return (MyObjectBuilder_SwitchScriptNode)m_objectBuilder; }
        }

        public MyVisualSyntaxSwitchNode(MyObjectBuilder_ScriptNode ob) : base(ob)
        {
            Debug.Assert(ob is MyObjectBuilder_SwitchScriptNode);
        }

        internal override void CollectSequenceExpressions(List<StatementSyntax> expressions)
        {
            CollectInputExpressions(expressions);

            var valueVariableName = m_valueInput.VariableSyntaxName(ObjectBuilder.ValueInput.VariableName);

            var switchSections = new List<SwitchSectionSyntax>();
            var syntaxList = new List<StatementSyntax>();
            for (int index = 0; index < m_sequenceOutputs.Count; index++)
            {
                var sequenceOutputNode = m_sequenceOutputs[index];
                if (sequenceOutputNode == null) continue;

                syntaxList.Clear();
                // Collect
                sequenceOutputNode.CollectSequenceExpressions(syntaxList);
                // Add break to the end of the section
                syntaxList.Add(SyntaxFactory.BreakStatement());

                // New section syntax
                var section = SyntaxFactory.SwitchSection().WithLabels(
                    SyntaxFactory.SingletonList<SwitchLabelSyntax>(
                        SyntaxFactory.CaseSwitchLabel(
                            MySyntaxFactory.Literal(ObjectBuilder.NodeType, ObjectBuilder.Options[index].Option)
                            )
                        )).WithStatements(SyntaxFactory.List(syntaxList));

                switchSections.Add(section);
            }

            var switchSyntax = SyntaxFactory.SwitchStatement(
                    SyntaxFactory.IdentifierName(valueVariableName)
                )
                .WithSections(SyntaxFactory.List(switchSections));

            expressions.Add(switchSyntax);
        }

        internal override void Reset()
        {
            base.Reset();

            m_sequenceOutputs.Clear();
        }

        protected internal override void Preprocess(int currentDepth)
        {
            if (!Preprocessed)
            {
                TryRegisterNode(ObjectBuilder.SequenceInput, SequenceInputs);
                m_valueInput = TryRegisterNode(ObjectBuilder.ValueInput.NodeID, Inputs);

                // Dont add Sequence outputs to general structures
                // and handle them separately.
                foreach (var optionData in ObjectBuilder.Options)
                {
                    if(optionData.SequenceOutput == -1)
                        continue;

                    var node = Navigator.GetNodeByID(optionData.SequenceOutput);
                    if (node != null)
                        m_sequenceOutputs.Add(node);
                }

                // Manually preprocess custom handled nodes
                m_sequenceOutputs.ForEach(node => node.Preprocess(currentDepth + 1));
            }

            base.Preprocess(currentDepth);
        }
    }
}
