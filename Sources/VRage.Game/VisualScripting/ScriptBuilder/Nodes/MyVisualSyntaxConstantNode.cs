using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VRage.Game.VisualScripting.Utils;
using VRageMath;

namespace VRage.Game.VisualScripting.ScriptBuilder.Nodes
{
    public class MyVisualSyntaxConstantNode : MyVisualSyntaxNode
    {
        internal override bool SequenceDependent
        {
            get { return false; }
        }

        public new MyObjectBuilder_ConstantScriptNode ObjectBuilder
        {
            get { return (MyObjectBuilder_ConstantScriptNode) m_objectBuilder; }
        }

        public MyVisualSyntaxConstantNode(MyObjectBuilder_ScriptNode ob) : base(ob)
        {
            Debug.Assert(ob is MyObjectBuilder_ConstantScriptNode);
        }

        internal override void CollectInputExpressions(List<StatementSyntax> expressions)
        {
            var value = ObjectBuilder.Value ?? string.Empty;
            var type = MyVisualScriptingProxy.GetType(ObjectBuilder.Type);

            base.CollectInputExpressions(expressions);

            // First handle special cases as Color and Enums
            if (type == typeof(Color) || type.IsEnum)
            {
                expressions.Add(
                    MySyntaxFactory.LocalVariable(
                        ObjectBuilder.Type,
                        VariableSyntaxName(),
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(ObjectBuilder.Type),
                            SyntaxFactory.IdentifierName(ObjectBuilder.Value)
                            )
                        )
                    );
            }
            else if(type == typeof(Vector3D))
            {
                expressions.Add(
                    MySyntaxFactory.LocalVariable(ObjectBuilder.Type, VariableSyntaxName(),
                        MySyntaxFactory.NewVector3D(ObjectBuilder.Value)
                    )
                    );
            }
            else
            {
                // Rest is generic
                expressions.Add(
                    MySyntaxFactory.LocalVariable(
                    ObjectBuilder.Type, 
                    VariableSyntaxName(), 
                    MySyntaxFactory.Literal(ObjectBuilder.Type, value))
                    );   
            }
        }

        protected internal override string VariableSyntaxName(string variableIdentifier = null)
        {
            return "constantNode_" + ObjectBuilder.ID;
        }

        protected internal override void Preprocess(int currentDepth)
        {            
            if(!Preprocessed)
            {
                // Fill in the Output data
                for (int index = 0; index < ObjectBuilder.OutputIds.Ids.Count; index++)
                {
                    var syntaxNode = Navigator.GetNodeByID(ObjectBuilder.OutputIds.Ids[index].NodeID);
                    if (syntaxNode != null)
                    {
                        Outputs.Add(syntaxNode);
                    }
                }
            }

            base.Preprocess(currentDepth);
        }
    }
}
