#region Using

using BulletXNA.BulletCollision;
using Havok;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using VRage.Utils;
using VRageMath.PackedVector;
using VRageRender.Animations;
using VRage.FileSystem;
using VRage.Import;
using BoundingBox = VRageMath.BoundingBox;
using BoundingSphere = VRageMath.BoundingSphere;
using Vector3 = VRageMath.Vector3;
using VRageMath;
using VRageRender;
using VRageRender.Fractures;
using VRageRender.Import;
using VRageRender.Models;

#endregion


namespace VRage.Game.Models
{

    /// <summary>
    /// structure used to set up the mesh
    /// </summary>
    public struct MyTriangleVertexIndices
    {
        public int I0, I1, I2;

        public MyTriangleVertexIndices(int i0, int i1, int i2)
        {
            this.I0 = i0;
            this.I1 = i1;
            this.I2 = i2;
        }

        public void Set(int i0, int i1, int i2)
        {
            I0 = i0; I1 = i1; I2 = i2;
        }
    }

    public class MyModel : IDisposable, IPrimitiveManagerBase
    {
        #region Static Fields

        static int m_nextUniqueId = 0;
        static Dictionary<int, string> m_uniqueModelNames = new Dictionary<int, string>();
        static Dictionary<string, int> m_uniqueModelIds = new Dictionary<string, int>();

        [ThreadStatic]
        static MyModelImporter m_perThreadImporter;

        #endregion // Static Fields

        #region Instance Fields

        public bool KeepInMemory { get; private set; }

        public readonly int UniqueId;

        public int DataVersion { get; private set; }

        int m_verticesCount;
        int m_trianglesCount;

        private MyCompressedVertexNormal[] m_vertices;
        private MyCompressedBoneIndicesWeights[] m_bonesIndicesWeights;

        private int[] m_Indices = null;
        private ushort[] m_Indices_16bit = null;

        public MyTriangleVertexIndices[] Triangles;       //  Triangles specified by three indices to "Vertex" list      //TODO: Could be made readonly from the outside, and alterable only from the inside of this class
        public Dictionary<string, MyModelDummy> Dummies;
        public MyModelInfo ModelInfo;


        public ModelAnimations Animations;
        public MyModelBone[] Bones;

        public byte[] HavokData;

        public MyModelFractures ModelFractures;

        // This was exported in wrong way with model offset and block center wrong offset
        // Will be removed once all models contains correct collision shapes
        private bool m_hasUV = false;

        public bool m_loadUV = false;

        public bool ExportedWrong;
        public HkShape[] HavokCollisionShapes;
        public HkdBreakableShape[] HavokBreakableShapes;
        public byte[] HavokDestructionData;

        private HalfVector2[] m_texCoords;

        Byte4[] m_tangents = null;

        //  Bounding volumes
        private BoundingSphere m_boundingSphere;
        private BoundingBox m_boundingBox;

        //  Size of the bounding box
        private Vector3 m_boundingBoxSize;
        private Vector3 m_boundingBoxSizeHalf;

        public VRageMath.Vector3I[] BoneMapping;
        public float PatternScale = 1;

        //  Octree
        IMyTriangePruningStructure m_bvh;

        readonly string m_assetName;
        bool m_loadedData;

        List<MyMesh> m_meshContainer = new List<MyMesh>();
        Dictionary<string, MyMeshSection> m_meshSections = new Dictionary<string, MyMeshSection>();

        bool m_loadingErrorProcessed = false;

        private float m_scaleFactor = 1.0f;

        #endregion // Fields

        #region Properties

        static MyModelImporter m_importer
        {
            get
            {
                if (m_perThreadImporter == null)
                    m_perThreadImporter = new MyModelImporter();
                return m_perThreadImporter;
            }
        }

        public int[] Indices
        {
            get { return m_Indices; }
        }

        public ushort[] Indices16
        {
            get { return m_Indices_16bit; }
        }


        public bool HasUV
        {
            get { return m_hasUV; }
        }

        public bool LoadUV
        {
            set { m_loadUV = value; }
        }

        public HalfVector2[] TexCoords
        {
            get { return m_texCoords; }
        }

        public bool LoadedData
        {
            get { return m_loadedData; }
        }

        public float ScaleFactor { get { return m_scaleFactor; } }

        /// <summary>
        /// File path of the model
        /// </summary>
        public string AssetName
        {
            get { return m_assetName; }
        }

        public BoundingSphere BoundingSphere { get { return m_boundingSphere; } }
        public BoundingBox BoundingBox { get { return m_boundingBox; } }

        public Vector3 BoundingBoxSize { get { return m_boundingBoxSize; } }
        public Vector3 BoundingBoxSizeHalf { get { return m_boundingBoxSizeHalf; } }

        #endregion // Properties

        public static string GetById(int id)
        {
            return m_uniqueModelNames[id];
        }

        public static int GetId(string assetName)
        {
            int result;
            lock (m_uniqueModelIds)
            {
                if (!m_uniqueModelIds.TryGetValue(assetName, out result))
                {
                    result = m_nextUniqueId++;
                    m_uniqueModelIds.Add(assetName, result);
                    m_uniqueModelNames.Add(result, assetName);
                }
            }
            return result;
        }

        public Vector3 GetVertexInt(int vertexIndex)
        {
            return VF_Packer.UnpackPosition(ref m_vertices[vertexIndex].Position);
        }

        public MyTriangleVertexIndices GetTriangle(int triangleIndex)
        {
            return Triangles[triangleIndex];
        }

        public Vector3 GetVertex(int vertexIndex)
        {
            return GetVertexInt(vertexIndex);
        }

        public void SetVertexPosition(int vertexIndex, ref Vector3 newPosition)
        {
            m_vertices[vertexIndex].Position = VF_Packer.PackPosition(newPosition);
        }

        public void GetVertex(int vertexIndex1, int vertexIndex2, int vertexIndex3, out Vector3 v1, out Vector3 v2, out Vector3 v3)
        {
            v1 = GetVertex(vertexIndex1);
            v2 = GetVertex(vertexIndex2);
            v3 = GetVertex(vertexIndex3);
        }

        public MyTriangle_BoneIndicesWeigths? GetBoneIndicesWeights(int triangleIndex)
        {
            if (m_bonesIndicesWeights == null)
                return null;

            MyTriangleVertexIndices indices = Triangles[triangleIndex];

            MyCompressedBoneIndicesWeights boneIndicesWeightsV0 = m_bonesIndicesWeights[indices.I0];
            MyCompressedBoneIndicesWeights boneIndicesWeightsV1 = m_bonesIndicesWeights[indices.I1];
            MyCompressedBoneIndicesWeights boneIndicesWeightsV2 = m_bonesIndicesWeights[indices.I2];

            Vector4UByte indicesV0 = boneIndicesWeightsV0.Indices.ToVector4UByte();
            Vector4 weightsV0 = boneIndicesWeightsV0.Weights.ToVector4();

            Vector4UByte indicesV1 = boneIndicesWeightsV1.Indices.ToVector4UByte();
            Vector4 weightsV1 = boneIndicesWeightsV1.Weights.ToVector4();

            Vector4UByte indicesV2 = boneIndicesWeightsV2.Indices.ToVector4UByte();
            Vector4 weightsV2 = boneIndicesWeightsV2.Weights.ToVector4();

            MyTriangle_BoneIndicesWeigths ret = new MyTriangle_BoneIndicesWeigths()
            {
                Vertex0 = new MyVertex_BoneIndicesWeights() { Indices = indicesV0, Weights = weightsV0 },
                Vertex1 = new MyVertex_BoneIndicesWeights() { Indices = indicesV1, Weights = weightsV1 },
                Vertex2 = new MyVertex_BoneIndicesWeights() { Indices = indicesV2, Weights = weightsV2 }
            };
            return ret;
        }

        public Vector3 GetVertexNormal(int vertexIndex)
        {
            return VF_Packer.UnpackNormal(ref m_vertices[vertexIndex].Normal);
        }

        public Vector3 GetVertexTangent(int vertexIndex)
        {
            if (m_tangents == null)
            {
                m_importer.ImportData(AssetName, new string[] { MyImporterConstants.TAG_TANGENTS });
                Dictionary<string, object> tagData = m_importer.GetTagData();
                if (tagData.ContainsKey(MyImporterConstants.TAG_TANGENTS))
                {
                    m_tangents = (Byte4[])tagData[MyImporterConstants.TAG_TANGENTS];
                }
            }

            if (m_tangents != null)
            {
                return VF_Packer.UnpackNormal(m_tangents[vertexIndex]);
            }

            return Vector3.Zero;
        }
 
        //  Create instance of a model, but doesn't really load the model from file to memory. Only remembers its definition.
        //  Data are loaded later using lazy-load mechanism - in LoadData or LoadInDraw
        //  Parameters of this constructor that are nullable aren't mandatory - that's why they are nullable.
        //  But they might be needed at some point of model's life, so think!
        //  E.g. if texture isn't specified, then it's not assigned to shader during rendering. Same for "shininess" and "specularPower"
        //  But models that use it should have null in all other texture parameters, because those textures won't be used.
        //  IMPORTANT: ASSERTS IN THIS CONSTRUCTOR SHOULD CHECK IF ALL REQUIRED PARAMETERS AND THEIR COMBINATIONS ARE FINE!
        //  BUT THE REALITY IS THAT I DON'T HAVE TIME TO ASSERT ALL POSSIBLE COMBINATIONS...                          

        public MyModel(string assetName)
            : this(assetName, false)
        {
            UniqueId = GetId(assetName);
        }

        /// <summary>
        /// c-tor - this constructor should be used just for max models - not voxels!
        /// </summary>
        public MyModel(string assetName, bool keepInMemory)
        {
            m_assetName = assetName;
            m_loadedData = false;
            KeepInMemory = keepInMemory;

            var fsPath = Path.IsPathRooted(AssetName) ? AssetName : Path.Combine(MyFileSystem.ContentPath, AssetName);
            //System.Diagnostics.Debug.Assert(MyFileSystem.FileExists(fsPath), "Model data for " + m_assetName + " does not exists!");
        }

        public List<MyMesh> GetMeshList()
        {
            return m_meshContainer;
        }

        public MyMeshSection GetMeshSection(string name)
        {
            return m_meshSections[name];
        }

        public bool TryGetMeshSection(string name, out MyMeshSection section)
        {
            return m_meshSections.TryGetValue(name, out section);
        }

        //  Sort of lazy-load, where constructor just saves information about what this model should be, but real load is done here - and only one time.
        //  This loads only vertex data, doesn't touch GPU
        //  Can be called from main and background thread
        public void LoadData()
        {
            lock (this)
            {
                if (m_loadedData) return;

                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyModel::LoadData");


                MyLog.Default.WriteLine("MyModel.LoadData -> START", LoggingOptions.LOADING_MODELS);
                MyLog.Default.IncreaseIndent(LoggingOptions.LOADING_MODELS);

                MyLog.Default.WriteLine("m_assetName: " + m_assetName, LoggingOptions.LOADING_MODELS);

                //  Read data from model TAG parameter. There are stored vertex positions, triangle indices, vectors, ... everything we need.
                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Model - load data - import data");

                MyLog.Default.WriteLine(String.Format("Importing asset {0}, path: {1}", m_assetName, AssetName), LoggingOptions.LOADING_MODELS);


                string assetForImport = AssetName;
                var fsPath = Path.IsPathRooted(AssetName) ? AssetName : Path.Combine(MyFileSystem.ContentPath, AssetName);
                if (!MyFileSystem.FileExists(fsPath))
                {
                    assetForImport = @"Models\Debug\Error.mwm";
                }

                try
                {
                    m_importer.ImportData(assetForImport);
                }
                catch
                {
                    MyLog.Default.WriteLine(String.Format("Importing asset failed {0}", m_assetName));
                    VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
                    VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
                    throw;
                }
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

                DataVersion = m_importer.DataVersion;

                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Model - load data - load tag data");
                Dictionary<string, object> tagData = m_importer.GetTagData();
                if (tagData.Count == 0)
                {
                    VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
                    VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
                    throw new Exception(String.Format("Uncompleted tagData for asset: {0}, path: {1}", m_assetName, AssetName));
                }
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Model - load data - vertex, normals, texture coords");


                HalfVector4[] vertices = (HalfVector4[])tagData[MyImporterConstants.TAG_VERTICES];

                System.Diagnostics.Debug.Assert(vertices.Length > 0);

                Byte4[] normals = (Byte4[])tagData[MyImporterConstants.TAG_NORMALS];
                m_vertices = new MyCompressedVertexNormal[vertices.Length];
                if (normals.Length > 0)
                {
                    for (int v = 0; v < vertices.Length; v++)
                    {
                        m_vertices[v] = new MyCompressedVertexNormal()
                        {
                            Position = vertices[v],// VF_Packer.PackPosition(ref vertices[v]),
                            Normal = normals[v]//VF_Packer.PackNormalB4(ref normals[v])
                        };
                    }
                }
                else
                {
                    for (int v = 0; v < vertices.Length; v++)
                    {
                        m_vertices[v] = new MyCompressedVertexNormal()
                        {
                            Position = vertices[v],// VF_Packer.PackPosition(ref vertices[v]),
                        };
                    }
                }


                m_verticesCount = vertices.Length;

                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Model - load data - mesh");

                var materials = new Dictionary<string, MyMeshMaterial>();
                m_meshContainer.Clear();
                if (tagData.ContainsKey(MyImporterConstants.TAG_MESH_PARTS))
                {
                    List<int> indices = new List<int>(GetVerticesCount()); // Default capacity estimation
                    int maxIndex = 0;

                    List<MyMeshPartInfo> meshParts = tagData[MyImporterConstants.TAG_MESH_PARTS] as List<MyMeshPartInfo>;
                    foreach (MyMeshPartInfo meshPart in meshParts)
                    {
                        MyMesh mesh = new MyMesh(meshPart, m_assetName);
                        mesh.IndexStart = indices.Count;
                        mesh.TriCount = meshPart.m_indices.Count / 3;

                        if (mesh.Material.Name != null)
                            materials.Add(mesh.Material.Name, mesh.Material);

                        if (m_loadUV && false == m_hasUV)
                        {
                            m_texCoords = (HalfVector2[])tagData[MyImporterConstants.TAG_TEXCOORDS0];
                            m_hasUV = true;
                            m_loadUV = false;
                        }

                        System.Diagnostics.Debug.Assert(mesh.TriCount > 0);

                        if (mesh.TriCount == 0)
                        {
                            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
                            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
                            return;
                        }

                        foreach (var i in meshPart.m_indices)
                        {
                            indices.Add(i);
                            if (i > maxIndex)
                            {
                                maxIndex = i;
                            }
                        }



                        m_meshContainer.Add(mesh);
                    }

                    if (maxIndex <= ushort.MaxValue)
                    {
                        // create 16 bit indices
                        m_Indices_16bit = new ushort[indices.Count];
                        for (int i = 0; i < indices.Count; i++)
                        {
                            m_Indices_16bit[i] = (ushort)indices[i];
                        }
                    }
                    else
                    {
                        // use 32bit indices
                        m_Indices = indices.ToArray();
                    }
                }

                m_meshSections.Clear();
                if (tagData.ContainsKey(MyImporterConstants.TAG_MESH_SECTIONS))
                {
                    List<MyMeshSectionInfo> sections = tagData[MyImporterConstants.TAG_MESH_SECTIONS] as List<MyMeshSectionInfo>;
                    int sectionindex = 0;
                    foreach (MyMeshSectionInfo sectinfo in sections)
                    {
                        MyMeshSection section = new MyMeshSection() { Name = sectinfo.Name, Index = sectionindex };
                        m_meshSections.Add(section.Name, section);
                        sectionindex++;
                    }
                }

                if (tagData.ContainsKey(MyImporterConstants.TAG_MODEL_BVH))
                {
                    m_bvh = new MyQuantizedBvhAdapter(tagData[MyImporterConstants.TAG_MODEL_BVH] as GImpactQuantizedBvh, this);
                }

                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Model - load data - other data");

                Animations = (ModelAnimations)tagData[MyImporterConstants.TAG_ANIMATIONS];
                Bones = (MyModelBone[])tagData[MyImporterConstants.TAG_BONES];

                Vector4I[] boneIndices = (Vector4I[])tagData[MyImporterConstants.TAG_BLENDINDICES];
                Vector4[] boneWeights = (Vector4[])tagData[MyImporterConstants.TAG_BLENDWEIGHTS];
                if (boneIndices != null && boneIndices.Length != 0)
                {
                    if (boneWeights != null && boneIndices.Length == boneWeights.Length && boneIndices.Length == m_vertices.Length)
                    {
                        m_bonesIndicesWeights = new MyCompressedBoneIndicesWeights[boneIndices.Length];

                        for (int it = 0; it < boneIndices.Length; it++)
                        {
                            m_bonesIndicesWeights[it].Indices = new Byte4(boneIndices[it].X, boneIndices[it].Y, boneIndices[it].Z, boneIndices[it].W);
                            m_bonesIndicesWeights[it].Weights = new HalfVector4(boneWeights[it]);
                        }
                    }
                    else
                    {
                        Debug.Assert(false, "Bone indices/weights my be same number as vertices");
                    }
                }

                m_boundingBox = (BoundingBox)tagData[MyImporterConstants.TAG_BOUNDING_BOX];
                m_boundingSphere = (BoundingSphere)tagData[MyImporterConstants.TAG_BOUNDING_SPHERE];
                m_boundingBoxSize = BoundingBox.Max - BoundingBox.Min;
                m_boundingBoxSizeHalf = BoundingBoxSize / 2.0f;
                Dummies = tagData[MyImporterConstants.TAG_DUMMIES] as Dictionary<string, MyModelDummy>;
                BoneMapping = tagData[MyImporterConstants.TAG_BONE_MAPPING] as Vector3I[];

                if (tagData.ContainsKey(MyImporterConstants.TAG_MODEL_FRACTURES))
                    ModelFractures = (MyModelFractures)tagData[MyImporterConstants.TAG_MODEL_FRACTURES];

                object patternScale;
                if (tagData.TryGetValue(MyImporterConstants.TAG_PATTERN_SCALE, out patternScale))
                {
                    PatternScale = (float)patternScale;
                }

                if (BoneMapping.Length == 0)
                    BoneMapping = null;

                if (tagData.ContainsKey(MyImporterConstants.TAG_HAVOK_COLLISION_GEOMETRY))
                {
                    HavokData = (byte[])tagData[MyImporterConstants.TAG_HAVOK_COLLISION_GEOMETRY];
                    byte[] tagCollisionData = (byte[])tagData[MyImporterConstants.TAG_HAVOK_COLLISION_GEOMETRY];
                    if (tagCollisionData.Length > 0 && HkBaseSystem.IsThreadInitialized)
                    {
                        bool containsSceneData;
                        bool containsDestructionData;
                        List<HkShape> shapesList = new List<HkShape>();
                        if (!HkShapeLoader.LoadShapesListFromBuffer(tagCollisionData, shapesList, out containsSceneData, out containsDestructionData))
                        {
                            MyLog.Default.WriteLine(string.Format("Model {0} - Unable to load collision geometry", AssetName), LoggingOptions.LOADING_MODELS);
                        //Debug.Fail("Collision model was exported in wrong way: " + m_assetName);
                        }

                        if (shapesList.Count > 10)
                            MyLog.Default.WriteLine(string.Format("Model {0} - Found too many collision shapes, only the first 10 will be used", AssetName), LoggingOptions.LOADING_MODELS);

                        if (HavokCollisionShapes != null)
                        {
                            Debug.Fail("Shapes already loaded");
                        }
                        if (shapesList.Count > 0)
                        {
                            HavokCollisionShapes = shapesList.ToArray();
                        }
                        else
                        {
                            MyLog.Default.WriteLine(string.Format("Model {0} - Unable to load collision geometry from file, default collision will be used !", AssetName));
                        }

                        if (containsDestructionData)
                            HavokDestructionData = tagCollisionData;

                        ExportedWrong = !containsSceneData;
                    }
                }


                if (tagData.ContainsKey(MyImporterConstants.TAG_HAVOK_DESTRUCTION))
                {
                    if (((byte[])tagData[MyImporterConstants.TAG_HAVOK_DESTRUCTION]).Length > 0)
                        HavokDestructionData = (byte[])tagData[MyImporterConstants.TAG_HAVOK_DESTRUCTION];
                }

                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Model - load data - copy triangle indices");
                //  Prepare data
                CopyTriangleIndices();
                m_trianglesCount = Triangles.Length;

                //  Remember this numbers as list may be cleared at the end of this method
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

                MyLog.Default.WriteLine("Triangles.Length: " + Triangles.Length, LoggingOptions.LOADING_MODELS);
                MyLog.Default.WriteLine("Vertexes.Length: " + GetVerticesCount(), LoggingOptions.LOADING_MODELS);
                MyLog.Default.WriteLine("UseChannelTextures: " + (bool)tagData[MyImporterConstants.TAG_USE_CHANNEL_TEXTURES], LoggingOptions.LOADING_MODELS);
                MyLog.Default.WriteLine("BoundingBox: " + BoundingBox, LoggingOptions.LOADING_MODELS);
                MyLog.Default.WriteLine("BoundingSphere: " + BoundingSphere, LoggingOptions.LOADING_MODELS);

                VRageRender.Utils.Stats.PerAppLifetime.MyModelsCount++;
                VRageRender.Utils.Stats.PerAppLifetime.MyModelsMeshesCount += m_meshContainer.Count;
                VRageRender.Utils.Stats.PerAppLifetime.MyModelsVertexesCount += GetVerticesCount();
                VRageRender.Utils.Stats.PerAppLifetime.MyModelsTrianglesCount += Triangles.Length;

                ModelInfo = new MyModelInfo(GetTrianglesCount(), GetVerticesCount(), BoundingBoxSize);

                m_loadedData = true;
                m_loadingErrorProcessed = false;
                MyLog.Default.DecreaseIndent(LoggingOptions.LOADING_MODELS);
                MyLog.Default.WriteLine("MyModel.LoadData -> END", LoggingOptions.LOADING_MODELS);

                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
            }
        }

        public bool LoadTexCoordData()
        {
            if (m_hasUV == false)
            {
                lock (this)
                {
                    try
                    {
                        m_importer.ImportData(AssetName, new string[] { MyImporterConstants.TAG_TEXCOORDS0 });
                    }
                    catch
                    {
                        MyLog.Default.WriteLine(String.Format("Importing asset failed {0}", m_assetName));
                        return false;
                    }

                    var tagData = m_importer.GetTagData();
                    m_texCoords = (HalfVector2[])tagData[MyImporterConstants.TAG_TEXCOORDS0];
                    m_hasUV = true;
                    m_loadUV = false;
                }
            }
            return m_hasUV;
        }

        public void LoadAnimationData()
        {
            if (m_loadedData) return;

            lock (this)
            {
                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyModel::LoadAnimationData");


                MyLog.Default.WriteLine("MyModel.LoadData -> START", LoggingOptions.LOADING_MODELS);
                MyLog.Default.IncreaseIndent(LoggingOptions.LOADING_MODELS);

                MyLog.Default.WriteLine("m_assetName: " + m_assetName, LoggingOptions.LOADING_MODELS);

                //  Read data from model TAG parameter. There are stored vertex positions, triangle indices, vectors, ... everything we need.
                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Model - load data - import data");

                MyLog.Default.WriteLine(String.Format("Importing asset {0}, path: {1}", m_assetName, AssetName), LoggingOptions.LOADING_MODELS);
                try
                {
                    m_importer.ImportData(AssetName);
                }
                catch
                {
                    MyLog.Default.WriteLine(String.Format("Importing asset failed {0}", m_assetName));
                    throw;
                }
                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

                VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Model - load data - load tag data");
                Dictionary<string, object> tagData = m_importer.GetTagData();
                //Debug.Assert(tagData.Count != 0, String.Format("Uncompleted tagData for asset: {0}, path: {1}", m_assetName, AssetName));
                if (tagData.Count != 0)
                {
                    DataVersion = m_importer.DataVersion;

                    Animations = (ModelAnimations)tagData[MyImporterConstants.TAG_ANIMATIONS];
                    Bones = (MyModelBone[])tagData[MyImporterConstants.TAG_BONES];

                    m_boundingBox = (BoundingBox)tagData[MyImporterConstants.TAG_BOUNDING_BOX];
                    m_boundingSphere = (BoundingSphere)tagData[MyImporterConstants.TAG_BOUNDING_SPHERE];
                    m_boundingBoxSize = BoundingBox.Max - BoundingBox.Min;
                    m_boundingBoxSizeHalf = BoundingBoxSize / 2.0f;
                    Dummies = tagData[MyImporterConstants.TAG_DUMMIES] as Dictionary<string, MyModelDummy>;
                    BoneMapping = tagData[MyImporterConstants.TAG_BONE_MAPPING] as VRageMath.Vector3I[];
                    if (BoneMapping.Length == 0)
                        BoneMapping = null;
                }
                else
                {
                    DataVersion = 0;

                    Animations = null;
                    Bones = null;

                    m_boundingBox = default(BoundingBox);
                    m_boundingSphere = default(BoundingSphere);
                    m_boundingBoxSize = default(Vector3);
                    m_boundingBoxSizeHalf = default(Vector3);
                    Dummies = null;
                    BoneMapping = null;
                }

                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

                ModelInfo = new MyModelInfo(GetTrianglesCount(), GetVerticesCount(), BoundingBoxSize);

                if (tagData.Count != 0)
                    m_loadedData = true;

                MyLog.Default.DecreaseIndent(LoggingOptions.LOADING_MODELS);
                MyLog.Default.WriteLine("MyModel.LoadAnimationData -> END", LoggingOptions.LOADING_MODELS);

                VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
            }
        }


        int GetNumberOfTrianglesForColDet()
        {
            int trianglesCount = 0;
            foreach (MyMesh mesh in m_meshContainer)
            {
                trianglesCount += mesh.TriCount;
            }
            return trianglesCount;
        }

        void CopyTriangleIndices()
        {
            Triangles = new MyTriangleVertexIndices[GetNumberOfTrianglesForColDet()];
            int triangleIndex = 0;

            foreach (MyMesh mesh in m_meshContainer)
            {
                mesh.TriStart = triangleIndex;

                if (m_Indices != null)
                {
                    for (int i = 0; i < mesh.TriCount; i++)
                    {
                        //  Notice we swap indices. It's because XNA's clock-wise rule probably differs from FBX's, and JigLib needs it in this order.
                        //  But because of this, I did similar swaping in my col/det functions
                        Triangles[triangleIndex] = new MyTriangleVertexIndices(m_Indices[mesh.IndexStart + i * 3 + 0], m_Indices[mesh.IndexStart + i * 3 + 2], m_Indices[mesh.IndexStart + i * 3 + 1]);
                        triangleIndex++;
                    }
                }
                else if (m_Indices_16bit != null)
                {
                    for (int i = 0; i < mesh.TriCount; i++)
                    {
                        //  Notice we swap indices. It's because XNA's clock-wise rule probably differs from FBX's, and JigLib needs it in this order.
                        //  But because of this, I did similar swaping in my col/det functions
                        Triangles[triangleIndex] = new MyTriangleVertexIndices(m_Indices_16bit[mesh.IndexStart + i * 3 + 0], m_Indices_16bit[mesh.IndexStart + i * 3 + 2], m_Indices_16bit[mesh.IndexStart + i * 3 + 1]);
                        triangleIndex++;
                    }
                }
                else throw new InvalidBranchException(); // Neither 32bit or 16bit indices are set, probably already called mesh.DisposeIndices()
            }

            CheckTriangles(triangleIndex);
        }

        [Conditional("DEBUG")]
        private void CheckTriangles(int triangleCount)
        {
            //  Validate this new array, if size is correct and if all indices are OK
            bool isOk = true;
            //MyDebug.AssertDebug(triangleCount == Triangles.Length, "Invalid triangles in model: " + m_assetName);
            foreach (MyTriangleVertexIndices triangle in Triangles)
            {
                isOk = isOk &
                    (triangle.I0 != triangle.I1) &
                    (triangle.I1 != triangle.I2) &
                    (triangle.I2 != triangle.I0) &
                    ((triangle.I0 >= 0) & (triangle.I0 < m_verticesCount)) &
                    ((triangle.I1 >= 0) & (triangle.I1 < m_verticesCount)) &
                    ((triangle.I2 >= 0) & (triangle.I2 < m_verticesCount));
            }

            //Debug.Assert(isOk, "Invalid triangles in model: " + m_assetName);
        }

        public bool UnloadData()
        {
            bool res = m_loadedData;
            m_loadedData = false;
            if (m_bvh != null)
            {
                m_bvh.Close();
                m_bvh = null;
            }

            VRageRender.Utils.Stats.PerAppLifetime.MyModelsMeshesCount -= m_meshContainer.Count;
            if (m_vertices != null)
                VRageRender.Utils.Stats.PerAppLifetime.MyModelsVertexesCount -= GetVerticesCount();
            if (Triangles != null)
                VRageRender.Utils.Stats.PerAppLifetime.MyModelsTrianglesCount -= Triangles.Length;
            if (res)
                VRageRender.Utils.Stats.PerAppLifetime.MyModelsCount--;

            if (HavokCollisionShapes != null)
            {
                for (int i = 0; i < HavokCollisionShapes.Length; i++)
                {
                    HavokCollisionShapes[i].RemoveReference();
                }
                HavokCollisionShapes = null;
            }

            if (HavokBreakableShapes != null)
            {
                HavokBreakableShapes = null;
            }

            m_vertices = null;
            Triangles = null;
            m_meshContainer.Clear();
            m_Indices_16bit = null;
            m_Indices = null;

            Dummies = null;


            HavokData = null;
            HavokDestructionData = null;

            m_scaleFactor = 1f;

            Animations = null;

            return res;
        }

        public IMyTriangePruningStructure GetTrianglePruningStructure()
        {
            Debug.Assert(m_bvh != null, "BVH should be loaded from content processor");
            return m_bvh;
        }

        public void GetTriangleBoundingBox(int triangleIndex, ref BoundingBox boundingBox)
        {
            boundingBox = BoundingBox.CreateInvalid();
            Vector3 v1, v2, v3;
            GetVertex(Triangles[triangleIndex].I0, Triangles[triangleIndex].I1, Triangles[triangleIndex].I2, out v1, out v2, out v3);

            boundingBox.Include(
                v1,
                v2,
                v3);
        }

        public int GetTrianglesCount()
        {
            return m_trianglesCount;
        }

        public int GetVerticesCount()
        {
            return m_verticesCount;
        }

        public int GetBVHSize()
        {
            return m_bvh != null ? m_bvh.Size : 0;
        }

        public MyMeshDrawTechnique GetDrawTechnique(int triangleIndex)
        {
            MyMeshDrawTechnique t = MyMeshDrawTechnique.MESH;

            for (int i = 0; i < m_meshContainer.Count; i++)
            {
                if (triangleIndex >= m_meshContainer[i].TriStart && triangleIndex < (m_meshContainer[i].TriStart + m_meshContainer[i].TriCount))
                    t = m_meshContainer[i].Material.DrawTechnique;
            }

            return t;
        }


        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            m_meshContainer.Clear();
        }

        //  Load only snappoints.
        public void LoadOnlyDummies()
        {
            if (!m_loadedData)
            {
                lock (this)
                {
                    MyLog.Default.WriteLine("MyModel.LoadSnapPoints -> START", LoggingOptions.LOADING_MODELS);
                    using (var indent = MyLog.Default.IndentUsing(LoggingOptions.LOADING_MODELS))
                    {
                        MyLog.Default.WriteLine("m_assetName: " + m_assetName, LoggingOptions.LOADING_MODELS);

                        //  Read data from model TAG parameter. There are stored vertex positions, triangle indices, vectors, ... everything we need.
                        MyModelImporter importer = new MyModelImporter();

                        MyLog.Default.WriteLine(String.Format("Importing asset {0}, path: {1}", m_assetName, AssetName), LoggingOptions.LOADING_MODELS);

                        try
                        {
                            // Read only TAG_DUMMIES data
                            importer.ImportData(AssetName, new string[] { MyImporterConstants.TAG_DUMMIES });
                        }
                        catch (Exception e)
                        {
                            MyLog.Default.WriteLine(String.Format("Importing asset failed {0}, message: {1}, stack:{2}", m_assetName, e.Message, e.StackTrace));
                        }

                        Dictionary<string, object> tagData = importer.GetTagData();

                        //Dummies = tagData[MyImporterConstants.TAG_DUMMIES] as Dictionary<string, MyModelDummy>;
                        if (tagData.Count > 0)
                        {
                            Dummies = tagData[MyImporterConstants.TAG_DUMMIES] as Dictionary<string, MyModelDummy>;
                        }
                        else
                        {
                            Dummies = new Dictionary<string, MyModelDummy>();
                        }
                    }
                }
            }
        }

        //  Load only snappoints.
        public void LoadOnlyModelInfo()
        {
            if (!m_loadedData)
            {
                lock (this)
                {
                    MyLog.Default.WriteLine("MyModel.LoadModelData -> START", LoggingOptions.LOADING_MODELS);
                    using (var indent = MyLog.Default.IndentUsing(LoggingOptions.LOADING_MODELS))
                    {
                        MyLog.Default.WriteLine("m_assetName: " + m_assetName, LoggingOptions.LOADING_MODELS);

                        //  Read data from model TAG parameter. There are stored vertex positions, triangle indices, vectors, ... everything we need.
                        MyModelImporter exporter = new MyModelImporter();

                        MyLog.Default.WriteLine(String.Format("Importing asset {0}, path: {1}", m_assetName, AssetName), LoggingOptions.LOADING_MODELS);

                        try
                        {
                            // Read only TAG_DUMMIES data
                            exporter.ImportData(AssetName, new string[] { MyImporterConstants.TAG_MODEL_INFO });
                        }
                        catch (Exception e)
                        {
                            MyLog.Default.WriteLine(String.Format("Importing asset failed {0}, message: {1}, stack:{2}", m_assetName, e.Message, e.StackTrace));
                        }

                        Dictionary<string, object> tagData = exporter.GetTagData();

                        if (tagData.Count > 0)
                        {
                            ModelInfo = tagData[MyImporterConstants.TAG_MODEL_INFO] as MyModelInfo;
                        }
                        else
                        {
                            ModelInfo = new MyModelInfo(0, 0, Vector3.Zero);
                        }
                    }
                }
            }
        }

        void IPrimitiveManagerBase.Cleanup()
        {
            //throw new NotImplementedException();
        }

        bool IPrimitiveManagerBase.IsTrimesh()
        {
            return true;
            //throw new NotImplementedException();
        }

        int IPrimitiveManagerBase.GetPrimitiveCount()
        {
            return this.m_trianglesCount;
            //throw new NotImplementedException();
        }

        void IPrimitiveManagerBase.GetPrimitiveBox(int prim_index, out AABB primbox)
        {
            BoundingBox bbox = BoundingBox.CreateInvalid();
            Vector3 v1 = GetVertex(Triangles[prim_index].I0);
            Vector3 v2 = GetVertex(Triangles[prim_index].I1);
            Vector3 v3 = GetVertex(Triangles[prim_index].I2);
            bbox.Include(
                ref v1,
                ref v2,
                ref v3);

            primbox = new AABB() { m_min = bbox.Min.ToBullet(), m_max = bbox.Max.ToBullet() };
        }

        void IPrimitiveManagerBase.GetPrimitiveTriangle(int prim_index, PrimitiveTriangle triangle)
        {
            triangle.m_vertices[0] = GetVertex(Triangles[prim_index].I0).ToBullet();
            triangle.m_vertices[1] = GetVertex(Triangles[prim_index].I1).ToBullet();
            triangle.m_vertices[2] = GetVertex(Triangles[prim_index].I2).ToBullet();
        }

        public void CheckLoadingErrors(MyModContext context, out bool errorFound)
        {
            if (ExportedWrong && m_loadingErrorProcessed == false)
            {
                errorFound = true;
                // generation of an error message delegated to the caller
                // Sandbox.Definitions.MyDefinitionErrors.Add(context, "There was error during loading of model, please check log file.", Sandbox.Definitions.TErrorSeverity.Error);
                m_loadingErrorProcessed = true;
            }
            else
            {
                errorFound = false;
            }
        }

        public void Rescale(float scaleFactor)
        {
            if (m_scaleFactor != scaleFactor)
            {
                float scaleChange = scaleFactor / m_scaleFactor;
                m_scaleFactor = scaleFactor;

                for (int i = 0; i < m_verticesCount; i++)
                {
                    Vector3 p = GetVertex(i) * scaleChange;
                    SetVertexPosition(i, ref p);
                }

                if (Dummies != null) 
                {
                    foreach (var dummy in Dummies)
                    {
                        var localMatrix = dummy.Value.Matrix;
                        localMatrix.Translation *= scaleChange;
                        dummy.Value.Matrix = localMatrix;
                    }
                }

                m_boundingBox.Min *= scaleChange;
                m_boundingBox.Max *= scaleChange;

                m_boundingBoxSize = BoundingBox.Max - BoundingBox.Min;
                m_boundingBoxSizeHalf = BoundingBoxSize / 2.0f;

                m_boundingSphere.Radius *= scaleChange;
            }
        }
    }

    //  Vertices and indices used for collision detection
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct MyCompressedVertexNormal
    {  //8 + 4 bytes
        public HalfVector4 Position;
        public Byte4 Normal;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MyCompressedBoneIndicesWeights
    {  //8 + 4 bytes
        public HalfVector4 Weights;
        public Byte4 Indices;
    }
}
