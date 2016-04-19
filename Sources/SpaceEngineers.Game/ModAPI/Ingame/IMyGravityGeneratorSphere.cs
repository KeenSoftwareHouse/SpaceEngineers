namespace SpaceEngineers.Game.ModAPI.Ingame
{
    public interface IMyGravityGeneratorSphere : IMyGravityGeneratorBase
    {
        float Radius { get; }
        float Gravity { get; }
    }
}
