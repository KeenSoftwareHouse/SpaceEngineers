using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VRage.Collections;
using System.Diagnostics;
using System.Linq;

namespace VRage.Game.VisualScripting.ScriptBuilder.Nodes
{
    // Note: Every nonsequential node can have multiple connections to lower layers
    // of code hierarchy. This needs to be took care of in CollectExpressions function.
    // Anytime we process the last output from that node there can be two options:
    //      1) His only reference is in current level of code hierarchy
    //      2) He has multiple references in this level of hierarchy
    // In the second case the syntax needs to be pushed up a layer.
    public class MyVisualSyntaxNode
    {
        /// <summary>
        /// Depth of the node in the graph.
        /// </summary>
        internal int Depth = int.MaxValue;

        /// <summary>
        /// Tells if the node was already preprocessed.
        /// (default value is false)
        /// </summary>
        protected bool Preprocessed { get; set; }

        /// <summary>
        /// Tells whenever the node has sequence or not.
        /// </summary>
        internal virtual bool SequenceDependent { get { return true; } }

        /// <summary>
        /// Nodes that got referenced more than once at one syntax level.
        /// </summary>
        internal HashSet<MyVisualSyntaxNode> SubTreeNodes = new HashSet<MyVisualSyntaxNode>(); 

        /// <summary>
        /// Resets nodes to state when they are ready for new run of the builder.
        /// </summary>
        internal virtual void Reset()
        {
            Depth = int.MaxValue; 
            SubTreeNodes.Clear();
            Inputs.Clear();
            Outputs.Clear();
            SequenceOutputs.Clear();
            SequenceInputs.Clear();
            Collected = false;
            Preprocessed = false;
        }

        /// <summary>
        /// Unique identifier within class syntax.
        /// </summary>
        protected internal virtual string VariableSyntaxName(string variableIdentifier = null) { throw new NotImplementedException(); }

        /// <summary>
        /// Is getting set to true the first time the syntax from this node is collected.
        /// Prevents duplicities in syntax.
        /// </summary>
        internal bool Collected { get; private set; }

        /// <summary>
        /// Returns ordered set of expressions from value suppliers.
        /// </summary>
        /// <returns></returns>
        internal virtual void CollectInputExpressions(List<StatementSyntax> expressions)
        {
            Debug.Assert(Preprocessed, "Trying to collect input expression from node that was not processed first.");
            // To avoid the duplicities in syntax
            Collected = true;
            // Collec expressions from assigned nodes
            foreach (var node in SubTreeNodes)
            {
                if(!node.Collected)
                    node.CollectInputExpressions(expressions);
            }
        }

        /// <summary>
        /// Returns ordered set of all expressions.
        /// </summary>
        /// <param name="expressions"></param>
        internal virtual void CollectSequenceExpressions(List<StatementSyntax> expressions)
        {
            // Collect syntax
            CollectInputExpressions(expressions);

            // Follow sequence
            foreach (var outputNode in SequenceOutputs)
            {
                outputNode.CollectSequenceExpressions(expressions);
            }
        }

        /// <summary>
        /// List of sequence input nodes connected to this one.
        /// </summary>
        internal virtual List<MyVisualSyntaxNode> SequenceInputs { get; private set; }

        /// <summary>
        /// List of sequence output nodes connected to this one.
        /// </summary>
        internal virtual List<MyVisualSyntaxNode> SequenceOutputs { get; private set; } 

        /// <summary>
        /// Output nodes.
        /// </summary>
        internal virtual List<MyVisualSyntaxNode> Outputs { get; private set; }

        /// <summary>
        /// Input Nodes.
        /// </summary>
        internal virtual List<MyVisualSyntaxNode> Inputs { get; private set; }

        /// <summary>
        /// Data container;
        /// </summary>
        protected MyObjectBuilder_ScriptNode m_objectBuilder;

        /// <summary>
        /// Data container getter.
        /// </summary>
        public MyObjectBuilder_ScriptNode ObjectBuilder { get { return m_objectBuilder; } }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="ob">Should never be a base of _scriptNode.</param>
        internal MyVisualSyntaxNode(MyObjectBuilder_ScriptNode ob)
        {
            m_objectBuilder = ob;

            Inputs = new List<MyVisualSyntaxNode>();
            Outputs = new List<MyVisualSyntaxNode>();
            SequenceInputs = new List<MyVisualSyntaxNode>();
            SequenceOutputs = new List<MyVisualSyntaxNode>();
        }

        /// <summary>
        /// Member used for in-graph navigation.
        /// </summary>
        internal MyVisualScriptNavigator Navigator { get; set; }

        /// <summary>
        /// Pre-generation process that loads necessary data in derived nodes
        /// and adjusts the internal state of graph to generation ready state.
        /// </summary>
        /// <param name="currentDepth"></param>
        protected internal virtual void Preprocess(int currentDepth)
        {
            // Adjust the Depth of the node.
            // The point is to minimize this utility.
            if(currentDepth < Depth)
            {
                 Depth = currentDepth;
            }

            // Do general preprocessing
            if (!Preprocessed)
            {
                // First preprocess all sequence dependent nodes
                foreach (var node in SequenceOutputs)
                {
                    node.Preprocess(Depth + 1);
                }
            }

            // All sequence independet nodes needs to get updated
            // when the sequence dependent ancestor gets preprocessed.
            foreach (var inputNode in Inputs)
            {
                if(!inputNode.SequenceDependent)
                    inputNode.Preprocess(Depth);
            }

            // This needs to be done from bottom to top, otherwise the sub tree nodes 
            // would be in invalid order.
            if (!SequenceDependent && !Preprocessed)
            {
                // Sequence dependent nodes need to be processed later along with the second usecase
                // to keep the syntax order right.
                if (Outputs.Count == 1 && !Outputs[0].SequenceDependent)
                {
                    // Easy usecase, just put yourself to Outputs subTree
                    Outputs[0].SubTreeNodes.Add(this);
                }
                else if (Outputs.Count > 0)
                {
                    // Add your self to set that will be resolved once the first 
                    // preprocess run is complete and all nodes have resolved Inputs/Outputs.
                    Navigator.FreshNodes.Add(this);
                }
            }

            // To prevent recursive calls to get in and cause stack overflow.
            Preprocessed = true;
        }

        /// <summary>
        /// Tries to put the node of id into collection.
        /// </summary>
        /// <param name="nodeID">Id of looked for node.</param>
        /// <param name="collection">Target collection.</param>
        protected MyVisualSyntaxNode TryRegisterNode(int nodeID, List<MyVisualSyntaxNode> collection)
        {
            if(nodeID == -1) return null;

            var node = Navigator.GetNodeByID(nodeID);
            if(node != null)
                collection.Add(node);

            return node;
        }

        /// <summary>
        /// Method that is needed for Hashing purposes.
        /// Id should be a unique identifier within a graph.
        /// </summary>
        /// <returns>Id of node.</returns>
        public override int GetHashCode()
        {
            if(ObjectBuilder == null)
                return GetType().GetHashCode();

            return ObjectBuilder.ID;
        }

        protected struct HeapNodeWrapper
        {
            public MyVisualSyntaxNode Node;
        }

        // Used in common parent search method
        private static readonly MyBinaryStructHeap<int, HeapNodeWrapper> m_activeHeap = new MyBinaryStructHeap<int, HeapNodeWrapper>();
        // Gets rid of duplicitly accessed nodes
        private static readonly HashSet<MyVisualSyntaxNode> m_commonParentSet = new HashSet<MyVisualSyntaxNode>();

        // Finds common parent for set of given nodes
        protected static MyVisualSyntaxNode CommonParent(IEnumerable<MyVisualSyntaxNode> nodes)
        {
            m_commonParentSet.Clear();
            m_activeHeap.Clear();

            // Use the set to remove duplicities
            foreach (var node in nodes)
            {
                if (m_commonParentSet.Add(node))
                {
                    // inverse the Depth because we have only Min heap
                    m_activeHeap.Insert(new HeapNodeWrapper { Node = node }, -node.Depth);
                }
            }

            HeapNodeWrapper current;
            do
            {
                current = m_activeHeap.RemoveMin();
                // We have a solution
                if(m_activeHeap.Count == 0) break;
                // Node with no sequence inputs means that we have a solution or
                // there is no solution of we still have some nodes in the heap.
                if (current.Node.SequenceInputs.Count == 0)
                {
                    if (m_activeHeap.Count > 0)
                        return null;

                    continue;
                }

                current.Node.SequenceInputs.ForEach(node =>
                {
                    // Insert all not visited inputs into the heap
                    if (m_activeHeap.Count > 0 && m_commonParentSet.Add(node))
                    {
                        current.Node = node;
                        m_activeHeap.Insert(current, -current.Node.Depth);
                    }
                });
            } while (true);

            // Special case...
            if (current.Node is MyVisualSyntaxForLoopNode)
            {
                var parent = current.Node.SequenceInputs.FirstOrDefault();
                return parent;
            }

            return current.Node;
        }

        // Helper node repository
        private static readonly HashSet<MyVisualSyntaxNode> m_sequenceHelper = new HashSet<MyVisualSyntaxNode>(); 
        // Method wrapper for easy use
        public IEnumerable<MyVisualSyntaxNode> GetSequenceDependentOutputs()
        {
            m_sequenceHelper.Clear();
            SequenceDependentChildren(m_sequenceHelper);
            return m_sequenceHelper;
        }
        // Finds set of sequence dependent children
        private void SequenceDependentChildren(HashSet<MyVisualSyntaxNode> results)
        {
            if (Outputs.Count == 0|| Depth == Int32.MaxValue)
                return;

            foreach (var child in Outputs)
            {
                if (child.Depth == Int32.MaxValue)
                    continue;

                // Taking into account only nodes from currently procest tree that are sequence dependent.
                if (child.SequenceDependent)
                    results.Add(child);
                else
                    child.SequenceDependentChildren(results);
            }
        }
    }
}
