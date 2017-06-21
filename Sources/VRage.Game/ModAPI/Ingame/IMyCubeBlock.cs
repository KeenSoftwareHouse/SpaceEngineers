using System;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace VRage.Game.ModAPI.Ingame
{
    /// <summary>
    /// Basic cube interface
    /// </summary>
    public interface IMyCubeBlock : IMyEntity
    {
        SerializableDefinitionId BlockDefinition { get; }

        bool CheckConnectionAllowed { get; }
        IMyCubeGrid CubeGrid { get; }
        String DefinitionDisplayNameText { get; }
        float DisassembleRatio { get; }
        String DisplayNameText { get; }
        string GetOwnerFactionTag();
        VRage.Game.MyRelationsBetweenPlayerAndBlock GetPlayerRelationToOwner();
        VRage.Game.MyRelationsBetweenPlayerAndBlock GetUserRelationToOwner(long playerId);
        bool IsBeingHacked { get; }
        bool IsFunctional { get; }
        bool IsWorking { get; }
        VRageMath.Vector3I Max { get; }

        /// <summary>
        /// Block mass
        /// </summary>
        float Mass { get; }
        VRageMath.Vector3I Min { get; }
        int NumberInGrid { get; }
        VRageMath.MyBlockOrientation Orientation { get; }
        long OwnerId { get; }
        VRageMath.Vector3I Position { get; }
        //void ReleaseInventory(Sandbox.Game.MyInventory inventory, bool damageContent = false);
        void UpdateIsWorking();
        void UpdateVisual();
    }
}
