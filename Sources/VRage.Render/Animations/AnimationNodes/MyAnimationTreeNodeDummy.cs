namespace VRageRender.Animations
{
    /// <summary>
    /// Node of animation tree: single track. Contains reference to animation clip.
    /// </summary>
    public class MyAnimationTreeNodeDummy : MyAnimationTreeNode
    {
        // Local normalized time
        private float m_localNormalizedTime = 0;
        
        // --------------- constructor --------------------------------------------------------

        // Default constructor.
        public MyAnimationTreeNodeDummy()
        {
        }

        // --------------- methods (public) --------------------------------------------------

        // Update animation node = compute bones positions and orientations from the track. 
        public override void Update(ref MyAnimationUpdateData data)
        {
            // debug going through animation tree
            data.BonesResult = data.Controller.ResultBonesPool.Alloc();
            data.AddVisitedTreeNodesPathPoint(-1);  // finishing this node, we will go back to parent
        }

        // Get local time in normalized format (from 0 to 1).
        // May fail for more complicated structure - there can be more independent local times (each track has its own).
        public override float GetLocalTimeNormalized()
        {
            return m_localNormalizedTime;
        }

        // Set local time in normalized format (from 0 to 1).
        public override void SetLocalTimeNormalized(float normalizedTime)
        {
            m_localNormalizedTime = normalizedTime;
        }
    }
}
