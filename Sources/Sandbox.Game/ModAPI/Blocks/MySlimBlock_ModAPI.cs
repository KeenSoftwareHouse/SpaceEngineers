using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Entities.Cube
{
    public partial class MySlimBlock : IMySlimBlock
    {
        IMyCubeBlock IMySlimBlock.FatBlock
        {
            get { return FatBlock; }
        }

        Sandbox.ModAPI.Ingame.IMyCubeBlock Sandbox.ModAPI.Ingame.IMySlimBlock.FatBlock
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

        Common.ObjectBuilders.MyObjectBuilder_CubeBlock IMySlimBlock.GetCopyObjectBuilder()
        {
            return GetCopyObjectBuilder();
        }

        void IMySlimBlock.GetMissingComponents(Dictionary<string, int> addToDictionary)
        {
            GetMissingComponents(addToDictionary);
        }

        Common.ObjectBuilders.MyObjectBuilder_CubeBlock IMySlimBlock.GetObjectBuilder()
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

        Sandbox.ModAPI.Ingame.IMyCubeGrid ModAPI.Ingame.IMySlimBlock.CubeGrid
        {
            get { return CubeGrid; }
        }

        Sandbox.ModAPI.IMyCubeGrid ModAPI.IMySlimBlock.CubeGrid
        {
            get { return CubeGrid; }
        }

        VRageMath.Vector3 IMySlimBlock.GetColorMask()
        {
            return ColorMaskHSV;
        }
    }
}
