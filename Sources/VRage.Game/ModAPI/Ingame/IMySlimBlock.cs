using System;
using VRageMath;
namespace VRage.Game.ModAPI.Ingame
{
    /// <summary>
    /// basic block interface
    /// </summary>
    public interface IMySlimBlock
    {
        float AccumulatedDamage { get; }
        float BuildIntegrity { get; }
        float BuildLevelRatio { get; }
        float CurrentDamage { get; }
        float DamageRatio { get; }
        IMyCubeBlock FatBlock { get; }
        void GetMissingComponents(System.Collections.Generic.Dictionary<string, int> addToDictionary);
        bool HasDeformation { get; }
        bool IsDestroyed { get; }
        bool IsFullIntegrity { get; }
        bool IsFullyDismounted { get; }
        float MaxDeformation { get; }
        float MaxIntegrity { get; }
        /// <summary>
        /// Block mass
        /// </summary>
        float Mass { get; }
        bool ShowParts { get; }
        bool StockpileAllocated { get; }
        bool StockpileEmpty { get; }
        void UpdateVisual();
        Vector3I Position { get; }
        IMyCubeGrid CubeGrid { get; }
    }
}
