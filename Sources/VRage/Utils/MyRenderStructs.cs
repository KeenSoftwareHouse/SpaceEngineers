using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using VRage.Import;
using VRageMath;
using VRageMath.PackedVector;

namespace VRage
{
    public struct MyVertexFormatVoxelSingleData2
    {
        const int AMBIENT_MASK = 0x3FFF;

        // Packed vertex format
        public MyUShort4 m_positionAndAmbient;
        public Byte4 m_normal;

        public Vector3 Position
        {
            get { return new Vector3(m_positionAndAmbient.X, m_positionAndAmbient.Y, m_positionAndAmbient.Z) / (float)ushort.MaxValue; }
            set
            {
                m_positionAndAmbient.X = (ushort)(value.X * ushort.MaxValue);
                m_positionAndAmbient.Y = (ushort)(value.Y * ushort.MaxValue);
                m_positionAndAmbient.Z = (ushort)(value.Z * ushort.MaxValue);
            }
        }

        /// <summary>
        /// For multimaterial vertex only
        /// 0, 1 or 2, indicates what material is on this vertex
        /// </summary>
        public byte MaterialAlphaIndex
        {
            get { return (byte)((m_positionAndAmbient.W & AMBIENT_MASK) >> 14); }
            set { m_positionAndAmbient.W = (ushort)((m_positionAndAmbient.W & AMBIENT_MASK) | ((value & 3) << 14)); }
        }

        public float Ambient
        {
            get { return (m_positionAndAmbient.W & AMBIENT_MASK) * 2 - 1; }
            set { m_positionAndAmbient.W = (ushort)((m_positionAndAmbient.W & ~AMBIENT_MASK) | (int)((value * 0.5f + 0.5f) * 16383)); }
        }

        public Vector3 Normal
        {
            get { return VF_Packer.UnpackNormal(ref m_normal); }
            set { m_normal.PackedValue = VF_Packer.PackNormal(ref value); }
        }

        public Byte4 PackedNormal
        {
            get { return m_normal; }
            set { m_normal = value; }
        }
    }


    //It is public because voxel render data comes in this format
    public struct MyVertexFormatVoxelSingleData
    {
        private const int VOXEL_OFFSET = 32767;                           // Offset to add to coordinates when mapping voxel from float<0, 8191> to short<-32767, 32767>.
        private const int VOXEL_MULTIPLIER = 8;                           // Multiplier for mapping voxel from float to short.
        private const float INV_VOXEL_MULTIPLIER = 1.0f / VOXEL_MULTIPLIER;
        private const float VOXEL_COORD_EPSILON = INV_VOXEL_MULTIPLIER / 2;  // Due to rounding errors we must add VOXEL_COORD_EPSILON to coordinates when converting from float to short.
        private const int AMBIENT_MULTIPLIER = 32767;                     // Multiplier for mapping value from float<-1, 1> to short<-32767, 32767>.
        private const float INV_AMBIENT_MULTIPLIER = 1.0f / AMBIENT_MULTIPLIER;

        // Packed vertex format
        public MyShort4 PackedPositionAndAmbientMaterial;
        public MyShort4 PackedPositionAndAmbientMaterialMorph;
        public Byte4 PackedNormal;
        public Byte4 PackedNormalMorph;
        public Byte4 MaterialInfo; // New material related data added to support rendering of entire voxel block in single drawcall. XYZ components defines materials for whole triangle, W is material for current vertex 

        public Vector3 Position
        {
            get
            {
                return new Vector3(PackedPositionAndAmbientMaterial.X, PackedPositionAndAmbientMaterial.Y, PackedPositionAndAmbientMaterial.Z) / (float)short.MaxValue;
            }
            set
            {
                Debug.Assert(value.IsInsideInclusive(ref Vector3.MinusOne, ref Vector3.One));
                PackedPositionAndAmbientMaterial.X = (short)(value.X * short.MaxValue);
                PackedPositionAndAmbientMaterial.Y = (short)(value.Y * short.MaxValue);
                PackedPositionAndAmbientMaterial.Z = (short)(value.Z * short.MaxValue);
            }
        }

        public Vector3 PositionMorph
        {
            get
            {
                return new Vector3(PackedPositionAndAmbientMaterialMorph.X, PackedPositionAndAmbientMaterialMorph.Y, PackedPositionAndAmbientMaterialMorph.Z) / (float)short.MaxValue;
            }
            set
            {
            //    Debug.Assert(value.IsInsideInclusive(ref Vector3.MinusOne, ref Vector3.One));
                PackedPositionAndAmbientMaterialMorph.X = (short)(value.X * short.MaxValue);
                PackedPositionAndAmbientMaterialMorph.Y = (short)(value.Y * short.MaxValue);
                PackedPositionAndAmbientMaterialMorph.Z = (short)(value.Z * short.MaxValue);
            }
        }

        /// <summary>
        /// For multimaterial vertex only
        /// 0, 1 or 2, indicates what material is on this vertex
        /// </summary>
        public byte Material
        {
            get { return VF_Packer.UnpackAlpha(PackedPositionAndAmbientMaterial.W); }
            set { PackedPositionAndAmbientMaterial.W = VF_Packer.PackAmbientAndAlpha(Ambient, value); }
        }

        public byte MaterialMorph
        {
            get { return VF_Packer.UnpackAlpha(PackedPositionAndAmbientMaterialMorph.W); }
            set { PackedPositionAndAmbientMaterialMorph.W = VF_Packer.PackAmbientAndAlpha(AmbientMorph, value); }
        }

        public float Ambient
        {
            get { return VF_Packer.UnpackAmbient(PackedPositionAndAmbientMaterial.W); }
            set { PackedPositionAndAmbientMaterial.W = VF_Packer.PackAmbientAndAlpha(value, Material); }
        }

        public float AmbientMorph
        {
            get { return VF_Packer.UnpackAmbient(PackedPositionAndAmbientMaterialMorph.W); }
            set { PackedPositionAndAmbientMaterialMorph.W = VF_Packer.PackAmbientAndAlpha(value, MaterialMorph); }
        }

        public Vector3 Normal
        {
            get { return VF_Packer.UnpackNormal(ref PackedNormal); }
            set { PackedNormal.PackedValue = VF_Packer.PackNormal(ref value); }
        }

        public Vector3 NormalMorph
        {
            get { return VF_Packer.UnpackNormal(ref PackedNormalMorph); }
            set { PackedNormalMorph.PackedValue = VF_Packer.PackNormal(ref value); }
        }
    }

    public struct MyDecalTriangle_Data
    {
        public MyTriangle_Vertices Vertices;
        public MyTriangle_Normals Normals;
        //public MyTriangle_Normals Tangents;
        public MyTriangle_Coords TexCoords;
        public MyTriangle_Colors Colors;

    }

    public struct MyTriangle_Vertices
    {
        public Vector3 Vertex0;
        public Vector3 Vertex1;
        public Vector3 Vertex2;

        public void Transform(ref Matrix transform)
        {
            Vertex0 = Vector3.Transform(Vertex0, ref transform);
            Vertex1 = Vector3.Transform(Vertex1, ref transform);
            Vertex2 = Vector3.Transform(Vertex2, ref transform);
        }
    }

    public struct MyTriangle_BoneIndicesWeigths
    {
        public MyVertex_BoneIndicesWeights Vertex0;
        public MyVertex_BoneIndicesWeights Vertex1;
        public MyVertex_BoneIndicesWeights Vertex2;
    }

    public struct MyBoneIndexWeight
    {
        public int Index;
        public float Weight;
    }

    public struct MyVertex_BoneIndicesWeights
    {
        public Vector4UByte Indices;
        public Vector4 Weights;
    }

    public struct MyTriangle_Normals
    {
        public Vector3 Normal0;
        public Vector3 Normal1;
        public Vector3 Normal2;
    }

    public struct MyTriangle_Colors
    {
        public Vector4 Color0;
        public Vector4 Color1;
        public Vector4 Color2;
    }

    public struct MyTriangle_Coords
    {
        public Vector2 TexCoord0;
        public Vector2 TexCoord1;
        public Vector2 TexCoord2;
    }

    public unsafe struct MyInstanceData
    {
        public HalfVector4 m_row0;
        public HalfVector4 m_row1;
        public HalfVector4 m_row2;
        public HalfVector4 ColorMaskHSV;

        public MyInstanceData(Matrix m)
        {
            m_row0 = new HalfVector4(m.M11, m.M21, m.M31, m.M41);
            m_row1 = new HalfVector4(m.M12, m.M22, m.M32, m.M42);
            m_row2 = new HalfVector4(m.M13, m.M23, m.M33, m.M43);
            ColorMaskHSV = new HalfVector4();
        }

        public Matrix LocalMatrix
        {
            get
            {
                var row0 = m_row0.ToVector4();
                var row1 = m_row1.ToVector4();
                var row2 = m_row2.ToVector4();

                return new Matrix(
                    row0.X, row1.X, row2.X, 0,
                    row0.Y, row1.Y, row2.Y, 0,
                    row0.Z, row1.Z, row2.Z, 0,
                    row0.W, row1.W, row2.W, 1);
                
            }
            set
            {
                m_row0 = new HalfVector4(value.M11, value.M21, value.M31, value.M41);
                m_row1 = new HalfVector4(value.M12, value.M22, value.M32, value.M42);
                m_row2 = new HalfVector4(value.M13, value.M23, value.M33, value.M43);
            }
        }

        public Vector3 Translation
        {
            get
            {
                return new Vector3(HalfUtils.Unpack((ushort)(m_row0.PackedValue >> 48)),
                    HalfUtils.Unpack((ushort)(m_row1.PackedValue >> 48)),
                    HalfUtils.Unpack((ushort)(m_row2.PackedValue >> 48)));
            }
            set
            {
                m_row0.PackedValue = (m_row0.PackedValue & 0xFFFFFFFFFFFF) | ((ulong)HalfUtils.Pack(value.X) << 48);
                m_row1.PackedValue = (m_row1.PackedValue & 0xFFFFFFFFFFFF) | ((ulong)HalfUtils.Pack(value.Y) << 48);
                m_row2.PackedValue = (m_row2.PackedValue & 0xFFFFFFFFFFFF) | ((ulong)HalfUtils.Pack(value.Z) << 48);
            }
        }
    }

    // We need to fit into 10 registers, so packing is necessary
    // 9th bone is stored as v0.w, v1.w, v2.w
    // v3.w is EnableSkinning
    // v4.w is BoneRange
    // v5.w, v6.w is texture offset
    // v6.w is dithering sign, used to differentiate between projection (negative) and regular transparency (positive)
    // Total RAM size is 64 bytes
    public unsafe struct MyCubeInstanceData
    {
        private fixed byte m_bones[8*4]; // 4 bytes per vector * 8 vectors = 32 bytes
        public Vector4 m_translationAndRot;
        //If you want negative dithering, use SetColorMaskHSV instead!
        public Vector4 ColorMaskHSV;


        // ref. vertex_transformations.h, construct_deformed_cube_instance_matrix()
        public Matrix ConstructDeformedCubeInstanceMatrix(ref Vector4UByte boneIndices, ref Vector4 boneWeights, out Matrix localMatrix)
        {
            localMatrix = LocalMatrix;
            Matrix ret = localMatrix;
            if (EnableSkinning)
            {
                Vector3 offset = ComputeBoneOffset(ref boneIndices, ref boneWeights);
                Vector3 translationM = ret.Translation;
                translationM += offset;
                ret.Translation = translationM;
            }
            return ret;
        }

        public Vector3 ComputeBoneOffset(ref Vector4UByte boneIndices, ref Vector4 boneWeights)
        {
            Matrix bonesMatrix = new Matrix();
            Vector4 bone0 = GetNormalizedBone(boneIndices[0]);
            Vector4 bone1 = GetNormalizedBone(boneIndices[1]);
            Vector4 bone2 = GetNormalizedBone(boneIndices[2]);
            Vector4 bone3 = GetNormalizedBone(boneIndices[3]);
            bonesMatrix.SetRow(0, bone0);
            bonesMatrix.SetRow(1, bone1);
            bonesMatrix.SetRow(2, bone2);
            bonesMatrix.SetRow(3, bone3);

            return Denormalize(Vector4.Transform(boneWeights, bonesMatrix), BoneRange);
        }

        public byte[] RawBones()
        {
            var buffer = new byte[32];
            fixed (byte* ptr = m_bones)
            {
                for (int i = 0; i < 32; i++)
                    buffer[i] = ptr[i];
            }
            return buffer;
        }

        public Matrix LocalMatrix
        {
            get
            {
                return Vector4.UnpackOrthoMatrix(ref m_translationAndRot);
            }
            set
            {
                m_translationAndRot = Vector4.PackOrthoMatrix(ref value);
            }
        }

        /// <summary>
        /// Gets translation, faster than getting local matrix
        /// </summary>
        public Vector3 Translation
        {
            get
            {
                return new Vector3(m_translationAndRot);
            }
        }

        public Vector4 PackedOrthoMatrix
        {
            get { return m_translationAndRot; }
            set { m_translationAndRot = value; }
        }

        /// <summary>
        /// Resets bones to zero and disables skinning
        /// </summary>
        public void ResetBones()
        {
            // Set all components to 128, which equals zero in shader
            const ulong ALL_128 = 0x8080808080808080;
            const ulong ALL_128_UPPER_W_0 = 0x0080808080808080; // Set all components to 128, but set skinning flag to false

            fixed (byte* d = m_bones)
            {
                ulong* x = (ulong*)d;
                x[0] = ALL_128;
                x[1] = ALL_128_UPPER_W_0;
                x[2] = ALL_128;
                x[3] = ALL_128;
            }
        }

        public void SetTextureOffset(Vector2 patternOffset)
        {
            fixed (byte* d = m_bones)
            {
                ((Vector4UByte*)d)[5].W = (byte)(patternOffset.X * 255);
                ((Vector4UByte*)d)[6].W = (byte)(patternOffset.Y * 255);
            }
        }

        public float GetTextureOffset(int index)
        {
            fixed (byte* d = m_bones)
            {
                return ((Vector4UByte*)d)[5 + index].W / 255f;
            }
        }

        public void SetColorMaskHSV(Vector4 colorMaskHSV)
        {
            ColorMaskHSV = colorMaskHSV;
            if (colorMaskHSV.W < 0)
            {
                fixed (byte* d = m_bones)
                {
                    ((Vector4UByte*)d)[7].W = 1;
                }
                ColorMaskHSV.W = -ColorMaskHSV.W;
            }
        }

        public float BoneRange
        {
            get
            {
                fixed (byte* d = m_bones)
                {
                    return ((Vector4UByte*)d)[4].W / 10.0f;
                }
            }
            set
            {
                fixed (byte* d = m_bones)
                {
                    ((Vector4UByte*)d)[4].W = (byte)(value * 10);
                }
            }
        }

        public bool EnableSkinning
        {
            get
            {
                fixed (byte* d = m_bones)
                {
                    return ((Vector4UByte*)d)[3].W != 0;
                }
            }
            set
            {
                fixed (byte* d = m_bones)
                {
                    ((Vector4UByte*)d)[3].W = value ? (byte)255 : (byte)0;
                }
            }
        }

        public Vector3UByte this[int index]
        {
            get
            {
                Debug.Assert(index >= 0 && index < 9);
                fixed (byte* d = m_bones)
                {
                    if (index == 8)
                    {
                        return new Vector3UByte(((Vector4UByte*)d)[0].W, ((Vector4UByte*)d)[1].W, ((Vector4UByte*)d)[2].W);
                    }
                    else
                    {
                        return *((Vector3UByte*)&((Vector4UByte*)d)[index]);
                    }
                }
            }
            set
            {
                Debug.Assert(index >= 0 && index < 9);
                fixed (byte* d = m_bones)
                {
                    if (index == 8)
                    {
                        ((Vector4UByte*)d)[0].W = value.X;
                        ((Vector4UByte*)d)[1].W = value.Y;
                        ((Vector4UByte*)d)[2].W = value.Z;
                    }
                    else
                    {
                        *((Vector3UByte*)&((Vector4UByte*)d)[index]) = value;
                    }
                }
            }
        }

        public Vector3 GetDenormalizedBone(int index)
        {
            return Denormalize(GetNormalizedBone(index), BoneRange);
        }

        public Vector4UByte GetPackedBone(int index)
        {
            Debug.Assert(index >= 0 && index < 9);
            fixed (byte* d = m_bones)
            {
                if (index == 8)
                {
                    return new Vector4UByte(((Vector4UByte*)d)[0].W, ((Vector4UByte*)d)[1].W, ((Vector4UByte*)d)[2].W, 0);
                }
                else
                {
                    return ((Vector4UByte*)d)[index];
                }
            }
        }

        /// <returns>Vector in range [0,1]</returns>
        private Vector4 GetNormalizedBone(int index)
        {
            Vector4UByte packedBone = GetPackedBone(index);
            return new Vector4(packedBone.X, packedBone.Y, packedBone.Z, packedBone.W) / 255f;
        }

        /// <param name="position">Scaled in range [0,1]</param>
        /// <param name="range">Unscaled</param>
        /// <returns>Unscaled position</returns>
        private Vector3 Denormalize(Vector4 position, float range)
        {
            const float eps = 0.5f / 255;
            return (new Vector3(position) + eps - 0.5f) * range * 2;
        }
    }
}
