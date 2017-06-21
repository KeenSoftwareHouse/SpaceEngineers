using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Utils;

namespace VRage.Generics
{
    /// <summary>
    /// Implementation of generic state machine. Inherit from this class to create your own state machine.
    /// Transitions are performed automatically on each update (if conditions of transition are fulfilled).
    /// </summary>
    public class MyStateMachine
    {
        // Transition id counter.
        private int m_transitionIdCounter = 0;
        // Nodes of state machine, transitions are stored inside them.
        protected Dictionary<string, MyStateMachineNode> m_nodes = new Dictionary<string, MyStateMachineNode>();
        // All state machine transitions, starting node is stored here too.
        protected Dictionary<int, MyStateMachineTransitionWithStart> m_transitions = new Dictionary<int, MyStateMachineTransitionWithStart>();
        // Enqueued actions for next update. These actions ignore conditions.
        private MyConcurrentHashSet<MyStringId> m_enqueuedActions = new MyConcurrentHashSet<MyStringId>();

        // Reader of all nodes.
        public DictionaryReader<string, MyStateMachineNode> AllNodes
        {
            get { return m_nodes; }
        }
        
        public delegate void StateChangedHandler(MyStateMachineTransitionWithStart transition);
        // Called when the state is changed.
        public event StateChangedHandler OnStateChanged;
        protected void NotifyStateChanged(MyStateMachineTransitionWithStart transitionWithStart)
        {
            if (OnStateChanged != null)
                OnStateChanged(transitionWithStart);
        }

        // State machine name.
        public string Name { get; set; }
        // Reference to current (active) node.
        public MyStateMachineNode CurrentNode { get; protected set; }
        
        // Constructor of the state machine.
        public MyStateMachine()
        {
        }

        // ------------------------------------------------------------------------------------
        #region Node manipulation

        // Add new node. Node can be instance of MyStateMachineNode subclass.
        // Parameter must not be null.
        // Returns false on failure (name collision).
        public bool AddNode(MyStateMachineNode newNode)
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
        public bool DeleteNode(string nodeName)
        {
            MyStateMachineNode rtnNode;
            m_nodes.TryGetValue(nodeName, out rtnNode);
            if (rtnNode == null)
                return false;

            // remove links from other nodes
            foreach (var nodePair in m_nodes)
            {
                nodePair.Value.Transitions.RemoveAll(x => x.TargetNode == rtnNode);
            }

            m_nodes.Remove(nodeName);
            return true;
        }

        #endregion Node manipulation
        // ------------------------------------------------------------------------------------
        #region Transition manipulation

        // Add animation leading from one node to another.
        // You can pass existing instance of transition (for example, you need subclass), however, it must not be present anywhere else.
        // Returns instanced transition, where you can add your conditions etc.
        // Returns null on failure.
        public MyStateMachineTransition AddTransition(string startNodeName, string endNodeName, MyStateMachineTransition existingInstance = null)
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

            startNode.Transitions.Add(rtnTransition);
            m_transitions.Add(m_transitionIdCounter, new MyStateMachineTransitionWithStart(startNode, rtnTransition));
            return rtnTransition;
        }

        // Find the transition based on the id.
        public MyStateMachineTransition FindTransition(int transitionId)
        {
            MyStateMachineTransitionWithStart transition;
            m_transitions.TryGetValue(transitionId, out transition);
            return transition.Transition;
        }

        // Delete the transition, returns true on success, false if node does not exist.
        public bool DeleteTransition(int transitionId)
        {
            MyStateMachineTransitionWithStart transition;
            if (!m_transitions.TryGetValue(transitionId, out transition))
                return false;

            // remove from dictionary of transitions
            m_transitions.Remove(transitionId);
            // remove from start node
            bool itemRemoved = transition.StartNode.Transitions.Remove(transition.Transition);
            Debug.Assert(itemRemoved, "Transition was not found in its starting node.");
            return true;
        }
        
        #endregion Transition manipulation
        // ------------------------------------------------------------------------------------

        /// <summary>
        /// Set the current state. Warning - this is not a thing that you would like to normally do, 
        /// state machine should live its own life (based on transition condition).
        /// Returns true on success.
        /// </summary>
        public virtual bool SetState(string nameOfNewState)
        {
            var newState = FindNode(nameOfNewState);
            if (newState != null)
            {
                CurrentNode = newState;
                return true;
            }
            else
            {
                return false;
            }
        }

        // Update the state machine, try changing state.
        public void Update()
        {
            if (CurrentNode == null)
            {
                m_enqueuedActions.Clear();
                return;
            }

            int maxPassThrough = 100;
            MyStateMachineNode nextNode;
            do
            {
                nextNode = null;
                int transitionId = -1;
                // enqueued transitions (actions) first
                if (m_enqueuedActions.Count > 0)
                {
                    int bestPriority = int.MaxValue;
                    foreach (var transition in CurrentNode.Transitions)
                    {
                        int transitionPriority = transition.Priority ?? int.MaxValue;
                        if (transitionPriority <= bestPriority && m_enqueuedActions.Contains(transition.Name)
                            && (transition.Conditions.Count == 0 || transition.Evaluate()))
                        {
                            transitionId = transition.Id;
                            nextNode = transition.TargetNode;
                            bestPriority = transitionPriority;
                        }
                    }
                }
                // transitions checking conditions
                if (nextNode == null)
                {
                    nextNode = CurrentNode.QueryNewState(false, out transitionId);
                }
                // now try to transfer from one state to another
                if (nextNode != null)
                {
                    //var fromNode = CurrentNode;
                    var transitionWithStart = m_transitions[transitionId];

                    CurrentNode = nextNode;
                    NotifyStateChanged(transitionWithStart);
                }
            } while (nextNode != null       // we changed state
                && CurrentNode.PassThrough  // we want to pass through
                && maxPassThrough-- > 0);   // safety, prevent infinite loop caused by wrong data

            m_enqueuedActions.Clear();
            if (CurrentNode != null)
                CurrentNode.OnUpdate(this);
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
                state.Transitions.Sort((transition1, transition2) =>
                {
                    int leftValue = transition1.Priority ?? int.MaxValue;
                    int rightValue = transition2.Priority ?? int.MaxValue;
                    return leftValue.CompareTo(rightValue);
                });
            }
        }
    }
}
