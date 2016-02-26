using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.IO;
using VRage.Voxels;
using VRageMath;

namespace Sandbox.Engine.Voxels
{
    public class MyStorageDataProviderAttribute : System.Attribute
    {
        public readonly int ProviderTypeId;
        public Type ProviderType;

        public MyStorageDataProviderAttribute(int typeId)
        {
            ProviderTypeId = typeId;
        }
    }

    public struct MyVoxelDataRequest
    {
        public int Lod;
        public Vector3I minInLod;
        public Vector3I maxInLod;
        public Vector3I Offset;

        public MyStorageDataTypeFlags RequestedData;

        public MyVoxelRequestFlags RequestFlags;
        public MyVoxelRequestFlags Flags;

        public MyStorageData Target;

        public string ToStringShort()
        {
            return String.Format("lod{0}: {1}voxels", Lod, SizeLinear);
        }

        public int SizeLinear { get { return (maxInLod - minInLod + Vector3I.One).Size; } }
    }

    [Flags]
    /**
     * Flags used when requesting voxel materials and content.
     * 
     * These flags allow for optimizations such as avoiding
     * expensive material computations or quickly assigning the
     * whole storage the same material or content.
     * 
     */
    public enum MyVoxelRequestFlags
    {
        // Materials are assigned only to surface (convention now is materias to be filled are 0, rest is left null (255)
        SurfaceMaterial = 1 << 0,

        // Materials carry Surface Normal (not used anymore)
        // SurfaceNormal = 1 << 1,

        // Content carries occlusion hint (not used anymore)
        //OcclusionHint = 1 << 2,

        // Content is empty, materials are unassigned
        EmptyContent = 1 << 3,

        // Content is full
        FullContent = 1 << 4,

        // The whole assigned storage area contains a single material, the material is on the first cell
        OneMaterial = 1 << 5,

        // Do not assign material where there is no content.
        MaterialForContent = 1 << 6,

        // Storages that *do* support content hints will mark this, this is also the flag they should check when doing
        // this optimization.
        ContentChecked = 1 << 7,

        // When the content is empty or full this flag tells that the content is empty or full at all lower lod levels.
        ContentCheckedDeep = 1 << 8,

        // Do not check for containment beforehand, assume the caller expects content
        DoNotCheck = 1<<16,
    }

    public static class MyVoxelrequestFlagsExtensions
    {
        public static bool HasFlags(this MyVoxelRequestFlags self, MyVoxelRequestFlags other)
        {
            return (self & other) == other;
        }
    }

    public interface IMyStorageDataProvider
    {
        int SerializedSize { get; }

        void WriteTo(Stream stream);

        void ReadFrom(ref MyOctreeStorage.ChunkHeader header, Stream stream, ref bool isOldFormat);

        void ReadRange(MyStorageData target, MyStorageDataTypeFlags dataType, ref Vector3I writeOffset, int lodIndex, ref Vector3I minInLod, ref Vector3I maxInLod);

        /**
         * Read range of data.
         * 
         * The data for the request, providing any optimizations that are requested in there.
         */
        void ReadRange(ref MyVoxelDataRequest request);

        /**
         * What optimizations this storage supports, the caller will never request for an optimization the storage does not support.
         */
        MyVoxelRequestFlags SupportedFlags();

        void DebugDraw(ref MatrixD worldMatrix);

        void ReindexMaterials(Dictionary<byte, byte> oldToNewIndexMap);

        float GetDistanceToPoint(ref Vector3D localPos);

        /**
         * Get the material at a given position in the voxel storage.
         * 
         * Position is in storage local space.
         */
        MyVoxelMaterialDefinition GetMaterialAtPosition(ref Vector3D localPosition);

        ContainmentType Intersect(BoundingBoxI box, bool lazy);

        /**
         * Intersect line with storage.
         * 
         * Returnas the tightest line interval that does intersect the storage.
         * The precision of this method varies from storage to storage.
         * 
         * The offsets are normalised.
         */
        bool Intersect(ref LineD line, out double startOffset, out double endOffset);

        void Close();

        bool ProvidesAmbient { get; }
    }
}
