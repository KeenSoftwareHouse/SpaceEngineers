using VRage.Collections;
using VRage.Generics;
using VRage.Utils;

namespace VRage.Game.VisualScripting.Missions
{
    public class MyVSStateMachineSpreadNode : MyStateMachineNode
    {
        public MyVSStateMachineSpreadNode(string nodeName) : base(nodeName)
        {
        }

        protected override void ExpandInternal(MyStateMachineCursor cursor, MyConcurrentHashSet<MyStringId> enquedActions, int passThrough)
        {
            if(OutTransitions.Count == 0) return;

            var stateMachine = cursor.StateMachine;
            // Remove the current Cursor
            stateMachine.DeleteCursor(cursor.Id);

            // Spawn new cursors for the rest
            for (var i = 0; i < OutTransitions.Count; i++)
                stateMachine.CreateCursor(OutTransitions[i].TargetNode.Name);
        }
    }
}
