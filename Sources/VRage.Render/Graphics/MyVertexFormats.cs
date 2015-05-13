using System.Runtime.InteropServices;
using System;
using System.Diagnostics;

using SharpDX;
using SharpDX.Direct3D9;


namespace VRageRender.Graphics
{
    using Byte4 = VRageMath.PackedVector.Byte4;
    using HalfVector2 = VRageMath.PackedVector.HalfVector2;
    using HalfVector4 = VRageMath.PackedVector.HalfVector4;
    using Vector2 = VRageMath.Vector2;
    using Vector3 = VRageMath.Vector3;
    using Vector4 = VRageMath.Vector4;
    using Rectangle = VRageMath.Rectangle;
    using Matrix = VRageMath.Matrix;
    using Color = VRageMath.Color;
    using BoundingBox = VRageMath.BoundingBox;
    using BoundingSphere = VRageMath.BoundingSphere;
    using BoundingFrustum = VRageMath.BoundingFrustum;
    using VRage.Import;
    using VRageRender.Utils;
    using VRageMath;
      
    internal struct MyVertexFormatPositionTextureColor 
    {
        static VertexDeclaration m_vertexDeclaration;
        static int m_stride;

        public Vector3 Position;
        public Vector2 TexCoord;
        public Vector4 Color;

        public MyVertexFormatPositionTextureColor(Vector3 position, Vector2 texCoord, Vector4 color)
        {
            Position = position;
            TexCoord = texCoord;
            Color = color;
        }

        public static VertexDeclaration VertexDeclaration
        {
            get { return m_vertexDeclaration; }
        }

        public static int Stride
        {
            get { return m_stride; }
        }

        public static void LoadContent(Device device)
        {
            //new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            //new VertexElement(sizeof(float) * (3), VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            //new VertexElement(sizeof(float) * (3 + 2), VertexElementFormat.Vector4, VertexElementUsage.Color, 0)

            var elements = new[] {
        		new VertexElement(0, 0, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.Position, 0),
        		new VertexElement(0, sizeof(float) * (3), DeclarationType.Float2, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 0),
                new VertexElement(0, sizeof(float) * (3 + 2), DeclarationType.Float4, DeclarationMethod.Default, DeclarationUsage.Color, 0),
				VertexElement.VertexDeclarationEnd
        	};

            m_stride = D3DX.GetDeclarationVertexSize(elements, 0);
            System.Diagnostics.Debug.Assert(m_vertexDeclaration == null);
            m_vertexDeclaration = new VertexDeclaration(device, elements);
        }

        public static void UnloadContent()
        {
            if (m_vertexDeclaration != null)
            {
                m_vertexDeclaration.Dispose();
                m_vertexDeclaration = null;
            }
        }
    }


    internal struct MyVertexFormatPosition
    {
        static VertexDeclaration m_vertexDeclaration;
        static int m_stride;

        public HalfVector4 Position;

        public MyVertexFormatPosition(HalfVector4 position)
        {
            Position = position;
        }

        public static VertexDeclaration VertexDeclaration
        {
            get { return m_vertexDeclaration; }
        }

        public static int Stride
        {
            get { return m_stride; }
        }

        public static void LoadContent(Device device)
        {
            var elements = new[] {
        		new VertexElement(0, 0, DeclarationType.HalfFour, DeclarationMethod.Default, DeclarationUsage.Position, 0),
				VertexElement.VertexDeclarationEnd
        	};

            m_stride = D3DX.GetDeclarationVertexSize(elements, 0);
            System.Diagnostics.Debug.Assert(m_vertexDeclaration == null);
            m_vertexDeclaration = new VertexDeclaration(device, elements);
        }

        public static void UnloadContent()
        {
            if (m_vertexDeclaration != null)
            {
                m_vertexDeclaration.Dispose();
                m_vertexDeclaration = null;
            }
        }
    }

    internal struct MyVertexFormatPositionFull
    {
        static VertexDeclaration m_vertexDeclaration;
        static int m_stride;

        public Vector4 Position;

        public MyVertexFormatPositionFull(Vector4 position)
        {
            Position = position;
        }

        public static VertexDeclaration VertexDeclaration
        {
            get { return m_vertexDeclaration; }
        }

        public static int Stride
        {
            get { return m_stride; }
        }

        public static void LoadContent(Device device)
        {
            var elements = new[] {
        		new VertexElement(0, 0, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.Position, 0),
				VertexElement.VertexDeclarationEnd
        	};

            m_stride = D3DX.GetDeclarationVertexSize(elements, 0);
            System.Diagnostics.Debug.Assert(m_vertexDeclaration == null);
            m_vertexDeclaration = new VertexDeclaration(device, elements);
        }

        public static void UnloadContent()
        {
            if (m_vertexDeclaration != null)
            {
                m_vertexDeclaration.Dispose();
                m_vertexDeclaration = null;
            }
        }
    }

    internal struct MyVertexFormatPositionColor
    {
        static VertexDeclaration m_vertexDeclaration;
        static int m_stride;

        public Vector3 Position;
        public Vector4 Color;

        public MyVertexFormatPositionColor(Vector3 position, Vector4 color)
        {
            Position = position;
            Color = color;
        }

        public static VertexDeclaration VertexDeclaration
        {
            get { return m_vertexDeclaration; }
        }

        public static int Stride
        {
            get { return m_stride; }
        }

        public static void LoadContent(Device device)
        {
            var elements = new[] {
        		new VertexElement(0, 0, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.Position, 0),
                new VertexElement(0, sizeof(float) * 3, DeclarationType.Float4, DeclarationMethod.Default, DeclarationUsage.Color, 0),
				VertexElement.VertexDeclarationEnd
        	};

            m_stride = D3DX.GetDeclarationVertexSize(elements, 0);
            System.Diagnostics.Debug.Assert(m_vertexDeclaration == null);
            m_vertexDeclaration = new VertexDeclaration(device, elements);
        }

        public static void UnloadContent()
        {
            if (m_vertexDeclaration != null)
            {
                m_vertexDeclaration.Dispose();
                m_vertexDeclaration = null;
            }
        }
    }
    
    //  For drawing background cube using cube textures (see that texture coord0 has 3 components)
    internal struct MyVertexFormatPositionTexture3 
    {
        static VertexDeclaration m_vertexDeclaration;
        static int m_stride;

        public Vector3 Position;
        public Vector3 TexCoord;

        public MyVertexFormatPositionTexture3(Vector3 position, Vector3 texCoord)
        {
            Position = position;
            TexCoord = texCoord;
        }

        public static VertexDeclaration VertexDeclaration
        {
            get { return m_vertexDeclaration; }
        }

        public static int Stride
        {
            get { return m_stride; }
        }

        public static void LoadContent(Device device)
        {
            //new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            //new VertexElement(sizeof(float) * 3, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0)

            var elements = new[] {
        		new VertexElement(0, 0, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.Position, 0),
        		new VertexElement(0, sizeof(float) * (3), DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 0),
				VertexElement.VertexDeclarationEnd
        	};

            m_stride = D3DX.GetDeclarationVertexSize(elements, 0);
            System.Diagnostics.Debug.Assert(m_vertexDeclaration == null);
            m_vertexDeclaration = new VertexDeclaration(device, elements);
        }

        public static void UnloadContent()
        {
            if (m_vertexDeclaration != null)
            {
                m_vertexDeclaration.Dispose();
                m_vertexDeclaration = null;
            }
        }
    }

    internal struct MyVertexFormatPositionTexture
    {
        static VertexDeclaration m_vertexDeclaration;
        static int m_stride;

        public Vector3 Position;
        public Vector2 TexCoord;

        public MyVertexFormatPositionTexture(Vector3 position, Vector2 texCoord)
        {
            Position = position;
            TexCoord = texCoord;
        }

        public static VertexDeclaration VertexDeclaration
        {
            get { return m_vertexDeclaration; }
        }

        public static int Stride
        {
            get { return m_stride; }
        }

        public static void LoadContent(Device device)
        {
            //new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            //new VertexElement(sizeof(float) * 3, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0)

            var elements = new[] {
        		new VertexElement(0, 0, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.Position, 0),
        		new VertexElement(0, sizeof(float) * (3), DeclarationType.Float2, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 0),
				VertexElement.VertexDeclarationEnd
        	};

            m_stride = D3DX.GetDeclarationVertexSize(elements, 0);
            System.Diagnostics.Debug.Assert(m_vertexDeclaration == null);
            m_vertexDeclaration = new VertexDeclaration(device, elements);
        }

        public static void UnloadContent()
        {
            if (m_vertexDeclaration != null)
            {
                m_vertexDeclaration.Dispose();
                m_vertexDeclaration = null;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct MyVertexFormatPositionNormal 
    {
        static VertexDeclaration m_vertexDeclaration;
        static int m_stride;

        public HalfVector4 _PositionScale;
        public Byte4 _Normal;

        public Vector3 Position
        {
            get { return VF_Packer.UnpackPosition(ref _PositionScale); }
            set { _PositionScale = VF_Packer.PackPosition(ref value); }
        }

        public Vector3 Normal
        {
            get { return VF_Packer.UnpackNormal(ref _Normal); }
            set { _Normal.PackedValue = VF_Packer.PackNormal(ref value); }
        }

        public static VertexDeclaration VertexDeclaration
        {
            get { return m_vertexDeclaration; }
        }

        public static int Stride
        {
            get { return m_stride; }
        }

        public static void LoadContent(Device device)
        {
            //new VertexElement(0, VertexElementFormat.HalfVector4, VertexElementUsage.Position, 0),
            //new VertexElement(8, VertexElementFormat.Byte4, VertexElementUsage.Normal, 0)


            var elements = new[] {
        		new VertexElement(0, 0, DeclarationType.HalfFour, DeclarationMethod.Default, DeclarationUsage.Position, 0),
        		new VertexElement(0, 8, DeclarationType.Ubyte4, DeclarationMethod.Default, DeclarationUsage.Normal, 0),
				VertexElement.VertexDeclarationEnd
        	};

            m_stride = D3DX.GetDeclarationVertexSize(elements, 0);
            System.Diagnostics.Debug.Assert(m_vertexDeclaration == null);
            m_vertexDeclaration = new VertexDeclaration(device, elements);
        }

        public static void UnloadContent()
        {
            if (m_vertexDeclaration != null)
            {
                m_vertexDeclaration.Dispose();
                m_vertexDeclaration = null;
            }
        }
    }

     
 
    //  Used for drawing models that have diffuse, texture and normals
    internal struct MyVertexFormatPositionNormalTexture 
    {
        static VertexDeclaration m_vertexDeclaration;
        static int m_stride;

        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TexCoord;


        public static VertexDeclaration VertexDeclaration
        {
            get { return m_vertexDeclaration; }
        }

        public static int Stride
        {
            get { return m_stride; }
        }

        public static void LoadContent(Device device)
        {
            //new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            //new VertexElement(sizeof(float) * 3, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
            //new VertexElement(sizeof(float) * (3 + 3), VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)


            var elements = new[] {
        		new VertexElement(0, 0, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.Position, 0),
        		new VertexElement(0, sizeof(float) * 3, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.Normal, 0),
                new VertexElement(0, sizeof(float) * (3 + 3), DeclarationType.Float2, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 0),
				VertexElement.VertexDeclarationEnd
        	};

            m_stride = D3DX.GetDeclarationVertexSize(elements, 0);
            System.Diagnostics.Debug.Assert(m_vertexDeclaration == null);
            m_vertexDeclaration = new VertexDeclaration(device, elements);
        }

        public static void UnloadContent()
        {
            if (m_vertexDeclaration != null)
            {
                m_vertexDeclaration.Dispose();
                m_vertexDeclaration = null;
            }
        }
    }

            /*
    //  Used for drawing models that have diffuse, texture and normals
    public struct MyVertexFormatPositionNormalTexturePacked : IVertexType
    {
        // packed from 32B to 16B
        // position Range: signed <0 - 65mil> Error: 0.00244%
        public HalfVector4 _PositionScale;    // unpacking in HLSL: Position = _PositionScale.xyz * _PositionScale.w;
        Byte4 _Normal;
        HalfVector2 _TexCoord;

        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration
        (
            new VertexElement(0, VertexElementFormat.HalfVector4, VertexElementUsage.Position, 0),
            new VertexElement(8, VertexElementFormat.Byte4, VertexElementUsage.Normal, 0),
            new VertexElement(12, VertexElementFormat.HalfVector2, VertexElementUsage.TextureCoordinate, 0)
        );

        public Vector3 Position
        {
            get { return VF_Packer.UnpackPosition(ref _PositionScale); }
            set { _PositionScale = VF_Packer.PackPosition(ref value); }
        }

        public Vector3 Normal
        {
            get { return VF_Packer.UnpackNormal(ref _Normal); }
            set { _Normal.PackedValue = VF_Packer.PackNormal(ref value); }
        }

        public Vector2 TexCoord
        {
            get { return _TexCoord.ToVector2(); }
            set { _TexCoord = new HalfVector2(value); }
        }


        public HalfVector4 PositionPacked
        {
            get { return _PositionScale; }
            set { _PositionScale = value; }
        }

        public Byte4 NormalPacked
        {
            get { return _Normal; }
            set { _Normal = value; }
        }

        public HalfVector2 TexCoordPacked
        {
            get { return _TexCoord; }
            set { _TexCoord = value; }
        }


        VertexDeclaration IVertexType.VertexDeclaration { get { return VertexDeclaration; } }
    }

*/

    //  Used for drawing models that have diffuse, texture and normals
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct MyVertexFormatPositionNormalTextureMask 
    {
        static VertexDeclaration m_vertexDeclaration;
        static int m_stride;


        // packed from 40B to 20B
        public HalfVector4 _PositionScale;
        public Byte4 _Normal;
        public HalfVector2 _TexCoord;
        public HalfVector2 _MaskCoord;

        public Vector3 Position
        {
            get { return VF_Packer.UnpackPosition(ref _PositionScale); }
            set { _PositionScale = VF_Packer.PackPosition(ref value); }
        }

        public Vector3 Normal
        {
            get { return VF_Packer.UnpackNormal(ref _Normal); }
            set { _Normal.PackedValue = VF_Packer.PackNormal(ref value); }
        }

        public Vector2 TexCoord
        {
            get { return _TexCoord.ToVector2(); }
            set { _TexCoord = new HalfVector2(value); }
        }

        public Vector2 MaskCoord
        {
            get { return _MaskCoord.ToVector2(); }
            set { _MaskCoord = new HalfVector2(value); }
        }


        public HalfVector4 PositionPacked
        {
            get { return _PositionScale; }
            set { _PositionScale = value; }
        }

        public Byte4 NormalPacked
        {
            get { return _Normal; }
            set { _Normal = value; }
        }

        public HalfVector2 TexCoordPacked
        {
            get { return _TexCoord; }
            set { _TexCoord = value; }
        }

        public HalfVector2 MaskCoordPacked
        {
            get { return _MaskCoord; }
            set { _MaskCoord = value; }
        }

        public static VertexDeclaration VertexDeclaration
        {
            get { return m_vertexDeclaration; }
        }

        public static int Stride
        {
            get { return m_stride; }
        }

        public static void LoadContent(Device device)
        {
            //new VertexElement(0, VertexElementFormat.HalfVector4, VertexElementUsage.Position, 0),
            //new VertexElement(8, VertexElementFormat.Byte4, VertexElementUsage.Normal, 0),
            //new VertexElement(12, VertexElementFormat.HalfVector2, VertexElementUsage.TextureCoordinate, 0),
            //new VertexElement(16, VertexElementFormat.HalfVector2, VertexElementUsage.TextureCoordinate, 1)


            var elements = new[] {
        		new VertexElement(0, 0, DeclarationType.HalfFour, DeclarationMethod.Default, DeclarationUsage.Position, 0),
        		new VertexElement(0, 8, DeclarationType.Ubyte4, DeclarationMethod.Default, DeclarationUsage.Normal, 0),
                new VertexElement(0, 12, DeclarationType.HalfTwo, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 0),
                new VertexElement(0, 16, DeclarationType.HalfTwo, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 1),
				VertexElement.VertexDeclarationEnd
        	};

            m_stride = D3DX.GetDeclarationVertexSize(elements, 0);
            System.Diagnostics.Debug.Assert(m_vertexDeclaration == null);
            m_vertexDeclaration = new VertexDeclaration(device, elements);
        }

        public static void UnloadContent()
        {
            if (m_vertexDeclaration != null)
            {
                m_vertexDeclaration.Dispose();
                m_vertexDeclaration = null;
            }
        }
    }
               
    //  Used for drawing models that have diffuse, specular and normal map, plus light map and dirt map
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct MyVertexFormatPositionNormalTextureTangent
    {
        static VertexDeclaration m_vertexDeclaration;
        static int m_stride;

        // packed from 56B to 28B
        public HalfVector4 _PositionScale;
        public HalfVector2 _TexCoord;
        public Byte4 _Normal;
        public Byte4 _Tangent;

        public Vector3 Position
        {
            get { return VF_Packer.UnpackPosition(ref _PositionScale); }
            set { _PositionScale = VF_Packer.PackPosition(ref value); }
        }

        public Vector3 Normal
        {
            get { return VF_Packer.UnpackNormal(ref _Normal); }
            set { _Normal.PackedValue = VF_Packer.PackNormal(ref value); }
        }

        public Vector2 TexCoord
        {
            get { return _TexCoord.ToVector2(); }
            set { _TexCoord = new HalfVector2(value); }
        }

        public Vector3 Tangent
        {
            get { return VF_Packer.UnpackNormal(ref _Tangent); }
            set { _Tangent.PackedValue = VF_Packer.PackNormal(ref value); }
        }

        public HalfVector4 PositionPacked
        {
            get { return _PositionScale; }
            set { _PositionScale = value; }
        }

        public Byte4 NormalPacked
        {
            get { return _Normal; }
            set { _Normal = value; }
        }

        public HalfVector2 TexCoordPacked
        {
            get { return _TexCoord; }
            set { _TexCoord = value; }
        }

        public Byte4 TangentPacked
        {
            get { return _Tangent; }
            set { _Tangent = value; }
        }

        public static VertexDeclaration VertexDeclaration
        {
            get { return m_vertexDeclaration; }
        }

        public static int Stride
        {
            get { return m_stride; }
        }

        public static void LoadContent(Device device)
        {
            //new VertexElement(0, VertexElementFormat.HalfVector4, VertexElementUsage.Position, 0),
            //new VertexElement(8, VertexElementFormat.HalfVector2, VertexElementUsage.TextureCoordinate, 0),
            //new VertexElement(12, VertexElementFormat.Byte4, VertexElementUsage.Normal, 0),
            //new VertexElement(16, VertexElementFormat.Byte4, VertexElementUsage.Tangent, 0),

            var elements = new[] {
        		new VertexElement(0, 0, DeclarationType.HalfFour, DeclarationMethod.Default, DeclarationUsage.Position, 0),
        		new VertexElement(0, 8, DeclarationType.HalfTwo, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 0),
                new VertexElement(0, 12, DeclarationType.Ubyte4, DeclarationMethod.Default, DeclarationUsage.Normal, 0),
                new VertexElement(0, 16, DeclarationType.Ubyte4, DeclarationMethod.Default, DeclarationUsage.Tangent, 0),
				VertexElement.VertexDeclarationEnd
        	};

            m_stride = D3DX.GetDeclarationVertexSize(elements, 0);
            System.Diagnostics.Debug.Assert(m_vertexDeclaration == null);
            m_vertexDeclaration = new VertexDeclaration(device, elements);
        }

        public static void UnloadContent()
        {
            if (m_vertexDeclaration != null)
            {
                m_vertexDeclaration.Dispose();
                m_vertexDeclaration = null;
            }
        }
    }

    //  Used for drawing models that have diffuse, specular and normal map, skinned
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct MyVertexFormatPositionNormalTextureTangentSkinned
    {
        static VertexDeclaration m_vertexDeclaration;
        static int m_stride;

        // packed from 56B to 28B
        public HalfVector4 _PositionScale;
        public HalfVector2 _TexCoord;
        public Byte4 _Normal;
        public Byte4 _Tangent;
        public Byte4 BoneIndices;
        public Vector4 BoneWeights;

        public Vector3 Position
        {
            get { return VF_Packer.UnpackPosition(ref _PositionScale); }
            set { _PositionScale = VF_Packer.PackPosition(ref value); }
        }

        public Vector3 Normal
        {
            get { return VF_Packer.UnpackNormal(ref _Normal); }
            set { _Normal.PackedValue = VF_Packer.PackNormal(ref value); }
        }

        public Vector2 TexCoord
        {
            get { return _TexCoord.ToVector2(); }
            set { _TexCoord = new HalfVector2(value); }
        }

        public Vector3 Tangent
        {
            get { return VF_Packer.UnpackNormal(ref _Tangent); }
            set { _Tangent.PackedValue = VF_Packer.PackNormal(ref value); }
        }

        public HalfVector4 PositionPacked
        {
            get { return _PositionScale; }
            set { _PositionScale = value; }
        }

        public Byte4 NormalPacked
        {
            get { return _Normal; }
            set { _Normal = value; }
        }

        public HalfVector2 TexCoordPacked
        {
            get { return _TexCoord; }
            set { _TexCoord = value; }
        }

        public Byte4 TangentPacked
        {
            get { return _Tangent; }
            set { _Tangent = value; }
        }

        public static VertexDeclaration VertexDeclaration
        {
            get { return m_vertexDeclaration; }
        }

        public static int Stride
        {
            get { return m_stride; }
        }

        public static void LoadContent(Device device)
        {
            //new VertexElement(0, VertexElementFormat.HalfVector4, VertexElementUsage.Position, 0),
            //new VertexElement(8, VertexElementFormat.HalfVector2, VertexElementUsage.TextureCoordinate, 0),
            //new VertexElement(12, VertexElementFormat.Byte4, VertexElementUsage.Normal, 0),
            //new VertexElement(16, VertexElementFormat.Byte4, VertexElementUsage.Tangent, 0),

            var elements = new[] {
        		new VertexElement(0, 0, DeclarationType.HalfFour, DeclarationMethod.Default, DeclarationUsage.Position, 0),
        		new VertexElement(0, 8, DeclarationType.HalfTwo, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 0),
                new VertexElement(0, 12, DeclarationType.Ubyte4, DeclarationMethod.Default, DeclarationUsage.Normal, 0),
                new VertexElement(0, 16, DeclarationType.Ubyte4, DeclarationMethod.Default, DeclarationUsage.Tangent, 0),
                new VertexElement(0, 20, DeclarationType.Ubyte4, DeclarationMethod.Default, DeclarationUsage.BlendIndices, 0),
                new VertexElement(0, 24, DeclarationType.Float4, DeclarationMethod.Default, DeclarationUsage.BlendWeight, 0),
				VertexElement.VertexDeclarationEnd
        	};

            m_stride = D3DX.GetDeclarationVertexSize(elements, 0);
            System.Diagnostics.Debug.Assert(m_vertexDeclaration == null);
            m_vertexDeclaration = new VertexDeclaration(device, elements);
        }

        public static void UnloadContent()
        {
            if (m_vertexDeclaration != null)
            {
                m_vertexDeclaration.Dispose();
                m_vertexDeclaration = null;
            }
        }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct MyVertexFormatPositionNormalTextureTangentMask 
    {
        static VertexDeclaration m_vertexDeclaration;
        static int m_stride;

        // packed from 64B to 28B
        public HalfVector4 _PositionScale;
        public Byte4 _Normal;
        public HalfVector2 _TexCoord;
        public Byte4 _Tangent;
        public HalfVector2 _MaskCoord;

        public Vector3 Position
        {
            get { return VF_Packer.UnpackPosition(ref _PositionScale); }
            set { _PositionScale = VF_Packer.PackPosition(ref value); }
        }

        public HalfVector4 PositionPacked
        {
            get { return _PositionScale; }
            set { _PositionScale = value; }
        }


        public Vector3 Normal
        {
            get { return VF_Packer.UnpackNormal(ref _Normal); }
            set { _Normal.PackedValue = VF_Packer.PackNormal(ref value); }
        }

        public Byte4 NormalPacked
        {
            get { return _Normal; }
            set { _Normal = value; }
        }

        public Vector2 TexCoord
        {
            get { return _TexCoord.ToVector2(); }
            set { _TexCoord = new HalfVector2(value); }
        }

        public HalfVector2 TexCoordPacked
        {
            get { return _TexCoord; }
            set { _TexCoord = value; }
        }

        public Vector3 Tangent
        {
            get { return VF_Packer.UnpackNormal(ref _Tangent); }
            set { _Tangent.PackedValue = VF_Packer.PackNormal(ref value); }
        }

        public Byte4 TangentPacked
        {
            get { return _Tangent; }
            set { _Tangent = value; }
        }

        public Vector2 MaskCoord
        {
            get { return _MaskCoord.ToVector2(); }
            set { _MaskCoord = new HalfVector2(value); }
        }

        public HalfVector2 MaskCoordPacked
        {
            get { return _MaskCoord; }
            set { _MaskCoord = value; }
        }

        public static VertexDeclaration VertexDeclaration
        {
            get { return m_vertexDeclaration; }
        }

        public static int Stride
        {
            get { return m_stride; }
        }

        public static void LoadContent(Device device)
        {
            //new VertexElement(0, VertexElementFormat.HalfVector4, VertexElementUsage.Position, 0),
            //new VertexElement(8, VertexElementFormat.Byte4, VertexElementUsage.Normal, 0),
            //new VertexElement(12, VertexElementFormat.HalfVector2, VertexElementUsage.TextureCoordinate, 0),
            //new VertexElement(16, VertexElementFormat.Byte4, VertexElementUsage.Tangent, 0),
            //new VertexElement(20, VertexElementFormat.HalfVector2, VertexElementUsage.TextureCoordinate, 1)


            var elements = new[] {
        		new VertexElement(0, 0, DeclarationType.HalfFour, DeclarationMethod.Default, DeclarationUsage.Position, 0),
        		new VertexElement(0, 8, DeclarationType.Ubyte4, DeclarationMethod.Default, DeclarationUsage.Normal, 0),
                new VertexElement(0, 12, DeclarationType.HalfTwo, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 0),
                new VertexElement(0, 16, DeclarationType.Ubyte4, DeclarationMethod.Default, DeclarationUsage.Tangent, 0),
                new VertexElement(0, 20, DeclarationType.HalfTwo, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 1),
				VertexElement.VertexDeclarationEnd
        	};

            m_stride = D3DX.GetDeclarationVertexSize(elements, 0);
            System.Diagnostics.Debug.Assert(m_vertexDeclaration == null);
            m_vertexDeclaration = new VertexDeclaration(device, elements);
        }

        public static void UnloadContent()
        {
            if (m_vertexDeclaration != null)
            {
                m_vertexDeclaration.Dispose();
                m_vertexDeclaration = null;
            }
        }
    }

    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct MyVertexFormatDecal 
    {
        static VertexDeclaration m_vertexDeclaration;
        static int m_stride;

        // 76B packed to 36B
        public Vector3 Position;    // Don't pack decal position, because it is stored in world space and can cause Z-fight with original model.
        HalfVector4 _Color;
        HalfVector2 _TexCoord;
        Byte4 _Normal;
        Byte4 _Tangent;
        HalfVector2 _EmissiveRatio;

        public Vector4 Color
        {
            get { return _Color.ToVector4(); }
            set { _Color = new HalfVector4(value); }
        }

        public Vector2 TexCoord
        {
            get { return _TexCoord.ToVector2(); }
            set { _TexCoord = new HalfVector2(value); }
        }

        public Vector3 Normal
        {
            get { return VF_Packer.UnpackNormal(ref _Normal); }
            set { _Normal.PackedValue = VF_Packer.PackNormal(ref value); }
        }

        public Vector3 Tangent
        {
            get { return VF_Packer.UnpackNormal(ref _Tangent); }
            set { _Tangent.PackedValue = VF_Packer.PackNormal(ref value); }
        }

        public float EmissiveRatio
        {
            get { return _EmissiveRatio.ToVector2().X; }
            set { _EmissiveRatio = new HalfVector2(value, 0); }
        }

        public static VertexDeclaration VertexDeclaration
        {
            get { return m_vertexDeclaration; }
        }

        public static int Stride
        {
            get { return m_stride; }
        }

        public static void LoadContent(Device device)
        {
            //new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            //new VertexElement(12, VertexElementFormat.HalfVector4, VertexElementUsage.Color, 0),
            //new VertexElement(20, VertexElementFormat.HalfVector2, VertexElementUsage.TextureCoordinate, 0),
            //new VertexElement(24, VertexElementFormat.Byte4, VertexElementUsage.Normal, 0),
            //new VertexElement(28, VertexElementFormat.Byte4, VertexElementUsage.Tangent, 0),
            //new VertexElement(32, VertexElementFormat.HalfVector2, VertexElementUsage.TextureCoordinate, 1)

            var elements = new[] {
        		new VertexElement(0, 0, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.Position, 0),
        		new VertexElement(0, 12, DeclarationType.HalfFour, DeclarationMethod.Default, DeclarationUsage.Color, 0),
                new VertexElement(0, 20, DeclarationType.HalfTwo, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 0),
                new VertexElement(0, 24, DeclarationType.Ubyte4, DeclarationMethod.Default, DeclarationUsage.Normal, 0),
                new VertexElement(0, 28, DeclarationType.Ubyte4, DeclarationMethod.Default, DeclarationUsage.Tangent, 0),
                new VertexElement(0, 32, DeclarationType.HalfTwo, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 1),
				VertexElement.VertexDeclarationEnd
        	};

            m_stride = D3DX.GetDeclarationVertexSize(elements, 0);
            System.Diagnostics.Debug.Assert(m_vertexDeclaration == null);
            m_vertexDeclaration = new VertexDeclaration(device, elements);
        }

        public static void UnloadContent()
        {
            if (m_vertexDeclaration != null)
            {
                m_vertexDeclaration.Dispose();
                m_vertexDeclaration = null;
            }
        }
    }
          
    internal struct MyVertexFormatGlassDecal 
    {
        static VertexDeclaration m_vertexDeclaration;
        static int m_stride;

        public HalfVector4 _PositionScale;
        public HalfVector2 _TexCoord;
        public Byte4 _Normal;

        public Vector3 Position
        {
            get { return VF_Packer.UnpackPosition(ref _PositionScale); }
            set { _PositionScale = VF_Packer.PackPosition(ref value); }
        }

        // PositionAndAlpha for MyEffectCockpitGlass
        public void SetPositionAndAlpha(ref Vector3 position, float alpha)
        {
            _PositionScale = new HalfVector4(position.X, position.Y, position.Z, alpha);
        }

        public Vector3 Normal
        {
            get { return VF_Packer.UnpackNormal(ref _Normal); }
            set { _Normal.PackedValue = VF_Packer.PackNormal(ref value); }
        }

        public Vector2 TexCoord
        {
            get { return _TexCoord.ToVector2(); }
            set { _TexCoord = new HalfVector2(value); }
        }

        public static VertexDeclaration VertexDeclaration
        {
            get { return m_vertexDeclaration; }
        }

        public static int Stride
        {
            get { return m_stride; }
        }

        public static void LoadContent(Device device)
        {
            //new VertexElement(0, VertexElementFormat.HalfVector4, VertexElementUsage.Position, 0),
            //new VertexElement(8, VertexElementFormat.HalfVector2, VertexElementUsage.TextureCoordinate, 0),
            //new VertexElement(12, VertexElementFormat.Byte4, VertexElementUsage.Normal, 0)

            var elements = new[] {
        		new VertexElement(0, 0, DeclarationType.HalfFour, DeclarationMethod.Default, DeclarationUsage.Position, 0),
        		new VertexElement(0, 8, DeclarationType.HalfFour, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 0),
                new VertexElement(0, 12, DeclarationType.Ubyte4, DeclarationMethod.Default, DeclarationUsage.Normal, 0),
				VertexElement.VertexDeclarationEnd
        	};

            m_stride = D3DX.GetDeclarationVertexSize(elements, 0);
            System.Diagnostics.Debug.Assert(m_vertexDeclaration == null);
            m_vertexDeclaration = new VertexDeclaration(device, elements);
        }

        public static void UnloadContent()
        {
            if (m_vertexDeclaration != null)
            {
                m_vertexDeclaration.Dispose();
                m_vertexDeclaration = null;
            }
        }
    }
                 /*
    //  Used for drawing models that have diffuse, specular and normal map, plus complete map
    struct MyVertexFormatPositionNormalTangentTwoTexture : IVertexType
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TexCoord0;
        public Vector2 TexCoord1;
        public Vector3 Tangent;

        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration
        (
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(sizeof(float) * 3, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
            new VertexElement(sizeof(float) * 6, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(sizeof(float) * 8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1),
            new VertexElement(sizeof(float) * 10, VertexElementFormat.Vector3, VertexElementUsage.Tangent, 0),
        );

        VertexDeclaration IVertexType.VertexDeclaration { get { return VertexDeclaration; } }
    }

    struct MyVertexFormatDebugDrawBillboard : IVertexType
    {
        public Vector3 Position;
        public Vector4 Color;      //  Color stored in TEXCOORD because of clamping
        public Vector2 TexCoord;

        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration
        (
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(sizeof(float) * (0 + 3), VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(sizeof(float) * (0 + 3 + 4), VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1)
        );

        VertexDeclaration IVertexType.VertexDeclaration { get { return VertexDeclaration; } }
    }
    */
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct MyVertexFormatTransparentGeometry
    {
        static VertexDeclaration m_vertexDeclaration;
        static int m_stride;

        public Vector3 Position; //12
        public HalfVector4 TexCoord; //8
        public HalfVector4 Color;    //8
        public HalfVector4 TexCoord2; //8

        public static VertexDeclaration VertexDeclaration
        {
            get { return m_vertexDeclaration; }
        }

        public static int Stride
        {
            get { return m_stride; }
        }

        public static void LoadContent(Device device)
        {
            //new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),    //12
            //new VertexElement(sizeof(float) * (0 + 3), VertexElementFormat.HalfVector4, VertexElementUsage.TextureCoordinate, 0), //8
            //new VertexElement(sizeof(float) * (0 + 3 + 2), VertexElementFormat.HalfVector4, VertexElementUsage.Normal, 0), //8
            //new VertexElement(sizeof(float) * (0 + 3 + 2 + 2), VertexElementFormat.HalfVector2, VertexElementUsage.TextureCoordinate, 1) //4

            var elements = new[] {
        		new VertexElement(0, 0, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.Position, 0), //12
                new VertexElement(0, sizeof(float) * (3), DeclarationType.HalfFour, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 0), //8
                new VertexElement(0, sizeof(float) * (3 + 2), DeclarationType.HalfFour, DeclarationMethod.Default, DeclarationUsage.Normal, 0), //8
                new VertexElement(0, sizeof(float) * (3 + 2 + 2), DeclarationType.HalfFour, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 1), //8
				VertexElement.VertexDeclarationEnd
        	};

            m_stride = D3DX.GetDeclarationVertexSize(elements, 0);
            System.Diagnostics.Debug.Assert(m_vertexDeclaration == null);
            m_vertexDeclaration = new VertexDeclaration(device, elements);
        }

        public static void UnloadContent()
        {
            if (m_vertexDeclaration != null)
            {
                m_vertexDeclaration.Dispose();
                m_vertexDeclaration = null;
            }
        }
    }
    //28 per vertex, 6 vertices per particle
    //10000 particlu = 1680000 kB

    //once:
    //indices + corner pos

    //18 per vertex, 4 vertices per particle
    //1 camera pos = 12
    //radius 2
    //color 8
    //texcoord 8
    //10000 particlu = 12 + 720000
    

    
    /// <summary>
    /// A struct that represents a single vertex in the
    /// vertex buffer.
    /// </summary>
    internal struct MyVertexFormatFullScreenQuad 
    {
        static VertexDeclaration m_vertexDeclaration;
        static int m_stride;

        public Vector3 Position;
        public Vector3 TexCoordAndCornerIndex;

        public static VertexDeclaration VertexDeclaration
        {
            get { return m_vertexDeclaration; }
        }

        public static int Stride
        {
            get { return m_stride; }
        }

        public static void LoadContent(Device device)
        {
            //new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            //new VertexElement(sizeof(float) * (0 + 3), VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0)

            var elements = new[] {
        		new VertexElement(0, 0, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.Position, 0), //12
                new VertexElement(0, sizeof(float) * (0 + 3), DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 0), 
				VertexElement.VertexDeclarationEnd
        	};

            m_stride = D3DX.GetDeclarationVertexSize(elements, 0);
            System.Diagnostics.Debug.Assert(m_vertexDeclaration == null);
            m_vertexDeclaration = new VertexDeclaration(device, elements);
        }

        public static void UnloadContent()
        {
            if (m_vertexDeclaration != null)
            {
                m_vertexDeclaration.Dispose();
                m_vertexDeclaration = null;
            }
        }
    }

    /*
    struct MyVertexFormatPointLightInstance : IVertexType
    {
        public Vector4 PositionAndRadius;
        public Vector4 ColorAndIntensity;
        public Vector4 SpecularColorAndAndFallof;
        // we can also bind more textures and add texture ID here   

        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration
        (
            new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0),
            new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 1),
            new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 2)
        );

        VertexDeclaration IVertexType.VertexDeclaration { get { return VertexDeclaration; } }
    }

    struct MyVertexFormatHemisphereLightInstance : IVertexType
    {
        public Vector4 WorldMatrixRow0;
        public Vector4 WorldMatrixRow1;
        public Vector4 WorldMatrixRow2;
        public Vector4 WorldMatrixRow3;

        public Vector4 PositionAndRadius;
        public Vector4 ColorAndIntensity;
        public Vector4 SpecularColorAndAndFallof;

        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration
        (
            new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0),
            new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 1),
            new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 2),
            new VertexElement(48, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 3),
            new VertexElement(64, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 4),
            new VertexElement(80, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 5),
            new VertexElement(96, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 6)
        );

        VertexDeclaration IVertexType.VertexDeclaration { get { return VertexDeclaration; } }
    }

    struct MyVertexFormatSpotLightInstance : IVertexType
    {
        // Instance World matrix
        public Matrix WorldMatrix;

        // Instance
        public Vector4 PositionAndReflectorRangeAndTextureEnabledSign;
        public Vector4 SpecularColorAndAndReflectorFallof;
        public Vector4 ReflectorDirectionAndConeMaxAngleCos;
        public Vector4 ReflectorColorAndReflectorIntensity;

        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration
        (
            new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0),
            new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 1),
            new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 2),
            new VertexElement(48, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 3),
            new VertexElement(64, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 4),
            new VertexElement(80, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 5),
            new VertexElement(96, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 6),
            new VertexElement(112, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 7)
        );

        VertexDeclaration IVertexType.VertexDeclaration { get { return VertexDeclaration; } }
    }

    struct MyVertexFormatSpotShipLightInstance : IVertexType
    {
        // Instance World matrix
        public Matrix WorldMatrix;

        // Instance LightViewProjection for texture projection
        public Vector4 LightViewProjectionColumn0;
        public Vector4 LightViewProjectionColumn1;
        public Vector4 LightViewProjectionColumn2;

        // Instance
        public Vector4 PositionAndReflectorRangeAndTextureEnabledSign;
        public Vector4 SpecularColorAndAndReflectorFallof;
        public Vector4 ReflectorDirectionAndConeMaxAngleCos;
        public Vector4 ReflectorColorAndReflectorIntensity;

        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration
        (
            new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0),
            new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 1),
            new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 2),
            new VertexElement(48, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 3),
            new VertexElement(64, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 4),
            new VertexElement(80, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 5),
            new VertexElement(96, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 6),
            new VertexElement(112, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 7),
            new VertexElement(128, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 8),
            new VertexElement(144, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 9),
            new VertexElement(160, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 10)
        );

        VertexDeclaration IVertexType.VertexDeclaration { get { return VertexDeclaration; } }
    }

    struct MyVertexFormatMeshInstance : IVertexType
    {
        public MyVertexFormatMeshInstance(ref Matrix world, ref Vector3 diffuseColor, float specularIntensity, float specularPower, float emissivity, ref Vector3 highlightColor)
        {
            Matrix = world;
            Diffuse = new Vector4(diffuseColor, 0);
            SpecularIntensity_SpecularPower_Emisivity_HighlightFlag = new Vector4(specularIntensity, specularPower, emissivity, 0);
            Highlight = highlightColor;
        }

        // Instance matrix
        public Matrix Matrix;
        public Vector4 Diffuse;
        public Vector4 SpecularIntensity_SpecularPower_Emisivity_HighlightFlag;
        public Vector3 Highlight;

        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration
        (
            new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0), // Matrix
            new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 1), // Matrix
            new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 2), // Matrix
            new VertexElement(48, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 3), // Matrix
            new VertexElement(64, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 4), // Diffuse
            new VertexElement(80, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 5), // SpecularIntensity_SpecularPower_Emisivity_HighlightFlag
            new VertexElement(96, VertexElementFormat.Vector3, VertexElementUsage.BlendWeight, 6) // Highlight color
            //new VertexElement(128, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 8)
        );

        VertexDeclaration IVertexType.VertexDeclaration { get { return VertexDeclaration; } }
    }

    struct MyVertexFormatMeshInstanceShadow : IVertexType
    {
        public MyVertexFormatMeshInstanceShadow(Matrix world)
        {
            Matrix = world;
        }

        // Instance matrix
        public Matrix Matrix;

        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration
        (
            new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0),
            new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 1),
            new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 2),
            new VertexElement(48, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 3)
        );

        VertexDeclaration IVertexType.VertexDeclaration { get { return VertexDeclaration; } }
    }
     */

    internal struct MyVertexFormatVoxelSingleMaterial
    {
        internal static int m_stride;
        internal static VertexDeclaration m_vertexDeclaration;

        internal static VertexDeclaration VertexDeclaration
        {
            get { return m_vertexDeclaration; }
        }

        internal static int Stride
        {
            get { return m_stride; }
        }

        public static void LoadContent(Device device)
        {
            var elements = new[] {
                new VertexElement(0, 0, DeclarationType.Short4, DeclarationMethod.Default, DeclarationUsage.Position, 0),
                new VertexElement(0, 8, DeclarationType.Short4, DeclarationMethod.Default, DeclarationUsage.Position, 1),
                new VertexElement(0, 16, DeclarationType.Ubyte4, DeclarationMethod.Default, DeclarationUsage.Normal, 0),
                new VertexElement(0, 20, DeclarationType.Ubyte4, DeclarationMethod.Default, DeclarationUsage.Normal, 1),
                VertexElement.VertexDeclarationEnd
            };

            m_stride = D3DX.GetDeclarationVertexSize(elements, 0);
            System.Diagnostics.Debug.Assert(m_vertexDeclaration == null);
            m_vertexDeclaration = new VertexDeclaration(device, elements);
        }

        public static void UnloadContent()
        {
            if (m_vertexDeclaration != null)
            {
                m_vertexDeclaration.Dispose();
                m_vertexDeclaration = null;
            }
        }
    }

    internal class MyVertexFormats : MyRenderComponentBase
    {
        public override int GetID()
        {
            return (int)MyRenderComponentID.VertexFormats;
        }

        public override void LoadContent(Device device)
        {
            MyVertexFormatPositionNormalTextureTangentMask.LoadContent(device);
            MyVertexFormatPositionNormalTextureTangent.LoadContent(device);
            MyVertexFormatPositionNormalTextureMask.LoadContent(device);
            MyVertexFormatPositionNormalTexture.LoadContent(device);
            MyVertexFormatPositionNormal.LoadContent(device);
            MyVertexFormatTransparentGeometry.LoadContent(device);
            MyVertexFormatPositionTextureColor.LoadContent(device);
            MyVertexFormatGlassDecal.LoadContent(device);
            MyVertexFormatDecal.LoadContent(device);
            MyVertexFormatPositionTexture3.LoadContent(device);
            MyVertexFormatPositionTexture.LoadContent(device);
            MyVertexFormatFullScreenQuad.LoadContent(device);
            MyVertexFormatPositionColor.LoadContent(device);
            MyVertexFormatPosition.LoadContent(device);
            MyVertexFormatPositionFull.LoadContent(device);
            MyVertexFormatVoxelSingleMaterial.LoadContent(device);
            MyVertexFormatPositionNormalTextureTangentSkinned.LoadContent(device);
        }

        public override void UnloadContent()
        {
            MyVertexFormatPositionNormalTextureTangentMask.UnloadContent();
            MyVertexFormatPositionNormalTextureTangent.UnloadContent();
            MyVertexFormatPositionNormalTextureMask.UnloadContent();
            MyVertexFormatPositionNormalTexture.UnloadContent();
            MyVertexFormatPositionNormal.UnloadContent();
            MyVertexFormatTransparentGeometry.UnloadContent();
            MyVertexFormatPositionTextureColor.UnloadContent();
            MyVertexFormatGlassDecal.UnloadContent();
            MyVertexFormatDecal.UnloadContent();
            MyVertexFormatPositionTexture3.UnloadContent();
            MyVertexFormatPositionTexture.UnloadContent();
            MyVertexFormatFullScreenQuad.UnloadContent();
            MyVertexFormatPositionColor.UnloadContent();
            MyVertexFormatPosition.UnloadContent();
            MyVertexFormatPositionFull.UnloadContent();
            MyVertexFormatVoxelSingleMaterial.UnloadContent();
            MyVertexFormatPositionNormalTextureTangentSkinned.UnloadContent();
        }
    }
}
