using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VRage.FileSystem;
using VRage.Game.ObjectBuilders.VisualScripting;
using VRage.Game.VisualScripting.ScriptBuilder;
using VRage.Game.VisualScripting.ScriptBuilder.Nodes;
using VRage.Game.VisualScripting.Utils;
using VRage.ObjectBuilders;
using VRageMath;

namespace VRage.Game.VisualScripting
{
    /// <summary>
    /// Creates class syntax for provided file.
    /// 
    /// Notes:
    /// WorldScripts
    ///     Consist of Event methods having only input purpose, so they have no output variables and void return value.
    ///     One event type with same signature can appear on multiple places in the script. Such situaltion mean that 
    ///     the method body will have multiple sections that will be later evaluated independently without 
    ///     any order dependency.
    /// 
    /// NormalScripts
    ///     Should have only one input as entry point of the method and multiple or none Output nodes. Output nodes have
    ///     parameters defined. Method signature will contain input variables (from input node), output variables (from 
    ///     outputs - all outputs must have same signature) and bool return value. 
    ///     Return value tells the system if the output node was reached and whenever we should or should not continue
    ///     executing the sequence chain.
    /// </summary>
    public class MyVisualScriptBuilder
    {
        private     string                          m_scriptFilePath;
        private     string                          m_scriptName;
        private     MyObjectBuilder_VisualScript    m_objectBuilder;
        private     Type                            m_baseType;

        private             CompilationUnitSyntax           m_compilationUnit;
        private             MyVisualScriptNavigator         m_navigator;
        private             ClassDeclarationSyntax          m_scriptClassDeclaration;
        private             ConstructorDeclarationSyntax    m_constructor;
        private             MethodDeclarationSyntax         m_disposeMethod;
        private             NamespaceDeclarationSyntax      m_namespaceDeclaration;
        private readonly    List<MemberDeclarationSyntax>   m_fieldDeclarations = new List<MemberDeclarationSyntax>();
        private readonly    List<MethodDeclarationSyntax>   m_methodDeclarations = new List<MethodDeclarationSyntax>();

        // Helper objects that can be reused
        private readonly    List<StatementSyntax>           m_helperStatementList = new List<StatementSyntax>(); 
        private readonly    MyVisualSyntaxBuilderNode         m_builderNode = new MyVisualSyntaxBuilderNode();

        public string Syntax { get { return m_compilationUnit.ToFullString().Replace(@"\\n", @"\n"); } }

        public string ScriptName { get { return m_scriptName; } }

        public List<string> Dependencies { get { return m_objectBuilder.DependencyFilePaths; } }

        public string ScriptFilePath
        {
            get { return m_scriptFilePath; }
            set { m_scriptFilePath = value; }
        }

        public MyVisualScriptBuilder()
        {
        }

        private void Clear()
        {
            m_fieldDeclarations.Clear();
            m_methodDeclarations.Clear();
        }

        /// <summary>
        /// Loads the script file.
        /// </summary>
        /// <returns></returns>
        public bool Load()
        {
            if(string.IsNullOrEmpty(m_scriptFilePath)) return false;

            MyObjectBuilder_VSFiles bundle;

            using (var fstream = MyFileSystem.OpenRead(m_scriptFilePath))
            {
                if (!MyObjectBuilderSerializer.DeserializeXML(fstream, out bundle))
                {
                    return false;
                }
            }

            try
            {
                if (bundle.LevelScript != null)
                {
                    m_objectBuilder = bundle.LevelScript;
                }
                else if (bundle.VisualScript != null)
                {
                    m_objectBuilder = bundle.VisualScript;
                }

                m_navigator = new MyVisualScriptNavigator(m_objectBuilder);
                m_scriptName = m_objectBuilder.Name;

                if(m_objectBuilder.Interface != null)
                    m_baseType = MyVisualScriptingProxy.GetType(m_objectBuilder.Interface);
            }
            catch (Exception e)
            {
                Debug.Fail("Error occured during the graph reconstruction: " + e);
            }

            return true;
        }

        /// <summary>
        /// Creates syntax of a class generated out of interactionNodes.
        /// </summary>
        /// <returns></returns>
        public bool Build()
        {
            if (string.IsNullOrEmpty(m_scriptFilePath)) return false;

            try
            {
                // Clear possible old data
                Clear();
                // Create general class implementation + namespace
                CreateClassSyntax();
                // interface dispose method implementation
                CreateDisposeMethod();
                // Define members from variable declarations and scripts
                CreateVariablesAndConstructorSyntax();
                // Define script instance members
                CreateScriptInstances();
                // Create methods - Events are entry points for worldscripts, Input for normal scripts
                CreateMethods();
                // Create using statements for compilation unit
                CreateNamespaceDeclaration();
                // Finalize
                FinalizeSyntax();
            }
            catch (Exception e)
            {
                Debug.Fail("Script: " + m_scriptName + " failed to build.\nError message: " + e.Message);
                return false;
            }

            return true;
        }

        // interface dispose method implementation
        private void CreateDisposeMethod()
        {
            m_disposeMethod = 
                SyntaxFactory.MethodDeclaration(
                    SyntaxFactory.PredefinedType(
                        SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                    SyntaxFactory.Identifier("Dispose"))
                .WithModifiers(
                    SyntaxFactory.TokenList(
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                        .WithBody(SyntaxFactory.Block());

        }

        private void CreateClassSyntax()
        {
            var baseType = SyntaxFactory.IdentifierName("IMyLevelScript");

            if(!(m_objectBuilder is MyObjectBuilder_VisualLevelScript))
                baseType = string.IsNullOrEmpty(m_objectBuilder.Interface) ? null : SyntaxFactory.IdentifierName(m_baseType.Name);

            m_scriptClassDeclaration = MySyntaxFactory.PublicClass(m_scriptName);
                
            if(baseType != null)
            m_scriptClassDeclaration = m_scriptClassDeclaration 
                                        .WithBaseList(
                                                SyntaxFactory.BaseList(
                                                    SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                                                        SyntaxFactory.SimpleBaseType(
                                                            baseType
                                                        )
                                                    )
                                                )
                                            );
        }

        private void CreateVariablesAndConstructorSyntax()
        {
            m_constructor = MySyntaxFactory.Constructor(m_scriptClassDeclaration);
            var variables = m_navigator.OfType<MyVisualSyntaxVariableNode>();

            foreach (var variableNode in variables)
            {
                m_fieldDeclarations.Add(variableNode.CreateFieldDeclaration());
                m_constructor = m_constructor.AddBodyStatements(variableNode.CreateInitializationSyntax());
            }
        }

        private void CreateMethods()
        {
            // Generate Interface methods
            if (!string.IsNullOrEmpty(m_objectBuilder.Interface))
            {
                var methodNodes = m_navigator.OfType<MyVisualSyntaxInterfaceMethodNode>();
                foreach (var methodNode in methodNodes)
                {
                    var methodSyntax = methodNode.GetMethodDeclaration();
                    ProcessNodes(new[] { methodNode }, ref methodSyntax);
                    m_methodDeclarations.Add(methodSyntax);
                }
            }

            var events = m_navigator.OfType<MyVisualSyntaxEventNode>();
            events.AddRange(m_navigator.OfType<MyVisualSyntaxKeyEventNode>());
            // Generate Event methods
            // Take all events of same name and make a method out of theire bodies.
            while(events.Count > 0)
            {
                var firstEvent = events[0];
                var eventsWithSameName = events.Where(@event => @event.ObjectBuilder.Name == firstEvent.ObjectBuilder.Name);
                var methodDeclaration = MySyntaxFactory.PublicMethodDeclaration(
                                    firstEvent.EventName,
                                    SyntaxKind.VoidKeyword,
                                    firstEvent.ObjectBuilder.OutputNames,
                                    firstEvent.ObjectBuilder.OuputTypes);

                ProcessNodes(eventsWithSameName, ref methodDeclaration);
                // Bind with VisualScriptingProxy in constructor.
                m_constructor = m_constructor.AddBodyStatements(
                    MySyntaxFactory.DelegateAssignment(
                    firstEvent.ObjectBuilder.Name, 
                    methodDeclaration.Identifier.ToString())
                    );
                // unBind from visualScriptingProxy in dispose method
                m_disposeMethod = m_disposeMethod.AddBodyStatements(
                    MySyntaxFactory.DelegateRemoval(
                    firstEvent.ObjectBuilder.Name,
                    methodDeclaration.Identifier.ToString())
                    );

                m_methodDeclarations.Add(methodDeclaration);

                events.RemoveAll(@event => eventsWithSameName.Contains(@event));
            }

            // There can be only one method from single input node.
            // Input nodes are of type Event. 
            var inputs = m_navigator.OfType<MyVisualSyntaxInputNode>();
            var outputs = m_navigator.OfType<MyVisualSyntaxOutputNode>();

            if(inputs.Count > 0)
            {
                Debug.Assert(inputs.Count == 1);

                var input = inputs[0];
                MethodDeclarationSyntax methodDeclaration = null;
                if(outputs.Count > 0)
                {
                    List<string> outputParamNames = new List<string>(outputs[0].ObjectBuilder.Inputs.Count);
                    List<string> outputParamTypes = new List<string>(outputs[0].ObjectBuilder.Inputs.Count);

                    foreach (var outputData in outputs[0].ObjectBuilder.Inputs)
                    {
                        outputParamNames.Add(outputData.Name);
                        outputParamTypes.Add(outputData.Type);
                    }

                    methodDeclaration = MySyntaxFactory.PublicMethodDeclaration("RunScript", SyntaxKind.BoolKeyword, input.ObjectBuilder.OutputNames, input.ObjectBuilder.OuputTypes, outputParamNames, outputParamTypes);
                } else
                {
                    methodDeclaration = MySyntaxFactory.PublicMethodDeclaration("RunScript", SyntaxKind.BoolKeyword, input.ObjectBuilder.OutputNames, input.ObjectBuilder.OuputTypes);
                }

                ProcessNodes(new[] { input }, ref methodDeclaration, new[] { SyntaxFactory.ReturnStatement(SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression)) });
                m_methodDeclarations.Add(methodDeclaration);
            }
        }

        private void ProcessNodes(IEnumerable<MyVisualSyntaxNode> nodes, ref MethodDeclarationSyntax methodDeclaration, IEnumerable<StatementSyntax> statementsToAppend = null)
        {
            m_helperStatementList.Clear();
            m_navigator.ResetNodes();
            m_builderNode.Reset();
            m_builderNode.SequenceOutputs.AddRange(nodes);
            m_builderNode.Navigator = m_navigator;

            foreach (var node in nodes)
            {
                Debug.Assert(node is IMyVisualSyntaxEntryPoint);
                ((IMyVisualSyntaxEntryPoint)node).AddSequenceInput(m_builderNode);
            }

            m_builderNode.Preprocess();
            m_builderNode.CollectSequenceExpressions(m_helperStatementList);

            if (statementsToAppend != null)
                m_helperStatementList.AddRange(statementsToAppend);

            methodDeclaration = methodDeclaration.AddBodyStatements(m_helperStatementList.ToArray());
        }

        private void AddMissionLogicScriptMethods()
        {
            var ownerIdMethod = SyntaxFactory.MethodDeclaration(
                    SyntaxFactory.PredefinedType(
                        SyntaxFactory.Token(SyntaxKind.LongKeyword)),
                    SyntaxFactory.Identifier("GetOwnerId"))
                .WithModifiers(
                    SyntaxFactory.TokenList(
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithBody(
                    SyntaxFactory.Block(
                        SyntaxFactory.SingletonList<StatementSyntax>(
                            SyntaxFactory.ReturnStatement(
                                SyntaxFactory.IdentifierName("OwnerId")))));

            var ownerIdProp = SyntaxFactory.PropertyDeclaration(
                    SyntaxFactory.PredefinedType(
                        SyntaxFactory.Token(SyntaxKind.LongKeyword)),
                    SyntaxFactory.Identifier("OwnerId"))
                .WithModifiers(
                    SyntaxFactory.TokenList(
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithAccessorList(
                    SyntaxFactory.AccessorList(
                        SyntaxFactory.List<AccessorDeclarationSyntax>(
                            new AccessorDeclarationSyntax[]{
                                            SyntaxFactory.AccessorDeclaration(
                                                SyntaxKind.GetAccessorDeclaration)
                                            .WithSemicolonToken(
                                                SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                                            SyntaxFactory.AccessorDeclaration(
                                                SyntaxKind.SetAccessorDeclaration)
                                            .WithSemicolonToken(
                                                SyntaxFactory.Token(SyntaxKind.SemicolonToken))})));

            var transitionToSyntax = SyntaxFactory.PropertyDeclaration(
                    SyntaxFactory.PredefinedType(
                        SyntaxFactory.Token(SyntaxKind.StringKeyword)),
                    SyntaxFactory.Identifier("TransitionTo"))
                .WithModifiers(
                    SyntaxFactory.TokenList(
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithAccessorList(
                    SyntaxFactory.AccessorList(
                        SyntaxFactory.List<AccessorDeclarationSyntax>(
                            new AccessorDeclarationSyntax[]{
                                SyntaxFactory.AccessorDeclaration(
                                    SyntaxKind.GetAccessorDeclaration)
                                .WithSemicolonToken(
                                    SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                                SyntaxFactory.AccessorDeclaration(
                                    SyntaxKind.SetAccessorDeclaration)
                                .WithSemicolonToken(
                                    SyntaxFactory.Token(SyntaxKind.SemicolonToken))})));

            var completeMethodSyntax = SyntaxFactory.MethodDeclaration(
                        SyntaxFactory.PredefinedType(
                            SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                        SyntaxFactory.Identifier("Complete"))
                    .WithModifiers(
                        SyntaxFactory.TokenList(
                            SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                    .WithParameterList(
                        SyntaxFactory.ParameterList(
                            SyntaxFactory.SingletonSeparatedList<ParameterSyntax>(
                                SyntaxFactory.Parameter(
                                    SyntaxFactory.Identifier("transitionName"))
                                .WithType(
                                    SyntaxFactory.PredefinedType(
                                        SyntaxFactory.Token(SyntaxKind.StringKeyword)))
                                .WithDefault(
                                SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.LiteralExpression(
                                        SyntaxKind.StringLiteralExpression,
                                        SyntaxFactory.Literal("Completed")))))))
                    .WithBody(
                        SyntaxFactory.Block(
                            SyntaxFactory.SingletonList<StatementSyntax>(
                                SyntaxFactory.ExpressionStatement(
                                    SyntaxFactory.AssignmentExpression(
                                        SyntaxKind.SimpleAssignmentExpression,
                                        SyntaxFactory.IdentifierName("TransitionTo"),
                                        SyntaxFactory.IdentifierName("transitionName"))))));

            m_methodDeclarations.Add(completeMethodSyntax);
            m_fieldDeclarations.Add(transitionToSyntax);
            m_fieldDeclarations.Add(ownerIdProp);
            m_methodDeclarations.Add(ownerIdMethod);
        }

        private void CreateScriptInstances()
        {
            var scriptNodes = m_navigator.OfType<MyVisualSyntaxScriptNode>();

            if (scriptNodes != null)
            {
                foreach (var node in scriptNodes)
                {
                    m_fieldDeclarations.Add(node.InstanceDeclaration());
                    m_disposeMethod = m_disposeMethod.AddBodyStatements(node.DisposeCallDeclaration());
                }
            }
        }

        private void CreateNamespaceDeclaration()
        {
            m_namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName("VisualScripting.CustomScripts"));
        }

        private void AddMissingInterfaceMethods()
        {
            if (m_baseType == null || !m_baseType.IsInterface)
                return;

            foreach (var methodInfo in m_baseType.GetMethods())
            {
                var exists = false;
                foreach (var declaration in m_methodDeclarations)
                {
                    if (declaration.Identifier.ToFullString() == methodInfo.Name)
                    {
                        exists = true;
                        break;
                    }
                }

                if(exists)
                    continue;

                // isSpecialName is true for properties
                var attr = methodInfo.GetCustomAttribute<VisualScriptingMember>();
                if (attr == null || attr.Reserved || methodInfo.IsSpecialName)
                {
                    continue;
                }
                // Create missing syntax
                m_methodDeclarations.Add(MySyntaxFactory.MethodDeclaration(methodInfo));
            }
        }

        private void FinalizeSyntax()
        {
            // Dispose method handling
            var disposeFound = false;
            // For empty dispose we dont want to merge it really
            for (var i = 0; i < m_methodDeclarations.Count; i++)
            {
                if (m_methodDeclarations[i].Identifier.ToString() == m_disposeMethod.Identifier.ToString())
                {
                    // Dont add empty body to the already created one
                    if(m_disposeMethod.Body.Statements.Count > 0)
                    {
                        m_methodDeclarations[i] = m_methodDeclarations[i].AddBodyStatements(m_disposeMethod.Body);
                    }
                    disposeFound = true;
                    break;
                }
            }

            if(!disposeFound)
                m_methodDeclarations.Add(m_disposeMethod);

            AddMissingInterfaceMethods();

            // if the interface type is objective logic script we need to hack in the rest of the interface methods
            if(m_baseType == typeof(IMyStateMachineScript))
                AddMissionLogicScriptMethods();

            m_scriptClassDeclaration = m_scriptClassDeclaration.AddMembers(m_fieldDeclarations.ToArray());
            m_scriptClassDeclaration = m_scriptClassDeclaration.AddMembers(m_constructor);
            m_scriptClassDeclaration = m_scriptClassDeclaration.AddMembers(m_methodDeclarations.ToArray());

            m_namespaceDeclaration = m_namespaceDeclaration.AddMembers(m_scriptClassDeclaration);

            var usings = new List<UsingDirectiveSyntax>();
            var usingsUniqueSet = new HashSet<string>();
            var defaultUsing = MySyntaxFactory.UsingStatementSyntax("VRage.Game.VisualScripting");
            var collectionsUsing = MySyntaxFactory.UsingStatementSyntax("System.Collections.Generic");
            usings.Add(defaultUsing);
            usings.Add(collectionsUsing);
            usingsUniqueSet.Add(defaultUsing.ToFullString());
            usingsUniqueSet.Add(collectionsUsing.ToFullString());

            foreach (var node in m_navigator.OfType<MyVisualSyntaxFunctionNode>())
            {
                if(usingsUniqueSet.Add(node.Using.ToFullString()))
                    usings.Add(node.Using);
            }

            foreach (var node in m_navigator.OfType<MyVisualSyntaxVariableNode>())
            {
                if (usingsUniqueSet.Add(node.Using.ToFullString()))
                    usings.Add(node.Using);
            }


            m_compilationUnit = SyntaxFactory.CompilationUnit().WithUsings(SyntaxFactory.List(
                usings
                )).AddMembers(m_namespaceDeclaration).NormalizeWhitespace();
        }
    }
}
