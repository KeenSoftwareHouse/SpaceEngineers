using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VRage.Game.VisualScripting.Utils;

namespace VRage.Game.VisualScripting.ScriptBuilder.Nodes
{
    /// <summary>
    /// Special case of Event node that also generates some syntax.
    /// Creates a simple if statement that filters the input to this node.
    /// </summary>
    public class MyVisualSyntaxKeyEventNode : MyVisualSyntaxEventNode
    {
        public new MyObjectBuilder_KeyEventScriptNode ObjectBuilder
        {
            get { return (MyObjectBuilder_KeyEventScriptNode) m_objectBuilder; }
        }

        public MyVisualSyntaxKeyEventNode(MyObjectBuilder_ScriptNode ob) : base(ob)
        {
            Debug.Assert(ob is MyObjectBuilder_KeyEventScriptNode);
        }

        internal override void CollectSequenceExpressions(List<StatementSyntax> expressions)
        {
            // Create conditional statement for easier event evaluation.
            // Considering the first parameter is the key.
            if (ObjectBuilder.SequenceOutputID == -1)
                return;

            var nextSequenceNode = Navigator.GetNodeByID(ObjectBuilder.SequenceOutputID);
            var ifBodyExpressions = new List<StatementSyntax>();
            // Collect the syntax from the true branch
            nextSequenceNode.CollectSequenceExpressions(ifBodyExpressions);

            var keyIndexes = new List<int>();
            var parameters = m_fieldInfo.FieldType.GetMethod("Invoke").GetParameters();
            var attribute = m_fieldInfo.FieldType.GetCustomAttribute<VisualScriptingEvent>();
            // Find the keys of this key event
            for (var index = 0; index < parameters.Length; index++)
            {
                if(index >= attribute.IsKey.Length)
                    break;

                if(attribute.IsKey[index])
                    keyIndexes.Add(index);
            }

            // generate the condition statement
            var condition = CreateAndClauses(keyIndexes.Count - 1, keyIndexes);

            var ifStatment = MySyntaxFactory.IfExpressionSyntax(condition, ifBodyExpressions);
            expressions.Add(ifStatment);
        }

        // Takes array of key indexs and recursively creates syntax for condition clause.
        private ExpressionSyntax CreateAndClauses(int index, List<int> keyIndexes)
        {
            var literal = MySyntaxFactory.Literal(ObjectBuilder.OuputTypes[keyIndexes[index]], ObjectBuilder.Keys[keyIndexes[index]]);

            if (index == 0)
            {
                return SyntaxFactory.BinaryExpression(
                                    SyntaxKind.EqualsExpression,
                                    SyntaxFactory.IdentifierName(ObjectBuilder.OutputNames[keyIndexes[index]]),
                                    literal
                                );
            }

            var currentExpression = SyntaxFactory.BinaryExpression(
                                    SyntaxKind.EqualsExpression,
                                    SyntaxFactory.IdentifierName(ObjectBuilder.OutputNames[keyIndexes[index]]),
                                    literal
                                );

            return SyntaxFactory.BinaryExpression(
                    SyntaxKind.LogicalAndExpression,
                    CreateAndClauses(--index, keyIndexes),
                    currentExpression
                );
        }
    }
}
