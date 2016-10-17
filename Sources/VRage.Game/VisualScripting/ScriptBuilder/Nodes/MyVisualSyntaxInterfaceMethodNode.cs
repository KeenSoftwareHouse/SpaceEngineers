using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VRage.Game.ObjectBuilders.VisualScripting;
using VRage.Game.VisualScripting.Utils;

namespace VRage.Game.VisualScripting.ScriptBuilder.Nodes
{
    /// <summary>
    /// Simple method declaration node for implementing the interface methods.
    /// </summary>
    public class MyVisualSyntaxInterfaceMethodNode : MyVisualSyntaxNode, IMyVisualSyntaxEntryPoint
    {
        private readonly MethodInfo m_method;

        public new MyObjectBuilder_InterfaceMethodNode ObjectBuilder
        {
            get { return (MyObjectBuilder_InterfaceMethodNode) m_objectBuilder; }
        }

        public MyVisualSyntaxInterfaceMethodNode(MyObjectBuilder_ScriptNode ob, Type baseClass) : base(ob)
        {
            Debug.Assert(ob is MyObjectBuilder_InterfaceMethodNode);
            m_method = baseClass.GetMethod(ObjectBuilder.MethodName);
            Debug.Assert(m_method != null);
        }

        protected internal override string VariableSyntaxName(string variableIdentifier = null)
        {
            foreach (var outputName in ObjectBuilder.OutputNames)
            {
                if(outputName == variableIdentifier)
                    return variableIdentifier;
            }

            return null;
        }

        public void AddSequenceInput(MyVisualSyntaxNode parent)
        {
        }

        protected internal override void Preprocess(int currentDepth)
        {
            if (!Preprocessed)
            {
                foreach (var sequenceOutputID in ObjectBuilder.SequenceOutputIDs)
                {
                    var interaction = Navigator.GetNodeByID(sequenceOutputID);
                    Debug.Assert(interaction != null);
                    SequenceOutputs.Add(interaction);
                }
            }

            base.Preprocess(currentDepth);
        }

        public MethodDeclarationSyntax GetMethodDeclaration()
        {
            return MySyntaxFactory.PublicMethodDeclaration(
                m_method.Name,
                SyntaxKind.VoidKeyword,
                ObjectBuilder.OutputNames,
                ObjectBuilder.OuputTypes
                );
        }
    }
}
