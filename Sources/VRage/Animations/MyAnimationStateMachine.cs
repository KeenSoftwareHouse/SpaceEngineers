using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VRage.Generics;
using VRage.Utils;
using VRageMath;

namespace VRage.Animations
{
    /// <summary>
    /// Animation state machine selects the animation to match current state.
    /// When it finds valid transition to some next state, transition is performed automatically.
    /// </summary>
    public class MyAnimationStateMachine : MyStateMachine
    {
        // Blending mode of the layer.
        public enum MyBlendingMode
        {
            Replace, // replace current bone transformations with our results
            Add      // add our results to current bone transformations
        }

        // Structure used for storing information necessary for blending between states.
        private struct MyStateTransitionBlending
        {
            public double TimeLeftInSeconds;      // remaining time of blending in seconds
            public double InvTotalTime;  // 1/(total time of transition blending)
            public MyAnimationStateMachineNode SourceState; // source state
        }

        // Animation update data for current update call.
        public MyAnimationUpdateData CurrentUpdateData;
        // Layer blending mode. Each layer can replace masked bones or can add transformations from this layer to them.
        public MyBlendingMode Mode = MyBlendingMode.Replace;
        // Layer bone mask - hashset string ids.
        public readonly HashSet<MyStringId> BoneMaskStrIds = new HashSet<MyStringId>();
        // Layer bone mask - fast array, length = count of character bones. True = layer affects this bone, false = opposite.
        public bool[] BoneMask;

        // List of states which are currently blended out.
        private readonly List<MyStateTransitionBlending> m_stateTransitionBlending;
        // Name of variable that holds information whether animation finished.
        static readonly MyStringId m_variableAnimationFinished = MyStringId.GetOrCompute("@AnimationFinished");
        
        // Default constructor.
        public MyAnimationStateMachine()
        {
            m_stateTransitionBlending = new List<MyStateTransitionBlending>();
            base.OnStateChanged += AnimationStateChanged;
        }

        // Update state machine. That means:
        //   Change state if transition conditions are fulfilled.
        //   Update skeleton.
        public void Update(ref MyAnimationUpdateData data)
        {
            if (data.CharacterBones == null)
                return; // safety
            CurrentUpdateData = data;                         // local copy
            if (BoneMask == null)                             // rebuild bone mask array if not done yet
                RebuildBoneMask();
            data.LayerBoneMask = CurrentUpdateData.LayerBoneMask = BoneMask;       // pass bone mask array to all subnodes and result
            Debug.Assert(data.LayerBoneMask != null);
            base.Update();

            // blended transitions
            //    - from most recent to oldest
            //    - please preserve order of blending for three or more states
            //      when CurrentState changes
            for (int i = 0; i < m_stateTransitionBlending.Count; i++)
            {
                MyStateTransitionBlending stateTransitionBlending = m_stateTransitionBlending[i];
                float weight = 1 - (float)(stateTransitionBlending.TimeLeftInSeconds * stateTransitionBlending.InvTotalTime);

                // update nodes that we are just leaving
                var lastResult = CurrentUpdateData.BonesResult;
                CurrentUpdateData.BonesResult = null;
                stateTransitionBlending.SourceState.OnUpdate(this);
                if (lastResult != null && CurrentUpdateData.BonesResult != null)
                {
                    for (int j = 0; j < lastResult.Count; j++)
                        if (data.LayerBoneMask[j])
                        {
                            // these nodes lose their weight, it goes from 1 to 0
                            // we need to blend them to last result (current node or another node that we are leaving)
                            CurrentUpdateData.BonesResult[j].Rotation = Quaternion.Slerp(lastResult[j].Rotation, CurrentUpdateData.BonesResult[j].Rotation, 1 - weight);
                            CurrentUpdateData.BonesResult[j].Translation = Vector3.Lerp(lastResult[j].Translation, CurrentUpdateData.BonesResult[j].Translation, weight);
                        }
                    // give back last result (free list of bones), we dont need it anymore
                    if (lastResult != null)
                        data.Controller.ResultBonesPool.Free(lastResult);
                }
                // update, decrease remaining time
                stateTransitionBlending.TimeLeftInSeconds -= data.DeltaTimeInSeconds;
                m_stateTransitionBlending[i] = stateTransitionBlending;
            }
            m_stateTransitionBlending.RemoveAll(s => s.TimeLeftInSeconds <= 0.0);

            data.BonesResult = CurrentUpdateData.BonesResult; // local copy contains resulting list of bones

            // setting animation finished flag
            MyAnimationStateMachineNode currentAnimationNode = CurrentNode as MyAnimationStateMachineNode;
            if (currentAnimationNode != null && currentAnimationNode.RootAnimationNode != null)
            {
                float finishedPercent = currentAnimationNode.RootAnimationNode.GetLocalTimeNormalized();
                data.Controller.Variables.SetValue(m_variableAnimationFinished, finishedPercent);
            }
        }

        // Rebuild bone mask array.
        private void RebuildBoneMask()
        {
            if (CurrentUpdateData.CharacterBones == null)
                return;
            BoneMask = new bool[CurrentUpdateData.CharacterBones.Length];
            if (BoneMaskStrIds.Count == 0) // no mask => all bones by default.
            {
                for (int i = 0; i < CurrentUpdateData.CharacterBones.Length; i++)
                    BoneMask[i] = true;
            }
            else // mask defined => affect only bones named in BoneMaskStrIds
            {
                for (int i = 0; i < CurrentUpdateData.CharacterBones.Length; i++)
                {
                    MyStringId charBoneStrId = MyStringId.TryGet(CurrentUpdateData.CharacterBones[i].Name);
                    if (charBoneStrId != MyStringId.NullOrEmpty && BoneMaskStrIds.Contains(charBoneStrId))
                        BoneMask[i] = true;
                }
            }
        }

        // Implementation of ToString - for better debugging.
        public override string ToString()
        {
            return String.Format("MyAnimationStateMachine, Name='{0}', Mode='{1}'", Name, Mode);
        }

        // Event handling - animation state changed. 
        // We need to capture that because:
        //   1: state can be changed over time.
        //   2: synchronization of states
        private void AnimationStateChanged(MyStateMachineTransitionWithStart transitionWithStart)
        {
            var animationTransition = transitionWithStart.Transition as MyAnimationStateMachineTransition;
            var animationSourceNode = transitionWithStart.StartNode as MyAnimationStateMachineNode;
            var animationTargetNode = animationTransition.TargetNode as MyAnimationStateMachineNode;
            if (animationSourceNode != null && animationTransition != null)
            {
                // synchronization
                if (animationTargetNode != null)
                {
                    switch (animationTransition.Sync)
                    {
                        case MyAnimationTransitionSyncType.Restart:
                            if (animationTargetNode.RootAnimationNode != null)
                                animationTargetNode.RootAnimationNode.SetLocalTimeNormalized(0);
                            break;
                        case MyAnimationTransitionSyncType.Synchronize:
                            if (animationSourceNode.RootAnimationNode != null && animationTargetNode.RootAnimationNode != null)
                            {
                                float normalizedTimeInSource = animationSourceNode.RootAnimationNode.GetLocalTimeNormalized();
                                animationTargetNode.RootAnimationNode.SetLocalTimeNormalized(normalizedTimeInSource);
                            }
                            break;
                        case MyAnimationTransitionSyncType.NoSynchonization:
                            // do nothing
                            break;
                        default:
                            Debug.Fail("Unknown synchronization option.");
                            break;
                    }
                }
                // transition over time
                if (animationTransition.TransitionTimeInSec > 0.0)
                {
                    MyStateTransitionBlending stateTransitionBlending = new MyStateTransitionBlending(); // struct
                    stateTransitionBlending.SourceState = animationSourceNode;
                    stateTransitionBlending.TimeLeftInSeconds = animationTransition.TransitionTimeInSec;
                    stateTransitionBlending.InvTotalTime = 1.0 / animationTransition.TransitionTimeInSec;
                    m_stateTransitionBlending.Insert(0, stateTransitionBlending);
                }
                else
                {
                    m_stateTransitionBlending.Clear(); // instantly play new animation, forget about all blending stuff!
                }
                CurrentUpdateData.Controller.Variables.SetValue(m_variableAnimationFinished, 0.0f);
            }
        }
    }
}
