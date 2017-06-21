using Sandbox.Game.Entities.Character;
using VRage.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.ModAPI;
using Sandbox.ModAPI;

namespace Sandbox.Game.Entities
{
    partial class MyCubeBuilder : IMyCubeBuilder
    {
        bool IMyCubeBuilder.AddConstruction(IMyEntity buildingEntity)
        {
            /*if (buildingEntity is MyCharacter)
                return AddConstruction(buildingEntity as MyCharacter);*/
            return false;
        }

        IMyCubeGrid IMyCubeBuilder.FindClosestGrid()
        {
            return FindClosestGrid();
        }

        void IMyCubeBuilder.Activate(MyDefinitionId? blockDefinitionId = null)
        {
            Activate(blockDefinitionId);
        }

        bool IMyCubeBuilder.BlockCreationIsActivated
        {
            get { return BlockCreationIsActivated; }
        }
        void IMyCubeBuilder.Deactivate()
        {
            Deactivate();
        }

        void IMyCubeBuilder.DeactivateBlockCreation()
        {
            DeactivateBlockCreation();
        }

        bool IMyCubeBuilder.FreezeGizmo
        {
            get
            {
                return FreezeGizmo;
            }
            set
            {
                FreezeGizmo = true;
            }
        }

        bool IMyCubeBuilder.ShowRemoveGizmo
        {
            get
            {
                return ShowRemoveGizmo;
            }
            set
            {
                ShowRemoveGizmo = value;
            }
        }

        void IMyCubeBuilder.StartNewGridPlacement(MyCubeSize cubeSize, bool isStatic)
        {
            StartStaticGridPlacement(cubeSize, isStatic);
        }

        bool IMyCubeBuilder.UseSymmetry
        {
            get
            {
                return UseSymmetry;
            }
            set
            {
                UseSymmetry = value;
            }
        }

        bool IMyCubeBuilder.UseTransparency
        {
            get
            {
                return UseTransparency;
            }
            set
            {
                UseTransparency = value;
            }
        }

        bool IMyCubeBuilder.IsActivated
        {
            get { return IsActivated; }
        }
    }
}
