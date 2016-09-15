using System.Diagnostics;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Definitions.SessionComponents
{

    public struct MyPlacementSettings
    {
        public MyGridPlacementSettings SmallGrid;
        public MyGridPlacementSettings SmallStaticGrid;
        public MyGridPlacementSettings LargeGrid;
        public MyGridPlacementSettings LargeStaticGrid;

        /// <summary>
        /// Align static grids to corners (false) or centers (true).
        /// You should always set to corners in new games. Center alignment is only for backwards compatibility so that
        /// static grids are correctly aligned with already existing saves.
        /// </summary>
        public bool StaticGridAlignToCenter;
        public MyGridPlacementSettings GetGridPlacementSettings(MyCubeSize cubeSize, bool isStatic)
        {
            switch (cubeSize)
            {
                case MyCubeSize.Large: return (isStatic) ? LargeStaticGrid : LargeGrid;
                case MyCubeSize.Small: return (isStatic) ? SmallStaticGrid : SmallGrid;

                default:
                    Debug.Fail("Invalid branch.");
                    return LargeGrid;
            }
        }

        public MyGridPlacementSettings GetGridPlacementSettings(MyCubeSize cubeSize)
        {
            switch (cubeSize)
            {
                case MyCubeSize.Large: return LargeGrid;
                case MyCubeSize.Small: return SmallGrid;

                default:
                    Debug.Fail("Invalid branch.");
                    return LargeGrid;
            }
        }

    }

    public struct MyGridPlacementSettings
    {
        public SnapMode SnapMode;
        public float SearchHalfExtentsDeltaRatio;
        public float SearchHalfExtentsDeltaAbsolute;
        public VoxelPlacementSettings? VoxelPlacement;

        /// <summary>
        /// When min. allowed penetration is not met, block may still be placed if it is touching static grid and this property is true.
        /// </summary>
        public bool CanAnchorToStaticGrid;
        public bool EnablePreciseRotationWhenSnapped;
    }

    public enum SnapMode
    {
        Base6Directions,
        OneFreeAxis,
    }

    public enum VoxelPlacementMode
    {
        None,
        InVoxel,
        OutsideVoxel,
        Both,
        Volumetric
    }

    /// <summary>
    /// Voxel penetration definition
    /// </summary>
    public struct VoxelPlacementSettings
    {

        public VoxelPlacementMode PlacementMode;
        /// <summary>
        /// Maximum amount in % of block being inside voxel (where 1 - 100% to 0 - 0%)
        /// </summary>
        public float MaxAllowed;
        /// <summary>
        /// Minimum amount in % of block being inside voxel (where 1 - 100% to 0 - 0%)
        /// </summary>
        public float MinAllowed;
    }

    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CubeBuilderDefinition : MyObjectBuilder_SessionComponentDefinition
    {
        /// <summary>
        /// Default block building distance for creative mode.
        /// </summary>
        public float DefaultBlockBuildingDistance = 20f;
        
        /// <summary>
        /// Max building distance for creative mode.
        /// </summary>
        public float MaxBlockBuildingDistance = 20f;
        
        /// <summary>
        /// Min building distnace for creative mode.
        /// </summary>
        public float MinBlockBuildingDistance = 1f;

        /// <summary>
        /// Building distance for small grid in survival mode when controlling character.
        /// </summary>
        public double BuildingDistSmallSurvivalCharacter = 5;

        /// <summary>
        /// Building distance for large grid in survival mode when controlling character.
        /// </summary>
        public double BuildingDistLargeSurvivalCharacter = 10;

        /// <summary>
        /// Building distance for small grid in survival mode when controlling ship.
        /// </summary>
        public double BuildingDistSmallSurvivalShip = 12.5;

        /// <summary>
        /// Building distance for large grid in survival mode when controlling ship.
        /// </summary>
        public double BuildingDistLargeSurvivalShip = 12.5;

        /// <summary>
        /// Defines placement settings for building mode.
        /// </summary>
        public MyPlacementSettings BuildingSettings;

    }
}
