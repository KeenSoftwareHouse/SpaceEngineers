using System.Collections.Generic;

namespace VRageRender.Animations
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

        // movements in animation tree, positive - go to child nr x, negative - go back to parent, zero - end
        // (IN/OUT)
        public int[] VisitedTreeNodesPath;

        // current number of elements in VisitedTreeNodesPath
        // (IN/OUT)
        public int VisitedTreeNodesCounter;

        // Helper function, store walking through animation controller.
        public void AddVisitedTreeNodesPathPoint(int nextPoint)
        {
            if (VisitedTreeNodesPath != null && VisitedTreeNodesCounter < VisitedTreeNodesPath.Length)
            {
                VisitedTreeNodesPath[VisitedTreeNodesCounter] = nextPoint;
                VisitedTreeNodesCounter++;
            }
        }
    }
}
