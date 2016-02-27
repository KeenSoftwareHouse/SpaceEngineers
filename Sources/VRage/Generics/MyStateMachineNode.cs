using System.Collections.Generic;

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

        // Constructor, pass node name.
        public MyStateMachineNode(string name)
        {
            Name = name;
        }

        // Should we change state? If yes, it returns reference to new state, otherwise null.
        public virtual MyStateMachineNode QueryNewState()
        {
            int transitionId;
            return QueryNewState(out transitionId); // stay in current state
        }

        // Should we change state? If yes, it returns reference to new state, otherwise null.
        // This variation also returns id of transition.
        public virtual MyStateMachineNode QueryNewState(out int transitionId)
        {
            for (int i = 0; i < Transitions.Count; i++)
            {
                if (Transitions[i].Evaluate()) // first transition that is valid is used
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
