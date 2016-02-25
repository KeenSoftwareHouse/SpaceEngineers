using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Havok;
using Sandbox.Common.ObjectBuilders;
using VRage.Game;
using VRageMath;

namespace Sandbox.Game.Entities.Cube
{
    /// <summary>
    /// Interface for implementation of additional geometry in cube grids (automatic placement of additional geometry).
    /// </summary>
    public interface IMyBlockAdditionalModelGenerator
    {
        /// <summary>
        /// initializes the generator with grid and its size. Note that the grid is not fully initializd yet. 
        /// </summary>
        bool Initialize(MyCubeGrid grid, MyCubeSize gridSizeEnum);

        /// <summary>
        /// Closes the generator, use for unregistering from other objects.
        /// </summary>
        void Close();

        /// <summary>
        /// Enables/disable generator.
        /// </summary>
        /// <param name="enable"></param>
        void EnableGenerator(bool enable);

        /// <summary>
        /// Block was added but causes merging grids (so it was not catched in events OnBlockAdded/OnBLockRemoved because generator is disabled during merging).
        /// </summary>
        /// <param name="block"></param>
        void BlockAddedToMergedGrid(MySlimBlock block);

        /// <summary>
        /// Generate blocks for the given generating block (build progress exceeeds some value where generated objects should appear).
        /// </summary>
        /// <param name="block"></param>
        void GenerateBlocks(MySlimBlock generatingBlock);

        /// <summary>
        /// Update generator after simulation. Add/remove generated blocks. Called from grid.
        /// </summary>
        void UpdateAfterSimulation();

        /// <summary>
        /// Update generator before simulation. Add/remove generated blocks. Called from grid.
        /// </summary>
        void UpdateBeforeSimulation();

        /// <summary>
        /// Updates generated objects with the block after grid has been spawn (grid is not in scene when creating blocks in spawned grid so this method must be called after it is initialized).
        /// </summary>
        void UpdateAfterGridSpawn(MySlimBlock block);

        /// <summary>
        /// Returns generating block which generated the given one.
        /// </summary>
        /// <param name="generatedBlock">Generated block</param>
        /// <returns>Generating block or null.</returns>
        MySlimBlock GetGeneratingBlock(MySlimBlock generatedBlock);
    }
}
