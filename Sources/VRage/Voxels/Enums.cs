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
    public enum MyStorageDataTypeFlags : byte
    {
        None               = 0,
        Content            = 1 << MyStorageDataTypeEnum.Content,
        Material           = 1 << MyStorageDataTypeEnum.Material,
        ContentAndMaterial = Content | Material,
        Occlusion          = 1 << MyStorageDataTypeEnum.Occlusion,
        All                = 0xFF,
    }


    /// <summary>
    /// Flags used when requesting voxel materials and content.
    /// </summary>
    /// <remarks>
    /// These flags allow for optimizations such as avoiding
    /// expensive material computations or quickly assigning the
    /// whole storage the same material or content.
    /// </remarks>
    [Flags]
    public enum MyVoxelRequestFlags
    {
        /// <summary>
        /// Materials are assigned only to surface (convention now is materials to be filled are 0, rest is left null (255)
        /// </summary>
        SurfaceMaterial = 1 << 0,

        /// <summary>
        /// Assign material according to content, i.e. null where empty
        /// </summary>
        ConsiderContent = 1 << 1,

        // Content carries occlusion hint (not used anymore)
        //OcclusionHint = 1 << 2,

        /// <summary>
        /// Content is empty, materials are unassigned
        /// </summary>
        EmptyContent = 1 << 3,

        /// <summary>
        /// Content is full
        /// </summary>
        FullContent = 1 << 4,

        /// <summary>
        /// The whole assigned storage area contains a single material, the material is on the first cell
        /// </summary>
        OneMaterial = 1 << 5,

        /// <summary>
        /// cache storage chunks, this only works for lod0
        /// </summary>
        AdviseCache = 1 << 6,

        /// <summary>
        /// Storages that *do* support content hints will mark this, this is also the flag they should check when doing this optimization.
        /// </summary>
        ContentChecked = 1 << 7,

        /// <summary>
        /// When the content is empty or full this flag tells that the content is empty or full at all lower lod levels.
        /// </summary>
        ContentCheckedDeep = 1 << 8,

        /// <summary>
        /// Do not check for containment beforehand, assume the caller expects content
        /// </summary>
        DoNotCheck = 1 << 16,

        /// <summary>
        /// Minimum flags to use when making requests
        /// </summary>
        RequestFlags = ConsiderContent | SurfaceMaterial
    }

    /// <summary>
    /// Type of voxel operation to perform
    /// </summary>
    public enum OperationType : byte
    {
        /// <summary>
        /// Fills in a range with voxels
        /// </summary>
        Fill,
        /// <summary>
        /// Paints a range of voxels a different material
        /// </summary>
        Paint,
        /// <summary>
        /// Cuts out a range of voxels
        /// </summary>
        Cut
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