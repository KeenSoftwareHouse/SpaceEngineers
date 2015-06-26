using VRage.Utils;

namespace Sandbox.Game.Entities.Interfaces
{
    public interface IMyInventoryItemAdapter
    {
        float Mass { get; }
        float Volume { get; }
        string DisplayNameText { get; }
        string Icon { get; }
        MyStringId? IconSymbol { get; }
    }
}
