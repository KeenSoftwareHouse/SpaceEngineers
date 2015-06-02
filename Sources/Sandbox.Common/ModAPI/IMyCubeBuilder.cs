using System;
using VRage.ModAPI;
namespace Sandbox.ModAPI
{
    public interface IMyCubeBuilder
    {
        /// <summary>
        /// Activates the building mode
        /// </summary>
        void Activate();

        /// <summary>
        /// Activates creating grids
        /// </summary>
        /// <param name="grid">grid to be created</param>
        /// <param name="centerDeltaDirection"></param>
        /// <param name="dragVectorLength"></param>
        void ActivateShipCreationClipboard(Sandbox.Common.ObjectBuilders.MyObjectBuilder_CubeGrid grid, VRageMath.Vector3 centerDeltaDirection, float dragVectorLength);

        /// <summary>
        /// Activates creating grids
        /// </summary>
        /// <param name="grid">grids to be created</param>
        /// <param name="centerDeltaDirection"></param>
        /// <param name="dragVectorLength"></param>
        void ActivateShipCreationClipboard(Sandbox.Common.ObjectBuilders.MyObjectBuilder_CubeGrid[] grids, VRageMath.Vector3 centerDeltaDirection, float dragVectorLength);
        
        /// <summary>
        /// Adds construction site of block with currently selected definition
        /// </summary>
        /// <param name="buildingEntity"></param>
        bool AddConstruction(IMyEntity buildingEntity);

        /// <summary>
        /// Returns state of building mode
        /// </summary>
        bool BlockCreationIsActivated { get; }

        /// <summary>
        /// Returns state of copy pasting mode
        /// </summary>
        bool CopyPasteIsActivated { get; }

        /// <summary>
        /// Deactivates all modes
        /// </summary>
        void Deactivate();

        /// <summary>
        /// Deactivates building mode
        /// </summary>
        void DeactivateBlockCreation();

        /// <summary>
        /// Deactivates copy pasting mode
        /// </summary>
        void DeactivateCopyPaste();

        /// <summary>
        /// Deactivates creating grids
        /// </summary>
        void DeactivateShipCreationClipboard();

        /// <summary>
        /// Freezes the built object preview in current position
        /// </summary>
        bool FreezeGizmo { get; set; }

        /// <summary>
        /// Current stat of grid creation mode
        /// </summary>
        bool ShipCreationIsActivated { get; }

        /// <summary>
        /// Shows the delete area preview
        /// </summary>
        bool ShowRemoveGizmo { get; set; }

        /// <summary>
        /// Creates new grid 
        /// </summary>
        /// <param name="cubeSize">Grid size</param>
        /// <param name="isStatic">Station = static</param>
        void StartNewGridPlacement(Sandbox.Common.ObjectBuilders.MyCubeSize cubeSize, bool isStatic);

        /// <summary>
        /// Enables synmetry block placing
        /// </summary>
        bool UseSymmetry { get; set; }

        /// <summary>
        /// 
        /// </summary>
        bool UseTransparency { get; set; }

        /// <summary>
        /// Finds grid to build on
        /// </summary>
        /// <returns>found grid</returns>
        IMyCubeGrid FindClosestGrid();

        /// <summary>
        /// Is any mode active
        /// </summary>
        bool IsActivated { get; }


        //Missing dependencies
        //Sandbox.Definitions.MyCubeBlockDefinition CurrentBlockDefinition { get; set; }
        //Sandbox.Definitions.MyCubeBlockDefinition HudBlockDefinition { get; }
        //void ActivateBlockCreation(Sandbox.Definitions.MyDefinitionId? blockDefinitionId = null);

        //Not for use for scripters?
        //void UpdateBeforeSimulation();
        //void LoadData();
        //void InputLost();
        //void Draw();
        //void UpdateNotificationBlockNotAvailable(bool changeText = true);
        //bool HandleGameInput();

    }
}
