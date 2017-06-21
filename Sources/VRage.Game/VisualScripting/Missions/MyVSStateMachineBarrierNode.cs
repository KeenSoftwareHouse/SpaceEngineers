using System.Collections.Generic;
using System.Diagnostics;
using VRage.Collections;
using VRage.Generics;
using VRage.Utils;

namespace VRage.Game.VisualScripting.Missions
{
    public class MyVSStateMachineBarrierNode : MyStateMachineNode
    {
        private readonly List<bool> m_cursorsFromInEdgesReceived = new List<bool>();

        public MyVSStateMachineBarrierNode(string name) : base(name)
        {
        }

        // Cache cursor data, remove cursor, let go when all incoming branch cursors reach the barrier node
        protected override void ExpandInternal(MyStateMachineCursor cursor, MyConcurrentHashSet<MyStringId> enquedActions, int passThrough)
        {
            var stateMachine = cursor.StateMachine;
            var inTransitionIndex = 0;
            for (; inTransitionIndex < InTransitions.Count; inTransitionIndex++)
                if(InTransitions[inTransitionIndex].Id == cursor.LastTransitionTakenId)
                    break;

            Debug.Assert(inTransitionIndex < InTransitions.Count, "Transition not found."); 
            Debug.Assert(!m_cursorsFromInEdgesReceived[inTransitionIndex], "More than one cursor from branch received.");

            m_cursorsFromInEdgesReceived[inTransitionIndex] = true;
            stateMachine.DeleteCursor(cursor.Id);

            // Check if all cursors arrived
            foreach (var value in m_cursorsFromInEdgesReceived)
                if(!value) return;

            if(OutTransitions.Count > 0)
                stateMachine.CreateCursor(OutTransitions[0].TargetNode.Name);
        }

        protected override void TransitionAddedInternal(MyStateMachineTransition transition)
        {
            // is Incoming edge
            if (transition.TargetNode == this)
            {
                m_cursorsFromInEdgesReceived.Add(false);
            }
            else
            {
                Debug.Assert(OutTransitions.Count < 2, "Only one output per barrier node intended.");
            }
        }

        protected override void TransitionRemovedInternal(MyStateMachineTransition transition)
        {
            // is Incoming edge
            if (transition.TargetNode == this)
            {
                var transitionIndex = InTransitions.IndexOf(transition);
                m_cursorsFromInEdgesReceived.RemoveAt(transitionIndex);
            }
        }
    }
}
