using VRage.Collections;
using VRage.Generics;
using VRage.Utils;

namespace VRage.Game.VisualScripting.Missions
{
    public class MyVSStateMachineFinalNode : MyStateMachineNode
    {
        public MyVSStateMachineFinalNode(string name) : base(name)
        {
        }

        protected override void ExpandInternal(MyStateMachineCursor cursor, MyConcurrentHashSet<MyStringId> enquedActions, int passThrough)
        {
            foreach (var activeCursor in cursor.StateMachine.ActiveCursors)
            {
                cursor.StateMachine.DeleteCursor(activeCursor.Id);
            }
        }
    }
}
