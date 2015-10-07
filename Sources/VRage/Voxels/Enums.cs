using System;

namespace VRage.Voxels
{
    //  This enum tells us if cell is 100% empty, 100% full or mixed (some voxels are full, some empty, some are something between)
    public enum MyVoxelRangeType : byte
    {
        EMPTY,
        FULL,
        MIXED
    }

    public enum MyStorageDataTypeEnum
    {
        Content,
        Material
    }

    [Flags]
    public enum MyStorageDataTypeFlags
    {
        None               = 0,
        Content            = 1 << MyStorageDataTypeEnum.Content,
        Material           = 1 << MyStorageDataTypeEnum.Material,
        ContentAndMaterial = Content | Material,
        All                = ~0,
    }

    public enum MyClipmapScaleEnum
    {
        // Used as indices to array of LoD groups.
        Normal, // asteroid-sized
    }

}