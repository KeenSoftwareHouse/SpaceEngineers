using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using VRageMath;
using VRageMath.PackedVector;

namespace VRage.Import
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

        //Leave decal type last because it is alpha blended, meshes are sorted by this enum
        DECAL,                      //  Alpha blended object, it has alpha in diffuse.a texture channel and emissivity in normal.a texture channel
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
    }

    public static class PositionPacker
    {
        static public HalfVector4 PackPosition(ref Vector3 position)
        {
            float max_value = System.Math.Max(System.Math.Abs(position.X), System.Math.Abs(position.Y));
            max_value = System.Math.Max(max_value, System.Math.Abs(position.Z));
            float multiplier = System.Math.Min((float)System.Math.Floor(max_value), 2048.0f);
            float invMultiplier = 0;
            if (multiplier > 0)
                invMultiplier = 1.0f / multiplier;
            else
                multiplier = invMultiplier = 1.0f;

            return new HalfVector4(invMultiplier * position.X, invMultiplier * position.Y, invMultiplier * position.Z, multiplier);
        }

        static public Vector3 UnpackPosition(ref HalfVector4 position)
        {
            Vector4 unpacked = position.ToVector4();
            return unpacked.W * new Vector3(unpacked.X, unpacked.Y, unpacked.Z);
        }
    }


    public class MyModelDummy
    {
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
                if (m_MaterialDesc.Technique == "FOLIAGE")
                {
                    Technique = MyMeshDrawTechnique.ALPHA_MASKED;
                }
            }
            else
            {
                m_MaterialDesc = null;
            }

            return bRes;
        }
    }
}
