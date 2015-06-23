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

    public interface IMyStorageDataProvider
    {
        int SerializedSize { get; }

        void WriteTo(Stream stream);

        void ReadFrom(ref MyOctreeStorage.ChunkHeader header, Stream stream, ref bool isOldFormat);

        void ReadRange(MyStorageDataCache target, MyStorageDataTypeEnum dataType, ref Vector3I writeOffset, int lodIndex, ref Vector3I minInLod, ref Vector3I maxInLod);

        void DebugDraw(ref MatrixD worldMatrix);

        void ReindexMaterials(Dictionary<byte, byte> oldToNewIndexMap);

        float GetDistanceToPoint(ref Vector3D localPos);

        bool IsMaterialAtPositionSpawningFlora(ref Vector3D localPos);

        bool HasMaterialSpawningFlora();
    }
}
