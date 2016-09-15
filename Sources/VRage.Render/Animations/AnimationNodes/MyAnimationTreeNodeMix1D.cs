using System.Collections.Generic;
using System.Diagnostics;
using VRage.Utils;
using VRageMath;

namespace VRageRender.Animations
{
    /// <summary>
    /// Mixing between animation nodes on 1D axis.
    /// </summary>
    public class MyAnimationTreeNodeMix1D : MyAnimationTreeNode
    {
        public struct MyParameterNodeMapping
        {
            public float ParamValueBinding;    // value on axis
            public MyAnimationTreeNode Child;  // link to used subtree
        };

        // Last known parameter value.
        private float? m_lastKnownParamValue;
        // Last known frame counter value.
        private int m_lastKnownFrameCounter = -1;

        // Links to child nodes.
        public List<MyParameterNodeMapping> ChildMappings = new List<MyParameterNodeMapping>(4);
        // Name of parameter that controls blending of this node.
        public MyStringId ParameterName;
        // If true: parameter warps around after biggest param value binding. Useful for an angle
        public bool Circular = false;
        // Sensitivity to changes of parameter value. 1=immediate change, 0=no sensitivity.
        public float Sensitivity = 1.0f;
        // Threshold: maximum change of variable to take sensitivity in account, if crossed, value is set immediatelly.
        public float MaxChange = float.PositiveInfinity;
 
        // Update this node. Mixes two nodes from the interval.
        public override void Update(ref MyAnimationUpdateData data)
        {
            // basic check, we do not expect these values to be null, but you never know...
            Debug.Assert(data.Controller != null && data.Controller.Variables != null);
            if (ChildMappings.Count == 0) // no child nodes, no result
            {
                // debug going through animation tree
                data.AddVisitedTreeNodesPathPoint(-1);  // we will go back to parent
                // m_lastKnownFrameCounter = data.Controller.FrameCounter; // do NOT set last known frame counter here, this is invalid state
                return;
            }

            float parameterValue = ComputeParamValue(ref data);

            int index1 = -1;
            for (int i = ChildMappings.Count - 1; i >= 0; i--) // we expect ChildMappings.Count to be less than 5, for loop is ok
                if (ChildMappings[i].ParamValueBinding <= parameterValue)
                {
                    index1 = i;
                    break;
                }

            if (index1 == -1) // simply copy first one
            {
                if (ChildMappings[0].Child != null)
                {
                    // debug going through animation tree
                    data.AddVisitedTreeNodesPathPoint(1);  // we will go to first child

                    ChildMappings[0].Child.Update(ref data);
                    PushLocalTimeToSlaves(0);
                }
                else
                    data.BonesResult = data.Controller.ResultBonesPool.Alloc(); // else return bind pose
            }
            else if (index1 == ChildMappings.Count - 1) // simply copy last one
            {
                if (ChildMappings[index1].Child != null)
                {
                    // debug going through animation tree
                    data.AddVisitedTreeNodesPathPoint(ChildMappings.Count - 1 + 1);  // we will go to last child

                    ChildMappings[index1].Child.Update(ref data);
                    PushLocalTimeToSlaves(index1);
                }
                else
                    data.BonesResult = data.Controller.ResultBonesPool.Alloc(); // else return bind pose
            }
            else // blend between two
            {
                int index2 = index1 + 1;
                float paramBindingDiff = ChildMappings[index2].ParamValueBinding - ChildMappings[index1].ParamValueBinding;
                float t = (parameterValue - ChildMappings[index1].ParamValueBinding) / paramBindingDiff; // division here, improve me!
                if (t > 0.5f) // dominant index will always be index1
                {
                    index1++;
                    index2--;
                    t = 1.0f - t;
                }
                if (t < 0.001f)
                    t = 0.0f;
                else if (t > 0.999f)
                    t = 1.0f;

                var child1 = ChildMappings[index1].Child;
                var child2 = ChildMappings[index2].Child;

                // first (its weight > 0.5) node, dominant
                if (child1 != null && t < 1.0f)
                {
                    // debug going through animation tree
                    data.AddVisitedTreeNodesPathPoint(index1 + 1);  // we will go to dominant child

                    child1.Update(ref data);
                    PushLocalTimeToSlaves(index1);
                }
                else
                    data.BonesResult = data.Controller.ResultBonesPool.Alloc();

                // second (its weight < 0.5) node
                MyAnimationUpdateData animationUpdateData2 = data; // local copy for second one
                if (child2 != null && t > 0.0f)
                {
                    animationUpdateData2.DeltaTimeInSeconds = 0.0; // driven by dominant child 1, do not change you time yourself
                    // debug going through animation tree
                    animationUpdateData2.AddVisitedTreeNodesPathPoint(index2 + 1);  // we will go to dominant child

                    child2.Update(ref animationUpdateData2);
                    data.VisitedTreeNodesCounter = animationUpdateData2.VisitedTreeNodesCounter;
                }
                else
                    animationUpdateData2.BonesResult = animationUpdateData2.Controller.ResultBonesPool.Alloc();

                // and now blend
                for (int j = 0; j < data.BonesResult.Count; j++)
                {
                    if (data.LayerBoneMask[j]) // mix only bones affected by current layer
                    {
                        data.BonesResult[j].Rotation = Quaternion.Slerp(data.BonesResult[j].Rotation,
                            animationUpdateData2.BonesResult[j].Rotation, t);
                        data.BonesResult[j].Translation = Vector3.Lerp(data.BonesResult[j].Translation,
                            animationUpdateData2.BonesResult[j].Translation, t);
                    }
                }
                // and deallocate animationUpdateData2.BonesResult since we no longer need it
                data.Controller.ResultBonesPool.Free(animationUpdateData2.BonesResult);
            }

            // debug going through animation tree
            m_lastKnownFrameCounter = data.Controller.FrameCounter;
            data.AddVisitedTreeNodesPathPoint(-1);  // we will go back to parent
        }

        // Take normalized time from master node (node with the biggest weight) and push it to all other nodes.
        private void PushLocalTimeToSlaves(int masterIndex)
        {
            float localtime = ChildMappings[masterIndex].Child != null ? ChildMappings[masterIndex].Child.GetLocalTimeNormalized() : 0.0f;
            for (int i = 0; i < ChildMappings.Count; i++)
                if (i != masterIndex && ChildMappings[i].Child != null)
                    ChildMappings[i].Child.SetLocalTimeNormalized(localtime);
        }

        // Compute current parameter value.
        private float ComputeParamValue(ref MyAnimationUpdateData data)
        {
            // expecting that ChildMappings.Count > 0, see calling function
            float minVal = ChildMappings[0].ParamValueBinding;
            float maxVal = ChildMappings[ChildMappings.Count - 1].ParamValueBinding;
            float smallestDifferenceToNotice = 0.001f * (maxVal - minVal);

            float currParamValue;
            data.Controller.Variables.GetValue(ParameterName, out currParamValue);
            if (m_lastKnownParamValue.HasValue && data.Controller.FrameCounter - m_lastKnownFrameCounter <= 1)
            {
                if (Circular)
                {
                    // find the closest, wrap around if closer
                    float lastClosestValue = m_lastKnownParamValue.Value;
                    // (diff between current and last)^2
                    float diffNoWrap2 = currParamValue - m_lastKnownParamValue.Value;
                    diffNoWrap2 = diffNoWrap2 * diffNoWrap2;
                    float minDiff2 = diffNoWrap2;
                    // (diff between current and last + range)^2
                    float diffWrapRight = currParamValue - (m_lastKnownParamValue.Value + maxVal - minVal);
                    if (diffWrapRight * diffWrapRight < diffNoWrap2)
                    {
                        lastClosestValue = m_lastKnownParamValue.Value + maxVal - minVal;
                        minDiff2 = diffWrapRight * diffWrapRight;
                    }
                    // (diff between current and last - range)^2
                    float diffWrapLeft = currParamValue - (m_lastKnownParamValue.Value - maxVal + minVal);
                    if (diffWrapLeft * diffWrapLeft < minDiff2)
                    {
                        lastClosestValue = m_lastKnownParamValue.Value - maxVal + minVal;
                        minDiff2 = diffWrapLeft * diffWrapLeft;
                    }

                    // interpolate between cuttent and last
                    float nonwrappedValue = minDiff2 <= MaxChange * MaxChange ? MathHelper.Lerp(lastClosestValue, currParamValue, Sensitivity) : currParamValue;
                    // wrap the value in the interval
                    while (nonwrappedValue < minVal)
                        nonwrappedValue += maxVal - minVal;
                    while (nonwrappedValue > maxVal)
                        nonwrappedValue -= maxVal - minVal;

                    // remember the value
                    if ((m_lastKnownParamValue.Value - nonwrappedValue) * (m_lastKnownParamValue.Value - nonwrappedValue) 
                        > smallestDifferenceToNotice * smallestDifferenceToNotice)
                    {
                        m_lastKnownParamValue = nonwrappedValue;
                    }
                }
                else
                {
                    // interpolate between current and last
                    float diff = (currParamValue - m_lastKnownParamValue.Value);
                    float currentValue = diff * diff <= MaxChange * MaxChange ? MathHelper.Lerp(m_lastKnownParamValue.Value, currParamValue, Sensitivity) : currParamValue;
                    // remember the value
                    if ((m_lastKnownParamValue.Value - currentValue) * (m_lastKnownParamValue.Value - currentValue)
                        > smallestDifferenceToNotice * smallestDifferenceToNotice)
                    {
                        m_lastKnownParamValue = currentValue;
                    }
                }
            }
            else
            {
                m_lastKnownParamValue = currParamValue;
            }
            return m_lastKnownParamValue.Value;
        }

        // Get normalized local time (0-1). Returns local time of first valid child.
        public override float GetLocalTimeNormalized()
        {
            // get the time of first one that is valid
            foreach (var childMapping in ChildMappings)
            {
                if (childMapping.Child != null)
                    return childMapping.Child.GetLocalTimeNormalized();
            }
            return 0.0f;
        }

        // Set normalized local time (0-1) for all children.
        public override void SetLocalTimeNormalized(float normalizedTime)
        {
            foreach (var childMapping in ChildMappings)
            {
                if (childMapping.Child != null)
                    childMapping.Child.SetLocalTimeNormalized(normalizedTime);
            }
        }
    }
}
