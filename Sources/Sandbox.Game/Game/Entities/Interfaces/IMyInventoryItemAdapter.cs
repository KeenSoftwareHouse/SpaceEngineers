using VRage;
using VRage.Utils;

namespace Sandbox.Game.Entities.Interfaces
{
    public interface IMyInventoryItemAdapter
    {
        float Mass { get; }
        float Volume { get; }
        bool HasIntegralAmounts { get; }
        MyFixedPoint MaxStackAmount { get; }
        string DisplayNameText { get; }
        string[] Icons { get; }
        MyStringId? IconSymbol { get; }
    }
}
