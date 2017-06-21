using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using VRageMath;

namespace VRageRender.Import
{
    //  IMPORTANT: If you add/delete technique in this enum, don't forget to change code
    //  in these methods: 
    //      MyModel.CreateVertexBuffer
    [Obfuscation(Feature = Obfuscator.NoRename, ApplyToMembers = true, Exclude = true)]
    public enum MyMeshDrawTechnique : byte
    {
        MESH,                      //  Renders using diffuse, normal map and specular textures
        VOXELS_DEBRIS,              //  For explosion debris objects, with scaling and texture is calculated by tri-planar mapping (same as with voxel maps)
        VOXEL_MAP,                  //  Destroyable voxel asteroid
        ALPHA_MASKED,               //  Alpha masked object
        FOLIAGE,                    // Alpha masked foliage

        //Leave decal type last because it is alpha blended, meshes are sorted by this enum
        DECAL,                      //  Decal with alpha blending (premultiplied alpha)
        DECAL_NOPREMULT,            //  Decal with alpha blending (no premultiplied alpha)
        DECAL_CUTOUT,               //  Decal with alpha cutout
        HOLO,                       //  Advanced type of blended object, it has some special features decal doesnt have (cull none, no physics, sorting..)

        VOXEL_MAP_SINGLE, // Special technique used in alt. render
        VOXEL_MAP_MULTI, // Special technique used in alt. render

        SKINNED, //Animated characters
        MESH_INSTANCED, // Mesh with instancing
        MESH_INSTANCED_SKINNED, // Skinned mesh with instancing

        GLASS, //Cockpit glass rendering through billboards

        MESH_INSTANCED_GENERIC, //Classic instancing
        MESH_INSTANCED_GENERIC_MASKED,

        ATMOSPHERE,
        PLANET_SURFACE,
		CLOUD_LAYER,
    }


    public class MyModelDummy
    {
        public const string SUBBLOCK_PREFIX = "subblock_";
        public const string SUBPART_PREFIX = "subpart_";
        public const string ATTRIBUTE_FILE = "file";
        public const string ATTRIBUTE_HIGHLIGHT = "highlight";
        public const string ATTRIBUTE_HIGHLIGHT_SEPARATOR = ";";

        public string Name;
        public Dictionary<string, object> CustomData;
        public Matrix Matrix;
    }

    public class MyModelInfo
    {
        public int TrianglesCount;
        public int VerticesCount;
        public Vector3 BoundingBoxSize;

        public MyModelInfo(int triCnt, int VertCnt, Vector3 BBsize)
        {
            this.TrianglesCount = triCnt;
            this.VerticesCount = VertCnt;
            this.BoundingBoxSize = BBsize;
        }
    }

    public class MyMeshPartInfo
    {
        public int m_MaterialHash;
        public MyMaterialDescriptor m_MaterialDesc = null;
        public List<int> m_indices = new List<int>();
        public MyMeshDrawTechnique Technique = MyMeshDrawTechnique.MESH;

        public string GetMaterialName()
        {
            string materialName = "";
            if (m_MaterialDesc != null)
                materialName = m_MaterialDesc.MaterialName;
            return materialName;
        }

        public bool Export(BinaryWriter writer)
        {
            writer.Write(m_MaterialHash);
            writer.Write(m_indices.Count);
            foreach (int indice in m_indices)
                writer.Write(indice);

            bool bRes = true;
            if (m_MaterialDesc != null)
            {
                writer.Write(true);
                bRes = m_MaterialDesc.Write(writer);
            }
            else
            {
                writer.Write(false);
            }

            return bRes;
        }

        public bool Import(BinaryReader reader, int version)
        {
            m_MaterialHash = reader.ReadInt32();
            if (version < 01052001)
                reader.ReadInt32(); //MyMeshDrawTechnique

            int nCount = reader.ReadInt32();
            for (int i = 0; i < nCount; ++i)
            {
                m_indices.Add(reader.ReadInt32());
            }

            bool bMatDesc = reader.ReadBoolean();
            bool bRes = true;
            if (bMatDesc)
            {
                m_MaterialDesc = new MyMaterialDescriptor();
                bRes = m_MaterialDesc.Read(reader, version);

                bRes &= Enum.TryParse(m_MaterialDesc.Technique, out Technique);
            }
            else
            {
                m_MaterialDesc = null;
            }

            return bRes;
        }
    }

    public class MyMeshSectionInfo
    {
        public MyMeshSectionInfo()
        {
            Meshes = new List<MyMeshSectionMeshInfo>();
        }

        public string Name
        {
            get;
            set;
        }

        public List<MyMeshSectionMeshInfo> Meshes
        {
            get;
            private set;
        }

        public bool Export(BinaryWriter writer)
        {
            writer.Write(Name);
            writer.Write(Meshes.Count);
            bool rval = true;
            foreach (MyMeshSectionMeshInfo mesh in Meshes)
            {
                rval &= mesh.Export(writer);
            }

            return rval;
        }

        public bool Import(BinaryReader reader, int version)
        {
            Name = reader.ReadString();
            int nCount = reader.ReadInt32();

            bool rval = true;
            for (int i = 0; i < nCount; ++i)
            {
                MyMeshSectionMeshInfo info = new MyMeshSectionMeshInfo();
                rval &= info.Import(reader, version);
                Meshes.Add(info);
            }

            return rval;
        }
    }

    public class MyMeshSectionMeshInfo
    {
        public MyMeshSectionMeshInfo()
        {
            StartIndex = -1;
        }

        public string MaterialName
        {
            get;
            set;
        }

        /// <summary>Offset in index list</summary>
        public int StartIndex
        {
            get;
            set;
        }

        /// <summary>Offset in index list</summary>
        public int IndexCount
        {
            get;
            set;
        }

        public bool Export(BinaryWriter writer)
        {
            writer.Write(MaterialName);
            writer.Write(StartIndex);
            writer.Write(IndexCount);
            return true;
        }

        public bool Import(BinaryReader reader, int version)
        {
            MaterialName = reader.ReadString();
            StartIndex = reader.ReadInt32();
            IndexCount = reader.ReadInt32();
            return true;
        }
    }
}
