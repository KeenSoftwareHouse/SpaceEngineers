using System;
using VRageMath;
namespace Sandbox.ModAPI
{
    public interface IMySlimBlock : Ingame.IMySlimBlock
    {
        float AccumulatedDamage { get; }
        void AddNeighbours();
        void ApplyAccumulatedDamage(bool addDirtyParts = true);
        float BuildIntegrity { get; }
        float BuildLevelRatio { get; }
        string CalculateCurrentModel(out VRageMath.Matrix orientation);
        //bool CanContinueBuild(Sandbox.Game.MyInventory sourceInventory);
        //void ClearConstructionStockpile(Sandbox.Game.MyInventory outputInventory);
        void ComputeScaledCenter(out VRageMath.Vector3D scaledCenter);
        void ComputeScaledHalfExtents(out VRageMath.Vector3 scaledHalfExtents);
        void ComputeWorldCenter(out VRageMath.Vector3D worldCenter);
        float CurrentDamage { get; }
        float DamageRatio { get; }
        //void DecreaseMountLevel(float grinderAmount, Sandbox.Game.MyInventory outputInventory);
        //void DoDamage(float damage, Sandbox.Game.Weapons.MyDamageType damageType, bool addDirtyParts = true);
        IMyCubeBlock FatBlock { get; }
        void FixBones(float oldDamage, float maxAllowedBoneMovement);
        //int GetConstructionStockpileItemAmount(Sandbox.Definitions.MyDefinitionId id);
        Sandbox.Common.ObjectBuilders.MyObjectBuilder_CubeBlock GetCopyObjectBuilder();
        void GetMissingComponents(System.Collections.Generic.Dictionary<string, int> addToDictionary);
        Sandbox.Common.ObjectBuilders.MyObjectBuilder_CubeBlock GetObjectBuilder();
        bool HasDeformation { get; }
        //void IncreaseMountLevel(float welderMountAmount, long welderOwnerPlayerId, Sandbox.Game.MyInventory outputInventory = null, float maxAllowedBoneMovement = 0.0f);
        //void Init(Sandbox.Common.ObjectBuilders.MyObjectBuilder_CubeBlock objectBuilder, Sandbox.Game.Entities.MyCubeGrid cubeGrid, Sandbox.Game.Entities.MyCubeBlock fatBlock);
        void InitOrientation(ref VRageMath.Vector3I forward, ref VRageMath.Vector3I up);
        void InitOrientation(VRageMath.Base6Directions.Direction Forward, VRageMath.Base6Directions.Direction Up);
        void InitOrientation(VRageMath.MyBlockOrientation orientation);
        bool IsDestroyed { get; }
        bool IsFullIntegrity { get; }
        bool IsFullyDismounted { get; }
        float MaxDeformation { get; }
        float MaxIntegrity { get; }
        float Mass { get; }
        //void MoveFirstItemToConstructionStockpile(Sandbox.Game.MyInventory fromInventory);
        //void MoveItemsFromConstructionStockpile(Sandbox.Game.MyInventory toInventory, Sandbox.Common.ObjectBuilders.MyItemFlags flags = MyItemFlags.None);
        //void MoveItemsToConstructionStockpile(Sandbox.Game.MyInventory fromInventory);
        //void PlayConstructionSound(Sandbox.Game.Entities.MyCubeGrid.MyIntegrityChangeEnum integrityChangeType, bool deconstruction = false);
        void RemoveNeighbours();
        void SetToConstructionSite();
        bool ShowParts { get; }
        void SpawnConstructionStockpile();
        void SpawnFirstItemInConstructionStockpile();
        bool StockpileAllocated { get; }
        bool StockpileEmpty { get; }
        void UpdateVisual();
        Vector3I Position { get; set; }
        IMyCubeGrid CubeGrid { get; }
        VRageMath.Vector3 GetColorMask();
    }
}
