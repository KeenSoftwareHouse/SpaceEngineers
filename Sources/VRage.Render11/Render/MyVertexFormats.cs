using System;
using System.Collections.Generic;
using System.Deployment.Internal;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using VRage.Import;
using VRageMath;
using VRageMath.PackedVector;
using Vector2 = VRageMath.Vector2;
using Vector3 = VRageMath.Vector3;
using Vector4 = VRageMath.Vector4;
using Color = VRageMath.Color;

namespace VRageRender.Vertex
{
    struct MyVertexFormatPosition
    {
        internal Vector3 Position;

        internal MyVertexFormatPosition(Vector3 position)
        {
            Position = position;
        }

        internal static unsafe int STRIDE = sizeof(MyVertexFormatPosition);
    };

    struct MyVertexFormatPositionH4
    {
        internal HalfVector4 Position;

        internal MyVertexFormatPositionH4(HalfVector4 position)
        {
            Position = position;
        }

        internal MyVertexFormatPositionH4(Vector3 position)
        {
            
            Position = VF_Packer.PackPosition(ref position);
        }

        internal static unsafe int STRIDE = sizeof(MyVertexFormatPositionH4);
    };

    struct MyVertexFormatPositionTextureH
    {
        internal Vector3 Position;
        internal HalfVector2 Texcoord;

        internal MyVertexFormatPositionTextureH(Vector3 position, HalfVector2 texcoord)
        {
            Position = position;
            Texcoord = texcoord;
        }

        internal static unsafe int STRIDE = sizeof(MyVertexFormatPositionTextureH);
    };

    struct MyVertexFormatPositionHTextureH
    {
        internal HalfVector4 Position;
        internal HalfVector2 Texcoord;

        internal MyVertexFormatPositionHTextureH(HalfVector4 position, HalfVector2 texcoord)
        {
            Position = position;
            Texcoord = texcoord;
        }

        internal static unsafe int STRIDE = sizeof(MyVertexFormatPositionHTextureH);
    }

    struct MyVertexFormatPosition2Texcoord
    {
        internal Vector2 Position;
        internal Vector2 Texcoord;

        internal MyVertexFormatPosition2Texcoord(Vector2 position, Vector2 texcoord)
        {
            Position = position;
            Texcoord = texcoord;
        }

        internal static unsafe int STRIDE = sizeof(MyVertexFormatPosition2Texcoord);
    }

    struct MyVertexFormatPositionSkinning
    {
        internal HalfVector4 Position;
        internal HalfVector4 BoneWeights;
        internal Byte4 BoneIndices;

        internal MyVertexFormatPositionSkinning(HalfVector4 position, Byte4 indices, Vector4 weights)
        {
            Position = position;
            BoneIndices = indices;
            BoneWeights = new HalfVector4(weights);
        }

        internal static unsafe int STRIDE = sizeof(MyVertexFormatPositionSkinning);   
    }

    struct MyVertexFormatSpritePositionTextureColor
    {
        internal HalfVector4 ClipspaceOffsetScale;
        internal HalfVector4 TexcoordOffsetScale;
        internal Byte4 Color;

        internal MyVertexFormatSpritePositionTextureColor(HalfVector4 position, HalfVector4 texcoord, Byte4 color)
        {
            ClipspaceOffsetScale = position;
            TexcoordOffsetScale = texcoord;
            Color = color;
        }

        internal static unsafe int STRIDE = sizeof(MyVertexFormatSpritePositionTextureColor);
    }

    struct MyVertexFormatSpritePositionTextureRotationColor
    {
        internal HalfVector4 ClipspaceOffsetScale;
        internal HalfVector4 TexcoordOffsetScale;
        internal HalfVector4 OriginTangent;
        internal Byte4 Color;

        internal MyVertexFormatSpritePositionTextureRotationColor(HalfVector4 position, HalfVector4 texcoord, HalfVector4 originTangent, Byte4 color)
        {
            ClipspaceOffsetScale = position;
            TexcoordOffsetScale = texcoord;
            OriginTangent = originTangent; 
            Color = color;
        }

        internal static unsafe int STRIDE = sizeof(MyVertexFormatSpritePositionTextureRotationColor);
    }

    struct MyVertexFormatPositionTextureSkinning
    {
        internal HalfVector4 Position;
        internal HalfVector2 Texcoord;
        internal Byte4 BoneIndices;
        internal HalfVector4 BoneWeights;

        internal MyVertexFormatPositionTextureSkinning(HalfVector4 position, HalfVector2 texcoord, Byte4 indices, Vector4 weights)
        {
            Position = position;
            Texcoord = texcoord;
            BoneIndices = indices;
            BoneWeights = new HalfVector4(weights);
        }

        internal static unsafe int STRIDE = sizeof(MyVertexFormatPositionTextureSkinning);
    }

    struct MyVertexFormatPositionTexcoordNormalTangent
    {
        internal HalfVector4 Position;
        internal Byte4 Normal;
        internal Byte4 Tangent;
        internal HalfVector2 Texcoord;

        internal static unsafe int STRIDE = sizeof(MyVertexFormatPositionTexcoordNormalTangent);
    }

    struct MyVertexFormatTexcoordNormalTangent
    {
        internal Byte4 Normal;
        internal Byte4 Tangent;
        internal HalfVector2 Texcoord;

        internal MyVertexFormatTexcoordNormalTangent(HalfVector2 texcoord, Vector3 normal, Vector4 tangent)
        {
            Texcoord = texcoord;
            Normal = VF_Packer.PackNormalB4(ref normal);
            Tangent = VF_Packer.PackTangentSignB4(ref tangent);
        }

        internal MyVertexFormatTexcoordNormalTangent(Vector2 texcoord, Vector3 normal, Vector3 tangent)
        {
            Texcoord = new HalfVector2(texcoord.X, texcoord.Y);
            Normal = VF_Packer.PackNormalB4(ref normal);
            Vector4 T = new Vector4(tangent, 1);
            Tangent = VF_Packer.PackTangentSignB4(ref T);
        }

        internal MyVertexFormatTexcoordNormalTangent(HalfVector2 texcoord, Byte4 normal, Byte4 tangent)
        {
            Texcoord = texcoord;
            Normal = normal;
            Tangent = tangent;
        }

        internal static unsafe int STRIDE = sizeof(MyVertexFormatTexcoordNormalTangent);
    }

    struct MyVertexFormatTexcoordNormalTangentTexindices
    {
        internal Byte4 Normal;
        internal Byte4 Tangent;
        internal HalfVector2 Texcoord;
        internal Byte4 TexIndices;

        internal MyVertexFormatTexcoordNormalTangentTexindices(Vector2 texcoord, Vector3 normal, Vector3 tangent, Byte4 texIndices)
        {
            Texcoord = new HalfVector2(texcoord.X, texcoord.Y);
            Normal = VF_Packer.PackNormalB4(ref normal);
            Vector4 T = new Vector4(tangent, 1);
            Tangent = VF_Packer.PackTangentSignB4(ref T);
            TexIndices = texIndices;
        }

        internal static unsafe int STRIDE = sizeof(MyVertexFormatTexcoordNormalTangentTexindices);
    }
    
    unsafe struct MyVertexFormatCubeInstance
    {
#if XB1
		static MyVertexFormatCubeInstance()
		{
			System.Diagnostics.Debug.Assert(MyRender11Constants.CUBE_INSTANCE_BONES_NUM == 8);
		}
		internal fixed byte bones[8 * 4];
#else
        internal fixed byte bones[MyRender11Constants.CUBE_INSTANCE_BONES_NUM * 4];
#endif
        internal Vector4 translationRotation;
        internal Vector4 colorMaskHSV;

        internal static int STRIDE = sizeof(MyVertexFormatCubeInstance);
    }

    unsafe struct MyVertexFormatGenericInstance
    {
        internal HalfVector4 row0;
        internal HalfVector4 row1;
        internal HalfVector4 row2;
        internal HalfVector4 colorMaskHSV;

        internal static int STRIDE = sizeof(MyVertexFormatGenericInstance);
    }

    struct MyVertexFormatVoxel
    {
        internal MyUShort4  m_positionMaterials;
        internal MyUShort4  m_positionMaterialsMorph;
        internal Byte4      m_materialInfo;

        public Vector3 Position
        {
            get { return new Vector3(m_positionMaterials.X, m_positionMaterials.Y, m_positionMaterials.Z) / (float) ushort.MaxValue * 2.0f - 1.0f; }
            set { m_positionMaterials.X = (ushort)((value.X * 0.5f + 0.5f) * ushort.MaxValue); m_positionMaterials.Y = (ushort)((value.Y * 0.5f + 0.5f) * ushort.MaxValue); m_positionMaterials.Z = (ushort)((value.Z * 0.5f + 0.5f) * ushort.MaxValue); }
        }

        public Vector3 PositionMorph
        {
            get { return new Vector3(m_positionMaterialsMorph.X, m_positionMaterialsMorph.Y, m_positionMaterialsMorph.Z) / (float)ushort.MaxValue * 2.0f - 1.0f; }
            set { m_positionMaterialsMorph.X = (ushort)((value.X * 0.5f + 0.5f) * ushort.MaxValue); m_positionMaterialsMorph.Y = (ushort)((value.Y * 0.5f + 0.5f) * ushort.MaxValue); m_positionMaterialsMorph.Z = (ushort)((value.Z * 0.5f + 0.5f) * ushort.MaxValue); }
        }

        internal static unsafe int STRIDE = sizeof(MyVertexFormatVoxel);
    }

    struct MyVertexFormatNormal
    {
        internal Byte4 Normal;
        internal Byte4 NormalMorph;

        internal MyVertexFormatNormal(Byte4 normal, Byte4 normalMorph)
        {
            Normal = normal;
            NormalMorph = normalMorph;
        }

        internal static unsafe int STRIDE = sizeof(MyVertexFormatNormal);
    }

    struct MyVertexFormatPositionPackedColor
    {
        internal HalfVector4 Position;
        internal Byte4 Color;

        internal MyVertexFormatPositionPackedColor(HalfVector4 position, Byte4 color)
        {
            Position = position;
            Color = color;
        }

        internal MyVertexFormatPositionPackedColor(Vector3 position, Byte4 color)
        {
            Position = new HalfVector4(position.X, position.Y, position.Z, 1);
            Color = color;
        }

        internal MyVertexFormatPositionPackedColor(Vector3 position, Color color)
        {
            Position = new HalfVector4(position.X, position.Y, position.Z, 1);
            Color = new Byte4(color.PackedValue);
        }
    
        internal static unsafe int STRIDE = sizeof(MyVertexFormatPositionPackedColor);
    }

    struct MyVertexFormatPositionColor
    {
        internal Vector3 Position;
        internal Byte4 Color;

        internal MyVertexFormatPositionColor(Vector3 position, Byte4 color)
        {
            Position = position;
            Color = color;
        }

        internal MyVertexFormatPositionColor(Vector3 position, Color color)
        {
            Position = position;
            Color = new Byte4(color.PackedValue);
        }

        internal static unsafe int STRIDE = sizeof(MyVertexFormatPositionColor);
    }

    struct MyVertexFormat2DPosition
    {
        internal Vector2 Position;

        internal MyVertexFormat2DPosition(Vector2 position)
        {
            Position = position;
        }

        internal static unsafe int STRIDE = sizeof(MyVertexFormat2DPosition);
    };
}
