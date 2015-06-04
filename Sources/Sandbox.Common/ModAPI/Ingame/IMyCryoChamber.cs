namespace Sandbox.ModAPI.Ingame
{
    public interface IMyCryoChamber : IMyCockpit
    {
        bool IsOccupied { get; }

        AstronautInfo GetOccupantInfo();
    }
}