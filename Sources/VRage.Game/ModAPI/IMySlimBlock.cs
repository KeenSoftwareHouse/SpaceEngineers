using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ModAPI.Interfaces;
using VRageMath;

namespace VRage.Game.ModAPI
{
    public interface IMySlimBlock : Ingame.IMySlimBlock, IMyDestroyableObject, IMyDecalProxy
    {
        float AccumulatedDamage { get; }
        void AddNeighbours();
        void ApplyAccumulatedDamage(bool addDirtyParts = true);
        float BuildIntegrity { get; }
        float BuildLevelRatio { get; }
        string CalculateCurrentModel(out VRageMath.Matrix orientation);
        void ComputeScaledCenter(out VRageMath.Vector3D scaledCenter);
        void ComputeScaledHalfExtents(out VRageMath.Vector3 scaledHalfExtents);
        void ComputeWorldCenter(out VRageMath.Vector3D worldCenter);
        float CurrentDamage { get; }
        float DamageRatio { get; }
        //void DoDamage(float damage, Sandbox.Game.Weapons.MyStringHash damageType, bool addDirtyParts = true);
        IMyCubeBlock FatBlock { get; }
        void FixBones(float oldDamage, float maxAllowedBoneMovement);
        void FullyDismount(IMyInventory outputInventory);
        MyObjectBuilder_CubeBlock GetCopyObjectBuilder();
        void GetMissingComponents(System.Collections.Generic.Dictionary<string, int> addToDictionary);
        MyObjectBuilder_CubeBlock GetObjectBuilder();
        bool HasDeformation { get; }
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
        void MoveItemsFromConstructionStockpile(IMyInventory toInventory, MyItemFlags flags = MyItemFlags.None);
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

        /// ---
        /// <summary>
        /// Decreases the build level of a block
        /// </summary>
        /// <param name="grinderAmount">The integrity amount of change</param>
        /// <param name="outputInventory">The inventory where output components will be sent to</param>
        /// <param name="useDefaultDeconstructEfficiency"></param>
        void DecreaseMountLevel(float grinderAmount, IMyInventory outputInventory, bool useDefaultDeconstructEfficiency = false);
        /// <summary>
        /// Increases the build level of a block
        /// </summary>
        /// <param name="welderMountAmount">The integrity amount of change</param>
        /// <param name="welderOwnerPlayerId">The player id of the entity increasing the mount level</param>
        /// <param name="outputInventory">The inventory where components are taken from</param>
        /// <param name="maxAllowedBoneMovement">Maximum movement of bones</param>
        /// <param name="isHelping">Is this increase helping another player</param>
        /// <param name="share">ShareMode used when block becomes functional</param>
        void IncreaseMountLevel(float welderMountAmount, long welderOwnerPlayerId, IMyInventory outputInventory = null, float maxAllowedBoneMovement = 0, bool isHelping = false, MyOwnershipShareModeEnum share = MyOwnershipShareModeEnum.Faction);
        /// <summary>
        /// Get the amount of items in the construction stockpile
        /// </summary>
        /// <param name="id">Definition of component in stockpile to check</param>
        /// <returns>Amount of components in the stockpile of this type</returns>
        int GetConstructionStockpileItemAmount(MyDefinitionId id);
        /// <summary>
        /// Move items missing from an inventory into the construction stockpile
        /// </summary>
        /// <param name="fromInventory">The inventory where the components are being taken from</param>
        void MoveItemsToConstructionStockpile(IMyInventory fromInventory);
        /// <summary>
        /// Clears out the construction stockpile and moves the components into a destination inventory
        /// </summary>
        /// <param name="outputInventory">The inventory where the components are moved into</param>
        void ClearConstructionStockpile(IMyInventory outputInventory);
        /// <summary>
        /// Play the construction sound associated with the integrity change
        /// </summary>
        /// <param name="integrityChangeType">Type of integrity change</param>
        /// <param name="deconstruction">Is this deconstruction?</param>
        void PlayConstructionSound(MyIntegrityChangeEnum integrityChangeType, bool deconstruction = false);
        /// <summary>
        /// Can we continue to weld this block?
        /// </summary>
        /// <param name="sourceInventory">Source inventory that is used for components</param>
        /// <returns></returns>
        bool CanContinueBuild(IMyInventory sourceInventory);
        /// <summary>
        /// The blocks definition
        /// </summary>
        MyDefinitionBase BlockDefinition { get; }
        /// <summary>
        /// Largest part of block
        /// </summary>
        Vector3I Max { get; }
        /// <summary>
        /// Blocks orientation
        /// </summary>
        MyBlockOrientation Orientation { get; }
        /// <summary>
        /// The blocks that neighbour this block
        /// </summary>
        List<IMySlimBlock> Neighbours { get; }
        /// <summary>
        /// The AABB of this block
        /// </summary>
        /// <param name="aabb"></param>
        /// <param name="useAABBFromBlockCubes"></param>
        void GetWorldBoundingBox(out BoundingBoxD aabb, bool useAABBFromBlockCubes = false);
    }
}
