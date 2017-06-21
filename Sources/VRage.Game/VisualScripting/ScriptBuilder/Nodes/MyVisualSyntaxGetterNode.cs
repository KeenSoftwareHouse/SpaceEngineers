using System.Diagnostics;

namespace VRage.Game.VisualScripting.ScriptBuilder.Nodes
{
    /// <summary>
    /// This node represents a class variable name getter.
    /// Genertes no syntax and only provides other node with
    /// variable name.
    /// </summary>
    public class MyVisualSyntaxGetterNode : MyVisualSyntaxNode
    {
        internal override bool SequenceDependent { get { return false; } }

        public new MyObjectBuilder_GetterScriptNode ObjectBuilder
        {
            get { return (MyObjectBuilder_GetterScriptNode) m_objectBuilder; }
        }

        public MyVisualSyntaxGetterNode(MyObjectBuilder_ScriptNode ob) : base(ob)
        {
            Debug.Assert(ob is MyObjectBuilder_GetterScriptNode);
        }

        protected internal override string VariableSyntaxName(string variableIdentifier = null)
        {
            return ObjectBuilder.BoundVariableName;
        }

        protected internal override void Preprocess(int currentDepth)
        {
            if(!Preprocessed)
            {
                // Fill in output data
                for (int index = 0; index < ObjectBuilder.OutputIDs.Ids.Count; index++)
                {
                    var syntaxNode = Navigator.GetNodeByID(ObjectBuilder.OutputIDs.Ids[index].NodeID);
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
