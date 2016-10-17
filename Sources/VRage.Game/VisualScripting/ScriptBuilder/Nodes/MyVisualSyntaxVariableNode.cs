using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VRage.Game.VisualScripting.Utils;
using VRageMath;

namespace VRage.Game.VisualScripting.ScriptBuilder.Nodes
{
    public class MyVisualSyntaxVariableNode : MyVisualSyntaxNode
    {
        public new MyObjectBuilder_VariableScriptNode ObjectBuilder
        {
            get { return (MyObjectBuilder_VariableScriptNode) m_objectBuilder; }
        }

        internal override bool SequenceDependent { get { return false; } }

        private readonly Type m_variableType;

        public UsingDirectiveSyntax Using { get; private set; }

        public MyVisualSyntaxVariableNode(MyObjectBuilder_ScriptNode ob) : base(ob)
        {
            Debug.Assert(ob is MyObjectBuilder_VariableScriptNode);
            m_variableType = MyVisualScriptingProxy.GetType(ObjectBuilder.VariableType);
            Using = MySyntaxFactory.UsingStatementSyntax(m_variableType.Namespace);
        }

        public FieldDeclarationSyntax CreateFieldDeclaration()
        {
            return MySyntaxFactory.GenericFieldDeclaration(m_variableType, ObjectBuilder.VariableName);
        }

        public ExpressionStatementSyntax CreateInitializationSyntax()
        {
            if (m_variableType.IsGenericType)
            {
                var objectCreationSyntax = MySyntaxFactory.GenericObjectCreation(m_variableType);
                var variableAssignmentExpression = MySyntaxFactory.VariableAssignment(ObjectBuilder.VariableName, objectCreationSyntax);
                return SyntaxFactory.ExpressionStatement(variableAssignmentExpression);
            }

            if (m_variableType == typeof (Vector3D))
            {
                return MySyntaxFactory.VectorAssignmentExpression(
                    ObjectBuilder.VariableName, ObjectBuilder.VariableType,
                    ObjectBuilder.Vector.X, ObjectBuilder.Vector.Y, ObjectBuilder.Vector.Z
                    );
            }

            if (m_variableType == typeof (string))
            {
                return MySyntaxFactory.VariableAssignmentExpression(
                    ObjectBuilder.VariableName, ObjectBuilder.VariableValue, SyntaxKind.StringLiteralExpression
                    );
            }

            if (m_variableType == typeof (bool))
            {
                var normalizedValue = MySyntaxFactory.NormalizeBool(ObjectBuilder.VariableValue);
                var syntaxKind = normalizedValue == "true"
                    ? SyntaxKind.TrueLiteralExpression
                    : SyntaxKind.FalseLiteralExpression;
                return MySyntaxFactory.VariableAssignmentExpression(
                    ObjectBuilder.VariableName, ObjectBuilder.VariableValue, syntaxKind);
            }

            return MySyntaxFactory.VariableAssignmentExpression(
                ObjectBuilder.VariableName, ObjectBuilder.VariableValue, SyntaxKind.NumericLiteralExpression
                );
        }

        protected internal override string VariableSyntaxName(string variableIdentifier = null)
        {
            return ObjectBuilder.VariableName;
        }
    }
}
