namespace SpaceEngineers.Game.ModAPI.Ingame
{
    public interface IMyGravityGeneratorSphere : IMyGravityGeneratorBase
    {
        /// <summary>
        /// Radius of the gravity field, in meters
        /// </summary>
        float Radius { get; set; }
    }
}
