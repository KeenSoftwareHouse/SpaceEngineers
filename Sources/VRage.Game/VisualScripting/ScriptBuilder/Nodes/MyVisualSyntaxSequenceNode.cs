using System.Diagnostics;
using VRage.Game.ObjectBuilders.VisualScripting;

namespace VRage.Game.VisualScripting.ScriptBuilder.Nodes
{
    public class MyVisualSyntaxSequenceNode : MyVisualSyntaxNode
    {
        public new MyObjectBuilder_SequenceScriptNode ObjectBuilder
        {
            get { return (MyObjectBuilder_SequenceScriptNode)m_objectBuilder;}
        }

        public MyVisualSyntaxSequenceNode(MyObjectBuilder_ScriptNode ob) : base(ob)
        {
            Debug.Assert(ob is MyObjectBuilder_SequenceScriptNode);
        }

        protected internal override void Preprocess(int currentDepth)
        {
            if (!Preprocessed)
            {
                if (ObjectBuilder.SequenceInput != -1)
                {
                    var sequenceInputNode = Navigator.GetNodeByID(ObjectBuilder.SequenceInput);
                    Debug.Assert(sequenceInputNode != null);
                    SequenceInputs.Add(sequenceInputNode);
                }

                foreach (var sequenceOutputId in ObjectBuilder.SequenceOutputs)
                {
                    if (sequenceOutputId != -1)
                    {
                        var interaction = Navigator.GetNodeByID(sequenceOutputId);
                        Debug.Assert(interaction != null);
                        SequenceOutputs.Add(interaction);
                    }
                }
            }

            base.Preprocess(currentDepth);
        }
    }
}
