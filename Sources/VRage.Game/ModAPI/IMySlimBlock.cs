using System;
using VRage.Game;
using VRageMath;
namespace VRage.Game.ModAPI
{
    public interface IMySlimBlock : Ingame.IMySlimBlock
    {
        new float AccumulatedDamage { get; }
        void AddNeighbours();
        void ApplyAccumulatedDamage(bool addDirtyParts = true);
        new float BuildIntegrity { get; }
        new float BuildLevelRatio { get; }
        string CalculateCurrentModel(out VRageMath.Matrix orientation);
        //bool CanContinueBuild(Sandbox.Game.MyInventory sourceInventory);
        //void ClearConstructionStockpile(Sandbox.Game.MyInventory outputInventory);
        void ComputeScaledCenter(out VRageMath.Vector3D scaledCenter);
        void ComputeScaledHalfExtents(out VRageMath.Vector3 scaledHalfExtents);
        void ComputeWorldCenter(out VRageMath.Vector3D worldCenter);
        new float CurrentDamage { get; }
        new float DamageRatio { get; }
        //void DecreaseMountLevel(float grinderAmount, Sandbox.Game.MyInventory outputInventory);
        //void DoDamage(float damage, Sandbox.Game.Weapons.MyStringHash damageType, bool addDirtyParts = true);
        new IMyCubeBlock FatBlock { get; }
        void FixBones(float oldDamage, float maxAllowedBoneMovement);
        void FullyDismount(IMyInventory outputInventory);
        //int GetConstructionStockpileItemAmount(Sandbox.Definitions.MyDefinitionId id);
        MyObjectBuilder_CubeBlock GetCopyObjectBuilder();
        new void GetMissingComponents(System.Collections.Generic.Dictionary<string, int> addToDictionary);
        MyObjectBuilder_CubeBlock GetObjectBuilder();
        new bool HasDeformation { get; }
        //void IncreaseMountLevel(float welderMountAmount, long welderOwnerPlayerId, Sandbox.Game.MyInventory outputInventory = null, float maxAllowedBoneMovement = 0.0f);
        //void Init(Sandbox.Common.ObjectBuilders.MyObjectBuilder_CubeBlock objectBuilder, Sandbox.Game.Entities.MyCubeGrid cubeGrid, Sandbox.Game.Entities.MyCubeBlock fatBlock);
        void InitOrientation(ref VRageMath.Vector3I forward, ref VRageMath.Vector3I up);
        void InitOrientation(VRageMath.Base6Directions.Direction Forward, VRageMath.Base6Directions.Direction Up);
        void InitOrientation(VRageMath.MyBlockOrientation orientation);
        new bool IsDestroyed { get; }
        new bool IsFullIntegrity { get; }
        new bool IsFullyDismounted { get; }
        new float MaxDeformation { get; }
        new float MaxIntegrity { get; }
        new float Mass { get; }
        //void MoveFirstItemToConstructionStockpile(Sandbox.Game.MyInventory fromInventory);
        void MoveItemsFromConstructionStockpile(IMyInventory toInventory, MyItemFlags flags = MyItemFlags.None);
        //void MoveItemsToConstructionStockpile(Sandbox.Game.MyInventory fromInventory);
        //void PlayConstructionSound(Sandbox.Game.Entities.MyCubeGrid.MyIntegrityChangeEnum integrityChangeType, bool deconstruction = false);
        void RemoveNeighbours();
        void SetToConstructionSite();
        new bool ShowParts { get; }
        void SpawnConstructionStockpile();
        void SpawnFirstItemInConstructionStockpile();
        new bool StockpileAllocated { get; }
        new bool StockpileEmpty { get; }
        new void UpdateVisual();
        new Vector3I Position { get; set; }
        new IMyCubeGrid CubeGrid { get; }
        VRageMath.Vector3 GetColorMask();
    }
}
