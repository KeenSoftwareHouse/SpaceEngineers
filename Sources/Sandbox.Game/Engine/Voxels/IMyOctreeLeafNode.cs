using System.Collections.Generic;
using VRage.Voxels;
using VRageMath;

namespace Sandbox.Engine.Voxels
{
    public interface IMyOctreeLeafNode
    {
        MyOctreeStorage.ChunkTypeEnum SerializedChunkType { get; }

        int SerializedChunkSize { get; }

        Vector3I VoxelRangeMin { get; }

        bool ReadOnly { get; }

        byte GetFilteredValue();

        /// <param name="minInLod">Inclusive.</param>
        /// <param name="maxInLod">Inclusive.</param>
        void ReadRange(MyStorageDataCache target, ref Vector3I writeOffset, int lodIndex, ref Vector3I minInLod, ref Vector3I maxInLod);

        /// <param name="minInLod">Inclusive.</param>
        /// <param name="maxInLod">Inclusive.</param>
        void WriteRange(MyStorageDataCache source, ref Vector3I readOffset, ref Vector3I min, ref Vector3I max);

        void OnDataProviderChanged(IMyStorageDataProvider newProvider);

        void ReplaceValues(Dictionary<byte, byte> oldToNewValueMap);
    }
}