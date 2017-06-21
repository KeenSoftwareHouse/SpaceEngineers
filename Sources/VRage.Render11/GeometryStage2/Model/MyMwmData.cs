using System;
using System.Collections;
using System.Collections.Generic;
using VRage.FileSystem;
using VRage.Import;
using VRageMath;
using VRageMath.PackedVector;
using VRageRender;
using VRageRender.Import;

namespace VRage.Render11.GeometryStage2.Model
{
    struct MyMwmData
    {
        class MyPartsComparer : IComparer<MyMeshPartInfo>
        {
            int IComparer<MyMeshPartInfo>.Compare(MyMeshPartInfo x, MyMeshPartInfo y)
            {
                return ((int)x.Technique).CompareTo((int)y.Technique);
            }
        }

        static readonly MyPartsComparer m_partsComparer = new MyPartsComparer();

        public string MwmFilepath { get; private set; }
        public string MwmContentPath { get; private set; }

        public MyLODDescriptor[] Lods { get; private set; }
        public List<MyMeshPartInfo> PartInfos { get; private set; }
        public List<MyMeshSectionInfo> SectionInfos { get; private set; }

        public int VerticesCount { get { return Positions.Length; } }
        public HalfVector4[] Positions { get; private set; }
        public Byte4[] Normals { get; private set; }
        public Byte4[] Tangents { get; private set; }
        public Byte4[] Bitangents { get; private set; }
        public HalfVector2[] Texcoords { get; private set; }

        public MyModelBone[] Bones { get; private set; }
        public Vector4I[] BoneIndices { get; private set; }
        public Vector4[] BoneWeights { get; private set; }
        
        public BoundingBox BoundindBox { get; private set; }
        public BoundingSphere BoundingSphere { get; private set; }

        public bool HasBones
        {
            get
            {
                return BoneIndices.Length > 0 || BoneWeights.Length > 0 || BoneIndices.Length == VerticesCount;
            }
        }
        
        public bool IsAnimated
        {
            get
            {
                return BoneIndices.Length > 0 && BoneWeights.Length > 0 && BoneIndices.Length == VerticesCount && BoneWeights.Length == VerticesCount;
            }
        }

        public bool IsValid2ndStream
        {
            get
            {
                return
                    ((Normals.Length > 0) && Normals.Length == VerticesCount) &&
                    ((Texcoords.Length > 0) && Texcoords.Length == VerticesCount) &&
                    ((Tangents.Length > 0) && Tangents.Length == VerticesCount) &&
                    ((Bitangents.Length > 0) && Bitangents.Length == VerticesCount);
            }
        }

        public bool ContainsGlass
        {
            get
            {
                foreach(var part in PartInfos)
                    if (part.Technique == MyMeshDrawTechnique.GLASS)
                        return true;
                return false;
            }
        }

        static MyModelImporter GetModelImporter(string mwmFilepath)
        {
            MyModelImporter importer = new MyModelImporter();

            importer.ImportData(mwmFilepath, new[]
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
            return importer;
        }

        // the functionality is not clear to me, the code is legacy from the old pipeline
        Byte4[] CreateAlteredTangents(Byte4[] normals, Byte4[] tangents, Byte4[] bitangents)
        {
            int verticesNum = tangents.Length;
            var alteredTangents = new Byte4[verticesNum];

            if (tangents.Length > 0 && bitangents.Length > 0)
            {
                MyRenderProxy.Assert(normals.Length == tangents.Length && normals.Length == bitangents.Length);

                // calculate tangents used by run-time
                for (int i = 0; i < verticesNum; i++)
                {
                    var N = VF_Packer.UnpackNormal(normals[i].PackedValue);
                    var T = VF_Packer.UnpackNormal(tangents[i].PackedValue);
                    var B = VF_Packer.UnpackNormal(bitangents[i].PackedValue);

                    var tanW = new Vector4(T.X, T.Y, T.Z, 0);
                    tanW.W = T.Cross(N).Dot(B) < 0 ? -1 : 1;

                    alteredTangents[i] = VF_Packer.PackTangentSignB4(ref tanW);
                }
            }

            return alteredTangents;
        }

        public bool LoadFromFile(string mwmFilepath)
        {
            MwmFilepath = MyMwmUtils.GetFullMwmFilepath(mwmFilepath);
            MwmContentPath = MyMwmUtils.GetFullMwmContentPath(mwmFilepath);
            if (!MyFileSystem.FileExists(MwmFilepath))
            {
                MyRender11.Log.WriteLine(String.Format("Mesh asset {0} missing", MwmFilepath));
                return false;
            }

            MyModelImporter modelImporter = GetModelImporter(MwmFilepath);
            Dictionary<string, object> tagData = modelImporter.GetTagData();

            // Lods
            if (tagData.ContainsKey(MyImporterConstants.TAG_LODS))
                Lods = (MyLODDescriptor[])tagData[MyImporterConstants.TAG_LODS];

            // Parts
            MyRenderProxy.Assert(tagData.ContainsKey(MyImporterConstants.TAG_MESH_PARTS));
            PartInfos = tagData[MyImporterConstants.TAG_MESH_PARTS] as List<MyMeshPartInfo>;
            // Sort parts
            PartInfos.Sort(m_partsComparer);

            // Sections
            if (tagData.ContainsKey(MyImporterConstants.TAG_MESH_SECTIONS))
                SectionInfos = tagData[MyImporterConstants.TAG_MESH_SECTIONS] as List<MyMeshSectionInfo>;

            // Common buffers
            Positions = (HalfVector4[])tagData[MyImporterConstants.TAG_VERTICES];
            Normals = (Byte4[])tagData[MyImporterConstants.TAG_NORMALS];
            Tangents = (Byte4[])tagData[MyImporterConstants.TAG_TANGENTS];
            Bitangents = (Byte4[])tagData[MyImporterConstants.TAG_BINORMALS];
            Texcoords = (HalfVector2[])tagData[MyImporterConstants.TAG_TEXCOORDS0];

            // Animation
            Bones = (MyModelBone[])tagData[MyImporterConstants.TAG_BONES];
            BoneIndices = (Vector4I[])tagData[MyImporterConstants.TAG_BLENDINDICES];
            BoneWeights = (Vector4[])tagData[MyImporterConstants.TAG_BLENDWEIGHTS];

            object objectPatternScale;
            float patternScale = 1f;
            if (tagData.TryGetValue(MyImporterConstants.TAG_PATTERN_SCALE, out objectPatternScale))
                patternScale = (float)objectPatternScale;

            // Data validation
            BoundindBox = (BoundingBox)tagData[MyImporterConstants.TAG_BOUNDING_BOX];
            BoundingSphere = (BoundingSphere)tagData[MyImporterConstants.TAG_BOUNDING_SPHERE];

            if (patternScale != 1f && Texcoords.Length > 0)
                for (int i = 0; i < Texcoords.Length; ++i)
                    Texcoords[i] = new HalfVector2(Texcoords[i].ToVector2() / patternScale);

            if (Normals.Length > 0 && Tangents.Length > 0 && Bitangents.Length > 0)
                Tangents = CreateAlteredTangents(Normals, Tangents, Bitangents);

            modelImporter.Clear();
            return true;
        }
    }

}
