using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Generics;
using VRage.Utils;
using VRageMath;

namespace VRageRender.Animations
{
    /// <summary>
    /// Animation state machine selects the animation to match current state.
    /// When it finds valid transition to some next state, transition is performed automatically.
    /// </summary>
    public class MyAnimationStateMachine : MySingleStateMachine
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
        // Visited tree nodes of current state - used for debugging.
        public int[] VisitedTreeNodesPath { get; private set; }
        // Visited tree nodes of current state - used for debugging.
        private int[] m_lastVisitedTreeNodesPath;

        // Default constructor.
        public MyAnimationStateMachine()
        {
            VisitedTreeNodesPath = new int[64];
            m_lastVisitedTreeNodesPath = new int[64];
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
            
            CurrentUpdateData.VisitedTreeNodesCounter = 0;
            CurrentUpdateData.VisitedTreeNodesPath = m_lastVisitedTreeNodesPath;
            CurrentUpdateData.VisitedTreeNodesPath[0] = 0;

            if (BoneMask == null)                             // rebuild bone mask array if not done yet
                RebuildBoneMask();
            data.LayerBoneMask = CurrentUpdateData.LayerBoneMask = BoneMask;       // pass bone mask array to all subnodes and result
            Debug.Assert(data.LayerBoneMask != null);

            // setting animation finished flag
            MyAnimationStateMachineNode currentAnimationNode = CurrentNode as MyAnimationStateMachineNode;
            if (currentAnimationNode != null && currentAnimationNode.RootAnimationNode != null)
            {
                float finishedPercent = currentAnimationNode.RootAnimationNode.GetLocalTimeNormalized();
                data.Controller.Variables.SetValue(MyAnimationVariableStorageHints.StrIdAnimationFinished, finishedPercent);
            }
            else
            {
                data.Controller.Variables.SetValue(MyAnimationVariableStorageHints.StrIdAnimationFinished, 0);
            }

            base.Update();

            int[] swapVisitedTreeNodesPath = VisitedTreeNodesPath;
            VisitedTreeNodesPath = m_lastVisitedTreeNodesPath;
            m_lastVisitedTreeNodesPath = swapVisitedTreeNodesPath;
            CurrentUpdateData.VisitedTreeNodesPath = null;     // disconnect our array

            // blended transitions
            //    - from most recent to oldest
            //    - please preserve order of blending for three or more states
            //      when CurrentState changes
            float weightMultiplier = 1.0f;
            for (int i = 0; i < m_stateTransitionBlending.Count; i++)
            {
                MyStateTransitionBlending stateTransitionBlending = m_stateTransitionBlending[i];
                float localWeight = (float)(stateTransitionBlending.TimeLeftInSeconds * stateTransitionBlending.InvTotalTime); // 1 to 0 over time
                weightMultiplier *= localWeight;

                // update nodes that we are just leaving
                if (weightMultiplier > 0)
                {
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
                                float w = ComputeEaseInEaseOut(MathHelper.Clamp(weightMultiplier, 0, 1));
                                CurrentUpdateData.BonesResult[j].Rotation = Quaternion.Slerp(lastResult[j].Rotation,
                                    CurrentUpdateData.BonesResult[j].Rotation, w);
                                CurrentUpdateData.BonesResult[j].Translation = Vector3.Lerp(lastResult[j].Translation,
                                    CurrentUpdateData.BonesResult[j].Translation, w);
                            }
                        // give back last result (free list of bones), we dont need it anymore
                        data.Controller.ResultBonesPool.Free(lastResult);
                    }
                }
                // update, decrease remaining time
                stateTransitionBlending.TimeLeftInSeconds -= data.DeltaTimeInSeconds;
                m_stateTransitionBlending[i] = stateTransitionBlending;
                if (stateTransitionBlending.TimeLeftInSeconds <= 0 || weightMultiplier <= 0)
                {
                    // skip older blended states and mark them for deletion, because their (global) weight is now zero 
                    for (int j = i + 1; j < m_stateTransitionBlending.Count; j++)
                    {
                        var temp = m_stateTransitionBlending[j];
                        temp.TimeLeftInSeconds = 0;  // 
                        m_stateTransitionBlending[j] = temp;
                    }
                    break; 
                }
            }
            m_stateTransitionBlending.RemoveAll(s => s.TimeLeftInSeconds <= 0.0);

            data.BonesResult = CurrentUpdateData.BonesResult; // local copy contains resulting list of bones
        }

        private static float ComputeEaseInEaseOut(float t)
        {
            return t * t * (3.0f - 2.0f * t);
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
            if (animationTransition == null)
                return;
            var animationSourceNode = transitionWithStart.StartNode as MyAnimationStateMachineNode;
            var animationTargetNode = animationTransition.TargetNode as MyAnimationStateMachineNode;
            if (animationSourceNode != null)
            {
                // synchronization
                if (animationTargetNode != null)
                {
                    bool targetAnimationStillPlaying = false;
                    foreach (var blending in m_stateTransitionBlending)
                    {
                        if (blending.SourceState == animationTargetNode)
                        {
                            targetAnimationStillPlaying = true;
                            break;
                        }
                    }

                    // synchronization options... use them only if target animation is not playing already 
                    switch (animationTransition.Sync)
                    {
                        case MyAnimationTransitionSyncType.Restart:
                            if (animationTargetNode.RootAnimationNode != null)
                            {
                                animationTargetNode.RootAnimationNode.SetLocalTimeNormalized(0);
                            }
                            break;
                        case MyAnimationTransitionSyncType.Synchronize:
                            if (!targetAnimationStillPlaying && animationSourceNode.RootAnimationNode != null && animationTargetNode.RootAnimationNode != null)
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
                if ((animationTransition.TransitionTimeInSec > 0.0 || transitionWithStart.Transition.TargetNode.PassThrough)
                    && !transitionWithStart.StartNode.PassThrough)
                {
                    MyStateTransitionBlending stateTransitionBlending = new MyStateTransitionBlending // struct
                    {
                        SourceState = animationSourceNode,
                        TimeLeftInSeconds = animationTransition.TransitionTimeInSec,
                        InvTotalTime = 1.0/animationTransition.TransitionTimeInSec
                    };
                    if (!stateTransitionBlending.InvTotalTime.IsValid())
                        stateTransitionBlending.InvTotalTime = 1.0f;
                    m_stateTransitionBlending.Insert(0, stateTransitionBlending);
                }
                else
                {
                    if (transitionWithStart.StartNode.PassThrough && m_stateTransitionBlending.Count > 0)
                    {
                        // node1 -> pass -> node2, take maximum transition time
                        MyStateTransitionBlending stateTransitionBlending = m_stateTransitionBlending[0]; // struct
                        stateTransitionBlending.TimeLeftInSeconds = Math.Max(animationTransition.TransitionTimeInSec, m_stateTransitionBlending[0].TimeLeftInSeconds);
                        stateTransitionBlending.InvTotalTime = 1.0 / stateTransitionBlending.TimeLeftInSeconds;
                        if (!stateTransitionBlending.InvTotalTime.IsValid())
                            stateTransitionBlending.InvTotalTime = 1.0f;
                        m_stateTransitionBlending[0] = stateTransitionBlending;
                    }
                    else if (animationTransition.TransitionTimeInSec <= MyMathConstants.EPSILON)
                    {
                        m_stateTransitionBlending.Clear();
                            // instantly play new animation, forget about all blending stuff!
                    }
                }
            }
        }
    }
}
