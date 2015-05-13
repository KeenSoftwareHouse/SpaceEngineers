namespace VRageMath
{
    /// <summary>
    /// Describes the intersection between a plane and a bounding volume.
    /// </summary>
    /// <param name="Back">There is no intersection, and the bounding volume is in the negative half-space of the Plane.</param><param name="Front">There is no intersection, and the bounding volume is in the positive half-space of the Plane.</param><param name="Intersecting">The Plane is intersected.</param>
    public enum PlaneIntersectionType
    {
        Front,
        Back,
        Intersecting,
    }
}
