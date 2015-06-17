using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using VRage;
using VRage.Import;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;
using VRageMath.PackedVector;
using VRageRender.Vertex;
using VRage.Library.Utils;
using VRage.FileSystem;

namespace VRageRender
{
    struct MeshId
    {
        internal int Index;

        public static bool operator ==(MeshId x, MeshId y)
        {
            return x.Index == y.Index;
        }

        public static bool operator !=(MeshId x, MeshId y)
        {
            return x.Index != y.Index;
        }

        internal static readonly MeshId NULL = new MeshId { Index = -1 };

        internal MyMeshInfo Info { get { return MyMeshes.Meshes.Data[Index]; } }
    }

    struct LodMeshId
    {
        internal int Index;

        public static bool operator ==(LodMeshId x, LodMeshId y)
        {
            return x.Index == y.Index;
        }

        public static bool operator !=(LodMeshId x, LodMeshId y)
        {
            return x.Index != y.Index;
        }

        internal static readonly LodMeshId NULL = new LodMeshId { Index = -1 };

        internal MyLodMeshInfo Info { get { return MyMeshes.Lods.Data[Index]; } }
        internal MyMeshBuffers Buffers { get { return MyMeshes.LodMeshBuffers[Index]; } }
        internal VertexLayoutId VertexLayout { get { return MyMeshes.Lods.Data[Index].Data.VertexLayout; } }
    }

    struct MeshPartId
    {
        internal int Index;

        public static bool operator ==(MeshPartId x, MeshPartId y)
        {
            return x.Index == y.Index;
        }

        public static bool operator !=(MeshPartId x, MeshPartId y)
        {
            return x.Index != y.Index;
        }

        internal static readonly MeshPartId NULL = new MeshPartId { Index = -1 };

        internal MyMeshPartInfo1 Info { get { return MyMeshes.Parts.Data[Index]; } }
    }

    struct VoxelPartId
    {
        internal int Index;

        public static bool operator ==(VoxelPartId x, VoxelPartId y)
        {
            return x.Index == y.Index;
        }

        public static bool operator !=(VoxelPartId x, VoxelPartId y)
        {
            return x.Index != y.Index;
        }

        internal static readonly VoxelPartId NULL = new VoxelPartId { Index = -1 };

        internal MyVoxelPartInfo1 Info { get { return MyMeshes.VoxelParts.Data[Index]; } }
    }

    struct MyMeshPartInfo1
    {
        internal int IndexCount;
        internal int StartIndex;
        internal int BaseVertex;

        internal int[] BonesMapping;

        internal MyMeshMaterialId Material;
    }

    struct MyLodMeshInfo
    {
        internal string Name;
        internal string FileName;
        internal int PartsNum;
        internal bool HasBones { get { return Data.VertexLayout.Info.HasBonesInfo; } }

        internal int VerticesNum;
        internal int IndicesNum;

        internal float LodDistance;

        internal MyMeshRawData Data;

        internal BoundingBox? BoundingBox;
        internal BoundingSphere? BoundingSphere;
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

    struct MyMeshBuffers
    {
        internal VertexBufferId VB0;
        internal VertexBufferId VB1;
        internal IndexBufferId IB;

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
    }

    struct MyMeshPart
    {
        internal MeshId Mesh;
        internal int Lod;
        internal int Part;
    }

    // fractures are the only asset existing over sessions (performance reasons) and some parts need to be recreated after they get dropped on session end (like material ids)
    struct MyRuntimeMeshPersistentInfo
    {
        internal MySectionInfo[] Sections;
    }

    static class MyMeshes
    {
        static Dictionary<MyStringId, MeshId> MeshNameIndex = new Dictionary<MyStringId, MeshId>(MyStringId.Comparer);
        static Dictionary<MyStringId, MeshId> RuntimeMeshNameIndex = new Dictionary<MyStringId, MeshId>(MyStringId.Comparer);
        internal static MyFreelist<MyMeshInfo> Meshes = new MyFreelist<MyMeshInfo>(4096);
        internal static MyFreelist<MyLodMeshInfo> Lods = new MyFreelist<MyLodMeshInfo>(4096);
        internal static MyMeshBuffers[] LodMeshBuffers = new MyMeshBuffers[4096];
        internal static MyFreelist<MyMeshPartInfo1> Parts = new MyFreelist<MyMeshPartInfo1>(8192);

        static Dictionary<MyLodMesh, LodMeshId> LodMeshIndex = new Dictionary<MyLodMesh, LodMeshId>();
        static Dictionary<MyMeshPart, MeshPartId> PartIndex = new Dictionary<MyMeshPart, MeshPartId>();

        static Dictionary<MeshId, MyVoxelCellInfo> MeshVoxelInfo = new Dictionary<MeshId, MyVoxelCellInfo>();
        internal static MyFreelist<MyVoxelPartInfo1> VoxelParts = new MyFreelist<MyVoxelPartInfo1>(2048);
        static Dictionary<MyMeshPart, VoxelPartId> VoxelPartIndex = new Dictionary<MyMeshPart,VoxelPartId>();


        static Dictionary<MeshId, MyRuntimeMeshPersistentInfo> InterSessionData = new Dictionary<MeshId, MyRuntimeMeshPersistentInfo>();
        static HashSet<MeshId> InterSessionDirty = new HashSet<MeshId>();

        static HashSet<MeshId>[] State;

        internal static VertexLayoutId VoxelLayout = VertexLayoutId.NULL;

        internal static void Init()
        {
            int statesNum = Enum.GetNames(typeof(MyMeshState)).Length;
            State = new HashSet<MeshId>[statesNum];
            for (int i = 0; i < statesNum; i++)
            {
                State[i] = new HashSet<MeshId>();
            }

            VoxelLayout = MyVertexLayouts.GetLayout(
                new MyVertexInputComponent(MyVertexInputComponentType.VOXEL_POSITION_MAT),
                new MyVertexInputComponent(MyVertexInputComponentType.VOXEL_NORMAL, 1));
        }

        internal static bool Exists(string name)
        {
            var x = X.TEXT(name);
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
            return LodMeshIndex[new MyLodMesh { Mesh = mesh, Lod = lod }];
        }

        internal static MeshPartId GetMeshPart(MeshId mesh, int lod, int part)
        {
            return PartIndex[new MyMeshPart { Mesh = mesh, Lod = lod, Part = part }];
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

                if(InterSessionDirty.Contains(id))
                {
                    RefreshMaterialIds(id);

                    InterSessionDirty.Remove(id);
                }

                return id;
            }

            if(!MeshNameIndex.ContainsKey(nameKey))
            {
                var id = new MeshId{ Index = Meshes.Allocate() };
                MeshNameIndex[nameKey] = id;

                Meshes.Data[id.Index] = new MyMeshInfo
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
            for(int l=0; l<lods; l++)
            {
                var mesh = GetLodMesh(model, l);

                DisposeLodMeshBuffers(mesh);

                int parts = Lods.Data[mesh.Index].PartsNum;
                for (int p = 0; p < parts; p++ )
                {
                    var part = GetMeshPart(model, l, p);
                    Parts.Free(part.Index);
                }

                Lods.Data[mesh.Index].Data = new MyMeshRawData();
                Lods.Free(mesh.Index);
            }

            Meshes.Free(model.Index);

            if (model.Info.NameKey != MyStringId.NullOrEmpty)
            {
                MeshNameIndex.Remove(model.Info.NameKey);
                RuntimeMeshNameIndex.Remove(model.Info.NameKey);
            }

            //internal static MyFreelist<MyLodMeshInfo> Lods = new MyFreelist<MyLodMeshInfo>(4096);
            //internal static MyFreelist<MyMeshPartInfo1> Parts = new MyFreelist<MyMeshPartInfo1>(8192);
        }

        internal static void OnSessionEnd()
        {
            bool KEEP_FRACTURES = true;

            foreach (var id in RuntimeMeshNameIndex.Values.ToArray())
            {
                bool fracture = id.Info.RuntimeGenerated && !id.Info.Dynamic;
                if(!(fracture && KEEP_FRACTURES))
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
            var id = new LodMeshId { Index = Lods.Allocate() };
            Lods.Data[id.Index] = new MyLodMeshInfo { };

            LodMeshIndex[new MyLodMesh { Mesh = mesh, Lod = lod}] = id;
            MyArrayHelpers.Reserve(ref LodMeshBuffers, id.Index + 1);
            LodMeshBuffers[id.Index] = MyMeshBuffers.Empty;

            return id;
        }

        static MeshPartId NewMeshPart(MeshId mesh, int lod, int part)
        {
            var id = new MeshPartId { Index = Parts.Allocate() };
            Parts.Data[id.Index] = new MyMeshPartInfo1 { };

            PartIndex[new MyMeshPart { Mesh = mesh, Lod = lod, Part = part}] = id;

            return id;
        }

        static bool LoadMwm(ref MyLodMeshInfo lodMeshInfo, 
            out MyMeshPartInfo1 [] parts)
        {
            MyLODDescriptor[] lodDescriptors;
            return LoadMwm(ref lodMeshInfo, out parts, out lodDescriptors);
        }

        // returns false when mesh couldn't be loaded
        unsafe static bool LoadMwm(ref MyLodMeshInfo lodMeshInfo, 
            out MyMeshPartInfo1 [] parts, out MyLODDescriptor[] lodDescriptors)
        {
            parts = null;
            lodDescriptors = null;

            var importer = new MyModelImporter();
            var file = lodMeshInfo.FileName;
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

            if(validStreams == false)
            {
                MyRender11.Log.WriteLine(String.Format("Mesh asset {0} has inconsistent vertex streams", file));
                normals = new Byte4[0];
                texcoords = new HalfVector2[0];
                tangents = new Byte4[0];
                bitangents = new Byte4[0];
            }
            if(tangents.Length > 0 && bitangents.Length > 0)
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
                for (int i = 0; i < texcoords.Length; ++i )
                {
                    texcoords[i] = new HalfVector2(texcoords[i].ToVector2() / PatternScale);
                }
            }

            bool hasBonesInfo = boneIndices.Length > 0 && boneWeights.Length > 0 && boneIndices.Length == verticesNum && boneWeights.Length == verticesNum;
            var bones = (MyModelBone[]) tagData[MyImporterConstants.TAG_BONES];

            bool isAnimated = hasBonesInfo;
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

                    if(materialDesc == null)
                    {
                        MyRender11.Log.WriteLine(String.Format("Mesh asset {0} has no material in part {1}", file, partIndex));
                    }


                    var matId = MyMeshMaterials1.GetMaterialId(materialDesc, contentPath, file);

                    parts[partIndex] = new MyMeshPartInfo1 
                    {
                        IndexCount = indexCount,
                        StartIndex = startIndex,
                        BaseVertex = (int)baseVertex,
                        
                        Material = matId,
                        
                        BonesMapping = partUsedBonesMap[partIndex]
                    };

                    #endregion

                    partIndex++;
                }

                #region Part data

                var rawData = new MyMeshRawData();

                lodMeshInfo.IndicesNum = indices.Count;

                // indices
                if (maxIndex <= ushort.MaxValue)
                {
                    var indices16 = new ushort[indices.Count];
                    for (int i = 0; i < indices.Count; i++)
                    {
                        indices16[i] = (ushort)indices[i];
                    }

                    //for (int i = 0; i < indices.Count; i+=3)
                    //{
                    //    indices16[i + 1] = (ushort)indices[i + 2];
                    //    indices16[i + 2] = (ushort)indices[i + 1];
                    //}


                    FillIndex16Data(ref rawData, indices16);
                }
                else
                {
                    var indices32 = indices.ToArray();

                    rawData.IndicesFmt = Format.R32_UInt;
                    var byteSize = indices.Count * FormatHelper.SizeOfInBytes(rawData.IndicesFmt);
                    rawData.Indices = new byte[byteSize];

                    fixed (void* dst = rawData.Indices)
                    {
                        fixed (void* src = indices32)    
                        {
                            SharpDX.Utilities.CopyMemory(new IntPtr(dst), new IntPtr(src), byteSize);
                        }
                    }
                }

                var vertexComponents = new List<MyVertexInputComponent>();

                // stream 0
                if (!isAnimated)
                {
                    var vertices = new MyVertexFormatPositionH4[verticesNum];
                    for (int i = 0; i < verticesNum; i++)
                    {
                        vertices[i] = new MyVertexFormatPositionH4(positions[i]);
                    }

                    FillStream0Data(ref rawData, vertices);

                    vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.POSITION_PACKED));
                }
                else
                {
                    var vertices = new MyVertexFormatPositionSkinning[verticesNum];
                    for (int i = 0; i < verticesNum; i++)
                    {
                        vertices[i] = new MyVertexFormatPositionSkinning(
                            positions[i],
                            new Byte4(boneIndices[i].X, boneIndices[i].Y, boneIndices[i].Z, boneIndices[i].W),
                            boneWeights[i]);
                    }

                    rawData.Stride0 = sizeof(MyVertexFormatPositionSkinning);
                    var byteSize = rawData.Stride0 * verticesNum;
                    rawData.VertexStream0 = new byte[byteSize];

                    fixed (void* dst = rawData.VertexStream0)
                    {
                        fixed (void* src = vertices)
                        {
                            SharpDX.Utilities.CopyMemory(new IntPtr(dst), new IntPtr(src), byteSize);
                        }
                    }
                    

                    vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.POSITION_PACKED));
                    vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.BLEND_WEIGHTS));
                    vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.BLEND_INDICES));
                }


                // stream 1
                if(validStreams)
                {
                    var vertices = new MyVertexFormatTexcoordNormalTangent[verticesNum];
                    for (int i = 0; i < verticesNum; i++)
                    {
                        vertices[i] = new MyVertexFormatTexcoordNormalTangent(texcoords[i], normals[i], storedTangents[i]);
                    }

                    FillStream1Data(ref rawData, vertices);

                    vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.NORMAL, 1));
                    vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.TANGENT_SIGN_OF_BITANGENT, 1));
                    vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.TEXCOORD0_H, 1));
                }

                rawData.VertexLayout = MyVertexLayouts.GetLayout(vertexComponents.ToArray());

                #endregion

                lodMeshInfo.Data = rawData;
                lodMeshInfo.BoundingBox = (BoundingBox)tagData[MyImporterConstants.TAG_BOUNDING_BOX];
                lodMeshInfo.BoundingSphere = (BoundingSphere)tagData[MyImporterConstants.TAG_BOUNDING_SPHERE];
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

            Lods.Data[lod.Index] = lodMeshInfo;

            for(int i=0; i<parts.Length; i++)
            {
                var part = NewMeshPart(mesh, lodIndex, i);
                Parts.Data[part.Index] = parts[i];
            }

            return lod;
        }

        // 1 lod, n parts
        internal static MeshId CreateRuntimeMesh(MyStringId nameKey, int parts, bool dynamic)
        {
            Debug.Assert(!RuntimeMeshNameIndex.ContainsKey(nameKey));

            var id = new MeshId { Index = Meshes.Allocate() };
            RuntimeMeshNameIndex[nameKey] = id;

            Meshes.Data[id.Index] = new MyMeshInfo
            {
                Name = nameKey.ToString(),
                NameKey = nameKey,
                LodsNum = 1,
                Dynamic = dynamic,
                RuntimeGenerated = true
            };

            MyLodMeshInfo lodInfo = new MyLodMeshInfo{ PartsNum = parts };
            MyMeshPartInfo1 [] partsInfo = new MyMeshPartInfo1[parts];
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

            var lod = LodMeshIndex[new MyLodMesh { Mesh = mesh, Lod = 0 }];

            FillIndex16Data(ref Lods.Data[lod.Index].Data, indices);
            FillStream0Data(ref Lods.Data[lod.Index].Data, stream0);
            FillStream1Data(ref Lods.Data[lod.Index].Data, stream1);

            Lods.Data[lod.Index].BoundingBox = aabb;

            var vertexComponents = new List<MyVertexInputComponent>();
            vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.POSITION_PACKED));
            vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.NORMAL, 1));
            vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.TANGENT_SIGN_OF_BITANGENT, 1));
            vertexComponents.Add(new MyVertexInputComponent(MyVertexInputComponentType.TEXCOORD0_H, 1));

            Lods.Data[lod.Index].Data.VertexLayout = MyVertexLayouts.GetLayout(vertexComponents.ToArray());

            for(int i=0; i<sections.Length; i++)
            {
                var part = PartIndex[new MyMeshPart { Mesh = mesh, Lod = 0, Part = i }];
                Parts.Data[part.Index].StartIndex = sections[i].IndexStart;
                Parts.Data[part.Index].IndexCount = sections[i].TriCount * 3;
                Parts.Data[part.Index].Material = MyMeshMaterials1.GetMaterialId(sections[i].MaterialName);
            }

            Lods.Data[lod.Index].IndicesNum = indices.Length;
            Lods.Data[lod.Index].VerticesNum = stream0.Length;

            //if (mesh.Info.Dynamic && Lods.Data[lod.Index].VerticesNum)
            //{
            //    UpdateData(lod);
            //}
            if (Lods.Data[lod.Index].VerticesNum > 0)
            {
                MoveData(lod);
            }
        }

        unsafe static void FillIndex16Data(ref MyMeshRawData rawData, ushort[] indices)
        {
            rawData.IndicesFmt = Format.R16_UInt;
            var byteSize = indices.Length * FormatHelper.SizeOfInBytes(rawData.IndicesFmt);
            if (rawData.Indices == null)
            { 
                rawData.Indices = new byte[byteSize];
            }
            else
            {
                MyArrayHelpers.Reserve(ref rawData.Indices, byteSize);
            }

            fixed (void* dst = rawData.Indices)
            {
                fixed (void* src = indices)
                {
                    SharpDX.Utilities.CopyMemory(new IntPtr(dst), new IntPtr(src), byteSize);
                }
            }
        }

        unsafe static void FillStream0Data(ref MyMeshRawData rawData, MyVertexFormatPositionH4[] vertices)
        {
            rawData.Stride0 = sizeof(MyVertexFormatPositionH4);
            var byteSize = rawData.Stride0 * vertices.Length;
            rawData.VertexStream0 = new byte[byteSize];

            fixed (void* dst = rawData.VertexStream0)
            {
                fixed (void* src = vertices)
                {
                    SharpDX.Utilities.CopyMemory(new IntPtr(dst), new IntPtr(src), byteSize);
                }
            }
        }

        unsafe static void FillStream1Data(ref MyMeshRawData rawData, MyVertexFormatTexcoordNormalTangent[] vertices)
        {
            rawData.Stride1 = sizeof(MyVertexFormatTexcoordNormalTangent);
            var byteSize = rawData.Stride1 * vertices.Length;
            rawData.VertexStream1 = new byte[byteSize];

            fixed (void* dst = rawData.VertexStream1)
            {
                fixed (void* src = vertices)
                {
                    SharpDX.Utilities.CopyMemory(new IntPtr(dst), new IntPtr(src), byteSize);
                }
            }
        }

        unsafe static void FillStream0Data(ref MyMeshRawData rawData, MyVertexFormatVoxel[] vertices)
        {
            rawData.Stride0 = sizeof(MyVertexFormatVoxel);
            var byteSize = rawData.Stride0 * vertices.Length;
            if (rawData.VertexStream0 == null)
            {
                rawData.VertexStream0 = new byte[byteSize];
            }
            else
            {
                MyArrayHelpers.Reserve(ref rawData.VertexStream0, byteSize);
            }
            

            fixed (void* dst = rawData.VertexStream0)
            {
                fixed (void* src = vertices)
                {
                    SharpDX.Utilities.CopyMemory(new IntPtr(dst), new IntPtr(src), byteSize);
                }
            }
        }

        unsafe static void FillStream1Data(ref MyMeshRawData rawData, MyVertexFormatNormal[] vertices)
        {
            rawData.Stride1 = sizeof(MyVertexFormatNormal);
            var byteSize = rawData.Stride1 * vertices.Length;
            if (rawData.VertexStream1 == null)
            {
                rawData.VertexStream1 = new byte[byteSize];
            }
            else
            {
                MyArrayHelpers.Reserve(ref rawData.VertexStream1, byteSize);
            }

            fixed (void* dst = rawData.VertexStream1)
            {
                fixed (void* src = vertices)
                {
                    SharpDX.Utilities.CopyMemory(new IntPtr(dst), new IntPtr(src), byteSize);
                }
            }
        }

        unsafe static void MoveData(LodMeshId id)
        {
            var data = Lods.Data[id.Index].Data;
            var verticesNum = id.Info.VerticesNum;

            fixed(void* ptr = data.VertexStream0)
            {
                if (LodMeshBuffers[id.Index].VB0 != VertexBufferId.NULL)
                {
                    MyHwBuffers.Destroy(LodMeshBuffers[id.Index].VB0);
                    LodMeshBuffers[id.Index].VB0 = VertexBufferId.NULL;
                }

                LodMeshBuffers[id.Index].VB0 = MyHwBuffers.CreateVertexBuffer(verticesNum, data.Stride0, new IntPtr(ptr), id.Info.FileName + " vb 0");
            }
            if (data.Stride1 > 0)
            {
                fixed (void* ptr = data.VertexStream1)
                {
                    if (LodMeshBuffers[id.Index].VB1 != VertexBufferId.NULL)
                    {
                        MyHwBuffers.Destroy(LodMeshBuffers[id.Index].VB1);
                        LodMeshBuffers[id.Index].VB1 = VertexBufferId.NULL;
                    }

                    LodMeshBuffers[id.Index].VB1 = MyHwBuffers.CreateVertexBuffer(verticesNum, data.Stride1, new IntPtr(ptr), id.Info.FileName + " vb 1");
                }
            }
            fixed (void* ptr = data.Indices)
            {
                if (LodMeshBuffers[id.Index].IB != IndexBufferId.NULL)
                {
                    MyHwBuffers.Destroy(LodMeshBuffers[id.Index].IB);
                    LodMeshBuffers[id.Index].IB = IndexBufferId.NULL;
                }

                LodMeshBuffers[id.Index].IB = MyHwBuffers.CreateIndexBuffer(id.Info.IndicesNum, data.IndicesFmt, new IntPtr(ptr), id.Info.FileName + " ib");
            }
        }

        class MyVoxelTripleComparer : IComparer<MyVoxelMaterialTriple>
        {
            public int Compare(MyVoxelMaterialTriple x, MyVoxelMaterialTriple y)
            {
                var a = x.I0.CompareTo(y.I0);
                if (a != 0)
                { 
                    return a;
                }
                var b = x.I1.CompareTo(y.I1);
                if( b != 0)
                {
                    return b;
                }
                var c = x.I2.CompareTo(y.I2);
                return c;
            }

            internal static MyVoxelTripleComparer Instance = new MyVoxelTripleComparer();
        } 

        internal static MeshId CreateVoxelCell(Vector3I coord, int lod)
        {
            var id = new MeshId { Index = Meshes.Allocate() };
            MeshVoxelInfo[id] = new MyVoxelCellInfo { Coord = coord, Lod = lod };

            Meshes.Data[id.Index] = new MyMeshInfo
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
            var lodId = StoreLodMeshWithParts(id, 0, ref lodInfo, ref partsInfo);

            Lods.Data[lodId.Index].Data.VertexLayout = MyVertexLayouts.GetLayout(
                new MyVertexInputComponent(MyVertexInputComponentType.VOXEL_POSITION_MAT),
                new MyVertexInputComponent(MyVertexInputComponentType.VOXEL_NORMAL, 1));

            return id;
        }

        internal struct MyBufferSegment
        {
            internal int vertexOffset;
            internal int vertexCapacity;
            internal int indexOffset;
            internal int indexCapacity;

            internal int indexCount;
            internal int vertexCount;
        }

        struct MyVoxelCellUpdate
        {
            internal short[] indices;
            internal MyVertexFormatVoxelSingleData[] vertexData;
            internal int material0;
            internal int material1;
            internal int material2;
        }

        internal static void UpdateVoxelCell(MeshId mesh, List<MyClipmapCellBatch> batches)
        {
            var lod = LodMeshIndex[new MyLodMesh { Mesh = mesh, Lod = 0 }];

            int vertexCapacity = 0;
            int indexCapacity = 0;
            //var vbAllocations = new SortedDictionary<MyVoxelMaterialTriple, MyBufferSegment>(MyVoxelTripleComparer.Instance);

            int batchesNum = batches.Count;

            for (int i = 0; i < batchesNum; i++)
            {
                indexCapacity += batches[i].Indices.Length;
                vertexCapacity += batches[i].Vertices.Length;
            }

            //for (int i = 0; i < len; i++)
            //{
            //    var ilen = batches[i].Indices.Length;
            //    var vlen = batches[i].Vertices.Length;

            //    var key = new MyVoxelMaterialTriple(batches[i].Material0, batches[i].Material1, batches[i].Material2);

            //    MyBufferSegment entry = new MyBufferSegment();

            //    entry.indexCapacity = ilen;
            //    entry.vertexCapacity = vlen;
            //    entry.indexCount = ilen;
            //    entry.vertexCount = vlen;
            //    vbAllocations[key] = entry;
            //}

            //int voffset = 0;
            //int ioffset = 0;

            //// allocation
            //var keys = vbAllocations.Keys.ToList();
            //foreach (var key in keys)
            //{
            //    var val = vbAllocations[key];
            //    val.indexOffset = ioffset;
            //    val.vertexOffset = voffset;
            //    ioffset += val.indexCapacity;
            //    voffset += val.vertexCapacity;
            //    vbAllocations[key] = val;
            //}

            //vertexCapacity = voffset;
            //indexCapacity = ioffset;

            var indices = new ushort[indexCapacity];
            var vertices0 = new MyVertexFormatVoxel[vertexCapacity];
            var vertices1 = new MyVertexFormatNormal[vertexCapacity];

            int singleMat = 0;
            int multiMat = 0;

            int startIndex = 0;
            int baseVertex = 0;
            ResizeVoxelParts(mesh, lod, batchesNum);

            for (int i = 0; i < batchesNum; i++)
            {
                var key = new MyVoxelMaterialTriple(batches[i].Material0, batches[i].Material1, batches[i].Material2);
                //var entry = vbAllocations[key];

                if (key.I1 == -1 && key.I2 == -1)
                    singleMat++;
                else
                    multiMat++;

                //var offset = startIndex;
                var batchIndices = batches[i].Indices;
                for (int j = 0; j < batchIndices.Length; j++)
                {
                    indices[startIndex + j] = (ushort)(batchIndices[j] + baseVertex);
                }

                //offset = entry.vertexOffset;
                var batchVertices = batches[i].Vertices;
                for (int j = 0; j < batchVertices.Length; j++)
                {
                    vertices0[baseVertex + j] = new MyVertexFormatVoxel();
                    vertices0[baseVertex + j].Position = batchVertices[j].Position;
                    vertices0[baseVertex + j].PositionMorph = batchVertices[j].PositionMorph;
                    
                    var mat = batchVertices[j].MaterialAlphaIndex;
                    switch (mat)
                    {
                        case 0:
                            vertices0[baseVertex + j].Weight0 = 1;
                            break;
                        case 1:
                            vertices0[baseVertex + j].Weight1 = 1;
                            break;
                        case 2:
                            vertices0[baseVertex + j].Weight2 = 1;
                            break;
                    }
                    mat = batchVertices[j].MaterialMorph;
                    switch (mat)
                    {
                        case 0:
                            vertices0[baseVertex + j].Weight0Morph = 1;
                            break;
                        case 1:
                            vertices0[baseVertex + j].Weight1Morph = 1;
                            break;
                        case 2:
                            vertices0[baseVertex + j].Weight2Morph = 1;
                            break;
                    }

                    vertices1[baseVertex + j] = new MyVertexFormatNormal(batchVertices[j].PackedNormal, batchVertices[j].PackedNormalMorph);
                }

                var id = VoxelPartIndex[new MyMeshPart { Mesh = mesh, Lod = 0, Part = i }];
                VoxelParts.Data[id.Index] = new MyVoxelPartInfo1 { IndexCount = batches[i].Indices.Length, StartIndex = startIndex, BaseVertex = 0, 
                    MaterialTriple = new MyVoxelMaterialTriple(batches[i].Material0, batches[i].Material1, batches[i].Material2) };

                //Debug.WriteLine(batches[i].Indices.Length + ", " + startIndex);

                baseVertex += batchVertices.Length;
                startIndex += batchIndices.Length;
            }
            
            FillIndex16Data(ref Lods.Data[lod.Index].Data, indices);
            FillStream0Data(ref Lods.Data[lod.Index].Data, vertices0);
            FillStream1Data(ref Lods.Data[lod.Index].Data, vertices1);

            Lods.Data[lod.Index].VerticesNum = vertices0.Length;
            Lods.Data[lod.Index].IndicesNum = indices.Length;

            MoveData(lod);
        }

        internal static void ResizeVoxelParts(MeshId mesh, LodMeshId lod, int num)
        {
            int currentParts = Lods.Data[lod.Index].PartsNum;

            // extend
            if(currentParts < num)
            {
                for(int i = currentParts; i<num; i++)
                {
                    var id = new VoxelPartId { Index = VoxelParts.Allocate() };
                    VoxelPartIndex[new MyMeshPart{ Mesh = mesh, Lod = 0, Part = i}] = id;
                }
            }
            // drop
            else if(currentParts > num)
            {
                for(int i = num; i<currentParts; i++)
                {
                    var id = VoxelPartIndex[new MyMeshPart { Mesh = mesh, Lod = 0, Part = i }];
                    VoxelParts.Free(id.Index);
                    VoxelPartIndex.Remove(new MyMeshPart { Mesh = mesh, Lod = 0, Part = i });
                }
            }
            
            Lods.Data[lod.Index].PartsNum = num;
        }

        internal static void DisposeLodMeshBuffers(LodMeshId id)
        {
            var buffers = id.Buffers;
            if(buffers.IB != IndexBufferId.NULL)
            {
                MyHwBuffers.Destroy(buffers.IB);
            }
            if (buffers.VB0 != VertexBufferId.NULL)
            {
                MyHwBuffers.Destroy(buffers.VB0);
            }
            if (buffers.VB1 != VertexBufferId.NULL)
            {
                MyHwBuffers.Destroy(buffers.VB1);
            }

            LodMeshBuffers[id.Index] = MyMeshBuffers.Empty;
        }

        internal static void RemoveVoxelCell(MeshId id)
        {
            Debug.Assert(MeshVoxelInfo.ContainsKey(id));

            var lod = LodMeshIndex[new MyLodMesh { Mesh = id, Lod = 0 }];

            ResizeVoxelParts(id, lod, 0);
            DisposeLodMeshBuffers(lod);
            Lods.Data[lod.Index].Data = new MyMeshRawData();
            Lods.Free(lod.Index);
            Meshes.Free(id.Index);

            LodMeshIndex.Remove(new MyLodMesh { Mesh = id, Lod = 0 });
            MeshVoxelInfo.Remove(id);
        }

        const string ERROR_MODEL_PATH = "Models/Debug/Error.mwm";

        static void LoadMesh(MeshId id)
        {
            var assetName = Meshes.Data[id.Index].Name;

            MyLodMeshInfo meshMainLod = new MyLodMeshInfo
            {
                Name = assetName,
                FileName = assetName
            };

            Meshes.Data[id.Index].Loaded = true;

            
            MyMeshPartInfo1 [] parts;
            MyLODDescriptor[] lodDescriptors;

            bool modelOk = LoadMwm(ref meshMainLod, out parts, out lodDescriptors);

            if(!modelOk)
            {
                meshMainLod.FileName = ERROR_MODEL_PATH;

                if(!LoadMwm(ref meshMainLod, out parts, out lodDescriptors))
                {
                    Debug.Fail("error model missing");
                }
            }

            Meshes.Data[id.Index].FileExists = true;

            StoreLodMeshWithParts(id, 0, ref meshMainLod, ref parts);

            int modelLods = 1;

            for(int i=0; i<lodDescriptors.Length; i++)
            {
                var meshFile = lodDescriptors[i].Model;
                if (!meshFile.EndsWith(".mwm"))
                {
                    meshFile += ".mwm";
                }

                MyLodMeshInfo lodMesh = new MyLodMeshInfo
                {
                    FileName = meshFile,
                    LodDistance = lodDescriptors[i].Distance
                };
                
                MyMeshPartInfo1 [] lodParts;

                bool lodOk = LoadMwm(ref lodMesh, out lodParts);
                if(lodOk)
                {
                    //lodMesh.FileName = ERROR_MODEL_PATH;
                    //if(!LoadMwm(ref lodMesh, out lodParts))
                    //{
                    //    Debug.Fail("error model missing");
                    //}
                    StoreLodMeshWithParts(id, modelLods++, ref lodMesh, ref lodParts);
                }
            }

            Meshes.Data[id.Index].LodsNum = modelLods;

            for(int i=0; i<modelLods; i++)
            {
                MoveData(LodMeshIndex[new MyLodMesh { Mesh = id, Lod = i }]);
            }
        }

        internal static void Load()
        {
            foreach(var id in State[(int)MyMeshState.WAITING].ToList())
            {
                LoadMesh(id);
                MoveState(id, MyMeshState.WAITING, MyMeshState.LOADED);
            }
        }

        internal static void OnDeviceReset()
        {
            foreach(var lod in LodMeshIndex.Values)
            {
                MoveData(lod);
            }
        }
    }
}
