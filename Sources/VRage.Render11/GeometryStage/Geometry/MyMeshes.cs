using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using VRage;
using VRage.Import;
using VRage.Utils;
using VRageMath;
using VRageMath.PackedVector;
using VRageRender.Vertex;
using VRage.FileSystem;
using SharpDX.Direct3D11;
using VRage.Profiler;
using VRage.Render11.Common;
using VRage.Render11.Resources;
using VRage.Voxels;
using VRageRender.Import;
using VRageRender.Messages;

namespace VRageRender
{
    public class MyMeshData
    {
        public int VerticesNum { get { return Positions.Length; } }
        public bool ValidStreams { get; private set; }
        public bool IsAnimated { get; set; }
        public float PatternScale { get; private set; }


        public MyLODDescriptor[] Lods;

        public HalfVector4[] Positions;

        public Byte4[] Normals;
        public Byte4[] Tangents;
        public Byte4[] Bitangents;

        public HalfVector2[] Texcoords;
        public Vector4I[] TexIndices;

        public MyModelBone[] Bones;
        public Vector4I[] BoneIndices;
        public Vector4[] BoneWeights;

        public List<MyMeshPartInfo> PartInfos;
        public List<MyMeshSectionInfo> SectionInfos;

        public BoundingBox BoundindBox;
        public BoundingSphere BoundingSphere;


        #region Data not filled during import (filled from the outside)

        public List<uint> NewIndices;
        public uint MaxIndex;

        public Byte4[] StoredTangents;

        // Used to containt mapping from material name to PartIndex, Index offset of the part and BaseVertex of the part
        public Dictionary<string, Tuple<int, int, int>> MatsIndices;

        #endregion


        public void DoImport(MyModelImporter importer, string fsPath)
        {
            importer.ImportData(fsPath, new[]
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


            // Parts
            MyRenderProxy.Assert(tagData.ContainsKey(MyImporterConstants.TAG_MESH_PARTS));
            PartInfos = tagData[MyImporterConstants.TAG_MESH_PARTS] as List<MyMeshPartInfo>;

            // Sections
            if (tagData.ContainsKey(MyImporterConstants.TAG_MESH_SECTIONS))
                SectionInfos = tagData[MyImporterConstants.TAG_MESH_SECTIONS] as List<MyMeshSectionInfo>;

            // Common buffers
            if (tagData.ContainsKey(MyImporterConstants.TAG_LODS))
                Lods = (MyLODDescriptor[])tagData[MyImporterConstants.TAG_LODS];

            Positions = (HalfVector4[])tagData[MyImporterConstants.TAG_VERTICES];

            Normals = (Byte4[])tagData[MyImporterConstants.TAG_NORMALS];
            Tangents = (Byte4[])tagData[MyImporterConstants.TAG_TANGENTS];
            Bitangents = (Byte4[])tagData[MyImporterConstants.TAG_BINORMALS];

            Texcoords = (HalfVector2[])tagData[MyImporterConstants.TAG_TEXCOORDS0];
            TexIndices = MyManagers.GeometryTextureSystem.CreateTextureIndices(PartInfos, VerticesNum, MyMeshes.GetContentPath(fsPath));

            BoneIndices = (Vector4I[])tagData[MyImporterConstants.TAG_BLENDINDICES];
            BoneWeights = (Vector4[])tagData[MyImporterConstants.TAG_BLENDWEIGHTS];

            // Validation
            ValidStreams =
                ((Normals.Length > 0) && Normals.Length == VerticesNum) &&
                ((Texcoords.Length > 0) && Texcoords.Length == VerticesNum) &&
                ((Tangents.Length > 0) && Tangents.Length == VerticesNum) &&
                ((Bitangents.Length > 0) && Bitangents.Length == VerticesNum);

            if (ValidStreams == false)
            {
                MyRender11.Log.WriteLine(String.Format("Mesh asset {0} has inconsistent vertex streams", Path.GetFileName(fsPath)));
                Normals = new Byte4[0];
                Texcoords = new HalfVector2[0];
                Tangents = new Byte4[0];
                Bitangents = new Byte4[0];
            }

            // Scale
            object patternScale;
            PatternScale = 1f;
            if (tagData.TryGetValue(MyImporterConstants.TAG_PATTERN_SCALE, out patternScale))
                PatternScale = (float)patternScale;

            // Animations
            IsAnimated = BoneIndices.Length > 0 && BoneWeights.Length > 0 && BoneIndices.Length == VerticesNum &&
                              BoneWeights.Length == VerticesNum;
            Bones = (MyModelBone[])tagData[MyImporterConstants.TAG_BONES];

            BoundindBox = (BoundingBox)tagData[MyImporterConstants.TAG_BOUNDING_BOX];
            BoundingSphere = (BoundingSphere)tagData[MyImporterConstants.TAG_BOUNDING_SPHERE];

        }
    }

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
        // Materials that are not used after part merging (they are replaced by the new Material)
        public HashSet<string> UnusedMaterials;
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
        internal MyIndexBufferFormat IndicesFmt;

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
        internal string[] SectionNames;
        internal bool HasBones { get { return Data.VertexLayout.Info.HasBonesInfo; } }

        internal int VerticesNum;
        internal int IndicesNum;
        internal int TrianglesNum;

        /// <summary>Triangle density in the squared diagonal</summary>
        internal float TriangleDensity;

        internal float LodDistance;

        internal MyMeshRawData Data;
        internal MyClipmapCellBatch[] DataBatches;
        internal MyClipmapCellMeshMetadata BatchMetadata;

        internal BoundingBox? BoundingBox;
        internal BoundingSphere? BoundingSphere;
        internal bool NullLodMesh;
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
        public float Rescale;
    }


    struct MyMeshBuffers
    {
        internal IVertexBuffer VB0;
        internal IVertexBuffer VB1;
        internal IIndexBuffer IB;

        public static bool operator ==(MyMeshBuffers left, MyMeshBuffers right)
        {
            return left.VB0 == right.VB0 && left.VB1 == right.VB1 && left.IB == right.IB;
        }

        public static bool operator !=(MyMeshBuffers left, MyMeshBuffers right)
        {
            return left.VB0 != right.VB0 || left.VB1 != right.VB1 || left.IB == right.IB;
        }

        internal static readonly MyMeshBuffers Empty = new MyMeshBuffers();
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
        internal string Section;

        #region Equals
        public class MyMeshSectionComparerType : IEqualityComparer<MyMeshSection>
        {
            public bool Equals(MyMeshSection left, MyMeshSection right)
            {
                return left.Mesh == right.Mesh &&
                        left.Lod == right.Lod &&
                        left.Section == right.Section;
            }

            public int GetHashCode(MyMeshSection section)
            {
                return section.Mesh.GetHashCode() << 20 | section.Lod << 10 | section.Section.GetHashCode();
            }
        }
        public static readonly MyMeshSectionComparerType Comparer = new MyMeshSectionComparerType();

        #endregion
    }

    // fractures are the only asset existing over sessions (performance reasons) and some parts need to be recreated after they get dropped on session end (like material ids)
    struct MyRuntimeMeshPersistentInfo
    {
        internal MyRuntimeSectionInfo[] Sections;
    }

    static class MyMeshes
    {
        static readonly Dictionary<MyStringId, MeshId> MeshNameIndex = new Dictionary<MyStringId, MeshId>(MyStringId.Comparer);
        static readonly Dictionary<MyStringId, MeshId> RuntimeMeshNameIndex = new Dictionary<MyStringId, MeshId>(MyStringId.Comparer);

        internal static readonly MyFreelist<MyMeshInfo> MeshInfos = new MyFreelist<MyMeshInfo>(4096);

        internal static readonly MyFreelist<MyLodMeshInfo> LodMeshInfos = new MyFreelist<MyLodMeshInfo>(4096);

        static readonly Dictionary<MyLodMesh, LodMeshId> LodMeshIndex =
            new Dictionary<MyLodMesh, LodMeshId>(MyLodMesh.Comparer);

        internal static MyMeshBuffers[] LodMeshBuffers = new MyMeshBuffers[4096];

        internal static readonly MyFreelist<MyMeshPartInfo1> Parts = new MyFreelist<MyMeshPartInfo1>(8192);
        internal static readonly MyFreelist<MyMeshSectionInfo1> Sections = new MyFreelist<MyMeshSectionInfo1>(8192);

        static readonly Dictionary<MeshId, MyVoxelCellInfo> MeshVoxelInfo =
            new Dictionary<MeshId, MyVoxelCellInfo>(MeshId.Comparer);

        static readonly Dictionary<MyMeshPart, VoxelPartId> VoxelPartIndex =
            new Dictionary<MyMeshPart, VoxelPartId>(MyMeshPart.Comparer);

        static readonly Dictionary<MyMeshPart, MeshPartId> PartIndex =
            new Dictionary<MyMeshPart, MeshPartId>(MyMeshPart.Comparer);

        static readonly Dictionary<MyMeshSection, MeshSectionId> SectionIndex =
            new Dictionary<MyMeshSection, MeshSectionId>(MyMeshSection.Comparer);

        internal static readonly MyFreelist<MyVoxelPartInfo1> VoxelParts = new MyFreelist<MyVoxelPartInfo1>(2048);

        static readonly Dictionary<MeshId, MyRuntimeMeshPersistentInfo> InterSessionData =
            new Dictionary<MeshId, MyRuntimeMeshPersistentInfo>(MeshId.Comparer);

        //static readonly HashSet<MeshId> InterSessionDirty = new HashSet<MeshId>(MeshId.Comparer);

        static HashSet<MeshId>[] State;

        internal static VertexLayoutId VoxelLayout = VertexLayoutId.NULL;

        // Helpers to reduce allocations
        static MyVertexFormatVoxel[] m_tmpVertices0;
        static MyVertexFormatNormal[] m_tmpVertices1;
        static uint[] m_tmpIndices;
        static ushort[] m_tmpShortIndices;

        // Geometry merging helpers
        static readonly Dictionary<MyMeshMaterialId, List<int>> MergableParts = new Dictionary<MyMeshMaterialId, List<int>>(new MyMeshMaterialId.CustomMergingEqualityComparer());
        static readonly Dictionary<MyMeshMaterialId, List<int>> NonMergableParts = new Dictionary<MyMeshMaterialId, List<int>>();
        static readonly Dictionary<MyMeshMaterialId, List<int>> TempParts = new Dictionary<MyMeshMaterialId, List<int>>();
        static int m_mergedPartCounter;

        internal static void Init()
        {
            int statesNum = Enum.GetNames(typeof(MyMeshState)).Length;
            State = new HashSet<MeshId>[statesNum];
            for (int stateIndex = 0; stateIndex < statesNum; ++stateIndex)
            {
                State[stateIndex] = new HashSet<MeshId>(MeshId.Comparer);
            }

            for (int bufferIndex = 0; bufferIndex < LodMeshBuffers.Length; ++bufferIndex)
            {
                LodMeshBuffers[bufferIndex] = MyMeshBuffers.Empty;
            }

            VoxelLayout = MyVertexLayouts.GetLayout(
                new MyVertexInputComponent(MyVertexInputComponentType.VOXEL_POSITION_MAT),
                new MyVertexInputComponent(MyVertexInputComponentType.VOXEL_NORMAL, 1));

            m_mergedPartCounter = 0;
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

        internal static MyVoxelCellInfo GetVoxelInfo(MeshId mesh)
        {
            return MeshVoxelInfo[mesh];
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

        internal static MeshPartId GetMeshPart(MeshId mesh, int lod, int part)
        {
            return PartIndex[new MyMeshPart { Mesh = mesh, Lod = lod, Part = part }];
        }

        internal static bool TryGetMeshPart(MeshId mesh, int lod, int part, out MeshPartId partId)
        {
            return PartIndex.TryGetValue(new MyMeshPart { Mesh = mesh, Lod = lod, Part = part }, out partId);
        }

        internal static MeshSectionId GetMeshSection(MeshId mesh, int lod, string section)
        {
            return SectionIndex[new MyMeshSection { Mesh = mesh, Lod = lod, Section = section }];
        }

        internal static bool TryGetMeshSection(MeshId mesh, int lod, string section, out MeshSectionId sectionId)
        {
            return SectionIndex.TryGetValue(new MyMeshSection { Mesh = mesh, Lod = lod, Section = section }, out sectionId);
        }

        internal static VoxelPartId GetVoxelPart(MeshId mesh, int part)
        {
            return VoxelPartIndex[new MyMeshPart { Mesh = mesh, Lod = 0, Part = part }];
        }

        private static void InitState(MeshId id, MyMeshState state)
        {
            Debug.Assert(!CheckState(id, MyMeshState.LOADED));
            Debug.Assert(!CheckState(id, MyMeshState.WAITING));

            State[(int)state].Add(id);
        }

        private static void MoveState(MeshId id, MyMeshState from, MyMeshState to)
        {
            State[(int)from].Remove(id);
            State[(int)to].Add(id);
        }

        private static bool CheckState(MeshId id, MyMeshState state)
        {
            return State[(int)state].Contains(id);
        }

        internal static void ClearState(MeshId id)
        {
            foreach (HashSet<MeshId> t in State)
            {
                t.Remove(id);
            }
        }

        internal static MeshId GetMeshId(MyStringId nameKey, float rescale)
        {
            if (RuntimeMeshNameIndex.ContainsKey(nameKey))
            {
                var id = RuntimeMeshNameIndex[nameKey];

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
                    LodsNum = -1,
                    Rescale = rescale
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
            RuntimeMeshNameIndex.Clear();

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

        static MeshPartId NewMeshPart(MeshId mesh, int lod, int part)
        {
            var id = new MeshPartId { Index = Parts.Allocate() };
            Parts.Data[id.Index] = new MyMeshPartInfo1 { };

            PartIndex[new MyMeshPart { Mesh = mesh, Lod = lod, Part = part }] = id;

            return id;
        }

        static MeshSectionId NewMeshSection(MeshId mesh, int lod, string section)
        {
            var id = new MeshSectionId { Index = Sections.Allocate() };
            Sections.Data[id.Index] = new MyMeshSectionInfo1 { };

            SectionIndex[new MyMeshSection { Mesh = mesh, Lod = lod, Section = section }] = id;

            return id;
        }


        #region Mwm loading

        static bool LoadMwm(ref MyLodMeshInfo lodMeshInfo,
            out MyMeshPartInfo1[] parts, out MyMeshSectionInfo1[] sections, float rescale)
        {
            MyLODDescriptor[] lodDescriptors;
            return LoadMwm(ref lodMeshInfo, out parts, out sections, out lodDescriptors, rescale);
        }

        // returns false when mesh couldn't be loaded
        static bool LoadMwm(ref MyLodMeshInfo lodMeshInfo,
            out MyMeshPartInfo1[] parts, out MyMeshSectionInfo1[] sections,
            out MyLODDescriptor[] lodDescriptors, float rescale)
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

            //// Info extraction
            MyMeshData meshData = new MyMeshData();
            meshData.DoImport(importer, fsPath);

            // Return lods
            lodDescriptors = (MyLODDescriptor[])meshData.Lods.Clone();

            // Alter positions
            {
                var positions = meshData.Positions;

                if (rescale != 1.0f)
                    for (int i = 0; i < positions.Length; i++)
                        positions[i] = VF_Packer.PackPosition(VF_Packer.UnpackPosition(positions[i]) * rescale);

                var verticesNum = positions.Length;

                lodMeshInfo.VerticesNum = verticesNum;

                Debug.Assert(positions.Length > 0);
                if (positions.Length == 0)
                {
                    MyRender11.Log.WriteLine(String.Format("Mesh asset {0} has no vertices", file));
                    importer.Clear();
                    return false;
                }
            }

            // Alter tangents
            {
                var normals = meshData.Normals;
                var tangents = meshData.Tangents;
                var bitangents = meshData.Bitangents;

                var storedTangents = new Byte4[meshData.VerticesNum];
                meshData.StoredTangents = storedTangents;

                if (tangents.Length > 0 && bitangents.Length > 0)
                {
                    // calculate tangents used by run-time
                    for (int i = 0; i < meshData.VerticesNum; i++)
                    {
                        var N = VF_Packer.UnpackNormal(normals[i].PackedValue);
                        var T = VF_Packer.UnpackNormal(tangents[i].PackedValue);
                        var B = VF_Packer.UnpackNormal(bitangents[i].PackedValue);

                        var tanW = new Vector4(T.X, T.Y, T.Z, 0);
                        tanW.W = T.Cross(N).Dot(B) < 0 ? -1 : 1;

                        storedTangents[i] = VF_Packer.PackTangentSignB4(ref tanW);
                    }
                }
            }

            // Scale texcoords
            {
                var patternScale = meshData.PatternScale;
                var texcoords = meshData.Texcoords;

                if (patternScale != 1f && texcoords.Length > 0)
                    for (int i = 0; i < texcoords.Length; ++i)
                        texcoords[i] = new HalfVector2(texcoords[i].ToVector2() / patternScale);
            }

            // Mesh part info gathering and merging of parts
            CreatePartInfos(file, GetContentPath(file), meshData, ref lodMeshInfo, out parts, out sections);

            // Fill raw mesh data
            MyMeshRawData rawData;
            FillMeshData(meshData, ref lodMeshInfo, out rawData);

            // Set output mesh info
            lodMeshInfo.Data = rawData;
            lodMeshInfo.BoundingBox = meshData.BoundindBox;
            lodMeshInfo.BoundingSphere = meshData.BoundingSphere;

            var diagonal = meshData.BoundindBox.Size.Length();
            lodMeshInfo.TriangleDensity = lodMeshInfo.TrianglesNum / (diagonal * diagonal);

            importer.Clear();
            return true;
        }

        static void CreatePartInfos(
            string assetName,
            string contentPath,
            MyMeshData meshData,
            ref MyLodMeshInfo lodMeshInfo,
            out MyMeshPartInfo1[] parts,
            out MyMeshSectionInfo1[] sections)
        {
            var meshParts = meshData.PartInfos;

            // 1. Create a list of parts for each material
            // The comparer will ignore name of the material and compare only valid stuff for texture arrays
            MergableParts.Clear();
            NonMergableParts.Clear();
            TempParts.Clear();

            for (int partIndex = 0; partIndex < meshParts.Count; partIndex++)
            {
                MyMeshPartInfo meshPart = meshParts[partIndex];
                MyMaterialDescriptor materialDesc = meshPart.m_MaterialDesc;

                // Allocate material IDs for later reference
                if (materialDesc == null)
                    MyRender11.Log.WriteLine(String.Format("Mesh asset {0} has no material in part {1}", assetName, partIndex));
                else if (materialDesc.Facing == MyFacingEnum.Impostor)
                {
                    // Load a custom normalgloss texture
                    const string ngName = "NormalGlossTexture";
                    const string customNgName = "_normal_depth";

                    if (materialDesc.Textures.ContainsKey(ngName))
                        MyRender11.Log.WriteLine(string.Format("The impostor model {0} defines the normalGloss texture. Overwriting it with custom extension: {1}. Path to model: {2}", assetName, customNgName, contentPath));
                    else
                    {
                        const string cmName = "ColorMetalTexture";
                        Debug.Assert(materialDesc.Textures.ContainsKey(cmName));
                        string cmTexName = materialDesc.Textures[cmName];
                        string extension = Path.GetExtension(cmTexName);

                        Debug.Assert(!string.IsNullOrEmpty(extension), "Invalid texture extension");

                        materialDesc.Textures[ngName] =
                            cmTexName.Substring(0, cmTexName.Length - extension.Length)
                            + customNgName
                            + extension;
                    }
                }

                // Get material token
                MyMeshMaterialId matId = MyMeshMaterials1.NullMaterialId;
                bool isProcessedByGeometryTextureSystem = false;

                if (MyRender11.Settings.UseGeometryArrayTextures && materialDesc != null)
                {
                    MyMeshMaterialInfo meshMaterialInfo = MyMeshMaterials1.ConvertImportDescToMeshMaterialInfo(materialDesc, contentPath, assetName);
                    if (MyManagers.GeometryTextureSystem.IsMaterialAcceptableForTheSystem(meshMaterialInfo))
                    {
                        MyManagers.GeometryTextureSystem.ValidateMaterialTextures(meshMaterialInfo);
                        matId = MyManagers.GeometryTextureSystem.GetOrCreateMaterialId(meshMaterialInfo);
                        isProcessedByGeometryTextureSystem = true;
                    }
                }

                if (!isProcessedByGeometryTextureSystem)
                    matId = MyMeshMaterials1.GetMaterialId(materialDesc, contentPath, assetName);

                if (materialDesc != null)
                    Debug.Assert(matId.Info.Name == X.TEXT_(materialDesc.MaterialName));

                // Save the part for processing -- choose to try merge this part with others or not
                var targetDictionary = isProcessedByGeometryTextureSystem ? MergableParts : NonMergableParts;

                if (!targetDictionary.ContainsKey(matId))
                    targetDictionary[matId] = new List<int>();

                targetDictionary[matId].Add(partIndex);
            }

            // Transfer groups with just one part to the non-mergable dictionary
            // -- it uses simpler code and does not create new materials for merged parts
            foreach (var mergablePart in MergableParts)
                if (mergablePart.Value.Count == 1)
                    TempParts[mergablePart.Key] = mergablePart.Value;

            foreach (var tempPart in TempParts)
            {
                MergableParts.Remove(tempPart.Key);
                NonMergableParts[tempPart.Key] = tempPart.Value;
            }

            //foreach (var nonMergablePart in NonMergableParts)
            //    Debug.Assert(nonMergablePart.Value.Count == 1, "There are non-unique names for part materials.");

            // 2. Alter part data and create part infos
            // and create a mapping from material names to the indices of the original parts (matsIndices)
            meshData.MatsIndices = new Dictionary<string, Tuple<int, int, int>>();
            CreateMergedPartInfos(assetName, MergableParts, NonMergableParts, meshData, out parts);

            // 3. Create sections
            sections = null;

            if (meshData.SectionInfos != null && meshData.SectionInfos.Count > 0)
                CreateSections(meshData, parts, assetName, out sections);

            // 4. Fill mesh info
            Debug.Assert(meshData.NewIndices != null);

            lodMeshInfo.PartsNum = parts.Length;

            if (meshData.SectionInfos != null && meshData.SectionInfos.Count > 0)
            {
                lodMeshInfo.SectionNames = new string[meshData.SectionInfos.Count];
                for (int i = 0; i < meshData.SectionInfos.Count; i++)
                    lodMeshInfo.SectionNames[i] = meshData.SectionInfos[i].Name;
            }
            else
                lodMeshInfo.SectionNames = new string[0];

            lodMeshInfo.IndicesNum = meshData.NewIndices.Count;
            lodMeshInfo.TrianglesNum = meshData.NewIndices.Count / 3;
        }

        static void CreateMergedPartInfos(
            string assetName,
            Dictionary<MyMeshMaterialId, List<int>> mergablePartGroups,
            Dictionary<MyMeshMaterialId, List<int>> nonMergablePartGroups,
            MyMeshData meshData,
            out MyMeshPartInfo1[] parts)
        {
            // 1. Prepare merged mesh parts
            meshData.NewIndices = new List<uint>();
            var outputPartsInternal = new List<MyMeshPartInfo1>();

            foreach (var mergableGroup in mergablePartGroups)
            {
                if (!AddMergableGroup(assetName, mergableGroup.Key, mergableGroup.Value, meshData, outputPartsInternal))
                {
                    // The group was not merged, add it to non-mergable parts
                    Debug.Assert(!nonMergablePartGroups.ContainsKey(mergableGroup.Key));
                    nonMergablePartGroups[mergableGroup.Key] = mergablePartGroups[mergableGroup.Key];
                }
            }

            // 2. Prepare parts that ought not to be merged
            foreach (var nonMergableGroup in nonMergablePartGroups)
            {
                if (!AddNonMergableGroup(assetName, nonMergableGroup.Key, nonMergableGroup.Value, meshData, outputPartsInternal))
                {
                    string error = string.Format("Could not load part '{0}' of model '{1}'.", nonMergableGroup.Key.Info.Name.ToString(), assetName);
                    Debug.Fail(error);
                    MyRender11.Log.WriteLine(error);
                }
            }

            parts = outputPartsInternal.ToArray(); // Out param
        }

        static bool AddMergableGroup(
            string assetName,
            MyMeshMaterialId groupMaterial,
            List<int> groupParts,
            MyMeshData meshData,
            List<MyMeshPartInfo1> outputParts)
        {
            var meshParts = meshData.PartInfos;

            bool doOffset = true;

            if (groupMaterial == MyMeshMaterials1.NullMaterialId)
                doOffset = false;
            else
            {
                MyFacingEnum facing = groupMaterial.Info.Facing;

                if (facing != MyFacingEnum.Full && facing != MyFacingEnum.Impostor)
                    doOffset = false;
            }

            // Merge part indices for parts that are in the same material group
            var mergedIndices = new List<int>();

            foreach (var partIndex in groupParts)
            {
                var part = meshParts[partIndex];
                mergedIndices.AddRange(part.m_indices);
            }

            // Prepare animations
            int[] bonesMapping = null;

            bool animationsSucceeded = true;

            if (meshData.IsAnimated)
                animationsSucceeded = CreatePartAnimations(mergedIndices, meshData, out bonesMapping);

            if (!animationsSucceeded)
            {
                // Too many bones for the merged part; try to load the original parts instead
                MyRender11.Log.WriteLine(
                    String.Format("Model asset {0} has more than {1} bones in parth with {2} material. Skipping part merging for this material.",
                        assetName, MyRender11Constants.SHADER_MAX_BONES, groupMaterial.Info.Name));

                return false;
            }

            // Prepare merged part
            var newMergedPartInfo = AppendPartIndices(meshData, mergedIndices, doOffset);
            newMergedPartInfo.BonesMapping = bonesMapping;

            // Generate a new material for the merged part -- Copy all contents to new matInfo and replace the name
            MyMeshMaterialInfo matInfo = groupMaterial.Info;
            matInfo.Name = X.TEXT_("Merged::" + m_mergedPartCounter);
            m_mergedPartCounter++;
            newMergedPartInfo.Material = MyMeshMaterials1.GetMaterialId(ref matInfo);

            // Record the original material names to know what materials are not used (debug reasons)
            newMergedPartInfo.UnusedMaterials = new HashSet<string>();

            foreach (var partIndex in groupParts)
            {
                string partMaterialName = meshParts[partIndex].m_MaterialDesc.MaterialName;
                MyMeshMaterialId partMaterialId = MyMeshMaterials1.GetMaterialId(partMaterialName);
                newMergedPartInfo.UnusedMaterials.Add(partMaterialId.Info.Name.String);
            }

            // Determine the start indices of the parts of this group; we iterate in the same order as the mergedIndices were filled
            int currentIndexOffset = meshData.NewIndices.Count - mergedIndices.Count;

            foreach (var partIndex in groupParts)
            {
                var part = meshParts[partIndex];

                if (part.m_MaterialDesc != null)
                    meshData.MatsIndices[part.m_MaterialDesc.MaterialName] =
                        new Tuple<int, int, int>(
                        // The index among the output parts
                            outputParts.Count,
                        // The corrected offset of the merged part after accounting for skipped parts
                            currentIndexOffset,
                            newMergedPartInfo.BaseVertex);

                currentIndexOffset += part.m_indices.Count;
            }

            outputParts.Add(newMergedPartInfo);

            return true;
        }

        static bool AddNonMergableGroup(
            string assetName,
            MyMeshMaterialId groupMaterial,
            List<int> groupParts,
            MyMeshData meshData,
            List<MyMeshPartInfo1> outputParts)
        {
            var meshParts = meshData.PartInfos;

            bool doOffset = true;

            if (groupMaterial == MyMeshMaterials1.NullMaterialId)
                doOffset = false;
            else
            {
                MyFacingEnum facing = groupMaterial.Info.Facing;

                if (facing != MyFacingEnum.Full && facing != MyFacingEnum.Impostor)
                    doOffset = false;
            }

            foreach (var partIndex in groupParts)
            {
                var part = meshParts[partIndex];
                var partIndices = part.m_indices;

                // Prepare animations
                int[] bonesMapping = null;

                bool animationsSucceeded = true;

                if (meshData.IsAnimated)
                    animationsSucceeded = CreatePartAnimations(partIndices, meshData, out bonesMapping);

                if (!animationsSucceeded)
                {
                    // This part has too many bones, disable animations for the whole model
                    var error = String.Format("Model asset {0} has more than {1} bones in parth with {2} material. "
                        + "Split model into more meshes or assign multiple materials to meshes."
                        + "Disabling animations for this model.",
                        assetName, MyRender11Constants.SHADER_MAX_BONES,
                        groupMaterial.Info.Name);

                    Debug.Fail(error);
                    MyRender11.Log.WriteLine(error);

                    meshData.BoneIndices = new Vector4I[0];
                    meshData.IsAnimated = false;
                }

                // Prepare merged part
                var newPartInfo = AppendPartIndices(meshData, partIndices, doOffset);
                newPartInfo.BonesMapping = bonesMapping;
                newPartInfo.Material = groupMaterial;

                // Correct offset info and add BaseVertex info 
                if (part.m_MaterialDesc != null)
                    meshData.MatsIndices[part.m_MaterialDesc.MaterialName] =
                        new Tuple<int, int, int>(
                        // The index among the output parts
                            outputParts.Count,
                        // The final offset of the current part
                            meshData.NewIndices.Count - part.m_indices.Count,
                            newPartInfo.BaseVertex);

                outputParts.Add(newPartInfo);
            }

            return true;
        }

        static bool CreatePartAnimations(
            List<int> partIndices,
            MyMeshData meshData,
            out int[] bonesMapping)
        {
            bonesMapping = null; // Out param

            MyModelBone[] bones = meshData.Bones;
            Vector4I[] boneIndices = meshData.BoneIndices;
            Vector4[] boneWeights = meshData.BoneWeights;

            // check animations, bones, prepare remapping
            if (boneIndices.Length <= 0 || bones.Length <= MyRender11Constants.SHADER_MAX_BONES)
                return true;

            Dictionary<int, int> bonesUsed = new Dictionary<int, int>();

            int trianglesNum = partIndices.Count / 3;
            for (int i = 0; i < trianglesNum; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    int index = partIndices[i * 3 + j];
                    Vector4 boneWeight = boneWeights[index];
                    Vector4I boneIndex = boneIndices[index];

                    if (boneWeight.X > 0)
                        bonesUsed[boneIndex.X] = 1;
                    if (boneWeight.Y > 0)
                        bonesUsed[boneIndex.Y] = 1;
                    if (boneWeight.Z > 0)
                        bonesUsed[boneIndex.Z] = 1;
                    if (boneWeight.W > 0)
                        bonesUsed[boneIndex.W] = 1;
                }
            }

            if (bonesUsed.Count > MyRender11Constants.SHADER_MAX_BONES)
                return false;

            var partBones = new List<int>(bonesUsed.Keys);
            partBones.Sort();

            if (partBones.Count == 0 ||
                partBones[partBones.Count - 1] < MyRender11Constants.SHADER_MAX_BONES)
                return true;

            for (int i = 0; i < partBones.Count; i++)
                bonesUsed[partBones[i]] = i;

            Dictionary<int, int> vertexTouched = new Dictionary<int, int>();

            for (int i = 0; i < trianglesNum; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    int index = partIndices[i * 3 + j];

                    if (vertexTouched.ContainsKey(index))
                        continue;

                    Vector4 boneWeight = boneWeights[index];
                    Vector4I boneIndex = boneIndices[index];

                    if (boneWeight.X > 0)
                        boneIndex.X = bonesUsed[boneIndex.X];
                    if (boneWeight.Y > 0)
                        boneIndex.Y = bonesUsed[boneIndex.Y];
                    if (boneWeight.Z > 0)
                        boneIndex.Z = bonesUsed[boneIndex.Z];
                    if (boneWeight.W > 0)
                        boneIndex.W = bonesUsed[boneIndex.W];

                    boneIndices[index] = boneIndex;
                    vertexTouched[index] = 1;
                }
            }

            bonesMapping = partBones.ToArray();
            return true;
        }

        static MyMeshPartInfo1 AppendPartIndices(
            MyMeshData meshData,
            List<int> partIndices,
            bool doOffset)
        {
            // Transforms part indices and appends them to meshData.NewIndices
            List<uint> indices = meshData.NewIndices;

            #region Minimize index span

            int startIndex = indices.Count;
            int indexCount = partIndices.Count;
            uint minIndex = (uint)partIndices[0];
            uint maxIndex = 0;

            foreach (var i in partIndices)
            {
                indices.Add((uint)i);
                minIndex = Math.Min(minIndex, (uint)i);
            }

            for (int i = startIndex; i < startIndex + indexCount; i++)
            {
                indices[i] -= minIndex;
                maxIndex = Math.Max(maxIndex, indices[i]);
            }

            #endregion

            #region Offset vertices to center

            Vector3 centerOffset = Vector3.Zero;

            if (doOffset)
            {
                Vector3[] unpackedPositions = new Vector3[partIndices.Count];

                for (int i = 0; i < partIndices.Count; i++)
                {
                    HalfVector4 packedPos = meshData.Positions[partIndices[i]];
                    Vector3 unpackedPos = PositionPacker.UnpackPosition(ref packedPos);
                    centerOffset += unpackedPos;
                    unpackedPositions[i] = unpackedPos;
                }

                centerOffset /= partIndices.Count;

                for (int i = 0; i < partIndices.Count; i++)
                {
                    Vector3 unpackedPosition = unpackedPositions[i];
                    unpackedPosition -= centerOffset;
                    meshData.Positions[partIndices[i]] = PositionPacker.PackPosition(ref unpackedPosition);
                }
            }

            #endregion

            #region Fill part info

            Debug.Assert(minIndex <= int.MaxValue);

            var newPartInfo = new MyMeshPartInfo1
            {
                IndexCount = indexCount,
                StartIndex = startIndex,
                BaseVertex = (int)minIndex,
                CenterOffset = centerOffset,
            };

            if (maxIndex > meshData.MaxIndex)
                meshData.MaxIndex = maxIndex;

            return newPartInfo;

            #endregion
        }

        static void CreateSections(
            MyMeshData meshData,
            MyMeshPartInfo1[] parts,
            string assetName, // debug
            out MyMeshSectionInfo1[] sections)
        {
            var sectionInfos = meshData.SectionInfos;

            sections = new MyMeshSectionInfo1[sectionInfos.Count];
            var partSubmeshCounts = new int[parts.Length];

            for (int sectionIndex = 0; sectionIndex < sectionInfos.Count; sectionIndex++)
            {
                MyMeshSectionInfo section = sectionInfos[sectionIndex];
                MyMeshSectionPartInfo1[] sectionMeshInfos = new MyMeshSectionPartInfo1[section.Meshes.Count];

                int meshesIndex = 0;
                foreach (MyMeshSectionMeshInfo meshInfo in section.Meshes)
                {
                    Tuple<int, int, int> materialPartInfo; // Contains PartIndex, StartIndex and BaseVertex
                    if(!meshData.MatsIndices.TryGetValue(meshInfo.MaterialName, out materialPartInfo))
                    {
                        // Material does not exist within this mesh.
                        // Invalidate the section loading process with log msg.
                        // Remove section infos to prevent their usage.
                        MyRender11.Log.WriteLine(String.Format("Section references material that is not present and sections wont be loaded. Section: {0}, Material_Name:{1}", section.Name, meshInfo.MaterialName));
                        sections = null;
                        meshData.SectionInfos = null;
                        return;
                    }

                    var matId = MyMeshMaterials1.GetMaterialId(meshInfo.MaterialName);

                    sectionMeshInfos[meshesIndex] = new MyMeshSectionPartInfo1()
                    {
                        IndexCount = meshInfo.IndexCount,
                        StartIndex = meshInfo.StartIndex + materialPartInfo.Item2,
                        BaseVertex = materialPartInfo.Item3,
                        PartIndex = materialPartInfo.Item1,
                        PartSubmeshIndex = partSubmeshCounts[materialPartInfo.Item1],
                        Material = matId,
                    };

                    partSubmeshCounts[materialPartInfo.Item1]++;
                    meshesIndex++;
                }

                sections[sectionIndex] = new MyMeshSectionInfo1()
                {
                    Name = section.Name,
                    Meshes = sectionMeshInfos
                };
            }

            for (int idx = 0; idx < parts.Length; idx++)
                parts[idx].SectionSubmeshCount = partSubmeshCounts[idx];
        }

        static unsafe void FillMeshData(
            MyMeshData meshData,
            ref MyLodMeshInfo lodMeshInfo,
            out MyMeshRawData rawData)
        {
            rawData = new MyMeshRawData();
            int verticesNum = meshData.Positions.Length;

            // Create and fill index buffers
            if (meshData.MaxIndex <= ushort.MaxValue)
            {
                MyArrayHelpers.InitOrReserveNoCopy(ref m_tmpShortIndices, lodMeshInfo.IndicesNum);
                uint[] sourceData = meshData.NewIndices.GetInternalArray();

                fixed (uint* sourcePointer = sourceData)
                {
                    fixed (void* destinationPointer = m_tmpShortIndices)
                    {
                        CopyIndices(sourcePointer, destinationPointer, 0, 0, sizeof(ushort), (uint)lodMeshInfo.IndicesNum);
                    }
                }

                FillIndexData(ref rawData, m_tmpShortIndices, lodMeshInfo.IndicesNum);
            }
            else
            {
                FillIndexData(ref rawData, meshData.NewIndices.GetInternalArray(), lodMeshInfo.IndicesNum);
            }

            int numComponents = 0;
            if (!meshData.IsAnimated) numComponents += 1; else numComponents += 3;
            if (meshData.ValidStreams)
            {
                numComponents += 3;
                if (MyRender11.Settings.UseGeometryArrayTextures)
                    numComponents++;
            }
            var vertexComponents = new List<MyVertexInputComponent>(numComponents); // Capacity is used in an assert below...

            // stream 0
            if (!meshData.IsAnimated)
            {
                Debug.Assert(sizeof(HalfVector4) == sizeof(MyVertexFormatPositionH4));
                FillStream0Data(ref rawData, meshData.Positions, verticesNum);

                vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.POSITION_PACKED));
            }
            else
            {
                var vertices = new MyVertexFormatPositionSkinning[verticesNum];
                fixed (MyVertexFormatPositionSkinning* destinationPointer = vertices)
                {
                    for (int vertexIndex = 0; vertexIndex < verticesNum; ++vertexIndex)
                    {
                        destinationPointer[vertexIndex].Position = meshData.Positions[vertexIndex];
                        destinationPointer[vertexIndex].BoneIndices = new Byte4(meshData.BoneIndices[vertexIndex].X, meshData.BoneIndices[vertexIndex].Y, meshData.BoneIndices[vertexIndex].Z, meshData.BoneIndices[vertexIndex].W);
                        destinationPointer[vertexIndex].BoneWeights = new HalfVector4(meshData.BoneWeights[vertexIndex]);
                    }
                }

                FillStream0Data(ref rawData, vertices, verticesNum);

                vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.POSITION_PACKED));
                vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.BLEND_WEIGHTS));
                vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.BLEND_INDICES));
            }

            // stream 1
            if (meshData.ValidStreams)
            {
                var vertices = new MyVertexFormatTexcoordNormalTangentTexindices[verticesNum];
                fixed (MyVertexFormatTexcoordNormalTangentTexindices* destinationPointer = vertices)
                {
                    for (int vertexIndex = 0; vertexIndex < verticesNum; ++vertexIndex)
                    {
                        destinationPointer[vertexIndex].Normal = meshData.Normals[vertexIndex];
                        destinationPointer[vertexIndex].Tangent = meshData.StoredTangents[vertexIndex];
                        destinationPointer[vertexIndex].Texcoord = meshData.Texcoords[vertexIndex];
                        destinationPointer[vertexIndex].TexIndices = (Byte4)meshData.TexIndices[vertexIndex];
                    }
                }

                FillStream1Data(ref rawData, vertices, vertices.Length);

                vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.NORMAL, 1));
                vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.TANGENT_SIGN_OF_BITANGENT, 1));
                vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.TEXCOORD0_H, 1));
                if (MyRender11.Settings.UseGeometryArrayTextures)
                    vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.TEXINDICES, 1));
            }

            Debug.Assert(vertexComponents.Count == vertexComponents.Capacity);
            vertexComponents.Capacity = vertexComponents.Count;

            rawData.VertexLayout = MyVertexLayouts.GetLayout(vertexComponents.GetInternalArray());
        }

        #endregion Mwm loading


        static LodMeshId StoreLodMeshWithParts(
                MeshId mesh, int lodIndex,
                MyLodMeshInfo lodMeshInfo,
                MyMeshPartInfo1[] parts)
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
                    string sectionName = sections[i].Name;
                    var sectionId = NewMeshSection(mesh, lodIndex, sectionName);
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
            StoreLodMeshWithParts(id, 0, lodInfo, partsInfo);

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
            MyVertexFormatTexcoordNormalTangentTexindices[] stream1,
            MyRuntimeSectionInfo[] sections,
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

        static unsafe void MoveData(LodMeshId lodMeshId)
        {
            var info = LodMeshInfos.Data[lodMeshId.Index];
            if (info.NullLodMesh)
                return;

            DisposeLodMeshBuffers(lodMeshId);

            MoveData(lodMeshId.Info.VerticesNum, lodMeshId.Info.IndicesNum, ref LodMeshInfos.Data[lodMeshId.Index].Data, ref LodMeshBuffers[lodMeshId.Index]);
        }

        static unsafe void MoveData(int vertexCount, int indexCount, ref MyMeshRawData rawData, ref MyMeshBuffers meshBuffer)
        {
            fixed (void* ptr = rawData.VertexStream0)
            {
                meshBuffer.VB0 = MyManagers.Buffers.CreateVertexBuffer(
                    "vb 0", vertexCount, rawData.Stride0,
                    new IntPtr(ptr), ResourceUsage.Immutable);
            }
            if (rawData.Stride1 > 0)
            {
                fixed (void* ptr = rawData.VertexStream1)
                {
                    meshBuffer.VB1 = MyManagers.Buffers.CreateVertexBuffer(
                        "vb 1", vertexCount, rawData.Stride1,
                        new IntPtr(ptr), ResourceUsage.Immutable);
                }
            }
            fixed (void* ptr = rawData.Indices)
            {
                meshBuffer.IB = MyManagers.Buffers.CreateIndexBuffer(
                    "ib", indexCount, new IntPtr(ptr),
                    rawData.IndicesFmt, ResourceUsage.Immutable);
            }
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
            var lodId = StoreLodMeshWithParts(id, lod, lodInfo, partsInfo);

            LodMeshInfos.Data[lodId.Index].Data.VertexLayout = VoxelLayout;

            return id;
        }

        internal static int BatchNumMaterials(MyClipmapCellBatch b)
        {
            int res = 0;

            if (b.Material0 != -1)
                res++;

            if (b.Material1 != -1)
                res++;

            if (b.Material2 != -1)
                res++;

            return res;
        }

        internal static int CalcNumSingleMatBatches(List<MyClipmapCellBatch> batches)
        {
            int res = 0;

            for (int i = 0; i < batches.Count; i++)
            {
                if (BatchNumMaterials(batches[i]) == 1)
                {
                    res++;
                }
            }

            return res;
        }

        // NOTE: batches are expected to be sorted by single/multi material property
        static List<MyClipmapCellBatch> CollapseBatches(List<MyClipmapCellBatch> batches)
        {
            List<MyClipmapCellBatch> res = new List<MyClipmapCellBatch>();

            for (int i = 0; i < batches.Count;)
            {
                bool startBatchIsMulti = BatchNumMaterials(batches[i]) > 1;
                int  numInCluster = 1;

                for ( ;(i + numInCluster) < batches.Count; numInCluster++)
                {
                    bool currBatchIsMulti = BatchNumMaterials(batches[i + numInCluster]) > 1;

                    if (startBatchIsMulti != currBatchIsMulti)
                    {
                        break;
                    }
                }

                int numVerts = 0;
                int numIndices = 0;

                for (int j = 0; j < numInCluster; j++)
                {
                    numVerts += batches[i + j].Vertices.Length;
                    numIndices += batches[i + j].Indices.Length;
                }

                MyClipmapCellBatch newBatch = new MyClipmapCellBatch();

                newBatch.Vertices = new MyVertexFormatVoxelSingleData[numVerts];
                newBatch.Indices = new uint[numIndices];

                {
                    int vertexOffs = 0;
                    int indexOffs = 0;

                    for (int j = 0; j < numInCluster; j++)
                    {
                        int currNumVerts = batches[i + j].Vertices.Length;
                        int currNumIndices = batches[i + j].Indices.Length;

                        Array.Copy(batches[i + j].Vertices, 0, newBatch.Vertices, vertexOffs, currNumVerts);

                        var srcIndices = batches[i + j].Indices;

                        for (int k = 0; k < currNumIndices; k++)
                        {
                            newBatch.Indices[indexOffs + k] = srcIndices[k] + (uint)vertexOffs;
                        }

                        vertexOffs += currNumVerts;
                        indexOffs += currNumIndices;
                    }
                }

                // When merging batches for rendering, we actually don't care about materials, because they are handled
                // through global material lookup table accessed from GPU

                newBatch.Material0 = batches[i].Material0;
                newBatch.Material1 = batches[i].Material1;
                newBatch.Material2 = batches[i].Material2;

                res.Add(newBatch);

                i += numInCluster;
            }

            return res;
        }

        internal static void UpdateVoxelCell(MeshId mesh, MyClipmapCellMeshMetadata metadata, List<MyClipmapCellBatch> batches)
        {
            //
            // This block of code updates global voxel-materials related constant buffer. It is very likely
            // that this is not the best place where to perform this action and somebody with better
            // understanding of engine architecture should fix it.
            //

            for (int i = 0; i < batches.Count; i++)
            {
                if (batches[i].Material0 != -1)
                    MyVoxelMaterials1.UpdateGlobalVoxelMaterialsCB(batches[i].Material0);

                if (batches[i].Material1 != -1)
                    MyVoxelMaterials1.UpdateGlobalVoxelMaterialsCB(batches[i].Material1);

                if (batches[i].Material2 != -1)
                    MyVoxelMaterials1.UpdateGlobalVoxelMaterialsCB(batches[i].Material2);
            }

            List<MyClipmapCellBatch>  collapsedBatches = CollapseBatches(batches);
            MyClipmapCellMeshMetadata metadataForCollapsed = metadata;

            metadataForCollapsed.BatchCount = collapsedBatches.Count;

            UpdateVoxelCellInternal(mesh, metadataForCollapsed, collapsedBatches);
        }

        private static void UpdateVoxelCellInternal(MeshId mesh, MyClipmapCellMeshMetadata metadata, List<MyClipmapCellBatch> batches)
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

        public static string GetContentPath(string file)
        {
            string contentPath = null;
            if (Path.IsPathRooted(file) && file.ToLower().Contains("models"))
                contentPath = file.Substring(0, file.ToLower().IndexOf("models"));

            return contentPath;
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

            public static MyVertexCopyHelper operator +(MyVertexCopyHelper left, MyVertexCopyHelper right)   // Don't care about overflows as they don't happen in our use case
            {
                return new MyVertexCopyHelper { LowBits = left.LowBits + right.LowBits, HighBits = left.HighBits + right.HighBits };
            }
        }

        private static unsafe void CopyVertices(MyVertexFormatVoxelSingleData* sourcePointer, MyVertexFormatVoxel* destinationPointer0, MyVertexFormatNormal* destinationPointer1, uint elementsToCopy)
        {
            MyVertexFormatVoxel* currentDestination0 = destinationPointer0;
            MyVertexFormatNormal* currentDestination1 = destinationPointer1;

            const ulong ValueToAdd = (((ulong)short.MaxValue) << 0) + (((ulong)short.MaxValue) << 16) + (((ulong)short.MaxValue) << 32);
            MyVertexCopyHelper ValueToAdd128 = new MyVertexCopyHelper { LowBits = ValueToAdd, HighBits = ValueToAdd };
            for (int batchVertexIndex = 0; batchVertexIndex < elementsToCopy; ++batchVertexIndex)
            {
                MyVertexFormatVoxelSingleData* batchVertex = sourcePointer + batchVertexIndex;

                // NOTE: I don't like this :-)
                *((MyVertexCopyHelper*)currentDestination0) = (*((MyVertexCopyHelper*)batchVertex) + ValueToAdd128);
                *((ulong*)currentDestination1) = *((ulong*)batchVertex + 2);

                currentDestination0->m_materialInfo = batchVertex->MaterialInfo;

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

        private static unsafe void CopyIndices(uint* sourcePointer, void* destinationPointer, int startIndex, int baseVertex, int destinationIndexStride, uint elementsToCopy)
        {
            switch (destinationIndexStride)
            {
                case 2:
                    ushort* shortIndices = ((ushort*)destinationPointer) + startIndex;
                    ushort* endIndexPointer = shortIndices + elementsToCopy;

                    while (shortIndices <= endIndexPointer)
                    {
                        *shortIndices = (ushort)(*(sourcePointer++) + baseVertex);
                        ++shortIndices;
                    }
                    break;

                case 4:
                    uint* intIndices = ((uint*)destinationPointer) + startIndex;
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
        private static unsafe void CombineBatchesToParts(
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
        private static unsafe void CombineBatchesToParts(
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
        private static unsafe void CombineBatchesToParts(
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
                var batchMaterial = new MyVoxelMaterialTriple(batches[batchIndex].Material0, batches[batchIndex].Material1, batches[batchIndex].Material2, false);

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

            if (LodMeshBuffers[lodMeshId.Index].VB0 == null)
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

        static unsafe void FillStreamData(ref byte[] destinationData, void* sourcePointer, int elementCount, int elementStride)
        {
            var byteSize = elementCount * elementStride;
            MyArrayHelpers.ResizeNoCopy(ref destinationData, byteSize);
            fixed (void* destinationPointer = destinationData)
            {
                SharpDX.Utilities.CopyMemory(new IntPtr(destinationPointer), new IntPtr(sourcePointer), byteSize);
            }
        }

        static unsafe void FillStream0Data(ref MyMeshRawData rawData, void* sourcePointer, int vertexCount, int vertexStride)
        {
            rawData.Stride0 = vertexStride;
            FillStreamData(ref rawData.VertexStream0, sourcePointer, vertexCount, vertexStride);
        }

        static unsafe void FillStream0Data(ref MyMeshRawData rawData, MyVertexFormatPositionH4[] vertices, int vertexCount)
        {
            fixed (void* sourcePointer = vertices)
            {
                FillStream0Data(ref rawData, sourcePointer, vertexCount, MyVertexFormatPositionH4.STRIDE);
            }
        }

        static unsafe void FillStream0Data(ref MyMeshRawData rawData, HalfVector4[] vertices, int vertexCount)
        {
            fixed (void* sourcePointer = vertices)
            {
                FillStream0Data(ref rawData, sourcePointer, vertexCount, sizeof(HalfVector4));
            }
        }

        static unsafe void FillStream0Data(ref MyMeshRawData rawData, MyVertexFormatPositionSkinning[] vertices, int vertexCount)
        {
            fixed (void* sourcePointer = vertices)
            {
                FillStream0Data(ref rawData, sourcePointer, vertexCount, MyVertexFormatPositionSkinning.STRIDE);
            }
        }

        static unsafe void FillStream0Data(ref MyMeshRawData rawData, MyVertexFormatVoxel[] vertices, int vertexCount)
        {
            fixed (void* sourcePointer = vertices)
            {
                FillStream0Data(ref rawData, sourcePointer, vertexCount, MyVertexFormatVoxel.STRIDE);
            }
        }

        static unsafe void FillStream1Data(ref MyMeshRawData rawData, void* sourcePointer, int vertexCount, int vertexStride)
        {
            rawData.Stride1 = vertexStride;
            FillStreamData(ref rawData.VertexStream1, sourcePointer, vertexCount, vertexStride);
        }

        static unsafe void FillStream1Data(ref MyMeshRawData rawData, MyVertexFormatTexcoordNormalTangentTexindices[] vertices, int vertexCount)
        {
            fixed (void* sourcePointer = vertices)
            {
                FillStream1Data(ref rawData, sourcePointer, vertexCount, MyVertexFormatTexcoordNormalTangentTexindices.STRIDE);
            }
        }

        static unsafe void FillStream1Data(ref MyMeshRawData rawData, MyVertexFormatNormal[] vertices, int vertexCount)
        {
            fixed (void* sourcePointer = vertices)
            {
                FillStream1Data(ref rawData, sourcePointer, vertexCount, MyVertexFormatNormal.STRIDE);
            }
        }

        static unsafe void FillIndexData(ref MyMeshRawData rawData, ushort[] indices, int indexCapacity)
        {
            rawData.IndicesFmt = MyIndexBufferFormat.UShort;
            fixed (void* sourceIndices = indices)
            {
                FillStreamData(ref rawData.Indices, sourceIndices, indexCapacity, sizeof(ushort));
            }
        }

        static unsafe void FillIndexData(ref MyMeshRawData rawData, uint[] indices, int indexCapacity)
        {
            rawData.IndicesFmt = MyIndexBufferFormat.UInt;
            fixed (void* sourceIndices = indices)
            {
                FillStreamData(ref rawData.Indices, sourceIndices, indexCapacity, sizeof(uint));
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

        private static void DisposeLodMeshBuffers(LodMeshId lodMeshId)
        {
            DisposeMeshBuffers(lodMeshId.Buffers);
            LodMeshBuffers[lodMeshId.Index] = MyMeshBuffers.Empty;
        }

        private static void DisposeMeshBuffers(MyMeshBuffers buffers)
        {
            if (buffers.IB != null)
                MyManagers.Buffers.Dispose(buffers.IB); buffers.IB = null;

            if (buffers.VB0 != null)
                MyManagers.Buffers.Dispose(buffers.VB0); buffers.VB0 = null;

            if (buffers.VB1 != null)
                MyManagers.Buffers.Dispose(buffers.VB1); buffers.VB1 = null;
        }

        internal static void RemoveVoxelCell(MeshId id)
        {
            if (!MeshVoxelInfo.ContainsKey(id))
                return;

            MyLodMesh lodMesh = new MyLodMesh { Mesh = id, Lod = 0 };
            if (LodMeshIndex.ContainsKey(lodMesh))
            {
                var lodMeshId = LodMeshIndex[lodMesh];

                ResizeVoxelParts(id, lodMeshId, 0);
                DisposeLodMeshBuffers(lodMeshId);
                LodMeshInfos.Data[lodMeshId.Index].Data = new MyMeshRawData();
                LodMeshInfos.Free(lodMeshId.Index);

                LodMeshIndex.Remove(lodMesh);
            }
            
            MeshInfos.Free(id.Index);
            MeshVoxelInfo.Remove(id);
        }

        const string ERROR_MODEL_PATH = "Models/Debug/Error.mwm";

        static void LoadMesh(MeshId id)
        {
            var assetName = MeshInfos.Data[id.Index].Name;
            float rescale = MeshInfos.Data[id.Index].Rescale;

            MyLodMeshInfo meshMainLod = new MyLodMeshInfo
            {
                Name = assetName,
                FileName = assetName
            };

            MeshInfos.Data[id.Index].Loaded = true;

            MyMeshPartInfo1[] parts;
            MyMeshSectionInfo1[] sections;
            MyLODDescriptor[] lodDescriptors;

            bool modelOk = LoadMwm(ref meshMainLod, out parts, out sections, out lodDescriptors, rescale);
            if (!modelOk)
            {
                meshMainLod.FileName = ERROR_MODEL_PATH;

                if (!LoadMwm(ref meshMainLod, out parts, out sections, out lodDescriptors, rescale))
                {
                    Debug.Fail("error model missing");
                }
            }

            MeshInfos.Data[id.Index].FileExists = true;

            StoreLodMeshWithParts(id, 0, meshMainLod, parts);
            StoreLodMeshSections(id, 0, ref sections);

            int modelLods = 1;

            if (lodDescriptors != null)
                for (int i = 0; i < lodDescriptors.Length; i++)
                {
                    var meshFile = lodDescriptors[i].GetModelAbsoluteFilePath(assetName);
                    MyLodMeshInfo lodMesh = new MyLodMeshInfo
                    {
                        FileName = meshFile,
                        LodDistance = lodDescriptors[i].Distance,
                        NullLodMesh = meshFile == null,
                    };

                    MyMeshPartInfo1[] lodParts;
                    MyMeshSectionInfo1[] lodSections;
                    bool lodOk = LoadMwm(ref lodMesh, out lodParts, out lodSections, rescale);
                    if (lodOk)
                    {
                        //lodMesh.FileName = ERROR_MODEL_PATH;
                        //if(!LoadMwm(ref lodMesh, out lodParts))
                        //{
                        //    Debug.Fail("error model missing");
                        //}
                        StoreLodMeshWithParts(id, modelLods, lodMesh, lodParts);
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
