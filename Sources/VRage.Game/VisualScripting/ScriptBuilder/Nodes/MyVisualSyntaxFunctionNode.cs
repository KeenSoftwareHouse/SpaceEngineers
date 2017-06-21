using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VRage.Game.VisualScripting.Utils;

namespace VRage.Game.VisualScripting.ScriptBuilder.Nodes
{
    public class MyVisualSyntaxFunctionNode : MyVisualSyntaxNode
    {
        private readonly MethodInfo         m_methodInfo;
        private MyVisualSyntaxNode          m_sequenceOutputNode;
        private MyVisualSyntaxNode          m_instance;
        private readonly Type               m_scriptBaseType;

        private readonly Dictionary<ParameterInfo, MyTuple<MyVisualSyntaxNode, MyVariableIdentifier>> m_parametersToInputs =
            new Dictionary<ParameterInfo, MyTuple<MyVisualSyntaxNode, MyVariableIdentifier>>(); 

        internal override bool SequenceDependent
        {
            get { return m_methodInfo.IsSequenceDependent();  }
        }

        public UsingDirectiveSyntax Using { get; private set; }

        public new MyObjectBuilder_FunctionScriptNode ObjectBuilder
        {
            get { return (MyObjectBuilder_FunctionScriptNode) m_objectBuilder; }
        }

        public MyVisualSyntaxFunctionNode(MyObjectBuilder_ScriptNode ob, Type scriptBaseType) : base(ob)
        {
            Debug.Assert(ob is MyObjectBuilder_FunctionScriptNode);
            m_objectBuilder = (MyObjectBuilder_FunctionScriptNode)ob;
            m_methodInfo = MyVisualScriptingProxy.GetMethod(ObjectBuilder.Type);
            m_scriptBaseType = scriptBaseType;
            // Probably interface member
            if (m_methodInfo == null)
            {
                var methodName = ObjectBuilder.Type.Remove(0, ObjectBuilder.Type.LastIndexOf('.') + 1);
                var indexOfParenthesis = methodName.IndexOf('(');

                if (scriptBaseType != null && indexOfParenthesis > 0)
                {
                    methodName = methodName.Remove(indexOfParenthesis);
                    m_methodInfo = scriptBaseType.GetMethod(methodName);
                }
            }

            // Check instance methods
            if(m_methodInfo == null && !string.IsNullOrEmpty(ObjectBuilder.DeclaringType))
            {
                var declaringType = MyVisualScriptingProxy.GetType(ObjectBuilder.DeclaringType);
                Debug.Assert(declaringType != null, "Function Node: Declaring type parsing failed.");

                if (declaringType != null)
                {
                    m_methodInfo = MyVisualScriptingProxy.GetMethod(declaringType, ObjectBuilder.Type);
                }
            }

            // Look for extension
            if (m_methodInfo == null && !string.IsNullOrEmpty(ObjectBuilder.ExtOfType))
            {
                var extensionOfType = MyVisualScriptingProxy.GetType(ObjectBuilder.ExtOfType);
                m_methodInfo = MyVisualScriptingProxy.GetMethod(extensionOfType, ObjectBuilder.Type);
            }

            Debug.Assert(m_methodInfo != null,
                "For Designers: The Signature: " + ObjectBuilder.Type +
                " is out of date please consider updating the script in the Editor.");

            if(m_methodInfo != null)
                InitUsing();       
        }

        private void InitUsing()
        {
            if(m_methodInfo.DeclaringType == null) return;

            Using = MySyntaxFactory.UsingStatementSyntax(m_methodInfo.DeclaringType.Namespace);
        }

        internal override void Reset()
        {
            base.Reset();

            // Just needs to be reset because of the branch duplication and reuse.
            m_parametersToInputs.Clear();
        }

        internal override void CollectInputExpressions(List<StatementSyntax> expressions)
        {
            // Insert expressions from connected nodes
            base.CollectInputExpressions(expressions);

            // Create container variables for output parameters.
            // Assign correct identifiers to method call.
            var args = new List<SyntaxNodeOrToken>();
            var parameters = m_methodInfo.GetParameters();
            var index = 0;

            // Skip the first parameter for extension methods. 
            if (m_methodInfo.IsDefined(typeof(ExtensionAttribute), false))
                index++;

            for (; index < parameters.Length; index++)
            {
                var parameter = parameters[index];
                // Create container variables for output parameters.
                if (parameter.IsOut)
                {
                    // Output parameters
                    var localOutputVariableName = VariableSyntaxName(parameter.Name);
                    // add variable creation expression
                    expressions.Add(MySyntaxFactory.LocalVariable(parameter.ParameterType.GetElementType().Signature(), localOutputVariableName));
                    // add variable name to parameters
                    args.Add(SyntaxFactory.Argument(
                        SyntaxFactory.IdentifierName(localOutputVariableName)
                        ).WithNameColon(SyntaxFactory.NameColon(parameter.Name))
                        .WithRefOrOutKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword)
                        )
                        );
                }
                // Get the names of the variables supplying the values to the method call

                else
                {
                    // Find the value input node
                    MyTuple<MyVisualSyntaxNode, MyVariableIdentifier> inputData;
                    if (m_parametersToInputs.TryGetValue(parameter, out inputData))
                    {
                        // FOUND!
                        // add variable name to arguments
                        var variableName = inputData.Item1.VariableSyntaxName(inputData.Item2.VariableName);
                        Debug.Assert(variableName != null);
                        args.Add(SyntaxFactory.Argument(
                            SyntaxFactory.IdentifierName(
                                variableName
                                )
                            ).WithNameColon(SyntaxFactory.NameColon(parameter.Name))
                            );
                    }
                    else
                    {
                        // Not FOUND! will be probably an constant argument
                        var paramValue =
                            ObjectBuilder.InputParameterValues.Find(value => value.ParameterName == parameter.Name);
                        if (paramValue == null)
                        {
                            // Dont panic, there could be still the default value.
                            if (!parameter.HasDefaultValue)
                            {
                                // Add null / some default value
                                args.Add(
                                    MySyntaxFactory.ConstantDefaultArgument(parameter.ParameterType)
                                        .WithNameColon(SyntaxFactory.NameColon(parameter.Name))
                                    );
                            }
                            else
                            {
                                // default value will take care of this
                                continue; // no comma for you args
                            }
                        }
                        else
                        {
                            // Add constant value to arguments
                            args.Add(MySyntaxFactory.ConstantArgument(
                                parameter.ParameterType.Signature(), paramValue.Value
                                ).WithNameColon(SyntaxFactory.NameColon(parameter.Name))
                                );
                        }
                    }
                }

                // add comma
                args.Add(SyntaxFactory.Token(SyntaxKind.CommaToken));
            }

            // remove last comma
            if(args.Count > 0)
                args.RemoveAt(args.Count - 1);

            // Create the invocation syntax 
            InvocationExpressionSyntax methodInvocation = null;
            if (m_methodInfo.IsStatic && !m_methodInfo.IsDefined(typeof(ExtensionAttribute)))
            {
                // Static call
                methodInvocation = MySyntaxFactory.MethodInvocationExpressionSyntax(
                    SyntaxFactory.IdentifierName(m_methodInfo.DeclaringType.FullName + "." + m_methodInfo.Name),
                    SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList<ArgumentSyntax>(args))
                    );
            }
            else
            {
                var declaringType = m_methodInfo.DeclaringType;
                // Non static local method invocation           
                if (declaringType == m_scriptBaseType)
                {
                    methodInvocation = MySyntaxFactory.MethodInvocationExpressionSyntax(
                        SyntaxFactory.IdentifierName(m_methodInfo.Name),
                        SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList<ArgumentSyntax>(args))
                        );
                }
                // Non static instance method invocation or extension method
                else
                {
                    if(m_instance == null)
                    {
                        throw new Exception("FunctionNode: " + ObjectBuilder.ID +
                            " Is missing mandatory instance input.");
                    }

                    var instanceVariableName = m_instance.VariableSyntaxName(ObjectBuilder.InstanceInputID.VariableName);
                    methodInvocation = MySyntaxFactory.MethodInvocationExpressionSyntax(
                        SyntaxFactory.IdentifierName(m_methodInfo.Name),
                        SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList<ArgumentSyntax>(args)),
                        SyntaxFactory.IdentifierName(instanceVariableName)
                        );
                }
            }

            // Finalize the syntax
            if (m_methodInfo.ReturnType == typeof(void))
            {
                // just add invocation
                expressions.Add(
                    SyntaxFactory.ExpressionStatement(methodInvocation)
                    );
            }
            else
            {
                // Create variable for return value
                expressions.Add(
                    MySyntaxFactory.LocalVariable(
                        string.Empty,
                        VariableSyntaxName("Return"),
                        methodInvocation
                    )
                    );
            }
        }

        protected internal override string VariableSyntaxName(string variableIdentifier = null)
        {
            return "outParamFunctionNode_" + ObjectBuilder.ID + "_" + variableIdentifier;
        }

        protected internal override void Preprocess(int currentDepth)
        {
            if (!Preprocessed)
            {
                if (SequenceDependent)
                {
                    // Fill in the sequence output
                    if (ObjectBuilder.SequenceOutputID != -1)
                    {
                        m_sequenceOutputNode = Navigator.GetNodeByID(ObjectBuilder.SequenceOutputID);
                        Debug.Assert(m_sequenceOutputNode != null);
                        SequenceOutputs.Add(m_sequenceOutputNode);
                    }

                    // Fill in the sequence input
                    var sequenceInputNode = Navigator.GetNodeByID(ObjectBuilder.SequenceInputID);
                    Debug.Assert(sequenceInputNode != null);
                    SequenceInputs.Add(sequenceInputNode);
                }
                else
                {
                    // Fill in all the outputs of the node
                    foreach (var identifierList in ObjectBuilder.OutputParametersIDs)
                    {
                        foreach (var identifier in identifierList.Ids)
                        {
                            var node = Navigator.GetNodeByID(identifier.NodeID);
                            Outputs.Add(node);
                        }
                    }
                }

                var parameters = m_methodInfo.GetParameters();
                // Change the capacity to match the OB
                Inputs.Capacity = ObjectBuilder.InputParameterIDs.Count;

                if (ObjectBuilder.Version == 0)
                {
                    //// <<BACKWARDS COMPATIBILITY>>
                    for (int index = 0; index < ObjectBuilder.InputParameterIDs.Count; index++)
                    {
                        // Check for missing mandatory inputs
                        var inputIdentifier = ObjectBuilder.InputParameterIDs[index];
                        // Input parameters
                        var inputNode = Navigator.GetNodeByID(inputIdentifier.NodeID);
                        if (inputNode != null)
                        {
                            Inputs.Add(inputNode);
                            m_parametersToInputs.Add(parameters[index],
                                new MyTuple<MyVisualSyntaxNode, MyVariableIdentifier>(inputNode, inputIdentifier));
                        }
                    }
                    //// <<BACKWARDS COMPATIBILITY>>
                }
                else
                {
                    var index = 0;
                    // Skip the first parameter for extension methods.
                    if (m_methodInfo.IsDefined(typeof(ExtensionAttribute), false))
                        index++;

                    for (; index < parameters.Length; index++)
                    {
                        var parameter = parameters[index];
                        // Find the node node identifier in the OB
                        var identifier =
                            ObjectBuilder.InputParameterIDs.Find(ident => ident.OriginName == parameter.Name);
                        // Continue for empty records
                        if (string.IsNullOrEmpty(identifier.OriginName)) continue;

                        // Find node
                        var inputNode = Navigator.GetNodeByID(identifier.NodeID);
                        if (inputNode == null)
                        {
                            if (!parameter.HasDefaultValue)
                            {
                                Debug.Fail("FunctionNode: " + ObjectBuilder.ID + " Input node missing! NodeID: "
                                           + identifier.NodeID + " Function Signature:" + m_methodInfo.Signature() +
                                           " parameter: " + parameter.Name);
                            }

                            continue;
                        }

                        // register data
                        Inputs.Add(inputNode);
                        var t = !m_parametersToInputs.ContainsKey(parameter);
                        m_parametersToInputs.Add(parameter,
                            new MyTuple<MyVisualSyntaxNode, MyVariableIdentifier>(inputNode, identifier));
                    }

                    // Add instance input as regular input
                    if (ObjectBuilder.InstanceInputID.NodeID != -1)
                    {
                        m_instance = Navigator.GetNodeByID(ObjectBuilder.InstanceInputID.NodeID);
                        if (m_instance != null)
                            Inputs.Add(m_instance);
                    }
                }
            }

            base.Preprocess(currentDepth);
        }
    }
}
