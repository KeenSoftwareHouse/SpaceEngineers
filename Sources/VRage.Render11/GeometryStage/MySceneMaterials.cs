using System.Collections.Generic;
using SharpDX.Direct3D11;
using VRage.Render11.Common;
using VRage.Render11.Resources;

namespace VRageRender
{
    static class MySceneMaterials
    {
        internal static ISrvBuffer m_buffer;

        static readonly List<MyPerMaterialData> Data = new List<MyPerMaterialData>();
        static readonly Dictionary<int, int> HashIndex = new Dictionary<int, int>();

        // data refreshed every frame
        static readonly MyPerMaterialData[] TransferData = new MyPerMaterialData[4096];
        static readonly Dictionary<int, uint> TransferHashIndex = new Dictionary<int, uint>();

        internal unsafe static void Init()
        {
            m_buffer = MyManagers.Buffers.CreateSrv(
                "MySceneMaterials", 4096, sizeof(MyPerMaterialData),
                usage: ResourceUsage.Dynamic);
        }

        internal static void PreFrame()
        {
            TransferHashIndex.Clear();

            // bump default material as 0
            MyPerMaterialData defaultMat = new MyPerMaterialData();
            GetDrawMaterialIndex(GetPerMaterialDataIndex(ref defaultMat));

            // bump foliage material as 1 (important)
            MyPerMaterialData foliageMat = new MyPerMaterialData();
            foliageMat.Type = MyMaterialTypeEnum.FOLIAGE;
            GetDrawMaterialIndex(GetPerMaterialDataIndex(ref foliageMat));
        }

        internal static int GetPerMaterialDataIndex(ref MyPerMaterialData data)
        {
            var key = data.CalculateKey();

            if (!HashIndex.ContainsKey(key))
            {
                HashIndex[key] = HashIndex.Count;
                Data.Add(data);
            }

            return HashIndex[key];
        }

        internal static uint GetDrawMaterialIndex(int index)
        {
            if (!TransferHashIndex.ContainsKey(index))
            {
                TransferHashIndex[index] = (uint)TransferHashIndex.Count;
                TransferData[TransferHashIndex.Count - 1] = Data[index];
            }

            return TransferHashIndex[index];
        }

        internal static void OnDeviceReset()
        {
            if (m_buffer != null)
                MyManagers.Buffers.Dispose(m_buffer); m_buffer = null;

            Init();
        }

        internal static unsafe void MoveToGPU()
        {
            var mapping = MyMapping.MapDiscard(m_buffer);
            mapping.WriteAndPosition(TransferData, sizeof(MyPerMaterialData) * TransferHashIndex.Count);
            mapping.Unmap();
        }
    }
}
