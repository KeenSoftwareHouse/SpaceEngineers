namespace SpaceEngineers.Game.ModAPI.Ingame
{
    public interface IMyGravityGenerator : IMyGravityGeneratorBase
    {
        float FieldWidth { get; }
        float FieldHeight { get; }
        float FieldDepth { get; }
        float Gravity { get; }
    }
}
