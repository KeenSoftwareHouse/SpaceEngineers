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
        // Priority of the transition, lower is processed sooner.
        // Transitions with unset priorities are processed as last ones.
        // After changing this number, you should call MyStateMachine.SortTransitions.
        public int? Priority;

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

        public override string ToString()
        {
            if (TargetNode != null)
                return "transition -> " + TargetNode.Name;
            else
                return "transition -> (null)";
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
