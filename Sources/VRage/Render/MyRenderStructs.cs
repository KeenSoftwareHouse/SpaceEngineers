using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using VRage.Import;
using VRageMath;
using VRageMath.PackedVector;

namespace VRageRender
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
        public MyShort4 PackedPositionAndAmbient;
        public MyShort4 PackedPositionAndMaterialMorph;
        public Byte4 PackedNormal;
        public Byte4 PackedNormalMorph;

        public Vector3 Position
        {
            get
            {
                return new Vector3(PackedPositionAndAmbient.X, PackedPositionAndAmbient.Y, PackedPositionAndAmbient.Z) / (float)short.MaxValue;
            }
            set
            {
                Debug.Assert(value.IsInsideInclusive(ref Vector3.MinusOne, ref Vector3.One));
                PackedPositionAndAmbient.X = (short)(value.X * short.MaxValue);
                PackedPositionAndAmbient.Y = (short)(value.Y * short.MaxValue);
                PackedPositionAndAmbient.Z = (short)(value.Z * short.MaxValue);
            }
        }

        public Vector3 PositionMorph
        {
            get
            {
                return new Vector3(PackedPositionAndMaterialMorph.X, PackedPositionAndMaterialMorph.Y, PackedPositionAndMaterialMorph.Z) / (float)short.MaxValue;
            }
            set
            {
                Debug.Assert(value.IsInsideInclusive(ref Vector3.MinusOne, ref Vector3.One));
                PackedPositionAndMaterialMorph.X = (short)(value.X * short.MaxValue);
                PackedPositionAndMaterialMorph.Y = (short)(value.Y * short.MaxValue);
                PackedPositionAndMaterialMorph.Z = (short)(value.Z * short.MaxValue);
            }
        }

        /// <summary>
        /// For multimaterial vertex only
        /// 0, 1 or 2, indicates what material is on this vertex
        /// </summary>
        public byte MaterialAlphaIndex
        {
            get { return VF_Packer.UnpackAlpha(PackedPositionAndAmbient.W); }
            set { PackedPositionAndAmbient.W = VF_Packer.PackAmbientAndAlpha(Ambient, value); }
        }

        public byte MaterialMorph
        {
            get { return VF_Packer.UnpackAlpha(PackedPositionAndMaterialMorph.W); }
            set { PackedPositionAndMaterialMorph.W = VF_Packer.PackAmbientAndAlpha(0f, value); }
        }

        public float Ambient
        {
            get { return VF_Packer.UnpackAmbient(PackedPositionAndAmbient.W); }
            set { PackedPositionAndAmbient.W = VF_Packer.PackAmbientAndAlpha(value, MaterialAlphaIndex); }
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
        public MyTriangle_Vertexes Vertexes;
        public MyTriangle_Normals Normals;
        //public MyTriangle_Normals Tangents;
        public MyTriangle_Coords TexCoords;
        public MyTriangle_Colors Colors;

    }

    public struct MyTriangle_Vertexes
    {
        public Vector3 Vertex0;
        public Vector3 Vertex1;
        public Vector3 Vertex2;
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
        private fixed byte m_bones[32]; // 4 bytes per vector * 8 vectors = 32 bytes
        public Vector4 m_translationAndRot;
        //If you want negative dithering, use SetColorMaskHSV instead!
        public Vector4 ColorMaskHSV;

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
    }
}
