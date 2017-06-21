using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VRage.Game.VisualScripting.Utils;

namespace VRage.Game.VisualScripting.ScriptBuilder.Nodes
{
    /// <summary>
    /// Represens a method call from local instance of Script class.
    /// Contains some data that are used out of the graph generation process.
    /// </summary>
    public class MyVisualSyntaxScriptNode : MyVisualSyntaxNode
    {
        private readonly string m_instanceName;

        public new MyObjectBuilder_ScriptScriptNode ObjectBuilder
        {
            get { return (MyObjectBuilder_ScriptScriptNode) m_objectBuilder; }
        }

        public MyVisualSyntaxScriptNode(MyObjectBuilder_ScriptNode ob) : base(ob)
        {
            Debug.Assert(ob is MyObjectBuilder_ScriptScriptNode);

            m_instanceName = "m_scriptInstance_" + ObjectBuilder.ID;
        }

        /// <summary>
        /// MyClass m_instanceName = new MyClass();
        /// </summary>
        /// <returns></returns>
        public MemberDeclarationSyntax InstanceDeclaration()
        {
            return SyntaxFactory.FieldDeclaration(
                    SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.IdentifierName(ObjectBuilder.Name)
                        ).WithVariables(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.VariableDeclarator(
                                    SyntaxFactory.Identifier(m_instanceName)
                                    ).WithInitializer(
                                        SyntaxFactory.EqualsValueClause(
                                            SyntaxFactory.ObjectCreationExpression(
                                                SyntaxFactory.IdentifierName(ObjectBuilder.Name)
                                                ).WithArgumentList(
                                                SyntaxFactory.ArgumentList()
                                                )
                                            )
                                    )
                                )
                        )
                    );
        }

        public StatementSyntax DisposeCallDeclaration()
        {
            return SyntaxFactory.ExpressionStatement(MySyntaxFactory.MethodInvocation("Dispose", null, m_instanceName));
        }

        private StatementSyntax CreateScriptInvocationSyntax(List<StatementSyntax> dependentStatements)
        {
            // Collect names of value supplier variables
            List<string> variableIdentifiers = ObjectBuilder.Inputs.Select((t, index) => Inputs[index].VariableSyntaxName(t.Input.VariableName)).ToList();

            // add instance method invocation
            var invocationExpression = MySyntaxFactory.MethodInvocation("RunScript", variableIdentifiers, m_instanceName);

            if (dependentStatements == null)
                return SyntaxFactory.ExpressionStatement(invocationExpression);

            var ifStatement = MySyntaxFactory.IfExpressionSyntax(invocationExpression, dependentStatements);

            return ifStatement;
        }

        internal override void CollectInputExpressions(List<StatementSyntax> expressions)
        {
            base.CollectInputExpressions(expressions);
            // Add local output variable declarations
            expressions.AddRange(ObjectBuilder.Outputs.Select(outputData => MySyntaxFactory.LocalVariable(outputData.Type, outputData.Name)));
        }

        internal override void CollectSequenceExpressions(List<StatementSyntax> expressions)
        {
            CollectInputExpressions(expressions);

            var truSequenceExpressions = new List<StatementSyntax>();
            foreach (var outputNode in SequenceOutputs)
            {
                outputNode.CollectSequenceExpressions(truSequenceExpressions);
            }

            var syntax = CreateScriptInvocationSyntax(truSequenceExpressions);
            expressions.Add(syntax);
        }

        protected internal override string VariableSyntaxName(string variableIdentifier = null)
        {
            return variableIdentifier;
        }

        protected internal override void Preprocess(int currentDepth)
        {
            if(!Preprocessed)
            {
                if (ObjectBuilder.SequenceOutput != -1)
                {
                    var nextSequeceNode = Navigator.GetNodeByID(ObjectBuilder.SequenceOutput);
                    Debug.Assert(nextSequeceNode != null);
                    SequenceOutputs.Add(nextSequeceNode);
                }

                if (ObjectBuilder.SequenceInput != -1)
                {
                    var sequenceInputNode = Navigator.GetNodeByID(ObjectBuilder.SequenceInput);
                    Debug.Assert(sequenceInputNode != null);
                    SequenceInputs.Add(sequenceInputNode);
                }

                foreach (var inputData in ObjectBuilder.Inputs)
                {
                    if (inputData.Input.NodeID == -1)
                        throw new Exception("Output node missing input data. NodeID: " + ObjectBuilder.ID);

                    var inputValueNode = Navigator.GetNodeByID(inputData.Input.NodeID);
                    Debug.Assert(inputValueNode != null);
                    Inputs.Add(inputValueNode);
                }

                foreach (var outputData in ObjectBuilder.Outputs)
                {
                    foreach (var identifier in outputData.Outputs.Ids)
                    {
                        var outputValueNode = Navigator.GetNodeByID(identifier.NodeID);   
                        Outputs.Add(outputValueNode);
                    }
                }
            }

            base.Preprocess(currentDepth);
        }
    }
}
