using System.Collections.Generic;
using VRage.Collections;
using VRage.Utils;

namespace VRage.Generics
{
    /// <summary>
    /// Node of the state machine.
    /// </summary>
    public class MyStateMachineNode
    {
        // Node name. // SGEN workaround
        private readonly string m_name;

        public string Name
        {
            get { return m_name; }
        }

        // List of all outgoing transitions.
        protected internal List<MyStateMachineTransition> OutTransitions = new List<MyStateMachineTransition>();
        // List of all incoming transitions.
        protected internal List<MyStateMachineTransition> InTransitions = new List<MyStateMachineTransition>(); 
        // Set of cursors pointing at this node. Managed by cursors them selfs.
        protected internal HashSet<MyStateMachineCursor> Cursors = new HashSet<MyStateMachineCursor>(); 
        // When this state is about to become current and there is a valid transition, go immediatelly to the next node.
        public bool PassThrough = false;

        // Constructor, pass node name.
        public MyStateMachineNode(string name)
        {
            m_name = name;
        }

        // Gets called by state machine after transition is added
        // Leave INTERNAL
        internal void TransitionAdded(MyStateMachineTransition transition)
        {
            TransitionAddedInternal(transition);
        }

        /// <summary>
        /// Called after Transition is added.
        /// Override for custom behavior.
        /// </summary>
        protected virtual void TransitionAddedInternal(MyStateMachineTransition transition) { }

        // Gets called by state machine when transtion is removed
        // Leave INTERNAL
        internal void TransitionRemoved(MyStateMachineTransition transition)
        {
            TransitionRemovedInternal(transition);
        }

        /// <summary>
        /// Called before Transition remove.
        /// Override for custom behavior.
        /// </summary>
        protected virtual void TransitionRemovedInternal(MyStateMachineTransition transition) { }

        // Should stay internal, we do not want to anything outside of StateMachines to use
        // Expand methods.
        internal void Expand(MyStateMachineCursor cursor, MyConcurrentHashSet<MyStringId> enquedActions)
        {
            ExpandInternal(cursor, enquedActions, 100);
        }

        /// <summary>
        /// Expands current node with given cursor.
        /// First enquedAction is taking place then any valid transition.
        /// Cursor is being transitioned to result of expansion.
        /// Override this for custom behavior.
        /// </summary>
        protected virtual void ExpandInternal(MyStateMachineCursor cursor, MyConcurrentHashSet<MyStringId> enquedActions, int passThrough)
        {
            MyStateMachineTransition nextTransition;

            do
            {
                // Try to find best transition across enquedActions
                // Does not need to be evaluated one.
                nextTransition = null;
                var transitions = cursor.Node.OutTransitions;

                if (enquedActions.Count > 0)
                {
                    int bestPriority = int.MaxValue;

                    for (var i = 0; i < transitions.Count; i++)
                    {
                        int transitionPriority = transitions[i].Priority ?? int.MaxValue;
                        if (enquedActions.Contains(transitions[i].Name) && transitionPriority <= bestPriority &&
                            (transitions[i].Conditions.Count == 0 || transitions[i].Evaluate()))
                        {
                            nextTransition = transitions[i];
                            bestPriority = transitionPriority;
                        }
                    }
                }

                if (nextTransition == null)
                {
                    // Try to find valid transition to next state
                    nextTransition = cursor.Node.QueryNextTransition();
                }

                // Transition into next state
                if (nextTransition != null)
                    cursor.FollowTransition(nextTransition);

            } while (nextTransition != null && cursor.Node.PassThrough && passThrough-- > 0);   
        }

        // Should we change state? If yes, it returns reference to open transition, otherwise null.
        // Override for custom transition evaluation behavior.
        protected virtual MyStateMachineTransition QueryNextTransition()
        {
            for (int i = 0; i < OutTransitions.Count; i++)
            {
                if (OutTransitions[i].Evaluate()) // first transition that is valid is used
                {
                    return OutTransitions[i];
                }
            }

            return null; // stay in current state
        }

        // Method launched on each update of state machine, override to add custom behavior (by default empty).
        public virtual void OnUpdate(MyStateMachine stateMachine)
        {
        }
    }
}
