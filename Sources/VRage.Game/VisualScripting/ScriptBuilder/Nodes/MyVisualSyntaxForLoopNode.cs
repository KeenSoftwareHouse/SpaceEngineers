using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VRage.Game.ObjectBuilders.VisualScripting;
using VRage.Game.VisualScripting.Utils;

namespace VRage.Game.VisualScripting.ScriptBuilder.Nodes
{
    /// <summary>
    /// Sequence dependent node that creates syntax for For loops
    /// with support for custom initial index, increment and last index.
    /// </summary>
    public class MyVisualSyntaxForLoopNode : MyVisualSyntaxNode
    {
        private MyVisualSyntaxNode m_bodySequence; 
        private MyVisualSyntaxNode m_finishSequence;
        private MyVisualSyntaxNode m_firstInput;
        private MyVisualSyntaxNode m_lastInput;
        private MyVisualSyntaxNode m_incrementInput;

        private readonly List<MyVisualSyntaxNode> m_toCollectNodeCache = new List<MyVisualSyntaxNode>();

        public new MyObjectBuilder_ForLoopScriptNode ObjectBuilder
        {
            get { return (MyObjectBuilder_ForLoopScriptNode)m_objectBuilder; }
        }

        public MyVisualSyntaxForLoopNode(MyObjectBuilder_ScriptNode ob) : base(ob)
        {
            Debug.Assert(ob is MyObjectBuilder_ForLoopScriptNode);
        }

        protected internal override string VariableSyntaxName(string variableIdentifier = null)
        {
            return "forEach_" + ObjectBuilder.ID + "_counter";
        }

        internal override void CollectSequenceExpressions(List<StatementSyntax> expressions)
        {
            // Collect input only from these nodes that are inputs for this node.
            m_toCollectNodeCache.Clear();
            foreach (var subTreeNode in SubTreeNodes)
            {
                var directConnection = false;
                foreach (var outputNode in subTreeNode.Outputs)
                {
                    // Must be an input
                    if (outputNode == this && !outputNode.Collected)
                    {
                        directConnection = true;
                        break;
                    }
                }

                if (directConnection)
                {
                    subTreeNode.CollectInputExpressions(expressions);
                }
                else
                {
                    // Cache these that are getting the input from this one.
                    m_toCollectNodeCache.Add(subTreeNode);
                }
            }

            // We can ignore the nodes with empty bodies
            if (m_bodySequence != null)
            {
                var bodySyntax = new List<StatementSyntax>();

                // Collect the assigned bodies from 
                foreach (var node in m_toCollectNodeCache)
                {
                    node.CollectInputExpressions(bodySyntax);
                }

                // Collect body expressions
                m_bodySequence.CollectSequenceExpressions(bodySyntax);

                // Create syntax for first index of for loop
                ExpressionSyntax firstIndexExpression;
                if(m_firstInput != null)
                {
                    firstIndexExpression = SyntaxFactory.IdentifierName(
                        m_firstInput.VariableSyntaxName(ObjectBuilder.FirstIndexValueInput.VariableName)
                        );
                } else
                {
                     firstIndexExpression = MySyntaxFactory.Literal(
                                                    typeof (int).Signature(),
                                                    ObjectBuilder.FirstIndexValue
                                                    );
                }

                // Create syntax for last index condition
                ExpressionSyntax lastIndexExpression;
                if(m_lastInput != null)
                {
                    lastIndexExpression = SyntaxFactory.IdentifierName(
                        m_lastInput.VariableSyntaxName(ObjectBuilder.LastIndexValueInput.VariableName)
                        );    
                } else
                {
                    lastIndexExpression = MySyntaxFactory.Literal(typeof (int).Signature(), ObjectBuilder.LastIndexValue);
                }

                // Create syntax for increment 
                ExpressionSyntax incrementExpression;
                if(m_incrementInput != null)
                {
                    incrementExpression = SyntaxFactory.IdentifierName(
                        m_incrementInput.VariableSyntaxName(ObjectBuilder.IncrementValueInput.VariableName)
                        );
                }
                else
                {
                    incrementExpression = MySyntaxFactory.Literal(typeof (int).Signature(), ObjectBuilder.IncrementValue);
                }

                // Put the for statement syntax together
                var forStatement = SyntaxFactory.ForStatement(
                    SyntaxFactory.Block(bodySyntax)
                    ).WithDeclaration(
                        SyntaxFactory.VariableDeclaration(
                            SyntaxFactory.PredefinedType(
                                SyntaxFactory.Token(SyntaxKind.IntKeyword)))
                            .WithVariables(
                                SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(
                                    SyntaxFactory.VariableDeclarator(
                                        SyntaxFactory.Identifier(VariableSyntaxName()))
                                        .WithInitializer(
                                            SyntaxFactory.EqualsValueClause(
                                                firstIndexExpression
                                                )
                                        )
                                    )
                            )
                    ).WithCondition(
                        SyntaxFactory.BinaryExpression(
                            SyntaxKind.LessThanOrEqualExpression,
                            SyntaxFactory.IdentifierName(VariableSyntaxName()),
                            lastIndexExpression
                            )
                    ).WithIncrementors(
                        SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                            SyntaxFactory.AssignmentExpression(
                                SyntaxKind.AddAssignmentExpression,
                                SyntaxFactory.IdentifierName(VariableSyntaxName()),
                                incrementExpression
                                )
                            )
                    );

                expressions.Add(forStatement);
            }

            // Ignore if not present
            if(m_finishSequence != null)
                m_finishSequence.CollectSequenceExpressions(expressions);
        }

        protected internal override void Preprocess(int currentDepth)
        {
            if (!Preprocessed)
            {
                // Fill in the sequence inputs
                foreach (var sequenceInput in ObjectBuilder.SequenceInputs)
                {
                    TryRegisterNode(sequenceInput, SequenceInputs);
                }

                // Fill in the sequence output so preprocess sees it
                if (ObjectBuilder.SequenceOutput != -1)
                {
                    m_finishSequence = Navigator.GetNodeByID(ObjectBuilder.SequenceOutput);
                    if(m_finishSequence != null)
                        SequenceOutputs.Add(m_finishSequence);
                }

                // Fill in the body node
                if (ObjectBuilder.SequenceBody != -1)
                {
                    m_bodySequence = Navigator.GetNodeByID(ObjectBuilder.SequenceBody);
                    if(m_bodySequence != null)
                        SequenceOutputs.Add(m_bodySequence);
                }

                // Fill in the Counter outputs
                foreach (var identifier in ObjectBuilder.CounterValueOutputs)
                {
                    TryRegisterNode(identifier.NodeID, Outputs);
                }

                // Fill in the Inputs
                m_firstInput = TryRegisterNode(ObjectBuilder.FirstIndexValueInput.NodeID, Inputs);
                m_lastInput = TryRegisterNode(ObjectBuilder.LastIndexValueInput.NodeID, Inputs);
                m_incrementInput = TryRegisterNode(ObjectBuilder.IncrementValueInput.NodeID, Inputs);
            }

            base.Preprocess(currentDepth);
        }
    }
}
