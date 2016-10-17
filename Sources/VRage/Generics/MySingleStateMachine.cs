namespace VRage.Generics
{
    /// <summary>
    /// Implementation of generic state machine. Inherit from this class to create your own state machine.
    /// Transitions are performed automatically on each update (if conditions of transition are fulfilled).
    /// </summary>
    public class MySingleStateMachine : MyStateMachine
    {
        public delegate void StateChangedHandler(MyStateMachineTransitionWithStart transition);
        // Called when the state is changed.
        public event StateChangedHandler OnStateChanged;
        protected void NotifyStateChanged(MyStateMachineTransitionWithStart transitionWithStart)
        {
            if (OnStateChanged != null)
                OnStateChanged(transitionWithStart);
        }

        #region Blocking overrides
        // These methods are overrident to prevent user from using them.
        public override bool DeleteCursor(int id)
        {
            return false;
        }

        public override MyStateMachineCursor CreateCursor(string nodeName)
        {
            return null;
        }

        #endregion

        public MyStateMachineNode CurrentNode
        {
            get
            {
                if (m_activeCursors.Count == 0) return null;
                return m_activeCursors[0].Node;
            }
        }

        /// <summary>
        // Sets active state of the state machine.
        // Creates new cursor if needed.
        /// </summary>
        public bool SetState(string nameOfNewState)
        {
            if (m_activeCursors.Count == 0)
            {
                var cursor = base.CreateCursor(nameOfNewState);
                if (cursor == null) return false;
                m_activeCursors.ApplyChanges();
                m_activeCursors[0].OnCursorStateChanged += CursorStateChanged;
            }
            else
            {
                var node = FindNode(nameOfNewState);
                m_activeCursors[0].Node = node;
            }

            return true;
        }

        // Helper method.
        private void CursorStateChanged(int transitionId, MyStateMachineNode node, MyStateMachine stateMachine)
        {
            NotifyStateChanged(m_transitions[transitionId]);
        }
    }
}
