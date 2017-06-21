using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VRage.Game.ObjectBuilders.VisualScripting;
using VRage.Game.VisualScripting.Utils;
using VRage.Utils;

namespace VRage.Game.VisualScripting.ScriptBuilder.Nodes
{
    public class MyVisualSyntaxLocalizationNode : MyVisualSyntaxNode
    { 
        private readonly List<MyVisualSyntaxNode> m_inputParameterNodes = new List<MyVisualSyntaxNode>(); 

        public new MyObjectBuilder_LocalizationScriptNode ObjectBuilder
        {
            get { return (MyObjectBuilder_LocalizationScriptNode)m_objectBuilder; }
        }

        internal override bool SequenceDependent
        {
            get { return false; }
        }

        public MyVisualSyntaxLocalizationNode(MyObjectBuilder_ScriptNode ob) : base(ob)
        {
            Debug.Assert(ob is MyObjectBuilder_LocalizationScriptNode);
        }

        internal override void Reset()
        {
            base.Reset();

            m_inputParameterNodes.Clear();
        }

        protected internal override string VariableSyntaxName(string variableIdentifier = null)
        {
            return "localizationNode_" + ObjectBuilder.ID;
        }

        internal override void CollectInputExpressions(List<StatementSyntax> expressions)
        {
            base.CollectInputExpressions(expressions);

            // MyLocalization.Static["context", "messageId"];
            var localizationAccessExpression = SyntaxFactory.ElementAccessExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("VRage.Game.Localization.MyLocalization"),
                    SyntaxFactory.IdentifierName("Static")
                    )
                ).WithArgumentList(
                    SyntaxFactory.BracketedArgumentList(
                        SyntaxFactory.SeparatedList<ArgumentSyntax>(
                            new SyntaxNodeOrToken[]
                            {
                                SyntaxFactory.Argument(
                                    MySyntaxFactory.Literal(typeof(string).Signature(), ObjectBuilder.Context)),
                                SyntaxFactory.Token(SyntaxKind.CommaToken),
                                SyntaxFactory.Argument(
                                    MySyntaxFactory.Literal(typeof(string).Signature(), ObjectBuilder.MessageId))
                            })
                        )
                );

            // ToString() invocation expression
            var toStringExpression = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression, 
                        localizationAccessExpression,
                        SyntaxFactory.IdentifierName("ToString")
                    )
                );

            // String tmpVariableName = the above
            string tmpVariableName = "localizedText_" + ObjectBuilder.ID;

            if(m_inputParameterNodes.Count == 0)
                tmpVariableName = VariableSyntaxName();

            var localizedTextVariableSyntax = MySyntaxFactory.LocalVariable(typeof(string).Signature(),
                tmpVariableName, toStringExpression);

            if(m_inputParameterNodes.Count > 0)
            {
                // Prepare variable names for creation of string.Format call
                var parameterVariableNames = new List<string>();
                parameterVariableNames.Add(tmpVariableName);
                for (int index = 0; index < m_inputParameterNodes.Count; index++)
                {
                    var node = m_inputParameterNodes[index];
                    parameterVariableNames.Add(node.VariableSyntaxName(ObjectBuilder.ParameterInputs[index].VariableName));
                }

                // string.Format(....) call
                var stringFormatSyntax = MySyntaxFactory.MethodInvocation("Format", parameterVariableNames, "string");
                // node variable syntax creation
                var nodeVariableSyntax = MySyntaxFactory.LocalVariable(typeof(string).Signature(), VariableSyntaxName(), stringFormatSyntax);

                expressions.Add(localizedTextVariableSyntax);
                expressions.Add(nodeVariableSyntax);
            }
            else
            {
                expressions.Add(localizedTextVariableSyntax);
            }
        }

        protected internal override void Preprocess(int currentDepth)
        {
            if (!Preprocessed)
            {
                // Fill in Outputs of the node
                foreach (var identifier in ObjectBuilder.ValueOutputs)
                {
                    TryRegisterNode(identifier.NodeID, Outputs);
                }

                // Fill in the inputs of the node
                foreach (var identifier in ObjectBuilder.ParameterInputs)
                {
                    var node = TryRegisterNode(identifier.NodeID, Inputs);
                    if (node != null)
                    {
                        // Store the inputs -- because variable names
                        m_inputParameterNodes.Add(node);
                    }
                }
            }

            base.Preprocess(currentDepth);
        }
    }
}
