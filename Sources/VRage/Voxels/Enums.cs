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
        Material,
        Occlusion,

        NUM_STORAGE_DATA_TYPES
    }

    [Flags]
    public enum MyStorageDataTypeFlags
    {
        None               = 0,
        Content            = 1 << MyStorageDataTypeEnum.Content,
        Material           = 1 << MyStorageDataTypeEnum.Material,
        ContentAndMaterial = Content | Material,
        Occlusion          = 1 << MyStorageDataTypeEnum.Occlusion,
        All                = ~0,
    }

    public static class MyVoxelEnumExtensions
    {
        // Weather this flags request a given type of data
        public static bool Requests(this MyStorageDataTypeFlags self, MyStorageDataTypeEnum value)
        {
            return ((int)self & (1 << (int)value)) != 0;
        }

        // get the same flags except the provided type.
        public static MyStorageDataTypeFlags Without(this MyStorageDataTypeFlags self, MyStorageDataTypeEnum value)
        {
            return self & ~(MyStorageDataTypeFlags) (1 << (int) value);
        }

        // Convert enum to flags
        public static MyStorageDataTypeFlags ToFlags(this MyStorageDataTypeEnum self)
        {
            return (MyStorageDataTypeFlags) (1 << (int) self);
        }
    }

    public enum MyClipmapScaleEnum
    {
        // Used as indices to array of LoD groups.
        Normal, // asteroid-sized
        Massive, // planet-sized
    }

}