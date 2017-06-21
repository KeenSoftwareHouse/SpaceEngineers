using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VRage.Game.ObjectBuilders.VisualScripting;
using VRage.Game.VisualScripting.Utils;

namespace VRage.Game.VisualScripting.ScriptBuilder.Nodes
{
    public class MyVisualSyntaxNewListNode : MyVisualSyntaxNode
    {
        public new MyObjectBuilder_NewListScriptNode ObjectBuilder
        {
            get { return (MyObjectBuilder_NewListScriptNode)m_objectBuilder; }
        }

        public MyVisualSyntaxNewListNode(MyObjectBuilder_ScriptNode ob) : base(ob)
        {
            Debug.Assert(ob is MyObjectBuilder_NewListScriptNode);
        }

        internal override bool SequenceDependent
        {
            get { return false; }
        }

        protected internal override string VariableSyntaxName(string variableIdentifier = null)
        {
            return "newListNode_" + ObjectBuilder.ID;
        }

        internal override void CollectInputExpressions(List<StatementSyntax> expressions)
        {
            base.CollectInputExpressions(expressions);

            var entryType = MyVisualScriptingProxy.GetType(ObjectBuilder.Type);
            var listType = typeof(List<>).MakeGenericType(entryType);

            var separatedList = new List<SyntaxNodeOrToken>();

            // Create source of arguments for array creation syntax
            for (var index = 0; index < ObjectBuilder.DefaultEntries.Count; index++)
            {
                var entry = ObjectBuilder.DefaultEntries[index];
                var literal = MySyntaxFactory.Literal(ObjectBuilder.Type, entry);

                separatedList.Add(literal);

                if(index < ObjectBuilder.DefaultEntries.Count - 1)
                {
                    separatedList.Add(SyntaxFactory.Token(SyntaxKind.CommaToken));
                }
            }

            // Syntax of "new Type[]{arg0, arg1, ...}"
            ArrayCreationExpressionSyntax arrayCreationSyntax = null;
            if(separatedList.Count > 0)
            {
                arrayCreationSyntax = SyntaxFactory.ArrayCreationExpression(
                    SyntaxFactory.ArrayType(
                        SyntaxFactory.IdentifierName(ObjectBuilder.Type),
                        SyntaxFactory.SingletonList(
                            SyntaxFactory.ArrayRankSpecifier(
                                SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                    SyntaxFactory.OmittedArraySizeExpression()
                                    )
                                )
                            )
                        ),
                    SyntaxFactory.InitializerExpression(
                        SyntaxKind.ArrayInitializerExpression,
                        SyntaxFactory.SeparatedList<ExpressionSyntax>(
                            separatedList
                            )
                        )
                    );
            }

            // Syntax of new List<Type>(arrayCreationSyntax);
            var listCreationSyntax = MySyntaxFactory.GenericObjectCreation(listType, arrayCreationSyntax == null ? null : new[] {arrayCreationSyntax});

            var localVariableSyntax = MySyntaxFactory.LocalVariable(listType, VariableSyntaxName(), listCreationSyntax);

            expressions.Add(localVariableSyntax);
        }

        protected internal override void Preprocess(int currentDepth)
        {
            if (!Preprocessed)
            {
                foreach (var identifier in ObjectBuilder.Connections)
                {
                    TryRegisterNode(identifier.NodeID, Outputs);
                }
            }

            base.Preprocess(currentDepth);
        }
    }
}
