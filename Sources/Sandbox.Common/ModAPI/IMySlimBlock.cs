using System;
using VRage.Game;
using VRageMath;
namespace Sandbox.ModAPI
{
    public interface IMySlimBlock : Ingame.IMySlimBlock
    {
        void AddNeighbours();
        void ApplyAccumulatedDamage(bool addDirtyParts = true);
        string CalculateCurrentModel(out VRageMath.Matrix orientation);
        //bool CanContinueBuild(Sandbox.Game.MyInventory sourceInventory);
        //void ClearConstructionStockpile(Sandbox.Game.MyInventory outputInventory);
        void ComputeScaledCenter(out VRageMath.Vector3D scaledCenter);
        void ComputeScaledHalfExtents(out VRageMath.Vector3 scaledHalfExtents);
        void ComputeWorldCenter(out VRageMath.Vector3D worldCenter);
        //void DecreaseMountLevel(float grinderAmount, Sandbox.Game.MyInventory outputInventory);
        //void DoDamage(float damage, Sandbox.Game.Weapons.MyStringHash damageType, bool addDirtyParts = true);
        void FixBones(float oldDamage, float maxAllowedBoneMovement);
        void FullyDismount(IMyInventory outputInventory);
        //int GetConstructionStockpileItemAmount(Sandbox.Definitions.MyDefinitionId id);
        MyObjectBuilder_CubeBlock GetCopyObjectBuilder();
        MyObjectBuilder_CubeBlock GetObjectBuilder();
        //void IncreaseMountLevel(float welderMountAmount, long welderOwnerPlayerId, Sandbox.Game.MyInventory outputInventory = null, float maxAllowedBoneMovement = 0.0f);
        //void Init(Sandbox.Common.ObjectBuilders.MyObjectBuilder_CubeBlock objectBuilder, Sandbox.Game.Entities.MyCubeGrid cubeGrid, Sandbox.Game.Entities.MyCubeBlock fatBlock);
        void InitOrientation(ref VRageMath.Vector3I forward, ref VRageMath.Vector3I up);
        void InitOrientation(VRageMath.Base6Directions.Direction Forward, VRageMath.Base6Directions.Direction Up);
        void InitOrientation(VRageMath.MyBlockOrientation orientation);
        //void MoveFirstItemToConstructionStockpile(Sandbox.Game.MyInventory fromInventory);
        void MoveItemsFromConstructionStockpile(IMyInventory toInventory, MyItemFlags flags = MyItemFlags.None);
        //void MoveItemsToConstructionStockpile(Sandbox.Game.MyInventory fromInventory);
        //void PlayConstructionSound(Sandbox.Game.Entities.MyCubeGrid.MyIntegrityChangeEnum integrityChangeType, bool deconstruction = false);
        void RemoveNeighbours();
        void SetToConstructionSite();
        void SpawnConstructionStockpile();
        void SpawnFirstItemInConstructionStockpile();
        new Vector3I Position { get; set; } // overwrite intentional
        VRageMath.Vector3 GetColorMask();
    }
}
