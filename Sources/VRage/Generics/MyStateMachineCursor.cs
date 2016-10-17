using System.Diagnostics;
using System.Threading;

namespace VRage.Generics
{
    public class MyStateMachineCursor
    {
        // id counter common for all cursors.
        private static int m_idCounter;

        // State machine responsible for this cursor.
        private readonly MyStateMachine m_stateMachine;

        private MyStateMachineNode m_node;
        // Generated identifier.
        public readonly int Id;

        // Node can be changed only internaly by SetState methods
        // Every other state change should go through Transition method.
        public MyStateMachineNode Node
        {
            get { return m_node; }
            internal set { m_node = value; }
        }

        public int LastTransitionTakenId { get; private set; }

        // State change delegate
        public delegate void CursorStateChanged(int transitionId, MyStateMachineNode node, MyStateMachine stateMachine);
        public event CursorStateChanged OnCursorStateChanged;

        // Thread safe cursor creation
        public MyStateMachineCursor(MyStateMachineNode node, MyStateMachine stateMachine)
        {
            m_stateMachine = stateMachine;
            Id = Interlocked.Increment(ref m_idCounter);
            m_node = node;
            m_node.Cursors.Add(this);
            OnCursorStateChanged = null;
        }

        // Responsible state machine.
        public MyStateMachine StateMachine
        {
            get { return m_stateMachine; }
        }

        // Fires change event for all registered instances.
        private void NotifyCursorChanged(MyStateMachineTransition transition)
        {
            if(OnCursorStateChanged != null)
                OnCursorStateChanged(transition.Id, Node, StateMachine);
        }

        // Performs transition to different state. Fires state change event.
        public void FollowTransition(MyStateMachineTransition transition)
        {
            Debug.Assert(Node.OutTransitions.Contains(transition));
            // Manage Nodes cursors set.
            Node.Cursors.Remove(this);
            transition.TargetNode.Cursors.Add(this);
            // Do transition
            Node = transition.TargetNode;
            LastTransitionTakenId = transition.Id;
            NotifyCursorChanged(transition);
        }
    }
}
