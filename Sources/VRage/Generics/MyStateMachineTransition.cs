using System.Collections.Generic;
using VRage.Utils;

namespace VRage.Generics
{
    /// <summary>
    /// Definition of transition to some node.
    /// </summary>
    public class MyStateMachineTransition
    {
        public int Id { get; private set; }
        // Name of the transition stored in MyStringId. Note that transition does not have to have a name.
        public MyStringId Name = MyStringId.NullOrEmpty;
        // Reference to target node.
        public MyStateMachineNode TargetNode = null;
        // List of conditions that must be fulfilled to transfer to target node.
        public List<IMyCondition> Conditions = new List<IMyCondition>();

        // Evaluate all conditions, returns true if all "Conditions" are fulfulled.
        // Can be overriden it in subclasses.
        public virtual bool Evaluate()
        {
            for (int i = 0; i < Conditions.Count; i++)
            {
                if (!Conditions[i].Evaluate())
                    return false; // no need to eval other
            }
            return true;
        }

        public void _SetId(int newId)
        {
            Id = newId;
        }
    }

    /// <summary>
    /// Pair holding transition and its starting node.
    /// </summary>
    public struct MyStateMachineTransitionWithStart
    {
        public MyStateMachineNode StartNode;
        public MyStateMachineTransition Transition;

        /// <summary>
        /// Full constructor.
        /// </summary>
        public MyStateMachineTransitionWithStart(MyStateMachineNode startNode, MyStateMachineTransition transition)
        {
            StartNode = startNode;
            Transition = transition;
        }
    }
}
