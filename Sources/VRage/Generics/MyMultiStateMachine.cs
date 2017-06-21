using System.Collections.Generic;
using System.Diagnostics;
using VRage.Collections;
using VRage.Utils;

namespace VRage.Generics
{
    /// <summary>
    /// Implementation of generic multistate state machine. Is able to run multiple independent cursors
    /// at once updated with every update call. Use cursors as access point to active states.
    /// </summary>
    public class MyStateMachine
    {
        // Transition id counter.
        private int m_transitionIdCounter;
        // Nodes of state machine, transitions are stored inside them.
        protected Dictionary<string, MyStateMachineNode> m_nodes = new Dictionary<string, MyStateMachineNode>();
        // All state machine transitions, starting node is stored here too.
        protected Dictionary<int, MyStateMachineTransitionWithStart> m_transitions = new Dictionary<int, MyStateMachineTransitionWithStart>();
        // Active state machine cursors by id
        protected Dictionary<int, MyStateMachineCursor> m_activeCursorsById = new Dictionary<int, MyStateMachineCursor>();
        // Active cursors container for outside getter purpose
        protected CachingList<MyStateMachineCursor> m_activeCursors = new CachingList<MyStateMachineCursor>(); 
        // Enqueued actions for next update. These actions ignore conditions.
        protected MyConcurrentHashSet<MyStringId> m_enqueuedActions = new MyConcurrentHashSet<MyStringId>();

        // Reader of all nodes.
        public DictionaryReader<string, MyStateMachineNode> AllNodes
        {
            get { return m_nodes; }
        }

        // Reader of all active cursors
        // Warning - creates new list!
        public List<MyStateMachineCursor> ActiveCursors
        {
            get { return new List<MyStateMachineCursor>(m_activeCursors); }
        }

        // State machine name.
        public string Name { get; set; }
        #region Cursor Manipulation
        /// <summary>
        /// Creates new active cursor.
        /// </summary>
        public virtual MyStateMachineCursor CreateCursor(string nodeName)
        {
            var foundNode = FindNode(nodeName);
            if (foundNode != null)
            {
                var newCursor = new MyStateMachineCursor(foundNode, this);
                m_activeCursorsById.Add(newCursor.Id, newCursor);
                m_activeCursors.Add(newCursor);
                return newCursor;
            }

            return null;
        }

        // Finds cursor by its id, returns null if not found.
        public MyStateMachineCursor FindCursor(int cursorId)
        {
            MyStateMachineCursor cursor;
            m_activeCursorsById.TryGetValue(cursorId, out cursor);
            return cursor;
        }

        // Removes cursor of give id from state machine
        public virtual bool DeleteCursor(int id)
        {
            if (!m_activeCursorsById.ContainsKey(id))
                return false;

            var cursor = m_activeCursorsById[id];

            m_activeCursorsById.Remove(id);
            m_activeCursors.Remove(cursor);

            return true;
        }
        #endregion
        #region Node manipulation
        // Add new node. Node can be instance of MyStateMachineNode subclass.
        // Parameter must not be null.
        // Returns false on failure (name collision).
        public virtual bool AddNode(MyStateMachineNode newNode)
        {
            Debug.Assert(newNode != null, "Node added to state machine cannot be null.");
            if (FindNode(newNode.Name) != null)
            {
                Debug.Assert(false, "State machine '" + Name + "' already contains node having name '" + newNode.Name + "'.");
                return false;
            }

            m_nodes.Add(newNode.Name, newNode);
            return true;
        }

        // Find node by name. Returns null if that node does not exist.
        public MyStateMachineNode FindNode(string nodeName)
        {
            MyStateMachineNode rtnNode;
            m_nodes.TryGetValue(nodeName, out rtnNode);
            return rtnNode;
        }

        // Delete node identified by name. Returns false if no node was removed.
        public virtual bool DeleteNode(string nodeName)
        {
            MyStateMachineNode rtnNode;
            m_nodes.TryGetValue(nodeName, out rtnNode);
            if (rtnNode == null)
                return false;

            // remove links from other nodes
            foreach (var nodePair in m_nodes)
            {
                nodePair.Value.OutTransitions.RemoveAll(x => x.TargetNode == rtnNode);
            }

            m_nodes.Remove(nodeName);

            // remove cursors pointing on him
            for (var i = 0; i < m_activeCursors.Count;)
            {
                if(m_activeCursors[i].Node.Name == nodeName)
                {
                    m_activeCursors[i].Node = null;
                    m_activeCursorsById.Remove(m_activeCursors[i].Id);
                    m_activeCursors.Remove(m_activeCursors[i]);
                }
            }
            return true;
        }
        #endregion Node manipulation
        #region Transition manipulation
        // Add animation leading from one node to another.
        // You can pass existing instance of transition (for example, you need subclass), however, it must not be present anywhere else.
        // Returns instanced transition, where you can add your conditions etc.
        // Returns null on failure.
        public virtual MyStateMachineTransition AddTransition(string startNodeName, string endNodeName, MyStateMachineTransition existingInstance = null, string name = null)
        {
            // nodes are passed through their names because of these checks:
            var startNode = FindNode(startNodeName);
            var endNode = FindNode(endNodeName);
            if (startNode == null || endNode == null)
                return null;

            MyStateMachineTransition rtnTransition;
            if (existingInstance == null)
            {
                // autocreate
                rtnTransition = new MyStateMachineTransition();
                if(name != null)
                    rtnTransition.Name = MyStringId.GetOrCompute(name);
            }
            else
            {
                // check
                Debug.Assert(existingInstance.TargetNode == null, "Target node of existing transition must be null.");
                rtnTransition = existingInstance;
            }

            m_transitionIdCounter++;
            rtnTransition._SetId(m_transitionIdCounter);
            rtnTransition.TargetNode = endNode;

            startNode.OutTransitions.Add(rtnTransition);
            endNode.InTransitions.Add(rtnTransition);
            m_transitions.Add(m_transitionIdCounter, new MyStateMachineTransitionWithStart(startNode, rtnTransition));
            // Call transition added for both nodes.
            startNode.TransitionAdded(rtnTransition);
            endNode.TransitionAdded(rtnTransition);
            return rtnTransition;
        }

        // Find the transition based on the id.
        public MyStateMachineTransition FindTransition(int transitionId)
        {
            return FindTransitionWithStart(transitionId).Transition;
        }

        // Find transition with start base on the id.
        public MyStateMachineTransitionWithStart FindTransitionWithStart(int transitionId)
        {
            MyStateMachineTransitionWithStart transition;
            m_transitions.TryGetValue(transitionId, out transition);
            return transition;
        }

        // Delete the transition, returns true on success, false if node does not exist.
        public virtual bool DeleteTransition(int transitionId)
        {
            MyStateMachineTransitionWithStart transition;
            if (!m_transitions.TryGetValue(transitionId, out transition))
                return false;

            // call transition remove for start and end node
            transition.StartNode.TransitionRemoved(transition.Transition);
            transition.Transition.TargetNode.TransitionRemoved(transition.Transition);
            // remove from dictionary of transitions
            m_transitions.Remove(transitionId);
            // remove from start node
            bool itemRemoved = transition.StartNode.OutTransitions.Remove(transition.Transition);
            Debug.Assert(itemRemoved, "Transition was not found in its starting node.");
            itemRemoved = transition.Transition.TargetNode.InTransitions.Remove(transition.Transition);
            Debug.Assert(itemRemoved, "Transition was not found in its target node.");
            return true;
        }
        #endregion Transition manipulation

        /// <summary>
        /// Set the current state. Warning - this is not a thing that you would like to normally do, 
        /// state machine should live its own life (based on transition condition).
        /// Returns true on success.
        /// </summary>
        public virtual bool SetState(int cursorId, string nameOfNewState)
        {
            var newState = FindNode(nameOfNewState);
            var cursor = FindCursor(cursorId);
            if (newState != null)
            {
                cursor.Node = newState;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Update the state machine. Transition to new states.
        /// </summary>
        public virtual void Update()
        {
            m_activeCursors.ApplyChanges();
            if (m_activeCursorsById.Count == 0)
            {
                m_enqueuedActions.Clear();
                return;
            }

            foreach (var cursor in m_activeCursors)
            {
                cursor.Node.Expand(cursor, m_enqueuedActions);
                cursor.Node.OnUpdate(this);
            }
            m_enqueuedActions.Clear();
        }

        /// <summary>
        /// Trigger an action in this layer. 
        /// If there is a transition having given (non-null) name, it is followed immediatelly.
        /// Conditions of transition are ignored.
        /// </summary>
        public void TriggerAction(MyStringId actionName)
        {
            m_enqueuedActions.Add(actionName);
        }

        /// <summary>
        /// Sort the transitions between states according to their priorities.
        /// </summary>
        public void SortTransitions()
        {
            foreach (var state in m_nodes.Values)
            {
                state.OutTransitions.Sort((transition1, transition2) =>
                {
                    int leftValue = transition1.Priority ?? int.MaxValue;
                    int rightValue = transition2.Priority ?? int.MaxValue;
                    return leftValue.CompareTo(rightValue);
                });
            }
        }
    }
}
