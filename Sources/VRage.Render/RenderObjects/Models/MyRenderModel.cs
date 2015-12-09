#region Using

using System;
using System.Collections.Generic;
using System.Linq;

using System.Diagnostics;
using System.IO;
using VRageMath.PackedVector;
using System.Runtime.InteropServices;
using VRageRender.Textures;
using VRageRender.Utils;
using VRageRender.Graphics;

//using VRageMath;
//using VRageMath.Graphics;

using SharpDX.Direct3D9;

using Vector2 = VRageMath.Vector2;
using Vector3 = VRageMath.Vector3;
using Vector4 = VRageMath.Vector4;
using BoundingBox = VRageMath.BoundingBox;
using BoundingSphere = VRageMath.BoundingSphere;
using VRage.Animations;
using VRage.Import;
using VRage.Utils;
using VRageMath;
using VRage;
using VRage.Library.Utils;
using VRage.FileSystem;

#endregion

//  Coordinate system transformation:
//  3DS MAX displays different coordinate system than XNA, plus when converting to FBX, it looks like it switches sign of Z values (Z in XNA way).
//  So when thinking about converting coordinate system, this is it:
//  XNA X = 3DSMAX X
//  XNA Y = 3DSMAX Z
//  XNA Z = NEGATIVE OF 3DSMAX Y

namespace VRageRender
{
    public struct MyRenderTriangleVertexIndices
    {
        public int I0, I1, I2;

        public MyRenderTriangleVertexIndices(int i0, int i1, int i2)
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

    class MyRenderModel : IDisposable
    {
        private const string C_POSTFIX_MASK = "_m";

        static MyModelImporter m_importer = new MyModelImporter();

        MyMeshDrawTechnique m_drawTechnique;
        VertexBuffer m_vertexBuffer = null;
        IndexBuffer m_indexBuffer = null;
        int m_verticesCount;
        int m_trianglesCount;
        int m_vertexBufferSize;
        int m_indexBufferSize;
        int m_vertexStride;
        VertexDeclaration m_vertexDeclaration;

        public ModelAnimations Animations;
        public Vector4I[] BoneIndices;
        public Vector4[] BoneWeights;
        public MyModelBone[] Bones;

        /// <summary>
        /// If true, entity uses directly materials from this model. If false, entity uses cloned materials from this model.
        /// </summary>
        public bool HasSharedMaterials = false;

        public int GetVBSize
        {
            get { return m_vertexBufferSize; }
        }
        public int GetIBSize
        {
            get { return m_indexBufferSize; }
        }

        //  Vertices and indices used for collision detection
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct MyCompressedVertexNormal
        {  //8 + 4 bytes
            public HalfVector4 Position;
            public Byte4 Normal;
        }

        private MyCompressedVertexNormal[] m_vertices;


        private int[] m_Indices = null;
        private ushort[] m_Indices_16bit = null;

        public MyModelInfo ModelInfo;

        public List<MyLODDescriptor> LODs = new List<MyLODDescriptor>();

        private volatile LoadState m_loadState;

        /// <summary>
        /// Gets or sets the state of the load.
        /// </summary>
        public LoadState LoadState
        {
            get
            {
                return this.m_loadState;
            }
            internal set
            {
                this.m_loadState = value;
            }
        }

        public int GetVertexStride()
        {
            return m_vertexStride;
        }

        public VertexDeclaration GetVertexDeclaration()
        {
            return m_vertexDeclaration;
        }

        public Vector3 GetVertexInt(int vertexIndex)
        {
            return VF_Packer.UnpackPosition(ref m_vertices[vertexIndex].Position);
        }

        public Vector3 GetVertex(int vertexIndex)
        {
            return GetVertexInt(vertexIndex);
        }

        public void GetVertex(int vertexIndex1, int vertexIndex2, int vertexIndex3, out Vector3 v1, out Vector3 v2, out Vector3 v3)
        {
            v1 = GetVertex(vertexIndex1);
            v2 = GetVertex(vertexIndex2);
            v3 = GetVertex(vertexIndex3);
        }

        public Vector3 GetVertexNormal(int vertexIndex)
        {
            return VF_Packer.UnpackNormal(ref m_vertices[vertexIndex].Normal);
        }

        public Vector3 GetVertexTangent(int vertexIndex)
        {
            return VF_Packer.UnpackNormal(ref m_forLoadingTangents[vertexIndex]);
        }

        public MyRenderTriangleVertexIndices GetTriangle(int index)
        {
            System.Diagnostics.Debug.Assert((0 <= index) && (index < GetTrianglesCount()), "Invalid index!");
            if (m_Indices != null)
                return (new MyRenderTriangleVertexIndices(m_Indices[index * 3 + 0], m_Indices[index * 3 + 2], m_Indices[index * 3 + 1]));
            else if (m_Indices_16bit != null)
                return (new MyRenderTriangleVertexIndices(m_Indices_16bit[index * 3 + 0], m_Indices_16bit[index * 3 + 2], m_Indices_16bit[index * 3 + 1]));
            else
                throw new InvalidBranchException(); // Neither 32bit or 16bit indices are set, probably already called mesh.DisposeIndices()
        }

        //  Used only for loading and then disposed. It lives between LoadData and LoadInDraw... but that's OK because it's only during sector loading
        //  and I have to make it so because we can't load vertex/index buffers from other place than Draw call
        HalfVector2[] m_forLoadingTexCoords0;
        Byte4[] m_forLoadingTangents;
        public HalfVector2[] ForLoadingTexCoords0 { get { return m_forLoadingTexCoords0; } }

        float m_specularShininess;
        float m_specularPower;
        float m_rescaleFactor;

        //  Bounding volumes
        public BoundingSphere BoundingSphere;   //TODO: Could be made readonly from the outside, and alterable only from the inside of this class
        public BoundingBox BoundingBox;         //TODO: Could be made readonly from the outside, and alterable only from the inside of this class

        //  Size of the bounding box
        public Vector3 BoundingBoxSize;         //TODO: Could be made readonly from the outside, and alterable only from the inside of this class
        public Vector3 BoundingBoxSizeHalf;     //TODO: Could be made readonly from the outside, and alterable only from the inside of this class
        public float PatternScale = 1.0f;

        readonly string m_assetName;
        bool m_loadedData;
        bool m_loadedContent;


        public bool LoadedData
        {
            get { return m_loadedData; }
        }

        public bool LoadedContent
        {
            get { return m_loadedContent; }
        }

        List<MyRenderMesh> m_meshContainer = new List<MyRenderMesh>();

        /// <summary>
        /// c-tor - this constructor should be used just for max models - not voxels!
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="unloadableModel"></param>
        /// <param name="drawTechnique"></param>
        /// <param name="modelEnum"></param>
        public MyRenderModel(string assetName, MyMeshDrawTechnique drawTechnique)
        {
            m_assetName = assetName;
            m_loadedData = false;
            m_loadedContent = false;
            m_drawTechnique = drawTechnique;

            var fsPath = Path.IsPathRooted(AssetName) ? AssetName : Path.Combine(MyFileSystem.ContentPath, AssetName);
            System.Diagnostics.Debug.Assert(MyFileSystem.FileExists(fsPath), "Model data for " + m_assetName + " does not exists!");

            LoadState = Textures.LoadState.Unloaded;
        }

        public MyRenderModel(MyMeshDrawTechnique drawTechnique)
        {
            m_loadedData    = false;
            m_loadedContent = false;
            m_drawTechnique = drawTechnique;
            LoadState       = Textures.LoadState.Unloaded;
        }

        public List<MyRenderMesh> GetMeshList()
        {
            return m_meshContainer;
        }

        //  Sort of lazy-load, where constructor just saves information about what this model should be, but real load is done here - and only one time.
        //  This loads only vertex data, doesn't touch GPU
        //  Can be called from main and background thread
        public void LoadData()
        {
            if (m_loadedData) return;
            if (m_loadedContent) return;

            lock (this)
            {
                if (m_loadedData) return;
                if (m_loadedContent) return;

                MyRender.GetRenderProfiler().StartProfilingBlock("MyModel::LoadData");


                MyRender.Log.WriteLine("MyModel.LoadData -> START", LoggingOptions.LOADING_MODELS);
                MyRender.Log.IncreaseIndent(LoggingOptions.LOADING_MODELS);

                MyRender.Log.WriteLine("m_assetName: " + m_assetName, LoggingOptions.LOADING_MODELS);

                //  Read data from model TAG parameter. There are stored vertex positions, triangle indices, vectors, ... everything we need.
                MyRender.GetRenderProfiler().StartProfilingBlock("Model - load data - import data");

                string assetForImport = AssetName;
                var fsPath = Path.IsPathRooted(AssetName) ? AssetName : Path.Combine(MyFileSystem.ContentPath, AssetName);
                if (!MyFileSystem.FileExists(fsPath))
                {
                    MyRender.Log.WriteLine("ERROR: Asset " + AssetName + "not exists!");
                    assetForImport = @"Models\Debug\Error.mwm";
                }

                MyRender.Log.WriteLine(String.Format("Importing asset {0}, path: {1}", assetForImport, AssetName), LoggingOptions.LOADING_MODELS);
                try
                {
                    m_importer.ImportData(assetForImport);
                }
                catch
                {
                    MyRender.Log.WriteLine(String.Format("Importing asset failed {0}", m_assetName));
                    throw;
                }
                MyRender.GetRenderProfiler().EndProfilingBlock();

                MyRender.GetRenderProfiler().StartProfilingBlock("Model - load data - load tag data");
                Dictionary<string, object> tagData = m_importer.GetTagData();
                if (tagData.Count == 0)
                {
                    throw new Exception(String.Format("Uncompleted tagData for asset: {0}, path: {1}", m_assetName, AssetName));
                }
                MyRender.GetRenderProfiler().EndProfilingBlock();

                MyRender.GetRenderProfiler().StartProfilingBlock("Model - load data - vertex, normals, texture coords");

                object patternScale;
                if (tagData.TryGetValue(MyImporterConstants.TAG_PATTERN_SCALE, out patternScale))
                {
                    PatternScale = (float)patternScale;
                }

                HalfVector4[] vertices = (HalfVector4[])tagData[MyImporterConstants.TAG_VERTICES];

                //Dont assert, it can be animation
                //System.Diagnostics.Debug.Assert(vertices.Length > 0);

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

                HalfVector2[] forLoadingTexCoords0 = (HalfVector2[])tagData[MyImporterConstants.TAG_TEXCOORDS0];
                m_forLoadingTexCoords0 = new HalfVector2[forLoadingTexCoords0.Length];
                for (int t = 0; t < forLoadingTexCoords0.Length; t++)
                {
                    m_forLoadingTexCoords0[t] = forLoadingTexCoords0[t];// new HalfVector2(forLoadingTexCoords0[t]);
                    m_forLoadingTexCoords0[t] = new HalfVector2(m_forLoadingTexCoords0[t].ToVector2() / PatternScale);
                }

                MyRender.GetRenderProfiler().EndProfilingBlock();

                MyRender.GetRenderProfiler().StartProfilingBlock("Model - load data - mesh");
                m_meshContainer.Clear();

                if (tagData.ContainsKey(MyImporterConstants.TAG_MESH_PARTS))
                {
                    List<int> indices = new List<int>(GetVerticesCount()); // Default capacity estimation
                    int maxIndex = 0;

                    List<MyMeshPartInfo> meshParts = tagData[MyImporterConstants.TAG_MESH_PARTS] as List<MyMeshPartInfo>;
                    foreach (MyMeshPartInfo meshPart in meshParts)
                    {
                        if (meshPart.m_MaterialDesc != null)
                            MyRenderModels.Materials[meshPart.m_MaterialDesc.MaterialName] = meshPart.m_MaterialDesc;

                        MyRenderMesh mesh = new MyRenderMesh(meshPart, m_assetName);

                        mesh.IndexStart = indices.Count;
                        mesh.TriCount = meshPart.m_indices.Count / 3;

                        System.Diagnostics.Debug.Assert(mesh.TriCount > 0);

                        foreach (var i in meshPart.m_indices)
                        {
                            indices.Add(i);
                            if (i > maxIndex)
                            {
                                maxIndex = i;
                            }
                        }

                        m_meshContainer.Add(mesh);

                        if (meshPart.m_MaterialDesc != null && meshPart.Technique == MyMeshDrawTechnique.GLASS)
                        {
                            float minimumGlassShadow = 0.0f;

                            if (string.IsNullOrEmpty(meshPart.m_MaterialDesc.GlassCW))
                                continue;

                            if (string.IsNullOrEmpty(meshPart.m_MaterialDesc.GlassCCW))
                                continue;

                            var materialCW = MyTransparentMaterials.GetMaterial(meshPart.m_MaterialDesc.GlassCW);
                            var materialCCW = MyTransparentMaterials.GetMaterial(meshPart.m_MaterialDesc.GlassCCW);

                            mesh.GlassDithering = System.Math.Max(materialCW.Color.W, minimumGlassShadow);

                            MyRenderMesh glassMesh = new MyRenderMesh(meshPart, m_assetName);
                            glassMesh.GlassDithering = System.Math.Max(materialCCW.Color.W, minimumGlassShadow);


                            glassMesh.IndexStart = indices.Count;
                            glassMesh.TriCount = meshPart.m_indices.Count / 3;

                            System.Diagnostics.Debug.Assert(glassMesh.TriCount > 0);

                            for (int i = 0; i < meshPart.m_indices.Count; i += 3)
                            {
                                indices.Add(meshPart.m_indices[i + 0]);
                                indices.Add(meshPart.m_indices[i + 2]);
                                indices.Add(meshPart.m_indices[i + 1]);
                            }

                            m_meshContainer.Add(glassMesh);
                        }
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

                    m_trianglesCount = indices.Count / 3;
                }


                MyRender.GetRenderProfiler().EndProfilingBlock();

                MyRender.GetRenderProfiler().StartProfilingBlock("Model - load data - other data");
                if (MyRenderConstants.RenderQualityProfile.UseNormals && m_forLoadingTexCoords0.Length > 0)
                {
                    var verticesNum = vertices.Length;
                    
                    Byte4[] forLoadingTangents = (Byte4[])tagData[MyImporterConstants.TAG_TANGENTS];
                    Byte4[] forLoadingBitangents = (Byte4[])tagData[MyImporterConstants.TAG_BINORMALS];
                    m_forLoadingTangents = new Byte4[forLoadingTangents.Length];

                    for (int v = 0; v < forLoadingTangents.Length; v++)
                    {
                        var N = VF_Packer.UnpackNormal(m_vertices[v].Normal.PackedValue);
                        var T = VF_Packer.UnpackNormal(forLoadingTangents[v].PackedValue);
                        var B = VF_Packer.UnpackNormal(forLoadingBitangents[v].PackedValue);

                        var tangentSign = new Vector4(T.X, T.Y, T.Z, 0);
                        tangentSign.W = T.Cross(N).Dot(B) < 0 ? -1 : 1;

                        m_forLoadingTangents[v] = VF_Packer.PackTangentSignB4(ref tangentSign);
                    }
                }

                m_specularShininess = (float)tagData[MyImporterConstants.TAG_SPECULAR_SHININESS];
                m_specularPower = (float)tagData[MyImporterConstants.TAG_SPECULAR_POWER];
                m_rescaleFactor = (float)tagData[MyImporterConstants.TAG_RESCALE_FACTOR];

                BoneIndices = (Vector4I[])tagData[MyImporterConstants.TAG_BLENDINDICES];
                BoneWeights = (Vector4[])tagData[MyImporterConstants.TAG_BLENDWEIGHTS];

                Animations = (ModelAnimations)tagData[MyImporterConstants.TAG_ANIMATIONS];
                Bones = (MyModelBone[])tagData[MyImporterConstants.TAG_BONES];
                
                if(BoneIndices.Length > 0 && Bones.Length > MyRenderConstants.MAX_SHADER_BONES)
                {
                    List<MyMeshPartInfo> meshParts = tagData[MyImporterConstants.TAG_MESH_PARTS] as List<MyMeshPartInfo>;

                    Dictionary<int, int> vertexChanged = new Dictionary<int,int>();
                    for(int p=0; p<meshParts.Count; p++)
                    {
                        var meshPart = meshParts[p];

                        Dictionary<int, int> bonesUsed = new Dictionary<int, int>();

                        int trianglesNum = meshPart.m_indices.Count / 3;
                        for (int i = 0; i < trianglesNum; i++)
                        {
                            for (int j = 0; j < 3; j++)
                            {
                                int index = meshPart.m_indices[i * 3 + j];
                                if(BoneWeights[index].X > 0)
                                    bonesUsed[BoneIndices[index].X] = 1;
                                if (BoneWeights[index].Y > 0)
                                    bonesUsed[BoneIndices[index].Y] = 1;
                                if (BoneWeights[index].Z > 0)
                                    bonesUsed[BoneIndices[index].Z] = 1;
                                if (BoneWeights[index].W > 0)
                                    bonesUsed[BoneIndices[index].W] = 1;
                            }
                        }

                        var partBones = new List<int>(bonesUsed.Keys);
                        partBones.Sort();
                        if (partBones.Count > 0 && partBones[partBones.Count - 1] >= MyRenderConstants.MAX_SHADER_BONES)
                        {
                            for(int i=0; i<partBones.Count; i++)
                            {
                                bonesUsed[partBones[i]] = i;
                            }

                            Dictionary<int, int> vertexTouched = new Dictionary<int, int>();

                            for (int i = 0; i < trianglesNum; i++)
                            {
                                for (int j = 0; j < 3; j++)
                                {
                                    int index = meshPart.m_indices[i * 3 + j];
                                    if(!vertexTouched.ContainsKey(index))
                                    { 
                                        if (BoneWeights[index].X > 0)
                                            BoneIndices[index].X = bonesUsed[BoneIndices[index].X];
                                        if (BoneWeights[index].Y > 0)
                                            BoneIndices[index].Y = bonesUsed[BoneIndices[index].Y];
                                        if (BoneWeights[index].Z > 0)
                                            BoneIndices[index].Z = bonesUsed[BoneIndices[index].Z];
                                        if (BoneWeights[index].W > 0)
                                            BoneIndices[index].W = bonesUsed[BoneIndices[index].W];

                                        vertexTouched[index] = 1;

                                        int changes = 0;
                                        vertexChanged.TryGetValue(index, out changes);
                                        vertexChanged[index] = changes + 1;
                                    }
                                }
                            }

                            m_meshContainer[p].BonesUsed = partBones.ToArray();
                        }
                    }

                    if (vertexChanged.Values.Count > 0)
                        Debug.Assert(vertexChanged.Values.Max() < 2, "Vertex shared between model parts, will likely result in wrong skinning");
                }

                BoundingBox = (BoundingBox)tagData[MyImporterConstants.TAG_BOUNDING_BOX];
                BoundingSphere = (BoundingSphere)tagData[MyImporterConstants.TAG_BOUNDING_SPHERE];
                BoundingBoxSize = BoundingBox.Max - BoundingBox.Min;
                BoundingBoxSizeHalf = BoundingBoxSize / 2.0f;
                Dictionary<string, MyModelDummy> Dummies = tagData[MyImporterConstants.TAG_DUMMIES] as Dictionary<string, MyModelDummy>;
                MyRender.GetRenderProfiler().EndProfilingBlock();

                if (tagData.ContainsKey(MyImporterConstants.TAG_LODS))
                {
                    var tagLODs = tagData[MyImporterConstants.TAG_LODS];

                    LODs.Clear();
                    LODs.AddArray((MyLODDescriptor[])tagLODs);

                    foreach (var lodDesc in LODs)
                    {
                        if (!string.IsNullOrEmpty(lodDesc.RenderQuality))
                        {
                            lodDesc.RenderQualityList = new List<int>();
                            string[] qualityStrings = lodDesc.RenderQuality.ToUpper().Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries);
                            foreach (string qs in qualityStrings)
                            {
                                string qs2 = qs.Trim();
                                if (qs2 == "LOW")
                                    lodDesc.RenderQualityList.Add((int)MyRenderQualityEnum.LOW);
                                else
                                    if (qs2 == "NORMAL")
                                        lodDesc.RenderQualityList.Add((int)MyRenderQualityEnum.NORMAL);
                                    else
                                        if (qs2 == "HIGH")
                                            lodDesc.RenderQualityList.Add((int)MyRenderQualityEnum.HIGH);
                                        else
                                            if (qs2 == "EXTREME")
                                                lodDesc.RenderQualityList.Add((int)MyRenderQualityEnum.EXTREME);
                            }
                        }
                    }
                }


                MyRender.Log.WriteLine("Vertexes.Length: " + GetVerticesCount(), LoggingOptions.LOADING_MODELS);
                MyRender.Log.WriteLine("Centered: " + (bool)tagData[MyImporterConstants.TAG_CENTERED], LoggingOptions.LOADING_MODELS);
                MyRender.Log.WriteLine("UseChannelTextures: " + (bool)tagData[MyImporterConstants.TAG_USE_CHANNEL_TEXTURES], LoggingOptions.LOADING_MODELS);
                MyRender.Log.WriteLine("Length in meters: " + (float)tagData[MyImporterConstants.TAG_LENGTH_IN_METERS], LoggingOptions.LOADING_MODELS);
                MyRender.Log.WriteLine("Rescale to length in meters?: " + (bool)tagData[MyImporterConstants.TAG_RESCALE_TO_LENGTH_IN_METERS], LoggingOptions.LOADING_MODELS);
                MyRender.Log.WriteLine("SpecularShininess: " + m_specularShininess, LoggingOptions.LOADING_MODELS);
                MyRender.Log.WriteLine("SpecularPower: " + m_specularPower, LoggingOptions.LOADING_MODELS);
                MyRender.Log.WriteLine("RescaleFactor: " + m_rescaleFactor, LoggingOptions.LOADING_MODELS);
                MyRender.Log.WriteLine("BoundingBox: " + BoundingBox, LoggingOptions.LOADING_MODELS);
                MyRender.Log.WriteLine("BoundingSphere: " + BoundingSphere, LoggingOptions.LOADING_MODELS);

                MyPerformanceCounter.PerAppLifetime.MyModelsCount++;
                MyPerformanceCounter.PerAppLifetime.MyModelsMeshesCount += m_meshContainer.Count;
                MyPerformanceCounter.PerAppLifetime.MyModelsVertexesCount += GetVerticesCount();
                MyPerformanceCounter.PerAppLifetime.MyModelsTrianglesCount += m_trianglesCount;

                ModelInfo = new MyModelInfo(GetTrianglesCount(), GetVerticesCount(), BoundingBoxSize);

                m_loadedData = true;

                m_importer.Clear();

                MyRender.Log.DecreaseIndent(LoggingOptions.LOADING_MODELS);
                MyRender.Log.WriteLine("MyModel.LoadData -> END", LoggingOptions.LOADING_MODELS);

                MyRender.GetRenderProfiler().EndProfilingBlock();
            }
        }



        public void LoadMaterials()
        {
            lock (this)
            {
                MyRender.GetRenderProfiler().StartProfilingBlock("MyModel::LoadData");


                MyRender.Log.WriteLine("MyModel.LoadData -> START", LoggingOptions.LOADING_MODELS);
                MyRender.Log.IncreaseIndent(LoggingOptions.LOADING_MODELS);

                MyRender.Log.WriteLine("m_assetName: " + m_assetName, LoggingOptions.LOADING_MODELS);

                //  Read data from model TAG parameter. There are stored vertex positions, triangle indices, vectors, ... everything we need.
                MyRender.GetRenderProfiler().StartProfilingBlock("Model - load data - import data");

                string assetForImport = AssetName;
                var fsPath = Path.IsPathRooted(AssetName) ? AssetName : Path.Combine(MyFileSystem.ContentPath, AssetName);
                if (!MyFileSystem.FileExists(fsPath))
                {
                    MyRender.Log.WriteLine("ERROR: Asset " + AssetName + "not exists!");
                    assetForImport = @"Models\Debug\Error.mwm";
                }

                MyRender.Log.WriteLine(String.Format("Importing asset {0}, path: {1}", assetForImport, AssetName), LoggingOptions.LOADING_MODELS);
                try
                {
                    m_importer.ImportData(assetForImport);
                }
                catch
                {
                    MyRender.Log.WriteLine(String.Format("Importing asset failed {0}", m_assetName));
                    throw;
                }
                MyRender.GetRenderProfiler().EndProfilingBlock();

                MyRender.GetRenderProfiler().StartProfilingBlock("Model - load data - load tag data");
                Dictionary<string, object> tagData = m_importer.GetTagData();
                if (tagData.Count == 0)
                {
                    throw new Exception(String.Format("Uncompleted tagData for asset: {0}, path: {1}", m_assetName, AssetName));
                }
                MyRender.GetRenderProfiler().EndProfilingBlock();

                MyRender.GetRenderProfiler().StartProfilingBlock("Model - load data - mesh");
                m_meshContainer.Clear();

                if (tagData.ContainsKey(MyImporterConstants.TAG_MESH_PARTS))
                {
                    List<int> indices = new List<int>(GetVerticesCount()); // Default capacity estimation

                    List<MyMeshPartInfo> meshParts = tagData[MyImporterConstants.TAG_MESH_PARTS] as List<MyMeshPartInfo>;
                    foreach (MyMeshPartInfo meshPart in meshParts)
                    {
                        if (meshPart.m_MaterialDesc != null)
                            MyRenderModels.Materials[meshPart.m_MaterialDesc.MaterialName] = meshPart.m_MaterialDesc;

                        MyRenderMesh mesh = new MyRenderMesh(meshPart, m_assetName);

                        mesh.IndexStart = indices.Count;
                        mesh.TriCount = meshPart.m_indices.Count / 3;

                        System.Diagnostics.Debug.Assert(mesh.TriCount > 0);

                        m_meshContainer.Add(mesh);
                    }
                }


                PreloadTextures(LoadingMode.Immediate);

                m_importer.Clear();

                MyRender.Log.DecreaseIndent(LoggingOptions.LOADING_MODELS);
                MyRender.Log.WriteLine("MyModel.LoadData -> END", LoggingOptions.LOADING_MODELS);

                MyRender.GetRenderProfiler().EndProfilingBlock();
            }
        }


        //
        //
        public void LoadContent()
        {
            //If model was loaded previously, we need it reload because it has some temporary data discarded
            //otherwise model wont load after device reset
            // We must also reload when tangents are missing.
            /*
            if (m_loadedData && (m_forLoadingTexCoords0 == null || m_forLoadingTangents == null || m_meshContainer.Count == 0))
            {
                m_loadedData = false;
                LoadData();
            }  */

            LoadInDraw();


        }



        private void CreateRenderDataForMesh()
        {
            // Creating vertex and index buffer
            //  Write to GPU
            CreateVertexBuffer();
            CreateIndexBuffer();
        }

        //  Loads vertex/index buffers and textures, access GPU
        //  Should be called only from Draw method on main thread
        public void LoadInDraw(LoadingMode loadingMode = LoadingMode.Immediate)
        {
            //  If already loaded into GPU
            if (m_loadedContent)
                return;

            //  If this model wasn't loaded through lazy-load then it means we don't need it in this game/sector, and we
            //  don't need to load him into GPU
            if (m_loadedData == false)
                return;

            if (LoadState == Textures.LoadState.Loading)
                return;


            if (loadingMode == LoadingMode.Background)
            {
                //todo
                System.Diagnostics.Debug.Assert(false);
                //MyModels.LoadModelInDrawInBackground(this);
                return;
            }

            if (m_verticesCount == 0)
            {
                LoadState = Textures.LoadState.Error;
                MyRender.Log.WriteLine("ERROR: Attempted to load model " + AssetName + " with zero vertices", LoggingOptions.LOADING_MODELS);
                return;
            }


            Debug.Assert(m_forLoadingTexCoords0 != null && m_meshContainer.Count != 0, "Somebody forget to call LoadData on model before rendering");

            MyRender.GetRenderProfiler().StartProfilingBlock("MyModel::LoadInDraw");


            // Creating
            CreateRenderDataForMesh();
            PreloadTextures(LoadingMode.Immediate);

            m_loadedContent = true;

            //We can do this in render
            UnloadTemporaryData();

            LoadState = Textures.LoadState.Loaded;

            MyRender.GetRenderProfiler().EndProfilingBlock();
        }

        void CreateVertexBuffer()
        {
            //  Create vertex buffer - vertex format type depends on draw technique

            switch (m_drawTechnique)
            {
                case MyMeshDrawTechnique.MESH:
                case MyMeshDrawTechnique.DECAL:
                case MyMeshDrawTechnique.HOLO:
                case MyMeshDrawTechnique.ALPHA_MASKED:
                case MyMeshDrawTechnique.SKINNED:
                    {
                        if (m_forLoadingTexCoords0 == null) throw new Exception("Model '" + m_assetName + "' doesn't have texture channel 0 specified, but this shader requires it");

                        if (m_forLoadingTexCoords0.Length == 0)
                        {
                            MyVertexFormatPosition[] vertexArray = new MyVertexFormatPosition[GetVerticesCount()];
                            for (int i = 0; i < GetVerticesCount(); i++)
                            {
                                vertexArray[i].Position = m_vertices[i].Position;
                            }

                            m_vertexDeclaration = MyVertexFormatPosition.VertexDeclaration;
                            m_vertexStride = MyVertexFormatPosition.Stride;
                            m_vertexBufferSize = vertexArray.Length * m_vertexStride;
                            m_vertexBuffer = new VertexBuffer(MyRender.GraphicsDevice, m_vertexBufferSize, Usage.WriteOnly, VertexFormat.None, Pool.Default);
                            m_vertexBuffer.SetData(vertexArray);
                            m_vertexBuffer.Tag = this;
                        }
                        else
                            if (MyRenderConstants.RenderQualityProfile.UseNormals)
                            {
                                if (m_forLoadingTangents == null)
                                    throw new Exception("Model '" + m_assetName + "' doesn't have tangent vectors calculated, but this shader requires them");

                                if (BoneIndices.Length > 0)
                                {
                                    MyVertexFormatPositionNormalTextureTangentSkinned[] vertexArray = new MyVertexFormatPositionNormalTextureTangentSkinned[GetVerticesCount()];
                                    for (int i = 0; i < GetVerticesCount(); i++)
                                    {
                                        vertexArray[i].PositionPacked = m_vertices[i].Position;
                                        vertexArray[i].NormalPacked = m_vertices[i].Normal;
                                        vertexArray[i].TexCoordPacked = m_forLoadingTexCoords0[i];
                                        vertexArray[i].TangentPacked = m_forLoadingTangents[i];

                                        vertexArray[i].BoneIndices = new Byte4(BoneIndices[i].X, BoneIndices[i].Y, BoneIndices[i].Z, BoneIndices[i].W);
                                        vertexArray[i].BoneWeights = BoneWeights[i];

                                    }

                                    m_vertexDeclaration = MyVertexFormatPositionNormalTextureTangentSkinned.VertexDeclaration;
                                    m_vertexStride = MyVertexFormatPositionNormalTextureTangentSkinned.Stride;
                                    m_vertexBufferSize = vertexArray.Length * m_vertexStride;
                                    m_vertexBuffer = new VertexBuffer(MyRender.GraphicsDevice, m_vertexBufferSize, Usage.WriteOnly, VertexFormat.None, Pool.Default);
                                    m_vertexBuffer.SetData(vertexArray);
                                    m_vertexBuffer.Tag = this;
                                }
                                else
                                {

                                    MyVertexFormatPositionNormalTextureTangent[] vertexArray = new MyVertexFormatPositionNormalTextureTangent[GetVerticesCount()];
                                    for (int i = 0; i < GetVerticesCount(); i++)
                                    {
                                        vertexArray[i].PositionPacked = m_vertices[i].Position;
                                        vertexArray[i].NormalPacked = m_vertices[i].Normal;
                                        vertexArray[i].TexCoordPacked = m_forLoadingTexCoords0[i];
                                        vertexArray[i].TangentPacked = m_forLoadingTangents[i];
                                    }

                                    m_vertexDeclaration = MyVertexFormatPositionNormalTextureTangent.VertexDeclaration;
                                    m_vertexStride = MyVertexFormatPositionNormalTextureTangent.Stride;
                                    m_vertexBufferSize = vertexArray.Length * m_vertexStride;
                                    m_vertexBuffer = new VertexBuffer(MyRender.GraphicsDevice, m_vertexBufferSize, Usage.WriteOnly, VertexFormat.None, Pool.Default);
                                    m_vertexBuffer.SetData(vertexArray);
                                    m_vertexBuffer.Tag = this;
                                }

                            }
                            else
                            {

                                MyVertexFormatPositionNormalTexture[] vertexArray = new MyVertexFormatPositionNormalTexture[GetVerticesCount()];
                                for (int i = 0; i < GetVerticesCount(); i++)
                                {
                                    vertexArray[i].Position = GetVertexInt(i);
                                    vertexArray[i].Normal = GetVertexNormal(i);
                                    vertexArray[i].TexCoord = m_forLoadingTexCoords0[i].ToVector2();
                                }

                                m_vertexDeclaration = MyVertexFormatPositionNormalTexture.VertexDeclaration;
                                m_vertexStride = MyVertexFormatPositionNormalTexture.Stride;
                                m_vertexBufferSize = vertexArray.Length * m_vertexStride;
                                m_vertexBuffer = new VertexBuffer(MyRender.GraphicsDevice, m_vertexBufferSize, Usage.WriteOnly, VertexFormat.None, Pool.Default);
                                m_vertexBuffer.SetData(vertexArray);
                                m_vertexBuffer.Tag = this;

                            }
                    }
                    break;

                case MyMeshDrawTechnique.VOXELS_DEBRIS:
                    {
                        MyVertexFormatPositionNormal[] vertexArray = new MyVertexFormatPositionNormal[GetVerticesCount()];
                        for (int i = 0; i < GetVerticesCount(); i++)
                        {
                            vertexArray[i].Position = GetVertexInt(i);
                            vertexArray[i].Normal = GetVertexNormal(i);
                        }

                        m_vertexDeclaration = MyVertexFormatPositionNormal.VertexDeclaration;
                        m_vertexStride = MyVertexFormatPositionNormal.Stride;
                        m_vertexBufferSize = vertexArray.Length * m_vertexStride;
                        m_vertexBuffer = new VertexBuffer(MyRender.GraphicsDevice, m_vertexBufferSize, Usage.WriteOnly, VertexFormat.None, Pool.Default);
                        m_vertexBuffer.SetData(vertexArray);
                        m_vertexBuffer.Tag = this;
                    }
                    break;
                default:
                    {
                        throw new InvalidBranchException();
                    }
            }

            MyPerformanceCounter.PerAppLifetime.ModelVertexBuffersSize += m_vertexBufferSize;

            SignResource(m_vertexBuffer);
        }

        void CreateIndexBuffer()
        {
            MyDebug.AssertDebug(m_indexBuffer == null);

            if (m_Indices != null)
            {
                m_indexBuffer = new IndexBuffer(MyRender.GraphicsDevice, m_Indices.Length * sizeof(int), Usage.WriteOnly, Pool.Default, false);
                m_indexBuffer.SetData(m_Indices);
                m_indexBuffer.Tag = this;
                m_indexBufferSize = m_Indices.Length * sizeof(int);
            }
            else if (m_Indices_16bit != null)
            {
                m_indexBuffer = new IndexBuffer(MyRender.GraphicsDevice, m_Indices_16bit.Length * sizeof(short), Usage.WriteOnly, Pool.Default, true);
                m_indexBuffer.SetData(m_Indices_16bit);
                m_indexBuffer.Tag = this;
                m_indexBufferSize = m_Indices_16bit.Length * sizeof(short);
            }

            MyPerformanceCounter.PerAppLifetime.ModelIndexBuffersSize += m_indexBufferSize;

            SignResource(m_indexBuffer);
        }

        /// <summary>
        /// Signs the resource.
        /// </summary>
        /// <param name="indexBuffer">The index buffer.</param>
        [Conditional("DEBUG")]
        private void SignResource(IndexBuffer indexBuffer)
        {
            indexBuffer.DebugName = m_assetName + "_ib";
        }

        private void UnloadTemporaryData()
        {
            m_forLoadingTexCoords0 = null;
            m_forLoadingTangents = null;
            m_vertices = null;
            m_Indices_16bit = null;
            m_Indices = null;

            m_loadedData = false;
        }

        public bool UnloadData()
        {
            bool res = LoadState != Textures.LoadState.Unloaded;

            if (m_loadedContent)
                UnloadContent();

            MyPerformanceCounter.PerAppLifetime.MyModelsMeshesCount -= m_meshContainer.Count;
            if (m_vertices != null)
                MyPerformanceCounter.PerAppLifetime.MyModelsVertexesCount -= GetVerticesCount();
            if (res)
                MyPerformanceCounter.PerAppLifetime.MyModelsCount--;

            UnloadTemporaryData();

            LoadState = Textures.LoadState.Unloaded;

            m_meshContainer.Clear();

            return res;
        }

        public void UnloadContent()
        {
            if (m_vertexBuffer != null)
            {
                m_vertexBuffer.Dispose();
                m_vertexBuffer = null;
                MyPerformanceCounter.PerAppLifetime.ModelVertexBuffersSize -= m_vertexBufferSize;
                m_vertexBufferSize = 0;
            }

            if (m_indexBuffer != null)
            {
                m_indexBuffer.Dispose();
                m_indexBuffer = null;
                MyPerformanceCounter.PerAppLifetime.ModelIndexBuffersSize -= m_indexBufferSize;
                m_indexBufferSize = 0;
            }

            LoadState = Textures.LoadState.Unloaded;

            m_loadedContent = false;
        }

        public int GetTrianglesCount()
        {
            return m_trianglesCount;
        }

        public int GetVerticesCount()
        {
            return m_verticesCount;
        }

        public MyMeshDrawTechnique GetDrawTechnique()
        {
            return m_drawTechnique;
        }

        public void SetDrawTechnique(MyMeshDrawTechnique drawTechnique)
        {
            m_drawTechnique = drawTechnique;
        }

        public float GetSpecularShininess()
        {
            return m_specularShininess;
        }

        public float GetSpecularPower()
        {
            return m_specularPower;
        }

        public float GetRescaleFactor()
        {
            return m_rescaleFactor;
        }


        /// <summary>
        /// Render
        /// </summary>
        /// <param name="effect"></param>
        public void Render()
        {
            System.Diagnostics.Debug.Assert(m_vertexBuffer != null);

            Device device = MyRender.GraphicsDevice;
            device.SetStreamSource(0, m_vertexBuffer, 0, m_vertexStride);
            device.Indices = m_indexBuffer;
            device.VertexDeclaration = GetVertexDeclaration();

            foreach (MyRenderMesh mesh in m_meshContainer)
            {
                mesh.Render(device, m_verticesCount);
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            m_meshContainer.Clear();
            m_vertexBuffer.Dispose();
            m_vertexBuffer = null;
            MyPerformanceCounter.PerAppLifetime.ModelVertexBuffersSize -= m_vertexBufferSize;
            m_vertexBufferSize = 0;

            m_indexBuffer.Dispose();
            m_indexBuffer = null;
            MyPerformanceCounter.PerAppLifetime.ModelIndexBuffersSize -= m_indexBufferSize;
            m_indexBufferSize = 0;
        }

        /// <summary>
        /// File path of the model
        /// </summary>
        internal string AssetName
        {
            get { return m_assetName; }
        }

        internal VertexBuffer VertexBuffer
        {
            get { return m_vertexBuffer; }
        }

        internal IndexBuffer IndexBuffer
        {
            get { return m_indexBuffer; }
        }

        /// <summary>
        /// Signs the resource.
        /// </summary>
        /// <param name="vertexBuffer">The vertex buffer.</param>
        [Conditional("DEBUG")]
        private void SignResource(VertexBuffer vertexBuffer)
        {
            vertexBuffer.DebugName = m_assetName + "_vb";
        }

        public void PreloadTextures(LoadingMode loadingMode)
        {
            foreach (MyRenderMesh mesh in GetMeshList())
            {
                mesh.Material.PreloadTexture(loadingMode);
            }
        }

        public void CloneMaterials(List<MyRenderMeshMaterial> materials)
        {
            materials.Clear();
            foreach (MyRenderMesh mesh in GetMeshList())
            {
                materials.Add(mesh.Material.Clone());
            }
        }

        public void LoadBuffers(MyModelData modelData, string assetName = null)
        {
            System.Diagnostics.Debug.Assert(modelData.Sections.Count > 0, "Invalid object");
            if (modelData.Sections.Count == 0)
                return;

            // create index buffer
            {
                m_trianglesCount = modelData.Indices.Count / 3;
                m_Indices_16bit = new ushort[modelData.Indices.Count];

                for (int i = 0; i < modelData.Indices.Count; ++i)
                    m_Indices_16bit[i] = (ushort)modelData.Indices[i];

                m_indexBuffer = new IndexBuffer(MyRender.GraphicsDevice, m_Indices_16bit.Length * sizeof(short), Usage.WriteOnly, Pool.Default, true);
                m_indexBuffer.SetData(m_Indices_16bit);
                m_indexBuffer.Tag = this;
                m_indexBufferSize = m_Indices_16bit.Length * sizeof(short);

                SignResource(m_indexBuffer);
            }

            // create vertex buffer
            {
                m_verticesCount = modelData.Positions.Count;
                m_vertices      = new MyCompressedVertexNormal[m_verticesCount];
                var vertexArray = new MyVertexFormatPositionNormalTextureTangent[m_verticesCount];

                for (int i = 0; i < modelData.Positions.Count; ++i)
                {
                    vertexArray[i].Position = modelData.Positions[i];
                    vertexArray[i].Normal   = modelData.Normals[i];
                    vertexArray[i].Tangent  = modelData.Tangents[i];
                    vertexArray[i].TexCoord = modelData.TexCoords[i];

                    m_vertices[i] = new MyCompressedVertexNormal()
                    {
                        Position = vertexArray[i].PositionPacked,
                        Normal   = vertexArray[i].NormalPacked
                    };
                }

                m_vertexDeclaration = MyVertexFormatPositionNormalTextureTangent.VertexDeclaration;
                m_vertexStride      = MyVertexFormatPositionNormalTextureTangent.Stride;
                m_vertexBufferSize  = vertexArray.Length * m_vertexStride;
                m_vertexBuffer      = new VertexBuffer(MyRender.GraphicsDevice, m_vertexBufferSize, Usage.WriteOnly, VertexFormat.None, Pool.Default);
                m_vertexBuffer.SetData(vertexArray);
                m_vertexBuffer.Tag = this;

                SignResource(m_vertexBuffer);
            }

            m_meshContainer.Clear();

            // apply materials here
            for (int s = 0; s < modelData.Sections.Count; ++s)
            {
                var mpi            = new MyMeshPartInfo();
                mpi.Technique      = m_drawTechnique;
                
                // Disabled, because it assert always when models are loaded before materials
                //System.Diagnostics.Debug.Assert(MyRenderModels.Materials.ContainsKey(modelData.Sections[s].MaterialName), "Mesh material not present!");
                
                if (MyRenderModels.Materials.ContainsKey(modelData.Sections[s].MaterialName))
                    mpi.m_MaterialDesc = MyRenderModels.Materials[modelData.Sections[s].MaterialName];

                var start = modelData.Sections[s].IndexStart;
                var end   = start + modelData.Sections[s].TriCount*3;

                for (int i = start; i < end; ++i)
                    mpi.m_indices.Add(modelData.Indices[i]);

                m_meshContainer.Add(new MyRenderMesh(mpi, null)
                {
                    IndexStart = modelData.Sections[s].IndexStart,
                    TriCount   = modelData.Sections[s].TriCount
                });
            }

            // store properties of this model
            {
                BoundingBox    = modelData.AABB;
                BoundingSphere = new BoundingSphere(modelData.AABB.Center, modelData.AABB.HalfExtents.Length());

                BoundingBoxSize     = BoundingBox.Size;
                BoundingBoxSizeHalf = BoundingBox.HalfExtents;

                ModelInfo = new MyModelInfo(m_trianglesCount, m_verticesCount, BoundingBoxSize);

                PreloadTextures(LoadingMode.Immediate);
                LoadState = Textures.LoadState.Loaded;

                m_loadedContent = true;
                m_loadedData    = true;
            }
        }
    }
}
