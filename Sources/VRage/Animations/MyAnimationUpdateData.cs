using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Animations
{
    /// <summary>
    /// Helper structure passed as an parameter during computation of current pose.
    /// </summary>
    public struct MyAnimationUpdateData
    {
        // time passed from last computation
        // (IN)
        public double DeltaTimeInSeconds;

        // link to animation controller, item is set automatically in MyAnimationController.Update
        // if you are looking for variables - this contains variable storage!
        // (IN, but leave it null, non-null value overrides standard behavior)
        public MyAnimationController Controller;

        // Reference to character bones. Content should not be changed inside AnimationController.
        // Note: if you want to change this behavior, please change this comment as well, thx.
        // (IN)
        public MyCharacterBone[] CharacterBones;

        // Reference to bone mask of current layer.
        // (IN/OUT, set in MyAnimationStateMachine which represents layer)
        public bool[] LayerBoneMask;

        // link to list of bones, it can be used for passing the result of animation tree node 
        // (OUT)
        public List<MyAnimationClip.BoneState> BonesResult;
    }
}
