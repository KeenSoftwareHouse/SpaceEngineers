using System;
namespace Sandbox.ModAPI.Ingame
{
    public interface IMyCubeBlock : IMyEntity
    {
        event Action<IMyCubeBlock> IsWorkingChanged;
        Sandbox.Common.ObjectBuilders.Definitions.SerializableDefinitionId BlockDefinition { get; }

        bool CheckConnectionAllowed { get; }
        IMyCubeGrid CubeGrid { get; }
        String DefinitionDisplayNameText { get; }
        float DisassembleRatio { get; }
        String DisplayNameText { get; }
        string GetOwnerFactionTag();
        Sandbox.Common.MyRelationsBetweenPlayerAndBlock GetPlayerRelationToOwner();
        Sandbox.Common.MyRelationsBetweenPlayerAndBlock GetUserRelationToOwner(long playerId);
        bool IsBeingHacked { get; }
        bool IsFunctional { get; }
        bool IsWorking { get; }
        VRageMath.Vector3I Max { get; }
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
