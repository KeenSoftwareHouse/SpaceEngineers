using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    static class MySceneMaterials
    {
        internal static StructuredBufferId m_buffer = StructuredBufferId.NULL;

        static readonly List<MyPerMaterialData> Data = new List<MyPerMaterialData>();
        static readonly Dictionary<int, int> HashIndex = new Dictionary<int, int>();

        // data refreshed every frame
        static readonly MyPerMaterialData[] TransferData = new MyPerMaterialData[4096];
        static readonly Dictionary<int, uint> TransferHashIndex = new Dictionary<int, uint>();

        internal unsafe static void Init()
        {
            m_buffer = MyHwBuffers.CreateStructuredBuffer(4096, sizeof(MyPerMaterialData), true);
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
            if (m_buffer != StructuredBufferId.NULL)
            {
                MyHwBuffers.Destroy(m_buffer);
                m_buffer = StructuredBufferId.NULL;
            }
            Init();
        }

        internal unsafe static void MoveToGPU()
        {
            var context = MyImmediateRC.RC.DeviceContext;

            fixed (void* ptr = TransferData)
            {
                var intPtr = new IntPtr(ptr);

                var mapping = MyMapping.MapDiscard(m_buffer.Buffer);
                mapping.WriteAndPosition(TransferData, 0, sizeof(MyPerMaterialData) * TransferHashIndex.Count);
                mapping.Unmap();
            }
        }
    }
}
