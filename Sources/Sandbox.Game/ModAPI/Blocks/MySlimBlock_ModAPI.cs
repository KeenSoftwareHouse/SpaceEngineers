using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Sandbox.Game.Entities.Cube
{
    public partial class MySlimBlock : IMySlimBlock
    {
        ModAPI.Ingame.IMyCubeBlock ModAPI.Ingame.IMySlimBlock.FatBlock
        {
            get { return FatBlock; }
        }

        void IMySlimBlock.AddNeighbours()
        {
            AddNeighbours();
        }

        void IMySlimBlock.ApplyAccumulatedDamage(bool addDirtyParts)
        {
            ApplyAccumulatedDamage(addDirtyParts);
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

        MyObjectBuilder_CubeBlock IMySlimBlock.GetObjectBuilder()
        {
            return GetObjectBuilder();
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

        void IMySlimBlock.RemoveNeighbours()
        {
            RemoveNeighbours();
        }

        void IMySlimBlock.SetToConstructionSite()
        {
            SetToConstructionSite();
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

        float ModAPI.Ingame.IMySlimBlock.AccumulatedDamage
        {
            get { return AccumulatedDamage; }
        }

        float ModAPI.Ingame.IMySlimBlock.BuildIntegrity
        {
            get { return BuildIntegrity; }
        }

        float ModAPI.Ingame.IMySlimBlock.BuildLevelRatio
        {
            get { return BuildLevelRatio; }
        }

        float ModAPI.Ingame.IMySlimBlock.CurrentDamage
        {
            get { return CurrentDamage; }
        }

        float ModAPI.Ingame.IMySlimBlock.DamageRatio
        {
            get { return DamageRatio; }
        }

        void ModAPI.Ingame.IMySlimBlock.GetMissingComponents(Dictionary<string, int> addToDictionary)
        {
            GetMissingComponents(addToDictionary);
        }

        bool ModAPI.Ingame.IMySlimBlock.HasDeformation
        {
            get { return HasDeformation; }
        }

        bool ModAPI.Ingame.IMySlimBlock.IsDestroyed
        {
            get { return IsDestroyed; }
        }

        bool ModAPI.Ingame.IMySlimBlock.IsFullIntegrity
        {
            get { return IsFullIntegrity; }
        }

        bool ModAPI.Ingame.IMySlimBlock.IsFullyDismounted
        {
            get { return IsFullyDismounted; }
        }

        float ModAPI.Ingame.IMySlimBlock.MaxDeformation
        {
            get { return MaxDeformation; }
        }

        float ModAPI.Ingame.IMySlimBlock.MaxIntegrity
        {
            get { return MaxIntegrity; }
        }

        float ModAPI.Ingame.IMySlimBlock.Mass
        {
            get { return GetMass(); }
        }

        bool ModAPI.Ingame.IMySlimBlock.ShowParts
        {
            get { return ShowParts; }
        }

        bool ModAPI.Ingame.IMySlimBlock.StockpileAllocated
        {
            get { return StockpileAllocated; }
        }

        bool ModAPI.Ingame.IMySlimBlock.StockpileEmpty
        {
            get { return StockpileEmpty; }
        }

        void ModAPI.Ingame.IMySlimBlock.UpdateVisual()
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

        VRageMath.Vector3I ModAPI.Ingame.IMySlimBlock.Position
        {
            get { return Position; }
        }

        ModAPI.Ingame.IMyCubeGrid ModAPI.Ingame.IMySlimBlock.CubeGrid
        {
            get { return CubeGrid; }
        }

        MyDefinitionId ModAPI.Ingame.IMySlimBlock.BlockDefinition
        {
            get { return BlockDefinition.Id; }
        }

        VRageMath.Vector3 IMySlimBlock.GetColorMask()
        {
            return ColorMaskHSV;
        }
    }
}
