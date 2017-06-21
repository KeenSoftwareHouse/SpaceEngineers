using System.Diagnostics;
using System.Linq;

namespace VRage.Game.VisualScripting.ScriptBuilder.Nodes
{
    /// <summary>
    /// This node does not generate any syntax, but runs the preprocessing
    /// part and initializes the collecting of syntax.
    /// </summary>
    public class MyVisualSyntaxBuilderNode : MyVisualSyntaxNode
    {
        public MyVisualSyntaxBuilderNode() : base(null)
        {
        }

        public void Preprocess()
        {
            Depth = 0;
            Navigator.FreshNodes.Clear();
            // Run preprocessing on descendants
            Preprocess(Depth + 1);
            // Resolve the nodes from fresh set
            foreach (var syntaxNode in Navigator.FreshNodes)
            {
                // These are the nodes that have one output that is sequence
                // dependent, thats why they need to be resolved here so we keep
                // the order of operations in subtrees intact.
                if(syntaxNode.Outputs.Count == 1)
                {
                    syntaxNode.Outputs[0].SubTreeNodes.Add(syntaxNode);
                    continue;
                }
                // Get all sequence nodes that are connected to this one
                var sequenceNodes = syntaxNode.GetSequenceDependentOutputs();
                // Common parent for all the sequence node
                var commonParent = CommonParent(sequenceNodes);
                if (commonParent == null)
                {
                    // Put the nodes that have no common parent solution to this node (root)
                    SubTreeNodes.Add(syntaxNode);
                }
                else
                {
                    // add your self to the common parents sub tree
                    commonParent.SubTreeNodes.Add(syntaxNode);
                }
            }
        }
    }
}
