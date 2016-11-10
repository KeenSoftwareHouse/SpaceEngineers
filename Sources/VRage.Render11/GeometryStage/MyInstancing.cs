using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Profiler;
using VRage.Render11.Common;
using VRage.Render11.Resources;
using VRageMath;
using VRageRender.Messages;
using VRageRender.Vertex;

namespace VRageRender
{
    struct InstancingId
    {
        internal int Index;

        internal static readonly InstancingId NULL = new InstancingId { Index = -1 };

        internal MyInstancingInfo Info { get { return MyInstancing.Instancings.Data[Index]; } }

        internal IVertexBuffer VB { get { return MyInstancing.Data[Index].VB; } }

        #region Equals
        public static bool operator ==(InstancingId x, InstancingId y)
        {
            return x.Index == y.Index;
        }

        public static bool operator !=(InstancingId x, InstancingId y)
        {
            return x.Index != y.Index;
        }

        public class MyInstancingIdComparerType : IEqualityComparer<InstancingId>
        {
            public bool Equals(InstancingId left, InstancingId right)
            {
                return left.Index == right.Index;
            }
            public int GetHashCode(InstancingId instancingId)
            {
                return instancingId.Index;
            }
        }
        public static readonly MyInstancingIdComparerType Comparer = new MyInstancingIdComparerType();
        #endregion

        private static StringBuilder m_stringHelper = new StringBuilder();
        public override string ToString()
        {
            m_stringHelper.Clear();
            return m_stringHelper.AppendInt32(Index).ToString();
        }
    }

    struct MyInstancingInfo
    {
        internal uint ParentID;
        internal MyRenderInstanceBufferType Type;
        internal VertexLayoutId Layout;
        internal int VisibleCapacity;
        internal int TotalCapacity;
        internal int Stride;
        internal string DebugName;
        internal byte[] Data;
        // Culling data
        // This is needed to be able to rebuild the buffer any time the VisibilityMask is changed
        internal MyInstanceData[] InstanceData;
        internal Vector3[] Positions;

        // Do not modify directly
        internal bool[] VisibilityMask;
        internal int NonVisibleInstanceCount;

        internal bool PerInstanceLods;

        internal void SetVisibility(int index, bool value)
        {
            if (VisibilityMask == null || index >= VisibilityMask.Length)
            {
                //Debug.Fail("Setting visibility for invalid index in instance buffer! (AF)");
                return;
            }
            if (value != VisibilityMask[index])
            {
                VisibilityMask[index] = value;
                NonVisibleInstanceCount += value ? -1 : 1;
            }
        }

        internal void ResetVisibility()
        {
            for (int maskIndex = 0; maskIndex < TotalCapacity; ++maskIndex)
            {
                VisibilityMask[maskIndex] = true;
            }
            NonVisibleInstanceCount = 0;
        }
    }

    struct MyInstancingData
    {
        internal IVertexBuffer VB;
    }

    static class MyInstancing
    {
        static List<MyDecalPositionUpdate> m_tmpDecalsUpdate = new List<MyDecalPositionUpdate>();
        static readonly Dictionary<uint, InstancingId> IdIndex = new Dictionary<uint, InstancingId>();
        static MyActor m_instanceActor;

        internal static readonly MyFreelist<MyInstancingInfo> Instancings = new MyFreelist<MyInstancingInfo>(128);
        internal static MyInstancingData[] Data = new MyInstancingData[128];

        internal static InstancingId Get(uint GID)
        {
            return IdIndex.Get(GID, InstancingId.NULL);
        }
        internal static void Remove(uint GID)
        {
            var id = IdIndex.Get(GID, InstancingId.NULL);
            if (id != InstancingId.NULL)
            {
                MyInstancing.Remove(GID, id);
            }
            else MyRenderProxy.Assert(false);
        }

        internal static MyActor GetInstanceActor(MyRenderableComponent original)
        {
            if (m_instanceActor == null)
            {
                m_instanceActor = MyActorFactory.Create();
                m_instanceActor.AddComponent<MyInstanceLodComponent>(MyComponentFactory<MyInstanceLodComponent>.Create());
            }
            return m_instanceActor;
        }

        internal unsafe static InstancingId Create(uint GID, uint parentGID, MyRenderInstanceBufferType type, string debugName)
        {
            var id = new InstancingId { Index = Instancings.Allocate() };

            Instancings.Data[id.Index] = new MyInstancingInfo
            {
                ParentID = parentGID,
                Type = type,
                DebugName = debugName
            };

            MyArrayHelpers.Reserve(ref Data, id.Index + 1);
            Data[id.Index] = new MyInstancingData();

            if (type == MyRenderInstanceBufferType.Cube)
            {
                Instancings.Data[id.Index].Layout = MyVertexLayouts.GetLayout(new MyVertexInputComponent(MyVertexInputComponentType.CUBE_INSTANCE, 2, MyVertexInputComponentFreq.PER_INSTANCE));
                Instancings.Data[id.Index].Stride = sizeof(MyVertexFormatCubeInstance);
            }
            else
            {
                Instancings.Data[id.Index].Layout = MyVertexLayouts.GetLayout(new MyVertexInputComponent(MyVertexInputComponentType.GENERIC_INSTANCE, 2, MyVertexInputComponentFreq.PER_INSTANCE));
                Instancings.Data[id.Index].Stride = sizeof(MyVertexFormatGenericInstance);
            }

            Debug.Assert(!IdIndex.ContainsKey(GID), "Creating instance with ID that already exists!");
            IdIndex.Add(GID, id);

            return id;
        }

        internal static void RemoveResource(InstancingId id)
        {
            if (Data[id.Index].VB != null)
                MyManagers.Buffers.Dispose(Data[id.Index].VB); Data[id.Index].VB = null;
        }

        internal static void Remove(uint GID, InstancingId id)
        {
            Debug.Assert(IdIndex.ContainsKey(GID), "Removing instance that doesn't exist");
            RemoveResource(id);
            IdIndex.Remove(GID);
            Instancings.Free(id.Index);
        }

        private static void DisposeInstanceActor()
        {
            if (m_instanceActor != null)
            {
                if (!m_instanceActor.IsDestroyed)
                {
                    MyActorFactory.Destroy(m_instanceActor);
                }
                m_instanceActor = null;
            }
        }

        internal static void OnSessionEnd()
        {
            foreach (var id in IdIndex.ToArray())
            {
                Remove(id.Key, id.Value);
            }

            DisposeInstanceActor();
        }

        internal static unsafe void UpdateGeneric(InstancingId id, MyInstanceData[] instanceData, int capacity)
        {
            Debug.Assert(id.Info.Type == MyRenderInstanceBufferType.Generic, "Wrong type of instance buffer for instancing!");

            capacity = instanceData.Length;

            if (capacity != Instancings.Data[id.Index].TotalCapacity)
            {
                Vector3[] positions = new Vector3[capacity];
                bool[] mask = new bool[capacity];
                for (int arrayIndex = 0; arrayIndex < capacity; ++arrayIndex)
                {
                    positions[arrayIndex] = new Vector3(instanceData[arrayIndex].m_row0.ToVector4().W, instanceData[arrayIndex].m_row1.ToVector4().W, instanceData[arrayIndex].m_row2.ToVector4().W);
                    mask[arrayIndex] = true;
                }

                Instancings.Data[id.Index].VisibleCapacity = capacity;
                Instancings.Data[id.Index].Positions = positions;
                Instancings.Data[id.Index].VisibilityMask = mask;
                Instancings.Data[id.Index].NonVisibleInstanceCount = 0;
            }

            Instancings.Data[id.Index].TotalCapacity = capacity;
            Instancings.Data[id.Index].InstanceData = instanceData;

            RebuildGeneric(id);
            MyInstanceLodComponent.ClearInvalidInstances(id);
        }

        internal static unsafe void RebuildGeneric(InstancingId instancingId)
        {
            ProfilerShort.Begin("RebuildGeneric");
            //Debug.Assert(instancingId.Info.Type == MyRenderInstanceBufferType.Generic);
            if (Instancings.Data[instancingId.Index].InstanceData == null)
            {
                ProfilerShort.End();
                //Debug.Fail("Instance Data is null!");
                return;
            }

            var info = instancingId.Info;

            int capacity = Instancings.Data[instancingId.Index].InstanceData.Length;
            for (int maskIndex = 0; maskIndex < Instancings.Data[instancingId.Index].TotalCapacity; ++maskIndex)
            {
                if (!Instancings.Data[instancingId.Index].VisibilityMask[maskIndex])
                    --capacity;
            }

            var byteSize = info.Stride * capacity;

            MyArrayHelpers.InitOrReserve(ref Instancings.Data[instancingId.Index].Data, byteSize);

            fixed (void* src = Instancings.Data[instancingId.Index].InstanceData)
            {
                fixed (void* dst = Instancings.Data[instancingId.Index].Data)
                {
                    int currentIndex = 0;
                    for (int maskIndex = 0; maskIndex < Instancings.Data[instancingId.Index].TotalCapacity; ++maskIndex)
                    {
                        if (Instancings.Data[instancingId.Index].VisibilityMask[maskIndex])
                        {
                            SharpDX.Utilities.CopyMemory(new IntPtr(dst) + currentIndex * info.Stride, new IntPtr(src) + maskIndex * info.Stride, info.Stride);
                            ++currentIndex;
                        }
                    }
                }
            }

            Instancings.Data[instancingId.Index].VisibleCapacity = capacity;
            UpdateVertexBuffer(instancingId);
            ProfilerShort.End();
        }

        internal static unsafe void UpdateCube(InstancingId id, List<MyCubeInstanceData> instanceData, List<MyCubeInstanceDecalData> decals, int capacity)
        {
            UpdateDecalPositions(id, instanceData, decals);

            Debug.Assert(id.Info.Type == MyRenderInstanceBufferType.Cube);

            var info = id.Info;

            var byteSize = info.Stride * capacity;

            MyArrayHelpers.InitOrReserve(ref Instancings.Data[id.Index].Data, byteSize);

            fixed (void* dst = Instancings.Data[id.Index].Data)
            {
                MyVertexFormatCubeInstance* ptr = (MyVertexFormatCubeInstance*)dst;

                for (int i = 0; i < instanceData.Count; i++)
                {
                    fixed (byte* pSource = instanceData[i].RawBones())
                    {
                        for (int j = 0; j < MyRender11Constants.CUBE_INSTANCE_BONES_NUM * 4; j++)
                            ptr[i].bones[j] = pSource[j];
                    }
                    ptr[i].translationRotation = instanceData[i].m_translationAndRot;

                    var colorMaskHSV = instanceData[i].ColorMaskHSV;
                    ptr[i].colorMaskHSV = colorMaskHSV;

                }
            }

            Instancings.Data[id.Index].VisibleCapacity = capacity;
            UpdateVertexBuffer(id);
        }

        private static void UpdateDecalPositions(InstancingId id, List<MyCubeInstanceData> instanceData, List<MyCubeInstanceDecalData> decals)
        {
            m_tmpDecalsUpdate.Clear();
            for (int it1 = 0; it1 < decals.Count; it1++)
            {
                MyCubeInstanceDecalData decal = decals[it1];
                MyCubeInstanceData cubeData = instanceData[decal.InstanceIndex];
                if (!cubeData.EnableSkinning)
                    break;

                MyDecalTopoData decalTopo;
                bool found = MyScreenDecals.GetDecalTopoData(decal.DecalId, out decalTopo);
                if (!found)
                    continue;

                Matrix localCubeMatrix;
                Matrix skinningMatrix = cubeData.ConstructDeformedCubeInstanceMatrix(ref decalTopo.BoneIndices, ref decalTopo.BoneWeights, out localCubeMatrix);

                Matrix localCubeMatrixInv;
                Matrix.Invert(ref localCubeMatrix, out localCubeMatrixInv);

                // TODO: Optimization: it would be cool if we keep original cube coordiates local intersection
                // and avoid matrix inversion here. Refer to MyCubeGrid.GetIntersectionWithLine(), MyCubeGridHitInfo

                Matrix invBinding = decalTopo.MatrixBinding * localCubeMatrixInv;
                Matrix transform = invBinding * skinningMatrix;

                m_tmpDecalsUpdate.Add(new MyDecalPositionUpdate() { ID = decal.DecalId, Transform = transform });
            }

            MyScreenDecals.UpdateDecals(m_tmpDecalsUpdate);
        }

        internal static unsafe void UpdateVertexBuffer(InstancingId id)
        {
            var info = id.Info;
            if (info.VisibleCapacity == 0)
                return;

            fixed (byte* ptr = info.Data)
            {
                IVertexBuffer buffer = Data[id.Index].VB;
                if (buffer == null)
                {
                    Data[id.Index].VB = MyManagers.Buffers.CreateVertexBuffer(
                        info.DebugName, info.VisibleCapacity, info.Stride,
                        new IntPtr(ptr), SharpDX.Direct3D11.ResourceUsage.Dynamic);
                }
                else if (buffer.ElementCount < info.VisibleCapacity ||
                         buffer.Description.StructureByteStride != info.Stride)
                {
                    MyManagers.Buffers.Resize(Data[id.Index].VB, info.VisibleCapacity, info.Stride, new IntPtr(ptr));
                }
                else
                {
                    var mapping = MyMapping.MapDiscard(MyImmediateRC.RC, buffer);
                    mapping.WriteAndPosition(info.Data, info.VisibleCapacity * info.Stride);
                    mapping.Unmap();
                }
            }
        }

        internal static void OnDeviceReset()
        {
            foreach (var id in IdIndex.Values)
            {
                RemoveResource(id);
                UpdateVertexBuffer(id);
            }

            DisposeInstanceActor();
        }

        public static void UpdateGenericSettings(InstancingId handle, bool setPerInstanceLod)
        {
            Instancings.Data[handle.Index].PerInstanceLods = setPerInstanceLod;
        }
    }
}
