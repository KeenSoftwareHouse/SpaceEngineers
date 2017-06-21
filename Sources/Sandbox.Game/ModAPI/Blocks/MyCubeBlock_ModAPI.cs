using VRage.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using Sandbox.ModAPI;

namespace Sandbox.Game.Entities
{
    public partial class MyCubeBlock : IMyCubeBlock, IMyUpgradableBlock
    {
        SerializableDefinitionId VRage.Game.ModAPI.IMyCubeBlock.BlockDefinition { get { return BlockDefinition.Id; } }
        SerializableDefinitionId VRage.Game.ModAPI.Ingame.IMyCubeBlock.BlockDefinition { get { return BlockDefinition.Id; } }

        public void Init(MyObjectBuilder_CubeBlock builder, IMyCubeGrid cubeGrid)
        {
            if(cubeGrid is MyCubeGrid)
                Init(builder, cubeGrid as MyCubeGrid);
        }

        Action<MyCubeBlock> GetDelegate(Action<VRage.Game.ModAPI.IMyCubeBlock> value)
        {
            return (Action<MyCubeBlock>)Delegate.CreateDelegate(typeof(Action<MyCubeBlock>), value.Target, value.Method);
        }

        event Action<VRage.Game.ModAPI.IMyCubeBlock> VRage.Game.ModAPI.IMyCubeBlock.IsWorkingChanged
        {
            add { IsWorkingChanged += GetDelegate(value); }
            remove { IsWorkingChanged -= GetDelegate(value); }
        }

        IMyCubeGrid IMyCubeBlock.CubeGrid { get { return CubeGrid; } }
        VRage.Game.ModAPI.Ingame.IMyCubeGrid VRage.Game.ModAPI.Ingame.IMyCubeBlock.CubeGrid { get { return CubeGrid; } }

        void IMyCubeBlock.CalcLocalMatrix(out VRageMath.Matrix localMatrix, out string currModel)
        {
            CalcLocalMatrix(out localMatrix, out currModel);
        }

        string IMyCubeBlock.CalculateCurrentModel(out VRageMath.Matrix orientation)
        {
            return CalculateCurrentModel(out orientation);
        }

        bool IMyCubeBlock.CheckConnectionAllowed
        {
            get
            {
                return CheckConnectionAllowed;
            }
            set
            {
                CheckConnectionAllowed = value;
            }
        }

        bool IMyCubeBlock.DebugDraw()
        {
             DebugDraw();
             return true;
        }

        String IMyCubeBlock.DefinitionDisplayNameText
        {
            get { return DefinitionDisplayNameText; }
        }

        float IMyCubeBlock.DisassembleRatio
        {
            get { return DisassembleRatio; }
        }

        String IMyCubeBlock.DisplayNameText
        {
            get { return DisplayNameText; }
        }

        MyObjectBuilder_CubeBlock IMyCubeBlock.GetObjectBuilderCubeBlock(bool copy)
        {
            return GetObjectBuilderCubeBlock(copy);
        }

        string IMyCubeBlock.GetOwnerFactionTag()
        {
            return GetOwnerFactionTag();
        }

        VRage.Game.MyRelationsBetweenPlayerAndBlock IMyCubeBlock.GetPlayerRelationToOwner()
        {
            return GetPlayerRelationToOwner();
        }

        VRage.Game.MyRelationsBetweenPlayerAndBlock IMyCubeBlock.GetUserRelationToOwner(long playerId)
        {
            return GetUserRelationToOwner(playerId);
        }

        void IMyCubeBlock.Init()
        {
            Init();
        }

        void IMyCubeBlock.Init(MyObjectBuilder_CubeBlock builder, IMyCubeGrid cubeGrid)
        {
            Init(builder, cubeGrid);
        }

        bool IMyCubeBlock.IsBeingHacked
        {
            get { return IsBeingHacked; }
        }

        bool IMyCubeBlock.IsFunctional
        {
            get { return IsFunctional; }
        }

        bool IMyCubeBlock.IsWorking
        {
            get { return IsWorking; }
        }

        VRageMath.Vector3I IMyCubeBlock.Max
        {
            get { return Max; }
        }

        float IMyCubeBlock.Mass
        {
            get { return GetMass(); }
        }

        float VRage.Game.ModAPI.Ingame.IMyCubeBlock.Mass
        {
            get { return GetMass(); }
        }
        VRageMath.Vector3I IMyCubeBlock.Min
        {
            get { return Min; }
        }

        int IMyCubeBlock.NumberInGrid
        {
            get
            {
                return NumberInGrid;
            }
            set
            {
                NumberInGrid = value;
            }
        }

        void IMyCubeBlock.OnBuildSuccess(long builtBy)
        {
            OnBuildSuccess(builtBy);
        }

        void IMyCubeBlock.OnDestroy()
        {
            OnDestroy();
        }

        void IMyCubeBlock.OnModelChange()
        {
            OnModelChange();
        }

        void IMyCubeBlock.OnRegisteredToGridSystems()
        {
            OnRegisteredToGridSystems();
        }

        void IMyCubeBlock.OnRemovedByCubeBuilder()
        {
            OnRemovedByCubeBuilder();
        }

        void IMyCubeBlock.OnUnregisteredFromGridSystems()
        {
            OnUnregisteredFromGridSystems();
        }

        VRageMath.MyBlockOrientation IMyCubeBlock.Orientation
        {
            get { return Orientation; }
        }

        long IMyCubeBlock.OwnerId
        {
            get { return OwnerId; }
        }

        VRageMath.Vector3I IMyCubeBlock.Position
        {
            get { return Position; }
        }

        string IMyCubeBlock.RaycastDetectors(VRageMath.Vector3 worldFrom, VRageMath.Vector3 worldTo)
        {
            return Components.Get<MyUseObjectsComponentBase>().RaycastDetectors(worldFrom, worldTo);
        }

        void IMyCubeBlock.ReloadDetectors(bool refreshNetworks)
        {
            Components.Get<MyUseObjectsComponentBase>().LoadDetectorsFromModel();
        }

        void IMyCubeBlock.UpdateIsWorking()
        {
            UpdateIsWorking();
        }

        void IMyCubeBlock.UpdateVisual()
        {
            UpdateVisual();
        }

        void IMyCubeBlock.SetDamageEffect(bool start)
        {
            SetDamageEffect(start);
        }

        IMySlimBlock IMyCubeBlock.SlimBlock
        {
            get
            {
                return SlimBlock;
            }
        }

        uint ModAPI.Ingame.IMyUpgradableBlock.UpgradeCount
        {
            get
            {
                return (uint)UpgradeValues.Count;
            }
        }

        void ModAPI.Ingame.IMyUpgradableBlock.GetUpgrades(out Dictionary<string, float> upgrades)
        {
            upgrades = new Dictionary<string, float>();
            foreach (var value in UpgradeValues)
                upgrades.Add(value.Key, value.Value);
        }
    }
}
