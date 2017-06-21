using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VRage.Game.ObjectBuilders.VisualScripting;
using VRage.Game.VisualScripting.Utils;

namespace VRage.Game.VisualScripting.ScriptBuilder.Nodes
{
    /// <summary>
    /// Sequence independent node that creates syntax for basic 
    /// boolean algebra operations. AND OR NOT XOR NAND NOR.
    /// It joins all provided values into a change of operations
    /// started and ended with parenthesis.
    /// </summary>
    public class MyVisualSyntaxLogicGateNode : MyVisualSyntaxNode
    {
        private readonly Dictionary<MyVisualSyntaxNode, string> m_inputsToVariableNames = new Dictionary<MyVisualSyntaxNode, string>(); 

        public new MyObjectBuilder_LogicGateScriptNode ObjectBuilder
        {
            get {return (MyObjectBuilder_LogicGateScriptNode)m_objectBuilder;}
        }

        internal override bool SequenceDependent
        {
            get { return false; }
        }

        private SyntaxKind OperationKind
        {
            get
            {
                switch (ObjectBuilder.Operation)
                {
                    case MyObjectBuilder_LogicGateScriptNode.LogicOperation.NAND:
                    case MyObjectBuilder_LogicGateScriptNode.LogicOperation.AND:
                        return SyntaxKind.LogicalAndExpression;
                    case MyObjectBuilder_LogicGateScriptNode.LogicOperation.NOR:
                    case MyObjectBuilder_LogicGateScriptNode.LogicOperation.OR:
                        return SyntaxKind.LogicalOrExpression;
                    case MyObjectBuilder_LogicGateScriptNode.LogicOperation.XOR:
                        return SyntaxKind.ExclusiveOrExpression;
                }

                return SyntaxKind.LogicalNotExpression;
            }
        }

        public MyVisualSyntaxLogicGateNode(MyObjectBuilder_ScriptNode ob) : base(ob)
        {
            Debug.Assert(ob is MyObjectBuilder_LogicGateScriptNode);
        }

        protected internal override string VariableSyntaxName(string variableIdentifier = null)
        {
            return "logicGate_" + ObjectBuilder.ID + "_output";
        }

        internal override void CollectInputExpressions(List<StatementSyntax> expressions)
        {
            base.CollectInputExpressions(expressions);

            if (Inputs.Count == 1)
           {
                ExpressionSyntax initializer;

                if(ObjectBuilder.Operation == MyObjectBuilder_LogicGateScriptNode.LogicOperation.NOT)
                {
                    // Initializer for NOT
                    initializer = SyntaxFactory.PrefixUnaryExpression(
                        SyntaxKind.LogicalNotExpression,
                        SyntaxFactory.IdentifierName(
                            Inputs[0].VariableSyntaxName(m_inputsToVariableNames[Inputs[0]])
                            )
                        );
                } else
                {
                    // Initilizer for case when someone just didn't connect anything to second input
                    initializer = SyntaxFactory.IdentifierName(
                        Inputs[0].VariableSyntaxName(m_inputsToVariableNames[Inputs[0]])
                        );
                }

                // Local variable without binary expression
                var localVariable = MySyntaxFactory.LocalVariable(
                        typeof(bool).Signature(), 
                        VariableSyntaxName(), 
                        initializer
                    );

                expressions.Add(localVariable);
            }
            else if (Inputs.Count > 1)
            {
                // Initial expression
                ExpressionSyntax initiliazer = SyntaxFactory.BinaryExpression(
                        OperationKind,
                        SyntaxFactory.IdentifierName(Inputs[0].VariableSyntaxName(m_inputsToVariableNames[Inputs[0]])),
                        SyntaxFactory.IdentifierName(Inputs[1].VariableSyntaxName(m_inputsToVariableNames[Inputs[1]]))
                    );

                // Add rest of the binary expression
                for (var index = 2; index < Inputs.Count; index++)
                {
                    var inputNode = Inputs[index];
                    initiliazer = SyntaxFactory.BinaryExpression(
                        OperationKind,
                        initiliazer,
                        SyntaxFactory.IdentifierName(
                            inputNode.VariableSyntaxName(
                                m_inputsToVariableNames[inputNode]
                                )
                            )
                        );
                }

                if(ObjectBuilder.Operation == MyObjectBuilder_LogicGateScriptNode.LogicOperation.NAND ||
                    ObjectBuilder.Operation == MyObjectBuilder_LogicGateScriptNode.LogicOperation.NOR)
                {
                    // For these also add parenthesis and negation
                    initiliazer = SyntaxFactory.PrefixUnaryExpression(
                        SyntaxKind.LogicalNotExpression,
                        SyntaxFactory.ParenthesizedExpression(
                            initiliazer
                            )
                        );
                }

                // Local variable syntax
                var localVariable = MySyntaxFactory.LocalVariable(typeof(bool).Signature(), VariableSyntaxName(), initiliazer);
                expressions.Add(localVariable);
            }
        }

        protected internal override void Preprocess(int currentDepth)
        {
            if (!Preprocessed)
            {
                foreach (var identifier in ObjectBuilder.ValueInputs)
                {
                    var node = TryRegisterNode(identifier.NodeID, Inputs);
                    if(node != null)
                    {
                        m_inputsToVariableNames.Add(node, identifier.VariableName);
                    }
                }

                foreach (var identifier in ObjectBuilder.ValueOutputs)
                {
                    TryRegisterNode(identifier.NodeID, Outputs);
                }
            }

            base.Preprocess(currentDepth);
        }
    }
}
