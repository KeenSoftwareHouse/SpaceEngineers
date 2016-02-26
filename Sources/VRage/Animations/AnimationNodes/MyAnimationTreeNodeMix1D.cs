using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace VRage.Animations.AnimationNodes
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

        // Links to child nodes.
        public List<MyParameterNodeMapping> ChildMappings = new List<MyParameterNodeMapping>(4);
        // Name of parameter that controls blending of this node.
        public MyStringId ParameterName;
 
        // Update this node. Mixes two nodes from the interval.
        public override void Update(ref MyAnimationUpdateData data)
        {
            // basic check, we do not expect these values to be null, but you never know...
            Debug.Assert(data.Controller != null && data.Controller.Variables != null);
            if (ChildMappings.Count == 0) // no child nodes, no result
                return;

            float parameterValue;
            data.Controller.Variables.GetValue(ParameterName, out parameterValue);

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
                    ChildMappings[0].Child.Update(ref data);
                else
                    data.BonesResult = data.Controller.ResultBonesPool.Alloc(); // else return bind pose
            }
            else if (index1 == ChildMappings.Count - 1) // simply copy last one
            {
                if (ChildMappings[index1].Child != null)
                    ChildMappings[index1].Child.Update(ref data);
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
                var child1 = ChildMappings[index1].Child;
                var child2 = ChildMappings[index2].Child;

                // first (its weight > 0.5) node, dominant
                if (child1 != null)
                    child1.Update(ref data);
                else
                    data.BonesResult = data.Controller.ResultBonesPool.Alloc();

                // second (its weight < 0.5) node
                MyAnimationUpdateData animationUpdateData2 = data; // local copy for second one
                if (child2 != null)
                {
                    if (child1 != null)
                    {
                        animationUpdateData2.DeltaTimeInSeconds = 0.0; // driven by dominant child 1, do not change you time yourself
                        child2.SetLocalTimeNormalized(child1.GetLocalTimeNormalized());
                    }
                    child2.Update(ref animationUpdateData2);
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
        }

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
