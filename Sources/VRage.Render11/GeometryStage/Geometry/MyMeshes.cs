﻿using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using VRage;
using VRage.Import;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;
using VRageMath.PackedVector;
using VRageRender.Vertex;
using VRage.FileSystem;
using SharpDX.Direct3D11;

namespace VRageRender
{
    struct MeshId
    {
        internal int Index;

        #region Equals
        public class MyMeshIdComparerType : IEqualityComparer<MeshId>
        {
            public bool Equals(MeshId left, MeshId right)
            {
                return left.Index == right.Index;
            }

            public int GetHashCode(MeshId meshId)
            {
                return meshId.Index;
            }
        }
        public static readonly MyMeshIdComparerType Comparer = new MyMeshIdComparerType();

        public static bool operator ==(MeshId x, MeshId y)
        {
            return x.Index == y.Index;
        }

        public static bool operator !=(MeshId x, MeshId y)
        {
            return x.Index != y.Index;
        }
        #endregion

        internal static readonly MeshId NULL = new MeshId { Index = -1 };

        internal MyMeshInfo Info { get { return MyMeshes.MeshInfos.Data[Index]; } }
    }

    struct LodMeshId
    {
        internal int Index;

        #region Equals
        public class MyLodMeshIdComparerType : IEqualityComparer<LodMeshId>
        {
            public bool Equals(LodMeshId left, LodMeshId right)
            {
                return left.Index == right.Index;
            }

            public int GetHashCode(LodMeshId lodMeshId)
            {
                return lodMeshId.Index;
            }
        }
        internal static readonly MyLodMeshIdComparerType Comparer = new MyLodMeshIdComparerType();

        public static bool operator ==(LodMeshId x, LodMeshId y)
        {
            return x.Index == y.Index;
        }

        public static bool operator !=(LodMeshId x, LodMeshId y)
        {
            return x.Index != y.Index;
        }
        #endregion

        internal static readonly LodMeshId NULL = new LodMeshId { Index = -1 };

        internal MyLodMeshInfo Info { get { return MyMeshes.LodMeshInfos.Data[Index]; } }

        internal MyMeshBuffers Buffers { get { return Index != -1 ? MyMeshes.LodMeshBuffers[Index] : MyMeshBuffers.Empty; } }

        internal VertexLayoutId VertexLayout { get { return MyMeshes.LodMeshInfos.Data[Index].Data.VertexLayout; } }
    }

    struct MyMergedLodMeshId : IEquatable<MyMergedLodMeshId>
    {
        internal int Index;

        internal static readonly MyMergedLodMeshId NULL = new MyMergedLodMeshId { Index = -1 };

        internal MyMergedLodMeshInfo Info { get { return MyMeshes.MergedLodMeshInfos.Data[Index]; } }
        internal MyMeshBuffers Buffers { get { return Index != -1 ? MyMeshes.MergedLodMeshBuffers[Index] : MyMeshBuffers.Empty; } }
        internal VertexLayoutId VertexLayout
        {
            get { var mergedMeshes = MyMeshes.MergedLodMeshInfos.Data[Index].MergedLodMeshes; return mergedMeshes.Count > 0 ? mergedMeshes.First().VertexLayout : VertexLayoutId.NULL; }
        }

        #region Equals
        public class MyMergedLodMeshIdComparerType : IEqualityComparer<MyMergedLodMeshId>
        {
            public bool Equals(MyMergedLodMeshId left, MyMergedLodMeshId right)
            {
                return left.Index == right.Index;
            }

            public int GetHashCode(MyMergedLodMeshId mergedLodMeshId)
            {
                return mergedLodMeshId.GetHashCode();
            }
        }
        internal static readonly MyMergedLodMeshIdComparerType Comparer = new MyMergedLodMeshIdComparerType();

        bool IEquatable<MyMergedLodMeshId>.Equals(MyMergedLodMeshId other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj.GetType() == GetType()))
                return false;

            var meshId = (MyMergedLodMeshId)obj;
            return this == meshId;
        }

        public static bool operator ==(MyMergedLodMeshId left, MyMergedLodMeshId right)
        {
            return left.Index == right.Index;
        }

        public static bool operator !=(MyMergedLodMeshId left, MyMergedLodMeshId right)
        {
            return !(left.Index == right.Index);
        }
        #endregion

        public override int GetHashCode()
        {
            return Index;
        }
    }

    struct MeshPartId
    {
        internal int Index;

        #region Equals
        public class MyMeshPartIdComparerType : IEqualityComparer<MeshPartId>
        {
            public bool Equals(MeshPartId left, MeshPartId right)
            {
                return left.Index == right.Index;
            }

            public int GetHashCode(MeshPartId meshPartId)
            {
                return meshPartId.Index;
            }
        }
        public static readonly MyMeshPartIdComparerType Comparer = new MyMeshPartIdComparerType();

        public static bool operator ==(MeshPartId x, MeshPartId y)
        {
            return x.Index == y.Index;
        }

        public static bool operator !=(MeshPartId x, MeshPartId y)
        {
            return x.Index != y.Index;
        }
        #endregion

        internal static readonly MeshPartId NULL = new MeshPartId { Index = -1 };

        internal MyMeshPartInfo1 Info { get { return MyMeshes.Parts.Data[Index]; } }
    }

    struct MeshSectionId
    {
        internal int Index;

        #region Equals
        public class MyMeshSectionIdComparerType : IEqualityComparer<MeshSectionId>
        {
            public bool Equals(MeshSectionId left, MeshSectionId right)
            {
                return left.Index == right.Index;
            }

            public int GetHashCode(MeshSectionId sectionId)
            {
                return sectionId.Index;
            }
        }
        public static readonly MyMeshSectionIdComparerType Comparer = new MyMeshSectionIdComparerType();

        public static bool operator ==(MeshSectionId x, MeshSectionId y)
        {
            return x.Index == y.Index;
        }

        public static bool operator !=(MeshSectionId x, MeshSectionId y)
        {
            return x.Index != y.Index;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is MeshSectionId))
                return false;

            MeshSectionId id = (MeshSectionId)obj;
            return Index == id.Index;
        }

        public override int GetHashCode()
        {
            return Index;
        }

        #endregion

        internal static readonly MeshSectionId NULL = new MeshSectionId { Index = -1 };

        internal MyMeshSectionInfo1 Info { get { return MyMeshes.Sections.Data[Index]; } }
    }

    struct VoxelPartId
    {
        internal int Index;

        #region Equals
        public class MyVoxelPartIdComparerType : IEqualityComparer<VoxelPartId>
        {
            public bool Equals(VoxelPartId left, VoxelPartId right)
            {
                return left.Index == right.Index;
            }

            public int GetHashCode(VoxelPartId voxelPartId)
            {
                return voxelPartId.Index;
            }
        }
        public static readonly MyVoxelPartIdComparerType Comparer = new MyVoxelPartIdComparerType();

        public static bool operator ==(VoxelPartId x, VoxelPartId y)
        {
            return x.Index == y.Index;
        }

        public static bool operator !=(VoxelPartId x, VoxelPartId y)
        {
            return x.Index != y.Index;
        }
        #endregion

        internal static readonly VoxelPartId NULL = new VoxelPartId { Index = -1 };

        internal MyVoxelPartInfo1 Info { get { return MyMeshes.VoxelParts.Data[Index]; } }
    }

    struct MyMeshPartInfo1
    {
        internal int IndexCount;
        internal int StartIndex;
        internal int BaseVertex;
        internal int SectionSubmeshCount;

        internal int[] BonesMapping;

        internal Vector3 CenterOffset;

        internal MyMeshMaterialId Material;
    }


    [DebuggerDisplay("Name = {Name}, MeshCount = {Meshes.Length}")]
    struct MyMeshSectionInfo1
    {
        internal string Name;
        internal MyMeshSectionPartInfo1[] Meshes;
    }

    struct MyMeshSectionPartInfo1
    {
        internal int StartIndex;
        internal int IndexCount;
        internal int BaseVertex;
        internal int PartIndex;
        internal int PartSubmeshIndex;
        internal MyMeshMaterialId Material;
    }


	struct MyMeshRawData
	{
		internal VertexLayoutId VertexLayout;

		internal byte[] Indices;
		internal Format IndicesFmt;

		internal byte[] VertexStream0;
		internal int Stride0;

		internal byte[] VertexStream1;
		internal int Stride1;
	}

    struct MyLodMeshInfo
    {
        internal string Name;
        internal string FileName;
        internal int PartsNum;
        internal int SectionsNum;
        internal bool HasBones { get { return Data.VertexLayout.Info.HasBonesInfo; } }

        internal int VerticesNum;
        internal int IndicesNum;

        internal float LodDistance;

        internal MyMeshRawData Data;
        internal MyClipmapCellBatch[] DataBatches;
        internal MyClipmapCellMeshMetadata BatchMetadata;

        internal BoundingBox? BoundingBox;
        internal BoundingSphere? BoundingSphere;
        internal bool NullLodMesh;
    }

    struct MyMergedLodMeshInfo
    {
        #region Comparer
        public class MergedLodMeshInfoComparerType : IEqualityComparer<MyMergedLodMeshInfo>
        {
            public bool Equals(MyMergedLodMeshInfo x, MyMergedLodMeshInfo y)
            {
                return x.MergedLodMeshes == y.MergedLodMeshes;
            }

            public int GetHashCode(MyMergedLodMeshInfo obj)
            {
                return obj.MergedLodMeshes.GetHashCode();
            }
        }

        internal static readonly MergedLodMeshInfoComparerType Comparer = new MergedLodMeshInfoComparerType();
        #endregion

        internal int PartsNum;
        internal int VerticesNum;
        internal int IndicesNum;

        internal MyMeshRawData Data;

        internal BoundingBox? BoundingBox;

        internal HashSet<LodMeshId> PendingLodMeshes;
        internal HashSet<LodMeshId> MergedLodMeshes;
        internal bool NullMesh;
    }

    struct MyMeshInfo
    {
        internal int LodsNum;
        internal string Name;
        internal MyStringId NameKey;
        internal bool Dynamic;
        internal bool RuntimeGenerated;
        internal bool Loaded;
        internal bool FileExists;
    }


    struct MyMeshBuffers
    {
        internal VertexBufferId VB0;
        internal VertexBufferId VB1;
        internal IndexBufferId IB;

        public static bool operator==(MyMeshBuffers left, MyMeshBuffers right)
        {
            return left.VB0 == right.VB0 && left.VB1 == right.VB1 && left.IB == right.IB;
        }

        public static bool operator !=(MyMeshBuffers left, MyMeshBuffers right)
        {
            return left.VB0 != right.VB0 || left.VB1 != right.VB1 || left.IB == right.IB;
        }

        internal static readonly MyMeshBuffers Empty = new MyMeshBuffers { IB = IndexBufferId.NULL, VB0 = VertexBufferId.NULL, VB1 = VertexBufferId.NULL };
    }

    struct MyVoxelCellInfo
    {
        internal Vector3I Coord;
        internal int Lod;
    }

    struct MyVoxelPartInfo1
    {
        internal int IndexCount;
        internal int StartIndex;
        internal int BaseVertex;

        internal MyVoxelMaterialTriple MaterialTriple;
    }

    //struct MyPipelineInputAssemblerData
    //{
    //    internal SharpDX.Direct3D11.Buffer[] VertexBuffers;
    //    internal uint[] Strides;
    //    internal SharpDX.Direct3D11.Buffer IndexBuffer;
    //    internal SharpDX.DXGI.Format IndexFormat;
    //}

    enum MyMeshState
    {
        WAITING,
        LOADED
    }

    struct MyLodMesh
    {
        internal MeshId Mesh;
        internal int Lod;

        #region Equals
        public class MyLodMeshComparerType : IEqualityComparer<MyLodMesh>
        {
            public bool Equals(MyLodMesh left, MyLodMesh right)
            {
                return left.Mesh == right.Mesh && left.Lod == right.Lod;
            }

            public int GetHashCode(MyLodMesh lodMesh)
            {
                return (ushort)(MeshId.Comparer.GetHashCode() << 16) | (ushort)lodMesh.Lod;
            }
        }
        public static readonly MyLodMeshComparerType Comparer = new MyLodMeshComparerType();
        #endregion
    }

    struct MyMergedLodMesh
    {
        internal MeshId Mesh;
        internal int Lod;
    }

    struct MyMeshPart
    {
        internal MeshId Mesh;
        internal int Lod;
        internal int Part;

        #region Equals
        public class MyMeshPartComparerType : IEqualityComparer<MyMeshPart>
        {
            public bool Equals(MyMeshPart left, MyMeshPart right)
            {
                return left.Mesh == right.Mesh &&
                        left.Lod == right.Lod &&
                        left.Part == right.Part;
            }

            public int GetHashCode(MyMeshPart meshPart)
            {
                return meshPart.Mesh.GetHashCode() << 20 | meshPart.Lod << 10 | meshPart.Part;
            }
        }
        public static readonly MyMeshPartComparerType Comparer = new MyMeshPartComparerType();
        #endregion
    }

    struct MyMeshSection
    {
        internal MeshId Mesh;
        internal int Lod;
        internal int Section;

        #region Equals
        public class MyMeshSectionComparerType : IEqualityComparer<MyMeshSection>
        {
            public bool Equals(MyMeshSection left, MyMeshSection right)
            {
                return left.Mesh == right.Mesh &&
                        left.Section == right.Section;
            }

            public int GetHashCode(MyMeshSection section)
            {
                return section.Mesh.GetHashCode() << 20 | section.Lod << 10 | section.Section;
            }
        }
        public static readonly MyMeshSectionComparerType Comparer = new MyMeshSectionComparerType();

        #endregion
    }

    // fractures are the only asset existing over sessions (performance reasons) and some parts need to be recreated after they get dropped on session end (like material ids)
    struct MyRuntimeMeshPersistentInfo
    {
        internal MySectionInfo[] Sections;
    }

    static class MyMeshes
    {
        static readonly Dictionary<MyStringId, MeshId> MeshNameIndex = new Dictionary<MyStringId, MeshId>(MyStringId.Comparer);
        static readonly Dictionary<MyStringId, MeshId> RuntimeMeshNameIndex = new Dictionary<MyStringId, MeshId>(MyStringId.Comparer);

        internal static readonly MyFreelist<MyMeshInfo> MeshInfos = new MyFreelist<MyMeshInfo>(4096);

        internal static readonly MyFreelist<MyLodMeshInfo> LodMeshInfos = new MyFreelist<MyLodMeshInfo>(4096);
        static readonly Dictionary<MyLodMesh, LodMeshId> LodMeshIndex = new Dictionary<MyLodMesh, LodMeshId>(MyLodMesh.Comparer);
        internal static MyMeshBuffers[] LodMeshBuffers = new MyMeshBuffers[4096];

        internal static readonly MyFreelist<MyMeshPartInfo1> Parts = new MyFreelist<MyMeshPartInfo1>(8192);
        internal static readonly MyFreelist<MyMeshSectionInfo1> Sections = new MyFreelist<MyMeshSectionInfo1>(8192);

        static readonly Dictionary<MeshId, MyVoxelCellInfo> MeshVoxelInfo = new Dictionary<MeshId, MyVoxelCellInfo>(MeshId.Comparer);

        static readonly Dictionary<MyMeshPart, VoxelPartId> VoxelPartIndex = new Dictionary<MyMeshPart, VoxelPartId>(MyMeshPart.Comparer);
        static readonly Dictionary<MyMeshPart, MeshPartId> PartIndex = new Dictionary<MyMeshPart, MeshPartId>(MyMeshPart.Comparer);
        static readonly Dictionary<MyMeshSection, MeshSectionId> SectionIndex = new Dictionary<MyMeshSection, MeshSectionId>(MyMeshSection.Comparer);

        #region Merged meshes
        private static readonly Dictionary<MyMergedLodMesh, MyMergedLodMeshId> MergedLodMeshIndex = new Dictionary<MyMergedLodMesh, MyMergedLodMeshId>();
        private static readonly Dictionary<MyMergedLodMeshId, MyVoxelCellInfo> MergedMeshVoxelInfo = new Dictionary<MyMergedLodMeshId, MyVoxelCellInfo>(MyMergedLodMeshId.Comparer);
        internal static readonly Dictionary<LodMeshId, MyMergedLodMeshId> LodMeshToMerged = new Dictionary<LodMeshId, MyMergedLodMeshId>(LodMeshId.Comparer);
        internal static readonly MyFreelist<MyMergedLodMeshInfo> MergedLodMeshInfos = new MyFreelist<MyMergedLodMeshInfo>(2048);
        internal static MyMeshBuffers[] MergedLodMeshBuffers = new MyMeshBuffers[4096];
        #endregion

        internal static readonly MyFreelist<MyVoxelPartInfo1> VoxelParts = new MyFreelist<MyVoxelPartInfo1>(2048);

        static readonly Dictionary<MeshId, MyRuntimeMeshPersistentInfo> InterSessionData = new Dictionary<MeshId, MyRuntimeMeshPersistentInfo>(MeshId.Comparer);
        static readonly HashSet<MeshId> InterSessionDirty = new HashSet<MeshId>(MeshId.Comparer);

        static HashSet<MeshId>[] State;

        internal static VertexLayoutId VoxelLayout = VertexLayoutId.NULL;

        // Helpers to reduce allocations
        static MyVertexFormatVoxel[] m_tmpVertices0;
        static MyVertexFormatNormal[] m_tmpVertices1;
        static uint[] m_tmpIndices;
        static ushort[] m_tmpShortIndices;

        internal static void Init()
        {
            int statesNum = Enum.GetNames(typeof(MyMeshState)).Length;
            State = new HashSet<MeshId>[statesNum];
            for (int stateIndex = 0; stateIndex < statesNum; ++stateIndex)
            {
                State[stateIndex] = new HashSet<MeshId>(MeshId.Comparer);
            }

            for (int bufferIndex = 0; bufferIndex < LodMeshBuffers.Length; ++bufferIndex )
            {
                LodMeshBuffers[bufferIndex] = MyMeshBuffers.Empty;
            }

            for (int bufferIndex = 0; bufferIndex < MergedLodMeshBuffers.Length; ++bufferIndex )
            {
                MergedLodMeshBuffers[bufferIndex] = MyMeshBuffers.Empty;
            }

            VoxelLayout = MyVertexLayouts.GetLayout(
                new MyVertexInputComponent(MyVertexInputComponentType.VOXEL_POSITION_MAT),
                new MyVertexInputComponent(MyVertexInputComponentType.VOXEL_NORMAL, 1));
        }

        internal static bool Exists(string name)
        {
            var x = X.TEXT_(name);
            return MeshNameIndex.ContainsKey(x) || RuntimeMeshNameIndex.ContainsKey(x);
        }

        internal static bool IsVoxelMesh(MeshId mesh)
        {
            return MeshVoxelInfo.ContainsKey(mesh);
        }

        internal static bool IsMergedVoxelMesh(MeshId mesh)
        {
            return MergedLodMeshIndex.ContainsKey(new MyMergedLodMesh { Mesh = mesh, Lod = 0 });
        }

        internal static MyVoxelCellInfo GetVoxelInfo(MeshId mesh)
        {
            return MeshVoxelInfo[mesh];
        }

        internal static MyVoxelCellInfo GetVoxelInfo(MyMergedLodMeshId mergedLodMeshId)
        {
            return MergedMeshVoxelInfo[mergedLodMeshId];
        }

        internal static LodMeshId GetLodMesh(MeshId mesh, int lod)
        {
            var id = new MyLodMesh { Mesh = mesh, Lod = lod };
            return LodMeshIndex[id];
        }

        internal static bool TryGetLodMesh(MeshId mesh, int lod, out LodMeshId lodMeshId)
        {
            var lodMesh = new MyLodMesh { Mesh = mesh, Lod = lod };

            return LodMeshIndex.TryGetValue(lodMesh, out lodMeshId);
        }

        internal static MyMergedLodMeshId GetMergedLodMesh(MeshId mesh, int lod)
        {
            return MergedLodMeshIndex[new MyMergedLodMesh { Mesh = mesh, Lod = lod }];
        }

        internal static void LinkLodMeshToMerged(LodMeshId lodMeshId, MyMergedLodMeshId mergedLodMeshId)
        {
            LodMeshToMerged[lodMeshId] = mergedLodMeshId;
        }

        internal static bool UnlinkLodMeshFromMerged(LodMeshId lodMeshId)
        {
            Debug.Assert(LodMeshToMerged.ContainsKey(lodMeshId), "Trying to remove non-existent merged lod mesh link");
            return LodMeshToMerged.Remove(lodMeshId);
        }

        internal static bool IsLodMeshMerged(LodMeshId lodMesh)
        {
            bool isMerged = false;
            MyMergedLodMeshId mergedLodMesh;
            if (LodMeshToMerged.TryGetValue(lodMesh, out mergedLodMesh))
                isMerged = mergedLodMesh.Info.MergedLodMeshes.Contains(lodMesh);

            return isMerged;
        }

        internal static MeshPartId GetMeshPart(MeshId mesh, int lod, int part)
        {
            return PartIndex[new MyMeshPart { Mesh = mesh, Lod = lod, Part = part }];
        }

        internal static bool TryGetMeshPart(MeshId mesh, int lod, int part, out MeshPartId partId)
        {
            return PartIndex.TryGetValue(new MyMeshPart { Mesh = mesh, Lod = lod, Part = part }, out partId);
        }

        internal static MeshSectionId GetMeshSection(MeshId mesh, int lod, int section)
        {
            return SectionIndex[new MyMeshSection { Mesh = mesh, Lod = lod, Section = section }];
        }

        internal static bool TryGetMeshSection(MeshId mesh, int lod, int section, out MeshSectionId sectionId)
        {
            return SectionIndex.TryGetValue(new MyMeshSection { Mesh = mesh, Lod = lod, Section = section }, out sectionId);
        }

        internal static VoxelPartId GetVoxelPart(MeshId mesh, int part)
        {
            return VoxelPartIndex[new MyMeshPart { Mesh = mesh, Lod = 0, Part = part }];
        }

        internal static void InitState(MeshId id, MyMeshState state)
        {
            Debug.Assert(!CheckState(id, MyMeshState.LOADED));
            Debug.Assert(!CheckState(id, MyMeshState.WAITING));

            State[(int)state].Add(id);
        }

        internal static void MoveState(MeshId id, MyMeshState from, MyMeshState to)
        {
            State[(int)from].Remove(id);
            State[(int)to].Add(id);
        }

        internal static bool CheckState(MeshId id, MyMeshState state)
        {
            return State[(int)state].Contains(id);
        }

        internal static void ClearState(MeshId id)
        {
            for (int i = 0; i < State.Length; i++)
            {
                State[i].Remove(id);
            }
        }

        internal static MeshId GetMeshId(MyStringId nameKey)
        {
            if (RuntimeMeshNameIndex.ContainsKey(nameKey))
            {
                var id = RuntimeMeshNameIndex[nameKey];

                if (InterSessionDirty.Contains(id))
                {
                    RefreshMaterialIds(id);

                    InterSessionDirty.Remove(id);
                }

                return id;
            }

            if (!MeshNameIndex.ContainsKey(nameKey))
            {
                var id = new MeshId { Index = MeshInfos.Allocate() };
                MeshNameIndex[nameKey] = id;

                MeshInfos.Data[id.Index] = new MyMeshInfo
                {
                    Name = nameKey.ToString(),
                    NameKey = nameKey,
                    LodsNum = -1
                };

                InitState(id, MyMeshState.WAITING);
                LoadMesh(id);
                MoveState(id, MyMeshState.WAITING, MyMeshState.LOADED);
            }

            return MeshNameIndex[nameKey];
        }

        internal static void RemoveMesh(MeshId model)
        {
            Debug.Assert(!IsVoxelMesh(model));

            var lods = model.Info.LodsNum;
            for (int lodIndex = 0; lodIndex < lods; ++lodIndex)
            {
                var mesh = GetLodMesh(model, lodIndex);

                DisposeLodMeshBuffers(mesh);

                int parts = LodMeshInfos.Data[mesh.Index].PartsNum;
                for (int partIndex = 0; partIndex < parts; ++partIndex)
                {
                    var part = GetMeshPart(model, lodIndex, partIndex);
                    Parts.Free(part.Index);
                }

                LodMeshInfos.Free(mesh.Index);
            }

            if (model.Info.NameKey != MyStringId.NullOrEmpty)
            {
                MeshNameIndex.Remove(model.Info.NameKey);
                RuntimeMeshNameIndex.Remove(model.Info.NameKey);
            }

            MeshInfos.Free(model.Index);
        }

        internal static void OnSessionEnd()
        {
            bool KEEP_FRACTURES = true;

            foreach (var id in RuntimeMeshNameIndex.Values.ToArray())
            {
                bool fracture = id.Info.RuntimeGenerated && !id.Info.Dynamic;
                if (!(fracture && KEEP_FRACTURES))
                {
                    RemoveMesh(id);
                }
                else
                {
                    InterSessionDirty.Add(id);
                }
            }

            // remove voxels
            foreach (var id in MeshVoxelInfo.Keys.ToArray())
            {
                RemoveVoxelCell(id);
            }

            // remove non-runtime meshes
            foreach (var id in MeshNameIndex.Values.ToArray())
            {
                RemoveMesh(id);
            }

            MeshVoxelInfo.Clear();
            VoxelParts.Clear();
            VoxelPartIndex.Clear();

            for (int i = 0; i < Enum.GetNames(typeof(MyMeshState)).Length; i++)
            {
                State[i].Clear();
            }
        }

        static LodMeshId NewLodMesh(MeshId mesh, int lod)
        {
            var id = new LodMeshId { Index = LodMeshInfos.Allocate() };
            LodMeshInfos.Data[id.Index] = new MyLodMeshInfo { };

            bool isVoxelMesh = IsVoxelMesh(mesh);
            int usedLod = isVoxelMesh ? 0 : lod;

            LodMeshIndex[new MyLodMesh { Mesh = mesh, Lod = usedLod }] = id;
            MyArrayHelpers.Reserve(ref LodMeshBuffers, id.Index + 1);
            LodMeshBuffers[id.Index] = MyMeshBuffers.Empty;

            return id;
        }

        static MyMergedLodMeshId NewMergedLodMesh(MeshId mesh)
        {
            var mergedId = new MyMergedLodMeshId { Index = MergedLodMeshInfos.Allocate() };
            MergedLodMeshInfos.Data[mergedId.Index] = new MyMergedLodMeshInfo
            {
                PendingLodMeshes = new HashSet<LodMeshId>(LodMeshId.Comparer),
                MergedLodMeshes = new HashSet<LodMeshId>(LodMeshId.Comparer)
            };

            MergedLodMeshIndex[new MyMergedLodMesh { Mesh = mesh, Lod = 0 }] = mergedId;
            MyArrayHelpers.Reserve(ref MergedLodMeshBuffers, mergedId.Index + 1);
            MergedLodMeshBuffers[mergedId.Index] = MyMeshBuffers.Empty;

            return mergedId;
        }

        static MeshPartId NewMeshPart(MeshId mesh, int lod, int part)
        {
            var id = new MeshPartId { Index = Parts.Allocate() };
            Parts.Data[id.Index] = new MyMeshPartInfo1 { };

            PartIndex[new MyMeshPart { Mesh = mesh, Lod = lod, Part = part }] = id;

            return id;
        }

        static MeshSectionId NewMeshSection(MeshId mesh, int lod, int section)
        {
            var id = new MeshSectionId { Index = Sections.Allocate() };
            Sections.Data[id.Index] = new MyMeshSectionInfo1 { };

            SectionIndex[new MyMeshSection { Mesh = mesh, Lod = lod, Section = section }] = id;

            return id;
        }

        static bool LoadMwm(ref MyLodMeshInfo lodMeshInfo,
            out MyMeshPartInfo1[] parts, out MyMeshSectionInfo1[] sections)
        {
            MyLODDescriptor[] lodDescriptors;
            return LoadMwm(ref lodMeshInfo, out parts, out sections, out lodDescriptors);
        }

        // returns false when mesh couldn't be loaded
        unsafe static bool LoadMwm(ref MyLodMeshInfo lodMeshInfo,
            out MyMeshPartInfo1[] parts, out MyMeshSectionInfo1[] sections,
            out MyLODDescriptor[] lodDescriptors)
        {
            parts = null;
            sections = null;
            lodDescriptors = null;

            var importer = new MyModelImporter();
            var file = lodMeshInfo.FileName;
            if (file == null)
            {
                return true;
            }
            var fsPath = Path.IsPathRooted(file) ? file : Path.Combine(MyFileSystem.ContentPath, file);

            if (!MyFileSystem.FileExists(fsPath))
            {
                MyRender11.Log.WriteLine(String.Format("Mesh asset {0} missing", file));
                return false;
            }

            string contentPath = null;
            if (Path.IsPathRooted(file) && file.ToLower().Contains("models"))
                contentPath = file.Substring(0, file.ToLower().IndexOf("models"));


            importer.ImportData(fsPath, new string[]
            {
                MyImporterConstants.TAG_VERTICES,
                MyImporterConstants.TAG_BLENDINDICES,
                MyImporterConstants.TAG_BLENDWEIGHTS,
                MyImporterConstants.TAG_NORMALS,
                MyImporterConstants.TAG_TEXCOORDS0,
                MyImporterConstants.TAG_TANGENTS,
                MyImporterConstants.TAG_BINORMALS,
                MyImporterConstants.TAG_BONES,
                MyImporterConstants.TAG_MESH_PARTS,
                MyImporterConstants.TAG_MESH_SECTIONS,
                MyImporterConstants.TAG_BOUNDING_BOX,
                MyImporterConstants.TAG_BOUNDING_SPHERE,
                MyImporterConstants.TAG_LODS,
                MyImporterConstants.TAG_PATTERN_SCALE
            });
            Dictionary<string, object> tagData = importer.GetTagData();

            // LODS INFO EXTRACTING
            if (tagData.ContainsKey(MyImporterConstants.TAG_LODS))
            {
                var lods = (MyLODDescriptor[])tagData[MyImporterConstants.TAG_LODS];
                lodDescriptors = (MyLODDescriptor[])lods.Clone();
            }

            var positions = (HalfVector4[])tagData[MyImporterConstants.TAG_VERTICES];
            var verticesNum = positions.Length;

            lodMeshInfo.VerticesNum = verticesNum;

            Debug.Assert(positions.Length > 0);
            if (positions.Length == 0)
            {
                MyRender11.Log.WriteLine(String.Format("Mesh asset {0} has no vertices", file));
                importer.Clear();
                return false;
            }

            var boneIndices = (Vector4I[])tagData[MyImporterConstants.TAG_BLENDINDICES];
            var boneWeights = (Vector4[])tagData[MyImporterConstants.TAG_BLENDWEIGHTS];
            var normals = (Byte4[])tagData[MyImporterConstants.TAG_NORMALS];
            var texcoords = (HalfVector2[])tagData[MyImporterConstants.TAG_TEXCOORDS0];
            var tangents = (Byte4[])tagData[MyImporterConstants.TAG_TANGENTS];
            var bitangents = (Byte4[])tagData[MyImporterConstants.TAG_BINORMALS];
            var storedTangents = new Byte4[verticesNum];

            bool validStreams =
                ((normals.Length > 0) && normals.Length == verticesNum) &&
                ((texcoords.Length > 0) && texcoords.Length == verticesNum) &&
                ((tangents.Length > 0) && tangents.Length == verticesNum) &&
                ((bitangents.Length > 0) && bitangents.Length == verticesNum);

            if (validStreams == false)
            {
                MyRender11.Log.WriteLine(String.Format("Mesh asset {0} has inconsistent vertex streams", file));
                normals = new Byte4[0];
                texcoords = new HalfVector2[0];
                tangents = new Byte4[0];
                bitangents = new Byte4[0];
            }
            if (tangents.Length > 0 && bitangents.Length > 0)
            {
                // calculate tangents used by run-time
                for (int i = 0; i < verticesNum; i++)
                {
                    var N = VF_Packer.UnpackNormal(normals[i].PackedValue);
                    var T = VF_Packer.UnpackNormal(tangents[i].PackedValue);
                    var B = VF_Packer.UnpackNormal(bitangents[i].PackedValue);

                    var tanW = new Vector4(T.X, T.Y, T.Z, 0);

                    tanW.W = T.Cross(N).Dot(B) < 0 ? -1 : 1;
                    storedTangents[i] = VF_Packer.PackTangentSignB4(ref tanW);
                }
            }

            object patternScale;
            float PatternScale = 1f;
            if (tagData.TryGetValue(MyImporterConstants.TAG_PATTERN_SCALE, out patternScale))
            {
                PatternScale = (float)patternScale;
            }
            if (PatternScale != 1f && texcoords.Length > 0)
            {
                for (int i = 0; i < texcoords.Length; ++i)
                {
                    texcoords[i] = new HalfVector2(texcoords[i].ToVector2() / PatternScale);
                }
            }


            bool hasBonesInfo = boneIndices.Length > 0 && boneWeights.Length > 0 && boneIndices.Length == verticesNum && boneWeights.Length == verticesNum;
            var bones = (MyModelBone[])tagData[MyImporterConstants.TAG_BONES];

            bool isAnimated = hasBonesInfo;
            var matsIndices = new Dictionary<string, int>();
            if (tagData.ContainsKey(MyImporterConstants.TAG_MESH_PARTS))
            {
                var meshParts = tagData[MyImporterConstants.TAG_MESH_PARTS] as List<MyMeshPartInfo>;
                lodMeshInfo.PartsNum = meshParts.Count;
                parts = new MyMeshPartInfo1[lodMeshInfo.PartsNum];

                var partUsedBonesMap = new List<int[]>();
                #region Animations
                // check animations, bones, prepare remapping
                foreach (MyMeshPartInfo meshPart in meshParts)
                {
                    int[] bonesRemapping = null;
                    if (boneIndices.Length > 0 && bones.Length > MyRender11Constants.SHADER_MAX_BONES)
                    {
                        Dictionary<int, int> vertexChanged = new Dictionary<int, int>();

                        Dictionary<int, int> bonesUsed = new Dictionary<int, int>();

                        int trianglesNum = meshPart.m_indices.Count / 3;
                        for (int i = 0; i < trianglesNum; i++)
                        {
                            for (int j = 0; j < 3; j++)
                            {
                                int index = meshPart.m_indices[i * 3 + j];
                                if (boneWeights[index].X > 0)
                                    bonesUsed[boneIndices[index].X] = 1;
                                if (boneWeights[index].Y > 0)
                                    bonesUsed[boneIndices[index].Y] = 1;
                                if (boneWeights[index].Z > 0)
                                    bonesUsed[boneIndices[index].Z] = 1;
                                if (boneWeights[index].W > 0)
                                    bonesUsed[boneIndices[index].W] = 1;
                            }
                        }

                        if (bonesUsed.Count > MyRender11Constants.SHADER_MAX_BONES)
                        {
                            MyRender11.Log.WriteLine(String.Format("Model asset {0} has more than {1} bones in parth with {2} material", file, MyRender11Constants.SHADER_MAX_BONES, meshPart.m_MaterialDesc.MaterialName));
                            boneIndices = new Vector4I[0];
                            isAnimated = false;
                        }
                        else
                        {
                            var partBones = new List<int>(bonesUsed.Keys);
                            partBones.Sort();
                            if (partBones.Count > 0 && partBones[partBones.Count - 1] >= MyRender11Constants.SHADER_MAX_BONES)
                            {
                                for (int i = 0; i < partBones.Count; i++)
                                {
                                    bonesUsed[partBones[i]] = i;
                                }

                                Dictionary<int, int> vertexTouched = new Dictionary<int, int>();

                                for (int i = 0; i < trianglesNum; i++)
                                {
                                    for (int j = 0; j < 3; j++)
                                    {
                                        int index = meshPart.m_indices[i * 3 + j];
                                        if (!vertexTouched.ContainsKey(index))
                                        {
                                            if (boneWeights[index].X > 0)
                                                boneIndices[index].X = bonesUsed[boneIndices[index].X];
                                            if (boneWeights[index].Y > 0)
                                                boneIndices[index].Y = bonesUsed[boneIndices[index].Y];
                                            if (boneWeights[index].Z > 0)
                                                boneIndices[index].Z = bonesUsed[boneIndices[index].Z];
                                            if (boneWeights[index].W > 0)
                                                boneIndices[index].W = bonesUsed[boneIndices[index].W];

                                            vertexTouched[index] = 1;

                                            int changes = 0;
                                            vertexChanged.TryGetValue(index, out changes);
                                            vertexChanged[index] = changes + 1;
                                        }
                                    }
                                }

                                bonesRemapping = partBones.ToArray();
                            }
                        }
                    }
                    partUsedBonesMap.Add(bonesRemapping);
                }
                #endregion

                var indices = new List<uint>(positions.Length);
                uint maxIndex = 0;
                int partIndex = 0;
                foreach (MyMeshPartInfo meshPart in meshParts)
                {
                    int startIndex = indices.Count;
                    int indexCount = meshPart.m_indices.Count;

                    uint minIndex = (uint)meshPart.m_indices[0];
                    foreach (var i in meshPart.m_indices)
                    {
                        indices.Add((uint)i);
                        minIndex = Math.Min(minIndex, (uint)i);
                    }

                    uint baseVertex = minIndex;

                    for (int i = startIndex; i < startIndex + indexCount; i++)
                    {
                        indices[i] -= minIndex;
                        maxIndex = Math.Max(maxIndex, indices[i]);
                    }

                    #region Material

                    var materialDesc = meshPart.m_MaterialDesc;

                    if (materialDesc == null)
                        MyRender11.Log.WriteLine(String.Format("Mesh asset {0} has no material in part {1}", file, partIndex));
                    else
                        matsIndices[materialDesc.MaterialName] = partIndex;

                    var matId = MyMeshMaterials1.GetMaterialId(materialDesc, contentPath, file);

                    Vector3 centerOffset = Vector3.Zero;
                    if (materialDesc != null && (materialDesc.Facing == MyFacingEnum.Full || materialDesc.Facing == MyFacingEnum.Impostor))
                    {
                        Vector3[] unpackedPos = new Vector3[meshPart.m_indices.Count];
                        for (int i = 0; i < meshPart.m_indices.Count; i++)
                        {
                            HalfVector4 packed = positions[meshPart.m_indices[i]];
                            Vector3 pos = PositionPacker.UnpackPosition(ref packed);
                            centerOffset += pos;
                            unpackedPos[i] = pos;
                        }

                        centerOffset /= meshPart.m_indices.Count;

                        for (int i = 0; i < meshPart.m_indices.Count; i++)
                        {
                            Vector3 pos = unpackedPos[i];
                            pos -= centerOffset;
                            positions[meshPart.m_indices[i]] = PositionPacker.PackPosition(ref pos);
                        }
                    }

                    Debug.Assert(baseVertex <= int.MaxValue);
                    parts[partIndex] = new MyMeshPartInfo1
                    {
                        IndexCount = indexCount,
                        StartIndex = startIndex,
                        BaseVertex = (int)baseVertex,

                        Material = matId,

                        CenterOffset = centerOffset,

                        BonesMapping = partUsedBonesMap[partIndex]
                    };

                    #endregion

                    partIndex++;
                }

                #region Part data

                var rawData = new MyMeshRawData();

                lodMeshInfo.IndicesNum = indices.Count;

                // Create and fill index buffers
                if (maxIndex <= ushort.MaxValue)
                {
                    MyArrayHelpers.InitOrReserveNoCopy(ref m_tmpShortIndices, lodMeshInfo.IndicesNum);
                    uint[] sourceData = indices.GetInternalArray();

                    fixed(uint* sourcePointer = sourceData)
                    {
                        fixed(void* destinationPointer = m_tmpShortIndices)
                        {
                            CopyIndices(sourcePointer, destinationPointer, 0, 0, sizeof(ushort), (uint)lodMeshInfo.IndicesNum);
                        }
                    }

                    FillIndexData(ref rawData, m_tmpShortIndices, lodMeshInfo.IndicesNum);
                }
                else
                {
                    FillIndexData(ref rawData, indices.GetInternalArray(), lodMeshInfo.IndicesNum);
                }

                int numComponents = 0;
                if (!isAnimated) numComponents += 1; else numComponents += 3;
                if (validStreams) numComponents += 3;
                var vertexComponents = new List<MyVertexInputComponent>(numComponents);

                // stream 0
                if (!isAnimated)
                {
                    Debug.Assert(sizeof(HalfVector4) == sizeof(MyVertexFormatPositionH4));                    
                    FillStream0Data(ref rawData, positions, verticesNum);

                    vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.POSITION_PACKED));
                }
                else
                {
                    var vertices = new MyVertexFormatPositionSkinning[verticesNum];
                    fixed(MyVertexFormatPositionSkinning* destinationPointer = vertices)
                    {
                        for(int vertexIndex = 0; vertexIndex < verticesNum; ++vertexIndex)
                        {
                            destinationPointer[vertexIndex].Position = positions[vertexIndex];
                            destinationPointer[vertexIndex].BoneIndices = new Byte4(boneIndices[vertexIndex].X, boneIndices[vertexIndex].Y, boneIndices[vertexIndex].Z, boneIndices[vertexIndex].W);
                            destinationPointer[vertexIndex].BoneWeights = new HalfVector4(boneWeights[vertexIndex]);
                        }
                    }

                    FillStream0Data(ref rawData, vertices, verticesNum);

                    vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.POSITION_PACKED));
                    vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.BLEND_WEIGHTS));
                    vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.BLEND_INDICES));
                }

                // stream 1
                if (validStreams)
                {
                    var vertices = new MyVertexFormatTexcoordNormalTangent[verticesNum];
                    fixed (MyVertexFormatTexcoordNormalTangent* destinationPointer = vertices)
                    {
                        for (int vertexIndex = 0; vertexIndex < verticesNum; ++vertexIndex)
                        {
                            destinationPointer[vertexIndex].Normal = normals[vertexIndex];
                            destinationPointer[vertexIndex].Tangent = storedTangents[vertexIndex];
                            destinationPointer[vertexIndex].Texcoord = texcoords[vertexIndex];
                        }
                    }

                    FillStream1Data(ref rawData, vertices, vertices.Length);

                    vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.NORMAL, 1));
                    vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.TANGENT_SIGN_OF_BITANGENT, 1));
                    vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.TEXCOORD0_H, 1));
                }

                Debug.Assert(vertexComponents.Count == vertexComponents.Capacity);
                vertexComponents.Capacity = vertexComponents.Count;

                rawData.VertexLayout = MyVertexLayouts.GetLayout(vertexComponents.GetInternalArray());

                #endregion

                lodMeshInfo.Data = rawData;
                lodMeshInfo.BoundingBox = (BoundingBox)tagData[MyImporterConstants.TAG_BOUNDING_BOX];
                lodMeshInfo.BoundingSphere = (BoundingSphere)tagData[MyImporterConstants.TAG_BOUNDING_SPHERE];
            }

            var sectionSubmeshes = new int[parts.Length];
            if (tagData.ContainsKey(MyImporterConstants.TAG_MESH_SECTIONS))
            {
                var sectionInfos = tagData[MyImporterConstants.TAG_MESH_SECTIONS] as List<MyMeshSectionInfo>;
                lodMeshInfo.SectionsNum = sectionInfos.Count;

                sections = new MyMeshSectionInfo1[sectionInfos.Count];

                int sectionIndex = 0;
                int meshCount = 0;
                foreach (MyMeshSectionInfo section in sectionInfos)
                {
                    MyMeshSectionPartInfo1[] meshes = new MyMeshSectionPartInfo1[section.Meshes.Count];

                    int meshesIndex = 0;
                    foreach (MyMeshSectionMeshInfo mesh in section.Meshes)
                    {
                        int partIndex = matsIndices[mesh.MaterialName];
                        var matId = MyMeshMaterials1.GetMaterialId(mesh.MaterialName);
                        meshes[meshesIndex] = new MyMeshSectionPartInfo1()
                        {
                            IndexCount = mesh.IndexCount,
                            StartIndex = mesh.StartIndex + parts[partIndex].StartIndex,
                            BaseVertex = parts[partIndex].BaseVertex,
                            PartIndex = partIndex,
                            PartSubmeshIndex = sectionSubmeshes[partIndex],
                            Material = matId
                        };

                        sectionSubmeshes[partIndex]++;
                        meshesIndex++;
                    }

                    sections[sectionIndex] = new MyMeshSectionInfo1()
                    {
                        Name = section.Name,
                        Meshes = meshes
                    };

                    meshCount += section.Meshes.Count;
                    sectionIndex++;
                }

                for (int idx = 0; idx < parts.Length; idx++)
                    parts[idx].SectionSubmeshCount = sectionSubmeshes[idx];
            }

            importer.Clear();
            return true;
        }

        static LodMeshId StoreLodMeshWithParts(
            MeshId mesh, int lodIndex,
            ref MyLodMeshInfo lodMeshInfo,
            ref MyMeshPartInfo1[] parts)
        {
            var lod = NewLodMesh(mesh, lodIndex);

            LodMeshInfos.Data[lod.Index] = lodMeshInfo;

            if (parts != null)
            {
                for (int i = 0; i < parts.Length; i++)
                {
                    var part = NewMeshPart(mesh, lodIndex, i);
                    Parts.Data[part.Index] = parts[i];
                }
            }

            return lod;
        }

        static void StoreLodMeshSections(MeshId mesh, int lodIndex, ref MyMeshSectionInfo1[] sections)
        {
            if (sections != null)
            {
                for (int i = 0; i < sections.Length; i++)
                {
                    var sectionId = NewMeshSection(mesh, lodIndex, i);
                    Sections.Data[sectionId.Index] = sections[i];
                }
            }
        }

        // 1 lod, n parts
        internal static MeshId CreateRuntimeMesh(MyStringId nameKey, int parts, bool dynamic)
        {
            Debug.Assert(!RuntimeMeshNameIndex.ContainsKey(nameKey));

            var id = new MeshId { Index = MeshInfos.Allocate() };
            RuntimeMeshNameIndex[nameKey] = id;

            MeshInfos.Data[id.Index] = new MyMeshInfo
            {
                Name = nameKey.ToString(),
                NameKey = nameKey,
                LodsNum = 1,
                Dynamic = dynamic,
                RuntimeGenerated = true
            };

            MyLodMeshInfo lodInfo = new MyLodMeshInfo { PartsNum = parts };
            MyMeshPartInfo1[] partsInfo = new MyMeshPartInfo1[parts];
            StoreLodMeshWithParts(id, 0, ref lodInfo, ref partsInfo);

            return id;
        }

        internal static void RefreshMaterialIds(MeshId mesh)
        {
            var sections = InterSessionData[mesh].Sections;
            var lod = LodMeshIndex[new MyLodMesh { Mesh = mesh, Lod = 0 }];

            for (int i = 0; i < sections.Length; i++)
            {
                var part = PartIndex[new MyMeshPart { Mesh = mesh, Lod = 0, Part = i }];
                Parts.Data[part.Index].Material = MyMeshMaterials1.GetMaterialId(sections[i].MaterialName);
            }
        }

        internal static void UpdateRuntimeMesh(
            MeshId mesh,
            ushort[] indices,
            MyVertexFormatPositionH4[] stream0,
            MyVertexFormatTexcoordNormalTangent[] stream1,
            MySectionInfo[] sections,
            BoundingBox aabb)
        {
            // get mesh lod 0

            InterSessionData[mesh] = new MyRuntimeMeshPersistentInfo { Sections = sections };

            var lodMeshId = LodMeshIndex[new MyLodMesh { Mesh = mesh, Lod = 0 }];

            Debug.Assert(stream0.LongLength == stream1.LongLength);
            long vertexCount = stream0.LongLength;
            long indexCount = indices.LongLength;

            Debug.Assert(vertexCount <= int.MaxValue && indexCount <= int.MaxValue);

            FillIndexData(ref LodMeshInfos.Data[lodMeshId.Index].Data, indices, (int)indexCount);
            FillStream0Data(ref LodMeshInfos.Data[lodMeshId.Index].Data, stream0, (int)vertexCount);
            FillStream1Data(ref LodMeshInfos.Data[lodMeshId.Index].Data, stream1, (int)vertexCount);

            LodMeshInfos.Data[lodMeshId.Index].BoundingBox = aabb;

            const int vertexComponentCount = 4;
            var vertexComponents = new List<MyVertexInputComponent>(vertexComponentCount);
            vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.POSITION_PACKED));
            vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.NORMAL, 1));
            vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.TANGENT_SIGN_OF_BITANGENT, 1));
            vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.TEXCOORD0_H, 1));

            Debug.Assert(vertexComponents.Count == vertexComponents.Capacity);
            vertexComponents.Capacity = vertexComponents.Count;
            LodMeshInfos.Data[lodMeshId.Index].Data.VertexLayout = MyVertexLayouts.GetLayout(vertexComponents.GetInternalArray());

            for (int i = 0; i < sections.Length; i++)
            {
                var part = PartIndex[new MyMeshPart { Mesh = mesh, Lod = 0, Part = i }];
                Parts.Data[part.Index].StartIndex = sections[i].IndexStart;
                Parts.Data[part.Index].IndexCount = sections[i].TriCount * 3;
                Parts.Data[part.Index].Material = MyMeshMaterials1.GetMaterialId(sections[i].MaterialName);
            }

            LodMeshInfos.Data[lodMeshId.Index].IndicesNum = (int)indexCount;
            LodMeshInfos.Data[lodMeshId.Index].VerticesNum = (int)vertexCount;

            //if (mesh.Info.Dynamic && LodMeshInfos.Data[lod.Index].VerticesNum)
            //{
            //    UpdateData(lod);
            //}
            if (LodMeshInfos.Data[lodMeshId.Index].VerticesNum > 0)
            {
                MoveData(lodMeshId);
            }
        }

        unsafe static void MoveData(LodMeshId lodMeshId)
        {
            var info = LodMeshInfos.Data[lodMeshId.Index];
            if (info.NullLodMesh)
                return;

            DisposeLodMeshBuffers(lodMeshId);

            MoveData(lodMeshId.Info.VerticesNum, lodMeshId.Info.IndicesNum, ref LodMeshInfos.Data[lodMeshId.Index].Data, ref LodMeshBuffers[lodMeshId.Index]);
        }

        unsafe static void MoveData(MyMergedLodMeshId mergedLodMeshId)
        {
            var info = MergedLodMeshInfos.Data[mergedLodMeshId.Index];
            if (info.NullMesh)
                return;

            DisposeMergedLodMeshBuffers(mergedLodMeshId);
            MoveData(mergedLodMeshId.Info.VerticesNum, mergedLodMeshId.Info.IndicesNum, ref MergedLodMeshInfos.Data[mergedLodMeshId.Index].Data, ref MergedLodMeshBuffers[mergedLodMeshId.Index]);
        }

        unsafe static void MoveData(int vertexCount, int indexCount, ref MyMeshRawData rawData, ref MyMeshBuffers meshBuffer)
        {
            fixed (void* ptr = rawData.VertexStream0)
            {
                meshBuffer.VB0 = MyHwBuffers.CreateVertexBuffer(vertexCount, rawData.Stride0, BindFlags.VertexBuffer, ResourceUsage.Immutable, new IntPtr(ptr), "vb 0");
            }
            if (rawData.Stride1 > 0)
            {
                fixed (void* ptr = rawData.VertexStream1)
                {
                    meshBuffer.VB1 = MyHwBuffers.CreateVertexBuffer(vertexCount, rawData.Stride1, BindFlags.VertexBuffer, ResourceUsage.Immutable, new IntPtr(ptr), "vb 1");
                }
            }
            fixed (void* ptr = rawData.Indices)
            {
                meshBuffer.IB = MyHwBuffers.CreateIndexBuffer(indexCount, rawData.IndicesFmt, BindFlags.IndexBuffer, ResourceUsage.Immutable, new IntPtr(ptr), "ib");
            }
        }

        internal static MeshId CreateMergedVoxelCell(Vector3I coordinates, int lod)
        {
            var meshId = new MeshId { Index = MeshInfos.Allocate() };

            MyVoxelCellInfo info = new MyVoxelCellInfo { Coord = coordinates, Lod = lod };
            MeshVoxelInfo[meshId] = info;

            MeshInfos.Data[meshId.Index] = new MyMeshInfo
            {
                Name = String.Format("MergedVoxelCell {0} Lod {1}", coordinates, lod),
                NameKey = MyStringId.NullOrEmpty,
                LodsNum = 1,
                Dynamic = false,
                RuntimeGenerated = true,
            };

            MyMergedLodMeshId mergedLodMesh = NewMergedLodMesh(meshId);
            MergedMeshVoxelInfo[mergedLodMesh] = info;

            MergedLodMeshInfos.Data[mergedLodMesh.Index].Data.VertexLayout = VoxelLayout;

            return meshId;
        }

        internal static bool CanStartMerge(MeshId mergedMeshId, int pendingThreshold)
        {
            MyMergedLodMesh mergedMesh = new MyMergedLodMesh { Mesh = mergedMeshId, Lod = 0 };
            MyMergedLodMeshId mergedLodMeshId = MergedLodMeshIndex[mergedMesh];

            return mergedLodMeshId.CanStartMerge(pendingThreshold);
        }

        internal static bool TryStartMerge(MeshId mergedMeshId, uint clipmapId, int pendingThreshold, List<LodMeshId> outLodMeshesSent, ulong workId)
        {
            MyMergedLodMeshId mergedLodMeshId = MergedLodMeshIndex[new MyMergedLodMesh { Mesh = mergedMeshId, Lod = 0 }];
            return mergedLodMeshId.TryStartMerge(clipmapId, pendingThreshold, outLodMeshesSent, workId);
        }

        internal static bool UpdateMergedVoxelCell(MeshId mesh, ref MyClipmapCellMeshMetadata metadata, List<MyClipmapCellBatch> batches)
        {
            ProfilerShort.Begin("MyMeshes.UpdateMergedVoxelCell");
            var mergedLodMesh = new MyMergedLodMesh { Mesh = mesh, Lod = 0 };

            MyMergedLodMeshId mergedLodMeshId;
            if (!MergedLodMeshIndex.TryGetValue(mergedLodMesh, out mergedLodMeshId))
            {
                Debug.Fail("Merged lod mesh not found!");
                ProfilerShort.End();
                return false;
            }

            // We'd immediately need to recalculate, don't bother
            if (mergedLodMeshId.Info.PendingLodMeshes.Count > 0)
            {
                ProfilerShort.End();
                return false;
            }

            MyVoxelCellInfo info = new MyVoxelCellInfo { Coord = metadata.Cell.CoordInLod, Lod = metadata.Cell.Lod };
            MeshVoxelInfo[mesh] = info;
            MergedMeshVoxelInfo[mergedLodMeshId] = info;

            ResizeVoxelParts(mesh, mergedLodMeshId, batches.Count);

            long vertexCount, indexCount;
            CalculateRequiredBufferCapacities(batches, out vertexCount, out indexCount);

            if (vertexCount <= ushort.MaxValue)
            {
                ProfilerShort.Begin("MyMeshes.CombineBatchesToParts");
                CombineBatchesToParts(mesh, batches, ref m_tmpVertices0, ref m_tmpVertices1, ref m_tmpShortIndices, vertexCount, indexCount);
                ProfilerShort.BeginNextBlock("MyMeshes.FillMeshRawData");
                FillMeshRawData(ref MergedLodMeshInfos.Data[mergedLodMeshId.Index].Data, m_tmpVertices0, m_tmpVertices1, m_tmpShortIndices, (int)vertexCount, (int)indexCount);
            }
            else if (vertexCount <= uint.MaxValue)
            {
                ProfilerShort.Begin("MyMeshes.CombineBatchesToParts");
                CombineBatchesToParts(mesh, batches, ref m_tmpVertices0, ref m_tmpVertices1, ref m_tmpIndices, vertexCount, indexCount);
                ProfilerShort.BeginNextBlock("MyMeshes.FillMeshRawData");
                FillMeshRawData(ref MergedLodMeshInfos.Data[mergedLodMeshId.Index].Data, m_tmpVertices0, m_tmpVertices1, m_tmpIndices, (int)vertexCount, (int)indexCount);
            }
            else
                Debug.Fail("Index overflow");

            MergedLodMeshInfos.Data[mergedLodMeshId.Index].VerticesNum = (int)vertexCount;
            MergedLodMeshInfos.Data[mergedLodMeshId.Index].IndicesNum = (int)indexCount;

            ProfilerShort.BeginNextBlock("MyMeshes.MoveData");
            MoveData(mergedLodMeshId);
            ProfilerShort.End();


            ProfilerShort.End();
            return true;
        }

        internal static MeshId CreateVoxelCell(Vector3I coord, int lod)
        {
            var id = new MeshId { Index = MeshInfos.Allocate() };
            MeshVoxelInfo[id] = new MyVoxelCellInfo { Coord = coord, Lod = lod };

            MeshInfos.Data[id.Index] = new MyMeshInfo
            {
                Name = String.Format("VoxelCell {0} Lod {1}", coord, lod),
                NameKey = MyStringId.NullOrEmpty,
                LodsNum = 1,
                Dynamic = false,
                RuntimeGenerated = true
            };

            MyLodMeshInfo lodInfo = new MyLodMeshInfo { PartsNum = 0 };

            // just dummy for function, we use different parts for cells
            MyMeshPartInfo1[] partsInfo = new MyMeshPartInfo1[0];
            var lodId = StoreLodMeshWithParts(id, lod, ref lodInfo, ref partsInfo);

            LodMeshInfos.Data[lodId.Index].Data.VertexLayout = VoxelLayout;

            return id;
        }

        internal static void UpdateVoxelCell(MeshId mesh, MyClipmapCellMeshMetadata metadata, List<MyClipmapCellBatch> batches)
        {
            var lodMesh = new MyLodMesh { Mesh = mesh, Lod = 0 };

            LodMeshId lodMeshId;
            if (!LodMeshIndex.TryGetValue(lodMesh, out lodMeshId))
            {
                Debug.Fail("Lod mesh not found!");
                return;
            }

            ResizeVoxelParts(mesh, lodMeshId, batches.Count);

            long vertexCount, indexCount;
            CalculateRequiredBufferCapacities(batches, out vertexCount, out indexCount);

            if (vertexCount <= ushort.MaxValue)
            {
                CombineBatchesToParts(mesh, batches, ref m_tmpVertices0, ref m_tmpVertices1, ref m_tmpShortIndices, vertexCount, indexCount);
                FillMeshRawData(ref LodMeshInfos.Data[lodMeshId.Index].Data, m_tmpVertices0, m_tmpVertices1, m_tmpShortIndices, (int)vertexCount, (int)indexCount);
            }
            else if (vertexCount <= int.MaxValue)
            {
                CombineBatchesToParts(mesh, batches, ref m_tmpVertices0, ref m_tmpVertices1, ref m_tmpIndices, vertexCount, indexCount);
                FillMeshRawData(ref LodMeshInfos.Data[lodMeshId.Index].Data, m_tmpVertices0, m_tmpVertices1, m_tmpIndices, (int)vertexCount, (int)indexCount);
            }
            else
                Debug.Fail("Index overflow");

            LodMeshInfos.Data[lodMeshId.Index].VerticesNum = (int)vertexCount;
            LodMeshInfos.Data[lodMeshId.Index].IndicesNum = (int)indexCount;
            LodMeshInfos.Data[lodMeshId.Index].BatchMetadata = metadata;

            for (int batchIndex = 0; batchIndex < batches.Count; ++batchIndex)
                LodMeshInfos.Data[lodMeshId.Index].DataBatches[batchIndex] = batches[batchIndex];

            MoveData(lodMeshId);
        }

        private static void CalculateRequiredBufferCapacities(List<MyClipmapCellBatch> batches, out long vertexCapacity, out long indexCapacity)
        {
            vertexCapacity = 0;
            indexCapacity = 0;

            for (int batchIndex = 0; batchIndex < batches.Count; ++batchIndex)
            {
                vertexCapacity += batches[batchIndex].Vertices.Length;
                indexCapacity += batches[batchIndex].Indices.Length;
            }
        }

        private struct MyVertexCopyHelper
        {
            public ulong LowBits;
            public ulong HighBits;

            public static MyVertexCopyHelper operator+(MyVertexCopyHelper left, MyVertexCopyHelper right)   // Don't care about overflows as they don't happen in our use case
            {
                return new MyVertexCopyHelper { LowBits = left.LowBits + right.LowBits, HighBits = left.HighBits + right.HighBits };
            }
        }

        private unsafe static void CopyVertices(MyVertexFormatVoxelSingleData* sourcePointer, MyVertexFormatVoxel* destinationPointer0, MyVertexFormatNormal* destinationPointer1, uint elementsToCopy)
        {
            MyVertexFormatVoxel* currentDestination0 = destinationPointer0;
            MyVertexFormatNormal* currentDestination1 = destinationPointer1;
            
            const ulong ValueToAdd = (((ulong)short.MaxValue) << 0) + (((ulong)short.MaxValue) << 16) + (((ulong)short.MaxValue) << 32);
            MyVertexCopyHelper ValueToAdd128 = new MyVertexCopyHelper { LowBits = ValueToAdd, HighBits = ValueToAdd };
            for (int batchVertexIndex = 0; batchVertexIndex < elementsToCopy; ++batchVertexIndex)
            {
                MyVertexFormatVoxelSingleData* batchVertex = sourcePointer + batchVertexIndex;

                *((MyVertexCopyHelper*)currentDestination0) = (*((MyVertexCopyHelper*)batchVertex) + ValueToAdd128);
                *((ulong*)currentDestination1) = *((ulong*)batchVertex + 2);

                //currentDestination0->Position = batchVertex->Position;
                //currentDestination0->PositionMorph = batchVertex->PositionMorph;
                //currentDestination0->m_positionMaterials.W = (ushort)batchVertex->PackedPositionAndAmbientMaterial.W;
                //currentDestination0->m_positionMaterialsMorph.W = (ushort)batchVertex->PackedPositionAndAmbientMaterialMorph.W;

                //currentDestination1->Normal = batchVertex->PackedNormal;
                //currentDestination1->NormalMorph = batchVertex->PackedNormalMorph;

                ++currentDestination0;
                ++currentDestination1;
            }
        }

        private unsafe static void CopyIndices(uint* sourcePointer, void* destinationPointer, int startIndex, int baseVertex, int destinationIndexStride, uint elementsToCopy)
        {
            switch (destinationIndexStride)
            {
                case 2:
                    ushort* shortIndices = ((ushort*)destinationPointer)+startIndex;
                    ushort* endIndexPointer = shortIndices + elementsToCopy;

                    while (shortIndices <= endIndexPointer)
                    {
                        *shortIndices = (ushort)(*(sourcePointer++) + baseVertex);
                        ++shortIndices;
                    }
                    break;

                case 4:
                    uint* intIndices = ((uint*)destinationPointer)+startIndex;
                    uint* endIntIndexPointer = intIndices + elementsToCopy;

                    while (intIndices <= endIntIndexPointer)
                    {
                        *intIndices = (uint)(*(sourcePointer++) + baseVertex);
                        ++intIndices;
                    }

                    //SharpDX.Utilities.CopyMemory(new IntPtr(intIndices), new IntPtr(sourcePointer), (int)(elementsToCopy * destinationIndexStride));
                    break;

                default:
                    Debug.Fail("Incorrect parameter");
                    break;
            }
        }

        // ushort overload
        private unsafe static void CombineBatchesToParts(
            MeshId mesh,
            List<MyClipmapCellBatch> batches,
            ref MyVertexFormatVoxel[] vertices0,
            ref MyVertexFormatNormal[] vertices1,
            ref ushort[] indices,
            long vertexCount,
            long indexCount)
        {
            MyArrayHelpers.InitOrReserveNoCopy(ref vertices0, (int)vertexCount);
            MyArrayHelpers.InitOrReserveNoCopy(ref vertices1, (int)vertexCount);
            MyArrayHelpers.InitOrReserveNoCopy(ref indices, (int)indexCount);

            fixed (MyVertexFormatVoxel* vertexPointer0 = vertices0)
            {
                fixed (MyVertexFormatNormal* vertexPointer1 = vertices1)
                {
                    fixed (void* indexPointer = indices)
                    {
                        CombineBatchesToParts(mesh, batches, vertexPointer0, vertexPointer1, indexPointer, sizeof(ushort));
                    }
                }
            }
        }

        // uint overload
        private unsafe static void CombineBatchesToParts(
            MeshId mesh,
            List<MyClipmapCellBatch> batches,
            ref MyVertexFormatVoxel[] vertices0,
            ref MyVertexFormatNormal[] vertices1,
            ref uint[] indices,
            long vertexCount,
            long indexCount)
        {
            MyArrayHelpers.InitOrReserveNoCopy(ref vertices0, (int)vertexCount);
            MyArrayHelpers.InitOrReserveNoCopy(ref vertices1, (int)vertexCount);
            MyArrayHelpers.InitOrReserveNoCopy(ref indices, (int)indexCount);

            fixed (MyVertexFormatVoxel* vertexPointer0 = vertices0)
            {
                fixed (MyVertexFormatNormal* vertexPointer1 = vertices1)
                {
                    fixed (void* indexPointer = indices)
                    {
                        CombineBatchesToParts(mesh, batches, vertexPointer0, vertexPointer1, indexPointer, sizeof(uint));
                    }
                }
            }
        }

        // Overload that does the actual work
        private unsafe static void CombineBatchesToParts(
            MeshId mesh,
            List<MyClipmapCellBatch> batches,
            MyVertexFormatVoxel* vertices0,
            MyVertexFormatNormal* vertices1,
            void* indices,
            int indexStride)
        {
            int batchCount = batches.Count;

            int startIndex = 0;
            int baseVertex = 0;

            for (int batchIndex = 0; batchIndex < batchCount; ++batchIndex)
            {
                var batchVertices = batches[batchIndex].Vertices;
                var batchIndices = batches[batchIndex].Indices;
                var batchMaterial = new MyVoxelMaterialTriple(batches[batchIndex].Material0, batches[batchIndex].Material1, batches[batchIndex].Material2);

                fixed (MyVertexFormatVoxelSingleData* sourcePointer = batchVertices)
                {
                    MyVertexFormatVoxel* destinationPointer0 = vertices0 + baseVertex;
                    MyVertexFormatNormal* destinationPointer1 = vertices1 + baseVertex;
                    CopyVertices(sourcePointer, destinationPointer0, destinationPointer1, (uint)batchVertices.LongLength);
                }

                fixed (uint* batchIndexPointer = batchIndices)
                {
                    CopyIndices(batchIndexPointer, indices, startIndex, baseVertex, indexStride, (uint)batchIndices.LongLength);
                }

                VoxelPartId id = VoxelPartIndex[new MyMeshPart { Mesh = mesh, Lod = 0, Part = batchIndex }];
                VoxelParts.Data[id.Index] = new MyVoxelPartInfo1
                {
                    IndexCount = batches[batchIndex].Indices.Length,
                    StartIndex = startIndex,
                    BaseVertex = 0,
                    MaterialTriple = batchMaterial
                };

                baseVertex += batchVertices.Length;
                startIndex += batchIndices.Length;
            }
        }


        public static void UnloadVoxelCell(MeshId mesh)
        {
            var lodMesh = new MyLodMesh { Mesh = mesh, Lod = 0 };
            var lodMeshId = LodMeshIndex[lodMesh];

            var info = LodMeshInfos.Data[lodMeshId.Index];
            if (info.NullLodMesh)
                return;

            DisposeLodMeshBuffers(lodMeshId);
        }

        public static unsafe void ReloadVoxelCell(MeshId mesh)
        {
            var lodMesh = new MyLodMesh { Mesh = mesh, Lod = 0 };
            var lodMeshId = LodMeshIndex[lodMesh];

            var info = LodMeshInfos.Data[lodMeshId.Index];
            if (info.NullLodMesh)
                return;

            var data = LodMeshInfos.Data[lodMeshId.Index].Data;
            var verticesNum = lodMeshId.Info.VerticesNum;

            if (LodMeshBuffers[lodMeshId.Index].VB0 == VertexBufferId.NULL)
                MoveData(verticesNum, lodMeshId.Info.IndicesNum, ref data, ref LodMeshBuffers[lodMeshId.Index]);
        }

        private static void FillMeshRawData(ref MyMeshRawData meshRawData, List<MyClipmapCellBatch> batches)
        {
            long vertexCount, indexCount;
            CalculateRequiredBufferCapacities(batches, out vertexCount, out indexCount);
            
        }

        private static void FillMeshRawData(ref MyMeshRawData meshRawData, MyVertexFormatVoxel[] vertices0, MyVertexFormatNormal[] vertices1, ushort[] indices, int vertexCapacity, int indexCapacity)
        {
            FillStream0Data(ref meshRawData, vertices0, vertexCapacity);
            FillStream1Data(ref meshRawData, vertices1, vertexCapacity);
            FillIndexData(ref meshRawData, indices, indexCapacity);
        }

        private static void FillMeshRawData(ref MyMeshRawData meshRawData, MyVertexFormatVoxel[] vertices0, MyVertexFormatNormal[] vertices1, uint[] indices, int vertexCapacity, int indexCapacity)
        {
            FillStream0Data(ref meshRawData, vertices0, vertexCapacity);
            FillStream1Data(ref meshRawData, vertices1, vertexCapacity);
            FillIndexData(ref meshRawData, indices, indexCapacity);
        }

        unsafe static void FillStreamData(ref byte[] destinationData, void* sourcePointer, int elementCount, int elementStride)
        {
            var byteSize = elementCount * elementStride;
            MyArrayHelpers.ResizeNoCopy(ref destinationData, byteSize);
            fixed (void* destinationPointer = destinationData)
            {
                SharpDX.Utilities.CopyMemory(new IntPtr(destinationPointer), new IntPtr(sourcePointer), byteSize);
            }
        }

        unsafe static void FillStream0Data(ref MyMeshRawData rawData, void* sourcePointer, int vertexCount, int vertexStride)
        {
            rawData.Stride0 = vertexStride;
            FillStreamData(ref rawData.VertexStream0, sourcePointer, vertexCount, vertexStride);
        }

        unsafe static void FillStream0Data(ref MyMeshRawData rawData, MyVertexFormatPositionH4[] vertices, int vertexCount)
        {
            fixed (void* sourcePointer = vertices)
            {
                FillStream0Data(ref rawData, sourcePointer, vertexCount, MyVertexFormatPositionH4.STRIDE);
            }
        }

        unsafe static void FillStream0Data(ref MyMeshRawData rawData, HalfVector4[] vertices, int vertexCount)
        {
            fixed (void* sourcePointer = vertices)
            {
                FillStream0Data(ref rawData, sourcePointer, vertexCount, sizeof(HalfVector4));
            }
        }

        unsafe static void FillStream0Data(ref MyMeshRawData rawData, MyVertexFormatPositionSkinning[] vertices, int vertexCount)
        {
            fixed (void* sourcePointer = vertices)
            {
                FillStream0Data(ref rawData, sourcePointer, vertexCount, MyVertexFormatPositionSkinning.STRIDE);
            }
        }

        unsafe static void FillStream0Data(ref MyMeshRawData rawData, MyVertexFormatVoxel[] vertices, int vertexCount)
        {
            fixed (void* sourcePointer = vertices)
            {
                FillStream0Data(ref rawData, sourcePointer, vertexCount, MyVertexFormatVoxel.STRIDE);
            }
        }

        unsafe static void FillStream1Data(ref MyMeshRawData rawData, void* sourcePointer, int vertexCount, int vertexStride)
        {
            rawData.Stride1 = vertexStride;
            FillStreamData(ref rawData.VertexStream1, sourcePointer, vertexCount, vertexStride);
        }

        unsafe static void FillStream1Data(ref MyMeshRawData rawData, MyVertexFormatTexcoordNormalTangent[] vertices, int vertexCount)
        {
            fixed (void* sourcePointer = vertices)
            {
                FillStream1Data(ref rawData, sourcePointer, vertexCount, MyVertexFormatTexcoordNormalTangent.STRIDE);
            }
        }

        unsafe static void FillStream1Data(ref MyMeshRawData rawData, MyVertexFormatNormal[] vertices, int vertexCount)
        {
            fixed (void* sourcePointer = vertices)
            {
                FillStream1Data(ref rawData, sourcePointer, vertexCount, MyVertexFormatNormal.STRIDE);
            }
        }

        unsafe static void FillIndexData(ref MyMeshRawData rawData, ushort[] indices, int indexCapacity)
        {
            rawData.IndicesFmt = Format.R16_UInt;
            fixed (void* sourceIndices = indices)
            {
                FillStreamData(ref rawData.Indices, sourceIndices, indexCapacity, FormatHelper.SizeOfInBytes(rawData.IndicesFmt));
            }
        }

        unsafe static void FillIndexData(ref MyMeshRawData rawData, uint[] indices, int indexCapacity)
        {
            rawData.IndicesFmt = Format.R32_UInt;
            fixed (void* sourceIndices = indices)
            {
                FillStreamData(ref rawData.Indices, sourceIndices, indexCapacity, FormatHelper.SizeOfInBytes(rawData.IndicesFmt));
            }
        }

        private static void ResizeVoxelParts(MeshId mesh, LodMeshId lod, int num)
        {
            int currentParts = LodMeshInfos.Data[lod.Index].PartsNum;

            // extend
            if (currentParts < num)
            {
                for (int i = currentParts; i < num; i++)
                {
                    var id = new VoxelPartId { Index = VoxelParts.Allocate() };
                    VoxelPartIndex[new MyMeshPart { Mesh = mesh, Lod = 0, Part = i }] = id;
                }
            }
            // drop
            else if (currentParts > num)
            {
                for (int i = num; i < currentParts; i++)
                {
                    var id = VoxelPartIndex[new MyMeshPart { Mesh = mesh, Lod = 0, Part = i }];
                    VoxelParts.Free(id.Index);
                    VoxelPartIndex.Remove(new MyMeshPart { Mesh = mesh, Lod = 0, Part = i });
                }
            }

            LodMeshInfos.Data[lod.Index].PartsNum = num;
            LodMeshInfos.Data[lod.Index].DataBatches = new MyClipmapCellBatch[num];
        }

        private static void ResizeVoxelParts(MeshId mesh, MyMergedLodMeshId mergedLodMeshId, int num)
        {
            int currentParts = MergedLodMeshInfos.Data[mergedLodMeshId.Index].PartsNum;

            // extend
            if (currentParts < num)
            {
                for (int i = currentParts; i < num; i++)
                {
                    var id = new VoxelPartId { Index = VoxelParts.Allocate() };
                    VoxelPartIndex[new MyMeshPart { Mesh = mesh, Lod = 0, Part = i }] = id;
                }
            }
            // drop
            else if (currentParts > num)
            {
                for (int i = num; i < currentParts; i++)
                {
                    var id = VoxelPartIndex[new MyMeshPart { Mesh = mesh, Lod = 0, Part = i }];
                    VoxelParts.Free(id.Index);
                    VoxelPartIndex.Remove(new MyMeshPart { Mesh = mesh, Lod = 0, Part = i });
                }
            }

            MergedLodMeshInfos.Data[mergedLodMeshId.Index].PartsNum = num;
        }

        private static void DisposeLodMeshBuffers(LodMeshId lodMeshId)
        {
            DisposeMeshBuffers(lodMeshId.Buffers);
            LodMeshBuffers[lodMeshId.Index] = MyMeshBuffers.Empty;
        }

        private static void DisposeMergedLodMeshBuffers(MyMergedLodMeshId mergedLodMeshId)
        {
            DisposeMeshBuffers(mergedLodMeshId.Buffers);
            MergedLodMeshBuffers[mergedLodMeshId.Index] = MyMeshBuffers.Empty;
        }

        private static void DisposeMeshBuffers(MyMeshBuffers buffers)
        {
            if (buffers.IB != IndexBufferId.NULL)
            {
                MyHwBuffers.Destroy(buffers.IB);
                buffers.IB = IndexBufferId.NULL;
            }

            if (buffers.VB0 != VertexBufferId.NULL)
            {
                MyHwBuffers.Destroy(buffers.VB0);
                buffers.VB0 = VertexBufferId.NULL;
            }

            if (buffers.VB1 != VertexBufferId.NULL)
            {
                MyHwBuffers.Destroy(buffers.VB1);
                buffers.VB1 = VertexBufferId.NULL;
            }
        }

        internal static void RemoveVoxelCell(MeshId id)
        {
            if (!MeshVoxelInfo.ContainsKey(id))
                return;

            bool isMergedMesh = IsMergedVoxelMesh(id);
            if (!isMergedMesh)
            {

                MyLodMesh lodMesh = new MyLodMesh { Mesh = id, Lod = 0 };
                if (LodMeshIndex.ContainsKey(lodMesh))
                {
                    var lodMeshId = LodMeshIndex[lodMesh];

                    bool isLodMerged = IsLodMeshMerged(lodMeshId);
                    if (isLodMerged)
                        LodMeshToMerged.Remove(lodMeshId);

                    ResizeVoxelParts(id, lodMeshId, 0);
                    DisposeLodMeshBuffers(lodMeshId);
                    LodMeshInfos.Data[lodMeshId.Index].Data = new MyMeshRawData();
                    LodMeshInfos.Free(lodMeshId.Index);

                    LodMeshIndex.Remove(lodMesh);
                }
            }
            else
            {
                MyMergedLodMesh mergedLodMesh = new MyMergedLodMesh { Mesh = id, Lod = 0 };
                if (MergedLodMeshIndex.ContainsKey(mergedLodMesh))
                {
                    var mergedMeshId = MergedLodMeshIndex[mergedLodMesh];

                    ResizeVoxelParts(id, mergedMeshId, 0);
                    DisposeMergedLodMeshBuffers(mergedMeshId);
                    MergedLodMeshInfos.Data[mergedMeshId.Index].PendingLodMeshes.Clear();
                    MergedLodMeshInfos.Data[mergedMeshId.Index].MergedLodMeshes.Clear();
                    MergedLodMeshInfos.Free(mergedMeshId.Index);

                    MergedLodMeshIndex.Remove(mergedLodMesh);
                    MergedMeshVoxelInfo.Remove(mergedMeshId);
                }
            }

            MeshInfos.Free(id.Index);
            MeshVoxelInfo.Remove(id);
        }

        const string ERROR_MODEL_PATH = "Models/Debug/Error.mwm";

        static void LoadMesh(MeshId id)
        {
            var assetName = MeshInfos.Data[id.Index].Name;

            MyLodMeshInfo meshMainLod = new MyLodMeshInfo
            {
                Name = assetName,
                FileName = assetName
            };

            MeshInfos.Data[id.Index].Loaded = true;


            MyMeshPartInfo1[] parts;
            MyMeshSectionInfo1[] sections;
            MyLODDescriptor[] lodDescriptors;

            bool modelOk = LoadMwm(ref meshMainLod, out parts, out sections, out lodDescriptors);

            if (!modelOk)
            {
                meshMainLod.FileName = ERROR_MODEL_PATH;

                if (!LoadMwm(ref meshMainLod, out parts, out sections, out lodDescriptors))
                {
                    Debug.Fail("error model missing");
                }
            }

            MeshInfos.Data[id.Index].FileExists = true;

            StoreLodMeshWithParts(id, 0, ref meshMainLod, ref parts);
            StoreLodMeshSections(id, 0, ref sections);

            int modelLods = 1;

            for (int i = 0; i < lodDescriptors.Length; i++)
            {
                var meshFile = lodDescriptors[i].Model;
                if (meshFile != null && !meshFile.EndsWith(".mwm"))
                {
                    meshFile += ".mwm";
                }

                MyLodMeshInfo lodMesh = new MyLodMeshInfo
                {
                    FileName = meshFile,
                    LodDistance = lodDescriptors[i].Distance,
                    NullLodMesh = meshFile == null,
                };

                MyMeshPartInfo1[] lodParts;
                MyMeshSectionInfo1[] lodSections;
                bool lodOk = LoadMwm(ref lodMesh, out lodParts, out lodSections);
                if (lodOk)
                {
                    //lodMesh.FileName = ERROR_MODEL_PATH;
                    //if(!LoadMwm(ref lodMesh, out lodParts))
                    //{
                    //    Debug.Fail("error model missing");
                    //}
                    StoreLodMeshWithParts(id, modelLods, ref lodMesh, ref lodParts);
                    StoreLodMeshSections(id, modelLods, ref lodSections);
                    modelLods++;
                }
            }

            MeshInfos.Data[id.Index].LodsNum = modelLods;

            for (int lodIndex = 0; lodIndex < modelLods; ++lodIndex)
            {
                MoveData(LodMeshIndex[new MyLodMesh { Mesh = id, Lod = lodIndex }]);
            }
        }

        internal static void Load()
        {
            foreach (var id in State[(int)MyMeshState.WAITING].ToList())
            {
                LoadMesh(id);
                MoveState(id, MyMeshState.WAITING, MyMeshState.LOADED);
            }
        }

        internal static void OnDeviceReset()
        {
            foreach (var lod in LodMeshIndex.Values)
            {
                MoveData(lod);
            }
        }
    }
}
