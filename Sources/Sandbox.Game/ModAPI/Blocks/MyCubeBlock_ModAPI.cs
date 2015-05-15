using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Entities
{
    public partial class MyCubeBlock : IMyCubeBlock
    {
        Sandbox.Common.ObjectBuilders.Definitions.SerializableDefinitionId Sandbox.ModAPI.IMyCubeBlock.BlockDefinition { get { return BlockDefinition.Id; } }
        Sandbox.Common.ObjectBuilders.Definitions.SerializableDefinitionId Sandbox.ModAPI.Ingame.IMyCubeBlock.BlockDefinition { get { return BlockDefinition.Id; } }

        public void Init(Common.ObjectBuilders.MyObjectBuilder_CubeBlock builder, IMyCubeGrid cubeGrid)
        {
            if(cubeGrid is MyCubeGrid)
                Init(builder, cubeGrid as MyCubeGrid);
        }

        Action<MyCubeBlock> GetDelegate(Action<ModAPI.IMyCubeBlock> value)
        {
            return (Action<MyCubeBlock>)Delegate.CreateDelegate(typeof(Action<MyCubeBlock>), value.Target, value.Method);
        }

        event Action<Sandbox.ModAPI.IMyCubeBlock> Sandbox.ModAPI.IMyCubeBlock.IsWorkingChanged
        {
            add { IsWorkingChanged += GetDelegate(value); }
            remove { IsWorkingChanged -= GetDelegate(value); }
        }

        IMyCubeGrid IMyCubeBlock.CubeGrid { get { return CubeGrid; } }
        Sandbox.ModAPI.Ingame.IMyCubeGrid Sandbox.ModAPI.Ingame.IMyCubeBlock.CubeGrid { get { return CubeGrid; } }

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

        Common.ObjectBuilders.MyObjectBuilder_CubeBlock IMyCubeBlock.GetObjectBuilderCubeBlock(bool copy)
        {
            return GetObjectBuilderCubeBlock(copy);
        }

        string IMyCubeBlock.GetOwnerFactionTag()
        {
            return GetOwnerFactionTag();
        }

        Common.MyRelationsBetweenPlayerAndBlock IMyCubeBlock.GetPlayerRelationToOwner()
        {
            return GetPlayerRelationToOwner();
        }

        Common.MyRelationsBetweenPlayerAndBlock IMyCubeBlock.GetUserRelationToOwner(long playerId)
        {
            return GetUserRelationToOwner(playerId);
        }

        void IMyCubeBlock.Init()
        {
            Init();
        }

        void IMyCubeBlock.Init(Common.ObjectBuilders.MyObjectBuilder_CubeBlock builder, IMyCubeGrid cubeGrid)
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

        float ModAPI.Ingame.IMyCubeBlock.Mass
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
            return RaycastDetectors(worldFrom, worldTo);
        }

        void IMyCubeBlock.ReloadDetectors(bool refreshNetworks)
        {
            ReloadDetectors(refreshNetworks);
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
    }
}
