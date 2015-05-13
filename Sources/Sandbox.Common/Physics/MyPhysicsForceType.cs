namespace Sandbox.Engine.Physics
{
    /// <summary>
    /// Force type applied to physic object.
    /// </summary>
    public enum MyPhysicsForceType : byte
    {
        /// <summary>
        /// 
        /// </summary>
        APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE,

        /// <summary>
        /// 
        /// </summary>
        ADD_BODY_FORCE_AND_BODY_TORQUE,

        APPLY_WORLD_FORCE
    }
}