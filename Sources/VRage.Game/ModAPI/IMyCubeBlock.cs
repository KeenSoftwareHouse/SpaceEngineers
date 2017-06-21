using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace VRage.Game.ModAPI
{
    /// <summary>
    /// base block interface, block can be affected by upgrade modules, and you can retrieve upgrade list from <see cref="IMyUpgradableBlock"/>
    /// </summary>
    public interface IMyCubeBlock : Ingame.IMyCubeBlock, IMyEntity
    {
        event Action<IMyCubeBlock> IsWorkingChanged;
        SerializableDefinitionId BlockDefinition { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="localMatrix"></param>
        /// <param name="currModel"></param>
        void CalcLocalMatrix(out VRageMath.Matrix localMatrix, out string currModel);

        /// <summary>
        /// Calculates model currently used by block depending on its build progress and other factors
        /// </summary>
        /// <param name="orientation">Model orientation</param>
        /// <returns>Model path</returns>
        string CalculateCurrentModel(out VRageMath.Matrix orientation);

        /// <summary>
        /// Whether the grid should call the ConnectionAllowed method for this block 
        ///(ConnectionAllowed checks mount points and other per-block requirements)
        /// </summary>
        bool CheckConnectionAllowed { get; set; }

        //bool ConnectionAllowed(ref VRageMath.Vector3I otherBlockMinPos, ref VRageMath.Vector3I otherBlockMaxPos, ref VRageMath.Vector3I faceNormal, Sandbox.Definitions.MyCubeBlockDefinition def);
        //bool ConnectionAllowed(ref VRageMath.Vector3I otherBlockPos, ref VRageMath.Vector3I faceNormal, Sandbox.Definitions.MyCubeBlockDefinition def);
        
        /// <summary>
        /// Grid in which the block is placed
        /// </summary>
        IMyCubeGrid CubeGrid { get; }

        /// <summary>
        /// Debug only method. Effects may wary through time.
        /// </summary>
        /// <returns></returns>
        bool DebugDraw();

        /// <summary>
        /// Definition name
        /// </summary>
        String DefinitionDisplayNameText { get; }

        /// <summary>
        /// Is set in definition
        /// Ratio at which is the block disassembled (grinding) 
        /// </summary>
        float DisassembleRatio { get; }

        /// <summary>
        /// Translated block name
        /// </summary>
        String DisplayNameText { get; }

        /// <summary>
        /// Returns block object builder which can be serialized or added to grid
        /// </summary>
        /// <param name="copy">Set if creating a copy of block</param>
        /// <returns>Block object builder</returns>
        MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false);

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Tag of faction owning block</returns>
        string GetOwnerFactionTag();

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Relation of local player to the block</returns>
        VRage.Game.MyRelationsBetweenPlayerAndBlock GetPlayerRelationToOwner();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="playerId">Id of player to check relation with (not steam id!)</param>
        /// <returns>Relation of defined player to the block</returns>
        VRage.Game.MyRelationsBetweenPlayerAndBlock GetUserRelationToOwner(long playerId);

        //Sandbox.Game.Entities.MyIDModule IDModule { get; }

        /// <summary>
        /// Reloads block model and interactive objects (doors, terminals, etc...)
        /// </summary>
        void Init();

        /// <summary>
        /// Initializes block state from object builder
        /// </summary>
        /// <param name="builder">Object builder of block (should correspond with block type)</param>
        /// <param name="cubeGrid">Owning grid</param>
        void Init(MyObjectBuilder_CubeBlock builder, IMyCubeGrid cubeGrid);


        bool IsBeingHacked { get; }

        /// <summary>
        /// True if integrity is above breaking threshold
        /// </summary>
        bool IsFunctional { get; }

        /// <summary>
        /// True if block is able to do its work depening on block type (is functional, powered, enabled, etc...)
        /// </summary>
        bool IsWorking { get; }

        //event Action<IMyCubeBlock> IsWorkingChanged; //TODO: use Event set for this
        /// <summary>
        /// Maximum coordinates of grid cells occupied by this block
        /// </summary>
        VRageMath.Vector3I Max { get; }
        
        /// <summary>
        /// Block mass
        /// </summary>
        float Mass { get; }
        /// <summary>
        /// Minimum coordinates of grid cells occupied by this block
        /// </summary>
        VRageMath.Vector3I Min { get; }

        /// <summary>
        /// Order in which were the blocks of same type added to grid
        /// Used in default display name
        /// </summary>
        int NumberInGrid { get; set; }

        /// <summary>
        /// Method called when a block has been built (after adding to the grid).
        /// This is called right after placing the block and it doesn't matter whether
        /// it is fully built (creative mode) or is only construction site.
        /// Note that it is not called for blocks which do not create FatBlock at that moment.
        /// </summary>
        void OnBuildSuccess(long builtBy);

        /// <summary>
        /// Called when block is destroyed before being removed from grid
        /// </summary>
        void OnDestroy();

        /// <summary>
        /// Called when the model referred by the block is changed
        /// </summary>
        void OnModelChange();

        /// <summary>
        /// Called at the end of registration from grid systems (after block has been registered).
        /// </summary>
        void OnRegisteredToGridSystems();

        /// <summary>
        /// Method called when user removes a cube block from grid. Useful when block
        /// has to remove some other attached block (like motors).
        /// </summary>
        void OnRemovedByCubeBuilder();

        /// <summary>
        /// Called at the end of unregistration from grid systems (after block has been unregistered).
        /// </summary>
        void OnUnregisteredFromGridSystems();

        /// <summary>
        /// Returns block orientation in base 6 directions
        /// </summary>
        VRageMath.MyBlockOrientation Orientation { get; }

        /// <summary>
        /// Id of player owning block (not steam Id)
        /// </summary>
        long OwnerId { get; }

        /// <summary>
        /// Position in grid coordinates
        /// </summary>
        VRageMath.Vector3I Position { get; }

        /// <summary>
        /// Gets the name of interactive object intersected by defined line
        /// </summary>
        /// <param name="worldFrom">Line from point in world coordinates</param>
        /// <param name="worldTo">Line to point in world coordinates</param>
        /// <returns>Name of intersected detector (interactive object)</returns>
        string RaycastDetectors(VRageMath.Vector3 worldFrom, VRageMath.Vector3 worldTo);
        //void ReleaseInventory(Sandbox.Game.MyInventory inventory, bool damageContent = false);

        /// <summary>
        /// Reloads detectors (interactive objects) in model
        /// </summary>
        /// <param name="refreshNetworks">ie conweyor network</param>
        void ReloadDetectors(bool refreshNetworks = true);

        /// <summary>
        /// Force refresh working state. Call if you change block state that could affect its working status.
        /// </summary>
        void UpdateIsWorking();

        /// <summary>
        /// Updates block visuals (ie. block emissivity)
        /// </summary>
        void UpdateVisual();

        /// <summary>
        /// Start or stop dammage effect on cube block
        /// </summary>
        void SetDamageEffect(bool start);

        /// <summary>
        /// Get all values changed by upgrade modules
        /// Should only be used as read-only
        /// </summary>
        Dictionary<string, float> UpgradeValues
        {
            get;
        }

        /// <summary>
        /// Preferred way of registering a block for upgrades
        /// Adding directly to the dictionary can have unintended consequences
        /// when multiple mods are involved.
        /// </summary>
        void AddUpgradeValue(string upgrade, float defaultValue);

        /// <summary>
        /// Gets the SlimBlock associated with this block
        /// </summary>
        IMySlimBlock SlimBlock { get; }

        /// <summary>
        /// Event called when upgrade values are changed
        /// Either upgrades were built or destroyed, or they become damaged or unpowered
        /// </summary>
        event Action OnUpgradeValuesChanged;
    }
}
