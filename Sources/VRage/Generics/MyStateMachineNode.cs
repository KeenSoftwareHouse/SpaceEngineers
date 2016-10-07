using System.Collections.Generic;
using VRage.Utils;

namespace VRage.Generics
{
    /// <summary>
    /// Node of the state machine.
    /// </summary>
    public class MyStateMachineNode
    {
        // Node name.
        public string Name { get; private set; }
        // List of all transitions.
        public List<MyStateMachineTransition> Transitions = new List<MyStateMachineTransition>();
        // When this state is about to become current and there is a valid transition, go immediatelly to the next node.
        public bool PassThrough = false;

        // Constructor, pass node name.
        public MyStateMachineNode(string name)
        {
            Name = name;
        }

        // Should we change state? If yes, it returns reference to new state, otherwise null.
        public virtual MyStateMachineNode QueryNewState()
        {
            int transitionId;
            return QueryNewState(false, out transitionId); // stay in current state
        }

        // Should we change state? If yes, it returns reference to new state, otherwise null.
        // This variation also returns id of transition.
        public virtual MyStateMachineNode QueryNewState(bool allowNamedTransitions, out int transitionId)
        {
            for (int i = 0; i < Transitions.Count; i++)
            {
                if ((allowNamedTransitions || Transitions[i].Name == MyStringId.NullOrEmpty) && Transitions[i].Evaluate()) // first transition that is valid is used
                {
                    transitionId = Transitions[i].Id;
                    return Transitions[i].TargetNode;
                }
            }
            transitionId = -1;
            return null; // stay in current state
        }

        // Method launched on each update of state machine, override to add custom behavior (by default empty).
        public virtual void OnUpdate(MyStateMachine stateMachine)
        {
        }
    }
}
