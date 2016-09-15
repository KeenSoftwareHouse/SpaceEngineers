namespace VRageRender.Animations
{
    /// <summary>
    /// Interface representing one node in animation tree.
    /// </summary>
    public abstract class MyAnimationTreeNode
    {
        public abstract void Update(ref MyAnimationUpdateData data);

        // Get local time in normalized format (from 0 to 1).
        // May fail for more complicated structure - there can be more independent local times (each track has its own).
        public abstract float GetLocalTimeNormalized();
        // Set local time in normalized format (from 0 to 1).
        public abstract void SetLocalTimeNormalized(float normalizedTime);
    }
}
