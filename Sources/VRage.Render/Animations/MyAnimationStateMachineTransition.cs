using VRage.Generics;
using VRage.Utils;

namespace VRageRender.Animations
{
    // Type of synchronization used when transitioning to the new animation.
    public enum MyAnimationTransitionSyncType
    {
        Restart,          // restart the new state, default value
        Synchronize,      // synchronize animations
        NoSynchonization  // don't do anything        
    }

    /// <summary>
    /// Description of transition to another state (MyAnimationStateMachineNode) in the state machine (MyAnimationStateMachine).
    /// </summary>
    public class MyAnimationStateMachineTransition : MyStateMachineTransition
    {
        // todo:
        // + transition style, mixing curves (?)

        // Length of transition in seconds, default is 0 (instant without blending).
        public double TransitionTimeInSec = 0.0;
        // Type of synchronization used when transitioning to the new animation.
        public MyAnimationTransitionSyncType Sync = MyAnimationTransitionSyncType.NoSynchonization;

        /// <summary>
        /// Animation transition evaluation - different behavior from default transition. 
        /// If no conditions are given and it has a name, it must be triggered manually.
        /// 
        /// </summary>
        public override bool Evaluate()
        {
            if (Conditions.Count > 0)
                return base.Evaluate();
            else
                return Name == MyStringId.NullOrEmpty;
        }
    }
}
