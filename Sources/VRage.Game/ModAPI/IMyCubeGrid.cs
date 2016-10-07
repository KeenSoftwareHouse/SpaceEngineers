using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.ModAPI;


namespace VRage.Game.ModAPI
{
    public interface IMyCubeGrid : IMyEntity, Ingame.IMyCubeGrid
    {
        /// <summary>
        /// Applies random deformation to given block
        /// </summary>
        /// <param name="block">block to be deformed</param>
        void ApplyDestructionDeformation(IMySlimBlock block);

        /// <summary>
        /// List of players with majority of blocks on grid
        /// </summary>
        List<long> BigOwners { get; }
        /// <summary>
        /// List of players with any blocks on grid
        /// </summary>
        List<long> SmallOwners { get; }

        /// <summary>
        /// Changes owner of all blocks on grid
        /// Call only on server!
        /// </summary>
        /// <param name="playerId">new owner id</param>
        /// <param name="shareMode">new share mode</param>
        void ChangeGridOwnership(long playerId, MyOwnershipShareModeEnum shareMode);

        //void ChangeOwner(IMyCubeBlock block, long oldOwner, long newOwner); //This just updates owner counters, called by fatblock.changeowner

        /// <summary>
        /// Clears symmetry planes
        /// </summary>
        void ClearSymmetries();

        /// <summary>
        /// Sets given color mask to range of blocks
        /// </summary>
        /// <param name="min">Starting coordinates of collored area</param>
        /// <param name="max">End coordinates of collored area</param>
        /// <param name="newHSV">new color mask (Saturation and Value are offsets)</param>
        void ColorBlocks(VRageMath.Vector3I min, VRageMath.Vector3I max, VRageMath.Vector3 newHSV);

        /// <summary>
        /// Converts station to ship
        /// </summary>
        void ConvertToDynamic();

        /// <summary>
        /// Returns true if there is any block occupying given position
        /// </summary>
        bool CubeExists(VRageMath.Vector3I pos);

        /// <summary>
        /// Clamps fractional grid position to nearest cell (prefers neighboring occupied cell before empty) 
        /// </summary>
        /// <param name="cube">Return value</param>
        /// <param name="fractionalGridPosition">Fractional position in grid space</param>
        void FixTargetCube(out VRageMath.Vector3I cube, VRageMath.Vector3 fractionalGridPosition);

        /// <summary>
        /// Gets position of closest cell corner
        /// </summary>
        /// <param name="gridPos">Cell coordinates</param>
        /// <param name="position">Position to find nearest corner to. Grid space</param>
        /// <returns>Fractional position of corner in grid space</returns>
        VRageMath.Vector3 GetClosestCorner(VRageMath.Vector3I gridPos, VRageMath.Vector3 position);

        /// <summary>
        /// Get cube block at given position
        /// </summary>
        /// <param name="pos">Block position</param>
        /// <returns>Block or null if none is present at given position</returns>
        IMySlimBlock GetCubeBlock(VRageMath.Vector3I pos);

        /// <summary>
        /// Returns point of intersection with line
        /// </summary>
        /// <param name="line">Intersecting line</param>
        /// <param name="distance">Distance of intersection</param>
        /// <param name="intersectedBlock"></param>
        /// <returns>Point of intersection</returns>
        VRageMath.Vector3D? GetLineIntersectionExactAll(ref VRageMath.LineD line, out double distance, out IMySlimBlock intersectedBlock);

        /// <summary>
        /// Same as GetLineIntersectionExactAll just without intersected block
        /// </summary>
        bool GetLineIntersectionExactGrid(ref VRageMath.LineD line, ref VRageMath.Vector3I position, ref double distanceSquared);

        /// <summary>
        /// Converts grid coordinates to world space
        /// </summary>
        VRageMath.Vector3D GridIntegerToWorld(VRageMath.Vector3I gridCoords);

        /// <summary>
        /// Grid size in meters
        /// </summary>
        float GridSize { get; }

        /// <summary>
        /// Grid size enumeration
        /// </summary>
        MyCubeSize GridSizeEnum { get; set; }

        /// <summary>
        /// Station = static
        /// </summary>
        bool IsStatic { get; }

        /// <summary>
        /// Finds out if given area has any neighboring block
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        bool IsTouchingAnyNeighbor(VRageMath.Vector3I min, VRageMath.Vector3I max);

        /// <summary>
        /// Algorithm used by game to define useless grids to be deleted
        /// </summary>
        bool IsTrash();

        /// <summary>
        /// Maximum coordinates of blocks in grid
        /// </summary>
        VRageMath.Vector3I Max { get; }

        /// <summary>
        /// Determines if merge between grids is possible with given offset
        /// </summary>
        /// <param name="gridToMerge"></param>
        /// <param name="gridOffset">offset to merged grid (in grid space)</param>
        /// <returns></returns>
        bool CanMergeCubes(IMyCubeGrid gridToMerge, VRageMath.Vector3I gridOffset);

        /// <summary>
        /// Transformation matrix that has to be applied to grid blocks to correctly merge it
        /// used because ie. ships can be turned 90 degrees along X axis when being merged
        /// </summary>
        /// <param name="gridToMerge"></param>
        /// <param name="gridOffset"></param>
        /// <returns></returns>
        VRageMath.MatrixI CalculateMergeTransform(IMyCubeGrid gridToMerge, VRageMath.Vector3I gridOffset);


        /// <summary>
        /// Merge used by merge blocks
        /// </summary>
        /// <param name="gridToMerge"></param>
        /// <param name="gridOffset"></param>
        /// <returns></returns>
        IMyCubeGrid MergeGrid_MergeBlock(IMyCubeGrid gridToMerge, VRageMath.Vector3I gridOffset);

        /// <summary>
        /// Minimum coordinates of blocks in grid
        /// </summary>
        VRageMath.Vector3I Min { get; }

        /// <summary>
        /// Returns cell with block intersecting given line
        /// </summary>
        /// <param name="worldStart"></param>
        /// <param name="worldEnd"></param>
        /// <returns></returns>
        VRageMath.Vector3I? RayCastBlocks(VRageMath.Vector3D worldStart, VRageMath.Vector3D worldEnd);

        /// <summary>
        /// Returns list of cells with blocks intersected by line
        /// </summary>
        /// <param name="worldStart"></param>
        /// <param name="worldEnd"></param>
        /// <param name="outHitPositions"></param>
        /// <param name="gridSizeInflate"></param>
        /// <param name="havokWorld">use physics intersection</param>
        void RayCastCells(VRageMath.Vector3D worldStart, VRageMath.Vector3D worldEnd, List<VRageMath.Vector3I> outHitPositions, VRageMath.Vector3I? gridSizeInflate = null, bool havokWorld = false);
        
        /// <summary>
        /// Remove block at given position
        /// </summary>
        void RazeBlock(VRageMath.Vector3I position);

        /// <summary>
        /// Remove blocks in given area
        /// </summary>
        /// <param name="pos">Starting position</param>
        /// <param name="size">Area extents</param>
        void RazeBlocks(ref VRageMath.Vector3I pos, ref VRageMath.Vector3UByte size);

        /// <summary>
        /// Remove blocks at given positions
        /// </summary>
        void RazeBlocks(List<VRageMath.Vector3I> locations);

        /// <summary>
        /// Removes given block
        /// </summary>
        /// <param name="block"></param>
        /// <param name="updatePhysics">Update grid physics</param>
        void RemoveBlock(IMySlimBlock block, bool updatePhysics = false);

        /// <summary>
        /// Removes block and deformates neighboring blocks
        /// </summary>
        /// <param name="block"></param>
        void RemoveDestroyedBlock(IMySlimBlock block);

        /// <summary>
        /// Refreshes block neighbors (checks connections)
        /// </summary>
        /// <param name="block"></param>
        void UpdateBlockNeighbours(IMySlimBlock block);


        //void UpdateOwnership(long ownerId, bool isFunctional); //updates ownership counters

        /// <summary>
        /// Converts world coordinates to grid space cell coordinates
        /// </summary>
        /// <param name="coords"></param>
        /// <returns></returns>
        VRageMath.Vector3I WorldToGridInteger(VRageMath.Vector3 coords);

        //Allocations
        /// <summary>
        /// Returns blocks in grid
        /// </summary>
        /// <param name="blocks">List of returned blocks</param>
        /// <param name="collect">Filter - function called on each block telling if it should be added to result</param>
        void GetBlocks(List<IMySlimBlock> blocks, Func<IMySlimBlock, bool> collect = null);
        /// <summary>
        /// Returns blocks inside given sphere (world space)
        /// </summary>
        List<IMySlimBlock> GetBlocksInsideSphere(ref VRageMath.BoundingSphereD sphere);


        event Action<IMySlimBlock> OnBlockAdded;
        event Action<IMySlimBlock> OnBlockRemoved;
        event Action<IMyCubeGrid> OnBlockOwnershipChanged;
        event Action<IMyCubeGrid> OnGridChanged;

        void UpdateOwnership(long ownerId, bool isFunctional);
        VRageMath.Vector3I WorldToGridInteger(VRageMath.Vector3D coords);

        /// <summary>
        /// Add a cubeblock to the grid
        /// </summary>
        /// <param name="objectBuilder">Object builder of cube to add</param>
        /// <param name="testMerge">test for grid merging</param>
        /// <returns></returns>
        IMySlimBlock AddBlock(MyObjectBuilder_CubeBlock objectBuilder, bool testMerge);

        /// <summary>
        /// Checks if removing a block will cause the grid to split
        /// </summary>
        /// <param name="testBlock"></param>
        /// <returns></returns>
        bool WillRemoveBlockSplitGrid(IMySlimBlock testBlock);

        //Missing dependencies
        //void BuildBlocks(long buildBy, ref IMyCubeGrid.MyBlockBuildArea area);
        //void BuildBlocks(VRageMath.Vector3 colorMaskHsv, System.Collections.Generic.HashSet<IMyCubeGrid.MyBlockLocation> locations);
        //bool CanPlaceBlock(VRageMath.Vector3I min, VRageMath.Vector3I max, VRageMath.MyBlockOrientation orientation, Sandbox.Definitions.MyCubeBlockDefinition definition);

        //Lot of overhead
        //void GetBlocksInsideSpheres(ref VRageMath.BoundingSphere sphere1, ref VRageMath.BoundingSphere sphere2, ref VRageMath.BoundingSphere sphere3, System.Collections.Generic.HashSet<IMySlimBlock> blocks1, System.Collections.Generic.HashSet<IMySlimBlock> blocks2, System.Collections.Generic.HashSet<MySlimBlock> blocks3, bool respectDeformationRatio, float detectionBlockHalfSize, ref VRageMath.Matrix invWorldGrid);

        //TODO
        //void RequestFillStockpile(VRageMath.Vector3I blockPosition, Sandbox.Game.MyInventory fromInventory);
        //void RequestSetToConstruction(VRageMath.Vector3I blockPosition, Sandbox.Game.MyInventory fromInventory);

        //Not for use for scripters?
        //void UpdateDirty();
        //void SetCubeDirty(VRageMath.Vector3I pos);
        //void SetBlockDirty(IMySlimBlock cubeBlock);
        //void RebuildGrid();
        //void RecalculateOwners();
        //void MultiplyBlockSkeleton(IMySlimBlock block, float factor, bool updateSync = false);
        //void ResetBlockSkeleton(IMySlimBlock block, bool updateSync = false);
        //void GetExistingBones(VRageMath.Vector3I boneMin, VRageMath.Vector3I boneMax, System.Collections.Generic.Dictionary<VRageMath.Vector3I, IMySlimBlock> resultSet);
        //int FilterSubsystemID { get; }
        //bool CanHavePhysics();
        //void AddDirtyBone(VRageMath.Vector3I gridPosition, VRageMath.Vector3I boneOffset);
        //bool CanMergeCubes(IMyCubeGrid gridToMerge, VRageMath.Vector3I gridOffset);
        //void DebugDrawPositions(System.Collections.Generic.List<VRageMath.Vector3I> positions);
        //void DebugDrawRange(VRageMath.Vector3I min, VRageMath.Vector3I max);
    }

    /// <summary>
    /// Types of block integrity change that can occur
    /// </summary>
    public enum MyIntegrityChangeEnum
    {
        Damage,
        ConstructionBegin,
        ConstructionEnd,
        ConstructionProcess,
        Repair
    }
}
