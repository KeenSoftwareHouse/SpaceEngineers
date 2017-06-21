using VRage.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Entity;
using VRageMath;

namespace Sandbox.Game.Entities.Cube
{
    public partial class MySlimBlock : IMySlimBlock
    {
        IMyCubeBlock IMySlimBlock.FatBlock
        {
            get { return FatBlock; }
        }

        VRage.Game.ModAPI.Ingame.IMyCubeBlock VRage.Game.ModAPI.Ingame.IMySlimBlock.FatBlock
        {
            get { return FatBlock; }
        }

        float IMySlimBlock.AccumulatedDamage
        {
            get { return AccumulatedDamage; }
        }

        void IMySlimBlock.AddNeighbours()
        {
            AddNeighbours();
        }

        void IMySlimBlock.ApplyAccumulatedDamage(bool addDirtyParts)
        {
            ApplyAccumulatedDamage(addDirtyParts);
        }

        float IMySlimBlock.BuildIntegrity
        {
            get { return BuildIntegrity; }
        }

        float IMySlimBlock.BuildLevelRatio
        {
            get { return BuildLevelRatio; }
        }

        string IMySlimBlock.CalculateCurrentModel(out VRageMath.Matrix orientation)
        {
            return CalculateCurrentModel(out orientation);
        }

        void IMySlimBlock.ComputeScaledCenter(out VRageMath.Vector3D scaledCenter)
        {
            ComputeScaledCenter(out scaledCenter);
        }

        void IMySlimBlock.ComputeScaledHalfExtents(out VRageMath.Vector3 scaledHalfExtents)
        {
            ComputeScaledHalfExtents(out scaledHalfExtents);
        }

        void IMySlimBlock.ComputeWorldCenter(out VRageMath.Vector3D worldCenter)
        {
            ComputeWorldCenter(out worldCenter);
        }

        float IMySlimBlock.CurrentDamage
        {
            get { return CurrentDamage; }
        }

        float IMySlimBlock.DamageRatio
        {
            get { return DamageRatio; }
        }

        void IMySlimBlock.FixBones(float oldDamage, float maxAllowedBoneMovement)
        {
            FixBones(oldDamage, maxAllowedBoneMovement);
        }

        void IMySlimBlock.FullyDismount(IMyInventory outputInventory)
        {
            FullyDismount(outputInventory as MyInventory);
        }

        MyObjectBuilder_CubeBlock IMySlimBlock.GetCopyObjectBuilder()
        {
            return GetCopyObjectBuilder();
        }

        void IMySlimBlock.GetMissingComponents(Dictionary<string, int> addToDictionary)
        {
            GetMissingComponents(addToDictionary);
        }

        MyObjectBuilder_CubeBlock IMySlimBlock.GetObjectBuilder()
        {
            return GetObjectBuilder();
        }

        bool IMySlimBlock.HasDeformation
        {
            get { return HasDeformation; }
        }

        void IMySlimBlock.InitOrientation(ref VRageMath.Vector3I forward, ref VRageMath.Vector3I up)
        {
            InitOrientation(ref forward, ref up);
        }

        void IMySlimBlock.InitOrientation(VRageMath.Base6Directions.Direction Forward, VRageMath.Base6Directions.Direction Up)
        {
            InitOrientation(Forward, Up);
        }

        void IMySlimBlock.InitOrientation(VRageMath.MyBlockOrientation orientation)
        {
            InitOrientation(orientation);
        }

        bool IMySlimBlock.IsDestroyed
        {
            get { return IsDestroyed; }
        }

        bool IMySlimBlock.IsFullIntegrity
        {
            get { return IsFullIntegrity; }
        }

        bool IMySlimBlock.IsFullyDismounted
        {
            get { return IsFullyDismounted; }
        }

        float IMySlimBlock.MaxDeformation
        {
            get { return MaxDeformation; }
        }

        float IMySlimBlock.MaxIntegrity
        {
            get { return MaxIntegrity; }
        }

        float IMySlimBlock.Mass
        {
            get { return GetMass();  }
        }
        void IMySlimBlock.RemoveNeighbours()
        {
            RemoveNeighbours();
        }

        void IMySlimBlock.SetToConstructionSite()
        {
            SetToConstructionSite();
        }

        bool IMySlimBlock.ShowParts
        {
            get { return ShowParts; }
        }

        void IMySlimBlock.SpawnConstructionStockpile()
        {
            SpawnConstructionStockpile();
        }

        void IMySlimBlock.MoveItemsFromConstructionStockpile(IMyInventory toInventory, MyItemFlags flags)
        {
            MoveItemsFromConstructionStockpile(toInventory as MyInventory, flags);
        }

        void IMySlimBlock.SpawnFirstItemInConstructionStockpile()
        {
            SpawnFirstItemInConstructionStockpile();
        }

        bool IMySlimBlock.StockpileAllocated
        {
            get { return StockpileAllocated; }
        }

        bool IMySlimBlock.StockpileEmpty
        {
            get { return StockpileEmpty; }
        }

        void IMySlimBlock.UpdateVisual()
        {
            UpdateVisual();
        }

        float VRage.Game.ModAPI.Ingame.IMySlimBlock.AccumulatedDamage
        {
            get { return AccumulatedDamage; }
        }

        float VRage.Game.ModAPI.Ingame.IMySlimBlock.BuildIntegrity
        {
            get { return BuildIntegrity; }
        }

        float VRage.Game.ModAPI.Ingame.IMySlimBlock.BuildLevelRatio
        {
            get { return BuildLevelRatio; }
        }

        float VRage.Game.ModAPI.Ingame.IMySlimBlock.CurrentDamage
        {
            get { return CurrentDamage; }
        }

        float VRage.Game.ModAPI.Ingame.IMySlimBlock.DamageRatio
        {
            get { return DamageRatio; }
        }

        void VRage.Game.ModAPI.Ingame.IMySlimBlock.GetMissingComponents(Dictionary<string, int> addToDictionary)
        {
            GetMissingComponents(addToDictionary);
        }

        bool VRage.Game.ModAPI.Ingame.IMySlimBlock.HasDeformation
        {
            get { return HasDeformation; }
        }

        bool VRage.Game.ModAPI.Ingame.IMySlimBlock.IsDestroyed
        {
            get { return IsDestroyed; }
        }

        bool VRage.Game.ModAPI.Ingame.IMySlimBlock.IsFullIntegrity
        {
            get { return IsFullIntegrity; }
        }

        bool VRage.Game.ModAPI.Ingame.IMySlimBlock.IsFullyDismounted
        {
            get { return IsFullyDismounted; }
        }

        float VRage.Game.ModAPI.Ingame.IMySlimBlock.MaxDeformation
        {
            get { return MaxDeformation; }
        }

        float VRage.Game.ModAPI.Ingame.IMySlimBlock.MaxIntegrity
        {
            get { return MaxIntegrity; }
        }

        float VRage.Game.ModAPI.Ingame.IMySlimBlock.Mass
        {
            get { return GetMass(); }
        }

        bool VRage.Game.ModAPI.Ingame.IMySlimBlock.ShowParts
        {
            get { return ShowParts; }
        }

        bool VRage.Game.ModAPI.Ingame.IMySlimBlock.StockpileAllocated
        {
            get { return StockpileAllocated; }
        }

        bool VRage.Game.ModAPI.Ingame.IMySlimBlock.StockpileEmpty
        {
            get { return StockpileEmpty; }
        }

        void VRage.Game.ModAPI.Ingame.IMySlimBlock.UpdateVisual()
        {
            UpdateVisual();
        }

        VRageMath.Vector3I IMySlimBlock.Position
        {
            get
            {
                return Position;
            }
            set
            {
                Position = value;
            }
        }

        VRageMath.Vector3I VRage.Game.ModAPI.Ingame.IMySlimBlock.Position
        {
            get { return Position; }
        }

        VRage.Game.ModAPI.Ingame.IMyCubeGrid VRage.Game.ModAPI.Ingame.IMySlimBlock.CubeGrid
        {
            get { return CubeGrid; }
        }

        VRage.Game.ModAPI.IMyCubeGrid VRage.Game.ModAPI.IMySlimBlock.CubeGrid
        {
            get { return CubeGrid; }
        }

        MyDefinitionBase IMySlimBlock.BlockDefinition
        {
            get
            {
                return BlockDefinition;
            }
        }

        Vector3I IMySlimBlock.Max
        {
            get
            {
                return Max;
            }
        }

        MyBlockOrientation IMySlimBlock.Orientation
        {
            get
            {
                return Orientation;
            }
        }

        List<IMySlimBlock> IMySlimBlock.Neighbours
        {
            get
            {
                return Neighbours.Cast<IMySlimBlock>().ToList();
            }
        }

        VRageMath.Vector3 IMySlimBlock.GetColorMask()
        {
            return ColorMaskHSV;
        }

        void IMySlimBlock.DecreaseMountLevel(float grinderAmount, IMyInventory outputInventory, bool useDefaultDeconstructEfficiency)
        {
            DecreaseMountLevel(grinderAmount, outputInventory as MyInventoryBase, useDefaultDeconstructEfficiency);
        }

        void IMySlimBlock.IncreaseMountLevel(float welderMountAmount, long welderOwnerPlayerId, IMyInventory outputInventory, float maxAllowedBoneMovement, bool isHelping, MyOwnershipShareModeEnum share)
        {
            IncreaseMountLevel(welderMountAmount, welderOwnerPlayerId, outputInventory as MyInventoryBase, maxAllowedBoneMovement, isHelping, share);
        }

        int IMySlimBlock.GetConstructionStockpileItemAmount(MyDefinitionId id)
        {
            return GetConstructionStockpileItemAmount(id);
        }

        void IMySlimBlock.MoveItemsToConstructionStockpile(IMyInventory fromInventory)
        {
            MoveItemsToConstructionStockpile(fromInventory as MyInventoryBase);
        }

        void IMySlimBlock.ClearConstructionStockpile(IMyInventory outputInventory)
        {
            ClearConstructionStockpile(outputInventory as MyInventoryBase);
        }

        bool IMySlimBlock.CanContinueBuild(IMyInventory sourceInventory)
        {
            return CanContinueBuild(sourceInventory as MyInventory);
        }

        void IMySlimBlock.GetWorldBoundingBox(out BoundingBoxD aabb, bool useAABBFromBlockCubes)
        {
            GetWorldBoundingBox(out aabb, useAABBFromBlockCubes);
        }
    }
}
