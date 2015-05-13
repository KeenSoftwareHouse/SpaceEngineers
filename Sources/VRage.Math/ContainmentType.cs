namespace VRageMath
{
    /// <summary>
    /// Indicates the extent to which bounding volumes intersect or contain one another.
    /// </summary>
    /// <param name="Contains">Indicates that one bounding volume completely contains the other.</param><param name="Disjoint">Indicates there is no overlap between the bounding volumes.</param><param name="Intersects">Indicates that the bounding volumes partially overlap.</param>
    public enum ContainmentType
    {
        Disjoint,
        Contains,
        Intersects,
    }
}
