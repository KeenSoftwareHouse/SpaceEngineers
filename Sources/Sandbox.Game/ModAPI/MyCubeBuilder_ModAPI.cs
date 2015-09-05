using Sandbox.Game.Entities.Character;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ModAPI;

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

        void IMyCubeBuilder.Activate()
        {
            Activate();
        }

        void IMyCubeBuilder.ActivateShipCreationClipboard(Common.ObjectBuilders.MyObjectBuilder_CubeGrid grid, VRageMath.Vector3 centerDeltaDirection, float dragVectorLength)
        {
            ActivateShipCreationClipboard(grid, centerDeltaDirection, dragVectorLength);
        }

        void IMyCubeBuilder.ActivateShipCreationClipboard(Common.ObjectBuilders.MyObjectBuilder_CubeGrid[] grids, VRageMath.Vector3 centerDeltaDirection, float dragVectorLength)
        {
            ActivateShipCreationClipboard(grids, centerDeltaDirection, dragVectorLength);
        }

        bool IMyCubeBuilder.BlockCreationIsActivated
        {
            get { return BlockCreationIsActivated; }
        }

        bool IMyCubeBuilder.CopyPasteIsActivated
        {
            get { return CopyPasteIsActivated; }
        }

        void IMyCubeBuilder.Deactivate()
        {
            Deactivate();
        }

        void IMyCubeBuilder.DeactivateBlockCreation()
        {
            DeactivateBlockCreation();
        }

        void IMyCubeBuilder.DeactivateCopyPaste()
        {
            DeactivateCopyPaste();
        }

        void IMyCubeBuilder.DeactivateShipCreationClipboard()
        {
            DeactivateShipCreationClipboard();
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

        bool IMyCubeBuilder.ShipCreationIsActivated
        {
            get { return ShipCreationIsActivated; }
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

        void IMyCubeBuilder.StartNewGridPlacement(Common.ObjectBuilders.MyCubeSize cubeSize, bool isStatic)
        {
            StartNewGridPlacement(cubeSize, isStatic);
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
