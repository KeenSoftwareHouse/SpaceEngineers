
#region Using Statements


#endregion


namespace Sandbox.Engine.Physics
{
    //////////////////////////////////////////////////////////////////////////    
    /// <summary>
    /// Configuration parameters of physics system
    /// </summary>
    public static class MyPhysicsConfig
    {
        public const float CollisionEpsilon = 0.2f;
        public const float Epsilon = 1.0e-6f;
        public const float TriangleEpsilon = 0.02f;
        public const float AllowedPenetration = 0.01f;
        public const float MaxVelMag = 0.5f;
        public const float AABBExtension = 3.0f;
        public const float AabbMultiplier = 1.3f;

        public const float DefaultEnergySleepThreshold = 0.02f;
        public const float DefaultMaxLinearVelocity = 1000.0f;
        public const float DefaultMaxAngularVelocity = 20.0f;
        public const int DefaultIterationCount = 20;
        public const int MaxContactPoints = 3;

        public const int MaxCollidingElements = 256;

        public static float WheelSoftnessRatio = 0.25f;
        public static float WheelSoftnessVelocity = 20;
    }
}